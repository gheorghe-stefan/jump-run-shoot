using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class WorldGeneratorTests
{
    [Fact]
    public void GenerateChunk_SameSeedProducesIdenticalTiles()
    {
        var genA = new WorldGenerator("SEEDED_WORLD_123");
        var genB = new WorldGenerator("SEEDED_WORLD_123");

        for (int chunkIdx = 0; chunkIdx < 10; chunkIdx++)
        {
            var chunkA = genA.GenerateChunk(chunkIdx);
            var chunkB = genB.GenerateChunk(chunkIdx);

            for (int r = 0; r < TileChunk.ChunkHeight; r++)
            {
                for (int c = 0; c < TileChunk.ChunkWidth; c++)
                {
                    Assert.Equal(chunkA.GetTile(c, r), chunkB.GetTile(c, r));
                }
            }
        }
    }

    [Fact]
    public void GenerateChunk_DifferentSeedsProduceDifferentTiles()
    {
        var genA = new WorldGenerator("SEED_ALPHA");
        var genB = new WorldGenerator("SEED_OMEGA");

        bool foundDifference = false;
        for (int chunkIdx = 1; chunkIdx < 5; chunkIdx++)
        {
            var chunkA = genA.GenerateChunk(chunkIdx);
            var chunkB = genB.GenerateChunk(chunkIdx);

            for (int r = 0; r < TileChunk.ChunkHeight; r++)
            {
                for (int c = 0; c < TileChunk.ChunkWidth; c++)
                {
                    if (chunkA.GetTile(c, r) != chunkB.GetTile(c, r))
                    {
                        foundDifference = true;
                        break;
                    }
                }
                if (foundDifference) break;
            }
            if (foundDifference) break;
        }

        Assert.True(foundDifference, "Different seeds should produce different terrain!");
    }

    [Fact]
    public void StartingChunk_HasSafeContinuousGround()
    {
        var gen = new WorldGenerator("ANY_SEED");
        var startChunk = gen.GenerateChunk(0);

        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            Assert.Equal(TileType.Solid, startChunk.GetTile(c, 28));
        }
    }
}
