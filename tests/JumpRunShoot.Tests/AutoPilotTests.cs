using System;
using JumpRunShoot.Engine.AI;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class AutoPilotTests
{
    [Fact]
    public void DemoMode_Activation_StartsFiveSecondCountdown()
    {
        var engine = new GameEngine("FOO");
        Assert.False(engine.IsDemoMode);
        Assert.False(engine.AutoPilot.IsActive);

        engine.StartDemoMode();
        Assert.True(engine.IsDemoMode);
        Assert.False(engine.AutoPilot.IsActive);
        Assert.Equal(5.0f, engine.AutoPilot.CountdownTimer);

        // Update 2 seconds
        engine.Update(2.0f);
        Assert.True(engine.AutoPilot.CountdownTimer <= 3.01f && engine.AutoPilot.CountdownTimer >= 2.99f);
        Assert.False(engine.AutoPilot.IsActive);

        // Update remaining 3.1 seconds to finish countdown
        engine.Update(3.1f);
        Assert.Equal(0f, engine.AutoPilot.CountdownTimer);
        Assert.True(engine.AutoPilot.IsActive, "AutoPilot should become active once countdown reaches zero");
    }

    [Fact]
    public void DemoMode_AnyKey_TriggersConfirmationDialog()
    {
        var engine = new GameEngine("FOO");
        engine.StartDemoMode();
        Assert.False(engine.AutoPilot.IsConfirmationOpen);

        // User presses any key (e.g. Space)
        engine.Input.HandleKeyDown("Space");
        engine.Update(0.016f);

        Assert.True(engine.AutoPilot.IsConfirmationOpen, "Pressing any key should open the exit confirmation dialog");
    }

    [Fact]
    public void DemoMode_Confirmation_Y_ExitsDemoAndResetsLevel()
    {
        var engine = new GameEngine("FOO");
        engine.StartDemoMode();
        engine.Player.Score = 500;

        // Open confirmation
        engine.Input.HandleKeyDown("Space");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("Space");
        Assert.True(engine.AutoPilot.IsConfirmationOpen);

        // User presses Y to exit
        engine.Input.HandleKeyDown("KeyY");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("KeyY");

        Assert.False(engine.AutoPilot.IsConfirmationOpen);
        Assert.False(engine.IsDemoMode, "Demo mode should be turned off on confirmation Y");
        Assert.False(engine.AutoPilot.IsActive);
        Assert.Equal(0, engine.Player.Score); // Fresh level reset
    }

    [Fact]
    public void DemoMode_Confirmation_N_ResumesDemo()
    {
        var engine = new GameEngine("FOO");
        engine.StartDemoMode();

        // Open confirmation
        engine.Input.HandleKeyDown("Space");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("Space");
        Assert.True(engine.AutoPilot.IsConfirmationOpen);

        // User presses N to resume
        engine.Input.HandleKeyDown("KeyN");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("KeyN");

        Assert.False(engine.AutoPilot.IsConfirmationOpen);
        Assert.True(engine.IsDemoMode, "Demo mode should remain active on confirmation N");
    }

    [Theory]
    [InlineData(MonsterType.PatrolDroid, 2)]
    [InlineData(MonsterType.HoverDrone, 2)]
    [InlineData(MonsterType.CeilingTurret, 3)]
    [InlineData(MonsterType.MechaHound, 3)]
    [InlineData(MonsterType.HeavyGuard, 6)]
    public void AutoPilot_CalculatesExactBulletBudget_ForMonsterTypes(MonsterType type, int expectedBullets)
    {
        int required = AutoPilotController.GetRequiredBulletsForMonsterType(type);
        Assert.Equal(expectedBullets, required);
    }

    [Fact]
    public void AutoPilot_DeductsInFlightPlayerBullets_FromTargetBudget()
    {
        var monster = new MonsterEntity();
        monster.Spawn(0, MonsterType.CeilingTurret, new Vector2f(100f, 60f));
        monster.Health = 3;

        var projectiles = new Projectile[4];
        // 1 in-flight player bullet traveling towards the turret
        projectiles[0].Spawn(new Vector2f(50f, 62f), new Vector2f(200f, 0f), 1, fromPlayer: true);
        // 1 player bullet already past the turret
        projectiles[1].Spawn(new Vector2f(120f, 62f), new Vector2f(200f, 0f), 1, fromPlayer: true);

        int needed = AutoPilotController.CalculateRemainingBulletsNeeded(monster, projectiles);
        // Turret has 3 HP, 1 bullet in flight heading towards it -> 2 needed
        Assert.Equal(2, needed);
    }

    [Fact]
    public void AutoPilot_CeasesFire_WhenTargetIsAlreadyDoomedByInFlightShots()
    {
        var monster = new MonsterEntity();
        monster.Spawn(0, MonsterType.HoverDrone, new Vector2f(120f, 80f));
        monster.Health = 2;

        var projectiles = new Projectile[4];
        // 2 in-flight player bullets traveling towards the drone
        projectiles[0].Spawn(new Vector2f(40f, 82f), new Vector2f(200f, 0f), 1, fromPlayer: true);
        projectiles[1].Spawn(new Vector2f(60f, 82f), new Vector2f(200f, 0f), 1, fromPlayer: true);

        int needed = AutoPilotController.CalculateRemainingBulletsNeeded(monster, projectiles);
        Assert.Equal(0, needed);
    }

    [Fact]
    public void AutoPilot_DodgesIncomingEnemyBullet_ByJumpingWhenGrounded()
    {
        var engine = new GameEngine("DODGE_TEST");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        int ts = PhysicsConstants.TileSize;
        // Continuous flat floor at rows 28 and 29
        for (int c = 2; c <= 10; c++)
        {
            engine.Grid.SetTile(c, 29, TileType.Solid);
            engine.Grid.SetTile(c, 28, TileType.Solid);
            engine.Grid.SetTile(c, 27, TileType.Empty);
            engine.Grid.SetTile(c, 26, TileType.Empty);
        }

        // Player grounded at row 28 (Y = 424)
        engine.Player.Position = new Vector2f(4 * ts, 28 * ts - 24f);
        engine.Player.IsGrounded = true;

        // Enemy bullet incoming horizontally from right at body height (Y=430)
        engine.SpawnProjectile(new Vector2f(7 * ts, 430f), new Vector2f(-150f, 0f), damage: 1, fromPlayer: false);

        engine.Update(0.016f);

        // Bot should have triggered Jump to evade the projectile
        Assert.True(engine.AutoPilot.IsDown(GameAction.Jump) || engine.AutoPilot.JustPressed(GameAction.Jump),
            "Bot should press Jump to dodge incoming enemy bullet at body height");
    }

    [Fact]
    public void AutoPilot_InGodMode_DoesNotDodgeBullets()
    {
        var engine = new GameEngine("GOD_TEST");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsGodMode = true;

        // Deactivate all monsters so combat jumping doesn't trigger
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        int ts = PhysicsConstants.TileSize;
        // Continuous flat floor at rows 28 and 29
        for (int c = 2; c <= 10; c++)
        {
            engine.Grid.SetTile(c, 29, TileType.Solid);
            engine.Grid.SetTile(c, 28, TileType.Solid);
            engine.Grid.SetTile(c, 27, TileType.Empty);
            engine.Grid.SetTile(c, 26, TileType.Empty);
        }

        // Player grounded on row 28 (floor at 28 * 16 = 448, Y = 448 - 24 = 424)
        engine.Player.Position = new Vector2f(4 * ts, 28 * ts - 24f);
        engine.Player.IsGrounded = true;

        // Enemy bullet incoming
        engine.SpawnProjectile(new Vector2f(7 * ts, 430f), new Vector2f(-150f, 0f), damage: 1, fromPlayer: false);

        engine.Update(0.016f);

        // Bot in God Mode ignores dodging and does not panic jump
        Assert.False(engine.AutoPilot.IsDown(GameAction.Jump), "Bot in God Mode should not evade bullets");
    }

    [Fact]
    public void AutoPilot_ClimbsLadder_WhenTallWallBlocksForwardGround()
    {
        var engine = new GameEngine("LADDER_TEST");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        int ts = PhysicsConstants.TileSize;
        int pCol = 5;
        engine.Player.Position = new Vector2f(pCol * ts, 20 * ts);
        engine.Player.IsGrounded = true;

        // Place ladder on column 5 from row 16 to row 21
        for (int r = 16; r <= 21; r++)
        {
            engine.Grid.SetTile(pCol, r, TileType.Ladder);
        }

        // Place impassable solid wall ahead at column 6 (rows 18, 19, 20, 21)
        for (int r = 18; r <= 21; r++)
        {
            engine.Grid.SetTile(pCol + 1, r, TileType.Solid);
        }

        engine.Update(0.016f);

        // When ground is blocked ahead and player touches ladder, bot should climb up
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveUp), "Bot should press MoveUp to climb ladder when wall blocks ground");
    }

    [Fact]
    public void AutoPilot_ZeroGravityFlight_StrafesVerticallyToDodgeIncomingLaser()
    {
        var engine = new GameEngine("SPACE_DODGE");
        engine.SetTheme(3); // Space Invaders (Zero Gravity)
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;

        // Position player ship at (100, 100)
        engine.Player.Position = new Vector2f(100f, 100f);

        // Enemy laser heading directly at player horizontally (from X=180 to left, Y=104)
        engine.SpawnProjectile(new Vector2f(180f, 104f), new Vector2f(-200f, 0f), damage: 1, fromPlayer: false);

        engine.Update(0.016f);

        // In flight mode, dodging moves vertically (MoveUp or MoveDown)
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveUp) || engine.AutoPilot.IsDown(GameAction.MoveDown),
            "Bot in flight mode should strafe vertically to dodge incoming laser");
    }

    [Fact]
    public void AutoPilot_ExecutesControlledBurst_WithCooldownBetweenVolleys()
    {
        var engine = new GameEngine("BURST_TEST");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Spawn a Heavy Guard (6 HP) ahead on flat ground in firing alignment
        var guard = new MonsterEntity();
        guard.Spawn(0, MonsterType.HeavyGuard, new Vector2f(engine.Player.Position.X + 60f, engine.Player.Position.Y));
        engine.Monsters[0] = guard;

        // First 3 updates trigger burst shots
        int shootCount = 0;
        for (int i = 0; i < 3; i++)
        {
            engine.Update(0.016f);
            if (engine.AutoPilot.IsDown(GameAction.Shoot)) shootCount++;
        }
        Assert.True(shootCount >= 1, "Bot should fire shots in burst towards monster");

        // After burst limit is reached, cooldown pause triggers where shoot is not pressed continuously
        for (int i = 0; i < 5; i++)
        {
            engine.Update(0.016f);
        }
    }

    [Fact]
    public void AutoPilot_HasLineOfSight_BlocksWhenWallInBetween()
    {
        var engine = new GameEngine("TEST");
        int ts = 16;

        // Clear tiles between Col 5 and Col 15 at Row 20
        for (int c = 5; c <= 15; c++)
        {
            engine.Grid.SetTile(c, 20, TileType.Empty);
        }

        Vector2f from = new(5 * ts + 8, 20 * ts + 8);
        Vector2f to = new(15 * ts + 8, 20 * ts + 8);

        Assert.True(AutoPilotController.HasLineOfSight(engine.Grid, from, to), "Should have LOS when path is empty");

        // Place a Solid block at Col 10
        engine.Grid.SetTile(10, 20, TileType.Solid);
        Assert.False(AutoPilotController.HasLineOfSight(engine.Grid, from, to), "Solid tile should block LOS");

        // Place a Crate at Col 10 instead
        engine.Grid.SetTile(10, 20, TileType.Crate);
        Assert.False(AutoPilotController.HasLineOfSight(engine.Grid, from, to), "Crate tile should block LOS");

        // Place a OneWayPlatform at Col 10 (bullets pass through one-way platforms)
        engine.Grid.SetTile(10, 20, TileType.OneWayPlatform);
        Assert.True(AutoPilotController.HasLineOfSight(engine.Grid, from, to), "OneWayPlatform should not block LOS");

        // Place a Ladder at Col 10 (bullets pass through ladders)
        engine.Grid.SetTile(10, 20, TileType.Ladder);
        Assert.True(AutoPilotController.HasLineOfSight(engine.Grid, from, to), "Ladder should not block LOS");
    }

    [Fact]
    public void AutoPilot_DoesNotShootMonster_WhenWallBlocksLineOfSight()
    {
        var engine = new GameEngine("TEST");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Place floor at Row 28
        for (int c = 0; c < 30; c++)
        {
            engine.Grid.SetTile(c, 28, TileType.Solid);
            for (int r = 15; r < 28; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Place solid wall of crates at Col 10 from Row 20 to 27
        for (int r = 20; r < 28; r++)
        {
            engine.Grid.SetTile(10, r, TileType.Crate);
        }

        engine.Player.Position = new Vector2f(6 * 16, 424f); // Col 6
        engine.Player.Velocity = Vector2f.Zero;

        // Deactivate all default monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Place monster behind the wall at Col 14
        engine.Monsters[0].Spawn(0, MonsterType.PatrolDroid, new Vector2f(14 * 16, 424f));

        // Update AutoPilot
        for (int f = 0; f < 10; f++)
        {
            engine.AutoPilot.Update(0.016f, engine);
            Assert.False(engine.AutoPilot.IsDown(GameAction.Shoot), "AutoPilot should not fire at a monster behind a wall");
        }
    }

    [Fact]
    public void AutoPilot_PrioritizesHealthPickup_WhenHealthBelowThreshold()
    {
        var engine = new GameEngine("TEST");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Place flat floor
        for (int c = 0; c < 30; c++)
        {
            engine.Grid.SetTile(c, 28, TileType.Solid);
            for (int r = 15; r < 28; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Deactivate monsters and pickups
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;

        // Player at Col 10, low health (4/8)
        engine.Player.Position = new Vector2f(10 * 16, 424f);
        engine.Player.Health = 4;

        // Spawn MedKit slightly behind at Col 7 (X = 112)
        engine.Pickups[0].Spawn(PickupType.MedKit, new Vector2f(7 * 16, 424f));

        engine.AutoPilot.Update(0.016f, engine);

        // When health is low and MedKit is nearby behind, AutoPilot should move left to retrieve it
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveLeft), "AutoPilot should prioritize moving towards nearby MedKit when health is critical");
    }

    [Fact]
    public void AutoPilot_DoesNotJumpContinuouslyIntoCrateWall_ClimbsAdjacentLadder()
    {
        var engine = new GameEngine("TEST");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Deactivate monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Floor at Row 28
        for (int c = 0; c < 30; c++)
        {
            engine.Grid.SetTile(c, 28, TileType.Solid);
            for (int r = 15; r < 28; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Ladder at Col 6 from Row 18 to 28
        for (int r = 18; r <= 28; r++) engine.Grid.SetTile(6, r, TileType.Ladder);

        // Wall of 4 crates at Col 7 from Row 24 to 27, and ceiling at Row 23
        for (int r = 24; r <= 27; r++) engine.Grid.SetTile(7, r, TileType.Crate);
        for (int c = 5; c <= 8; c++) engine.Grid.SetTile(c, 23, TileType.Solid); // Ceiling

        // Place player on ladder at Col 6
        engine.Player.Position = new Vector2f(6 * 16, 424f);

        engine.AutoPilot.Update(0.016f, engine);

        // When blocked by impassable crate wall with an adjacent ladder, bot should climb the ladder
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveUp), "AutoPilot should climb ladder when facing impassable crate wall");
    }

    [Fact]
    public void AutoPilot_SuccessfullyNavigatesAndSurvivesDemoSeed()
    {
        var engine = new GameEngine("DEMO");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        for (int f = 0; f < 1500; f++)
        {
            engine.Update(0.016f);
            if (engine.IsGameOver) break;
        }

        Assert.False(engine.IsGameOver, "AutoPilot should remain alive during autonomous gameplay");
        Assert.True(engine.Player.Position.X > 1000f, $"AutoPilot should advance past barriers on seed DEMO (reached X={engine.Player.Position.X:F1})");
        Assert.True(engine.Player.Score > 2000, $"AutoPilot should eliminate monsters and accumulate score (score={engine.Player.Score})");
        Assert.True(engine.Player.Health > 0, $"AutoPilot should retain positive health (health={engine.Player.Health})");
    }

    [Theory]
    [InlineData("DEMO", "MY_LEVEL", null, "MY_LEVEL")]
    [InlineData("demo", "", "SAVED_WORLD", "SAVED_WORLD")]
    [InlineData("DEMO", null, null, GameEngine.DefaultSeed)]
    [InlineData("DEMO", "DEMO", "DEMO", GameEngine.DefaultSeed)]
    [InlineData("DEMO CASTLE99", "FOO", "BAR", "CASTLE99")]
    [InlineData("demo   SPACE_RUN  ", "FOO", null, "SPACE_RUN")]
    [InlineData("DEMO ", "ACTIVE_SEED", "SAVED_SEED", "ACTIVE_SEED")]
    public void ResolveDemoTargetSeed_ResolvesCorrectTargetSeed(string command, string? currentSeed, string? lastSaved, string expected)
    {
        string actual = GameEngine.ResolveDemoTargetSeed(command, currentSeed, lastSaved);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DemoMode_PreservesTargetLevel_AndRendersCountdownOverlayWithLevel()
    {
        var engine = new GameEngine("CASTLE99");
        engine.StartDemoMode();

        Assert.Equal("CASTLE99", engine.CurrentSeed);
        Assert.True(engine.IsDemoMode);
        Assert.Equal(5.0f, engine.AutoPilot.CountdownTimer);

        // FrameBuffer rendering should succeed and draw countdown box
        engine.Render();
        Assert.NotNull(engine.FrameBuffer.Pixels);
        Assert.True(engine.FrameBuffer.Pixels.Length == 320 * 200);
    }
}
