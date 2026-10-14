namespace JumpRunShoot.Engine.Physics;

public static class PhysicsConstants
{
    public const int TileSize = 16;

    // Movement speeds in pixels per second
    public const float WalkSpeed = 85.0f;
    public const float RunSpeed = 155.0f;
    public const float Acceleration = 650.0f;
    public const float Friction = 800.0f;
    public const float AirFriction = 350.0f;

    // Jump & Gravity in pixels per second
    public const float JumpSpeed = -240.0f;
    public const float HighJumpSpeed = -300.0f;
    public const float VariableJumpCutSpeed = -70.0f;
    public const float Gravity = 620.0f;
    public const float MaxFallSpeed = 340.0f;

    // Projectile speed
    public const float ProjectileSpeed = 260.0f;

    // Player default collision box
    public const float PlayerWidth = 12.0f;
    public const float PlayerHeight = 22.0f;
}
