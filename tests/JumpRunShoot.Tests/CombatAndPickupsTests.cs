using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
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
        engine.Player.Position = new Vector2f(100, 400);
        engine.Player.Health = 3; // damaged
        int initialScore = engine.Player.Score;

        // Place Blue Gem at player
        engine.Pickups[0].Spawn(PickupType.BlueGem, new Vector2f(100, 400));
        engine.Update(0.016f);

        Assert.False(engine.Pickups[0].IsActive, "Gem should be collected");
        Assert.Equal(initialScore + 100, engine.Player.Score);

        // Place MedKit at player
        engine.Pickups[1].Spawn(PickupType.MedKit, new Vector2f(100, 400));
        engine.Update(0.016f);

        Assert.False(engine.Pickups[1].IsActive, "MedKit should be collected");
        Assert.Equal(4, engine.Player.Health);
    }

    [Fact]
    public void Powerup_LostOnDamage_RuleIsEnforced()
    {
        var engine = new GameEngine("POWERUP_TEST");

        // Grant all 3 powerups
        engine.Player.HasRapidFire = true;
        engine.Player.HasHighJumpBoots = true;
        engine.Player.HasSpeedDash = true;

        Assert.True(engine.Player.HasRapidFire);
        Assert.True(engine.Player.HasHighJumpBoots);
        Assert.True(engine.Player.HasSpeedDash);

        // Player takes 1 damage
        engine.Player.TakeDamage(1);

        // Assert all powerups are immediately stripped
        Assert.False(engine.Player.HasRapidFire, "RapidFire should be lost on damage");
        Assert.False(engine.Player.HasHighJumpBoots, "HighJumpBoots should be lost on damage");
        Assert.False(engine.Player.HasSpeedDash, "SpeedDash should be lost on damage");
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
}
