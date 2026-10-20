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
        Assert.Equal(1.2f, player.InvulnerabilityTimer, tolerance: 0.01f);
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
}
