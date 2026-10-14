using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class ClimbingAndPlatformAlignmentTests
{
    [Fact]
    public void Player_CanClimbLadder_UpAndDown_AndHoldPosition()
    {
        var generator = new WorldGenerator("LADDER_TEST");
        var grid = new TileGrid(generator);
        var input = new InputManager();

        // Place a ladder from row 10 to row 20 at column 5
        for (int r = 10; r <= 20; r++)
        {
            grid.SetTile(5, r, TileType.Ladder);
        }

        // Place player inside ladder at column 5, row 15
        var player = new PlayerEntity(new Vector2f(5 * 16, 15 * 16));
        player.IsGrounded = false;

        // 1. Pressing Up climbs upward
        input.HandleKeyDown("ArrowUp");
        input.AdvanceFrame();
        player.Update(0.1f, input, grid);

        Assert.True(player.IsClimbing, "Player should be in climbing mode");
        Assert.Equal(PlayerState.Climbing, player.State);
        Assert.Equal(-PhysicsConstants.ClimbSpeed, player.Velocity.Y);

        // 2. Releasing Up holds position on ladder (gravity disabled)
        input.HandleKeyUp("ArrowUp");
        input.AdvanceFrame();
        player.Update(0.1f, input, grid);

        Assert.True(player.IsClimbing, "Player should still be on ladder");
        Assert.Equal(0.0f, player.Velocity.Y);

        // 3. Pressing Down climbs downward
        input.HandleKeyDown("ArrowDown");
        input.AdvanceFrame();
        player.Update(0.1f, input, grid);

        Assert.True(player.IsClimbing, "Player should still be climbing");
        Assert.Equal(PhysicsConstants.ClimbSpeed, player.Velocity.Y);

        // 4. Pressing Jump leaps off ladder
        input.HandleKeyUp("ArrowDown");
        input.HandleKeyDown("ControlLeft");
        input.AdvanceFrame();
        player.Update(0.1f, input, grid);

        Assert.False(player.IsClimbing, "Jumping should dismount ladder");
        Assert.Equal(PhysicsConstants.JumpSpeed, player.Velocity.Y);
    }

    [Fact]
    public void Monster_FeetAreStrictlyFlush_WithPlatformTop()
    {
        var monster = new MonsterEntity();
        float platformTop = 22 * 16.0f; // Row 22 top edge = 352px

        // HeavyGuard (Height 26)
        monster.Spawn(0, MonsterType.HeavyGuard, new Vector2f(100, platformTop - 26), platformY: platformTop);

        Assert.Equal(26.0f, monster.Height);
        Assert.Equal(platformTop - 26, monster.Position.Y);
        Assert.Equal(platformTop, monster.Position.Y + monster.Height);

        // Update monster horizontally and verify Y remains strictly anchored
        var generator = new WorldGenerator("MONSTER_ALIGN_TEST");
        var grid = new TileGrid(generator);
        monster.Update(0.1f, grid, Vector2f.Zero, out _, out _, out _, out _);

        Assert.Equal(platformTop - 26, monster.Position.Y);
        Assert.Equal(platformTop, monster.Position.Y + monster.Height);
    }

    [Fact]
    public void WorldGenerator_GeneratesLadders_ConnectingUpperDecks()
    {
        var generator = new WorldGenerator("LADDER_GEN_TEST");
        var startChunk = generator.GenerateChunk(0);

        // Verify starting chunk contains climbable ladders
        bool hasLadder = false;
        for (int r = 0; r < TileChunk.ChunkHeight; r++)
        {
            for (int c = 0; c < TileChunk.ChunkWidth; c++)
            {
                if (startChunk.GetTile(c, r) == TileType.Ladder)
                {
                    hasLadder = true;
                    break;
                }
            }
            if (hasLadder) break;
        }

        Assert.True(hasLadder, "Starting chunk must contain vertical climbing ladders to reach upper decks!");
    }
}
