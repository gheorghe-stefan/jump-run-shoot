using JumpRunShoot.Engine.Physics;

namespace JumpRunShoot.Engine.Entities;

public enum PickupType : byte
{
    BlueGem = 0,
    RedRuby = 1,
    GoldCartridge = 2,
    MedKit = 3,
    RapidFire = 4,
    HighJumpBoots = 5,
    SpeedDash = 6,
    Invulnerability = 7
}

public struct PickupItem
{
    public bool IsActive;
    public PickupType Type;
    public Vector2f Position;
    public float AnimTimer;

    public const float Width = 12.0f;
    public const float Height = 12.0f;

    public AABB BoundingBox => new(Position.X, Position.Y, Width, Height);

    public void Spawn(PickupType type, Vector2f pos)
    {
        Type = type;
        Position = pos;
        AnimTimer = 0.0f;
        IsActive = true;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        AnimTimer += dt;
    }
}
