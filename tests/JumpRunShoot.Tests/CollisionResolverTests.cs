using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class CollisionResolverTests
{
    private readonly TileGrid _grid;

    public CollisionResolverTests()
    {
        var generator = new WorldGenerator("TEST_SEED");
        _grid = new TileGrid(generator);
        // Clear procedural chunks to create controlled test layout
        _grid.Reset();
    }

    [Fact]
    public void MoveAndCollide_HorizontalSolidWall_StopsAtBoundaryAndZeroesVelocity()
    {
        // Place solid wall at column 5 (X = 80)
        _grid.SetTile(5, 5, TileType.Solid);

        // Position entity at X = 60, moving right at 100 px/s with width 12
        var pos = new Vector2f(60, 5 * 16);
        var vel = new Vector2f(100, 0);
        float dt = 0.5f; // would travel 50 px to X=110 without collision

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 16, boxOffset: Vector2f.Zero,
            _grid, dt);

        Assert.True(result.CollidedRight);
        Assert.Equal(0, vel.X);
        // Wall left edge is 5 * 16 = 80. Entity right edge should be at 80, so entity X = 80 - 12 = 68.
        Assert.Equal(68, pos.X);
    }

    [Fact]
    public void MoveAndCollide_FallingOntoSolidFloor_LandsAndGrounds()
    {
        // Floor at row 10 (Y = 160)
        _grid.SetTile(2, 10, TileType.Solid);

        // Position entity at Y = 130, falling downwards at 200 px/s with height 20
        var pos = new Vector2f(2 * 16, 130);
        var vel = new Vector2f(0, 200);
        float dt = 0.2f; // would fall to Y = 170

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 20, boxOffset: Vector2f.Zero,
            _grid, dt);

        Assert.True(result.IsGrounded);
        Assert.Equal(0, vel.Y);
        // Floor top is 160. Entity feet at 160, so entity Y = 160 - 20 = 140.
        Assert.Equal(140, pos.Y);
    }

    [Fact]
    public void MoveAndCollide_JumpingUpThroughOneWayPlatform_DoesNotBlock()
    {
        // One-way platform at row 8 (Y = 128)
        _grid.SetTile(2, 8, TileType.OneWayPlatform);

        // Position entity at Y = 140 (below platform), jumping UP at -200 px/s
        var pos = new Vector2f(2 * 16, 140);
        var vel = new Vector2f(0, -200);
        float dt = 0.1f; // travels up 20 px to Y = 120

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 16, boxOffset: Vector2f.Zero,
            _grid, dt);

        Assert.False(result.IsGrounded);
        Assert.False(result.CollidedCeiling);
        Assert.Equal(-200, vel.Y);
        Assert.Equal(120, pos.Y);
    }

    [Fact]
    public void MoveAndCollide_FallingOntoOneWayPlatform_LandsAndGrounds()
    {
        // One-way platform at row 8 (Y = 128)
        _grid.SetTile(2, 8, TileType.OneWayPlatform);

        // Position entity at Y = 100 (above platform), falling downwards
        var pos = new Vector2f(2 * 16, 100);
        var vel = new Vector2f(0, 200);
        float dt = 0.1f;

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 16, boxOffset: Vector2f.Zero,
            _grid, dt);

        Assert.True(result.IsGrounded);
        Assert.Equal(0, vel.Y);
        // Platform top is 128, entity feet at 128, entity Y = 128 - 16 = 112
        Assert.Equal(112, pos.Y);
    }

    [Fact]
    public void MoveAndCollide_DropThroughOneWayPlatform_PassesThroughWhenFlagged()
    {
        // One-way platform at row 8 (Y = 128)
        _grid.SetTile(2, 8, TileType.OneWayPlatform);

        // Position entity resting right on top of platform at Y = 112 (feet at 128)
        var pos = new Vector2f(2 * 16, 112);
        var vel = new Vector2f(0, 50); // downward nudge
        float dt = 0.1f;

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 16, boxOffset: Vector2f.Zero,
            _grid, dt, dropThroughOneWay: true);

        // When dropThroughOneWay is true, should NOT land on one-way platform
        Assert.False(result.IsGrounded);
        Assert.True(pos.Y > 112);
    }

    [Fact]
    public void MoveAndCollide_TouchingHazardSpikes_ReportsHazardHit()
    {
        _grid.SetTile(4, 10, TileType.Hazard);

        var pos = new Vector2f(4 * 16, 10 * 16);
        var vel = Vector2f.Zero;

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 16, boxOffset: Vector2f.Zero,
            _grid, 0.016f);

        Assert.True(result.HitHazard);
    }
}
