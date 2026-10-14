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

        // Ground layer (rows 11-13)
        bool hasPit = rng.NextBool(0.20f);
        int pitStart = hasPit ? rng.NextInt(4, 10) : -1;
        int pitWidth = hasPit ? rng.NextInt(2, 4) : 0;

        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            if (c >= pitStart && c < pitStart + pitWidth)
            {
                // Spikes in the pit
                chunk.SetTile(c, 13, TileType.Hazard);
            }
            else
            {
                chunk.SetTile(c, 11, TileType.Solid);
                chunk.SetTile(c, 12, TileType.Solid);
                chunk.SetTile(c, 13, TileType.Solid);
            }
        }

        // Mid-level platforms (rows 7-8)
        int numPlatforms = rng.NextInt(1, 3);
        for (int p = 0; p < numPlatforms; p++)
        {
            int pRow = rng.NextInt(6, 9);
            int pStart = rng.NextInt(1, 10);
            int pLen = rng.NextInt(3, 7);
            bool isOneWay = rng.NextBool(0.7f);
            TileType type = isOneWay ? TileType.OneWayPlatform : TileType.Solid;

            for (int col = pStart; col < Math.Min(TileChunk.ChunkWidth - 1, pStart + pLen); col++)
            {
                chunk.SetTile(col, pRow, type);
            }
        }

        // High platforms (rows 3-4) for higher jumps
        if (rng.NextBool(0.5f))
        {
            int highRow = rng.NextInt(3, 5);
            int highStart = rng.NextInt(3, 11);
            int highLen = rng.NextInt(2, 6);
            for (int col = highStart; col < Math.Min(TileChunk.ChunkWidth - 1, highStart + highLen); col++)
            {
                chunk.SetTile(col, highRow, TileType.OneWayPlatform);
            }
        }

        return chunk;
    }

    private static void GenerateStartingChunk(TileChunk chunk)
    {
        // Continuous solid floor
        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            chunk.SetTile(c, 11, TileType.Solid);
            chunk.SetTile(c, 12, TileType.Solid);
            chunk.SetTile(c, 13, TileType.Solid);
        }

        // Left boundary wall
        for (int r = 0; r < 12; r++)
        {
            chunk.SetTile(0, r, TileType.Solid);
        }

        // Starter one-way platforms
        for (int c = 4; c <= 8; c++) chunk.SetTile(c, 8, TileType.OneWayPlatform);
        for (int c = 10; c <= 14; c++) chunk.SetTile(c, 5, TileType.OneWayPlatform);
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
