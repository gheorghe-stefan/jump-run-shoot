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

    [Fact]
    public void StartingChunk_ContainsLaddersAndCratesConnectingFloors()
    {
        var gen = new WorldGenerator("TEST_SEED_99");
        var startChunk = gen.GenerateChunk(0);

        bool hasLadders = false;
        bool hasCrates = false;
        bool hasUpperPlatforms = false;

        for (int r = 0; r < TileChunk.ChunkHeight; r++)
        {
            for (int c = 0; c < TileChunk.ChunkWidth; c++)
            {
                var tile = startChunk.GetTile(c, r);
                if (tile == TileType.Ladder) hasLadders = true;
                if (tile == TileType.Crate) hasCrates = true;
                if (r < 25 && (tile == TileType.OneWayPlatform || tile == TileType.Solid)) hasUpperPlatforms = true;
            }
        }

        Assert.True(hasLadders, "Starting chunk must contain ladders for vertical ascent.");
        Assert.True(hasCrates, "Starting chunk must contain crate pyramids.");
        Assert.True(hasUpperPlatforms, "Starting chunk must have multi-deck platforms.");
    }

    [Fact]
    public void ProceduralChunks_ContainDiverseArchetypesAndLadders()
    {
        var gen = new WorldGenerator("DUKE_SEEDED_RUN");
        int totalLadders = 0;
        int totalCrates = 0;

        for (int chunkIdx = 1; chunkIdx <= 8; chunkIdx++)
        {
            var chunk = gen.GenerateChunk(chunkIdx);
            for (int r = 0; r < TileChunk.ChunkHeight; r++)
            {
                for (int c = 0; c < TileChunk.ChunkWidth; c++)
                {
                    var t = chunk.GetTile(c, r);
                    if (t == TileType.Ladder) totalLadders++;
                    if (t == TileType.Crate) totalCrates++;
                }
            }
        }

        Assert.True(totalLadders > 0, "Procedural chunks must generate ladders connecting vertical decks.");
        Assert.True(totalCrates > 0, "Procedural chunks must generate crate staircases/pyramids.");
    }
}
