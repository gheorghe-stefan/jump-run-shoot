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

    [Fact]
    public void MoveAndCollide_WithNoClip_WalksThroughSolidObstacleWhileStayingGroundedOnFloor()
    {
        // Continuous solid floor at TileChunk.FloorRow (28, Y = 448)
        int floorRow = TileChunk.FloorRow;
        _grid.SetTile(4, floorRow, TileType.Solid);
        _grid.SetTile(5, floorRow, TileType.Solid);
        _grid.SetTile(6, floorRow, TileType.Solid);

        // Solid obstacle / wall above floor at column 5, rows 26 and 27
        _grid.SetTile(5, floorRow - 2, TileType.Solid);
        _grid.SetTile(5, floorRow - 1, TileType.Solid);

        // Position entity at X = 4 * 16 = 64, standing on floor at Y = 448 - 20 = 428
        float floorY = floorRow * 16 - 20;
        var pos = new Vector2f(64, floorY);
        var vel = new Vector2f(100, 10); // moving right, gravity slightly down
        float dt = 0.3f; // moves 30px to X = 94 (across col 5 which is X = 80..96)

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 20, boxOffset: Vector2f.Zero,
            _grid, dt, noClip: true);

        // Should pass directly through col 5 wall obstacle without collision stopping it
        Assert.False(result.CollidedRight);
        Assert.Equal(94, pos.X);
        // And stay grounded on the floor
        Assert.True(result.IsGrounded);
        Assert.Equal(floorY, pos.Y);
    }

    [Fact]
    public void MoveAndCollide_WithNoClip_DoesNotFallThroughSolidFloor()
    {
        // Floor at TileChunk.FloorRow (28, Y = 448)
        int floorRow = TileChunk.FloorRow;
        _grid.SetTile(2, floorRow, TileType.Solid);

        // Entity falling downwards
        var pos = new Vector2f(2 * 16, floorRow * 16 - 30);
        var vel = new Vector2f(0, 200);
        float dt = 0.2f;

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 20, boxOffset: Vector2f.Zero,
            _grid, dt, noClip: true);

        Assert.True(result.IsGrounded);
        Assert.Equal(0, vel.Y);
        Assert.Equal(floorRow * 16 - 20, pos.Y);
    }

    [Fact]
    public void MoveAndCollide_WithNoClip_BlocksAtSolidCeiling_CannotEnterCeiling()
    {
        // Solid ceiling at row 0 (Y = 0..16)
        _grid.SetTile(2, 0, TileType.Solid);

        // Entity at Y = 24, moving upward at -200 px/s
        var pos = new Vector2f(2 * 16, 24);
        var vel = new Vector2f(0, -200);
        float dt = 0.2f; // travels up towards ceiling

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 20, boxOffset: Vector2f.Zero,
            _grid, dt, noClip: true);

        // Ceiling must block upward movement even with NoClip
        Assert.True(result.CollidedCeiling);
        Assert.Equal(0, vel.Y);
        // Ceiling bottom is (0 + 1) * 16 = 16. Entity Y stops at 16.
        Assert.Equal(16, pos.Y);
    }

    [Fact]
    public void MoveAndCollide_FloorAndCeilingCollision_BehavesIdenticallyWithOrWithoutNoClip()
    {
        // Floor at TileChunk.FloorRow
        int floorRow = TileChunk.FloorRow;
        _grid.SetTile(2, floorRow, TileType.Solid);

        var pos1 = new Vector2f(2 * 16, floorRow * 16 - 30);
        var vel1 = new Vector2f(0, 200);
        var resWithout = AabbCollisionResolver.MoveAndCollide(ref pos1, ref vel1, 12, 20, Vector2f.Zero, _grid, 0.2f, noClip: false);

        var pos2 = new Vector2f(2 * 16, floorRow * 16 - 30);
        var vel2 = new Vector2f(0, 200);
        var resWith = AabbCollisionResolver.MoveAndCollide(ref pos2, ref vel2, 12, 20, Vector2f.Zero, _grid, 0.2f, noClip: true);

        Assert.Equal(resWithout.IsGrounded, resWith.IsGrounded);
        Assert.Equal(pos1.Y, pos2.Y);
        Assert.Equal(vel1.Y, vel2.Y);
    }

    [Fact]
    public void MoveAndCollide_WithNoClip_CanMoveUpThroughBrickInMiddleOfMap()
    {
        // Solid brick in the middle of the map at row 16 (Y = 256..272)
        _grid.SetTile(2, 16, TileType.Solid);

        // Entity at Y = 280 (below brick), moving upward at -200 px/s
        var pos = new Vector2f(2 * 16, 280);
        var vel = new Vector2f(0, -200);
        float dt = 0.2f; // travels up 40px to Y = 240 (above brick)

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 20, boxOffset: Vector2f.Zero,
            _grid, dt, noClip: true);

        // Brick in middle of map must be go-through when NoClip is active
        Assert.False(result.CollidedCeiling);
        Assert.Equal(-200, vel.Y);
        Assert.Equal(240, pos.Y);
    }

    [Fact]
    public void MoveAndCollide_WithNoClip_CanMoveDownThroughBrickInMiddleOfMap()
    {
        // Solid brick in the middle of the map at row 16 (Y = 256..272)
        _grid.SetTile(2, 16, TileType.Solid);

        // Entity at Y = 230 (above brick), moving downward at 200 px/s
        var pos = new Vector2f(2 * 16, 230);
        var vel = new Vector2f(0, 200);
        float dt = 0.2f; // travels down 40px to Y = 270 (below brick)

        var result = AabbCollisionResolver.MoveAndCollide(
            ref pos, ref vel, boxWidth: 12, boxHeight: 20, boxOffset: Vector2f.Zero,
            _grid, dt, noClip: true);

        // Brick in middle of map must not stop downward movement when NoClip is active
        Assert.False(result.IsGrounded);
        Assert.Equal(200, vel.Y);
        Assert.Equal(270, pos.Y);
    }
}
