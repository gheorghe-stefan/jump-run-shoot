namespace JumpRunShoot.Engine.World;

public sealed class TileChunk
{
    public const int ChunkWidth = 16;
    public const int ChunkHeight = 14;
    public const int TotalTiles = ChunkWidth * ChunkHeight;

    public int ChunkIndex { get; }
    public TileType[] Tiles { get; } = new TileType[TotalTiles];

    public TileChunk(int chunkIndex)
    {
        ChunkIndex = chunkIndex;
    }

    public TileType GetTile(int localCol, int localRow)
    {
        if ((uint)localCol < ChunkWidth && (uint)localRow < ChunkHeight)
        {
            return Tiles[localRow * ChunkWidth + localCol];
        }
        return TileType.Empty;
    }

    public void SetTile(int localCol, int localRow, TileType type)
    {
        if ((uint)localCol < ChunkWidth && (uint)localRow < ChunkHeight)
        {
            Tiles[localRow * ChunkWidth + localCol] = type;
        }
    }
}
