using JumpRunShoot.Engine.Physics;

namespace JumpRunShoot.Engine.Entities;

/// <summary>
/// Represents an active death animation effect spawned when a monster is defeated.
/// Retains the monster type, bounds, facing direction, and animation timer.
/// </summary>
public struct DeathEffect
{
    public bool IsActive;
    public MonsterType MonsterType;
    public Vector2f Position;
    public float Width;
    public float Height;
    public Direction Facing;
    public float Timer;
    public float Duration;
}
