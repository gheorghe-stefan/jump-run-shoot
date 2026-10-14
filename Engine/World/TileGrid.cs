using System.Collections.Generic;
using JumpRunShoot.Engine.Physics;

namespace JumpRunShoot.Engine.World;

public sealed class TileGrid : ITileMap
{
    private readonly Dictionary<int, TileChunk> _chunks = new();
    private readonly WorldGenerator _generator;
    private int _maxVisitedChunk = 0;

    public WorldGenerator Generator => _generator;

    public TileGrid(WorldGenerator generator)
    {
        _generator = generator;
    }

    public void Reset()
    {
        _chunks.Clear();
        _maxVisitedChunk = 0;
    }

    public TileChunk GetOrCreateChunk(int chunkIndex)
    {
        if (!_chunks.TryGetValue(chunkIndex, out var chunk))
        {
            chunk = _generator.GenerateChunk(chunkIndex);
            _chunks[chunkIndex] = chunk;

            // Chunk Windowing: Evict chunks more than 8 chunks behind the player
            if (chunkIndex > _maxVisitedChunk)
            {
                _maxVisitedChunk = chunkIndex;
                int minKeep = _maxVisitedChunk - 8;
                List<int>? toRemove = null;
                foreach (var k in _chunks.Keys)
                {
                    if (k < minKeep)
                    {
                        toRemove ??= new List<int>();
                        toRemove.Add(k);
                    }
                }
                if (toRemove != null)
                {
                    foreach (var k in toRemove) _chunks.Remove(k);
                }
            }
        }
        return chunk;
    }

    public TileType GetTile(int col, int row)
    {
        if (row < 0 || row >= TileChunk.ChunkHeight || col < 0) return TileType.Border;

        int chunkIndex = MathDiv(col, TileChunk.ChunkWidth);
        int localCol = MathMod(col, TileChunk.ChunkWidth);

        var chunk = GetOrCreateChunk(chunkIndex);
        return chunk.GetTile(localCol, row);
    }

    public void SetTile(int col, int row, TileType type)
    {
        if (row < 0 || row >= TileChunk.ChunkHeight) return;

        int chunkIndex = MathDiv(col, TileChunk.ChunkWidth);
        int localCol = MathMod(col, TileChunk.ChunkWidth);

        var chunk = GetOrCreateChunk(chunkIndex);
        chunk.SetTile(localCol, row, type);
    }

    private static int MathDiv(int a, int b)
    {
        int rem = a % b;
        int div = a / b;
        return (rem != 0 && (a < 0 ^ b < 0)) ? div - 1 : div;
    }

    private static int MathMod(int a, int b)
    {
        int rem = a % b;
        return rem < 0 ? rem + b : rem;
    }
}
