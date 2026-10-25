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

        if (_escapeTimer > 0.0f)
        {
            _escapeTimer -= dt;
            if (_escapeTimer <= 0.0f)
            {
                _escapeDirection = 0;
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
        bool dodgeUp = false;
        bool dodgeDown = false;

        // 1. Bullet evasion (if not invincible)
        if (!player.IsGodMode && player.InvulnerabilityPowerupTimer <= 0.0f && player.InvulnerabilityTimer <= 0.3f)
        {
            for (int i = 0; i < engine.Projectiles.Length; i++)
            {
                ref var p = ref engine.Projectiles[i];
                if (!p.IsActive || p.FromPlayer) continue;

                float dx = p.Position.X - player.Position.X;
                float dy = p.Position.Y - player.Position.Y;

                // Incoming horizontal projectile
                if (MathF.Abs(dx) < 120.0f && ((dx > 0 && p.Velocity.X < 0) || (dx < 0 && p.Velocity.X > 0)))
                {
                    if (MathF.Abs(dy) < 20.0f)
                    {
                        if (dy >= 0) dodgeUp = true;
                        else dodgeDown = true;
                        break;
                    }
                }
            }
        }

        // 2. Combat Target Acquisition
        MonsterEntity? target = FindBestTarget(engine, isFlightMode: true);

        // 3. Health & Pickup Acquisition in Flight
        PickupItem? flightPickup = FindBestPickup(engine, isFlightMode: true);
        bool prioritizeFlightPickup = flightPickup.HasValue && (player.Health <= 6 || flightPickup.Value.Type == PickupType.Invulnerability || target == null);

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

            // Turn towards monster
            if (dx > 0) _isDown[(int)GameAction.MoveRight] = true;
            else if (dx < 0) _isDown[(int)GameAction.MoveLeft] = true;

            // Match vertical altitude
            if (!dodgeUp && !dodgeDown)
            {
                if (dy < -6.0f) _isDown[(int)GameAction.MoveUp] = true;
                else if (dy > 6.0f) _isDown[(int)GameAction.MoveDown] = true;
            }

            // Shoot if aligned, bullets needed, and clear line of sight
            int needed = CalculateRemainingBulletsNeeded(target, engine.Projectiles);
            if (needed > 0 && MathF.Abs(dy) < 14.0f && ((dx > 0 && player.Facing == Direction.Right) || (dx < 0 && player.Facing == Direction.Left)))
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

        // 1. Incoming Bullet Dodging (Jump over ground shots unless God mode / invulnerable)
        bool dodgeJump = false;
        if (!player.IsGodMode && player.InvulnerabilityPowerupTimer <= 0.0f && player.InvulnerabilityTimer <= 0.3f)
        {
            for (int i = 0; i < engine.Projectiles.Length; i++)
            {
                ref var p = ref engine.Projectiles[i];
                if (!p.IsActive || p.FromPlayer) continue;

                float dx = p.Position.X - player.Position.X;
                float dy = p.Position.Y - player.Position.Y;

                // Incoming bullet approaching player within 110px
                if (MathF.Abs(dx) < 110.0f && ((dx > 0 && p.Velocity.X < 0) || (dx < 0 && p.Velocity.X > 0)))
                {
                    if (dy >= -8.0f && dy <= PlayerEntity.BoxHeight + 6.0f)
                    {
                        if (player.IsGrounded)
                        {
                            dodgeJump = true;
                            _jumpHoldTimer = 0.28f;
                            break;
                        }
                    }
                }
            }
        }

        // 2. Ladder Detection & Climbing
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
        bool impassableWallAhead = false;
        int forwardWallCol = playerCol + 1;
        int obstacleHeight = 0;
        int obstacleTopRow = playerFeetRow;

        for (int r = playerFeetRow + 1; r >= Math.Max(0, playerFeetRow - 6); r--)
        {
            TileType t = grid.GetTile(forwardWallCol, r);
            if (t is TileType.Solid or TileType.Crate or TileType.Border)
            {
                obstacleHeight++;
                obstacleTopRow = Math.Min(obstacleTopRow, r);
            }
        }

        // Check if body (head to feet) intersects a solid obstacle in forwardWallCol
        bool bodyBlocked = (grid.GetTile(forwardWallCol, playerHeadRow) is TileType.Solid or TileType.Crate or TileType.Border) ||
                           (grid.GetTile(forwardWallCol, playerFeetRow) is TileType.Solid or TileType.Crate or TileType.Border);

        // Check overhead clearance above obstacle to determine if jumpable
        bool hasOverheadClearance = true;
        for (int r = obstacleTopRow - 1; r >= Math.Max(0, obstacleTopRow - 2); r--)
        {
            if (r < 0 || grid.GetTile(forwardWallCol, r) is TileType.Solid or TileType.Crate or TileType.Border ||
                grid.GetTile(playerCol, r) is TileType.Solid or TileType.Crate or TileType.Border)
            {
                hasOverheadClearance = false;
                break;
            }
        }

        if (bodyBlocked && (obstacleHeight >= 3 || !hasOverheadClearance))
        {
            impassableWallAhead = true;
        }

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
        if (!impassableWallAhead && player.IsGrounded && atFeet1 is TileType.Solid or TileType.Crate && hasOverheadClearance)
        {
            triggerJump = true;
            _jumpHoldTimer = 0.24f;
        }

        // Stuck detection
        if (_stuckTimer > 0.6f && player.IsGrounded)
        {
            if (bodyBlocked || impassableWallAhead)
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

                    // Ensure player faces the target
                    Direction desiredFacing = dx >= 0 ? Direction.Right : Direction.Left;
                    if (player.Facing != desiredFacing)
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
                    else
                    {
                        // Already facing target: advance if far, or hold position to shoot steadily
                        float absDx = MathF.Abs(dx);
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
                        else if (!pursuingHealth)
                        {
                            // In firing range: hold ground and fire
                            _isDown[(int)GameAction.MoveRight] = false;
                            _isDown[(int)GameAction.MoveLeft] = false;
                        }
                    }

                    // Fire if facing target, aligned horizontally, and has clear line of sight
                    bool isFacingTarget = (dx >= 0 && player.Facing == Direction.Right) || (dx < 0 && player.Facing == Direction.Left);
                    if (isFacingTarget)
                    {
                        Vector2f currentGunPos = new(
                            player.Position.X + PlayerEntity.BoxOffset.X + (player.Facing == Direction.Right ? PlayerEntity.BoxWidth + 4.0f : -4.0f),
                            player.Position.Y + 12.0f
                        );
                        Vector2f targetCenter = new(target.Position.X + target.Width * 0.5f, target.Position.Y + target.Height * 0.5f);

                        if (MathF.Abs(dy) <= 16.0f)
                        {
                            if (HasLineOfSight(engine.Grid, currentGunPos, targetCenter))
                            {
                                PerformControlledBurst();
                            }
                        }
                        // If monster is slightly elevated, leap and fire at peak
                        else if (dy < -16.0f && dy >= -40.0f && MathF.Abs(dx) <= 120.0f)
                        {
                            if (player.IsGrounded)
                            {
                                _isDown[(int)GameAction.Jump] = true;
                                _jumpHoldTimer = 0.22f;
                            }
                            if (MathF.Abs(dy) <= 24.0f)
                            {
                                if (HasLineOfSight(engine.Grid, currentGunPos, targetCenter))
                                {
                                    PerformControlledBurst();
                                }
                            }
                        }
                    }
                }
            }
        }

        // 6. Default Horizontal Progression
        if (!inCombatStance && !pursuingHealth && _targetLadderCol < 0)
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

        if (triggerJump)
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
                // Discard monsters that are vertically out of reach (> 45px)
                if (dy > 45.0f) continue;

                Vector2f gunPos = new(
                    pPos.X + PlayerEntity.BoxOffset.X + (dx >= 0 ? PlayerEntity.BoxWidth + 4.0f : -4.0f),
                    pPos.Y + 12.0f
                );

                // Check Line of Sight from standing gun height and jump-peak gun height
                bool hasStandingLOS = HasLineOfSight(engine.Grid, gunPos, mCenter);
                Vector2f jumpGunPos = new(gunPos.X, gunPos.Y - 28.0f);
                bool hasJumpLOS = HasLineOfSight(engine.Grid, jumpGunPos, mCenter);

                if (!hasStandingLOS && !hasJumpLOS)
                {
                    continue; // Completely obstructed by solid wall or crates!
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
