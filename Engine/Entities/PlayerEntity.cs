using System;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Entities;

public sealed class PlayerEntity
{
    public Vector2f Position;
    public Vector2f Velocity;
    public Direction Facing = Direction.Right;
    public PlayerState State = PlayerState.Idle;
    public bool IsGrounded;

    public const float BoxWidth = PhysicsConstants.PlayerWidth;
    public const float BoxHeight = PhysicsConstants.PlayerHeight;
    public static readonly Vector2f BoxOffset = new(2.0f, 2.0f);

    public int Health = 5;
    public const int MaxHealth = 5;
    public int Score = 0;
    public float InvulnerabilityTimer = 0.0f;
    public float AnimTimer = 0.0f;

    // Powerups (Retained indefinitely until damage is taken)
    public bool HasRapidFire = false;
    public bool HasHighJumpBoots = false;
    public bool HasSpeedDash = false;
    public float ShootCooldown = 0.0f;
    public bool PendingShoot = false;

    public AABB BoundingBox => new(Position.X + BoxOffset.X, Position.Y + BoxOffset.Y, BoxWidth, BoxHeight);

    public PlayerEntity(Vector2f startPosition)
    {
        Reset(startPosition);
    }

    public void Reset(Vector2f startPosition)
    {
        Position = startPosition;
        Velocity = Vector2f.Zero;
        Facing = Direction.Right;
        State = PlayerState.Idle;
        IsGrounded = false;
        Health = MaxHealth;
        Score = 0;
        InvulnerabilityTimer = 0.0f;
        AnimTimer = 0.0f;
        HasRapidFire = false;
        HasHighJumpBoots = false;
        HasSpeedDash = false;
        ShootCooldown = 0.0f;
        PendingShoot = false;
    }

    public bool TryConsumeShoot(out Vector2f spawnPos, out Vector2f vel)
    {
        if (PendingShoot)
        {
            PendingShoot = false;
            float bulletX = Facing == Direction.Right ? Position.X + BoxWidth + 2.0f : Position.X - 6.0f;
            float bulletY = Position.Y + 10.0f;
            spawnPos = new Vector2f(bulletX, bulletY);
            float speed = PhysicsConstants.ProjectileSpeed;
            vel = new Vector2f(Facing == Direction.Right ? speed : -speed, 0);
            return true;
        }
        spawnPos = Vector2f.Zero;
        vel = Vector2f.Zero;
        return false;
    }

    public void Update(float dt, IInputSource input, ITileMap grid)
    {
        AnimTimer += dt;
        if (InvulnerabilityTimer > 0) InvulnerabilityTimer -= dt;
        if (ShootCooldown > 0) ShootCooldown -= dt;

        bool moveLeft = input.IsDown(GameAction.MoveLeft);
        bool moveRight = input.IsDown(GameAction.MoveRight);
        bool isRunning = input.IsDown(GameAction.Run);
        bool moveDown = input.IsDown(GameAction.MoveDown);
        bool jumpPressed = input.JustPressed(GameAction.Jump);
        bool jumpReleased = input.JustReleased(GameAction.Jump);
        bool shootPressed = input.JustPressed(GameAction.Shoot) || (HasRapidFire && input.IsDown(GameAction.Shoot));

        // Shooting Trigger
        if (shootPressed && ShootCooldown <= 0)
        {
            PendingShoot = true;
            ShootCooldown = HasRapidFire ? 0.12f : 0.24f;
        }

        // 1. Horizontal Locomotion (Walk / Run / SpeedDash / Friction)
        float speedMultiplier = HasSpeedDash ? 1.25f : 1.0f;
        float baseSpeed = isRunning ? PhysicsConstants.RunSpeed : PhysicsConstants.WalkSpeed;
        float targetSpeed = baseSpeed * speedMultiplier;
        float accel = PhysicsConstants.Acceleration;
        float friction = IsGrounded ? PhysicsConstants.Friction : PhysicsConstants.AirFriction;

        if (moveLeft && !moveRight)
        {
            Facing = Direction.Left;
            Velocity.X = MathF.Max(Velocity.X - accel * dt, -targetSpeed);
        }
        else if (moveRight && !moveLeft)
        {
            Facing = Direction.Right;
            Velocity.X = MathF.Min(Velocity.X + accel * dt, targetSpeed);
        }
        else
        {
            if (Velocity.X > 0)
            {
                Velocity.X = MathF.Max(0, Velocity.X - friction * dt);
            }
            else if (Velocity.X < 0)
            {
                Velocity.X = MathF.Min(0, Velocity.X + friction * dt);
            }
        }

        // 2. Vertical Locomotion & Variable Jumping
        bool dropThrough = false;

        if (jumpPressed && IsGrounded)
        {
            if (moveDown)
            {
                dropThrough = true;
                Velocity.Y = 60.0f; // slight push down
                IsGrounded = false;
            }
            else
            {
                Velocity.Y = HasHighJumpBoots ? PhysicsConstants.HighJumpSpeed : PhysicsConstants.JumpSpeed;
                IsGrounded = false;
            }
        }
        else if (jumpReleased && Velocity.Y < PhysicsConstants.VariableJumpCutSpeed)
        {
            // Variable jump height: releasing jump key cuts upward momentum in half
            Velocity.Y = PhysicsConstants.VariableJumpCutSpeed;
        }

        // Gravity
        Velocity.Y = MathF.Min(Velocity.Y + PhysicsConstants.Gravity * dt, PhysicsConstants.MaxFallSpeed);

        // 3. AABB Collision Resolution against Tile Grid
        var collision = AabbCollisionResolver.MoveAndCollide(
            ref Position,
            ref Velocity,
            BoxWidth,
            BoxHeight,
            BoxOffset,
            grid,
            dt,
            dropThroughOneWay: dropThrough);

        IsGrounded = collision.IsGrounded;

        // Hazard collision (Spikes deal 2 damage pips + knockback)
        if (collision.HitHazard && InvulnerabilityTimer <= 0)
        {
            TakeDamage(2);
            Velocity.Y = -150.0f;
        }

        // 4. Update Animation State
        if (!IsGrounded)
        {
            State = Velocity.Y < 0 ? PlayerState.Jumping : PlayerState.Falling;
        }
        else if (MathF.Abs(Velocity.X) > 10.0f)
        {
            State = isRunning ? PlayerState.Running : PlayerState.Walking;
        }
        else
        {
            State = PlayerState.Idle;
        }
    }

    public void TakeDamage(int amount)
    {
        if (InvulnerabilityTimer > 0) return;
        Health = Math.Max(0, Health - amount);
        InvulnerabilityTimer = 1.2f; // 1.2 seconds i-frame damage flicker

        // Lost on Damage: taking damage immediately clears all active powerups
        HasRapidFire = false;
        HasHighJumpBoots = false;
        HasSpeedDash = false;
    }
}
