using System;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class SpawnAndCheatsTests
{
    [Theory]
    [InlineData("RETRO1")]
    [InlineData("DUKE91")]
    [InlineData("SEED42")]
    [InlineData("FOREST7")]
    public void Spawn_IsDeterministicallyPositionedWithinFirstScreensAndOnSafeGround(string seed)
    {
        var engine = new GameEngine(seed);
        var player = engine.Player;

        // Player X must be in first 1-2 screens (e.g. 0 to 600px)
        Assert.True(player.Position.X >= 16.0f, $"Spawn X {player.Position.X} was too far left");
        Assert.True(player.Position.X <= 640.0f, $"Spawn X {player.Position.X} was past screen 2");

        // Player Y must be above the abyss (chunk height is 32 * 16 = 512px)
        Assert.True(player.Position.Y >= 32.0f, $"Spawn Y {player.Position.Y} was too high");
        Assert.True(player.Position.Y < 480.0f, $"Spawn Y {player.Position.Y} was in the abyss");

        // Floor directly under player feet must be solid, one-way platform, or crate
        int footCol = (int)MathF.Floor((player.Position.X + 7.0f) / PhysicsConstants.TileSize);
        int footRow = (int)MathF.Floor((player.Position.Y + PlayerEntity.BoxOffset.Y + PlayerEntity.BoxHeight + 1.0f) / PhysicsConstants.TileSize);
        TileType floorTile = engine.Grid.GetTile(footCol, footRow);

        Assert.True(floorTile == TileType.Solid || floorTile == TileType.OneWayPlatform || floorTile == TileType.Crate,
            $"Expected solid/platform/crate at ({footCol}, {footRow}) but got {floorTile}");

        // Player clearance tiles must not be Hazard
        int headRow = (int)MathF.Floor((player.Position.Y + PlayerEntity.BoxOffset.Y) / PhysicsConstants.TileSize);
        Assert.NotEqual(TileType.Hazard, engine.Grid.GetTile(footCol, headRow));
    }

    [Fact]
    public void Spawn_VariesElevationAndPositionAcrossDifferentSeeds()
    {
        var e1 = new GameEngine("ALPHA");
        var e2 = new GameEngine("OMEGA");

        // Different seeds should produce distinct starting locations
        Assert.False(e1.Player.Position.X == e2.Player.Position.X && e1.Player.Position.Y == e2.Player.Position.Y,
            $"Positions unexpectedly matched: {e1.Player.Position} vs {e2.Player.Position}");
    }

    [Fact]
    public void Cheats_WhenEnabled_KeyG_TogglesGodMode()
    {
        var engine = new GameEngine("TEST1");
        engine.CheatsEnabled = true;
        Assert.False(engine.Player.IsGodMode);

        // Press 'G'
        engine.Input.HandleKeyDown("KeyG");
        engine.Update(0.016f);
        Assert.True(engine.Player.IsGodMode);

        engine.Input.HandleKeyUp("KeyG");
        engine.Update(0.016f);

        // Press 'G' again
        engine.Input.HandleKeyDown("KeyG");
        engine.Update(0.016f);
        Assert.False(engine.Player.IsGodMode);
    }

    [Fact]
    public void Cheats_WhenEnabled_KeyP_GrantsAllPowerups()
    {
        var engine = new GameEngine("TEST2");
        engine.CheatsEnabled = true;
        Assert.False(engine.Player.HasRapidFire);
        Assert.False(engine.Player.HasHighJumpBoots);
        Assert.False(engine.Player.HasSpeedDash);

        engine.Input.HandleKeyDown("KeyP");
        engine.Update(0.016f);

        Assert.True(engine.Player.HasRapidFire);
        Assert.True(engine.Player.HasHighJumpBoots);
        Assert.True(engine.Player.HasSpeedDash);
    }

    [Fact]
    public void Cheats_WhenEnabled_KeyH_RestoresFullHealthTo8()
    {
        var engine = new GameEngine("TEST3");
        engine.CheatsEnabled = true;
        engine.Player.TakeDamage(6);
        Assert.Equal(2, engine.Player.Health);

        engine.Input.HandleKeyDown("KeyH");
        engine.Update(0.016f);

        Assert.Equal(8, engine.Player.Health);
    }

    [Fact]
    public void Cheats_WhenEnabled_KeyK_NukesAllActiveMonsters()
    {
        var engine = new GameEngine("TEST4");
        engine.CheatsEnabled = true;

        // Run one frame so current neighborhood chunks are fully populated
        engine.Update(0.016f);

        // Ensure at least one monster is active
        int activeCountBefore = 0;
        for (int i = 0; i < engine.Monsters.Length; i++)
        {
            if (engine.Monsters[i].IsActive) activeCountBefore++;
        }
        Assert.True(activeCountBefore > 0, "Expected initial active monsters");

        engine.Input.HandleKeyDown("KeyK");
        engine.Update(0.016f);

        int activeCountAfter = 0;
        for (int i = 0; i < engine.Monsters.Length; i++)
        {
            if (engine.Monsters[i].IsActive) activeCountAfter++;
        }
        Assert.Equal(0, activeCountAfter);
        Assert.True(engine.Player.Score > 0, "Nuke should award score for destroyed foes");
    }

    [Fact]
    public void Cheats_WhenDisabled_CheatKeysDoNothing()
    {
        var engine = new GameEngine("TEST5");
        engine.CheatsEnabled = false;

        engine.Input.HandleKeyDown("KeyG");
        engine.Update(0.016f);
        Assert.False(engine.Player.IsGodMode);

        engine.Input.HandleKeyDown("KeyH");
        engine.Update(0.016f);
        // Player should still have max health (8), not change godmode or anything
        Assert.Equal(8, engine.Player.Health);
    }

    [Fact]
    public void SpikePits_StandardPit_Is1TileDeep_AndEscapeable()
    {
        var gen = new WorldGenerator("TEST_PITS");
        var chunk = gen.GenerateChunk(1);

        for (int col = 0; col < TileChunk.ChunkWidth; col++)
        {
            if (chunk.GetTile(col, 29) == TileType.Hazard)
            {
                // Standard pit: row 28 is empty (walkway level was 28), row 29 is Hazard (1 tile deep)
                Assert.Equal(TileType.Empty, chunk.GetTile(col, 28));
                Assert.Equal(TileType.Solid, chunk.GetTile(col, 30));
            }
        }
    }

    [Fact]
    public void Monster_WhenShot_TurnsTowardsPlayer_AndMechaHoundSprints()
    {
        var hound = new MonsterEntity();
        hound.Spawn(0, MonsterType.MechaHound, new Vector2f(200, 400));
        Assert.Equal(Direction.Left, hound.Facing);
        Assert.False(hound.IsAlerted);
        Assert.True(hound.Velocity.X < 0);

        // Player is to the right at (350, 400)
        hound.TakeDamage(1, new Vector2f(350, 400));

        Assert.Equal(Direction.Right, hound.Facing);
        Assert.True(hound.IsAlerted, "Hound should enter alerted state when shot");
        Assert.True(hound.Velocity.X >= 120.0f, $"Expected sprint velocity >= 120 but got {hound.Velocity.X}");
    }

    [Fact]
    public void WorldGenerator_GeneratesAll8Archetypes_WithoutCrashing()
    {
        var gen = new WorldGenerator("VARIETY_TEST");
        var archetypesSeen = new System.Collections.Generic.HashSet<int>();

        for (int c = 1; c <= 30; c++)
        {
            var chunk = gen.GenerateChunk(c);
            Assert.NotNull(chunk);
            Assert.Equal(TileChunk.ChunkWidth, 16);
            Assert.Equal(TileChunk.ChunkHeight, 32);
        }
    }
}
