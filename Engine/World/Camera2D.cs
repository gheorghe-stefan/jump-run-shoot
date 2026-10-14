using System;
using JumpRunShoot.Engine.Physics;

namespace JumpRunShoot.Engine.World;

public sealed class Camera2D
{
    public float X { get; private set; }
    public float Y { get; private set; }
    public int ViewportWidth { get; }
    public int ViewportHeight { get; }

    public Camera2D(int viewportWidth = 320, int viewportHeight = 184)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        X = 0;
        Y = 0;
    }

    public void Reset()
    {
        X = 0;
        Y = 0;
    }

    public void Follow(Vector2f target, float dt)
    {
        // Follow player horizontally with forward lead
        float desiredX = target.X - ViewportWidth * 0.35f;
        if (desiredX < 0) desiredX = 0;

        X += (desiredX - X) * Math.Clamp(dt * 8.0f, 0.0f, 1.0f);

        // Pin vertical tracking near bottom platforms
        float desiredY = target.Y - ViewportHeight * 0.65f;
        if (desiredY < 0) desiredY = 0;
        float maxCameraY = (TileChunk.ChunkHeight * PhysicsConstants.TileSize) - ViewportHeight;
        if (desiredY > maxCameraY) desiredY = maxCameraY;

        Y += (desiredY - Y) * Math.Clamp(dt * 6.0f, 0.0f, 1.0f);
    }

    public int WorldToScreenX(float worldX) => (int)MathF.Round(worldX - X);
    public int WorldToScreenY(float worldY) => (int)MathF.Round(worldY - Y);
}
