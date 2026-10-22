using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class DefaultSeedAndPersistenceTests
{
    [Fact]
    public void GameEngine_DefaultSeed_IsFooWhenUnspecifiedOrEmpty()
    {
        var engineDefault = new GameEngine();
        Assert.Equal("FOO", engineDefault.CurrentSeed);

        var engineEmpty = new GameEngine("");
        Assert.Equal("FOO", engineEmpty.CurrentSeed);

        var engineWhitespace = new GameEngine("   ");
        Assert.Equal("FOO", engineWhitespace.CurrentSeed);

        var engineNull = new GameEngine(null!);
        Assert.Equal("FOO", engineNull.CurrentSeed);
    }

    [Fact]
    public void WorldGenerator_DefaultSeed_IsFooWhenUnspecifiedOrEmpty()
    {
        var genDefault = new WorldGenerator();
        Assert.Equal("FOO", genDefault.Seed);

        var genEmpty = new WorldGenerator("");
        Assert.Equal("FOO", genEmpty.Seed);

        var genWhitespace = new WorldGenerator("   ");
        Assert.Equal("FOO", genWhitespace.Seed);

        var genNull = new WorldGenerator(null!);
        Assert.Equal("FOO", genNull.Seed);
    }

    [Fact]
    public void GameEngine_CustomSeed_IsPreserved()
    {
        var engine = new GameEngine("CASTLE99");
        Assert.Equal("CASTLE99", engine.CurrentSeed);

        engine.Restart("DUNGEON01");
        Assert.Equal("DUNGEON01", engine.CurrentSeed);
    }

    [Fact]
    public void WorldGeneration_FooSeed_ProducesDeterministicValidTerrain()
    {
        var engineA = new GameEngine("FOO");
        var engineB = new GameEngine("FOO");

        Assert.Equal(engineA.Player.Position.X, engineB.Player.Position.X);
        Assert.Equal(engineA.Player.Position.Y, engineB.Player.Position.Y);

        for (int r = 0; r < TileChunk.ChunkHeight; r++)
        {
            for (int c = 0; c < TileChunk.ChunkWidth; c++)
            {
                Assert.Equal(engineA.Grid.GetTile(c, r), engineB.Grid.GetTile(c, r));
            }
        }
    }
}
