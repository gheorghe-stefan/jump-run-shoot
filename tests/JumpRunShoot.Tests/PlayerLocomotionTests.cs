using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class PlayerLocomotionTests
{
    private readonly PlayerEntity _player;
    private readonly InputManager _input;
    private readonly TileGrid _grid;

    public PlayerLocomotionTests()
    {
        _player = new PlayerEntity(new Vector2f(64, 64));
        _input = new InputManager();
        var generator = new WorldGenerator("TEST_LOCOMOTION");
        _grid = new TileGrid(generator);
        _grid.Reset();

        // Create continuous floor at row 10 (Y = 160)
        for (int c = 0; c < 20; c++) _grid.SetTile(c, 10, TileType.Solid);

        // Position player grounded on floor (feet at 160)
        _player.Position = new Vector2f(64, 160 - PlayerEntity.BoxHeight - PlayerEntity.BoxOffset.Y);
        _player.IsGrounded = true;
    }

    [Fact]
    public void Update_WalkingRight_AcceleratesTowardsWalkSpeed()
    {
        _input.HandleKeyDown("ArrowRight");
        _input.AdvanceFrame();
        _player.Update(0.1f, _input, _grid);

        Assert.True(_player.Velocity.X > 0);
        Assert.True(_player.Velocity.X <= PhysicsConstants.WalkSpeed);
        Assert.Equal(Direction.Right, _player.Facing);
    }

    [Fact]
    public void Update_RunningWithShift_ReachesHigherSpeedThanWalk()
    {
        // First test walk max speed
        _input.HandleKeyDown("ArrowRight");
        for (int i = 0; i < 20; i++)
        {
            _input.AdvanceFrame();
            _player.Update(0.05f, _input, _grid);
        }
        float walkMax = _player.Velocity.X;

        // Now activate Run
        _input.HandleKeyDown("ShiftLeft");
        for (int i = 0; i < 20; i++)
        {
            _input.AdvanceFrame();
            _player.Update(0.05f, _input, _grid);
        }
        float runMax = _player.Velocity.X;

        Assert.Equal(PhysicsConstants.WalkSpeed, walkMax, tolerance: 0.1f);
        Assert.Equal(PhysicsConstants.RunSpeed, runMax, tolerance: 0.1f);
        Assert.True(runMax > walkMax);
    }

    [Fact]
    public void Update_ReleasingMovementKeys_DeceleratesWithFriction()
    {
        _player.Velocity.X = PhysicsConstants.WalkSpeed;

        _input.Reset();
        _input.AdvanceFrame();
        _player.Update(0.05f, _input, _grid);

        Assert.True(_player.Velocity.X < PhysicsConstants.WalkSpeed);
        Assert.True(_player.Velocity.X >= 0);
    }

    [Fact]
    public void Update_PressingJump_AppliesJumpImpulse()
    {
        _input.HandleKeyDown("Space");
        _input.AdvanceFrame();
        float dt = 0.016f;
        _player.Update(dt, _input, _grid);

        // JumpSpeed (-240) plus one frame of gravity (+620 * 0.016)
        float expectedVy = PhysicsConstants.JumpSpeed + PhysicsConstants.Gravity * dt;
        Assert.Equal(expectedVy, _player.Velocity.Y, tolerance: 1.0f);
        Assert.False(_player.IsGrounded);
        Assert.Equal(PlayerState.Jumping, _player.State);
    }

    [Fact]
    public void Update_ReleasingJumpEarly_CutsUpwardAscentVelocity()
    {
        _player.Velocity.Y = -200.0f;
        _player.IsGrounded = false;

        _input.HandleKeyUp("Space");
        _input.AdvanceFrame();
        float dt = 0.016f;
        _player.Update(dt, _input, _grid);

        // VariableJumpCutSpeed (-70) plus one frame of gravity
        float expectedVy = PhysicsConstants.VariableJumpCutSpeed + PhysicsConstants.Gravity * dt;
        Assert.Equal(expectedVy, _player.Velocity.Y, tolerance: 1.0f);
    }
}
