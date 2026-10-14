using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class CameraAndBorderTests
{
    [Fact]
    public void Camera_CentersOnPlayer_EvenNearLeftBorder()
    {
        var camera = new Camera2D(viewportWidth: 320, viewportHeight: 184);
        var playerPos = new Vector2f(32, 400);

        camera.SnapTo(playerPos);

        // Player center in world: (32 + 6, 400 + 11) = (38, 411)
        // Camera top-left: (38 - 160, 411 - 92) = (-122, 319)
        Assert.Equal(-122.0f, camera.X);
        Assert.Equal(319.0f, camera.Y);

        // Screen coordinate of player should be centered in viewport
        int screenX = camera.WorldToScreenX(playerPos.X);
        int screenY = camera.WorldToScreenY(playerPos.Y);

        // Screen center is 160, player width is 12 -> centered player X is 160 - 6 = 154
        Assert.Equal(154, screenX);
        // Screen center is 92, player height is 22 -> centered player Y is 92 - 11 = 81
        Assert.Equal(81, screenY);
    }

    [Fact]
    public void TileGrid_ReturnsBorderTile_ForOutOfBoundsQueries()
    {
        var generator = new WorldGenerator("BORDER_TEST");
        var grid = new TileGrid(generator);

        // Negative column (left border)
        Assert.Equal(TileType.Border, grid.GetTile(-1, 10));
        Assert.Equal(TileType.Border, grid.GetTile(-10, 20));

        // Negative row (ceiling border)
        Assert.Equal(TileType.Border, grid.GetTile(5, -1));
        Assert.Equal(TileType.Border, grid.GetTile(15, -10));

        // Row >= ChunkHeight (bedrock border)
        Assert.Equal(TileType.Border, grid.GetTile(5, TileChunk.ChunkHeight));
        Assert.Equal(TileType.Border, grid.GetTile(12, TileChunk.ChunkHeight + 5));
    }

    [Fact]
    public void CollisionResolver_BlocksPlayerAtLeftBorder()
    {
        var generator = new WorldGenerator("BORDER_TEST");
        var grid = new TileGrid(generator);

        // Player placed against left border at X = 2, moving left fast
        var pos = new Vector2f(2, 100);
        var vel = new Vector2f(-200.0f, 0.0f);

        var collision = AabbCollisionResolver.MoveAndCollide(
            ref pos,
            ref vel,
            PlayerEntity.BoxWidth,
            PlayerEntity.BoxHeight,
            PlayerEntity.BoxOffset,
            grid,
            dt: 0.1f);

        Assert.True(collision.CollidedLeft, "Should collide with left border");
        Assert.Equal(0.0f, vel.X);
        Assert.True(pos.X + PlayerEntity.BoxOffset.X >= 0, "Player bounding box should not penetrate left border");
    }

    [Fact]
    public void CollisionResolver_BlocksPlayerAtCeilingBorder()
    {
        var generator = new WorldGenerator("BORDER_TEST");
        var grid = new TileGrid(generator);

        // Player placed near ceiling at Y = 2, jumping up fast
        var pos = new Vector2f(50, 2);
        var vel = new Vector2f(0.0f, -300.0f);

        var collision = AabbCollisionResolver.MoveAndCollide(
            ref pos,
            ref vel,
            PlayerEntity.BoxWidth,
            PlayerEntity.BoxHeight,
            PlayerEntity.BoxOffset,
            grid,
            dt: 0.1f);

        Assert.True(collision.CollidedCeiling, "Should collide with ceiling border");
        Assert.Equal(0.0f, vel.Y);
        Assert.True(pos.Y + PlayerEntity.BoxOffset.Y >= 0, "Player bounding box should not penetrate ceiling border");
    }
}
