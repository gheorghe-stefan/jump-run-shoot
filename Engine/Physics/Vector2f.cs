using System;

namespace JumpRunShoot.Engine.Physics;

public struct Vector2f : IEquatable<Vector2f>
{
    public float X;
    public float Y;

    public static readonly Vector2f Zero = new(0, 0);

    public Vector2f(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2f operator +(Vector2f a, Vector2f b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2f operator -(Vector2f a, Vector2f b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2f operator *(Vector2f a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2f operator *(float scalar, Vector2f a) => new(a.X * scalar, a.Y * scalar);
    public static Vector2f operator /(Vector2f a, float scalar) => new(a.X / scalar, a.Y / scalar);

    public readonly bool Equals(Vector2f other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override readonly bool Equals(object? obj) => obj is Vector2f other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public static bool operator ==(Vector2f left, Vector2f right) => left.Equals(right);
    public static bool operator !=(Vector2f left, Vector2f right) => !left.Equals(right);

    public override readonly string ToString() => $"({X:F1}, {Y:F1})";
}
