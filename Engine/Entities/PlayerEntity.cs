using System;
using JumpRunShoot.Engine.Audio;
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

    public int Health = 8;
    public const int MaxHealth = 8;
    public int Score = 0;
    public float InvulnerabilityTimer = 0.0f;
    public float AnimTimer = 0.0f;
    public bool IsGodMode = false;
    public bool IsFlying = false;
    public bool IsNoClip = false;

    // Powerups (Retained indefinitely until damage is taken)
    public bool HasRapidFire = false;
    public bool HasHighJumpBoots = false;
    public bool HasSpeedDash = false;
    public float ShootCooldown = 0.0f;
    public bool PendingShoot = false;
    public bool IsClimbing = false;
    public float StepTimer = 0.0f;
    public AudioEvent PendingAudioEvents = AudioEvent.None;

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
        IsClimbing = false;
        StepTimer = 0.0f;
        PendingAudioEvents = AudioEvent.None;
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
        bool moveUp = input.IsDown(GameAction.MoveUp);
        bool jumpPressed = input.JustPressed(GameAction.Jump);
        bool jumpReleased = input.JustReleased(GameAction.Jump);
        bool shootPressed = input.JustPressed(GameAction.Shoot) || (HasRapidFire && input.IsDown(GameAction.Shoot));

        // Shooting Trigger
        if (shootPressed && ShootCooldown <= 0)
        {
            PendingShoot = true;
            ShootCooldown = HasRapidFire ? 0.12f : 0.24f;
        }

        // Ladder Climbing Detection
        int ts = PhysicsConstants.TileSize;
        int ladderMinCol = (int)MathF.Floor((Position.X + BoxOffset.X) / ts);
        int ladderMaxCol = (int)MathF.Floor((Position.X + BoxOffset.X + BoxWidth - 0.001f) / ts);
        int ladderMinRow = (int)MathF.Floor((Position.Y + BoxOffset.Y) / ts);
        int ladderMaxRow = (int)MathF.Floor((Position.Y + BoxOffset.Y + BoxHeight - 0.001f) / ts);

        bool touchesLadder = false;
        int ladderCol = ladderMinCol;
        for (int r = ladderMinRow; r <= ladderMaxRow; r++)
        {
            for (int c = ladderMinCol; c <= ladderMaxCol; c++)
            {
                if (grid.GetTile(c, r) == TileType.Ladder)
                {
                    touchesLadder = true;
                    ladderCol = c;
                    break;
                }
            }
            if (touchesLadder) break;
        }

        // If standing above a ladder and pressing down, grab the ladder
        if (!touchesLadder && moveDown && IsGrounded)
        {
            int belowRow = (int)MathF.Floor((Position.Y + BoxOffset.Y + BoxHeight + 2.0f) / ts);
            for (int c = ladderMinCol; c <= ladderMaxCol; c++)
            {
                if (grid.GetTile(c, belowRow) == TileType.Ladder)
                {
                    touchesLadder = true;
                    ladderCol = c;
                    Position.Y += 4.0f;
                    break;
                }
            }
        }

        bool dropThrough = false;

        if (IsFlying)
        {
            IsClimbing = false;
            float flightSpeed = (isRunning ? PhysicsConstants.RunSpeed * 1.25f : PhysicsConstants.WalkSpeed * 1.25f) * (HasSpeedDash ? 1.25f : 1.0f);

            // 1. Flight Horizontal Locomotion (WASD: A / D or Arrow Keys)
            if (moveLeft && !moveRight)
            {
                Facing = Direction.Left;
                Velocity.X = -flightSpeed;
            }
            else if (moveRight && !moveLeft)
            {
                Facing = Direction.Right;
                Velocity.X = flightSpeed;
            }
            else
            {
                Velocity.X = 0;
            }

            // 2. Flight Vertical Locomotion (WASD: W / S, Arrow Keys, or Jump Key)
            bool up = moveUp || input.IsDown(GameAction.Jump);
            if (up && !moveDown)
            {
                Velocity.Y = -flightSpeed;
            }
            else if (moveDown && !up)
            {
                Velocity.Y = flightSpeed;
                dropThrough = true;
            }
            else
            {
                Velocity.Y = 0; // Hover in place: zero vertical movement, no gravity
            }
        }
        else if (touchesLadder && (moveUp || (moveDown && !IsGrounded) || (IsClimbing && !IsGrounded)))
        {
            if (jumpPressed)
            {
                // Leap off ladder
                Velocity.Y = HasHighJumpBoots ? PhysicsConstants.HighJumpSpeed : PhysicsConstants.JumpSpeed;
                IsClimbing = false;
                IsGrounded = false;
                PendingAudioEvents |= AudioEvent.PlayerJump;
            }
            else
            {
                IsClimbing = true;
                IsGrounded = false;
                dropThrough = true;

                if (moveUp)
                {
                    Velocity.Y = -PhysicsConstants.ClimbSpeed;
                }
                else if (moveDown)
                {
                    Velocity.Y = PhysicsConstants.ClimbSpeed;
                }
                else
                {
                    Velocity.Y = 0; // Hold position on ladder
                }

                if (moveLeft && !moveRight)
                {
                    Facing = Direction.Left;
                    Velocity.X = -PhysicsConstants.WalkSpeed * 0.6f;
                }
                else if (moveRight && !moveLeft)
                {
                    Facing = Direction.Right;
                    Velocity.X = PhysicsConstants.WalkSpeed * 0.6f;
                }
                else
                {
                    Velocity.X = 0;
                    if (moveUp || moveDown)
                    {
                        Position.X = ladderCol * ts;
                    }
                }
            }
        }
        else
        {
            IsClimbing = false;

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
                    PendingAudioEvents |= AudioEvent.PlayerJump;
                }
            }
            else if (jumpReleased && Velocity.Y < PhysicsConstants.VariableJumpCutSpeed)
            {
                // Variable jump height: releasing jump key cuts upward momentum in half
                Velocity.Y = PhysicsConstants.VariableJumpCutSpeed;
            }

            // Gravity (only when not climbing)
            Velocity.Y = MathF.Min(Velocity.Y + PhysicsConstants.Gravity * dt, PhysicsConstants.MaxFallSpeed);
        }

        // 3. AABB Collision Resolution against Tile Grid
        var collision = AabbCollisionResolver.MoveAndCollide(
            ref Position,
            ref Velocity,
            BoxWidth,
            BoxHeight,
            BoxOffset,
            grid,
            dt,
            dropThroughOneWay: dropThrough,
            noClip: IsNoClip);

        IsGrounded = collision.IsGrounded;
        if (IsGrounded) IsClimbing = false;

        // Hazard collision (Spikes deal 2 damage pips + knockback)
        if (collision.HitHazard && InvulnerabilityTimer <= 0)
        {
            TakeDamage(2);
            Velocity.Y = -150.0f;
            IsClimbing = false;
            PendingAudioEvents |= AudioEvent.PlayerDamage;
        }

        // 4. Update Animation State & Footstep Audio
        if (IsClimbing)
        {
            State = PlayerState.Climbing;
            StepTimer = 0.0f;
        }
        else if (IsFlying)
        {
            State = (Velocity.X != 0 || Velocity.Y != 0)
                ? (IsGrounded ? PlayerState.Walking : PlayerState.Jumping)
                : (IsGrounded ? PlayerState.Idle : PlayerState.Jumping);
            StepTimer = 0.0f;
        }
        else if (!IsGrounded)
        {
            State = Velocity.Y < 0 ? PlayerState.Jumping : PlayerState.Falling;
            StepTimer = 0.0f;
        }
        else if (MathF.Abs(Velocity.X) > 10.0f)
        {
            State = isRunning ? PlayerState.Running : PlayerState.Walking;

            // Footstep audio: running accelerates cadence by ~50% (0.18s vs 0.28s)
            float stepInterval = isRunning ? 0.18f : 0.28f;
            StepTimer += dt;
            if (StepTimer >= stepInterval)
            {
                StepTimer = 0.0f;
                PendingAudioEvents |= isRunning ? AudioEvent.PlayerRunStep : AudioEvent.PlayerStep;
            }
        }
        else
        {
            State = PlayerState.Idle;
            StepTimer = 0.0f;
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsGodMode || InvulnerabilityTimer > 0) return;
        Health = Math.Max(0, Health - amount);
        InvulnerabilityTimer = 1.2f; // 1.2 seconds i-frame damage flicker
        PendingAudioEvents |= AudioEvent.PlayerDamage;

        // Lost on Damage: taking damage immediately clears all active powerups
        HasRapidFire = false;
        HasHighJumpBoots = false;
        HasSpeedDash = false;
    }
}
