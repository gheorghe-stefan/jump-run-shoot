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

    // Commitment and anti-stuck tracking
    private float _directionCommitTimer = 0.0f;
    private int _committedDirection = 1; // 1 = right, -1 = left
    private float _stuckTimer = 0.0f;
    private float _lastX = 0.0f;
    private float _burstCooldown = 0.0f;
    private int _burstShotsFired = 0;
    private const int MaxBurstShots = 3;

    public void StartDemoMode()
    {
        IsActive = false;
        CountdownTimer = 5.0f;
        IsConfirmationOpen = false;
        _directionCommitTimer = 0.0f;
        _committedDirection = 1;
        _stuckTimer = 0.0f;
        _lastX = 0.0f;
        _burstCooldown = 0.0f;
        _burstShotsFired = 0;
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
        var grid = engine.Grid;

        // Anti-stuck tracking
        if (MathF.Abs(player.Position.X - _lastX) < 4.0f)
        {
            _stuckTimer += dt;
        }
        else
        {
            _stuckTimer = 0.0f;
            _lastX = player.Position.X;
        }

        // Direction commitment update
        if (_directionCommitTimer > 0.0f)
        {
            _directionCommitTimer -= dt;
        }

        // Burst cooldown update
        if (_burstCooldown > 0.0f)
        {
            _burstCooldown -= dt;
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
        MonsterEntity? target = FindBestTarget(engine);
        if (target != null)
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

            // Shoot if aligned and bullets needed
            int needed = CalculateRemainingBulletsNeeded(target, engine.Projectiles);
            if (needed > 0 && MathF.Abs(dy) < 14.0f && ((dx > 0 && player.Facing == Direction.Right) || (dx < 0 && player.Facing == Direction.Left)))
            {
                PerformControlledBurst();
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

        int playerCol = (int)MathF.Floor((player.Position.X + PlayerEntity.BoxOffset.X + PlayerEntity.BoxWidth * 0.5f) / ts);
        int playerFeetRow = (int)MathF.Floor((player.Position.Y + PlayerEntity.BoxOffset.Y + PlayerEntity.BoxHeight - 1.0f) / ts);
        int playerHeadRow = (int)MathF.Floor((player.Position.Y + PlayerEntity.BoxOffset.Y) / ts);

        bool dodgeJump = false;

        // 1. Incoming Bullet Dodging (Jump over ground shots unless God mode / invulnerable)
        if (!player.IsGodMode && player.InvulnerabilityPowerupTimer <= 0.0f && player.InvulnerabilityTimer <= 0.3f)
        {
            for (int i = 0; i < engine.Projectiles.Length; i++)
            {
                ref var p = ref engine.Projectiles[i];
                if (!p.IsActive || p.FromPlayer) continue;

                float dx = p.Position.X - player.Position.X;
                float dy = p.Position.Y - player.Position.Y;

                // Incoming bullet approaching player within 100px
                if (MathF.Abs(dx) < 100.0f && ((dx > 0 && p.Velocity.X < 0) || (dx < 0 && p.Velocity.X > 0)))
                {
                    // Bullet at body/foot height
                    if (dy >= -6.0f && dy <= PlayerEntity.BoxHeight + 4.0f)
                    {
                        if (player.IsGrounded)
                        {
                            dodgeJump = true;
                            break;
                        }
                    }
                }
            }
        }

        // 2. Ladder Detection & Vertical Navigation
        bool touchesLadder = false;
        int ladderMinCol = (int)MathF.Floor((player.Position.X + PlayerEntity.BoxOffset.X) / ts);
        int ladderMaxCol = (int)MathF.Floor((player.Position.X + PlayerEntity.BoxOffset.X + PlayerEntity.BoxWidth - 0.001f) / ts);
        int ladderMinRow = (int)MathF.Floor((player.Position.Y + PlayerEntity.BoxOffset.Y) / ts);
        int ladderMaxRow = (int)MathF.Floor((player.Position.Y + PlayerEntity.BoxOffset.Y + PlayerEntity.BoxHeight - 0.001f) / ts);

        for (int r = ladderMinRow; r <= ladderMaxRow; r++)
        {
            for (int c = ladderMinCol; c <= ladderMaxCol; c++)
            {
                if (grid.GetTile(c, r) == TileType.Ladder)
                {
                    touchesLadder = true;
                    break;
                }
            }
            if (touchesLadder) break;
        }

        // Check path ahead for obstacles or dead ends
        int facingCol = playerCol + _committedDirection;
        bool wallAhead = false;
        int wallHeight = 0;

        for (int r = playerFeetRow; r >= playerHeadRow - 2; r--)
        {
            TileType t = grid.GetTile(facingCol, r);
            if (t is TileType.Solid or TileType.Crate or TileType.Border)
            {
                wallHeight++;
            }
        }
        if (wallHeight >= 2) wallAhead = true;

        // If touching a ladder and facing an impassable wall ahead, climb UP!
        if (touchesLadder && (wallAhead || player.IsClimbing))
        {
            // Check if we reached the top of the ladder
            TileType aboveLadder = grid.GetTile(playerCol, playerHeadRow - 1);
            if (aboveLadder != TileType.Ladder && aboveLadder != TileType.Solid)
            {
                // Dismount ladder onto platform
                _isDown[(int)GameAction.MoveRight] = true;
                _isDown[(int)GameAction.Jump] = true;
            }
            else
            {
                _isDown[(int)GameAction.MoveUp] = true;
                ComputeEdgeEvents();
                return;
            }
        }

        // 3. Terrain Lookahead (Holes, Pits, Spikes)
        bool needJump = dodgeJump;
        TileType belowAhead = grid.GetTile(facingCol, playerFeetRow + 1);
        TileType atFeetAhead = grid.GetTile(facingCol, playerFeetRow);

        // Gap / pit / hazard detection ahead
        if (belowAhead is TileType.Empty or TileType.Hazard && player.IsGrounded)
        {
            needJump = true;
        }
        // Low obstacle hop (1 block step)
        if (atFeetAhead is TileType.Solid or TileType.Crate && grid.GetTile(facingCol, playerHeadRow) is TileType.Empty)
        {
            needJump = true;
        }

        // Stuck recovery
        if (_stuckTimer > 1.0f)
        {
            needJump = true;
            if (_stuckTimer > 2.0f)
            {
                // Reverse direction temporarily to escape dead-end
                _committedDirection = -_committedDirection;
                _directionCommitTimer = 0.8f;
                _stuckTimer = 0.0f;
            }
        }

        // Horizontal Movement
        if (_directionCommitTimer <= 0.0f)
        {
            // Default rightward progression unless stuck
            _committedDirection = 1;
        }

        if (_committedDirection > 0)
        {
            _isDown[(int)GameAction.MoveRight] = true;
        }
        else
        {
            _isDown[(int)GameAction.MoveLeft] = true;
        }

        // Run when clearing gaps or invulnerable
        if (needJump || player.IsGodMode || player.InvulnerabilityPowerupTimer > 0.0f)
        {
            _isDown[(int)GameAction.Run] = true;
        }

        if (needJump)
        {
            _isDown[(int)GameAction.Jump] = true;
        }

        // 4. Combat & Shooting
        MonsterEntity? target = FindBestTarget(engine);
        if (target != null)
        {
            float dx = target.Position.X - player.Position.X;
            float dy = target.Position.Y - player.Position.Y;

            // Turn to face monster if close
            if (MathF.Abs(dx) < 140.0f)
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

            int needed = CalculateRemainingBulletsNeeded(target, engine.Projectiles);
            if (needed > 0)
            {
                // Monster is directly in horizontal line of sight
                if (MathF.Abs(dy) < 14.0f)
                {
                    if ((dx > 0 && player.Facing == Direction.Right) || (dx < 0 && player.Facing == Direction.Left))
                    {
                        PerformControlledBurst();
                    }
                }
                // Monster is elevated: jump and fire at peak!
                else if (dy < -14.0f && dy > -45.0f && MathF.Abs(dx) < 120.0f)
                {
                    if (player.IsGrounded)
                    {
                        _isDown[(int)GameAction.Jump] = true;
                    }
                    if (MathF.Abs(dy) < 24.0f)
                    {
                        PerformControlledBurst();
                    }
                }
            }
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
            _burstCooldown = 0.22f; // Organic pause between controlled volleys
        }
    }

    private MonsterEntity? FindBestTarget(GameEngine engine)
    {
        MonsterEntity? best = null;
        float bestDist = 200.0f;
        var pPos = engine.Player.Position;

        for (int i = 0; i < engine.Monsters.Length; i++)
        {
            var m = engine.Monsters[i];
            if (!m.IsActive || m.Health <= 0) continue;

            float dist = MathF.Abs(m.Position.X - pPos.X);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = m;
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
