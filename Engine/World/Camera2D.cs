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

    public void SnapTo(Vector2f target)
    {
        X = target.X + (PhysicsConstants.PlayerWidth * 0.5f) - (ViewportWidth * 0.5f);
        Y = target.Y + (PhysicsConstants.PlayerHeight * 0.5f) - (ViewportHeight * 0.5f);
    }

    public void Follow(Vector2f target, float dt)
    {
        // Camera keeps player centered in both X and Y
        float desiredX = target.X + (PhysicsConstants.PlayerWidth * 0.5f) - (ViewportWidth * 0.5f);
        float desiredY = target.Y + (PhysicsConstants.PlayerHeight * 0.5f) - (ViewportHeight * 0.5f);

        float factor = Math.Clamp(dt * 10.0f, 0.0f, 1.0f);
        X += (desiredX - X) * factor;
        Y += (desiredY - Y) * factor;
    }

    public int WorldToScreenX(float worldX) => (int)MathF.Round(worldX - X);
    public int WorldToScreenY(float worldY) => (int)MathF.Round(worldY - Y);
}
