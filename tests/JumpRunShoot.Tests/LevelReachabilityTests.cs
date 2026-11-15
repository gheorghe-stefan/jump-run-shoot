using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class LevelReachabilityTests
{
    [Fact]
    public void OutpostStructures_HaveAtLeastTwoTilesDoorwayClearance()
    {
        var seeds = new[] { "TEST_BUNKER", "OUTPOST_ALPHA", "POLAR_RUINS_99", "FOO" };
        int inspectedWalls = 0;

        foreach (var seed in seeds)
        {
            var gen = new WorldGenerator(seed);
            for (int chunkIdx = 1; chunkIdx <= 25; chunkIdx++)
            {
                var chunk = gen.GenerateChunk(chunkIdx);
                for (int c = 1; c < TileChunk.ChunkWidth - 1; c++)
                {
                    // Find true ground terrain top by scanning upward from bedrock bottom (row 31)
                    int floorRow = -1;
                    for (int r = TileChunk.ChunkHeight - 1; r >= WorldGenerator.MinGroundRow; r--)
                    {
                        if (chunk.GetTile(c, r) != TileType.Solid)
                        {
                            floorRow = r + 1;
                            break;
                        }
                    }
                    if (floorRow < 0 || floorRow >= TileChunk.ChunkHeight) continue;

                    // Detect if this column has an overhead structural wall/roof section
                    bool hasOverheadWall = false;
                    for (int r = floorRow - 4; r >= 6; r--)
                    {
                        if (chunk.GetTile(c, r) == TileType.Solid && chunk.GetTile(c, r - 1) == TileType.Solid)
                        {
                            hasOverheadWall = true;
                            break;
                        }
                    }

                    if (hasOverheadWall)
                    {
                        inspectedWalls++;
                        Assert.NotEqual(TileType.Solid, chunk.GetTile(c, floorRow - 1));
                        Assert.NotEqual(TileType.Solid, chunk.GetTile(c, floorRow - 2));
                    }
                }
            }
        }

        Assert.True(inspectedWalls > 0, "Expected parametric generators to build elevated walls with doorways.");
    }

    [Fact]
    public void VerticalLadderStructures_HaveClearEntryAndNoTwinLadderPillarArtifact()
    {
        var seeds = new[] { "TEST_TOWER", "CLIMB_SEEDS_42", "RANDOM_99123", "FOO" };
        int inspectedLadders = 0;

        foreach (var seed in seeds)
        {
            var gen = new WorldGenerator(seed);
            for (int chunkIdx = 1; chunkIdx <= 25; chunkIdx++)
            {
                var chunk = gen.GenerateChunk(chunkIdx);
                for (int c = 1; c < TileChunk.ChunkWidth - 1; c++)
                {
                    for (int r = 3; r < TileChunk.ChunkHeight - 4; r++)
                    {
                        if (chunk.GetTile(c, r) == TileType.Solid)
                        {
                            // Never allow twin ladders sandwiching a 1-tile solid column
                            bool sandwichedByLadders =
                                chunk.GetTile(c - 1, r) == TileType.Ladder &&
                                chunk.GetTile(c + 1, r) == TileType.Ladder;
                            Assert.False(sandwichedByLadders,
                                $"Seed '{seed}' chunk {chunkIdx} col {c} row {r} has twin-ladder-over-pillar artifact.");
                        }

                        if (chunk.GetTile(c, r) == TileType.Ladder)
                        {
                            inspectedLadders++;
                            // Ladder shaft cannot be sealed on both left and right by >=2-tile tall walls at the same rows
                            bool boxedIn =
                                chunk.GetTile(c - 1, r) == TileType.Solid &&
                                chunk.GetTile(c + 1, r) == TileType.Solid &&
                                chunk.GetTile(c - 1, r - 1) == TileType.Solid &&
                                chunk.GetTile(c + 1, r - 1) == TileType.Solid;
                            Assert.False(boxedIn, $"Ladder at chunk {chunkIdx} ({c},{r}) must not be walled off on both sides.");
                        }
                    }
                }
            }
        }

        Assert.True(inspectedLadders > 0);
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
