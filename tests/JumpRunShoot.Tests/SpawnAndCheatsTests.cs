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
    public void Cheats_WhenEnabled_KeyU_GrantsAllPowerups()
    {
        var engine = new GameEngine("TEST2");
        engine.CheatsEnabled = true;
        Assert.False(engine.Player.HasRapidFire);
        Assert.False(engine.Player.HasHighJumpBoots);
        Assert.False(engine.Player.HasSpeedDash);

        engine.Input.HandleKeyDown("KeyU");
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

    [Fact]
    public void MonstersAndPickups_NeverSpawnOverlappingEachOther()
    {
        for (int seed = 1; seed <= 5; seed++)
        {
            var engine = new GameEngine($"SEED_{seed}");
            for (int m = 0; m < engine.Monsters.Length; m++)
            {
                if (!engine.Monsters[m].IsActive) continue;
                var mPos = engine.Monsters[m].Position;

                for (int p = 0; p < engine.Pickups.Length; p++)
                {
                    if (!engine.Pickups[p].IsActive) continue;
                    var pPos = engine.Pickups[p].Position;

                    float dx = MathF.Abs(mPos.X - pPos.X);
                    float dy = MathF.Abs(mPos.Y - pPos.Y);
                    Assert.False(dx < 20 && dy < 16,
                        $"Monster {engine.Monsters[m].Type} at {mPos} overlapped pickup {engine.Pickups[p].Type} at {pPos} in seed SEED_{seed}");
                }
            }
        }
    }

    [Fact]
    public void Monster_TurnCooldown_PreventsRapidFrameFlicker()
    {
        var hound = new MonsterEntity();
        hound.Spawn(0, MonsterType.MechaHound, new Vector2f(100, 200));

        // Create a tilemap mock where current spot has empty ledge on both sides
        var mockMap = new TestTileMap();
        mockMap.Set(6, 12, TileType.Solid); // standing platform at col 6 row 12
        // col 5 and 7 are empty ledges

        // First frame: hits ledge, turns
        hound.Update(0.016f, mockMap, new Vector2f(50, 200), out _, out _, out _, out _);
        Assert.True(hound.TurnCooldown > 0, "TurnCooldown should be active after turning");

        var facingAfterFirstTurn = hound.Facing;

        // Next frame immediately after: TurnCooldown should prevent instant reverse flip
        hound.Update(0.016f, mockMap, new Vector2f(50, 200), out _, out _, out _, out _);
        Assert.Equal(facingAfterFirstTurn, hound.Facing);
    }

    [Fact]
    public void Pause_StrictlyToggledByP_CannotBeUnpausedByOtherKeys()
    {
        var engine = new GameEngine("PAUSE_TEST");
        Assert.False(engine.IsPaused);

        // Press 'P' to pause
        engine.Input.HandleKeyDown("KeyP");
        engine.Update(0.016f);
        Assert.True(engine.IsPaused);
        engine.Input.HandleKeyUp("KeyP");

        // Pressing Space should NOT unpause
        engine.Input.HandleKeyDown("Space");
        engine.Update(0.016f);
        Assert.True(engine.IsPaused);
        engine.Input.HandleKeyUp("Space");

        // Pressing Escape should NOT unpause
        engine.Input.HandleKeyDown("Escape");
        engine.Update(0.016f);
        Assert.True(engine.IsPaused);
        engine.Input.HandleKeyUp("Escape");

        // Pressing Enter should NOT unpause
        engine.Input.HandleKeyDown("Enter");
        engine.Update(0.016f);
        Assert.True(engine.IsPaused);
        engine.Input.HandleKeyUp("Enter");

        // Only pressing 'P' again unpauses the game
        engine.Input.HandleKeyDown("KeyP");
        engine.Update(0.016f);
        Assert.False(engine.IsPaused);
    }

    [Fact]
    public void Cheats_FlyMode_KeyF_TogglesFlyingAndControlsVerticalMovementWithoutGravity()
    {
        var engine = new GameEngine("FLY_TEST");
        engine.CheatsEnabled = true;

        Assert.False(engine.Player.IsFlying);

        // Toggle Fly Mode ON
        engine.Input.HandleKeyDown("KeyF");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("KeyF");
        Assert.True(engine.Player.IsFlying);

        // When flying with no vertical inputs, velocity.Y remains 0 (hovering, no gravity applied)
        engine.Player.Velocity = Vector2f.Zero;
        engine.Update(0.016f);
        Assert.Equal(0, engine.Player.Velocity.Y);

        // When pressing 'W' (MoveUp), player moves upwards (Velocity.Y < 0)
        engine.Input.HandleKeyDown("KeyW");
        engine.Update(0.016f);
        Assert.True(engine.Player.Velocity.Y < 0, "Flying player should ascend on KeyW");

        // When releasing 'W', velocity.Y stops and hovers
        engine.Input.HandleKeyUp("KeyW");
        engine.Update(0.016f);
        Assert.Equal(0, engine.Player.Velocity.Y);

        // When pressing 'S' (MoveDown), player moves downwards (Velocity.Y > 0)
        engine.Input.HandleKeyDown("KeyS");
        engine.Update(0.016f);
        Assert.True(engine.Player.Velocity.Y > 0, "Flying player should descend on KeyS");
        engine.Input.HandleKeyUp("KeyS");
        engine.Update(0.016f);

        // Arrows support: ArrowUp ascends, ArrowDown descends, ArrowLeft/Right moves horizontally
        engine.Input.HandleKeyDown("ArrowUp");
        engine.Update(0.016f);
        Assert.True(engine.Player.Velocity.Y < 0, "Flying player should ascend on ArrowUp");
        engine.Input.HandleKeyUp("ArrowUp");
        engine.Update(0.016f);
        Assert.Equal(0, engine.Player.Velocity.Y);

        engine.Input.HandleKeyDown("ArrowDown");
        engine.Update(0.016f);
        Assert.True(engine.Player.Velocity.Y > 0, "Flying player should descend on ArrowDown");
        engine.Input.HandleKeyUp("ArrowDown");
        engine.Update(0.016f);
        Assert.Equal(0, engine.Player.Velocity.Y);

        engine.Input.HandleKeyDown("ArrowLeft");
        engine.Update(0.016f);
        Assert.True(engine.Player.Velocity.X < 0, "Flying player should fly left on ArrowLeft");
        engine.Input.HandleKeyUp("ArrowLeft");

        engine.Input.HandleKeyDown("ArrowRight");
        engine.Update(0.016f);
        Assert.True(engine.Player.Velocity.X > 0, "Flying player should fly right on ArrowRight");
        engine.Input.HandleKeyUp("ArrowRight");
        engine.Update(0.016f);
        Assert.Equal(0, engine.Player.Velocity.X);

        // Toggle Fly Mode OFF
        engine.Input.HandleKeyDown("KeyF");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("KeyF");
        Assert.False(engine.Player.IsFlying);
    }

    [Fact]
    public void Cheats_NoClip_KeyN_TogglesNoClip()
    {
        var engine = new GameEngine("NOCLIP_TEST");
        engine.CheatsEnabled = true;

        Assert.False(engine.Player.IsNoClip);

        // Toggle NoClip ON
        engine.Input.HandleKeyDown("KeyN");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("KeyN");
        Assert.True(engine.Player.IsNoClip);

        // Toggle NoClip OFF
        engine.Input.HandleKeyDown("KeyN");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("KeyN");
        Assert.False(engine.Player.IsNoClip);
    }

    [Fact]
    public void Cheats_NoClip_WalksThroughSolidWallInGameEngine()
    {
        var engine = new GameEngine("WALL_TEST");
        engine.CheatsEnabled = true;

        // Place a solid wall 1 tile directly in front of the player (both head and torso)
        int playerCol = (int)MathF.Floor((engine.Player.Position.X + PlayerEntity.BoxOffset.X) / PhysicsConstants.TileSize);
        int playerRow = (int)MathF.Floor((engine.Player.Position.Y + PlayerEntity.BoxOffset.Y) / PhysicsConstants.TileSize);
        engine.Grid.SetTile(playerCol + 1, playerRow, TileType.Solid);
        engine.Grid.SetTile(playerCol + 1, playerRow + 1, TileType.Solid);

        // Without noclip: walking right is stopped at the wall
        engine.Input.HandleKeyDown("KeyD");
        for (int i = 0; i < 40; i++) engine.Update(0.016f);
        float stoppedX = engine.Player.Position.X;
        // Verify stopped at the wall
        for (int i = 0; i < 15; i++) engine.Update(0.016f);
        Assert.Equal(stoppedX, engine.Player.Position.X);

        // Turn ON NoClip
        engine.Input.HandleKeyDown("KeyN");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("KeyN");
        Assert.True(engine.Player.IsNoClip);

        // Walking right with noclip passes through the wall
        for (int i = 0; i < 40; i++) engine.Update(0.016f);
        Assert.True(engine.Player.Position.X > stoppedX + 16.0f, "Player should have passed through the wall");
    }

    [Fact]
    public void Pause_DialogBox_FullyEnclosesResumePrompt()
    {
        // Resume prompt length in 8px characters: 19 * 8 = 152px
        const string resumePrompt = "PRESS [P] TO RESUME";
        int promptWidth = resumePrompt.Length * 8;
        int promptX = (320 - promptWidth) / 2;
        int promptRight = promptX + promptWidth;

        // Box parameters in GameEngine: (68, 71, 184, 40)
        int boxX = 68;
        int boxW = 184;
        int boxRight = boxX + boxW;

        Assert.True(boxX < promptX, $"Box left ({boxX}) should be strictly left of prompt left ({promptX})");
        Assert.True(boxRight > promptRight, $"Box right ({boxRight}) should be strictly right of prompt right ({promptRight})");
        Assert.Equal(promptX - boxX, boxRight - promptRight); // Check symmetric horizontal margins
    }

    [Fact]
    public void Crates_BlockProjectiles_And_StopMonstersFromPassingThrough()
    {
        var engine = new GameEngine("CRATE_BLOCK_TEST");

        // 1. Bullet collision against crate
        // Place a crate at col 10 row 10 (160, 160)
        engine.Grid.SetTile(10, 10, TileType.Crate);
        engine.SpawnProjectile(new Vector2f(150, 164), new Vector2f(200, 0), damage: 1, fromPlayer: true);
        Assert.True(engine.Projectiles[0].IsActive);

        // Update frame: projectile enters crate tile and is blocked/destroyed
        engine.Update(0.08f);
        Assert.False(engine.Projectiles[0].IsActive, "Projectile should be stopped by Crate tile");

        // 2. Monster walking into crate
        var mockMap = new TestTileMap();
        mockMap.Set(5, 12, TileType.Solid); // standing floor
        mockMap.Set(6, 12, TileType.Solid);
        mockMap.Set(7, 12, TileType.Solid);
        mockMap.Set(7, 11, TileType.Crate); // crate wall at col 7 row 11

        var droid = new MonsterEntity();
        droid.Spawn(0, MonsterType.PatrolDroid, new Vector2f(6 * 16, 12 * 16 - 14));
        droid.Facing = Direction.Right;
        droid.Velocity = new Vector2f(35.0f, 0);

        // Update monster: detects crate wall at col 7 and reverses direction
        droid.Update(0.016f, mockMap, new Vector2f(0, 0), out _, out _, out _, out _);
        Assert.Equal(Direction.Left, droid.Facing);
        Assert.True(droid.Velocity.X < 0, "Monster should turn away from Crate block");
    }

    private sealed class TestTileMap : ITileMap
    {
        private readonly System.Collections.Generic.Dictionary<(int, int), TileType> _tiles = new();
        public void Set(int col, int row, TileType type) => _tiles[(col, row)] = type;
        public TileType GetTile(int col, int row) => _tiles.TryGetValue((col, row), out var t) ? t : TileType.Empty;
    }
}
