using System;

namespace JumpRunShoot.Engine.World;

public sealed class WorldGenerator
{
    public string Seed { get; private set; } = "RETRO1";
    private uint _numericSeed;

    public WorldGenerator(string seed = "RETRO1")
    {
        SetSeed(seed);
    }

    public void SetSeed(string seed)
    {
        Seed = string.IsNullOrWhiteSpace(seed) ? "RETRO1" : seed.Trim();
        var rng = new DeterministicRandom(Seed);
        _numericSeed = rng.NextUInt();
    }

    public TileChunk GenerateChunk(int chunkIndex)
    {
        var chunk = new TileChunk(chunkIndex);

        if (chunkIndex == 0)
        {
            GenerateStartingChunk(chunk);
            return chunk;
        }

        if (chunkIndex < 0)
        {
            GenerateLeftBoundary(chunk);
            return chunk;
        }

        // Deterministic generation for this chunk
        uint chunkSeed = _numericSeed ^ ((uint)chunkIndex * 2654435761u);
        var rng = new DeterministicRandom(chunkSeed);

        // 1. Ground foundation layer (rows 28-31)
        bool hasPit = rng.NextBool(0.20f);
        int pitStart = hasPit ? rng.NextInt(3, 11) : -1;
        int pitWidth = hasPit ? rng.NextInt(2, 4) : 0;

        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            if (c >= pitStart && c < pitStart + pitWidth)
            {
                // Spikes in the pit floor
                chunk.SetTile(c, 31, TileType.Hazard);
            }
            else
            {
                chunk.SetTile(c, 28, TileType.Solid);
                chunk.SetTile(c, 29, TileType.Solid);
                chunk.SetTile(c, 30, TileType.Solid);
                chunk.SetTile(c, 31, TileType.Solid);
            }
        }

        // 2. Reachable Multi-Tier Platforms (Step spacing 3 tiles max = 48px jump)
        // Stepping Stone 1: row 25
        GenerateStep(chunk, rng, row: 25, minCol: 1, maxCol: 12, minLen: 2, maxLen: 4);

        // Tier 1 Catwalk: row 22
        int t1Col = rng.NextInt(2, 8);
        int t1Len = rng.NextInt(4, 8);
        GenerateLedge(chunk, t1Col, row: 22, t1Len, TileType.OneWayPlatform);

        // Ladder connecting Ground (row 28) to Tier 1 (row 22)
        if (rng.NextBool(0.85f))
        {
            int ladderCol = Math.Clamp(t1Col + rng.NextInt(1, Math.Max(2, t1Len - 1)), 0, TileChunk.ChunkWidth - 1);
            PlaceLadder(chunk, ladderCol, topRow: 22, bottomRow: 28);
        }

        // Stepping Stone 2: row 19
        GenerateStep(chunk, rng, row: 19, minCol: 3, maxCol: 13, minLen: 2, maxLen: 4);

        // Tier 2 Mid-Deck: row 16
        int t2Col = rng.NextInt(2, 8);
        int t2Len = rng.NextInt(4, 8);
        GenerateLedge(chunk, t2Col, row: 16, t2Len, TileType.OneWayPlatform);

        // Ladder connecting Tier 1 (row 22) to Tier 2 (row 16)
        if (rng.NextBool(0.80f))
        {
            int ladderCol = Math.Clamp(t2Col + rng.NextInt(1, Math.Max(2, t2Len - 1)), 0, TileChunk.ChunkWidth - 1);
            PlaceLadder(chunk, ladderCol, topRow: 16, bottomRow: 22);
        }

        // Stepping Stone 3: row 13
        GenerateStep(chunk, rng, row: 13, minCol: 2, maxCol: 12, minLen: 2, maxLen: 4);

        // Tier 3 Upper Deck: row 10
        int t3Col = rng.NextInt(2, 8);
        int t3Len = rng.NextInt(4, 8);
        GenerateLedge(chunk, t3Col, row: 10, t3Len, TileType.OneWayPlatform);

        // Ladder connecting Tier 2 (row 16) to Tier 3 (row 10)
        if (rng.NextBool(0.75f))
        {
            int ladderCol = Math.Clamp(t3Col + rng.NextInt(1, Math.Max(2, t3Len - 1)), 0, TileChunk.ChunkWidth - 1);
            PlaceLadder(chunk, ladderCol, topRow: 10, bottomRow: 16);
        }

        // Stepping Stone 4: row 7
        GenerateStep(chunk, rng, row: 7, minCol: 3, maxCol: 13, minLen: 2, maxLen: 4);

        // Tier 4 Rooftop Deck: row 4
        int t4Col = rng.NextInt(2, 8);
        int t4Len = rng.NextInt(4, 9);
        GenerateLedge(chunk, t4Col, row: 4, t4Len, TileType.OneWayPlatform);

        // Ladder connecting Tier 3 (row 10) to Tier 4 (row 4)
        if (rng.NextBool(0.70f))
        {
            int ladderCol = Math.Clamp(t4Col + rng.NextInt(1, Math.Max(2, t4Len - 1)), 0, TileChunk.ChunkWidth - 1);
            PlaceLadder(chunk, ladderCol, topRow: 4, bottomRow: 10);
        }

        return chunk;
    }

    private static void GenerateStep(TileChunk chunk, DeterministicRandom rng, int row, int minCol, int maxCol, int minLen, int maxLen)
    {
        int col = rng.NextInt(minCol, Math.Max(minCol + 1, maxCol - 2));
        int len = rng.NextInt(minLen, maxLen + 1);
        GenerateLedge(chunk, col, row, len, TileType.OneWayPlatform);
    }

    private static void GenerateLedge(TileChunk chunk, int startCol, int row, int len, TileType type)
    {
        for (int c = startCol; c < Math.Min(TileChunk.ChunkWidth, startCol + len); c++)
        {
            // Do not overwrite existing ladders
            if (chunk.GetTile(c, row) != TileType.Ladder)
            {
                chunk.SetTile(c, row, type);
            }
        }
    }

    private static void PlaceLadder(TileChunk chunk, int col, int topRow, int bottomRow)
    {
        if ((uint)col >= TileChunk.ChunkWidth) return;
        for (int r = topRow; r <= bottomRow; r++)
        {
            // Keep solid blocks solid, turn empty/oneway space into climbable ladder
            if (chunk.GetTile(col, r) != TileType.Solid)
            {
                chunk.SetTile(col, r, TileType.Ladder);
            }
        }
    }

    private static void GenerateStartingChunk(TileChunk chunk)
    {
        // 1. Solid ground floor (rows 28-31)
        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            chunk.SetTile(c, 28, TileType.Solid);
            chunk.SetTile(c, 29, TileType.Solid);
            chunk.SetTile(c, 30, TileType.Solid);
            chunk.SetTile(c, 31, TileType.Solid);
        }

        // Left perimeter boundary wall
        for (int r = 0; r < 28; r++)
        {
            chunk.SetTile(0, r, TileType.Solid);
        }

        // 2. Stepped scaffolding with effortless 3-tile jump intervals (48px)
        // Step 1: row 25
        GenerateLedge(chunk, startCol: 2, row: 25, len: 3, TileType.OneWayPlatform);
        // Tier 1 Catwalk: row 22
        GenerateLedge(chunk, startCol: 4, row: 22, len: 6, TileType.OneWayPlatform);
        // Ladder from ground to Tier 1
        PlaceLadder(chunk, col: 5, topRow: 22, bottomRow: 28);

        // Step 2: row 19
        GenerateLedge(chunk, startCol: 9, row: 19, len: 3, TileType.OneWayPlatform);
        // Tier 2 Mid-Deck: row 16
        GenerateLedge(chunk, startCol: 5, row: 16, len: 6, TileType.OneWayPlatform);
        // Ladder from Tier 1 to Tier 2
        PlaceLadder(chunk, col: 8, topRow: 16, bottomRow: 22);

        // Step 3: row 13
        GenerateLedge(chunk, startCol: 10, row: 13, len: 3, TileType.OneWayPlatform);
        // Tier 3 Upper Deck: row 10
        GenerateLedge(chunk, startCol: 4, row: 10, len: 6, TileType.OneWayPlatform);
        // Ladder from Tier 2 to Tier 3
        PlaceLadder(chunk, col: 6, topRow: 10, bottomRow: 16);

        // Step 4: row 7
        GenerateLedge(chunk, startCol: 9, row: 7, len: 3, TileType.OneWayPlatform);
        // Tier 4 Rooftop Deck: row 4
        GenerateLedge(chunk, startCol: 4, row: 4, len: 8, TileType.OneWayPlatform);
        // Ladder from Tier 3 to Tier 4
        PlaceLadder(chunk, col: 8, topRow: 4, bottomRow: 10);
    }

    private static void GenerateLeftBoundary(TileChunk chunk)
    {
        for (int r = 0; r < TileChunk.ChunkHeight; r++)
        {
            for (int c = 0; c < TileChunk.ChunkWidth; c++)
            {
                chunk.SetTile(c, r, TileType.Solid);
            }
        }
    }
}
