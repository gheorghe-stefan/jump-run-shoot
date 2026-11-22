using System;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class SpikeHoleAndSolidCollisionTests
{
    private sealed class MockTileGrid : ITileMap
    {
        private readonly TileType[,] _tiles = new TileType[64, TileChunk.ChunkHeight];

        public TileType GetTile(int col, int row)
        {
            if (col < 0 || col >= 64 || row < 0 || row >= TileChunk.ChunkHeight)
                return TileType.Empty;
            return _tiles[col, row];
        }

        public void SetTile(int col, int row, TileType type)
        {
            if (col >= 0 && col < 64 && row >= 0 && row < TileChunk.ChunkHeight)
                _tiles[col, row] = type;
        }
    }

    private sealed class ScriptedInput : IInputSource
    {
        public bool Left { get; set; }
        public bool Right { get; set; }
        public bool Down { get; set; }
        public bool Run { get; set; }
        public bool IsDown(GameAction action) => action switch
        {
            GameAction.MoveLeft => Left,
            GameAction.MoveRight => Right,
            GameAction.MoveDown => Down,
            GameAction.Run => Run,
            _ => false
        };
        public bool JustPressed(GameAction action) => false;
        public bool JustReleased(GameAction action) => false;
    }

    [Fact]
    public void MoveAndCollide_SolidFloor_NeverAllowsFallThroughEvenWhenFeetAreOver4PxInside()
    {
        var grid = new MockTileGrid();
        // Solid floor at row 30 and 31 (as beneath a spike hole)
        for (int c = 2; c <= 10; c++)
        {
            grid.SetTile(c, 30, TileType.Solid);
            grid.SetTile(c, 31, TileType.Solid);
        }

        // Simulate feet already 6px below platformTop of row 30 (e.g. from knockback/ladder nudge)
        float boxHeight = PlayerEntity.BoxHeight; // 22
        var boxOffset = PlayerEntity.BoxOffset;   // (2, 2)
        float platformTop = 30 * PhysicsConstants.TileSize; // 480
        var pos = new Vector2f(5 * 16, platformTop + 6.0f - boxHeight - boxOffset.Y);
        var vel = new Vector2f(0, 120.0f);

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, PlayerEntity.BoxWidth, boxHeight, boxOffset, grid, 0.016f, noClip: false);

        // Must immediately clamp back onto row 30 instead of falling through bedrock into row 31 / abyss
        Assert.True(result.IsGrounded, "Solid floor must ground entity instead of bypassing via 4px one-way tolerance");
        Assert.Equal(platformTop - boxHeight - boxOffset.Y, pos.Y);
    }

    [Fact]
    public void MoveAndCollide_WithNoClip_InSpikeHole_CannotWalkSidewaysIntoSolidGroundOrFallThroughBedrock()
    {
        var grid = new MockTileGrid();
        // Build realistic terrain at row 28 with a recessed spike hole at cols 5..6:
        // row 28 = Empty in pit, Solid on sides; row 29 = Hazard in pit, Solid on sides; row 30..31 = Solid bedrock
        for (int c = 1; c <= 12; c++)
        {
            for (int r = 28; r < TileChunk.ChunkHeight; r++)
            {
                grid.SetTile(c, r, TileType.Solid);
            }
        }
        for (int c = 5; c <= 6; c++)
        {
            grid.SetTile(c, 28, TileType.Empty);
            grid.SetTile(c, 29, TileType.Hazard);
            grid.SetTile(c, 30, TileType.Solid);
            grid.SetTile(c, 31, TileType.Solid);
        }

        // Player inside the spike hole resting on row 30 (feetY = 480) with NoClip active
        var pos = new Vector2f(5 * 16 + 2, 30 * 16 - PlayerEntity.BoxHeight - PlayerEntity.BoxOffset.Y);
        var vel = new Vector2f(155.0f, 10.0f); // trying to run right into solid ground wall at col 7

        for (int f = 0; f < 40; f++)
        {
            vel.X = 155.0f;
            vel.Y = 60.0f;
            AabbCollisionResolver.MoveAndCollide(
                ref pos, ref vel, PlayerEntity.BoxWidth, PlayerEntity.BoxHeight, PlayerEntity.BoxOffset,
                grid, 0.016f, noClip: true);
        }

        float boxRight = pos.X + PlayerEntity.BoxOffset.X + PlayerEntity.BoxWidth;
        float feetY = pos.Y + PlayerEntity.BoxOffset.Y + PlayerEntity.BoxHeight;

        Assert.True(boxRight <= 7 * 16 + 0.01f,
            $"NoClip player walked out of spike hole into solid ground wall! boxRight={boxRight:F2} (wall at 112)");
        Assert.True(feetY <= 30 * 16 + 0.01f,
            $"NoClip player fell through solid bedrock under spike hole! feetY={feetY:F2}");
    }

    [Fact]
    public void MoveAndCollide_WithNoClip_InElevatedHillSpikeHole_CannotSinkOrEnterSolidPitWalls()
    {
        var grid = new MockTileGrid();
        // Elevated hill at row 25 (< FloorRow 28!) with a spike hole at cols 5..6:
        // row 25 = Empty, row 26 = Hazard, row 27..31 = Solid
        for (int c = 1; c <= 12; c++)
        {
            for (int r = 25; r < TileChunk.ChunkHeight; r++)
            {
                grid.SetTile(c, r, TileType.Solid);
            }
        }
        for (int c = 5; c <= 6; c++)
        {
            grid.SetTile(c, 25, TileType.Empty);
            grid.SetTile(c, 26, TileType.Hazard);
            grid.SetTile(c, 27, TileType.Solid);
        }

        // Drop player into the hill spike hole with NoClip active and press right
        var pos = new Vector2f(5 * 16 + 2, 25 * 16 - PlayerEntity.BoxHeight - PlayerEntity.BoxOffset.Y);
        var vel = new Vector2f(0f, 0f);

        for (int f = 0; f < 50; f++)
        {
            vel.X = 120.0f;
            vel.Y = MathF.Min(vel.Y + PhysicsConstants.Gravity * 0.016f, PhysicsConstants.MaxFallSpeed);
            AabbCollisionResolver.MoveAndCollide(
                ref pos, ref vel, PlayerEntity.BoxWidth, PlayerEntity.BoxHeight, PlayerEntity.BoxOffset,
                grid, 0.016f, noClip: true);
        }

        float boxRight = pos.X + PlayerEntity.BoxOffset.X + PlayerEntity.BoxWidth;
        float feetY = pos.Y + PlayerEntity.BoxOffset.Y + PlayerEntity.BoxHeight;

        Assert.True(feetY <= 27 * 16 + 0.01f,
            $"NoClip player sank through solid floor beneath elevated hill spike hole! feetY={feetY:F2} (floor at {27 * 16})");
        Assert.True(boxRight <= 7 * 16 + 0.01f,
            $"NoClip player clipped sideways from hill spike hole into solid ground! boxRight={boxRight:F2} (wall at {7 * 16})");
    }

    [Fact]
    public void NormalPlay_SpikeHole_AllActionsNeverClipIntoSolidGroundOrAbyss()
    {
        var grid = new MockTileGrid();
        for (int c = 1; c <= 12; c++)
        {
            for (int r = 28; r < TileChunk.ChunkHeight; r++)
            {
                grid.SetTile(c, r, TileType.Solid);
            }
        }
        for (int c = 5; c <= 6; c++)
        {
            grid.SetTile(c, 28, TileType.Empty);
            grid.SetTile(c, 29, TileType.Hazard);
            grid.SetTile(c, 30, TileType.Solid);
            grid.SetTile(c, 31, TileType.Solid);
        }

        var player = new PlayerEntity(new Vector2f(5 * 16 + 2, 27 * 16));
        player.InvulnerabilityPowerupTimer = 30.0f;

        var input = new ScriptedInput();
        // Phase 1: Fall into pit while pushing right & crouching
        input.Right = true;
        input.Down = true;
        for (int f = 0; f < 60; f++)
        {
            player.Update(0.016f, input, grid, allowCrouch: true);
            AssertPlayerNotInSolid(player, grid);
        }

        // Phase 2: Push left while crouched
        input.Right = false;
        input.Left = true;
        for (int f = 0; f < 60; f++)
        {
            player.Update(0.016f, input, grid, allowCrouch: true);
            AssertPlayerNotInSolid(player, grid);
        }

        // Phase 3: Stand up and run right
        input.Down = false;
        input.Left = false;
        input.Right = true;
        input.Run = true;
        for (int f = 0; f < 60; f++)
        {
            player.Update(0.016f, input, grid, allowCrouch: true);
            AssertPlayerNotInSolid(player, grid);
        }
    }

    [Fact]
    public void WorldGenerator_AllHazardPitsAcrossManySeeds_HaveSolidBedrockAllTheWayDown()
    {
        var seeds = new[] { "FOO", "DEMO", "DUKE", "POLAR_SPIKES", "RAVINE_2026", "SPIKE_HOLE_BUG", "ICE_CAVE", "TEST_99" };
        int hazardTilesChecked = 0;

        foreach (string seed in seeds)
        {
            var gen = new WorldGenerator(seed);
            for (int chunkIdx = 1; chunkIdx <= 25; chunkIdx++)
            {
                var chunk = gen.GenerateChunk(chunkIdx);
                for (int c = 0; c < TileChunk.ChunkWidth; c++)
                {
                    for (int r = 0; r < TileChunk.ChunkHeight; r++)
                    {
                        if (chunk.GetTile(c, r) == TileType.Hazard)
                        {
                            hazardTilesChecked++;
                            Assert.True(r < TileChunk.ChunkHeight - 1,
                                $"Seed '{seed}' chunk {chunkIdx} ({c},{r}): Hazard tile sits on bottom row with no solid floor beneath!");
                            for (int rr = r + 1; rr < TileChunk.ChunkHeight; rr++)
                            {
                                Assert.Equal(TileType.Solid, chunk.GetTile(c, rr));
                            }
                        }
                    }
                }
            }
        }

        Assert.True(hazardTilesChecked > 20, $"Expected hazard tiles to be tested, only found {hazardTilesChecked}");
    }

    [Fact]
    public void AbyssBounce_WhenRow18HasSolidBlock_PlacesPlayerInClearOpenAirInsteadOfInsideSolid()
    {
        var engine = new GameEngine("ABYSS_SAFE_RESPAWN");
        engine.Player.IsGodMode = true;
        int col = 5;
        // Place solid ceiling/platform covering rows 17..19 at col 5
        for (int c = col - 1; c <= col + 2; c++)
        {
            engine.Grid.SetTile(c, 17, TileType.Solid);
            engine.Grid.SetTile(c, 18, TileType.Solid);
            engine.Grid.SetTile(c, 19, TileType.Solid);
            for (int r = 10; r <= 16; r++)
            {
                engine.Grid.SetTile(c, r, TileType.Empty);
            }
        }

        engine.Player.Position = new Vector2f(col * 16, TileChunk.ChunkHeight * PhysicsConstants.TileSize + 10.0f);
        engine.Update(0.016f);

        AssertPlayerNotInSolid(engine.Player, engine.Grid);
    }

    private static void AssertPlayerNotInSolid(PlayerEntity player, ITileMap grid)
    {
        var box = player.BoundingBox;
        int ts = PhysicsConstants.TileSize;
        int minCol = (int)MathF.Floor((box.X + 0.1f) / ts);
        int maxCol = (int)MathF.Floor((box.X + box.Width - 0.1f) / ts);
        int minRow = (int)MathF.Floor((box.Y + 0.1f) / ts);
        int maxRow = (int)MathF.Floor((box.Y + box.Height - 0.1f) / ts);

        for (int r = minRow; r <= maxRow; r++)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                TileType t = grid.GetTile(c, r);
                Assert.False(t is TileType.Solid or TileType.Border,
                    $"Player box ({box.X:F1},{box.Y:F1} {box.Width}x{box.Height}) overlapped {t} at ({c},{r})!");
            }
        }
    }
}
