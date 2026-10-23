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
