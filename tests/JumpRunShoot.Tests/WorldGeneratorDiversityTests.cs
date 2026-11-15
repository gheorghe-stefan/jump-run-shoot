using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class WorldGeneratorDiversityTests
{
    private static string SerializeChunk(TileChunk chunk)
    {
        var chars = new char[TileChunk.TotalTiles];
        for (int i = 0; i < TileChunk.TotalTiles; i++)
        {
            chars[i] = (char)('0' + (int)chunk.Tiles[i]);
        }
        return new string(chars);
    }

    [Fact]
    public void DeterministicRandom_64BitSeeds_DivergeBeyond32BitsAndAcrossLongStrings()
    {
        // Two 64-bit numeric seeds sharing lower 32 bits must diverge
        var a = new DeterministicRandom(0x00000001_ABCDEF12UL);
        var b = new DeterministicRandom(0x00000002_ABCDEF12UL);
        bool numericDiverged = false;
        for (int i = 0; i < 4; i++)
        {
            if (a.NextULong() != b.NextULong()) numericDiverged = true;
        }
        Assert.True(numericDiverged);

        // Two long 14-character seeds differing only near the end must diverge immediately
        var sa = new DeterministicRandom("POLARADV991234A");
        var sb = new DeterministicRandom("POLARADV991234B");
        Assert.NotEqual(sa.NextULong(), sb.NextULong());
    }

    [Fact]
    public void GeneratedChunks_AreUniqueAcrossSeedsAndIndices_NotRepeatedStaticStamps()
    {
        var gen = new WorldGenerator("DIVERSE_SEED_10CHAR");
        var fingerprints = new HashSet<string>();

        for (int chunkIdx = 1; chunkIdx <= 30; chunkIdx++)
        {
            string fp = SerializeChunk(gen.GenerateChunk(chunkIdx));
            Assert.True(fingerprints.Add(fp), $"Chunk {chunkIdx} duplicated an earlier chunk layout!");
        }
    }

    [Fact]
    public void TerrainElevation_VariesSmoothlyAcrossChunks_WithSeamlessBoundaries()
    {
        var gen = new WorldGenerator("HILLS_AND_VALLEYS");
        var distinctBoundaryHeights = new HashSet<int>();

        for (int chunkIdx = 1; chunkIdx <= 25; chunkIdx++)
        {
            int leftExpected = gen.GetBoundaryGroundRow(chunkIdx);
            int rightExpected = gen.GetBoundaryGroundRow(chunkIdx + 1);
            distinctBoundaryHeights.Add(leftExpected);

            var chunk = gen.GenerateChunk(chunkIdx);

            // Exact solid bedrock starts cleanly at seam elevation on cols 0 and 15
            Assert.Equal(TileType.Solid, chunk.GetTile(0, leftExpected));
            Assert.Equal(TileType.Empty, chunk.GetTile(0, leftExpected - 1));

            Assert.Equal(TileType.Solid, chunk.GetTile(15, rightExpected));
            Assert.Equal(TileType.Empty, chunk.GetTile(15, rightExpected - 1));

            // Headroom of 3 tiles at seams
            for (int offset = 1; offset <= 3; offset++)
            {
                Assert.NotEqual(TileType.Solid, chunk.GetTile(0, leftExpected - offset));
                Assert.NotEqual(TileType.Solid, chunk.GetTile(15, rightExpected - offset));
            }
        }

        Assert.True(distinctBoundaryHeights.Count >= 3,
            $"Expected varied ground elevations across chunks, got only {distinctBoundaryHeights.Count}.");
    }

    [Fact]
    public void HazardPits_AreCappedAt4Tiles_AndNeverSpanLongContinuousTrenches()
    {
        var seeds = new[] { "HAZARD_CHECK_1", "RAVINE_RUN_2026", "POLAR_ICE_88", "FOO", "DEMO" };

        foreach (var seed in seeds)
        {
            var gen = new WorldGenerator(seed);
            for (int chunkIdx = 0; chunkIdx <= 35; chunkIdx++)
            {
                var chunk = gen.GenerateChunk(chunkIdx);
                int streak = 0;
                for (int c = 0; c < TileChunk.ChunkWidth; c++)
                {
                    bool hasHazard = false;
                    for (int r = 0; r < TileChunk.ChunkHeight; r++)
                    {
                        if (chunk.GetTile(c, r) == TileType.Hazard)
                        {
                            hasHazard = true;
                            // Every hazard tile must have open tile above and solid bedrock below
                            Assert.NotEqual(TileType.Solid, chunk.GetTile(c, r - 1));
                            Assert.Equal(TileType.Solid, chunk.GetTile(c, r + 1));
                        }
                    }

                    streak = hasHazard ? streak + 1 : 0;
                    Assert.True(streak <= WorldGenerator.MaxContiguousHazardWidth,
                        $"Seed '{seed}' chunk {chunkIdx} produced contiguous hazard run of {streak} tiles (> {WorldGenerator.MaxContiguousHazardWidth}).");
                }
            }
        }
    }

    [Fact]
    public void ZoneStyles_ChangeAcrossMultiChunkSegments_AndVaryBetweenSeeds()
    {
        var genA = new WorldGenerator("ZONE_ALPHA_01");
        var genB = new WorldGenerator("ZONE_OMEGA_99");

        var seenA = new HashSet<ZoneStyle>();
        var sequenceDiffers = false;

        for (int c = 1; c <= 24; c++)
        {
            var zA = genA.GetZoneForChunk(c);
            var zB = genB.GetZoneForChunk(c);
            seenA.Add(zA);
            if (zA != zB) sequenceDiffers = true;
        }

        Assert.True(seenA.Count >= 3, $"Expected multiple zone biomes within 24 chunks, saw {seenA.Count}.");
        Assert.True(sequenceDiffers, "Different seeds should generate distinct zone sequences.");
    }

    [Fact]
    public void Platforms_BreakFreeFromFixed4TierGrid()
    {
        var gen = new WorldGenerator("NON_GRID_TIERS");
        var fixedLegacyRows = new HashSet<int> { 4, 10, 16, 22, 28 };
        var customRowsSeen = new HashSet<int>();

        for (int chunkIdx = 1; chunkIdx <= 12; chunkIdx++)
        {
            var chunk = gen.GenerateChunk(chunkIdx);
            for (int r = 4; r < 27; r++)
            {
                for (int c = 1; c < TileChunk.ChunkWidth - 1; c++)
                {
                    if (chunk.GetTile(c, r) == TileType.OneWayPlatform && !fixedLegacyRows.Contains(r))
                    {
                        customRowsSeen.Add(r);
                    }
                }
            }
        }

        Assert.True(customRowsSeen.Count >= 5,
            $"Expected platforms across diverse non-grid rows, found only rows [{string.Join(",", customRowsSeen)}].");
    }

    [Fact]
    public void Ladders_NeverTerminateDeadEndUnderSolidCeiling()
    {
        var seeds = new[] { "DEMO", "FOO", "LADDER_TOP_TEST_1", "CANYON_VAULT_99", "SUPPLY_007" };

        foreach (var seed in seeds)
        {
            var gen = new WorldGenerator(seed);
            for (int chunkIdx = 0; chunkIdx <= 30; chunkIdx++)
            {
                var chunk = gen.GenerateChunk(chunkIdx);
                for (int c = 1; c < TileChunk.ChunkWidth - 1; c++)
                {
                    for (int r = 3; r < TileChunk.ChunkHeight - 4; r++)
                    {
                        if (chunk.GetTile(c, r) == TileType.Ladder &&
                            chunk.GetTile(c, r - 1) != TileType.Ladder)
                        {
                            // Top exit of every ladder must not be sealed directly overhead by a Solid or Crate block
                            Assert.NotEqual(TileType.Solid, chunk.GetTile(c, r - 1));
                            Assert.NotEqual(TileType.Crate, chunk.GetTile(c, r - 1));
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void AllSurfaces_NeverFormCrampedOneTilePinchUnderSolidBlock()
    {
        var seeds = new[] { "FOO", "DEMO", "POLAR_PINCH_1", "RAVINE_RUN_2026", "OUTPOST_ALPHA", "SUPPLY_007" };

        foreach (var seed in seeds)
        {
            var gen = new WorldGenerator(seed);
            for (int chunkIdx = 0; chunkIdx <= 30; chunkIdx++)
            {
                var chunk = gen.GenerateChunk(chunkIdx);
                for (int c = 0; c < TileChunk.ChunkWidth; c++)
                {
                    if (chunkIdx == 0 && c == 0) continue;
                    for (int r = 3; r < TileChunk.ChunkHeight - 2; r++)
                    {
                        TileType surface = chunk.GetTile(c, r);
                        if (surface is TileType.Solid or TileType.OneWayPlatform &&
                            chunk.GetTile(c, r - 1) != TileType.Solid)
                        {
                            // Must never have a Solid ceiling only 1 empty row above a surface (r-2 == Solid leaves only 16px < 22px player)
                            Assert.NotEqual(TileType.Solid, chunk.GetTile(c, r - 2));
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void AllSpawnedPickupsAndGroundMonsters_HaveAtLeastTwoTilesHeadroom()
    {
        var seeds = new[] { "FOO", "DEMO", "POLAR_PINCH_1", "RAVINE_RUN_2026", "OUTPOST_ALPHA", "SUPPLY_007" };
        int ts = PhysicsConstants.TileSize;

        foreach (var seed in seeds)
        {
            var engine = new GameEngine(seed);
            for (int chunkIdx = 0; chunkIdx <= 15; chunkIdx++)
            {
                engine.PopulateChunkEntities(chunkIdx);
            }

            for (int p = 0; p < engine.Pickups.Length; p++)
            {
                var pickup = engine.Pickups[p];
                if (!pickup.IsActive) continue;

                int col = (int)MathF.Floor((pickup.Position.X + PickupItem.Width * 0.5f) / ts);
                int pickupRow = (int)MathF.Floor((pickup.Position.Y + PickupItem.Height * 0.5f) / ts);
                int headRow = pickupRow - 1;

                TileType cell = engine.Grid.GetTile(col, pickupRow);
                TileType headroom = engine.Grid.GetTile(col, headRow);

                Assert.True(cell != TileType.Solid && cell != TileType.Border && cell != TileType.Crate,
                    $"Seed '{seed}': Pickup at ({col},{pickupRow}) spawned inside blocked tile {cell}.");
                Assert.True(headroom != TileType.Solid && headroom != TileType.Border && headroom != TileType.Crate,
                    $"Seed '{seed}': Pickup at ({col},{pickupRow}) is pinched under ceiling tile {headroom} at row {headRow}.");
            }
        }
    }

    [Fact]
    public void HoverDrone_SpawnsOnlyInEmptyAirspace_NeverInsideSolidBlocks()
    {
        var seeds = new[] { "FOO", "DEMO", "POLAR_PINCH_1", "RAVINE_RUN_2026", "OUTPOST_ALPHA", "REINDEER_99" };
        int ts = PhysicsConstants.TileSize;

        foreach (var seed in seeds)
        {
            var engine = new GameEngine(seed);
            for (int chunkIdx = 0; chunkIdx <= 20; chunkIdx++)
            {
                engine.PopulateChunkEntities(chunkIdx);
            }

            for (int m = 0; m < engine.Monsters.Length; m++)
            {
                var monster = engine.Monsters[m];
                if (!monster.IsActive || monster.Type != MonsterType.HoverDrone) continue;

                int minCol = (int)MathF.Floor((monster.Position.X + 1.0f) / ts);
                int maxCol = (int)MathF.Floor((monster.Position.X + monster.Width - 1.0f) / ts);
                int minRow = (int)MathF.Floor((monster.Position.Y + 1.0f) / ts);
                int maxRow = (int)MathF.Floor((monster.Position.Y + monster.Height - 1.0f) / ts);

                for (int c = minCol; c <= maxCol; c++)
                {
                    for (int r = minRow; r <= maxRow; r++)
                    {
                        TileType tile = engine.Grid.GetTile(c, r);
                        Assert.False(tile is TileType.Solid or TileType.Border or TileType.Crate,
                            $"Seed '{seed}': HoverDrone overlapped solid tile {tile} at ({c},{r}) on spawn.");
                    }
                }
            }
        }
    }

    [Fact]
    public void HoverDrone_FlightAndBobbing_NeverClipsIntoFloorOrStepWall()
    {
        var gen = new WorldGenerator("FOO");
        var grid = new TileGrid(gen);
        int ts = PhysicsConstants.TileSize;

        // Clear cols 2..12 rows 10..27, solid floor at row 27..28, plus a 1-tile rising step at col 9 row 26
        for (int c = 2; c <= 12; c++)
        {
            for (int r = 10; r < 27; r++) grid.SetTile(c, r, TileType.Empty);
            grid.SetTile(c, 27, TileType.Solid);
        }
        grid.SetTile(9, 26, TileType.Solid); // 1-tile raised platform edge whose top is at Y = 26 * 16 = 416

        // Spawn reindeer using exact GameEngine height (platformTop - 24) above solid floor at row 27
        var reindeer = new MonsterEntity();
        reindeer.Spawn(0, MonsterType.HoverDrone, new Vector2f(5 * ts, 27 * ts - 24.0f));
        reindeer.Facing = Direction.Right;
        reindeer.Velocity = new Vector2f(40.0f, 0.0f);

        // Simulate at high refresh (dt = 0.008f) & normal rate to catch unscaled frame-rate Y drift
        for (int f = 0; f < 360; f++)
        {
            reindeer.Update(0.008f, grid, Vector2f.Zero, out _, out _, out _, out _);

            int minCol = (int)MathF.Floor((reindeer.Position.X + 1.0f) / ts);
            int maxCol = (int)MathF.Floor((reindeer.Position.X + reindeer.Width - 1.0f) / ts);
            int minRow = (int)MathF.Floor((reindeer.Position.Y) / ts);
            int maxRow = (int)MathF.Floor((reindeer.Position.Y + reindeer.Height - 0.1f) / ts);

            for (int c = minCol; c <= maxCol; c++)
            {
                for (int r = minRow; r <= maxRow; r++)
                {
                    TileType tile = grid.GetTile(c, r);
                    Assert.False(tile is TileType.Solid or TileType.Border or TileType.Crate,
                        $"Frame {f}: HoverDrone clipped into {tile} at ({c},{r}), Y={reindeer.Position.Y:F2}.");
                }
            }
        }
    }

    [Fact]
    public void GroundPath_NeverHasUnjumpableObstacleStepAboveTwoTiles()
    {
        var seeds = new[] { "FOO", "DEMO", "POLAR_PINCH_1", "RAVINE_RUN_2026", "OUTPOST_ALPHA", "SUPPLY_007", "EASY_ADVANCE" };

        foreach (var seed in seeds)
        {
            var gen = new WorldGenerator(seed);
            for (int chunkIdx = 0; chunkIdx <= 30; chunkIdx++)
            {
                var chunk = gen.GenerateChunk(chunkIdx);

                // Measure effective ground surface top (bedrock top or crate obstacle stacked continuously on ground)
                int[] groundTop = new int[TileChunk.ChunkWidth];
                int[] crateBarrierTop = new int[TileChunk.ChunkWidth];

                for (int c = 0; c < TileChunk.ChunkWidth; c++)
                {
                    int bedrockTop = TileChunk.ChunkHeight - 1;
                    while (bedrockTop > 0 && chunk.GetTile(c, bedrockTop - 1) == TileType.Solid)
                    {
                        bedrockTop--;
                    }
                    // Handle 1-tile recessed pit: surface elevation sits at hazardRow - 1 (same as flat floor level)
                    if (bedrockTop > 1 && chunk.GetTile(c, bedrockTop - 1) == TileType.Hazard)
                    {
                        bedrockTop -= 2;
                    }
                    groundTop[c] = bedrockTop;

                    int obstacleTop = bedrockTop;
                    while (obstacleTop > 0 && chunk.GetTile(c, obstacleTop - 1) == TileType.Crate)
                    {
                        obstacleTop--;
                    }
                    crateBarrierTop[c] = obstacleTop;
                }

                int startC = chunkIdx == 0 ? 2 : 1;
                for (int c = startC; c < TileChunk.ChunkWidth; c++)
                {
                    // Pure terrain slope per single step should be gentle (<= 1 tile = 16px)
                    int terrainRise = Math.Abs(groundTop[c] - groundTop[c - 1]);
                    Assert.True(terrainRise <= 1,
                        $"Seed '{seed}' chunk {chunkIdx} col {c}: terrain slope {terrainRise} tiles exceeds easy 1-tile step.");

                    // Effective crate+ground rise left-to-right OR right-to-left must never require >2 tiles jump unless ladder adjacent
                    bool hasAdjacentLadder =
                        chunk.GetTile(c - 1, crateBarrierTop[c - 1] - 1) == TileType.Ladder ||
                        chunk.GetTile(c, crateBarrierTop[c] - 1) == TileType.Ladder;
                    int combinedStep = Math.Abs(crateBarrierTop[c] - crateBarrierTop[c - 1]);
                    Assert.True(combinedStep <= 2 || hasAdjacentLadder,
                        $"Seed '{seed}' chunk {chunkIdx} col {c}: obstacle step {combinedStep} tiles (>2 tiles) cannot be easily jumped.");
                }
            }
        }
    }

    [Fact]
    public void UpperPlatforms_AreAlwaysReachableByLadderOrShortJump()
    {
        var seeds = new[] { "FOO", "DEMO", "POLAR_PINCH_1", "RAVINE_RUN_2026", "OUTPOST_ALPHA", "SUPPLY_007", "EASY_ADVANCE" };

        foreach (var seed in seeds)
        {
            var gen = new WorldGenerator(seed);
            for (int chunkIdx = 0; chunkIdx <= 30; chunkIdx++)
            {
                var chunk = gen.GenerateChunk(chunkIdx);

                // Find contiguous elevated walkable segments (width >= 2, above terrain floor)
                for (int r = 4; r <= 26; r++)
                {
                    int c = chunkIdx == 0 ? 1 : 0;
                    while (c < TileChunk.ChunkWidth)
                    {
                        bool isDeckTile = chunk.GetTile(c, r) is TileType.OneWayPlatform or TileType.Solid &&
                                          chunk.GetTile(c, r - 1) != TileType.Solid &&
                                          chunk.GetTile(c, r - 2) != TileType.Solid &&
                                          r < FindBedrockTop(chunk, c);

                        if (!isDeckTile)
                        {
                            c++;
                            continue;
                        }

                        int segStart = c;
                        while (c < TileChunk.ChunkWidth &&
                               chunk.GetTile(c, r) is TileType.OneWayPlatform or TileType.Solid &&
                               chunk.GetTile(c, r - 1) != TileType.Solid &&
                               r < FindBedrockTop(chunk, c))
                        {
                            c++;
                        }
                        int segEnd = c - 1;

                        // Segment must either have a ladder intersecting it/adjacent to it OR a lower surface within 2 rows vertically & 3 cols horizontally
                        bool reachable = false;
                        for (int sc = Math.Max(0, segStart - 1); sc <= Math.Min(TileChunk.ChunkWidth - 1, segEnd + 1); sc++)
                        {
                            if (chunk.GetTile(sc, r) == TileType.Ladder || chunk.GetTile(sc, r + 1) == TileType.Ladder)
                            {
                                reachable = true;
                                break;
                            }
                        }

                        if (!reachable)
                        {
                            for (int sc = Math.Max(0, segStart - 3); sc <= Math.Min(TileChunk.ChunkWidth - 1, segEnd + 3); sc++)
                            {
                                for (int sr = r + 1; sr <= Math.Min(TileChunk.ChunkHeight - 2, r + 2); sr++)
                                {
                                    if (chunk.GetTile(sc, sr) is TileType.Solid or TileType.OneWayPlatform or TileType.Crate or TileType.Ladder)
                                    {
                                        reachable = true;
                                        break;
                                    }
                                }
                                if (reachable) break;
                            }
                        }

                        Assert.True(reachable,
                            $"Seed '{seed}' chunk {chunkIdx}: elevated deck cols {segStart}..{segEnd} at row {r} is unreachable by jump or ladder.");
                    }
                }
            }
        }
    }

    private static int FindBedrockTop(TileChunk chunk, int col)
    {
        int r = TileChunk.ChunkHeight - 1;
        while (r > 0 && chunk.GetTile(col, r - 1) == TileType.Solid) r--;
        return r;
    }
}
