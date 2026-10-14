using System;

namespace JumpRunShoot.Engine.Physics;

public struct AABB : IEquatable<AABB>
{
    public float X;
    public float Y;
    public float Width;
    public float Height;

    public readonly float Left => X;
    public readonly float Right => X + Width;
    public readonly float Top => Y;
    public readonly float Bottom => Y + Height;

    public AABB(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public readonly bool Overlaps(in AABB other)
    {
        return Left < other.Right &&
               Right > other.Left &&
               Top < other.Bottom &&
               Bottom > other.Top;
    }

    public readonly bool Contains(Vector2f point)
    {
        return point.X >= Left && point.X <= Right &&
               point.Y >= Top && point.Y <= Bottom;
    }

    public readonly bool Equals(AABB other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) &&
               Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    public override readonly bool Equals(object? obj) => obj is AABB other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
    public static bool operator ==(AABB left, AABB right) => left.Equals(right);
    public static bool operator !=(AABB left, AABB right) => !left.Equals(right);
}
