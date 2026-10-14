using JumpRunShoot.Engine.Physics;

namespace JumpRunShoot.Engine.Entities;

public struct Projectile
{
    public bool IsActive;
    public bool FromPlayer;
    public Vector2f Position;
    public Vector2f Velocity;
    public int Damage;
    public float Lifetime;

    public const float MaxLifetime = 2.0f;
    public const float Width = 8.0f;
    public const float Height = 4.0f;

    public AABB BoundingBox => new(Position.X, Position.Y, Width, Height);

    public void Spawn(Vector2f pos, Vector2f vel, int damage, bool fromPlayer)
    {
        Position = pos;
        Velocity = vel;
        Damage = damage;
        FromPlayer = fromPlayer;
        Lifetime = 0.0f;
        IsActive = true;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Position.X += Velocity.X * dt;
        Position.Y += Velocity.Y * dt;
        Lifetime += dt;
        if (Lifetime >= MaxLifetime)
        {
            IsActive = false;
        }
    }
}
