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
    }

    public void Update(float dt, InputManager input, TileGrid grid)
    {
        AnimTimer += dt;
        if (InvulnerabilityTimer > 0) InvulnerabilityTimer -= dt;

        bool moveLeft = input.IsDown(GameAction.MoveLeft);
        bool moveRight = input.IsDown(GameAction.MoveRight);
        bool isRunning = input.IsDown(GameAction.Run);
        bool moveDown = input.IsDown(GameAction.MoveDown);
        bool jumpPressed = input.JustPressed(GameAction.Jump);
        bool jumpReleased = input.JustReleased(GameAction.Jump);

        // 1. Horizontal Locomotion (Walk / Run / Friction)
        float targetSpeed = isRunning ? PhysicsConstants.RunSpeed : PhysicsConstants.WalkSpeed;
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
                Velocity.Y = PhysicsConstants.JumpSpeed;
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
    }
}
