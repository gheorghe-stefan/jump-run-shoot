using System;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Physics;

public struct CollisionResult
{
    public bool CollidedLeft;
    public bool CollidedRight;
    public bool CollidedCeiling;
    public bool IsGrounded;
    public bool HitHazard;
}

public static class AabbCollisionResolver
{
    private const float Epsilon = 0.001f;

    public static bool IsSolid(TileType type) => type is TileType.Solid or TileType.Border or TileType.Crate;

    public static CollisionResult MoveAndCollide(
        ref Vector2f position,
        ref Vector2f velocity,
        float boxWidth,
        float boxHeight,
        Vector2f boxOffset,
        ITileMap grid,
        float dt,
        bool dropThroughOneWay = false,
        bool noClip = false)
    {
        var result = new CollisionResult();
        int tileSize = PhysicsConstants.TileSize;

        // 1. Horizontal Movement & Resolution
        float newX = position.X + velocity.X * dt;
        if (!noClip)
        {
            float currentBoxTop = position.Y + boxOffset.Y;
            float currentBoxBottom = currentBoxTop + boxHeight;

            float testBoxLeft = newX + boxOffset.X;
            float testBoxRight = testBoxLeft + boxWidth;

            int minRow = (int)MathF.Floor(currentBoxTop / tileSize);
            int maxRow = (int)MathF.Floor((currentBoxBottom - Epsilon) / tileSize);

            if (velocity.X > 0)
            {
                int checkCol = (int)MathF.Floor((testBoxRight - Epsilon) / tileSize);
                int startCol = (int)MathF.Floor((position.X + boxOffset.X + boxWidth - Epsilon) / tileSize);

                for (int c = startCol; c <= checkCol; c++)
                {
                    for (int r = minRow; r <= maxRow; r++)
                    {
                        if (IsSolid(grid.GetTile(c, r)))
                        {
                            newX = c * tileSize - boxWidth - boxOffset.X;
                            velocity.X = 0;
                            result.CollidedRight = true;
                            goto ResolvedX;
                        }
                    }
                }
            }
            else if (velocity.X < 0)
            {
                int checkCol = (int)MathF.Floor(testBoxLeft / tileSize);
                int startCol = (int)MathF.Floor((position.X + boxOffset.X) / tileSize);

                for (int c = startCol; c >= checkCol; c--)
                {
                    for (int r = minRow; r <= maxRow; r++)
                    {
                        if (IsSolid(grid.GetTile(c, r)))
                        {
                            newX = (c + 1) * tileSize - boxOffset.X;
                            velocity.X = 0;
                            result.CollidedLeft = true;
                            goto ResolvedX;
                        }
                    }
                }
            }
        }

    ResolvedX:
        position.X = newX;

        // 2. Vertical Movement & Resolution
        float newY = position.Y + velocity.Y * dt;
        float finalBoxLeft = position.X + boxOffset.X;
        float finalBoxRight = finalBoxLeft + boxWidth;

        int minCol = (int)MathF.Floor(finalBoxLeft / tileSize);
        int maxCol = (int)MathF.Floor((finalBoxRight - Epsilon) / tileSize);

        float oldFeetY = position.Y + boxOffset.Y + boxHeight;
        float newFeetY = newY + boxOffset.Y + boxHeight;
        float newHeadY = newY + boxOffset.Y;

        if (velocity.Y >= 0)
        {
            int startRow = (int)MathF.Floor((oldFeetY - Epsilon) / tileSize);
            int checkRow = (int)MathF.Floor((newFeetY - Epsilon) / tileSize);

            for (int r = startRow; r <= checkRow; r++)
            {
                float platformTop = r * tileSize;

                for (int c = minCol; c <= maxCol; c++)
                {
                    TileType tile = grid.GetTile(c, r);

                    if (IsSolid(tile))
                    {
                        if (oldFeetY <= platformTop + 4.0f)
                        {
                            newY = platformTop - boxHeight - boxOffset.Y;
                            velocity.Y = 0;
                            result.IsGrounded = true;
                            goto ResolvedY;
                        }
                    }
                    else if (tile == TileType.OneWayPlatform && !dropThroughOneWay)
                    {
                        if (oldFeetY <= platformTop + 4.0f)
                        {
                            newY = platformTop - boxHeight - boxOffset.Y;
                            velocity.Y = 0;
                            result.IsGrounded = true;
                            goto ResolvedY;
                        }
                    }
                }
            }
        }
        else if (velocity.Y < 0)
        {
            int startRow = (int)MathF.Floor((position.Y + boxOffset.Y) / tileSize);
            int checkRow = (int)MathF.Floor(newHeadY / tileSize);

            for (int r = startRow; r >= checkRow; r--)
            {
                for (int c = minCol; c <= maxCol; c++)
                {
                    if (IsSolid(grid.GetTile(c, r)))
                    {
                        newY = (r + 1) * tileSize - boxOffset.Y;
                        velocity.Y = 0;
                        result.CollidedCeiling = true;
                        goto ResolvedY;
                    }
                }
            }
        }

    ResolvedY:
        // Prevent exceeding the world upper ceiling boundary (Row 0 / Y = 0)
        if (newY + boxOffset.Y < 0)
        {
            newY = -boxOffset.Y;
            velocity.Y = 0;
            result.CollidedCeiling = true;
        }

        position.Y = newY;

        // 3. Hazard checks (Spikes and hazards are checked regardless of NoClip)
        int hazardMinCol = (int)MathF.Floor((position.X + boxOffset.X) / tileSize);
        int hazardMaxCol = (int)MathF.Floor((position.X + boxOffset.X + boxWidth - Epsilon) / tileSize);
        int hazardMinRow = (int)MathF.Floor((position.Y + boxOffset.Y) / tileSize);
        int hazardMaxRow = (int)MathF.Floor((position.Y + boxOffset.Y + boxHeight - Epsilon) / tileSize);

        for (int r = hazardMinRow; r <= hazardMaxRow; r++)
        {
            for (int c = hazardMinCol; c <= hazardMaxCol; c++)
            {
                if (grid.GetTile(c, r) == TileType.Hazard)
                {
                    result.HitHazard = true;
                    return result;
                }
            }
        }

        return result;
    }
}
