using System;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.AI;

public sealed class AutoPilotController : IInputSource
{
    private const int ActionCount = (int)GameAction.Count;
    private readonly bool[] _isDown = new bool[ActionCount];
    private readonly bool[] _justPressed = new bool[ActionCount];
    private readonly bool[] _justReleased = new bool[ActionCount];
    private readonly bool[] _prevDown = new bool[ActionCount];

    public bool IsActive { get; set; } = false;
    public float CountdownTimer { get; set; } = 0.0f;
    public bool IsConfirmationOpen { get; set; } = false;

    // Commitment and jump holding state
    private float _jumpHoldTimer = 0.0f;
    private float _stuckTimer = 0.0f;
    private float _lastX = 0.0f;
    private float _burstCooldown = 0.0f;
    private int _burstShotsFired = 0;
    private const int MaxBurstShots = 3;
    private int _escapeDirection = 0; // 0 = normal rightward, -1 = escaping left
    private float _escapeTimer = 0.0f;
    private int _targetLadderCol = -1;
    private int _climbOriginRow = -1;
    private int _blockingWallTopRow = -1;
    private float _dropThroughTimer = 0.0f;
    private float _evasionTimer = 0.0f;
    private int _evasionDirX = 0;
    private int _evasionDirY = 0;
    private float _areaStuckTimer = 0.0f;
    private float _areaAnchorX = 0.0f;

    public void StartDemoMode()
    {
        IsActive = false;
        CountdownTimer = 5.0f;
        IsConfirmationOpen = false;
        _jumpHoldTimer = 0.0f;
        _stuckTimer = 0.0f;
        _lastX = 0.0f;
        _burstCooldown = 0.0f;
        _burstShotsFired = 0;
        _escapeDirection = 0;
        _escapeTimer = 0.0f;
        _targetLadderCol = -1;
        _climbOriginRow = -1;
        _blockingWallTopRow = -1;
        _dropThroughTimer = 0.0f;
        _evasionTimer = 0.0f;
        _evasionDirX = 0;
        _evasionDirY = 0;
        _areaStuckTimer = 0.0f;
        _areaAnchorX = 0.0f;
        Array.Clear(_isDown, 0, ActionCount);
        Array.Clear(_justPressed, 0, ActionCount);
        Array.Clear(_justReleased, 0, ActionCount);
        Array.Clear(_prevDown, 0, ActionCount);
    }

    public void StopDemoMode()
    {
        IsActive = false;
        CountdownTimer = 0.0f;
        IsConfirmationOpen = false;
        _targetLadderCol = -1;
        _climbOriginRow = -1;
        _blockingWallTopRow = -1;
        _dropThroughTimer = 0.0f;
        _evasionTimer = 0.0f;
        _evasionDirX = 0;
        _evasionDirY = 0;
        _areaStuckTimer = 0.0f;
        _areaAnchorX = 0.0f;
        Array.Clear(_isDown, 0, ActionCount);
        Array.Clear(_justPressed, 0, ActionCount);
        Array.Clear(_justReleased, 0, ActionCount);
        Array.Clear(_prevDown, 0, ActionCount);
    }

    public static int GetRequiredBulletsForMonsterType(MonsterType type) => type switch
    {
        MonsterType.PatrolDroid => 2,
        MonsterType.HoverDrone => 2,
        MonsterType.CeilingTurret => 3,
        MonsterType.MechaHound => 3,
        MonsterType.HeavyGuard => 6,
        _ => 2
    };

    public static int CalculateRemainingBulletsNeeded(MonsterEntity monster, Projectile[] projectiles)
    {
        if (monster == null || !monster.IsActive || monster.Health <= 0) return 0;
        int inFlight = 0;
        var mBox = monster.BoundingBox;

        for (int i = 0; i < projectiles.Length; i++)
        {
            if (!projectiles[i].IsActive || !projectiles[i].FromPlayer) continue;
            var p = projectiles[i];

            // Projectile heading towards monster horizontally
            if (p.Velocity.X > 0 && p.Position.X < monster.Position.X)
            {
                if (p.Position.Y + Projectile.Height >= mBox.Y - 2.0f && p.Position.Y <= mBox.Y + mBox.Height + 2.0f)
                {
                    inFlight++;
                }
            }
            else if (p.Velocity.X < 0 && p.Position.X > monster.Position.X)
            {
                if (p.Position.Y + Projectile.Height >= mBox.Y - 2.0f && p.Position.Y <= mBox.Y + mBox.Height + 2.0f)
                {
                    inFlight++;
                }
            }
        }

        return Math.Max(0, monster.Health - inFlight);
    }

    public static bool HasLineOfSight(ITileMap grid, Vector2f from, Vector2f to)
    {
        int ts = PhysicsConstants.TileSize;
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 4.0f) return true;

        // Sample points along ray every 8px (half-tile)
        int steps = Math.Max(1, (int)MathF.Ceiling(dist / 8.0f));
        float stepX = dx / steps;
        float stepY = dy / steps;

        for (int s = 1; s < steps; s++)
        {
            float cx = from.X + stepX * s;
            float cy = from.Y + stepY * s;
            int col = (int)MathF.Floor(cx / ts);
            int row = (int)MathF.Floor(cy / ts);

            TileType t = grid.GetTile(col, row);
            if (t is TileType.Solid or TileType.Border or TileType.Crate)
            {
                return false;
            }
        }

        return true;
    }

    public static bool HasHorizontalClearance(ITileMap grid, float startX, float endX, float y)
    {
        int ts = PhysicsConstants.TileSize;
        int row = (int)MathF.Floor(y / ts);
        if (row < 0) return false;

        float leftX = MathF.Min(startX, endX);
        float rightX = MathF.Max(startX, endX);

        int firstCol = (int)MathF.Floor((leftX + 2.0f) / ts);
        int lastCol = (int)MathF.Floor((rightX - 2.0f) / ts);

        for (int c = firstCol; c <= lastCol; c++)
        {
            TileType t = grid.GetTile(c, row);
            if (t is TileType.Solid or TileType.Border or TileType.Crate)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsShootingYAligned(float playerY, float monsterY, float monsterHeight)
    {
        float bulletTop = playerY + 10.0f;
        float bulletBottom = bulletTop + Projectile.Height; // + 4.0f
        float monsterTop = monsterY;
        float monsterBottom = monsterY + monsterHeight;

        return bulletBottom >= monsterTop && bulletTop <= monsterBottom;
    }

    public static (float minY, float maxY) GetValidShootingYRange(float monsterY, float monsterHeight)
    {
        return (monsterY - 14.0f, monsterY + monsterHeight - 10.0f);
    }

    public static float GetIdealCombatAltitude(MonsterEntity target)
    {
        if (target.Type == MonsterType.HeavyGuard)
        {
            // Sweet spot: HeavyGuard shoots at Y+6 (interval [Y+6, Y+10]).
            // At target.Position.Y + 12, HeavyGuard bullet passes above player (head at Y+13),
            // while player bullet (at Y+22) squarely hits HeavyGuard's lower body (covers up to Y+26).
            return target.Position.Y + 12.0f;
        }

        // For other monsters, align player's bullet center (playerY + 12) with monster center
        return target.Position.Y + target.Height * 0.5f - 12.0f;
    }

    private PickupItem? FindBestPickup(GameEngine engine, bool isFlightMode)
    {
        var player = engine.Player;
        bool isLowHealth = player.Health <= 6;
        bool isCriticalHealth = player.Health <= 4;

        PickupItem? best = null;
        float bestScore = float.MaxValue;
        var pPos = player.Position;
        Vector2f pCenter = new(
            pPos.X + PlayerEntity.BoxOffset.X + PlayerEntity.BoxWidth * 0.5f,
            pPos.Y + PlayerEntity.BoxOffset.Y + PlayerEntity.BoxHeight * 0.5f
        );

        for (int i = 0; i < engine.Pickups.Length; i++)
        {
            ref readonly var p = ref engine.Pickups[i];
            if (!p.IsActive) continue;

            float dx = p.Position.X - pPos.X;
            float dy = p.Position.Y - pPos.Y;
            Vector2f pickupCenter = new(p.Position.X + PickupItem.Width * 0.5f, p.Position.Y + PickupItem.Height * 0.5f);

            if (isFlightMode)
            {
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist > 260.0f) continue;
                if (!HasLineOfSight(engine.Grid, pCenter, pickupCenter)) continue;

                float score = dist;
                if (isLowHealth && p.Type == PickupType.MedKit)
                {
                    score -= (isCriticalHealth ? 350.0f : 180.0f);
                }
                else if (p.Type is PickupType.Invulnerability or PickupType.RapidFire)
                {
                    score -= 60.0f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
            else
            {
                // In platformer mode:
                float maxBehind = isCriticalHealth ? 140.0f : (isLowHealth ? 80.0f : 20.0f);
                if (dx < -maxBehind || dx > 240.0f) continue;

                // Vertical reach: within jump height (-48px) to 40px below
                if (dy < -48.0f || dy > 40.0f) continue;

                if (!HasLineOfSight(engine.Grid, pCenter, pickupCenter)) continue;

                float score = MathF.Abs(dx) + MathF.Abs(dy) * 2.0f;
                if (dx < 0) score += 30.0f; // slight penalty for moving backwards

                if (isLowHealth && p.Type == PickupType.MedKit)
                {
                    score -= (isCriticalHealth ? 350.0f : 180.0f);
                }
                else if (p.Type == PickupType.Invulnerability)
                {
                    score -= 80.0f;
                }
                else if (p.Type is PickupType.RapidFire or PickupType.HighJumpBoots or PickupType.SpeedDash)
                {
                    score -= 40.0f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
        }

        return best;
    }

    public void Update(float dt, GameEngine engine)
    {
        Array.Copy(_isDown, _prevDown, ActionCount);
        Array.Clear(_isDown, 0, ActionCount);

        // 1. Handle 5-second countdown
        if (CountdownTimer > 0.0f)
        {
            CountdownTimer -= dt;
            if (CountdownTimer <= 0.0f)
            {
                CountdownTimer = 0.0f;
                IsActive = true;
            }
            ComputeEdgeEvents();
            return;
        }

        // 2. If confirmation dialog is open or bot is inactive, do not output actions
        if (IsConfirmationOpen || !IsActive || engine.IsPaused || engine.IsGameOver)
        {
            ComputeEdgeEvents();
            return;
        }

        var player = engine.Player;

        // Anti-stuck tracking
        if (MathF.Abs(player.Position.X - _lastX) < 1.0f)
        {
            _stuckTimer += dt;
        }
        else
        {
            _stuckTimer = 0.0f;
            _lastX = player.Position.X;
        }

        // Area-stuck tracking: checks if player hasn't made substantial progress (> 24px)
        if (MathF.Abs(player.Position.X - _areaAnchorX) < 24.0f)
        {
            _areaStuckTimer += dt;
        }
        else
        {
            _areaStuckTimer = 0.0f;
            _areaAnchorX = player.Position.X;
        }

        if (_escapeTimer > 0.0f)
        {
            _escapeTimer -= dt;
            if (_escapeTimer <= 0.0f)
            {
                _escapeDirection = 0;
            }
        }

        if (_evasionTimer > 0.0f)
        {
            _evasionTimer -= dt;
            if (_evasionTimer <= 0.0f)
            {
                _evasionDirX = 0;
                _evasionDirY = 0;
            }
        }

        // Burst cooldown update
        if (_burstCooldown > 0.0f)
        {
            _burstCooldown -= dt;
        }

        // Jump hold timer
        if (_jumpHoldTimer > 0.0f)
        {
            _jumpHoldTimer -= dt;
        }

        // Drop-through timer (hold Down+Jump to clear one-way platform tile)
        if (_dropThroughTimer > 0.0f)
        {
            _dropThroughTimer -= dt;
        }

        // === ZERO-GRAVITY (FLIGHT) CONTROLLER ===
        if (player.IsFlying)
        {
            UpdateFlightMode(dt, engine);
            ComputeEdgeEvents();
            return;
        }

        // === PLATFORMER CONTROLLER ===
        UpdatePlatformerMode(dt, engine);
        ComputeEdgeEvents();
    }

    private void UpdateFlightMode(float dt, GameEngine engine)
    {
        var player = engine.Player;
        var grid = engine.Grid;
        int ts = PhysicsConstants.TileSize;
        bool dodgeUp = false;
        bool dodgeDown = false;

        // 1. Bullet evasion (if not invincible)
        if (!player.IsGodMode && player.InvulnerabilityPowerupTimer <= 0.0f && player.InvulnerabilityTimer <= 0.3f)
        {
            int pCol = (int)MathF.Floor((player.Position.X + PlayerEntity.BoxWidth * 0.5f) / ts);
            int pHeadRow = (int)MathF.Floor(player.Position.Y / ts);
            int pFeetRow = (int)MathF.Floor((player.Position.Y + PlayerEntity.BoxHeight) / ts);

            bool ceilingAbove = pHeadRow <= 1 || grid.GetTile(pCol, pHeadRow - 1) is TileType.Solid or TileType.Border or TileType.Crate;
            bool floorBelow = grid.GetTile(pCol, pFeetRow + 1) is TileType.Solid or TileType.Border or TileType.Crate;

            for (int i = 0; i < engine.Projectiles.Length; i++)
            {
                ref var p = ref engine.Projectiles[i];
                if (!p.IsActive || p.FromPlayer) continue;

                float dx = p.Position.X - player.Position.X;
                float dy = p.Position.Y - player.Position.Y;

                // Incoming horizontal projectile
                if (MathF.Abs(dx) < 120.0f && ((dx > 0 && p.Velocity.X < 0) || (dx < 0 && p.Velocity.X > 0)))
                {
                    if (dy >= -4.0f && dy <= PlayerEntity.BoxHeight + 4.0f)
                    {
                        if (dy >= PlayerEntity.BoxHeight * 0.5f)
                        {
                            // Bullet in lower half: dodge up if ceiling clear, else down
                            if (!ceilingAbove) dodgeUp = true;
                            else if (!floorBelow) dodgeDown = true;
                        }
                        else
                        {
                            // Bullet in upper half: dodge down if floor clear, else up
                            if (!floorBelow) dodgeDown = true;
                            else if (!ceilingAbove) dodgeUp = true;
                        }
                        break;
                    }
                }
            }
        }

        // 2. Immediate Monster Stuck / Touch Hazard Evasion
        MonsterEntity? stuckThreat = null;
        float stuckDistSq = float.MaxValue;
        if (!player.IsGodMode && player.InvulnerabilityPowerupTimer <= 0.0f)
        {
            for (int i = 0; i < engine.Monsters.Length; i++)
            {
                var m = engine.Monsters[i];
                if (!m.IsActive || m.Health <= 0) continue;

                float mDx = m.Position.X - player.Position.X;
                float mDy = m.Position.Y - player.Position.Y;
                float distSq = mDx * mDx + mDy * mDy;

                bool isOverlapping = m.BoundingBox.Overlaps(player.BoundingBox);
                bool isDangerouslyClose = MathF.Abs(mDx) < 55.0f && MathF.Abs(mDy) < 26.0f;

                if (isOverlapping || isDangerouslyClose)
                {
                    if (distSq < stuckDistSq)
                    {
                        stuckDistSq = distSq;
                        stuckThreat = m;
                    }
                }
            }
        }

        if (stuckThreat != null)
        {
            _evasionTimer = 0.45f;
            float threatDx = stuckThreat.Position.X - player.Position.X;
            float threatDy = stuckThreat.Position.Y - player.Position.Y;

            int pCol = (int)MathF.Floor((player.Position.X + PlayerEntity.BoxWidth * 0.5f) / ts);
            int pRow = (int)MathF.Floor((player.Position.Y + PlayerEntity.BoxHeight * 0.5f) / ts);

            // Check walls around player
            bool blockedLeft = pCol <= 1 || grid.GetTile(pCol - 1, pRow) is TileType.Solid or TileType.Border or TileType.Crate;
            bool blockedRight = grid.GetTile(pCol + 1, pRow) is TileType.Solid or TileType.Border or TileType.Crate;
            bool blockedUp = pRow <= 1 || grid.GetTile(pCol, pRow - 1) is TileType.Solid or TileType.Border or TileType.Crate;
            bool blockedDown = grid.GetTile(pCol, pRow + 1) is TileType.Solid or TileType.Border or TileType.Crate;

            if (threatDx >= 0)
            {
                _evasionDirX = blockedLeft ? 1 : -1;
            }
            else
            {
                _evasionDirX = blockedRight ? -1 : 1;
            }

            if (threatDy >= 0)
            {
                _evasionDirY = blockedUp ? 1 : -1;
            }
            else
            {
                _evasionDirY = blockedDown ? -1 : 1;
            }
        }

        if (_evasionTimer > 0.0f)
        {
            if (_evasionDirX < 0) _isDown[(int)GameAction.MoveLeft] = true;
            else if (_evasionDirX > 0) _isDown[(int)GameAction.MoveRight] = true;

            if (_evasionDirY < 0) _isDown[(int)GameAction.MoveUp] = true;
            else if (_evasionDirY > 0) _isDown[(int)GameAction.MoveDown] = true;

            _isDown[(int)GameAction.Run] = true;
            return;
        }

        // 3. Combat Target Acquisition
        MonsterEntity? target = FindBestTarget(engine, isFlightMode: true);

        // 4. Health & Pickup Acquisition in Flight
        PickupItem? flightPickup = FindBestPickup(engine, isFlightMode: true);
        bool isCriticalHealth = player.Health <= 3;
        bool pickupIsAhead = flightPickup.HasValue && (flightPickup.Value.Position.X - player.Position.X) >= -10.0f;
        bool prioritizeFlightPickup = flightPickup.HasValue &&
            (isCriticalHealth || flightPickup.Value.Type == PickupType.Invulnerability || target == null || (player.Health <= 6 && pickupIsAhead));

        if (prioritizeFlightPickup)
        {
            var p = flightPickup!.Value;
            float pDx = p.Position.X - player.Position.X;
            float pDy = p.Position.Y - player.Position.Y;

            if (pDx > 4.0f) _isDown[(int)GameAction.MoveRight] = true;
            else if (pDx < -4.0f) _isDown[(int)GameAction.MoveLeft] = true;

            if (!dodgeUp && !dodgeDown)
            {
                if (pDy < -6.0f) _isDown[(int)GameAction.MoveUp] = true;
                else if (pDy > 6.0f) _isDown[(int)GameAction.MoveDown] = true;
            }
        }
        else if (target != null)
        {
            float dx = target.Position.X - player.Position.X;
            float dy = target.Position.Y - player.Position.Y;
            float absDx = MathF.Abs(dx);

            float minSafeDist = (target.Type == MonsterType.HoverDrone) ? 45.0f : 65.0f;
            const float MaxCombatDist = 140.0f;

            // Horizontal spacing: maintain standoff distance
            if (absDx > MaxCombatDist)
            {
                if (dx > 0) _isDown[(int)GameAction.MoveRight] = true;
                else _isDown[(int)GameAction.MoveLeft] = true;
            }
            else if (absDx < minSafeDist)
            {
                // Too close: back away to restore safe standoff distance!
                if (absDx >= 16.0f)
                {
                    if (dx > 0) _isDown[(int)GameAction.MoveLeft] = true;
                    else _isDown[(int)GameAction.MoveRight] = true;
                }
            }
            else
            {
                // In sweet spot: face monster (with 16px deadband to prevent jitter)
                if (absDx >= 16.0f)
                {
                    Direction desiredFacing = dx >= 0 ? Direction.Right : Direction.Left;
                    if (player.Facing != desiredFacing)
                    {
                        _isDown[(int)(desiredFacing == Direction.Right ? GameAction.MoveRight : GameAction.MoveLeft)] = true;
                    }
                }
            }

            // Match ideal combat altitude (including under-bullet sweet spot for HeavyGuard)
            if (!dodgeUp && !dodgeDown)
            {
                float idealY = GetIdealCombatAltitude(target);
                float yDiff = idealY - player.Position.Y;
                if (yDiff < -3.0f) _isDown[(int)GameAction.MoveUp] = true;
                else if (yDiff > 3.0f) _isDown[(int)GameAction.MoveDown] = true;
            }

            // Shoot if aligned, bullets needed, and clear line of sight
            int needed = CalculateRemainingBulletsNeeded(target, engine.Projectiles);
            bool isFacing = (dx >= 0 && player.Facing == Direction.Right) || (dx < 0 && player.Facing == Direction.Left);
            if (needed > 0 && isFacing && IsShootingYAligned(player.Position.Y, target.Position.Y, target.Height))
            {
                Vector2f gunPos = new(player.Position.X + (player.Facing == Direction.Right ? 18.0f : -2.0f), player.Position.Y + 12.0f);
                Vector2f targetCenter = new(target.Position.X + target.Width * 0.5f, target.Position.Y + target.Height * 0.5f);
                if (HasLineOfSight(engine.Grid, gunPos, targetCenter))
                {
                    PerformControlledBurst();
                }
            }
        }
        else
        {
            // Default forward flight to the right
            _isDown[(int)GameAction.MoveRight] = true;
            _isDown[(int)GameAction.Run] = true;
        }

        if (dodgeUp)
        {
            _isDown[(int)GameAction.MoveUp] = true;
            _isDown[(int)GameAction.MoveDown] = false;
        }
        else if (dodgeDown)
        {
            _isDown[(int)GameAction.MoveDown] = true;
            _isDown[(int)GameAction.MoveUp] = false;
        }
    }

    private void UpdatePlatformerMode(float dt, GameEngine engine)
    {
        var player = engine.Player;
        var grid = engine.Grid;
        int ts = PhysicsConstants.TileSize;

        // Player tile coordinates
        float pCenterX = player.Position.X + PlayerEntity.BoxOffset.X + PlayerEntity.BoxWidth * 0.5f;
        float pFeetY = player.Position.Y + PlayerEntity.BoxOffset.Y + PlayerEntity.BoxHeight;
        float pHeadY = player.Position.Y + PlayerEntity.BoxOffset.Y;

        int playerCol = (int)MathF.Floor(pCenterX / ts);
        int playerFeetRow = (int)MathF.Floor((pFeetY - 1.0f) / ts);
        int playerHeadRow = (int)MathF.Floor(pHeadY / ts);

        // 1. Incoming Bullet Dodging (Prioritize dodging incoming bullets over combat standing)
        bool dodgeJump = false;
        bool dodgeMoveLeft = false;
        bool dodgeMoveRight = false;

        if (!player.IsGodMode && player.InvulnerabilityPowerupTimer <= 0.0f && player.InvulnerabilityTimer <= 0.3f)
        {
            for (int i = 0; i < engine.Projectiles.Length; i++)
            {
                ref var p = ref engine.Projectiles[i];
                if (!p.IsActive || p.FromPlayer) continue;

                float pProjCenterX = p.Position.X + Projectile.Width * 0.5f;
                float pProjCenterY = p.Position.Y + Projectile.Height * 0.5f;
                float dx = pProjCenterX - pCenterX;
                float dy = pProjCenterY - pHeadY;

                // Case A: Vertical bullet dropping from above (e.g. CeilingTurret)
                if (p.Velocity.Y > 20.0f && MathF.Abs(dx) < 18.0f && dy < 0.0f && dy > -140.0f)
                {
                    if (dx >= 0) dodgeMoveLeft = true;
                    else dodgeMoveRight = true;
                    break;
                }

                // Case B: Horizontal bullet approaching player within 120px
                bool isApproachingX = (dx > 0 && p.Velocity.X < 0) || (dx < 0 && p.Velocity.X > 0);
                if (MathF.Abs(dx) < 120.0f && isApproachingX)
                {
                    // Check if bullet Y trajectory hits player's vertical hitbox [0, BoxHeight]
                    if (dy >= -4.0f && dy <= PlayerEntity.BoxHeight + 4.0f)
                    {
                        bool ceilingOverhead = playerHeadRow > 0 && grid.GetTile(playerCol, playerHeadRow - 1) is TileType.Solid or TileType.Border or TileType.Crate;
                        if (player.IsGrounded && !ceilingOverhead)
                        {
                            dodgeJump = true;
                            _jumpHoldTimer = 0.28f;
                        }
                        else if (!player.IsGrounded)
                        {
                            // In air: if above bullet, let it pass underneath; if colliding, steer horizontally
                            if (pFeetY >= pProjCenterY - 4.0f)
                            {
                                if (dx > 0) dodgeMoveLeft = true;
                                else dodgeMoveRight = true;
                            }
                        }
                        else
                        {
                            // Under low ceiling and cannot jump: dodge away horizontally
                            if (dx > 0) dodgeMoveLeft = true;
                            else dodgeMoveRight = true;
                        }
                        break;
                    }
                }
            }
        }

        // 2. Immediate Monster Stuck / Touch Hazard Evasion
        MonsterEntity? stuckThreat = null;
        float stuckDistSq = float.MaxValue;
        if (!player.IsGodMode && player.InvulnerabilityPowerupTimer <= 0.0f)
        {
            for (int i = 0; i < engine.Monsters.Length; i++)
            {
                var m = engine.Monsters[i];
                if (!m.IsActive || m.Health <= 0) continue;

                float mDx = m.Position.X - player.Position.X;
                float mDy = m.Position.Y - player.Position.Y;

                // Only consider threats on the same vertical level (-12px to +26px, ignore drones safely overhead)
                if (mDy < -12.0f || mDy > 26.0f) continue;

                bool isOverlapping = m.BoundingBox.Overlaps(player.BoundingBox);
                bool isDangerouslyClose = MathF.Abs(mDx) < 45.0f;

                if (isOverlapping || isDangerouslyClose)
                {
                    float distSq = mDx * mDx + mDy * mDy;
                    if (distSq < stuckDistSq)
                    {
                        stuckDistSq = distSq;
                        stuckThreat = m;
                    }
                }
            }
        }

        if (_evasionTimer <= 0.0f && stuckThreat != null && !player.IsClimbing)
        {
            _evasionTimer = 0.40f;
            float threatDx = stuckThreat.Position.X - player.Position.X;

            if (threatDx >= 0)
            {
                // Threat is to the right: check if left (behind) is safe
                int tileBehindCol = playerCol - 1;
                TileType groundBehind = grid.GetTile(tileBehindCol, playerFeetRow + 1);
                TileType wallBehind = grid.GetTile(tileBehindCol, playerFeetRow);
                TileType wallBehindHead = grid.GetTile(tileBehindCol, playerHeadRow);

                bool canRetreatLeft = (wallBehind is not (TileType.Solid or TileType.Border or TileType.Crate)) &&
                                      (wallBehindHead is not (TileType.Solid or TileType.Border or TileType.Crate)) &&
                                      (groundBehind is TileType.Solid or TileType.OneWayPlatform or TileType.Crate or TileType.Ladder);

                if (canRetreatLeft)
                {
                    _evasionDirX = -1;
                    if (player.IsGrounded && (threatDx < 28.0f || stuckThreat.BoundingBox.Overlaps(player.BoundingBox)))
                    {
                        _jumpHoldTimer = 0.28f;
                    }
                }
                else
                {
                    // Blocked behind: vault OVER the monster to the right!
                    _evasionDirX = 1;
                    if (player.IsGrounded)
                    {
                        _jumpHoldTimer = 0.35f;
                    }
                }
            }
            else
            {
                // Threat is to the left: check if right is safe
                int tileAheadCol = playerCol + 1;
                TileType groundAhead = grid.GetTile(tileAheadCol, playerFeetRow + 1);
                TileType wallAhead = grid.GetTile(tileAheadCol, playerFeetRow);
                TileType wallAheadHead = grid.GetTile(tileAheadCol, playerHeadRow);

                bool canAdvanceRight = (wallAhead is not (TileType.Solid or TileType.Border or TileType.Crate)) &&
                                       (wallAheadHead is not (TileType.Solid or TileType.Border or TileType.Crate)) &&
                                       (groundAhead is TileType.Solid or TileType.OneWayPlatform or TileType.Crate or TileType.Ladder);

                if (canAdvanceRight)
                {
                    _evasionDirX = 1;
                    if (player.IsGrounded && (MathF.Abs(threatDx) < 28.0f || stuckThreat.BoundingBox.Overlaps(player.BoundingBox)))
                    {
                        _jumpHoldTimer = 0.28f;
                    }
                }
                else
                {
                    // Blocked ahead: vault OVER the monster to the left!
                    _evasionDirX = -1;
                    if (player.IsGrounded)
                    {
                        _jumpHoldTimer = 0.35f;
                    }
                }
            }
        }

        if (_evasionTimer > 0.0f && !player.IsClimbing)
        {
            if (_evasionDirX < 0) _isDown[(int)GameAction.MoveLeft] = true;
            else if (_evasionDirX > 0) _isDown[(int)GameAction.MoveRight] = true;
            _isDown[(int)GameAction.Run] = true;

            if (_jumpHoldTimer > 0.0f)
            {
                _isDown[(int)GameAction.Jump] = true;
            }
            return;
        }

        // 3. Ladder Detection & Climbing
        bool onLadder = false;
        int ladderCol = playerCol;
        int ladderMinCol = (int)MathF.Floor((player.Position.X + PlayerEntity.BoxOffset.X) / ts);
        int ladderMaxCol = (int)MathF.Floor((player.Position.X + PlayerEntity.BoxOffset.X + PlayerEntity.BoxWidth - 0.001f) / ts);

        for (int r = playerHeadRow; r <= playerFeetRow; r++)
        {
            for (int c = ladderMinCol; c <= ladderMaxCol; c++)
            {
                if (grid.GetTile(c, r) == TileType.Ladder)
                {
                    onLadder = true;
                    ladderCol = c;
                    break;
                }
            }
            if (onLadder) break;
        }

        // Check if ladder is hanging directly overhead (within jump reach)
        bool ladderOverhead = false;
        for (int r = playerHeadRow - 1; r >= playerHeadRow - 3; r--)
        {
            if (r >= 0 && (grid.GetTile(playerCol, r) == TileType.Ladder || grid.GetTile(ladderMinCol, r) == TileType.Ladder || grid.GetTile(ladderMaxCol, r) == TileType.Ladder))
            {
                ladderOverhead = true;
                break;
            }
        }

        // Check if forward path is blocked by an impassable wall or stack of crates
        int forwardWallCol = playerCol + 1;
        int stepHeightAboveFeet = 0;
        int obstacleTopRow = playerFeetRow + 1;

        for (int r = playerFeetRow; r >= Math.Max(0, playerFeetRow - 6); r--)
        {
            TileType t = grid.GetTile(forwardWallCol, r);
            if (t is TileType.Solid or TileType.Crate or TileType.Border)
            {
                stepHeightAboveFeet++;
                obstacleTopRow = r;
            }
            else
            {
                break;
            }
        }

        // Check overhead clearance above obstacle to determine if jumpable
        bool hasOverheadClearance = true;
        for (int r = obstacleTopRow - 1; r >= Math.Max(0, obstacleTopRow - 2); r--)
        {
            if (grid.GetTile(forwardWallCol, r) is TileType.Solid or TileType.Crate or TileType.Border)
            {
                hasOverheadClearance = false;
                break;
            }
        }
        if (grid.GetTile(playerCol, playerHeadRow - 1) is TileType.Solid or TileType.Crate or TileType.Border)
        {
            hasOverheadClearance = false;
        }

        bool bodyBlocked = stepHeightAboveFeet > 0;
        bool impassableWallAhead = stepHeightAboveFeet >= 3 || (bodyBlocked && !hasOverheadClearance);

        // Check if forward ground path has a wide pit (no floor for 2+ tiles)
        bool pitAhead = false;
        if (grid.GetTile(playerCol + 1, playerFeetRow + 1) is TileType.Empty or TileType.Hazard &&
            grid.GetTile(playerCol + 2, playerFeetRow + 1) is TileType.Empty or TileType.Hazard)
        {
            pitAhead = true;
        }

        // Drop through one-way platform if active
        if (_dropThroughTimer > 0.0f)
        {
            _isDown[(int)GameAction.MoveDown] = true;
            _isDown[(int)GameAction.Jump] = true;
            ComputeEdgeEvents();
            return;
        }

        // Dead end detection: if facing impassable wall ahead, seek the nearest ladder
        if (impassableWallAhead && player.IsGrounded && !player.IsClimbing)
        {
            // If on an elevated platform with a OneWayPlatform or ladder below: drop down!
            bool onOneWay = (grid.GetTile(playerCol, playerFeetRow) == TileType.OneWayPlatform) ||
                            (grid.GetTile(playerCol, playerFeetRow + 1) == TileType.OneWayPlatform);
            bool ladderBelow = (grid.GetTile(playerCol, playerFeetRow + 1) == TileType.Ladder) ||
                               (grid.GetTile(playerCol, playerFeetRow + 2) == TileType.Ladder);

            if ((onOneWay || ladderBelow) && playerFeetRow < 25 && !onLadder)
            {
                _dropThroughTimer = 0.25f;
                _isDown[(int)GameAction.MoveDown] = true;
                _isDown[(int)GameAction.Jump] = true;
                ComputeEdgeEvents();
                return;
            }

            if (_climbOriginRow < 0) _climbOriginRow = playerFeetRow;
            if (_blockingWallTopRow < 0) _blockingWallTopRow = obstacleTopRow - 1;

            if (_targetLadderCol < 0)
            {
                // Check starting right at player's current column, then scan backwards
                for (int c = playerCol; c >= Math.Max(0, playerCol - 20); c--)
                {
                    for (int r = playerFeetRow + 1; r >= playerHeadRow - 4; r--)
                    {
                        if (r >= 0 && grid.GetTile(c, r) == TileType.Ladder)
                        {
                            _targetLadderCol = c;
                            break;
                        }
                    }
                    if (_targetLadderCol >= 0) break;
                }
                if (_targetLadderCol >= 0)
                {
                    if (_targetLadderCol < playerCol)
                    {
                        _escapeDirection = -1;
                        _escapeTimer = 5.0f; // Ample time to reach ladder
                    }
                }
            }
        }

        // If currently climbing or on a ladder where we want to climb
        bool shouldClimb = onLadder && (impassableWallAhead || pitAhead || player.IsClimbing || _targetLadderCol == ladderCol || _targetLadderCol >= 0);
        if (shouldClimb || player.IsClimbing)
        {
            if (player.IsClimbing && !player.IsGrounded)
            {
                _targetLadderCol = -1;
                _escapeDirection = 0;
            }

            bool isElevatedAboveOrigin = (_climbOriginRow < 0) || (playerFeetRow < _climbOriginRow - 1);
            bool clearedBlockingWall = (_blockingWallTopRow < 0) || (playerFeetRow <= _blockingWallTopRow);

            // Check for exit platform or ladder to the right
            bool canDismountRight = false;
            for (int r = playerFeetRow; r >= Math.Max(0, playerFeetRow - 1); r--)
            {
                TileType tAt = grid.GetTile(ladderCol + 1, r);
                TileType tBelow = grid.GetTile(ladderCol + 1, r + 1);
                bool hasExit = (tAt == TileType.Ladder) || (tAt == TileType.OneWayPlatform) || (tBelow is TileType.Solid or TileType.OneWayPlatform or TileType.Crate);
                float surfaceY = r * ts;

                if (hasExit)
                {
                    bool hasSpace = (grid.GetTile(ladderCol + 1, r - 1) is TileType.Empty or TileType.OneWayPlatform or TileType.Ladder) &&
                                    (grid.GetTile(ladderCol + 1, r) is TileType.Empty or TileType.OneWayPlatform or TileType.Ladder);
                    bool wallCleared = (_blockingWallTopRow < 0) || (r <= _blockingWallTopRow);
                    bool elevated = (_climbOriginRow < 0) || (r < _climbOriginRow - 1);

                    if (hasSpace && wallCleared && elevated && pFeetY <= surfaceY + 3.0f)
                    {
                        canDismountRight = true;
                        break;
                    }
                }
            }

            // Check for exit platform or ladder to the left
            bool canDismountLeft = false;
            for (int r = playerFeetRow; r >= Math.Max(0, playerFeetRow - 1); r--)
            {
                TileType tAt = grid.GetTile(ladderCol - 1, r);
                TileType tBelow = grid.GetTile(ladderCol - 1, r + 1);
                bool hasExit = (tAt == TileType.Ladder) || (tAt == TileType.OneWayPlatform) || (tBelow is TileType.Solid or TileType.OneWayPlatform or TileType.Crate);
                float surfaceY = r * ts;

                if (hasExit)
                {
                    bool hasSpace = (grid.GetTile(ladderCol - 1, r - 1) is TileType.Empty or TileType.OneWayPlatform or TileType.Ladder) &&
                                    (grid.GetTile(ladderCol - 1, r) is TileType.Empty or TileType.OneWayPlatform or TileType.Ladder);
                    bool wallCleared = (_blockingWallTopRow < 0) || (r <= _blockingWallTopRow);
                    bool elevated = (_climbOriginRow < 0) || (r < _climbOriginRow - 1);

                    if (hasSpace && wallCleared && elevated && pFeetY <= surfaceY + 3.0f)
                    {
                        canDismountLeft = true;
                        break;
                    }
                }
            }

            // The ladder has ended when there is no ladder at or above player's feet
            bool ladderEnded = grid.GetTile(ladderCol, playerFeetRow) != TileType.Ladder &&
                               grid.GetTile(ladderCol, playerFeetRow - 1) != TileType.Ladder;

            // Dismount onto right platform if available and wall cleared
            if (canDismountRight)
            {
                _isDown[(int)GameAction.MoveRight] = true;
                _isDown[(int)GameAction.MoveUp] = false;
                _climbOriginRow = -1;
                _blockingWallTopRow = -1;
            }
            // Only dismount left if top of ladder reached, left is available, and wall cleared
            else if (canDismountLeft && ladderEnded && clearedBlockingWall)
            {
                _isDown[(int)GameAction.MoveLeft] = true;
                _isDown[(int)GameAction.MoveUp] = false;
                _climbOriginRow = -1;
                _blockingWallTopRow = -1;
            }
            else
            {
                // If climbing, elevated, wall cleared, and the ladder has ended above feet: leap off the top!
                if (player.IsClimbing && isElevatedAboveOrigin && clearedBlockingWall && ladderEnded)
                {
                    _isDown[(int)GameAction.Jump] = true;
                    _isDown[(int)GameAction.MoveRight] = true;
                    _jumpHoldTimer = 0.28f;
                    _climbOriginRow = -1;
                    _blockingWallTopRow = -1;
                }
                else
                {
                    // Continue climbing up!
                    _isDown[(int)GameAction.MoveUp] = true;
                    _isDown[(int)GameAction.MoveRight] = false;
                    _isDown[(int)GameAction.MoveLeft] = false;
                }
            }
            ComputeEdgeEvents();
            return;
        }

        // Navigate towards target ladder if active
        if (_targetLadderCol >= 0)
        {
            float targetLadderX = _targetLadderCol * ts + ts * 0.5f;
            float distToLadder = targetLadderX - pCenterX;

            if (MathF.Abs(distToLadder) <= 8.0f)
            {
                // Arrived at ladder column!
                if (onLadder)
                {
                    if (player.IsGrounded)
                    {
                        _isDown[(int)GameAction.Jump] = true;
                        _jumpHoldTimer = 0.32f;
                    }
                    _isDown[(int)GameAction.MoveUp] = true;
                    ComputeEdgeEvents();
                    return;
                }
                else if (ladderOverhead && player.IsGrounded)
                {
                    _isDown[(int)GameAction.Jump] = true;
                    _jumpHoldTimer = 0.32f;
                    _isDown[(int)GameAction.MoveUp] = true;
                }
            }
            else
            {
                if (distToLadder < 0) _isDown[(int)GameAction.MoveLeft] = true;
                else _isDown[(int)GameAction.MoveRight] = true;
            }
        }

        // 3. Terrain Lookahead (Pits, Hazards, and Ledge Hops)
        int moveDir = (_targetLadderCol >= 0) ? (_targetLadderCol * ts + ts * 0.5f < pCenterX ? -1 : 1) : (_escapeDirection < 0 ? -1 : 1);
        int forwardCol = playerCol + moveDir;
        int forwardCol2 = playerCol + moveDir * 2;

        bool triggerJump = dodgeJump;
        TileType below1 = grid.GetTile(forwardCol, playerFeetRow + 1);
        TileType below2 = grid.GetTile(forwardCol2, playerFeetRow + 1);
        TileType atFeet1 = grid.GetTile(forwardCol, playerFeetRow);

        // Gap / spike hazard detection: jump at the edge (when tile directly ahead has no ground)
        if (player.IsGrounded && (below1 is TileType.Empty or TileType.Hazard))
        {
            triggerJump = true;
            _jumpHoldTimer = 0.35f; // Hold jump for full arc distance across gaps
        }

        // Low obstacle hop (1-2 block step)
        if (!impassableWallAhead && player.IsGrounded && (stepHeightAboveFeet is 1 or 2 || atFeet1 is TileType.Solid or TileType.Crate) && hasOverheadClearance)
        {
            triggerJump = true;
            _jumpHoldTimer = 0.28f;
        }

        // Stuck detection
        bool isAreaStuck = _areaStuckTimer > 1.0f;
        if ((_stuckTimer > 0.6f || isAreaStuck) && player.IsGrounded)
        {
            if (isAreaStuck)
            {
                // Force an assertive forward vault jump to break free from pacing/looping in the same area
                _escapeDirection = 0;
                _escapeTimer = 0.0f;
                _targetLadderCol = -1;
                triggerJump = true;
                _jumpHoldTimer = 0.36f;
                _isDown[(int)GameAction.MoveRight] = true;
                _isDown[(int)GameAction.MoveLeft] = false;
                _isDown[(int)GameAction.Run] = true;
            }
            else if (bodyBlocked || impassableWallAhead)
            {
                _escapeDirection = -1;
                _escapeTimer = 2.0f;
                _stuckTimer = 0.0f;
            }
            else
            {
                triggerJump = true;
                _jumpHoldTimer = 0.28f;
                if (_stuckTimer > 1.8f)
                {
                    _escapeDirection = -1;
                    _escapeTimer = 2.0f;
                    _stuckTimer = 0.0f;
                }
            }
        }

        // Maintain jump while timer is active
        if (_jumpHoldTimer > 0.0f)
        {
            _isDown[(int)GameAction.Jump] = true;
            _isDown[(int)GameAction.Run] = true;
        }

        // 4. Pickup Seeking (Health Prioritization when hurt)
        PickupItem? targetPickup = FindBestPickup(engine, isFlightMode: false);
        bool pursuingHealth = false;

        if (targetPickup.HasValue)
        {
            var p = targetPickup.Value;
            float pDx = p.Position.X - player.Position.X;
            float pDy = p.Position.Y - player.Position.Y;

            bool isUrgentMedKit = (player.Health <= 6 && p.Type == PickupType.MedKit);
            bool isInvuln = (p.Type == PickupType.Invulnerability);

            if (isUrgentMedKit || isInvuln)
            {
                pursuingHealth = true;

                if (pDx > 4.0f)
                {
                    _isDown[(int)GameAction.MoveRight] = true;
                    _isDown[(int)GameAction.MoveLeft] = false;
                }
                else if (pDx < -4.0f)
                {
                    _isDown[(int)GameAction.MoveLeft] = true;
                    _isDown[(int)GameAction.MoveRight] = false;
                }

                if (pDy < -14.0f && player.IsGrounded)
                {
                    _isDown[(int)GameAction.Jump] = true;
                    _jumpHoldTimer = 0.28f;
                }
            }
        }

        // 5. Target Acquisition & Combat (Restricted to relevant vertical corridor!)
        MonsterEntity? target = FindBestTarget(engine, isFlightMode: false);
        bool inCombatStance = false;

        if (target != null && (!pursuingHealth || player.Health > 4))
        {
            float dx = target.Position.X - player.Position.X;
            float dy = target.Position.Y - player.Position.Y;

            // Only engage if in front or an immediate rear threat within 70px
            bool isImmediateRearThreat = dx < 0 && dx > -75.0f && MathF.Abs(dy) <= 20.0f;
            bool isForwardTarget = dx >= -10.0f && dx <= 200.0f;

            if (isImmediateRearThreat || isForwardTarget)
            {
                int needed = CalculateRemainingBulletsNeeded(target, engine.Projectiles);
                if (needed > 0)
                {
                    inCombatStance = true;

                    // Ensure player faces the target, with a 16px deadband to prevent rapid flipping when overhead
                    float absDx = MathF.Abs(dx);
                    Direction desiredFacing = dx >= 0 ? Direction.Right : Direction.Left;
                    if (absDx >= 16.0f && player.Facing != desiredFacing)
                    {
                        if (desiredFacing == Direction.Right)
                        {
                            _isDown[(int)GameAction.MoveRight] = true;
                            _isDown[(int)GameAction.MoveLeft] = false;
                        }
                        else
                        {
                            _isDown[(int)GameAction.MoveLeft] = true;
                            _isDown[(int)GameAction.MoveRight] = false;
                        }
                    }
                    else if (!dodgeMoveLeft && !dodgeMoveRight)
                    {
                        // Already facing target: advance if far, or hold position to shoot steadily
                        if (absDx > 120.0f && !pursuingHealth)
                        {
                            if (dx > 0)
                            {
                                _isDown[(int)GameAction.MoveRight] = true;
                                _isDown[(int)GameAction.MoveLeft] = false;
                            }
                            else
                            {
                                _isDown[(int)GameAction.MoveLeft] = true;
                                _isDown[(int)GameAction.MoveRight] = false;
                            }
                        }
                        else if (absDx < 65.0f && absDx >= 16.0f && !pursuingHealth)
                        {
                            // Monster is encroaching inside firing range: backpedal if floor behind is safe
                            int kiteCol = dx > 0 ? playerCol - 1 : playerCol + 1;
                            TileType groundKite = grid.GetTile(kiteCol, playerFeetRow + 1);
                            TileType wallKite = grid.GetTile(kiteCol, playerFeetRow);
                            bool canKite = (wallKite is not (TileType.Solid or TileType.Border or TileType.Crate)) &&
                                           (groundKite is TileType.Solid or TileType.OneWayPlatform or TileType.Crate or TileType.Ladder);
                            if (canKite)
                            {
                                if (dx > 0)
                                {
                                    _isDown[(int)GameAction.MoveLeft] = true;
                                    _isDown[(int)GameAction.MoveRight] = false;
                                }
                                else
                                {
                                    _isDown[(int)GameAction.MoveRight] = true;
                                    _isDown[(int)GameAction.MoveLeft] = false;
                                }
                            }
                            else
                            {
                                _isDown[(int)GameAction.MoveRight] = false;
                                _isDown[(int)GameAction.MoveLeft] = false;
                            }
                        }
                        else if (!pursuingHealth && absDx >= 16.0f)
                        {
                            // In firing sweet spot: hold ground and fire
                            _isDown[(int)GameAction.MoveRight] = false;
                            _isDown[(int)GameAction.MoveLeft] = false;
                        }
                    }

                    // Fire if facing target, aligned horizontally, and has clear line of sight
                    bool isFacingTarget = (dx >= 0 && player.Facing == Direction.Right) || (dx < 0 && player.Facing == Direction.Left);
                    if (isFacingTarget)
                    {
                        float currentBulletY = player.Position.Y + 10.0f;
                        float mTop = target.Position.Y;
                        float mBottom = target.Position.Y + target.Height;

                        // Check if currently aligned with monster hitbox
                        bool isCurrentlyAligned = (currentBulletY + 4.0f >= mTop) && (currentBulletY <= mBottom);
                        float gunX = player.Position.X + (player.Facing == Direction.Right ? PlayerEntity.BoxWidth + 4.0f : -4.0f);
                        bool pathClearNow = HasHorizontalClearance(engine.Grid, gunX, target.Position.X, currentBulletY + 2.0f);

                        if (isCurrentlyAligned && pathClearNow)
                        {
                            PerformControlledBurst();
                        }
                        else if (mBottom < currentBulletY && mBottom >= currentBulletY - 48.0f)
                        {
                            // Target is elevated: leap to bring gun up to target altitude!
                            if (player.IsGrounded)
                            {
                                _isDown[(int)GameAction.Jump] = true;
                                _jumpHoldTimer = 0.28f;
                            }
                        }
                    }
                }
            }
        }

        // 6. Default Horizontal Progression & Bullet Evasion Override
        if (dodgeMoveLeft)
        {
            _isDown[(int)GameAction.MoveLeft] = true;
            _isDown[(int)GameAction.MoveRight] = false;
            _isDown[(int)GameAction.Run] = true;
        }
        else if (dodgeMoveRight)
        {
            _isDown[(int)GameAction.MoveRight] = true;
            _isDown[(int)GameAction.MoveLeft] = false;
            _isDown[(int)GameAction.Run] = true;
        }
        else if (isAreaStuck)
        {
            _isDown[(int)GameAction.MoveRight] = true;
            _isDown[(int)GameAction.MoveLeft] = false;
            _isDown[(int)GameAction.Run] = true;
        }
        else if (!inCombatStance && !pursuingHealth && _targetLadderCol < 0)
        {
            if (_escapeDirection < 0)
            {
                _isDown[(int)GameAction.MoveLeft] = true;
                _isDown[(int)GameAction.Run] = true;
            }
            else
            {
                _isDown[(int)GameAction.MoveRight] = true;
                _isDown[(int)GameAction.Run] = true;
            }
        }

        if (triggerJump || isAreaStuck)
        {
            _isDown[(int)GameAction.Jump] = true;
            _isDown[(int)GameAction.Run] = true;
        }

        if ((player.IsGodMode || player.InvulnerabilityPowerupTimer > 0.0f) && _targetLadderCol < 0 && _escapeDirection == 0)
        {
            _isDown[(int)GameAction.Run] = true;
            _isDown[(int)GameAction.MoveRight] = true; // Rambo mode charges right!
        }
    }

    private void PerformControlledBurst()
    {
        if (_burstCooldown > 0.0f) return;

        _isDown[(int)GameAction.Shoot] = true;
        _burstShotsFired++;

        if (_burstShotsFired >= MaxBurstShots)
        {
            _burstShotsFired = 0;
            _burstCooldown = 0.20f; // Natural pause between controlled bursts
        }
    }

    private MonsterEntity? FindBestTarget(GameEngine engine, bool isFlightMode)
    {
        MonsterEntity? best = null;
        float bestScore = float.MaxValue;
        var pPos = engine.Player.Position;

        for (int i = 0; i < engine.Monsters.Length; i++)
        {
            var m = engine.Monsters[i];
            if (!m.IsActive || m.Health <= 0) continue;

            float dx = m.Position.X - pPos.X;
            float dy = MathF.Abs(m.Position.Y - pPos.Y);

            Vector2f mCenter = new(m.Position.X + m.Width * 0.5f, m.Position.Y + m.Height * 0.5f);

            if (isFlightMode)
            {
                // In flight mode, free 4-way targeting with clear line of sight
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist < 220.0f && dist < bestScore)
                {
                    Vector2f pCenter = new(pPos.X + PlayerEntity.BoxWidth * 0.5f, pPos.Y + PlayerEntity.BoxHeight * 0.5f);
                    if (HasLineOfSight(engine.Grid, pCenter, mCenter))
                    {
                        bestScore = dist;
                        best = m;
                    }
                }
            }
            else
            {
                // In platformer mode, Duke can only shoot horizontally!
                // Discard monsters that are vertically out of reach (> 50px above or > 16px below)
                float currentBulletY = pPos.Y + 10.0f;
                float mTop = m.Position.Y;
                float mBottom = m.Position.Y + m.Height;

                bool isStandingAligned = (currentBulletY + 4.0f >= mTop) && (currentBulletY <= mBottom);
                float gunX = pPos.X + (dx >= 0 ? PlayerEntity.BoxWidth + 4.0f : -4.0f);
                bool canHitStanding = isStandingAligned && HasHorizontalClearance(engine.Grid, gunX, m.Position.X, currentBulletY + 2.0f);

                bool canHitJumping = false;
                if (!canHitStanding && mBottom >= currentBulletY - 48.0f && mTop <= currentBulletY)
                {
                    // Check if player has headroom to jump and horizontal path at monster altitude is clear
                    int ts = PhysicsConstants.TileSize;
                    int pCol = (int)MathF.Floor((pPos.X + PlayerEntity.BoxWidth * 0.5f) / ts);
                    int pHeadRow = (int)MathF.Floor(pPos.Y / ts);
                    bool hasHeadroom = pHeadRow > 1 &&
                                       engine.Grid.GetTile(pCol, pHeadRow - 1) is not (TileType.Solid or TileType.Border or TileType.Crate);

                    if (hasHeadroom && HasHorizontalClearance(engine.Grid, gunX, m.Position.X, mTop + m.Height * 0.5f))
                    {
                        canHitJumping = true;
                    }
                }

                if (!canHitStanding && !canHitJumping)
                {
                    continue; // Completely obstructed by solid wall or crates or out of reach!
                }

                // Heavily favor monsters ahead in the direction of progression
                float score;
                if (dx >= 0)
                {
                    // Ahead: distance score heavily penalizes vertical misalignment
                    score = dx + dy * 2.5f;
                }
                else
                {
                    // Behind: only consider if within 75px, with high penalty
                    if (dx < -75.0f) continue;
                    score = MathF.Abs(dx) * 1.5f + dy * 3.0f + 50.0f;
                }

                if (score < 220.0f && score < bestScore)
                {
                    bestScore = score;
                    best = m;
                }
            }
        }
        return best;
    }

    private void ComputeEdgeEvents()
    {
        for (int i = 0; i < ActionCount; i++)
        {
            _justPressed[i] = _isDown[i] && !_prevDown[i];
            _justReleased[i] = !_isDown[i] && _prevDown[i];
        }
    }

    public bool IsDown(GameAction action) => _isDown[(int)action];
    public bool JustPressed(GameAction action) => _justPressed[(int)action];
    public bool JustReleased(GameAction action) => _justReleased[(int)action];
}
