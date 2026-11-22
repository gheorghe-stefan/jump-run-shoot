using System;
using System.Globalization;
using System.Threading;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class CrouchAndPolishTests
{
    [Fact]
    public void Crouch_GravityTheme_EntersTransitionsAndStandsBackUpWhenClear()
    {
        var engine = new GameEngine("CROUCH_BASIC");
        engine.SetTheme(4); // Polar Adventure (gravity)
        int ts = PhysicsConstants.TileSize;

        for (int c = 4; c <= 12; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        engine.Player.Position = new Vector2f(6 * ts, 24 * ts - 24f);
        engine.Player.IsGrounded = true;

        // Hold Down -> enters crouch
        engine.Input.HandleKeyDown("ArrowDown");
        engine.Update(0.016f);

        Assert.True(engine.Player.IsCrouching);
        Assert.Equal(PlayerState.Crouching, engine.Player.State);
        Assert.Equal(PlayerEntity.CrouchBoxHeight, engine.Player.BoundingBox.Height);
        Assert.True(engine.Player.CrouchTimer < PlayerEntity.CrouchTransitionDuration);

        // Hold Down past transition duration -> frozen crouched pose
        for (int f = 0; f < 12; f++) engine.Update(0.016f);
        Assert.True(engine.Player.IsCrouching);
        Assert.True(engine.Player.CrouchTimer >= PlayerEntity.CrouchTransitionDuration);

        // Release Down in open space -> stands back up to full 2-cell height
        engine.Input.HandleKeyUp("ArrowDown");
        engine.Update(0.016f);

        Assert.False(engine.Player.IsCrouching);
        Assert.Equal(PlayerEntity.BoxHeight, engine.Player.BoundingBox.Height);
    }

    [Fact]
    public void Crouch_UnderOneCellTunnel_StaysCrouchedAfterReleasingDown_UntilEmerging()
    {
        var engine = new GameEngine("CROUCH_TUNNEL");
        engine.SetTheme(4); // Polar Adventure
        int ts = PhysicsConstants.TileSize;

        // Clear cols 3..14, floor at row 24, 1-cell-high tunnel roof at row 22 across cols 7..9
        for (int c = 3; c <= 14; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int c = 7; c <= 9; c++) engine.Grid.SetTile(c, 22, TileType.Solid);
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        engine.Player.Position = new Vector2f(6 * ts, 24 * ts - 24f);
        engine.Player.IsGrounded = true;

        // Crouch and crawl right into the 1-cell tunnel (under col 8, row 22 roof)
        engine.Input.HandleKeyDown("ArrowDown");
        engine.Input.HandleKeyDown("ArrowRight");
        for (int f = 0; f < 35; f++) engine.Update(0.016f);

        Assert.True(engine.Player.Position.X > 7 * ts, "Crouched player should slide cleanly into 1-cell tunnel");

        // Release Down while STILL underneath the 1-cell roof: must stay crouched!
        engine.Input.HandleKeyUp("ArrowDown");
        engine.Input.HandleKeyUp("ArrowRight");
        engine.Update(0.016f);
        Assert.True(engine.Player.IsCrouching, "Player must stay crouched while upper cell is blocked by tunnel roof");

        // Continue moving right until past col 9 -> automatically stands back up!
        engine.Input.HandleKeyDown("ArrowRight");
        for (int f = 0; f < 50; f++) engine.Update(0.016f);
        engine.Input.HandleKeyUp("ArrowRight");
        engine.Update(0.016f);

        Assert.True(engine.Player.Position.X >= 10 * ts);
        Assert.False(engine.Player.IsCrouching, "Player should automatically stand up after exiting low tunnel");
    }

    [Fact]
    public void Crouch_ThreeSimultaneousKeys_CrouchMoveAndShootAllWorkTogether()
    {
        var engine = new GameEngine("CROUCH_3KEYS");
        engine.SetTheme(4); // Polar Adventure
        int ts = PhysicsConstants.TileSize;

        for (int c = 3; c <= 14; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int c = 7; c <= 10; c++) engine.Grid.SetTile(c, 22, TileType.Solid);
        engine.Grid.SetTile(9, 23, TileType.Crate); // Shootable box blocking the 1-cell tunnel!

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;

        engine.Player.Position = new Vector2f(6 * ts, 24 * ts - 24f);
        engine.Player.IsGrounded = true;
        float startX = engine.Player.Position.X;

        // Press all 3 keys simultaneously: Down (crouch) + Right (move) + KeyX (shoot)
        engine.Input.HandleKeyDown("ArrowDown");
        engine.Input.HandleKeyDown("ArrowRight");
        engine.Input.HandleKeyDown("KeyX");

        for (int f = 0; f < 25; f++) engine.Update(0.016f);

        Assert.True(engine.Player.IsCrouching, "Player must stay crouched while moving and shooting");
        Assert.True(engine.Player.Position.X > startX + 8f, "Crouched player must move right while holding Down + Right + Shoot");
        Assert.Equal(TileType.Empty, engine.Grid.GetTile(9, 23));
    }

    [Fact]
    public void Crouch_DodgesHeavyGuardTwoCellMonsterBullet_OnSamePlatform()
    {
        var engine = new GameEngine("CROUCH_DODGE_BEAR");
        engine.SetTheme(4); // Polar Adventure (gravity)
        int ts = PhysicsConstants.TileSize;
        float platformTop = 24 * ts;

        for (int c = 3; c <= 14; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;

        engine.Player.Position = new Vector2f(5 * ts, platformTop - 24f);
        engine.Player.IsGrounded = true;
        engine.Player.Health = 8;
        engine.Input.HandleKeyDown("ArrowDown"); // Crouch!

        var bear = engine.Monsters[0];
        bear.Spawn(0, MonsterType.HeavyGuard, new Vector2f(10 * ts, platformTop - 26f), platformY: platformTop);
        bear.Facing = Direction.Left;
        bear.Velocity = Vector2f.Zero;
        bear.ActionTimer = 0.001f;

        // Let bear shoot and let bullet travel past crouched player's position
        for (int f = 0; f < 45; f++) engine.Update(0.016f);

        Assert.True(engine.Player.IsCrouching);
        Assert.Equal(8, engine.Player.Health);
    }

    [Theory]
    [InlineData(2)] // Ocean (ZeroGravity)
    [InlineData(3)] // Space Invaders (ZeroGravity)
    public void Crouch_IsDisabledInZeroGravityThemes(int themeIdx)
    {
        var engine = new GameEngine("NO_CROUCH_ZEROG");
        engine.SetTheme(themeIdx);
        engine.Player.IsGrounded = true;

        engine.Input.HandleKeyDown("ArrowDown");
        for (int f = 0; f < 5; f++) engine.Update(0.016f);

        Assert.False(engine.Player.IsCrouching, $"Crouch must be disabled in zero-gravity theme {themeIdx}");
    }

    [Fact]
    public void ClimbingLadder_AnimTimerAdvancesOnlyWhenPressingUpOrDown()
    {
        var gen = new WorldGenerator("CLIMB_PAUSE_TEST");
        var grid = new TileGrid(gen);
        var input = new InputManager();

        for (int r = 10; r <= 20; r++) grid.SetTile(5, r, TileType.Ladder);
        var player = new PlayerEntity(new Vector2f(5 * 16, 15 * 16));
        player.IsGrounded = false;

        // Climb up -> ClimbAnimTimer advances
        input.HandleKeyDown("ArrowUp");
        input.AdvanceFrame();
        player.Update(0.1f, input, grid);
        Assert.True(player.IsClimbing);
        float afterMove = player.ClimbAnimTimer;
        Assert.True(afterMove > 0.0f);

        // Release Up -> holding still on ladder must NOT advance ClimbAnimTimer!
        input.HandleKeyUp("ArrowUp");
        input.AdvanceFrame();
        player.Update(0.25f, input, grid);
        Assert.True(player.IsClimbing);
        Assert.Equal(afterMove, player.ClimbAnimTimer);
    }

    [Fact]
    public void GravityThemes_LoadCrouchSprites_AndPolarLoadsShootFrame()
    {
        foreach (int idx in new[] { 0, 1, 4 }) // Duke, Forest, Polar
        {
            var assets = ThemeRegistry.AllThemes[idx].Assets;
            Assert.NotNull(assets.PlayerCrouch);
            Assert.Equal(3, assets.PlayerCrouch!.Length);
            // Crouch-walk step frame (2) must differ from idle crouch frame (1) for visible feet animation
            Assert.NotEqual(assets.PlayerCrouch[1].Pixels, assets.PlayerCrouch[2].Pixels);
        }

        var polar = new PolarAdventureTheme();
        Assert.NotNull(polar.Assets.PlayerShoot);

        var engine = new GameEngine("SHOOT_ANIM_TEST");
        engine.SetTheme(4);
        engine.Player.PendingShoot = true;
        engine.Update(0.016f);
        Assert.True(engine.Player.ShootAnimTimer > 0.0f);
    }

    [Fact]
    public void CrouchWalking_AdvancesCrouchWalkAnimTimer_AndAlternatesFeetFrames()
    {
        var engine = new GameEngine("CROUCH_WALK_ANIM");
        engine.SetTheme(4); // Polar Adventure
        int ts = PhysicsConstants.TileSize;

        for (int c = 2; c <= 14; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        engine.Player.Position = new Vector2f(4 * ts, 24 * ts - 24f);
        engine.Player.IsGrounded = true;

        // Hold Down stationary past crouch transition
        engine.Input.HandleKeyDown("ArrowDown");
        for (int i = 0; i < 15; i++) engine.Update(0.016f);

        Assert.True(engine.Player.IsCrouching);
        Assert.Equal(0.0f, engine.Player.CrouchWalkAnimTimer);

        // Render crouch-idle frame
        var polarAssets = ThemeRegistry.AllThemes[4].Assets;
        var fbIdle = new FrameBuffer();
        GameRenderer.DrawPlayer(fbIdle, polarAssets, engine.Player, 40, 40);

        // Hold Right while crouched to crouch-walk until stride B triggers (phase 1 at >= 1/9s)
        engine.Input.HandleKeyDown("ArrowRight");
        for (int i = 0; i < 10; i++) engine.Update(0.016f);

        Assert.True(engine.Player.IsCrouching);
        Assert.True(engine.Player.CrouchWalkAnimTimer > 0.11f, $"Expected CrouchWalkAnimTimer to advance while crouch-walking, got {engine.Player.CrouchWalkAnimTimer}");

        var fbWalk = new FrameBuffer();
        GameRenderer.DrawPlayer(fbWalk, polarAssets, engine.Player, 40, 40);
        Assert.NotEqual(fbIdle.Pixels, fbWalk.Pixels);

        // Releasing Right freezes back to idle crouch frame
        engine.Input.HandleKeyUp("ArrowRight");
        for (int i = 0; i < 15; i++) engine.Update(0.016f);
        Assert.Equal(0.0f, engine.Player.CrouchWalkAnimTimer);
    }

    [Fact]
    public void MechaHound_Elk_NeverSpawnsEmbeddedInSolidBlockAcrossSeeds()
    {
        var seeds = new[] { "FOO", "DEMO", "ELK_SAFE_1", "POLAR_ICE_88", "OUTPOST_ALPHA", "SUPPLY_007" };
        int ts = PhysicsConstants.TileSize;

        foreach (var seed in seeds)
        {
            var engine = new GameEngine(seed);
            engine.SetTheme(4);
            for (int c = 0; c <= 20; c++) engine.PopulateChunkEntities(c);

            for (int m = 0; m < engine.Monsters.Length; m++)
            {
                var monster = engine.Monsters[m];
                if (!monster.IsActive || monster.Type != MonsterType.MechaHound) continue;

                int minCol = (int)MathF.Floor((monster.Position.X + 0.5f) / ts);
                int maxCol = (int)MathF.Floor((monster.Position.X + monster.Width - 0.5f) / ts);
                int row = (int)MathF.Floor((monster.Position.Y + monster.Height * 0.5f) / ts);

                for (int c = minCol; c <= maxCol; c++)
                {
                    TileType t = engine.Grid.GetTile(c, row);
                    Assert.False(t is TileType.Solid or TileType.Border or TileType.Crate,
                        $"Seed '{seed}': Elk spawned embedded in {t} at ({c},{row})!");
                }
            }
        }
    }

    [Fact]
    public void WorldGenerator_GeneratesCrouchCrawlwaysWithOpenBypassAbove()
    {
        var gen = new WorldGenerator("CROUCH_LEVEL_GEN");
        int crouchTunnelColumns = 0;

        for (int chunkIdx = 1; chunkIdx <= 30; chunkIdx++)
        {
            var chunk = gen.GenerateChunk(chunkIdx);
            for (int c = 2; c < TileChunk.ChunkWidth - 2; c++)
            {
                // Find bedrock floor top
                int floorRow = TileChunk.ChunkHeight - 1;
                while (floorRow > 0 && chunk.GetTile(c, floorRow - 1) == TileType.Solid) floorRow--;

                if (floorRow >= 5 &&
                    chunk.GetTile(c, floorRow - 1) is TileType.Empty or TileType.Crate &&
                    chunk.GetTile(c, floorRow - 2) == TileType.Solid &&
                    chunk.GetTile(c, floorRow - 3) != TileType.Solid &&
                    chunk.GetTile(c, floorRow - 4) != TileType.Solid)
                {
                    crouchTunnelColumns++;
                }
            }
        }

        Assert.True(crouchTunnelColumns >= 12, $"Expected intentional crouch crawlways across 30 chunks, found only {crouchTunnelColumns} tunnel tiles.");
    }

    [Fact]
    public void AirCrouchAndCrouchJump_AllowsEnteringElevatedOneCellTunnel_WhileDownJumpDropsThroughOneWay()
    {
        // Build step-up elevated 1-cell tunnel:
        // cols 2..5: ground at row 24
        // cols 6..10: elevated tunnel floor at row 23, 1-cell tunnel gap at row 22, solid roof at row 21
        var engine = new GameEngine("AIR_CROUCH_TUNNEL");
        engine.SetTheme(4); // Polar Adventure
        int ts = PhysicsConstants.TileSize;
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        for (int c = 2; c <= 13; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
            engine.Grid.SetTile(c, 25, TileType.Solid);
        }
        for (int c = 6; c <= 10; c++)
        {
            engine.Grid.SetTile(c, 23, TileType.Solid);
            engine.Grid.SetTile(c, 21, TileType.Solid);
        }

        // 1) Crouch-jump from solid ground (holding Down + Jump + Right) clears 1-tile step and enters elevated 1-cell tunnel!
        engine.Player.Position = new Vector2f(4 * ts + 4, 24 * ts - 24f);
        engine.Player.IsGrounded = true;

        engine.Input.HandleKeyDown("ArrowDown");
        engine.Input.HandleKeyDown("ArrowRight");
        engine.Input.HandleKeyDown("Space");
        engine.Update(0.016f);
        Assert.True(engine.Player.IsCrouching, "Down + Jump on solid ground should crouch-jump");
        Assert.True(engine.Player.Velocity.Y < 0, "Down + Jump on solid ground should jump upward");

        // Hold Space for a normal short hop (~10 frames) before releasing, while continuing Down + Right into the 1-cell tunnel
        for (int f = 0; f < 10; f++) engine.Update(0.016f);
        engine.Input.HandleKeyUp("Space");
        for (int f = 0; f < 40; f++) engine.Update(0.016f);

        Assert.True(engine.Player.Position.X >= 6 * ts, $"Crouch-jumping should enter elevated 1-cell tunnel (X={engine.Player.Position.X:F1})");
        Assert.True(engine.Player.IsCrouching, "Player should stay crouched inside elevated 1-cell tunnel");

        // 1b) Standard jump followed by mid-air Down tuck ALSO enters the elevated 1-cell tunnel!
        engine.Input.HandleKeyUp("ArrowDown");
        engine.Input.HandleKeyUp("ArrowRight");
        engine.Player.Position = new Vector2f(4 * ts + 4, 24 * ts - 24f);
        engine.Player.Velocity = Vector2f.Zero;
        engine.Player.IsCrouching = false;
        engine.Player.IsGrounded = true;

        engine.Input.HandleKeyDown("ArrowRight");
        engine.Input.HandleKeyDown("Space");
        for (int f = 0; f < 4; f++) engine.Update(0.016f);
        // Mid-air: press Down to tuck into 1-cell crouch while jumping toward the tunnel!
        engine.Input.HandleKeyDown("ArrowDown");
        for (int f = 0; f < 8; f++) engine.Update(0.016f);
        engine.Input.HandleKeyUp("Space");
        for (int f = 0; f < 40; f++) engine.Update(0.016f);

        Assert.True(engine.Player.Position.X >= 6 * ts, $"Mid-air crouch tuck should enter elevated 1-cell tunnel (X={engine.Player.Position.X:F1})");
        Assert.True(engine.Player.IsCrouching, "Player should land crouched inside elevated 1-cell tunnel");

        // 2) Down + Jump on a OneWayPlatform still drops through!
        var engine2 = new GameEngine("ONEWAY_DROP");
        engine2.SetTheme(4);
        for (int i = 0; i < engine2.Monsters.Length; i++) engine2.Monsters[i].IsActive = false;
        for (int c = 2; c <= 10; c++)
        {
            for (int r = 14; r < 24; r++) engine2.Grid.SetTile(c, r, TileType.Empty);
            engine2.Grid.SetTile(c, 18, TileType.OneWayPlatform);
            engine2.Grid.SetTile(c, 24, TileType.Solid);
        }
        engine2.Player.Position = new Vector2f(5 * ts, 18 * ts - 24f);
        engine2.Player.IsGrounded = true;

        engine2.Input.HandleKeyDown("ArrowDown");
        engine2.Input.HandleKeyDown("Space");
        engine2.Update(0.016f);
        Assert.True(engine2.Player.DropThroughTimer > 0f, "Down + Jump on OneWayPlatform must still trigger drop-through");
        Assert.True(engine2.Player.Velocity.Y > 0f);
    }

    [Fact]
    public void CeilingTurret_Seals_NeverSpawnStackedAtSameLocationAcrossSeeds()
    {
        var seeds = new[] { "FOO", "DEMO", "SEAL_STACK_1", "POLAR_ICE", "OUTPOST_99", "RAVINE_RUN", "SUPPLY_7", "SNOW_2026" };
        int totalTurrets = 0;

        foreach (var seed in seeds)
        {
            var engine = new GameEngine(seed);
            engine.SetTheme(4);
            for (int c = 0; c <= 20; c++) engine.PopulateChunkEntities(c);

            var turrets = new System.Collections.Generic.List<MonsterEntity>();
            for (int i = 0; i < engine.Monsters.Length; i++)
            {
                if (engine.Monsters[i].IsActive && engine.Monsters[i].Type == MonsterType.CeilingTurret)
                {
                    turrets.Add(engine.Monsters[i]);
                }
            }

            totalTurrets += turrets.Count;
            for (int i = 0; i < turrets.Count; i++)
            {
                for (int j = i + 1; j < turrets.Count; j++)
                {
                    float dx = MathF.Abs(turrets[i].Position.X - turrets[j].Position.X);
                    float dy = MathF.Abs(turrets[i].Position.Y - turrets[j].Position.Y);
                    Assert.False(dx < 24.0f && dy < 20.0f,
                        $"Seed '{seed}': Two CeilingTurrets (Seals) spawned stacked at ({turrets[i].Position.X},{turrets[i].Position.Y}) and ({turrets[j].Position.X},{turrets[j].Position.Y})!");
                }
            }
        }

        Assert.True(totalTurrets > 15, $"Expected to check many CeilingTurrets across seeds, found only {totalTurrets}");
    }

    [Fact]
    public void InvulnerabilityCounter_UsesDotSeparatorAndMinimizesWithoutOverlappingNotifications()
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            // Force a comma-decimal culture (e.g. de-DE) to verify dot separator is strictly enforced
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            string full = GameRenderer.FormatInvulnerabilityCounterText(9.5f, minimized: false);
            string compact = GameRenderer.FormatInvulnerabilityCounterText(9.5f, minimized: true);

            Assert.Equal("SHIELD 9.5s", full);
            Assert.Equal("9.5s", compact);
            Assert.Contains(".", full);
            Assert.DoesNotContain(",", full);
            Assert.Contains(".", compact);
            Assert.DoesNotContain(",", compact);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }

        // Full counter (no notification) is wider than minimized counter (notification active)
        var fullRect = GameRenderer.GetInvulnerabilityCounterRect(9.5f, hasActiveNotification: false, notificationRightEdge: 0);
        var compactShortRect = GameRenderer.GetInvulnerabilityCounterRect(9.5f, hasActiveNotification: true, notificationRightEdge: 220);
        var compactWideRect = GameRenderer.GetInvulnerabilityCounterRect(9.5f, hasActiveNotification: true, notificationRightEdge: 300);

        Assert.True(compactShortRect.Width < fullRect.Width, "Counter must minimize when a notification is active");
        Assert.Equal(316, compactShortRect.X + compactShortRect.Width); // right-aligned
        Assert.Equal(20, compactShortRect.Y); // sits cleanly beside short banner on row y=20

        // For wide banners reaching past the minimized counter X, counter drops below banner (y=36) so it NEVER overlaps!
        Assert.Equal(316, compactWideRect.X + compactWideRect.Width);
        Assert.True(compactWideRect.Y >= 34, $"Wide banner (y=20..34) must not overlap minimized counter (counterY={compactWideRect.Y})");
    }
}
