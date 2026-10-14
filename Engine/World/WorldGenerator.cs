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

        // Ground foundation layer (rows 28-31)
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

        // Tier 1: Lower catwalks (rows 21-23)
        GeneratePlatformTier(chunk, rng, minRow: 21, maxRow: 23, count: rng.NextInt(1, 3));

        // Tier 2: Mid gantries (rows 14-16)
        GeneratePlatformTier(chunk, rng, minRow: 14, maxRow: 16, count: rng.NextInt(1, 3));

        // Tier 3: Upper catwalks (rows 8-10)
        GeneratePlatformTier(chunk, rng, minRow: 8, maxRow: 10, count: rng.NextInt(1, 3));

        // Tier 4: Rooftops & high rafters (rows 2-4)
        if (rng.NextBool(0.70f))
        {
            GeneratePlatformTier(chunk, rng, minRow: 2, maxRow: 4, count: 1);
        }

        return chunk;
    }

    private static void GeneratePlatformTier(TileChunk chunk, DeterministicRandom rng, int minRow, int maxRow, int count)
    {
        for (int p = 0; p < count; p++)
        {
            int pRow = rng.NextInt(minRow, maxRow + 1);
            int pStart = rng.NextInt(0, 10);
            int pLen = rng.NextInt(3, 7);
            bool isOneWay = rng.NextBool(0.65f);
            TileType type = isOneWay ? TileType.OneWayPlatform : TileType.Solid;

            for (int col = pStart; col < Math.Min(TileChunk.ChunkWidth, pStart + pLen); col++)
            {
                chunk.SetTile(col, pRow, type);
            }
        }
    }

    private static void GenerateStartingChunk(TileChunk chunk)
    {
        // Continuous solid ground floor (rows 28-31)
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

        // Starter scaffolding tiers to climb immediately
        for (int c = 4; c <= 8; c++) chunk.SetTile(c, 23, TileType.OneWayPlatform);
        for (int c = 9; c <= 13; c++) chunk.SetTile(c, 17, TileType.OneWayPlatform);
        for (int c = 3; c <= 7; c++) chunk.SetTile(c, 11, TileType.OneWayPlatform);
        for (int c = 8; c <= 14; c++) chunk.SetTile(c, 5, TileType.OneWayPlatform);
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
