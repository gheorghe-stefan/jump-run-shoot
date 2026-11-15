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
}
