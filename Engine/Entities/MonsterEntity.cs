using System;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Entities;

public enum MonsterType : byte
{
    PatrolDroid = 0,
    HoverDrone = 1,
    CeilingTurret = 2,
    MechaHound = 3,
    HeavyGuard = 4
}

public sealed class MonsterEntity
{
    public int Id;
    public MonsterType Type;
    public Vector2f Position;
    public Vector2f Velocity;
    public Direction Facing = Direction.Left;
    public int Health;
    public int MaxHealth;
    public bool IsActive;
    public float ActionTimer;
    public float AnimTimer;
    public float HitFlashTimer;
    public float Width;
    public float Height;
    public float PlatformY;
    public int ScoreReward;
    public int TouchDamage;
    public bool IsAlerted;
    public float TurnCooldown;

    public AABB BoundingBox => new(Position.X, Position.Y, Width, Height);

    public void Spawn(int id, MonsterType type, Vector2f pos, float platformY = 0)
    {
        Id = id;
        Type = type;
        PlatformY = platformY;
        IsActive = true;
        IsAlerted = false;
        TurnCooldown = 0.0f;
        AnimTimer = 0.0f;
        HitFlashTimer = 0.0f;
        Facing = Direction.Left;

        switch (type)
        {
            case MonsterType.PatrolDroid:
                Width = 14.0f;
                Height = 14.0f;
                Health = MaxHealth = 2;
                Velocity = new Vector2f(-35.0f, 0);
                ScoreReward = 100;
                TouchDamage = 1;
                ActionTimer = 0.0f;
                break;

            case MonsterType.HoverDrone:
                Width = 14.0f;
                Height = 12.0f;
                Health = MaxHealth = 2;
                Velocity = new Vector2f(-40.0f, 0);
                ScoreReward = 150;
                TouchDamage = 1;
                ActionTimer = 2.0f;
                break;

            case MonsterType.CeilingTurret:
                Width = 14.0f;
                Height = 12.0f;
                Health = MaxHealth = 3;
                Velocity = Vector2f.Zero;
                ScoreReward = 200;
                TouchDamage = 1;
                ActionTimer = 1.8f;
                break;

            case MonsterType.MechaHound:
                Width = 24.0f;
                Height = 14.0f;
                Health = MaxHealth = 3;
                Velocity = new Vector2f(-70.0f, 0);
                ScoreReward = 250;
                TouchDamage = 1;
                ActionTimer = 0.0f;
                break;

            case MonsterType.HeavyGuard:
                Width = 14.0f;
                Height = 26.0f;
                Health = MaxHealth = 6;
                Velocity = new Vector2f(-22.0f, 0);
                ScoreReward = 400;
                TouchDamage = 2;
                ActionTimer = 2.5f;
                break;
        }

        Position = pos;
        if (PlatformY > 0 && Type is MonsterType.PatrolDroid or MonsterType.MechaHound or MonsterType.HeavyGuard)
        {
            Position.Y = PlatformY - Height;
        }
    }

    public void Update(float dt, ITileMap grid, Vector2f playerPos, out bool wantsShoot, out Vector2f shootPos, out Vector2f shootVel, out int shootDamage)
    {
        wantsShoot = false;
        shootPos = Vector2f.Zero;
        shootVel = Vector2f.Zero;
        shootDamage = 1;

        if (!IsActive) return;

        AnimTimer += dt;
        if (HitFlashTimer > 0) HitFlashTimer -= dt;
        if (TurnCooldown > 0) TurnCooldown -= dt;

        int ts = PhysicsConstants.TileSize;

        switch (Type)
        {
            case MonsterType.PatrolDroid:
            case MonsterType.MechaHound:
            case MonsterType.HeavyGuard:
            {
                // Ensure feet strictly sit on top of the platform (never sink into or clip the platform)
                if (PlatformY > 0)
                {
                    Position.Y = PlatformY - Height;
                }

                // Edge and wall detection (sensor placed just in front of leading edge)
                float checkX = Facing == Direction.Right ? Position.X + Width + 2.0f : Position.X - 2.0f;
                int checkCol = (int)MathF.Floor(checkX / ts);
                int bodyRow = (int)MathF.Floor((Position.Y + Height * 0.5f) / ts);
                int footRow = (int)MathF.Floor((Position.Y + Height + 2.0f) / ts);

                TileType bodyTile = grid.GetTile(checkCol, bodyRow);
                TileType groundTile = grid.GetTile(checkCol, footRow);

                bool hitWall = bodyTile is TileType.Solid or TileType.Border;
                bool hitLedge = groundTile is TileType.Empty or TileType.Hazard or TileType.Ladder;

                if ((hitWall || hitLedge) && TurnCooldown <= 0f)
                {
                    Facing = Facing == Direction.Right ? Direction.Left : Direction.Right;
                    TurnCooldown = 0.35f;
                    float baseSpeed = Type switch
                    {
                        MonsterType.MechaHound => IsAlerted ? 125.0f : 70.0f,
                        MonsterType.HeavyGuard => 22.0f,
                        _ => 35.0f
                    };
                    Velocity.X = Facing == Direction.Right ? baseSpeed : -baseSpeed;
                    Position.X += Facing == Direction.Right ? 1.5f : -1.5f;
                }

                // MechaHound alert / vision trigger: if player is ahead on the same plane, alert and sprint
                if (Type == MonsterType.MechaHound && !IsAlerted)
                {
                    float dx = playerPos.X - Position.X;
                    float dy = MathF.Abs(playerPos.Y - Position.Y);
                    if (dy < 36.0f && ((Facing == Direction.Right && dx > 0 && dx < 120.0f) || (Facing == Direction.Left && dx < 0 && dx > -120.0f)))
                    {
                        IsAlerted = true;
                        Velocity.X = Facing == Direction.Right ? 125.0f : -125.0f;
                    }
                }

                Position.X += Velocity.X * dt;

                // Heavy guard periodic shooting
                if (Type == MonsterType.HeavyGuard)
                {
                    ActionTimer -= dt;
                    if (ActionTimer <= 0)
                    {
                        ActionTimer = 2.5f;
                        wantsShoot = true;
                        shootDamage = 2;
                        shootPos = new Vector2f(Facing == Direction.Right ? Position.X + Width + 2 : Position.X - 8, Position.Y + 6);
                        shootVel = new Vector2f(Facing == Direction.Right ? 180.0f : -180.0f, 0);
                    }
                }
                break;
            }

            case MonsterType.HoverDrone:
            {
                // Horizontal flight with wall bounce
                float checkX = Facing == Direction.Right ? Position.X + Width + 2.0f : Position.X - 2.0f;
                int checkCol = (int)MathF.Floor(checkX / ts);
                int bodyRow = (int)MathF.Floor((Position.Y + Height * 0.5f) / ts);
                TileType bodyTile = grid.GetTile(checkCol, bodyRow);

                if (bodyTile is TileType.Solid or TileType.Border)
                {
                    Facing = Facing == Direction.Right ? Direction.Left : Direction.Right;
                    Velocity.X = Facing == Direction.Right ? 40.0f : -40.0f;
                }

                Position.X += Velocity.X * dt;
                Position.Y += MathF.Sin(AnimTimer * 3.5f) * 0.35f;

                ActionTimer -= dt;
                if (ActionTimer <= 0)
                {
                    ActionTimer = 3.0f;
                    wantsShoot = true;
                    shootDamage = 1;
                    shootPos = new Vector2f(Facing == Direction.Right ? Position.X + Width + 2 : Position.X - 6, Position.Y + 4);
                    shootVel = new Vector2f(Facing == Direction.Right ? 200.0f : -200.0f, 0);
                }
                break;
            }

            case MonsterType.CeilingTurret:
            {
                // Stationary ceiling turret: faces toward player X
                Facing = playerPos.X > Position.X ? Direction.Right : Direction.Left;

                ActionTimer -= dt;
                if (ActionTimer <= 0)
                {
                    ActionTimer = 2.2f;
                    wantsShoot = true;
                    shootDamage = 1;
                    shootPos = new Vector2f(Position.X + Width * 0.5f - 2.0f, Position.Y + Height + 2.0f);
                    shootVel = new Vector2f(0, 160.0f); // shoots downward
                }
                break;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        HitFlashTimer = 0.2f;
        if (Health <= 0)
        {
            IsActive = false;
        }
    }

    public void TakeDamage(int amount, Vector2f playerPos)
    {
        TakeDamage(amount);
        if (!IsActive) return;

        // Turn towards the player when shot
        Facing = playerPos.X > Position.X ? Direction.Right : Direction.Left;

        if (Type == MonsterType.MechaHound)
        {
            IsAlerted = true;
            const float sprintSpeed = 125.0f;
            Velocity.X = Facing == Direction.Right ? sprintSpeed : -sprintSpeed;
        }
        else if (Type is MonsterType.PatrolDroid or MonsterType.HeavyGuard)
        {
            float speed = Type == MonsterType.HeavyGuard ? 26.0f : 45.0f;
            Velocity.X = Facing == Direction.Right ? speed : -speed;
            if (Type == MonsterType.HeavyGuard)
            {
                // Immediate retaliation: ready blaster to return fire
                ActionTimer = MathF.Min(ActionTimer, 0.4f);
            }
        }
        else if (Type == MonsterType.HoverDrone)
        {
            Velocity.X = Facing == Direction.Right ? 55.0f : -55.0f;
            ActionTimer = MathF.Min(ActionTimer, 0.5f);
        }
    }
}
