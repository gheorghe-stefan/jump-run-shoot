using System;
using JumpRunShoot.Engine.Audio;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class AudioTests
{
    private class TestInput : IInputSource
    {
        public bool Right, Jump, Run;

        public bool IsDown(GameAction action) => action switch
        {
            GameAction.MoveRight => Right,
            GameAction.Jump => Jump,
            GameAction.Run => Run,
            _ => false
        };

        public bool JustPressed(GameAction action) => IsDown(action);
        public bool JustReleased(GameAction action) => false;
    }

    private class FlatFloorGrid : ITileMap
    {
        public TileType GetTile(int col, int row)
        {
            // Floor at row 20
            if (row >= 20) return TileType.Solid;
            return TileType.Empty;
        }
    }

    [Fact]
    public void GameEngine_TriggerAudio_AccumulatesAndConsumesFlags()
    {
        var engine = new GameEngine();
        Assert.Equal(0, engine.ConsumeAudioFlags());

        engine.TriggerAudio(AudioEvent.PlayerShoot);
        engine.TriggerAudio(AudioEvent.PlayerJump);

        int consumed = engine.ConsumeAudioFlags();
        Assert.Equal((int)(AudioEvent.PlayerShoot | AudioEvent.PlayerJump), consumed);

        // Subsequent consume should be 0 (cleared)
        Assert.Equal(0, engine.ConsumeAudioFlags());
    }

    [Fact]
    public void PlayerEntity_Jump_TriggersPlayerJumpAudio()
    {
        var player = new PlayerEntity(new Vector2f(32, 19 * 16));
        player.IsGrounded = true;
        var grid = new FlatFloorGrid();
        var input = new TestInput { Jump = true };

        player.Update(0.016f, input, grid);

        Assert.True((player.PendingAudioEvents & AudioEvent.PlayerJump) != 0, "Jump should trigger PlayerJump audio flag");
    }

    [Fact]
    public void PlayerEntity_WalkAndRun_TriggersFootstepAudioWithAcceleratedCadence()
    {
        var grid = new FlatFloorGrid();
        float floorY = 20 * PhysicsConstants.TileSize;
        float playerY = floorY - PlayerEntity.BoxHeight - PlayerEntity.BoxOffset.Y;

        // 1. Walking: should trigger PlayerStep after 0.28s
        var walkPlayer = new PlayerEntity(new Vector2f(32, playerY));
        walkPlayer.IsGrounded = true;
        walkPlayer.Velocity.X = 80.0f;
        var walkInput = new TestInput { Right = true, Run = false };

        // Step not yet reached at 0.15s
        walkPlayer.Update(0.15f, walkInput, grid);
        Assert.False((walkPlayer.PendingAudioEvents & AudioEvent.PlayerStep) != 0);

        // Step reached after accumulating >= 0.28s (total 0.30s)
        walkPlayer.Update(0.15f, walkInput, grid);
        Assert.True((walkPlayer.PendingAudioEvents & AudioEvent.PlayerStep) != 0, "Walking should trigger PlayerStep");
        walkPlayer.PendingAudioEvents = AudioEvent.None;

        // 2. Running: should trigger PlayerRunStep at ~0.18s (accelerated)
        var runPlayer = new PlayerEntity(new Vector2f(32, playerY));
        runPlayer.IsGrounded = true;
        runPlayer.Velocity.X = 140.0f;
        var runInput = new TestInput { Right = true, Run = true };

        runPlayer.Update(0.19f, runInput, grid);
        Assert.True((runPlayer.PendingAudioEvents & AudioEvent.PlayerRunStep) != 0, "Running should trigger PlayerRunStep at faster cadence");
    }

    [Fact]
    public void GameEngine_ShootAndMonsterEvents_TriggerAppropriateAudio()
    {
        var engine = new GameEngine();
        engine.ConsumeAudioFlags(); // clear startup flags

        // 1. Trigger player shooting
        engine.Player.PendingShoot = true;
        engine.Update(0.016f);
        int flags = engine.ConsumeAudioFlags();
        Assert.True((flags & (int)AudioEvent.PlayerShoot) != 0, "Shooting should trigger PlayerShoot audio flag");

        // 2. Trigger monster shoot via engine monster pool
        engine.Monsters[0].Spawn(0, MonsterType.HoverDrone, new Vector2f(50, 50));
        engine.Monsters[0].ActionTimer = 0.001f; // force shoot on next update
        engine.Update(0.016f);
        flags = engine.ConsumeAudioFlags();
        Assert.True((flags & (int)AudioEvent.MonsterShoot) != 0, "Monster shoot should trigger MonsterShoot audio flag");

        // 3. Trigger monster death via player projectile collision
        engine.Monsters[0].Spawn(0, MonsterType.HoverDrone, new Vector2f(100, 100));
        engine.Monsters[0].Health = 1; // 1 HP so single hit kills it
        engine.SpawnProjectile(new Vector2f(100, 100), Vector2f.Zero, damage: 1, fromPlayer: true);
        engine.Update(0.016f);
        flags = engine.ConsumeAudioFlags();
        Assert.True((flags & (int)AudioEvent.MonsterDie) != 0, "Monster destruction should trigger MonsterDie audio flag");

        // 4. Trigger pickup collection
        engine.Pickups[0].Spawn(PickupType.BlueGem, new Vector2f(engine.Player.Position.X, engine.Player.Position.Y));
        engine.Update(0.016f);
        flags = engine.ConsumeAudioFlags();
        Assert.True((flags & (int)AudioEvent.Pickup) != 0, "Collecting item should trigger Pickup audio flag");
    }

    [Fact]
    public void PlayerEntity_HazardCollision_TriggersPlayerDamageAudio()
    {
        var player = new PlayerEntity(new Vector2f(32, 100));
        player.TakeDamage(1);
        Assert.True((player.PendingAudioEvents & AudioEvent.PlayerDamage) != 0, "Taking damage should trigger PlayerDamage audio flag");
    }
}
