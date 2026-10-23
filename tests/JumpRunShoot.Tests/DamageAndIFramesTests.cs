using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using Xunit;

namespace JumpRunShoot.Tests;

public class DamageAndIFramesTests
{
    [Fact]
    public void TakeDamage_ReducesHealthPipsAndSetsIFramesTimer()
    {
        var player = new PlayerEntity(new Vector2f(50, 50));
        Assert.Equal(8, player.Health);
        Assert.Equal(8, PlayerEntity.MaxHealth);
        Assert.Equal(0.0f, player.InvulnerabilityTimer);

        player.TakeDamage(2);

        Assert.Equal(6, player.Health);
        Assert.Equal(PlayerEntity.PostHitInvulnerabilityDuration, player.InvulnerabilityTimer, tolerance: 0.01f);
        Assert.Equal(2.0f, player.InvulnerabilityTimer, tolerance: 0.01f);
    }

    [Fact]
    public void TakeDamage_WhileInvulnerable_IsIgnored()
    {
        var player = new PlayerEntity(new Vector2f(50, 50));
        player.TakeDamage(1);
        Assert.Equal(7, player.Health);
        Assert.True(player.InvulnerabilityTimer > 0);

        // Try to deal another 2 damage immediately
        player.TakeDamage(2);

        // Health should still be 7 because player is in i-frames!
        Assert.Equal(7, player.Health);
    }

    [Fact]
    public void TakeDamage_AfterIFramesExpire_AppliesDamageAgain()
    {
        var player = new PlayerEntity(new Vector2f(50, 50));
        player.TakeDamage(1);
        Assert.Equal(7, player.Health);

        // Fast-forward time past 1.2s
        player.InvulnerabilityTimer = 0.0f;

        player.TakeDamage(2);
        Assert.Equal(5, player.Health);
    }

    [Fact]
    public void MonsterOverlap_DealsTouchDamage_OnlyWhenVulnerable()
    {
        var player = new PlayerEntity(new Vector2f(50, 50));
        // Simulate a monster with AABB at (52, 50, 16, 16) overlapping player
        var monsterBox = new AABB(52, 50, 16, 16);
        int monsterTouchDamage = 1;

        // 1. First overlap: player is vulnerable
        if (player.BoundingBox.Overlaps(monsterBox) && player.InvulnerabilityTimer <= 0)
        {
            player.TakeDamage(monsterTouchDamage);
        }
        Assert.Equal(7, player.Health);
        Assert.True(player.InvulnerabilityTimer > 0);

        // 2. Second frame while still overlapping inside i-frames:
        if (player.BoundingBox.Overlaps(monsterBox) && player.InvulnerabilityTimer <= 0)
        {
            player.TakeDamage(monsterTouchDamage);
        }
        // Health must remain 7!
        Assert.Equal(7, player.Health);
    }

    [Fact]
    public void TakeDamage_WhenGodMode_IsIgnored()
    {
        var player = new PlayerEntity(new Vector2f(50, 50));
        player.IsGodMode = true;

        player.TakeDamage(5);

        Assert.Equal(8, player.Health);
        Assert.Equal(0.0f, player.InvulnerabilityTimer);
    }

    [Fact]
    public void InvulnerabilityPowerup_GrantsImmunity_AndCountdownExpires()
    {
        var player = new PlayerEntity(new Vector2f(50, 50));
        player.InvulnerabilityPowerupTimer = 10.0f;
        Assert.True(player.HasInvulnerabilityPowerup);

        // While invulnerability powerup is active, damage is completely ignored
        player.TakeDamage(3);
        Assert.Equal(8, player.Health);
        Assert.Equal(0.0f, player.InvulnerabilityTimer);

        // Fast-forward 6 seconds
        player.InvulnerabilityPowerupTimer = 4.0f;
        Assert.True(player.HasInvulnerabilityPowerup);
        player.TakeDamage(2);
        Assert.Equal(8, player.Health);

        // Fast-forward to expiration
        player.InvulnerabilityPowerupTimer = 0.0f;
        Assert.False(player.HasInvulnerabilityPowerup);

        // Now damage applies and triggers 2.0s post-hit i-frames
        player.TakeDamage(2);
        Assert.Equal(6, player.Health);
        Assert.Equal(PlayerEntity.PostHitInvulnerabilityDuration, player.InvulnerabilityTimer, tolerance: 0.01f);
        Assert.Equal(2.0f, player.InvulnerabilityTimer, tolerance: 0.01f);
    }

    [Fact]
    public void InvulnerabilityPowerup_PreservesRetainedPowerupsOnHitAttempt()
    {
        var player = new PlayerEntity(new Vector2f(50, 50));
        player.HasRapidFire = true;
        player.HasHighJumpBoots = true;
        player.HasSpeedDash = true;
        player.InvulnerabilityPowerupTimer = 10.0f;

        // Damage attempt while invulnerable powerup is active
        player.TakeDamage(1);

        // Powerups must NOT be lost!
        Assert.True(player.HasRapidFire);
        Assert.True(player.HasHighJumpBoots);
        Assert.True(player.HasSpeedDash);
        Assert.Equal(8, player.Health);

        // Once shield expires, damage strips all powerups
        player.InvulnerabilityPowerupTimer = 0.0f;
        player.TakeDamage(1);
        Assert.False(player.HasRapidFire);
        Assert.False(player.HasHighJumpBoots);
        Assert.False(player.HasSpeedDash);
        Assert.Equal(7, player.Health);
    }
}
