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

    /// <summary>
    /// Calculates the defeat score reward quadratically based on the monster's maximum health.
    /// Formula: 50 * MaxHealth^2 (e.g. 2 HP -> 200, 3 HP -> 450, 6 HP -> 1800).
    /// </summary>
    public static int CalculateScoreReward(int maxHealth) => 50 * maxHealth * maxHealth;

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
                ScoreReward = CalculateScoreReward(MaxHealth);
                TouchDamage = 1;
                ActionTimer = 0.0f;
                break;

            case MonsterType.HoverDrone:
                Width = 14.0f;
                Height = 12.0f;
                Health = MaxHealth = 2;
                Velocity = new Vector2f(-40.0f, 0);
                ScoreReward = CalculateScoreReward(MaxHealth);
                TouchDamage = 1;
                ActionTimer = 2.0f;
                break;

            case MonsterType.CeilingTurret:
                Width = 14.0f;
                Height = 12.0f;
                Health = MaxHealth = 3;
                Velocity = Vector2f.Zero;
                ScoreReward = CalculateScoreReward(MaxHealth);
                TouchDamage = 1;
                ActionTimer = 1.8f;
                break;

            case MonsterType.MechaHound:
                Width = 24.0f;
                Height = 14.0f;
                Health = MaxHealth = 3;
                Velocity = new Vector2f(-70.0f, 0);
                ScoreReward = CalculateScoreReward(MaxHealth);
                TouchDamage = 1;
                ActionTimer = 0.0f;
                break;

            case MonsterType.HeavyGuard:
                Width = 14.0f;
                Height = 26.0f;
                Health = MaxHealth = 6;
                Velocity = new Vector2f(-22.0f, 0);
                ScoreReward = CalculateScoreReward(MaxHealth);
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

                // Edge and wall detection (sensor placed just in front of leading edge across full monster height)
                float checkX = Facing == Direction.Right ? Position.X + Width + 2.0f : Position.X - 2.0f;
                int checkCol = (int)MathF.Floor(checkX / ts);
                int topRow = (int)MathF.Floor((Position.Y + 1.0f) / ts);
                int bottomRow = (int)MathF.Floor((Position.Y + Height - 1.0f) / ts);
                int footRow = (int)MathF.Floor((Position.Y + Height + 2.0f) / ts);

                bool hitWall = false;
                for (int r = topRow; r <= bottomRow; r++)
                {
                    if (grid.GetTile(checkCol, r) is TileType.Solid or TileType.Border or TileType.Crate)
                    {
                        hitWall = true;
                        break;
                    }
                }

                TileType groundTile = grid.GetTile(checkCol, footRow);
                bool hitLedge = groundTile is TileType.Empty or TileType.Hazard or TileType.Ladder;

                float baseSpeed = Type switch
                {
                    MonsterType.MechaHound => IsAlerted ? 125.0f : 70.0f,
                    MonsterType.HeavyGuard => 22.0f,
                    _ => 35.0f
                };

                if ((hitWall || hitLedge) && TurnCooldown <= 0f)
                {
                    Facing = Facing == Direction.Right ? Direction.Left : Direction.Right;
                    TurnCooldown = 0.25f;
                    Velocity.X = Facing == Direction.Right ? baseSpeed : -baseSpeed;
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

                // Hard solid-wall collision clamping (never ignored during TurnCooldown!)
                float nextGroundX = Position.X + Velocity.X * dt;
                if (TryFindBlockingWallCol(grid, nextGroundX, Position.Y + 1.0f, Width, Height - 2.0f, Velocity.X, ts, out int wallCol))
                {
                    Position.X = Velocity.X > 0
                        ? wallCol * ts - Width - 0.5f
                        : (wallCol + 1) * ts + 0.5f;
                    Facing = Facing == Direction.Right ? Direction.Left : Direction.Right;
                    Velocity.X = Facing == Direction.Right ? baseSpeed : -baseSpeed;
                    TurnCooldown = 0.20f;
                }
                else
                {
                    Position.X = nextGroundX;
                }

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
                // Horizontal flight with full-height solid-wall collision clamping (never clips into solid blocks)
                float speed = IsAlerted ? 55.0f : 40.0f;
                float checkX = Facing == Direction.Right ? Position.X + Width + 2.0f : Position.X - 2.0f;
                int checkCol = (int)MathF.Floor(checkX / ts);
                int topCheckRow = (int)MathF.Floor((Position.Y + 0.5f) / ts);
                int bottomCheckRow = (int)MathF.Floor((Position.Y + Height - 0.5f) / ts);

                bool wallAhead = false;
                for (int r = topCheckRow; r <= bottomCheckRow; r++)
                {
                    if (grid.GetTile(checkCol, r) is TileType.Solid or TileType.Border or TileType.Crate)
                    {
                        wallAhead = true;
                        break;
                    }
                }

                if (wallAhead)
                {
                    Facing = Facing == Direction.Right ? Direction.Left : Direction.Right;
                    Velocity.X = Facing == Direction.Right ? speed : -speed;
                }

                float nextHoverX = Position.X + Velocity.X * dt;
                if (TryFindBlockingWallCol(grid, nextHoverX, Position.Y + 0.5f, Width, Height - 1.0f, Velocity.X, ts, out int hoverWallCol))
                {
                    Position.X = Velocity.X > 0
                        ? hoverWallCol * ts - Width - 0.5f
                        : (hoverWallCol + 1) * ts + 0.5f;
                    Facing = Facing == Direction.Right ? Direction.Left : Direction.Right;
                    Velocity.X = Facing == Direction.Right ? speed : -speed;
                }
                else
                {
                    Position.X = nextHoverX;
                }

                // Frame-rate-independent hover oscillation bounded cleanly between ceiling and floor/platforms
                float nextY = Position.Y - MathF.Sin(AnimTimer * 3.5f) * 14.0f * dt;
                int bodyMinCol = (int)MathF.Floor((Position.X + 0.5f) / ts);
                int bodyMaxCol = (int)MathF.Floor((Position.X + Width - 0.5f) / ts);

                float minSafeY = ts;
                float maxSafeY = (TileChunk.ChunkHeight - 2) * ts;
                int feetRow = (int)MathF.Floor((nextY + Height + 1.0f) / ts);
                int headRow = (int)MathF.Floor((nextY - 1.0f) / ts);
                for (int c = bodyMinCol; c <= bodyMaxCol; c++)
                {
                    TileType belowTile = grid.GetTile(c, feetRow);
                    if (belowTile is TileType.Solid or TileType.Border or TileType.Crate or TileType.OneWayPlatform)
                    {
                        maxSafeY = MathF.Min(maxSafeY, feetRow * ts - Height - 2.0f);
                    }

                    TileType aboveTile = grid.GetTile(c, headRow);
                    if (aboveTile is TileType.Solid or TileType.Border or TileType.Crate)
                    {
                        minSafeY = MathF.Max(minSafeY, (headRow + 1) * ts + 1.0f);
                    }
                }

                if (minSafeY <= maxSafeY)
                {
                    Position.Y = Math.Clamp(nextY, minSafeY, maxSafeY);
                }

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
            IsAlerted = true;
            Velocity.X = Facing == Direction.Right ? 55.0f : -55.0f;
            ActionTimer = MathF.Min(ActionTimer, 0.5f);
        }
    }

    private static bool TryFindBlockingWallCol(
        ITileMap grid,
        float x,
        float y,
        float width,
        float height,
        float vx,
        int ts,
        out int blockingCol)
    {
        blockingCol = 0;
        int minCol = (int)MathF.Floor((x + 0.25f) / ts);
        int maxCol = (int)MathF.Floor((x + width - 0.25f) / ts);
        int minRow = (int)MathF.Floor(y / ts);
        int maxRow = (int)MathF.Floor((y + height) / ts);

        if (vx >= 0)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                for (int r = minRow; r <= maxRow; r++)
                {
                    if (grid.GetTile(c, r) is TileType.Solid or TileType.Border or TileType.Crate)
                    {
                        blockingCol = c;
                        return true;
                    }
                }
            }
        }
        else
        {
            for (int c = maxCol; c >= minCol; c--)
            {
                for (int r = minRow; r <= maxRow; r++)
                {
                    if (grid.GetTile(c, r) is TileType.Solid or TileType.Border or TileType.Crate)
                    {
                        blockingCol = c;
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
