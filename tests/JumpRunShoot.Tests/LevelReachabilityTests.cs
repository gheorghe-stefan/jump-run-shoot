using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class LevelReachabilityTests
{
    [Fact]
    public void TechBunker_DoorwayHasAtLeastTwoTilesClearance()
    {
        var gen = new WorldGenerator("TEST_BUNKER");
        // Force TechBunker chunk (archetype 0)
        // Find a chunk that selects archetype 0
        TileChunk? bunkerChunk = null;
        for (int i = 0; i < 20; i++)
        {
            var chunk = gen.GenerateChunk(i);
            // Archetype 0 has outer walls at col 2 and col 13
            if (chunk.GetTile(2, 18) == TileType.Solid && chunk.GetTile(13, 18) == TileType.Solid)
            {
                bunkerChunk = chunk;
                break;
            }
        }

        Assert.NotNull(bunkerChunk);

        // Player height is 24px (1.5 tiles).
        // Ground is row 28. Doorway above ground at rows 26 and 27 must both be non-solid (Empty/Platform)
        // so player can walk through without getting blocked.
        Assert.NotEqual(TileType.Solid, bunkerChunk.GetTile(2, 27));
        Assert.NotEqual(TileType.Solid, bunkerChunk.GetTile(2, 26));

        Assert.NotEqual(TileType.Solid, bunkerChunk.GetTile(13, 27));
        Assert.NotEqual(TileType.Solid, bunkerChunk.GetTile(13, 26));
    }

    [Fact]
    public void ElevatorTower_GroundLevelHasTraversableEntrance()
    {
        var gen = new WorldGenerator("TEST_TOWER");
        TileChunk? towerChunk = null;
        for (int i = 0; i < 20; i++)
        {
            var chunk = gen.GenerateChunk(i);
            // Tower has continuous ladder at col 8
            if (chunk.GetTile(8, 6) == TileType.Ladder && chunk.GetTile(8, 20) == TileType.Ladder)
            {
                towerChunk = chunk;
                break;
            }
        }

        Assert.NotNull(towerChunk);

        // Ground is row 28. Rows 26 and 27 at column 6 and column 10 must have a doorway opening
        // so ground-walking players can enter the tower to climb the ladder.
        Assert.NotEqual(TileType.Solid, towerChunk.GetTile(6, 27));
        Assert.NotEqual(TileType.Solid, towerChunk.GetTile(6, 26));

        Assert.NotEqual(TileType.Solid, towerChunk.GetTile(10, 27));
        Assert.NotEqual(TileType.Solid, towerChunk.GetTile(10, 26));
    }

    [Fact]
    public void ChunkZero_GeneratesDifferentlyForDifferentSeeds()
    {
        var genA = new WorldGenerator("ALPHA_SEED");
        var genB = new WorldGenerator("OMEGA_SEED");

        var chunkA = genA.GenerateChunk(0);
        var chunkB = genB.GenerateChunk(0);

        bool foundDifference = false;
        for (int r = 0; r < TileChunk.ChunkHeight; r++)
        {
            for (int c = 4; c < TileChunk.ChunkWidth; c++) // Beyond safe spawn zone
            {
                if (chunkA.GetTile(c, r) != chunkB.GetTile(c, r))
                {
                    foundDifference = true;
                    break;
                }
            }
            if (foundDifference) break;
        }

        Assert.True(foundDifference, "Different seeds must generate different Chunk 0 layouts!");
    }

    [Fact]
    public void PowerupPickup_SetsNotificationMessage()
    {
        var engine = new GameEngine("NOTIF_TEST");
        engine.Player.Position = new Vector2f(50, 400);

        // Spawn a RapidFire powerup directly on top of player
        engine.Pickups[0].Spawn(PickupType.RapidFire, new Vector2f(50, 400));

        engine.Update(0.016f);

        Assert.True(engine.ActiveNotification.Timer > 0f, "Notification timer should be active after collecting powerup!");
        Assert.Contains("RAPID FIRE", engine.ActiveNotification.Text.ToUpperInvariant());
    }
}
