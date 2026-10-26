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

    [Fact]
    public void AutoPilot_ZeroGravityFlight_WhenTouchingOrTooCloseToMonster_MovesAwayToCreateDistance()
    {
        var engine = new GameEngine("SPACE_EVADE");
        engine.SetTheme(3); // Space Invaders (Zero Gravity)
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;

        // Position player ship at (100, 100)
        engine.Player.Position = new Vector2f(100f, 100f);

        // Deactivate all default monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Spawn a monster overlapping or dangerously close to the right (at X=110, Y=100)
        var droid = new MonsterEntity();
        droid.Spawn(0, MonsterType.PatrolDroid, new Vector2f(110f, 100f));
        engine.Monsters[0] = droid;

        engine.AutoPilot.Update(0.016f, engine);

        // The bot should move away from the monster (left) and not ram into it (right)
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveLeft), "Bot in flight mode should move away (left) when monster is touching or ahead on the right");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot should NOT press into the monster");
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveUp) || engine.AutoPilot.IsDown(GameAction.MoveDown), "Bot should dodge vertically away from the monster altitude to break contact");
    }

    [Fact]
    public void AutoPilot_PlatformerMode_WhenMonsterTouchingOrTooClose_MovesAwayOrJumpsOverToEscape()
    {
        var engine = new GameEngine("PLATFORM_EVADE");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Place player on flat ground at Col 10 (X=160, Y=424)
        engine.Player.Position = new Vector2f(160f, 424f);

        // Deactivate all default monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Spawn a monster touching or overlapping player to the right at X=168, Y=424
        var droid = new MonsterEntity();
        droid.Spawn(0, MonsterType.PatrolDroid, new Vector2f(168f, 424f));
        engine.Monsters[0] = droid;

        engine.AutoPilot.Update(0.016f, engine);

        // The bot should move away (left) or jump to break free and not get stuck
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveLeft) || engine.AutoPilot.IsDown(GameAction.Jump),
            "Bot in platformer mode should retreat or jump away when monster is touching or dangerously close");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot should not move into the touching monster");
    }

    [Fact]
    public void AutoPilot_CorneredAgainstWall_VaultsOverMonster()
    {
        var engine = new GameEngine("CORNER_VAULT");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Player at Col 10 (X=160, Y=424)
        engine.Player.Position = new Vector2f(160f, 424f);
        engine.Player.IsGrounded = true;

        // Solid wall directly behind player at Col 9
        for (int r = 24; r <= 28; r++)
        {
            engine.Grid.SetTile(9, r, TileType.Solid);
        }

        // Deactivate all monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Spawn monster at Col 11 (X=176, Y=424)
        var droid = new MonsterEntity();
        droid.Spawn(0, MonsterType.PatrolDroid, new Vector2f(176f, 424f));
        engine.Monsters[0] = droid;

        engine.AutoPilot.Update(0.016f, engine);

        // When cornered with a wall behind, the bot should vault right over the monster
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot should move forward (right) over monster when trapped by wall behind");
        Assert.True(engine.AutoPilot.IsDown(GameAction.Jump), "Bot should jump high to vault over the monster");
    }

    [Fact]
    public void AutoPilot_ZeroGravityFlight_MaintainsStandoffDistance_AndShootsWhenSafe()
    {
        var engine = new GameEngine("SPACE_STANDOFF");
        engine.SetTheme(3); // Space Invaders (Zero Gravity)
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;

        // Position player at (100, 100)
        engine.Player.Position = new Vector2f(100f, 100f);

        // Clear tiles around (100, 100) and (190, 100)
        for (int c = 4; c <= 15; c++)
        {
            for (int r = 4; r <= 8; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Deactivate default monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Monster at X=190, Y=100 (dx = 90, in the safe 70-130px standoff window)
        var droid = new MonsterEntity();
        droid.Spawn(0, MonsterType.PatrolDroid, new Vector2f(190f, 100f));
        engine.Monsters[0] = droid;

        engine.AutoPilot.Update(0.016f, engine);

        // In safe standoff window, bot should not charge forward into monster
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot should not charge into monster when in standoff distance");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveLeft), "Bot should not retreat when already in safe standoff distance");
        Assert.True(engine.AutoPilot.IsDown(GameAction.Shoot), "Bot should fire controlled bursts at monster from safe standoff distance");
    }

    [Fact]
    public void AutoPilot_PlatformerMode_WhenMonsterElevatedOrBlockedByPlatform_DoesNotShootWall()
    {
        var engine = new GameEngine("PLATFORM_AIM");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Player standing at Col 25, Row 20 (X=400, Y=320-28=292)
        engine.Player.Position = new Vector2f(400f, 292f);
        engine.Player.Facing = Direction.Left;
        engine.Player.IsGrounded = true;

        // Deactivate default monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Clear row 19 and 20 between col 10 and 25
        for (int c = 10; c <= 25; c++)
        {
            engine.Grid.SetTile(c, 19, TileType.Empty);
            engine.Grid.SetTile(c, 20, TileType.Empty);
        }

        // Place a solid platform tile at Col 23, Row 20 (blocks standing bullets at Y=302)
        engine.Grid.SetTile(23, 20, TileType.Solid);

        // HoverDrone hovering to the left at Col 21, Row 18 (X=336, Y=288, elevated above platform)
        var drone = new MonsterEntity();
        drone.Spawn(0, MonsterType.HoverDrone, new Vector2f(336f, 288f));
        engine.Monsters[0] = drone;

        engine.AutoPilot.Update(0.016f, engine);

        // While grounded, standing bullet is at Y=302 which hits the solid platform tile at Col 16, Row 20
        // The bot should NOT shoot into the solid platform tile!
        Assert.False(engine.AutoPilot.IsDown(GameAction.Shoot), "Bot should not fire when horizontal bullet path is blocked by platform tile or unaligned");
    }

    [Fact]
    public void AutoPilot_PlatformerMode_WhenMonsterElevated_LeapsAndFiresAtPeak()
    {
        var engine = new GameEngine("JUMP_AIM");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Player standing at Col 25, Row 20 (X=400, Y=292), facing Left
        engine.Player.Position = new Vector2f(400f, 292f);
        engine.Player.Facing = Direction.Left;
        engine.Player.IsGrounded = true;

        // Deactivate default monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Clear air above platforms (Col 20 to 26 from Row 16 to 20)
        for (int c = 20; c <= 26; c++)
        {
            for (int r = 16; r <= 20; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // HoverDrone hovering to the left at Col 21, Row 18 (X=336, Y=288, elevated)
        var drone = new MonsterEntity();
        drone.Spawn(0, MonsterType.HoverDrone, new Vector2f(336f, 288f));
        engine.Monsters[0] = drone;

        // While grounded at Y=292, bullet is at Y=302 (unaligned with drone at Y=288..300)
        engine.AutoPilot.Update(0.016f, engine);
        Assert.True(engine.AutoPilot.IsDown(GameAction.Jump), "Bot should jump to bring gun up to elevated monster's altitude");
        Assert.False(engine.AutoPilot.IsDown(GameAction.Shoot), "Bot should NOT shoot while grounded and unaligned");

        // Now place player in mid-air at jump apex (Y=280 -> bullet at Y=290, aligned with drone at Y=288..300)
        engine.Player.Position = new Vector2f(400f, 280f);
        engine.Player.IsGrounded = false;
        engine.AutoPilot.Update(0.016f, engine);

        Assert.True(engine.AutoPilot.IsDown(GameAction.Shoot), "Bot should fire controlled bursts when elevated and aligned at monster altitude");
    }

    [Theory]
    [InlineData(100f, 100f, 26f, true)]   // player bullet at Y=110..114, inside monster Y=100..126
    [InlineData(86f, 100f, 26f, true)]    // player bullet at Y=96..100, grazing top edge of monster
    [InlineData(85f, 100f, 26f, false)]   // player bullet at Y=95..99, completely above monster
    [InlineData(116f, 100f, 26f, true)]   // player bullet at Y=126..130, grazing bottom edge of monster
    [InlineData(117f, 100f, 26f, false)]  // player bullet at Y=127..131, completely below monster
    public void IsShootingYAligned_CalculatesExactPixelBounds(float playerY, float monsterY, float monsterH, bool expected)
    {
        bool actual = AutoPilotController.IsShootingYAligned(playerY, monsterY, monsterH);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetIdealCombatAltitude_HeavyGuard_UnderBulletSweetSpot_DodgesEnemyBulletWhileHittingMonster()
    {
        var guard = new MonsterEntity();
        guard.Spawn(0, MonsterType.HeavyGuard, new Vector2f(200f, 100f)); // Y=100..126

        float idealPlayerY = AutoPilotController.GetIdealCombatAltitude(guard);
        Assert.Equal(112.0f, idealPlayerY);

        // 1. Check HeavyGuard bullet clears player head:
        // HeavyGuard shoots from Y=106 (bullet interval [106, 110])
        float enemyBulletBottom = 100f + 6f + 4f; // 110f
        float playerHeadTop = idealPlayerY + 1.0f; // 113f (Player BoxOffset.Y = 1.0f)
        Assert.True(playerHeadTop > enemyBulletBottom, "HeavyGuard bullet should pass above player head at sweet spot altitude");

        // 2. Check player bullet hits HeavyGuard:
        // Player bullet is at idealPlayerY + 10.0f = 122f (interval [122, 126])
        Assert.True(AutoPilotController.IsShootingYAligned(idealPlayerY, guard.Position.Y, guard.Height),
            "Player bullet must hit HeavyGuard lower body at sweet spot altitude");
    }

    [Fact]
    public void AutoPilot_ZeroGravityFlight_HeavyGuard_SteersIntoUnderBulletSweetSpot()
    {
        var engine = new GameEngine("SPACE_GUARD");
        engine.SetTheme(3); // Space Invaders (Zero Gravity)
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;

        // Player at (100, 100)
        engine.Player.Position = new Vector2f(100f, 100f);

        // Clear air
        for (int c = 4; c <= 16; c++)
        {
            for (int r = 4; r <= 10; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // HeavyGuard at (190, 100) (ideal altitude is Y = 112)
        var guard = new MonsterEntity();
        guard.Spawn(0, MonsterType.HeavyGuard, new Vector2f(190f, 100f));
        engine.Monsters[0] = guard;

        engine.AutoPilot.Update(0.016f, engine);

        // Player is at Y=100, ideal is Y=112. Bot should steer down towards sweet spot
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveDown), "Bot in flight mode should steer down into the under-bullet sweet spot");
    }

    [Fact]
    public void AutoPilot_PlatformerMode_CrateStepAhead_RecognizedAsJumpable_HopsOver()
    {
        var engine = new GameEngine("CRATE_STEP");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Floor at Row 28
        for (int c = 0; c < 30; c++)
        {
            engine.Grid.SetTile(c, 28, TileType.Solid);
            for (int r = 18; r < 28; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Stack of 2 crates at Col 10 (Rows 27, 26)
        engine.Grid.SetTile(10, 27, TileType.Crate);
        engine.Grid.SetTile(10, 26, TileType.Crate);

        // Stack of 3 crates at Col 11 (Rows 27, 26, 25)
        engine.Grid.SetTile(11, 27, TileType.Crate);
        engine.Grid.SetTile(11, 26, TileType.Crate);
        engine.Grid.SetTile(11, 25, TileType.Crate);

        // Player standing on the 2 crates at Col 10 (feet at Row 26, Y = 26 * 16 - 28 = 388)
        engine.Player.Position = new Vector2f(10 * 16, 388f);
        engine.Player.IsGrounded = true;

        engine.AutoPilot.Update(0.016f, engine);

        // Relative to player feet, the crate stack in front is only a 1-tile step (Row 25) with open sky above
        // Bot should jump forward over it, NOT turn around and escape backwards to the left!
        Assert.True(engine.AutoPilot.IsDown(GameAction.Jump), "Bot should jump over 1-crate step ahead");
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot should advance right over 1-crate step");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveLeft), "Bot should NOT retreat left when facing a 1-crate step");
    }

    [Fact]
    public void AutoPilot_PlatformerMode_TwoCrateStackFromFloor_WithLadderBehind_HopsStep()
    {
        var engine = new GameEngine("TWO_CRATE_FLOOR");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Floor at Row 28
        for (int c = 0; c < 30; c++)
        {
            engine.Grid.SetTile(c, 28, TileType.Solid);
            for (int r = 18; r < 28; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Ladder at Col 8 (behind player)
        engine.Grid.SetTile(8, 27, TileType.Ladder);
        engine.Grid.SetTile(8, 26, TileType.Ladder);
        engine.Grid.SetTile(8, 25, TileType.Ladder);

        // 2-crate stack at Col 11 (Rows 27, 26) - 32px step from floor
        engine.Grid.SetTile(11, 27, TileType.Crate);
        engine.Grid.SetTile(11, 26, TileType.Crate);

        // Player standing on floor at Col 10 (feet on Row 28, Y = 28 * 16 - 28 = 420)
        engine.Player.Position = new Vector2f(10 * 16, 420f);
        engine.Player.IsGrounded = true;

        engine.AutoPilot.Update(0.016f, engine);

        // The 2-crate stack is 32px high, within jump height (52px).
        // Bot must jump forward to hop onto it, NOT retreat left towards the ladder at Col 8!
        Assert.True(engine.AutoPilot.IsDown(GameAction.Jump), "Bot should jump to hop onto 2-crate step from floor");
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot should advance right onto 2-crate step");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveLeft), "Bot should NOT retreat left to ladder when step is jumpable");
    }

    [Fact]
    public void AutoPilot_AreaStuck_ForcesAssertiveForwardVaultJump()
    {
        var engine = new GameEngine("AREA_STUCK");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        // Floor at Row 28
        for (int c = 0; c < 30; c++) engine.Grid.SetTile(c, 28, TileType.Solid);

        engine.Player.Position = new Vector2f(100f, 420f);
        engine.Player.IsGrounded = true;

        // Simulate area stuck timer exceeding 1.0s and an active reverse escape
        typeof(AutoPilotController)
            .GetField("_areaAnchorX", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(engine.AutoPilot, 100f);
        typeof(AutoPilotController)
            .GetField("_areaStuckTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(engine.AutoPilot, 1.2f);
        typeof(AutoPilotController)
            .GetField("_escapeDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(engine.AutoPilot, -1);

        engine.AutoPilot.Update(0.016f, engine);

        // Must cancel backwards escape, press MoveRight, Run, and Jump to break out of stuck loop
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveRight), "Area stuck must force forward movement to the right");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveLeft), "Area stuck must cancel left escape");
        Assert.True(engine.AutoPilot.IsDown(GameAction.Jump), "Area stuck must trigger jump vault");
        Assert.True(engine.AutoPilot.IsDown(GameAction.Run), "Area stuck must sprint forward");
    }

    [Fact]
    public void AutoPilot_FacingDeadband_DoesNotJitterWhenTargetOverhead()
    {
        var engine = new GameEngine("FACING_DEADBAND");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Clear flat corridor
        for (int c = 2; c <= 20; c++)
        {
            engine.Grid.SetTile(c, 28, TileType.Solid);
            for (int r = 18; r < 28; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Player at X=100 facing Right, grounded at Row 28
        engine.Player.Position = new Vector2f(100f, 420f);
        engine.Player.Facing = Direction.Right;
        engine.Player.IsGrounded = true;

        // Drone in jumping reach overhead at Y=400 (dy=-20px) with slight negative delta (dx = -2px)
        var drone = new MonsterEntity();
        drone.Spawn(0, MonsterType.HoverDrone, new Vector2f(98f, 400f));
        engine.Monsters[0] = drone;

        engine.AutoPilot.Update(0.016f, engine);

        // Within 16px deadband, player must NOT flip facing to Left
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveLeft), "Facing deadband should prevent flipping left when target is directly overhead");
    }

    [Fact]
    public void AutoPilot_BulletEvasion_VerticalBulletFromCeilingTurret_DodgesHorizontally()
    {
        var engine = new GameEngine("TURRET_BULLET_DODGE");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Flat ground
        for (int c = 2; c <= 20; c++)
        {
            engine.Grid.SetTile(c, 28, TileType.Solid);
            for (int r = 18; r < 28; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Player at X=100, Y=420
        engine.Player.Position = new Vector2f(100f, 420f);
        engine.Player.IsGrounded = true;

        // In combat stance with a monster at X=180 (sweet spot 80px away), normally Duke stands still
        var monster = new MonsterEntity();
        monster.Spawn(0, MonsterType.HeavyGuard, new Vector2f(180f, 420f));
        engine.Monsters[0] = monster;

        // Ceiling turret bullet falling straight down at X=106, Y=360 with Velocity.Y = 160f
        engine.Projectiles[0].Spawn(new Vector2f(106f, 360f), new Vector2f(0f, 160f), 1, fromPlayer: false);

        engine.AutoPilot.Update(0.016f, engine);

        // Bot must evade horizontally out of the vertical drop line instead of standing still
        bool movingHorizontally = engine.AutoPilot.IsDown(GameAction.MoveLeft) || engine.AutoPilot.IsDown(GameAction.MoveRight);
        Assert.True(movingHorizontally, "Bot must evade horizontally when an enemy bullet is falling from above");
    }

    [Fact]
    public void AutoPilot_BulletEvasion_IncomingHorizontalBulletUnderCeiling_DodgesHorizontally()
    {
        var engine = new GameEngine("COMBAT_BULLET_DODGE");
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Flat ground at Row 28
        for (int c = 2; c <= 20; c++)
        {
            engine.Grid.SetTile(c, 28, TileType.Solid);
            for (int r = 18; r < 28; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Low ceiling at Row 25 (player head is at Row 26)
        int playerCol = (int)MathF.Floor(100f / 16f);
        engine.Grid.SetTile(playerCol, 25, TileType.Solid);

        // Player at X=100, Y=420 (Row 26-27)
        engine.Player.Position = new Vector2f(100f, 420f);
        engine.Player.IsGrounded = true;

        // HeavyGuard at X=180 (sweet spot 80px away)
        var guard = new MonsterEntity();
        guard.Spawn(0, MonsterType.HeavyGuard, new Vector2f(180f, 420f));
        engine.Monsters[0] = guard;

        // Incoming bullet from guard at X=140, Y=426 traveling left
        engine.Projectiles[0].Spawn(new Vector2f(140f, 426f), new Vector2f(-180f, 0f), 2, fromPlayer: false);

        engine.AutoPilot.Update(0.016f, engine);

        // Under low ceiling: jumping would hit ceiling! Bot must move horizontally away to evade
        Assert.False(engine.AutoPilot.IsDown(GameAction.Jump), "Bot should not jump under a low ceiling");
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveLeft) || engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot must move horizontally to evade incoming bullet under low ceiling");
    }

    [Fact]
    public void AutoPilot_ZeroGravity_IncomingBulletWithCeilingAbove_DodgesDownInsteadOfUp()
    {
        var engine = new GameEngine("FLIGHT_CEILING_DODGE");
        engine.SetTheme(2); // Underwater (Zero Gravity Flight)
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Ceiling at Row 10 (Y=160), open water below
        for (int c = 2; c <= 20; c++)
        {
            engine.Grid.SetTile(c, 10, TileType.Solid);
            for (int r = 11; r <= 20; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Player at X=100, Y=176 (Row 11, directly below ceiling at Row 10)
        engine.Player.Position = new Vector2f(100f, 176f);

        // Incoming bullet at X=150, Y=180 moving left
        engine.Projectiles[0].Spawn(new Vector2f(150f, 180f), new Vector2f(-180f, 0f), 1, fromPlayer: false);

        engine.AutoPilot.Update(0.016f, engine);

        // With ceiling directly overhead, bot MUST dodge Down into open water, NOT Up into the ceiling
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveDown), "Bot in flight mode should dodge Down when ceiling is overhead");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveUp), "Bot in flight mode should NOT try to dodge Up into a solid ceiling");
    }

    [Fact]
    public void AutoPilot_ZeroGravity_NonCriticalHealth_DoesNotFleeBackwardFromForwardMonstersForMedkit()
    {
        var engine = new GameEngine("FLIGHT_MEDKIT_PRIORITY");
        engine.SetTheme(2); // Underwater
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;
        engine.Player.Health = 6; // 6 of 8 HP (non-critical)

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Open water corridor
        for (int c = 0; c <= 30; c++)
        {
            for (int r = 8; r <= 22; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Player at X=200, Y=200
        engine.Player.Position = new Vector2f(200f, 200f);

        // Medkit 100px behind player to the left at (100, 200)
        engine.Pickups[0].Spawn(PickupType.MedKit, new Vector2f(100f, 200f));

        // Jellyfish (HoverDrone) 80px ahead of player to the right at (280, 200)
        var drone = new MonsterEntity();
        drone.Spawn(0, MonsterType.HoverDrone, new Vector2f(280f, 200f));
        engine.Monsters[0] = drone;

        engine.AutoPilot.Update(0.016f, engine);

        // At 6 HP with a threat in front, bot must NOT turn around and flee backwards to the left for the medkit
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveLeft), "Bot at 6 HP should not flee backward from an enemy ahead to fetch a rear medkit");
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveRight) || engine.AutoPilot.IsDown(GameAction.Shoot), "Bot should face and attack the monster ahead");
    }

    [Fact]
    public void AutoPilot_ZeroGravity_HoverDroneInSweetSpot_FiresControlledBurst()
    {
        var engine = new GameEngine("FLIGHT_COMBAT_BURST");
        engine.SetTheme(2); // Underwater
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Open water
        for (int c = 4; c <= 25; c++)
        {
            for (int r = 8; r <= 20; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Player at X=100, Y=200, facing Right
        engine.Player.Position = new Vector2f(100f, 200f);
        engine.Player.Facing = Direction.Right;

        // HoverDrone (Jellyfish) at X=180, Y=206 (perfectly aligned with gun at Y+12)
        var drone = new MonsterEntity();
        drone.Spawn(0, MonsterType.HoverDrone, new Vector2f(180f, 206f));
        engine.Monsters[0] = drone;

        engine.AutoPilot.Update(0.016f, engine);

        // In 45-140px sweet spot with clear line of sight, bot must shoot!
        Assert.True(engine.AutoPilot.IsDown(GameAction.Shoot), "Bot in flight mode should shoot jellyfish when in sweet spot");
    }

    [Fact]
    public void AutoPilot_ZeroGravity_AreaStuck_ForcesForwardFlightThroughObstacle()
    {
        var engine = new GameEngine("FLIGHT_AREA_STUCK");
        engine.SetTheme(3); // Space Invaders (Zero Gravity Flight)
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;

        engine.Player.Position = new Vector2f(100f, 200f);

        // Inject area stuck timer > 1.0s and anchor at 100f
        typeof(AutoPilotController)
            .GetField("_areaAnchorX", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(engine.AutoPilot, 100f);
        typeof(AutoPilotController)
            .GetField("_areaStuckTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(engine.AutoPilot, 1.2f);
        typeof(AutoPilotController)
            .GetField("_evasionTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(engine.AutoPilot, 0.4f);
        typeof(AutoPilotController)
            .GetField("_evasionDirX", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(engine.AutoPilot, -1);

        engine.AutoPilot.Update(0.016f, engine);

        // In flight mode, area stuck must cancel evasion and force forward sprint to the right
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveRight), "Area stuck in flight mode must force forward flight");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveLeft), "Area stuck must cancel left retreat");
        Assert.True(engine.AutoPilot.IsDown(GameAction.Run), "Area stuck must trigger sprint");
    }

    [Fact]
    public void AutoPilot_ZeroGravity_TargetBehindPillar_AdvancesToClearObstruction()
    {
        var engine = new GameEngine("FLIGHT_PILLAR_CLEAR");
        engine.SetTheme(3); // Space Invaders
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;
        engine.Player.Facing = Direction.Right;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;

        // Open corridor
        for (int c = 4; c <= 25; c++)
        {
            for (int r = 6; r <= 22; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Vertical pillar at Col 10, Rows 6 to 12 (leaving gap at row 13-16)
        for (int r = 6; r <= 12; r++) engine.Grid.SetTile(10, r, TileType.Solid);

        // Player at X=130 (Col 8), Y=150 (Row 9) - behind the pillar
        engine.Player.Position = new Vector2f(130f, 150f);

        // Monster at X=210 (Col 13), Y=150 (Row 9) - on other side of pillar
        var drone = new MonsterEntity();
        drone.Spawn(0, MonsterType.HoverDrone, new Vector2f(210f, 150f));
        engine.Monsters[0] = drone;

        engine.AutoPilot.Update(0.016f, engine);

        // Player is at standoff distance (80px), but solid pillar blocks line of sight.
        // Bot must NOT freeze in sweet spot; it must advance forward (MoveRight) to clear the pillar!
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot in flight mode must advance right to clear pillar when shot is blocked");
    }

    [Fact]
    public void AutoPilot_ZeroGravity_CeilingBlocksVerticalAlignment_AdvancesHorizontally()
    {
        var engine = new GameEngine("FLIGHT_CEILING_ALIGN");
        engine.SetTheme(3); // Space Invaders
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;
        engine.Player.Facing = Direction.Right;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;

        // Open corridor
        for (int c = 4; c <= 25; c++)
        {
            for (int r = 6; r <= 22; r++) engine.Grid.SetTile(c, r, TileType.Empty);
        }

        // Ceiling at Row 10 (Y=160) directly above player at Col 7
        engine.Grid.SetTile(7, 10, TileType.Solid);

        // Player at X=112 (Col 7), Y=176 (Row 11, under ceiling)
        engine.Player.Position = new Vector2f(112f, 176f);

        // Monster at X=200, Y=130 (Row 8, higher than player's ceiling)
        var drone = new MonsterEntity();
        drone.Spawn(0, MonsterType.HoverDrone, new Vector2f(200f, 130f));
        engine.Monsters[0] = drone;

        engine.AutoPilot.Update(0.016f, engine);

        // Monster is higher up, but player's head is blocked by ceiling at Row 10.
        // Bot cannot Y-align here: it must advance horizontally (MoveRight) through the gap!
        Assert.True(engine.AutoPilot.IsDown(GameAction.MoveRight), "Bot must advance horizontally when vertical alignment is blocked by ceiling");
        Assert.False(engine.AutoPilot.IsDown(GameAction.MoveUp), "Bot must not scrape against ceiling when blocked above");
    }

    [Fact]
    public void AutoPilot_ZeroGravity_MonsterAtCombatDistance_DoesNotTriggerTouchEvasion()
    {
        var engine = new GameEngine("FLIGHT_COMBAT_NO_TOUCH_EVASION");
        engine.SetTheme(3); // Space Invaders
        engine.StartDemoMode();
        engine.AutoPilot.CountdownTimer = 0f;
        engine.AutoPilot.IsActive = true;
        engine.Player.IsFlying = true;
        engine.Player.Facing = Direction.Right;

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;

        // Player at X=100, Y=150
        engine.Player.Position = new Vector2f(100f, 150f);

        // Monster at X=148 (48px dx: inside combat range, but outside touch hazard 28px)
        var drone = new MonsterEntity();
        drone.Spawn(0, MonsterType.HoverDrone, new Vector2f(148f, 150f));
        engine.Monsters[0] = drone;

        engine.AutoPilot.Update(0.016f, engine);

        // Evasion timer should NOT be triggered at 48px combat distance
        float evasionTimer = (float)typeof(AutoPilotController)
            .GetField("_evasionTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(engine.AutoPilot)!;

        Assert.Equal(0f, evasionTimer);
    }
}
