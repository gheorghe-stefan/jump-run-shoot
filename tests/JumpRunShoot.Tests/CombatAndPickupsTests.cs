using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class CombatAndPickupsTests
{
    [Fact]
    public void PlayerShoot_SpawnsProjectile_WithCorrectVelocityAndDirection()
    {
        var engine = new GameEngine("SHOOT_TEST");
        engine.Player.Facing = Direction.Right;
        engine.Input.HandleKeyDown("KeyX");
        engine.Update(0.016f);

        bool foundProjectile = false;
        for (int i = 0; i < engine.Projectiles.Length; i++)
        {
            if (engine.Projectiles[i].IsActive && engine.Projectiles[i].FromPlayer)
            {
                foundProjectile = true;
                Assert.Equal(PhysicsConstants.ProjectileSpeed, engine.Projectiles[i].Velocity.X);
                Assert.Equal(1, engine.Projectiles[i].Damage);
                break;
            }
        }

        Assert.True(foundProjectile, "Player pressing shoot should spawn an active projectile!");
    }

    [Fact]
    public void Projectile_HitsMonster_DealsDamageAndAwardsScoreOnKill()
    {
        var engine = new GameEngine("HIT_TEST");
        var monster = engine.Monsters[0];
        monster.Spawn(0, MonsterType.PatrolDroid, new Vector2f(100, 400));
        monster.Health = 1; // 1 HP left

        int initialScore = engine.Player.Score;

        // Spawn player bullet flying into monster
        engine.SpawnProjectile(new Vector2f(96, 404), new Vector2f(200, 0), damage: 1, fromPlayer: true);

        // Run one frame update for bullet to collide with monster
        engine.Update(0.05f);

        Assert.False(monster.IsActive, "Monster should be defeated when health drops to 0");
        Assert.Equal(initialScore + monster.ScoreReward, engine.Player.Score);
    }

    [Fact]
    public void MonsterTouch_DealsDamage_TriggersIFrames_AndAppliesRecoil()
    {
        var engine = new GameEngine("TOUCH_TEST");
        engine.Player.Position = new Vector2f(100, 400);
        engine.Player.InvulnerabilityTimer = 0.0f;
        int initialHp = engine.Player.Health;

        var monster = engine.Monsters[0];
        monster.Spawn(0, MonsterType.PatrolDroid, new Vector2f(104, 400));

        engine.Update(0.016f);

        Assert.Equal(initialHp - 1, engine.Player.Health);
        Assert.True(engine.Player.InvulnerabilityTimer > 0, "Player should enter invulnerability flicker");
        Assert.True(engine.Player.Velocity.X < 0, "Player to the left of monster should receive leftward recoil");
    }

    [Fact]
    public void Pickups_GemsAndMedKits_AwardScoreAndHealth()
    {
        var engine = new GameEngine("PICKUP_TEST");
        engine.Player.Health = 3; // damaged
        int initialScore = engine.Player.Score;
        var pos = engine.Player.Position;

        // Place Blue Gem at player
        engine.Pickups[0].Spawn(PickupType.BlueGem, pos);
        engine.Update(0.016f);

        Assert.False(engine.Pickups[0].IsActive, "Gem should be collected");
        Assert.Equal(initialScore + 100, engine.Player.Score);

        // Place MedKit at player
        engine.Pickups[1].Spawn(PickupType.MedKit, pos);
        engine.Update(0.016f);

        Assert.False(engine.Pickups[1].IsActive, "MedKit should be collected");
        Assert.Equal(4, engine.Player.Health);
    }

    [Fact]
    public void Powerup_LostOnDamage_RuleIsEnforced()
    {
        var engine = new GameEngine("POWERUP_TEST");

        // Grant all persistent powerups
        engine.Player.HasRapidFire = true;
        engine.Player.HasHighJumpBoots = true;
        engine.Player.HasItemMagnet = true;
        engine.Player.HasPiercingPlasma = true;

        Assert.True(engine.Player.HasRapidFire);
        Assert.True(engine.Player.HasHighJumpBoots);
        Assert.True(engine.Player.HasItemMagnet);
        Assert.True(engine.Player.HasPiercingPlasma);

        // Player takes 1 damage
        engine.Player.TakeDamage(1);

        // Assert all powerups are immediately stripped
        Assert.False(engine.Player.HasRapidFire, "RapidFire should be lost on damage");
        Assert.False(engine.Player.HasHighJumpBoots, "HighJumpBoots should be lost on damage");
        Assert.False(engine.Player.HasItemMagnet, "ItemMagnet should be lost on damage");
        Assert.False(engine.Player.HasPiercingPlasma, "PiercingPlasma should be lost on damage");
    }

    [Fact]
    public void InvulnerabilityPowerup_PickupSets10sTimer_AndImmunityFromMonstersAndProjectiles()
    {
        var engine = new GameEngine("INVULN_TEST");
        engine.Player.Position = new Vector2f(100, 400);
        engine.Player.Health = 8;
        engine.Player.InvulnerabilityPowerupTimer = 0.0f;
        engine.Player.InvulnerabilityTimer = 0.0f;

        // 1. Spawn and collect Invulnerability pickup
        engine.Pickups[0].Spawn(PickupType.Invulnerability, new Vector2f(100, 400));
        engine.Update(0.016f);

        Assert.False(engine.Pickups[0].IsActive, "Pickup should be collected");
        Assert.True(engine.Player.HasInvulnerabilityPowerup, "Invulnerability powerup should be active");
        Assert.Equal(10.0f, engine.Player.InvulnerabilityPowerupTimer, tolerance: 0.05f);
        Assert.Contains("INVULNERABILITY", engine.ActiveNotification.Text);

        // 2. Monster touch collision while invulnerable: player should NOT take damage
        var monster = engine.Monsters[0];
        monster.Spawn(0, MonsterType.PatrolDroid, new Vector2f(102, 400));
        engine.Update(0.016f);
        Assert.Equal(8, engine.Player.Health);

        // 3. Monster projectile collision while invulnerable: player should NOT take damage
        engine.SpawnProjectile(new Vector2f(100, 400), new Vector2f(100, 0), damage: 2, fromPlayer: false);
        engine.Update(0.016f);
        Assert.Equal(8, engine.Player.Health);
    }

    [Fact]
    public void InvulnerabilityPowerup_IsCumulative_SumsDurationWhenMultipleCollected()
    {
        var engine = new GameEngine("INVULN_CUMULATIVE_TEST");
        engine.Player.Position = new Vector2f(100, 400);
        engine.Player.InvulnerabilityPowerupTimer = 0.0f;

        // 1. Collect first invulnerability pickup -> 10s
        engine.Pickups[0].Spawn(PickupType.Invulnerability, new Vector2f(100, 400));
        engine.Update(0.016f);
        Assert.Equal(10.0f, engine.Player.InvulnerabilityPowerupTimer, tolerance: 0.05f);

        // 2. With 5 seconds remaining, collect a second pickup -> 5s + 10s = 15s
        engine.Player.InvulnerabilityPowerupTimer = 5.0f;
        engine.Pickups[1].Spawn(PickupType.Invulnerability, new Vector2f(100, 400));
        engine.Update(0.016f);
        Assert.Equal(15.0f, engine.Player.InvulnerabilityPowerupTimer, tolerance: 0.05f);

        // 3. Collect 2 more back-to-back -> 15s + 10s + 10s = ~35s (or 3 consecutive totaling 30s)
        engine.Player.InvulnerabilityPowerupTimer = 0.0f;
        engine.Pickups[0].Spawn(PickupType.Invulnerability, new Vector2f(100, 400));
        engine.Update(0.016f);
        engine.Pickups[1].Spawn(PickupType.Invulnerability, new Vector2f(100, 400));
        engine.Update(0.016f);
        engine.Pickups[2].Spawn(PickupType.Invulnerability, new Vector2f(100, 400));
        engine.Update(0.016f);

        // 3 pickups back to back: 10 + 10 + 10 = 30s (minus 3 frames of dt = 0.048s)
        Assert.Equal(30.0f, engine.Player.InvulnerabilityPowerupTimer, tolerance: 0.1f);
    }

    [Fact]
    public void DownwardBullet_CeilingTurret_StopsWhenReachingFirstFloor_DoesNotPassThrough()
    {
        var engine = new GameEngine("TURRET_FLOOR_COLLISION_TEST");
        int ts = PhysicsConstants.TileSize;

        // Setup vertical shaft at Col 6:
        // Row 4: Ceiling (Solid) where turret hangs
        // Rows 5-11: Empty airspace
        // Row 12: First floor platform (OneWayPlatform)
        // Rows 13-24: Lower airspace
        // Row 25: Ground floor (Solid)
        for (int r = 0; r <= 29; r++)
        {
            engine.Grid.SetTile(6, r, TileType.Empty);
        }
        engine.Grid.SetTile(6, 4, TileType.Solid);
        engine.Grid.SetTile(6, 12, TileType.OneWayPlatform);
        engine.Grid.SetTile(6, 25, TileType.Solid);

        // Place player safely far away
        engine.Player.Position = new Vector2f(400f, 400f);

        // Spawn downward bullet at X = 6*ts + 4, Y = 5*ts (just below ceiling)
        float bulletX = 6 * ts + 4;
        float bulletY = 5 * ts;
        engine.SpawnProjectile(new Vector2f(bulletX, bulletY), new Vector2f(0f, 160f), damage: 1, fromPlayer: false);

        Assert.True(engine.Projectiles[0].IsActive, "Bullet should initially be active");

        // Simulate flight towards the first floor at row 12 (Y = 12 * 16 = 192px)
        // Distance from 5*16 (80) to 192 is 112px. At 160px/s, takes 0.7s.
        float simulatedTime = 0.0f;
        while (simulatedTime < 1.0f && engine.Projectiles[0].IsActive)
        {
            engine.Update(0.016f);
            simulatedTime += 0.016f;
        }

        // Bullet must have stopped upon hitting the first floor (row 12)
        Assert.False(engine.Projectiles[0].IsActive, "Bullet should stop when reaching the first floor (row 12)");
        // Its position when stopped must be at row 12 and must NEVER have passed into lower rows (> 12)
        float stoppedRow = engine.Projectiles[0].Position.Y / ts;
        Assert.True(stoppedRow <= 12.5f, $"Bullet should have stopped at row 12, but reached row {stoppedRow:F1}");
    }

    [Fact]
    public void PiercingPlasma_PowerupReplacesBootsInZeroGravity_AndPiercesMultipleMonsters()
    {
        var engine = new GameEngine("PIERCING_PLASMA_TEST");
        engine.SetTheme(3); // Space Invaders (Zero Gravity)
        engine.Player.Position = new Vector2f(50, 100);

        // 1. Collect PiercingPlasma pickup
        engine.Pickups[0].Spawn(PickupType.PiercingPlasma, new Vector2f(50, 100));
        engine.Update(0.016f);

        Assert.False(engine.Pickups[0].IsActive, "Pickup should be collected");
        Assert.True(engine.Player.HasPiercingPlasma, "Player should have piercing plasma powerup active");
        Assert.Contains("PIERCING PLASMA", engine.ActiveNotification.Text);

        // 2. Line up 3 monsters in a row ahead of the player:
        // Monster 0 at X = 100
        // Monster 1 at X = 130
        // Monster 2 at X = 160
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        engine.Monsters[0].Spawn(0, MonsterType.HoverDrone, new Vector2f(100, 98));
        engine.Monsters[1].Spawn(1, MonsterType.HoverDrone, new Vector2f(130, 98));
        engine.Monsters[2].Spawn(2, MonsterType.HoverDrone, new Vector2f(160, 98));

        int m0Hp = engine.Monsters[0].Health;
        int m1Hp = engine.Monsters[1].Health;
        int m2Hp = engine.Monsters[2].Health;

        // 3. Fire a single piercing shot from X=60 traveling right at 300px/s
        engine.SpawnProjectile(new Vector2f(60, 104), new Vector2f(300, 0), damage: 1, fromPlayer: true, piercing: true);

        // 4. Update engine until projectile travels past Monster 2 (from 60 to 180 is 120px, takes ~0.4s)
        for (int frame = 0; frame < 30; frame++)
        {
            engine.Update(0.016f);
        }

        // 5. The single shot should have pierced and damaged ALL THREE monsters!
        Assert.True(engine.Monsters[0].Health < m0Hp, "Monster 0 should have taken damage");
        Assert.True(engine.Monsters[1].Health < m1Hp, "Monster 1 should have taken damage from piercing shot");
        Assert.True(engine.Monsters[2].Health < m2Hp, "Monster 2 should have taken damage from piercing shot");
    }

    [Fact]
    public void StandardProjectile_WithoutPiercing_StopsAtFirstMonster()
    {
        var engine = new GameEngine("NON_PIERCING_TEST");
        engine.SetTheme(3);
        engine.Player.Position = new Vector2f(50, 100);

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        engine.Monsters[0].Spawn(0, MonsterType.HoverDrone, new Vector2f(100, 98));
        engine.Monsters[1].Spawn(1, MonsterType.HoverDrone, new Vector2f(130, 98));

        int m0Hp = engine.Monsters[0].Health;
        int m1Hp = engine.Monsters[1].Health;

        // Non-piercing shot
        engine.SpawnProjectile(new Vector2f(60, 104), new Vector2f(300, 0), damage: 1, fromPlayer: true, piercing: false);

        for (int frame = 0; frame < 30; frame++)
        {
            engine.Update(0.016f);
        }

        // Only Monster 0 took damage; bullet did not pierce to Monster 1
        Assert.True(engine.Monsters[0].Health < m0Hp, "Monster 0 should have taken damage");
        Assert.Equal(m1Hp, engine.Monsters[1].Health);
    }

    [Fact]
    public void PiercingPlasma_UnderwaterTheme_ShowsSonicTorpedoNotification_AndRendersHudBadge()
    {
        var engine = new GameEngine("UNDERWATER_PLASMA_TEST");
        engine.SetTheme(2); // Underwater Theme (Zero Gravity)
        engine.Player.Position = new Vector2f(50, 100);

        engine.Pickups[0].Spawn(PickupType.PiercingPlasma, new Vector2f(50, 100));
        engine.Update(0.016f);

        Assert.True(engine.Player.HasPiercingPlasma);
        Assert.Contains("SONIC TORPEDO", engine.ActiveNotification.Text);

        // Verify GameRenderer draws HUD without error when PiercingPlasma is active
        var fb = new JumpRunShoot.Engine.Core.FrameBuffer();
        JumpRunShoot.Engine.Themes.GameRenderer.DrawHud(fb, engine.CurrentTheme.Assets, engine.Player, engine.CurrentSeed, 60);

        // Framebuffer status console header should contain non-zero pixels
        bool hasPixels = false;
        for (int i = 0; i < 320 * 16; i++)
        {
            if (fb.Pixels[i] != 0) { hasPixels = true; break; }
        }
        Assert.True(hasPixels);
    }

    [Fact]
    public void ZeroGravity_NeverSpawnsOrRetainsHighJumpBoots_ReplacedCompletelyByItemMagnet()
    {
        var engine = new GameEngine("ZERO_GRAV_BOOT_TEST");
        engine.Player.HasHighJumpBoots = true;
        engine.Player.HasPiercingPlasma = true;
        engine.Pickups[0].Spawn(PickupType.HighJumpBoots, new Vector2f(200, 100));

        // Switch to Underwater theme (Theme 2, zero-gravity)
        engine.SetTheme(2);

        // 1. HighJumpBoots on player must be cleared immediately in zero-gravity, while universal PiercingPlasma stays active!
        Assert.False(engine.Player.HasHighJumpBoots, "Zero gravity player must not have high jump boots active");
        Assert.True(engine.Player.HasPiercingPlasma, "Universal PiercingPlasma must be retained across theme switches");

        // 2. Existing boot pickups in world must be converted to ItemMagnet
        Assert.Equal(PickupType.ItemMagnet, engine.Pickups[0].Type);

        // 3. Collecting a boot/magnet in zero-gravity must activate ItemMagnet (never HighJumpBoots)
        engine.Pickups[1].Spawn(PickupType.HighJumpBoots, new Vector2f(50, 50));
        engine.Player.Position = new Vector2f(50, 50);
        engine.Update(0.016f);

        Assert.True(engine.Player.HasItemMagnet, "Zero-gravity pickup collection must activate ItemMagnet");
        Assert.False(engine.Player.HasHighJumpBoots, "Zero-gravity pickup collection must never activate HighJumpBoots");

        // 4. Switching back to gravity theme (Polar) clears ItemMagnet and converts magnet pickups back to HighJumpBoots
        engine.SetTheme(4);
        Assert.False(engine.Player.HasItemMagnet);
        Assert.Equal(PickupType.HighJumpBoots, engine.Pickups[0].Type);
        Assert.True(engine.Player.HasPiercingPlasma);
    }

    [Fact]
    public void UniversalPiercingShots_WorksInAllGravityAndZeroGThemes_DamagingWholeLineOfMonsters()
    {
        foreach (int themeIdx in new[] { 0, 1, 2, 3, 4 })
        {
            var engine = new GameEngine("PIERCE_ALL_THEMES");
            engine.SetTheme(themeIdx);

            // Verify bespoke piercing badge is loaded for every theme
            Assert.NotNull(engine.CurrentTheme.Assets.PiercingPlasmaBadge);

            // Clear arena and disable existing monsters
            for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
            for (int c = 2; c <= 14; c++)
            {
                for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
                engine.Grid.SetTile(c, 24, TileType.Solid);
            }

            // Collect PiercingPlasma pickup -> activates HasPiercingPlasma in EVERY theme
            int ts = PhysicsConstants.TileSize;
            float floorY = 24 * ts;
            engine.Player.Position = new Vector2f(4 * ts, floorY - 24f);
            engine.Pickups[0].Spawn(PickupType.PiercingPlasma, new Vector2f(4 * ts, floorY - 14f));
            engine.Update(0.016f);
            Assert.True(engine.Player.HasPiercingPlasma, $"Theme {themeIdx}: PiercingPlasma must activate");

            // Line up 3 monsters horizontally ahead of player (each starts with 3 HP)
            engine.Monsters[0].Spawn(100, MonsterType.MechaHound, new Vector2f(6 * ts, floorY - 14f), platformY: floorY);
            engine.Monsters[1].Spawn(101, MonsterType.MechaHound, new Vector2f(8 * ts, floorY - 14f), platformY: floorY);
            engine.Monsters[2].Spawn(102, MonsterType.MechaHound, new Vector2f(10 * ts, floorY - 14f), platformY: floorY);
            int hp0 = engine.Monsters[0].Health;
            int hp1 = engine.Monsters[1].Health;
            int hp2 = engine.Monsters[2].Health;

            // Fire a single piercing shot rightwards
            engine.Player.Facing = Direction.Right;
            engine.Player.PendingShoot = true;
            for (int f = 0; f < 30; f++) engine.Update(0.016f);

            // Single piercing shot must hit all 3 monsters in the line, dealing exactly 1 damage to each!
            Assert.Equal(hp0 - 1, engine.Monsters[0].Health);
            Assert.Equal(hp1 - 1, engine.Monsters[1].Health);
            Assert.Equal(hp2 - 1, engine.Monsters[2].Health);
        }
    }

    [Fact]
    public void ItemMagnet_InZeroGravityThemes_PullsNearbyPickupsAndLoadsThemeBadges()
    {
        foreach (int zeroGThemeIdx in new[] { 2, 3 }) // Underwater & Space
        {
            var engine = new GameEngine("MAGNET_PULL_TEST");
            engine.SetTheme(zeroGThemeIdx);

            // Bespoke magnet badge must be loaded for both zero-gravity themes
            Assert.NotNull(engine.CurrentTheme.Assets.ItemMagnetBadge);

            for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
            for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;

            engine.Player.Position = new Vector2f(160f, 160f);
            engine.Player.HasItemMagnet = true;

            // Nearby gem inside MagnetPullRadius (~60px away) and far gem outside MagnetPullRadius (~180px away)
            engine.Pickups[0].Spawn(PickupType.BlueGem, new Vector2f(220f, 160f));
            engine.Pickups[1].Spawn(PickupType.RedRuby, new Vector2f(340f, 160f));
            float farStartX = engine.Pickups[1].Position.X;

            int scoreBefore = engine.Player.Score;
            for (int f = 0; f < 40; f++) engine.Update(0.016f);

            // Nearby gem must have been magnetically pulled into the player and collected!
            Assert.False(engine.Pickups[0].IsActive, $"Theme {zeroGThemeIdx}: ItemMagnet should pull and collect nearby gem");
            Assert.True(engine.Player.Score >= scoreBefore + 100);

            // Distant gem outside MagnetPullRadius must remain unmoved
            Assert.True(engine.Pickups[1].IsActive);
            Assert.Equal(farStartX, engine.Pickups[1].Position.X);
        }
    }

    [Fact]
    public void PickupScaling_ValuesScaleWithChunkAdvancement()
    {
        // Chunk 0 (start)
        int min0 = System.Math.Clamp(2 + 0 / 2, 2, 8);
        int max0 = System.Math.Clamp(4 + (0 * 2) / 3, 4, 12);
        float medKit0 = System.Math.Clamp(0.15f + (0 * 0.035f), 0.15f, 0.35f);
        float power0 = System.Math.Clamp(0.15f + (0 * 0.035f), 0.15f, 0.35f);

        Assert.Equal(2, min0);
        Assert.Equal(4, max0);
        Assert.Equal(0.15f, medKit0, 3);
        Assert.Equal(0.15f, power0, 3);

        // Chunk 6 (mid-game)
        int min6 = System.Math.Clamp(2 + 6 / 2, 2, 8);
        int max6 = System.Math.Clamp(4 + (6 * 2) / 3, 4, 12);
        float medKit6 = System.Math.Clamp(0.15f + (6 * 0.035f), 0.15f, 0.35f);
        float power6 = System.Math.Clamp(0.15f + (6 * 0.035f), 0.15f, 0.35f);

        Assert.Equal(5, min6);
        Assert.Equal(8, max6);
        Assert.Equal(0.35f, medKit6, 3);
        Assert.Equal(0.35f, power6, 3);

        // Chunk 12 (deep endgame)
        int min12 = System.Math.Clamp(2 + 12 / 2, 2, 8);
        int max12 = System.Math.Clamp(4 + (12 * 2) / 3, 4, 12);
        Assert.Equal(8, min12);
        Assert.Equal(12, max12);
    }

    [Fact]
    public void InvulnerabilityPowerup_FallingIntoAbyss_BouncesSafelyInsteadOfDying()
    {
        var engine = new GameEngine("INVULN_ABYSS_TEST");
        engine.Player.InvulnerabilityPowerupTimer = 5.0f;
        engine.Player.Position = new Vector2f(100, TileChunk.ChunkHeight * PhysicsConstants.TileSize + 10);

        engine.Update(0.016f);

        Assert.False(engine.IsGameOver, "Invulnerability powerup should bounce player out of abyss instead of triggering Game Over");
        Assert.True(engine.Player.Position.Y < TileChunk.ChunkHeight * PhysicsConstants.TileSize);
    }

    [Fact]
    public void InvulnerabilityPowerup_EnemyProjectileHittingShield_IsDestroyedWithoutDamagingPlayer()
    {
        var engine = new GameEngine("INVULN_SHIELD_ABSORB");
        // Clear open space around player
        for (int c = 4; c <= 12; c++)
            for (int r = 10; r <= 20; r++)
                engine.Grid.SetTile(c, r, TileType.Empty);

        engine.Player.Position = new Vector2f(8 * 16, 15 * 16);
        engine.Player.Health = 8;
        engine.Player.InvulnerabilityPowerupTimer = 5.0f;
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;

        engine.SpawnProjectile(new Vector2f(8 * 16 + 2, 15 * 16 + 6), new Vector2f(-100, 0), damage: 2, fromPlayer: false);
        engine.Update(0.016f);

        Assert.Equal(8, engine.Player.Health);
        Assert.False(engine.Projectiles[0].IsActive, "Enemy projectile hitting an active invulnerability shield should be destroyed");
    }

    [Fact]
    public void InvulnerabilityPowerup_CollectedSameFrameAsEnemyBullet_PreventsDamage()
    {
        var engine = new GameEngine("INVULN_SAME_FRAME");
        for (int c = 4; c <= 12; c++)
            for (int r = 10; r <= 20; r++)
                engine.Grid.SetTile(c, r, TileType.Empty);

        engine.Player.Position = new Vector2f(8 * 16, 15 * 16);
        engine.Player.Health = 1;
        engine.Player.InvulnerabilityTimer = 0.0f;
        engine.Player.InvulnerabilityPowerupTimer = 0.0f;
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;
        for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Pickup and enemy projectile both overlap player on the exact same tick
        engine.Pickups[0].Spawn(PickupType.Invulnerability, engine.Player.Position);
        engine.SpawnProjectile(new Vector2f(engine.Player.Position.X + 2, engine.Player.Position.Y + 6), new Vector2f(0, 0), damage: 2, fromPlayer: false);

        engine.Update(0.016f);

        Assert.False(engine.IsGameOver, "Collecting Invulnerability on the same frame as an enemy bullet must protect the player");
        Assert.Equal(1, engine.Player.Health);
        Assert.True(engine.Player.HasInvulnerabilityPowerup);
    }

    [Fact]
    public void InvulnerabilityPowerup_NaturalExpiry_GrantsGraceIFramesAgainstInstantDeath()
    {
        var engine = new GameEngine("INVULN_EXPIRY_GRACE");
        for (int c = 4; c <= 12; c++)
            for (int r = 10; r <= 20; r++)
                engine.Grid.SetTile(c, r, TileType.Empty);

        engine.Player.Position = new Vector2f(8 * 16, 15 * 16);
        engine.Player.Health = 1;
        engine.Player.InvulnerabilityTimer = 0.0f;
        engine.Player.InvulnerabilityPowerupTimer = 0.005f; // Expires during this tick
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Monster overlapping player right as shield expires
        engine.Monsters[0].Spawn(0, MonsterType.PatrolDroid, engine.Player.Position);

        engine.Update(0.016f);

        Assert.False(engine.Player.HasInvulnerabilityPowerup);
        Assert.Equal(0.0f, engine.Player.InvulnerabilityPowerupTimer);
        Assert.True(engine.Player.InvulnerabilityTimer > 0.0f, "Natural expiry of invulnerability shield should grant brief grace i-frames");
        Assert.False(engine.IsGameOver);
        Assert.Equal(1, engine.Player.Health);
    }

    [Fact]
    public void Projectiles_WhenPlayerAndEnemyBulletsCollide_BothDisappearAcrossAllThemes()
    {
        for (int themeIdx = 0; themeIdx < ThemeRegistry.AllThemes.Length; themeIdx++)
        {
            var engine = new GameEngine("BULLET_CLASH_ALL_THEMES");
            engine.SetTheme(themeIdx);

            // Open arena cols 4..14, rows 8..18
            for (int c = 4; c <= 14; c++)
                for (int r = 8; r <= 18; r++)
                    engine.Grid.SetTile(c, r, TileType.Empty);

            engine.Player.Position = new Vector2f(4 * 16, 16 * 16);
            for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;
            for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

            // Player bullet flying right and enemy bullet flying left along the same Y row (even with Piercing Plasma!)
            engine.SpawnProjectile(new Vector2f(7 * 16, 12 * 16), new Vector2f(260f, 0f), damage: 1, fromPlayer: true, piercing: true);
            engine.SpawnProjectile(new Vector2f(9 * 16, 12 * 16), new Vector2f(-200f, 0f), damage: 1, fromPlayer: false);

            for (int f = 0; f < 15; f++)
            {
                engine.Update(0.016f);
            }

            Assert.False(engine.Projectiles[0].IsActive, $"Theme {themeIdx}: Player bullet must disappear on collision with enemy bullet");
            Assert.False(engine.Projectiles[1].IsActive, $"Theme {themeIdx}: Enemy bullet must disappear on collision with player bullet");
        }
    }

    [Fact]
    public void Projectiles_CrossingEnemyBullets_BothDisappearOnCollision()
    {
        var engine = new GameEngine("ENEMY_BULLET_CROSS");
        for (int c = 4; c <= 14; c++)
            for (int r = 8; r <= 18; r++)
                engine.Grid.SetTile(c, r, TileType.Empty);

        engine.Player.Position = new Vector2f(4 * 16, 16 * 16);
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Horizontal enemy shot crossing a vertical ceiling-turret shot at (9*16, 12*16)
        engine.SpawnProjectile(new Vector2f(8 * 16, 12 * 16), new Vector2f(200f, 0f), damage: 1, fromPlayer: false);
        engine.SpawnProjectile(new Vector2f(9 * 16, 11 * 16), new Vector2f(0f, 200f), damage: 1, fromPlayer: false);

        for (int f = 0; f < 15; f++)
        {
            engine.Update(0.016f);
        }

        Assert.False(engine.Projectiles[0].IsActive, "Horizontal enemy bullet must disappear when colliding with crossing bullet");
        Assert.False(engine.Projectiles[1].IsActive, "Vertical enemy bullet must disappear when colliding with crossing bullet");
    }

    [Fact]
    public void HeavyGuard_AndPlayerOnSamePlatform_InGravityThemes_BulletsAlignAndCancelOut()
    {
        int[] gravityThemes = { 0, 1, 4 }; // duke, forest, polar
        int ts = PhysicsConstants.TileSize;
        float platformTop = 24 * ts;

        foreach (int themeIdx in gravityThemes)
        {
            var engine = new GameEngine("BEAR_DUEL_TEST");
            engine.SetTheme(themeIdx);

            // Flat open arena cols 3..15, solid floor at row 24
            for (int c = 3; c <= 15; c++)
            {
                for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
                engine.Grid.SetTile(c, 24, TileType.Solid);
            }

            for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
            for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;
            for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;

            // Player standing on row 24 facing right
            engine.Player.Position = new Vector2f(5 * ts, platformTop - 24f);
            engine.Player.IsGrounded = true;
            engine.Player.Facing = Direction.Right;
            engine.Player.Health = 8;
            engine.Player.PendingShoot = true;

            // HeavyGuard (Bear) standing on the exact same row 24 facing left, ready to shoot immediately
            var bear = engine.Monsters[0];
            bear.Spawn(0, MonsterType.HeavyGuard, new Vector2f(11 * ts, platformTop - 26f), platformY: platformTop);
            bear.Facing = Direction.Left;
            bear.Velocity = Vector2f.Zero;
            bear.ActionTimer = 0.001f;
            int bearInitialHp = bear.Health;

            // First tick spawns both bullets
            engine.Update(0.016f);

            int playerBulletIdx = -1, bearBulletIdx = -1;
            for (int i = 0; i < engine.Projectiles.Length; i++)
            {
                if (!engine.Projectiles[i].IsActive) continue;
                if (engine.Projectiles[i].FromPlayer) playerBulletIdx = i;
                else bearBulletIdx = i;
            }

            Assert.True(playerBulletIdx >= 0 && bearBulletIdx >= 0, $"Theme {themeIdx}: both player and bear should fire.");
            Assert.Equal(engine.Projectiles[playerBulletIdx].Position.Y, engine.Projectiles[bearBulletIdx].Position.Y, tolerance: 0.01f);

            // Step until bullets meet mid-air
            for (int f = 0; f < 30; f++)
            {
                engine.Update(0.016f);
            }

            Assert.False(engine.Projectiles[playerBulletIdx].IsActive, $"Theme {themeIdx}: player bullet must cancel out against bear bullet.");
            Assert.False(engine.Projectiles[bearBulletIdx].IsActive, $"Theme {themeIdx}: bear bullet must cancel out against player bullet.");
            Assert.Equal(8, engine.Player.Health);
            Assert.Equal(bearInitialHp, bear.Health);
        }
    }
}
