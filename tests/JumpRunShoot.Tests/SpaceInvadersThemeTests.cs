using System.IO;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class SpaceInvadersThemeTests
{
    private static string FindRepoRoot()
    {
        string? dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "JumpRunShoot.csproj")) && Directory.Exists(Path.Combine(dir, "wwwroot", "themes")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return Directory.GetCurrentDirectory();
    }

    [Fact]
    public void ExportAndVerifyAllSpaceAssets()
    {
        // 1. Bake procedural assets and export to wwwroot/themes/space/
        var bakedPack = SpaceInvadersTheme.BakeProceduralAssets();
        Assert.NotNull(bakedPack);
        Assert.True(bakedPack.ZeroGravity);

        string repoRoot = FindRepoRoot();
        string targetDir = Path.Combine(repoRoot, "wwwroot", "themes", "space");

        EgaPngCodec.ExportThemePackToDirectory(bakedPack, targetDir);

        // 2. Verify all 42 expected PNG files exist
        string[] expectedFiles =
        {
            "bg_far.png", "bg_mid.png", "bg_near.png",
            "tile_solid.png", "tile_oneway_0.png", "tile_oneway_1.png",
            "tile_hazard.png", "tile_border.png", "tile_ladder.png", "tile_crate.png",
            "player_idle.png", "player_walk_0.png", "player_walk_1.png", "player_walk_2.png", "player_walk_3.png",
            "player_jump.png", "player_climb_0.png", "player_climb_1.png",
            "monster_1_0.png", "monster_1_1.png",
            "monster_2_0.png", "monster_2_1.png",
            "monster_3_0.png", "monster_3_1.png",
            "monster_4_0.png", "monster_4_1.png",
            "monster_5_0.png", "monster_5_1.png",
            "proj_player.png", "proj_monster.png",
            "pickup_gem_0.png", "pickup_gem_1.png",
            "pickup_ruby_0.png", "pickup_ruby_1.png",
            "pickup_gold_0.png", "pickup_gold_1.png",
            "pickup_medkit_0.png", "pickup_medkit_1.png",
            "badge_rapid.png", "badge_jump.png", "badge_speed.png", "badge_invuln.png"
        };

        Assert.Equal(42, expectedFiles.Length);
        foreach (var file in expectedFiles)
        {
            string filePath = Path.Combine(targetDir, file);
            Assert.True(File.Exists(filePath), $"Expected theme PNG '{file}' to exist at '{filePath}'.");
            var fileInfo = new FileInfo(filePath);
            Assert.True(fileInfo.Length > 0, $"File '{file}' should not be empty.");
        }
    }

    [Fact]
    public void SpaceInvadersTheme_IsRegisteredInThemeRegistry()
    {
        bool found = false;
        foreach (var theme in ThemeRegistry.AllThemes)
        {
            if (theme is SpaceInvadersTheme)
            {
                found = true;
                Assert.Equal("Space Invaders", theme.Name);
                Assert.True(theme.ZeroGravity);
                Assert.Equal("audio/bgm_space.wav", theme.BgmTrack);
                break;
            }
        }
        Assert.True(found, "SpaceInvadersTheme should be registered in ThemeRegistry.AllThemes.");
    }

    [Fact]
    public void ThemeSwitching_TogglesSpaceInvadersZeroGravityFlight()
    {
        var engine = new GameEngine();

        // Default theme (Duke) has gravity active
        Assert.Equal(0, engine.CurrentThemeIndex);
        Assert.False(engine.CurrentTheme.ZeroGravity);
        Assert.False(engine.Player.IsFlying);

        // Switch to Space Invaders theme (theme 3)
        engine.SetTheme(3);
        Assert.Equal(3, engine.CurrentThemeIndex);
        Assert.Equal("Space Invaders", engine.CurrentTheme.Name);
        Assert.True(engine.CurrentTheme.ZeroGravity);
        Assert.True(engine.Player.IsFlying, "Player starfighter should enter flight mode automatically in zero gravity.");

        // Switch back to Default theme
        engine.SetTheme(0);
        Assert.Equal(0, engine.CurrentThemeIndex);
        Assert.False(engine.CurrentTheme.ZeroGravity);
        Assert.False(engine.Player.IsFlying, "Player should exit flight mode when switching back to default theme.");
    }

    [Fact]
    public void PlayerStarfighter_FourWayFlightLocomotionAndHover()
    {
        var engine = new GameEngine();
        engine.SetTheme(3); // Space Invaders
        var player = engine.Player;
        Assert.True(player.IsFlying);

        var input = new InputManager();

        // 1. Upward Flight (W / Up)
        input.HandleKeyDown("KeyW");
        input.AdvanceFrame();
        player.Update(0.016f, input, engine.Grid);
        Assert.True(player.Velocity.Y < 0, "Moving up in starfighter should give negative Y velocity.");

        // 2. Downward Flight (S / Down)
        input.Reset();
        input.HandleKeyDown("KeyS");
        input.AdvanceFrame();
        player.Update(0.016f, input, engine.Grid);
        Assert.True(player.Velocity.Y > 0, "Moving down in starfighter should give positive Y velocity.");

        // 3. Hover in space (zero vertical input)
        input.Reset();
        input.AdvanceFrame();
        player.Position = new Vector2f(player.Position.X, 64f);
        player.IsGrounded = false;
        player.Update(0.016f, input, engine.Grid);
        Assert.Equal(0f, player.Velocity.X);
        Assert.Equal(0f, player.Velocity.Y);
    }

    [Fact]
    public void PlayerStarfighter_RespectsSolidPlatformsWhileFlying()
    {
        var engine = new GameEngine();
        engine.SetTheme(3);
        var player = engine.Player;
        Assert.True(player.IsFlying);
        Assert.False(player.IsNoClip);

        // Place a solid wall directly to the right of player
        int col = (int)System.MathF.Floor((player.Position.X + 20) / PhysicsConstants.TileSize);
        int row = (int)System.MathF.Floor(player.Position.Y / PhysicsConstants.TileSize);
        engine.Grid.SetTile(col, row, TileType.Solid);

        float initialX = player.Position.X;
        var input = new InputManager();
        input.HandleKeyDown("KeyD");
        input.AdvanceFrame();

        // Run update towards the solid tile
        for (int frame = 0; frame < 10; frame++)
        {
            player.Update(0.016f, input, engine.Grid);
        }

        float solidWallX = col * PhysicsConstants.TileSize;
        Assert.True(player.BoundingBox.X + player.BoundingBox.Width <= solidWallX + 0.1f, "Player starfighter should not penetrate solid platform wall!");
    }

    [Fact]
    public void SpaceInvadersThemeRendering_AllDrawMethodsExecuteCleanly()
    {
        var theme = new SpaceInvadersTheme();
        var fb = new FrameBuffer();
        var camera = new Camera2D(320, 184);
        camera.SnapTo(new Vector2f(160f, 100f));

        // 1. Background
        theme.DrawBackground(fb, camera);

        // 2. Tiles
        var tileTypes = new[]
        {
            TileType.Solid, TileType.OneWayPlatform, TileType.Hazard,
            TileType.Border, TileType.Ladder, TileType.Crate
        };
        for (int i = 0; i < tileTypes.Length; i++)
        {
            theme.DrawTile(fb, tileTypes[i], i * 16, 50, i, 3);
        }

        // 3. Player
        var player = new PlayerEntity(new Vector2f(50f, 50f));
        theme.DrawPlayer(fb, player, 50, 50);

        // 4. Projectiles
        var proj = new Projectile();
        proj.Spawn(new Vector2f(100f, 60f), new Vector2f(200f, 0f), 1, fromPlayer: true);
        theme.DrawProjectile(fb, proj, 100, 60);

        // 5. Monster
        var monster = new MonsterEntity();
        monster.Spawn(0, MonsterType.HoverDrone, new Vector2f(120f, 70f));
        theme.DrawMonster(fb, monster, 120, 70);

        // 6. Death Effect
        var effect = new DeathEffect
        {
            IsActive = true,
            MonsterType = MonsterType.PatrolDroid,
            Position = new Vector2f(140f, 80f),
            Width = 16,
            Height = 16,
            Facing = Direction.Right,
            Timer = 0.2f,
            Duration = 0.45f
        };
        theme.DrawDeathEffect(fb, effect, 140, 80);

        // 7. Pickups
        var pickup = new PickupItem();
        pickup.Spawn(PickupType.BlueGem, new Vector2f(160f, 90f));
        theme.DrawPickup(fb, pickup, 160, 90);

        // 8. HUD
        theme.DrawHud(fb, player, "SPACE_TEST", 60);

        // Confirm framebuffer contains drawn pixels
        Assert.NotEqual(0u, fb.Pixels[100 * FrameBuffer.Width + 100]);
    }

    [Fact]
    public void ThemeSwitching_FromSpaceToForestOrDuke_DisablesFlightEvenIfCheatWasToggled()
    {
        var engine = new GameEngine("FOO");
        engine.CheatsEnabled = true;

        // Switch to Space Invaders (Zero Gravity)
        engine.SetTheme(3);
        Assert.True(engine.Player.IsFlying);

        // Player presses 'F' in Space Invaders
        engine.Input.HandleKeyDown("KeyF");
        engine.Update(0.016f);
        engine.Input.HandleKeyUp("KeyF");

        // Player switches to Forest (theme 1)
        engine.SetTheme(1);
        Assert.False(engine.Player.IsFlying, "Player should NOT be flying in Forest theme after switching from Space Invaders");
        Assert.False(engine.IsFlightCheatActive);

        // Player switches to Duke (theme 0)
        engine.SetTheme(0);
        Assert.False(engine.Player.IsFlying, "Player should NOT be flying in Duke theme after switching from Space Invaders");
        Assert.False(engine.IsFlightCheatActive);
    }

    [Fact]
    public void PlayerReset_And_EngineRestart_ResetsFlightInNonFlyingTheme()
    {
        var player = new PlayerEntity(new Vector2f(100, 100));
        player.IsFlying = true;
        player.Reset(new Vector2f(50, 50));
        Assert.False(player.IsFlying, "Player.Reset() must reset IsFlying to false");

        var engine = new GameEngine("FOO");
        engine.Player.IsFlying = true;
        engine.Restart("BAR");
        Assert.False(engine.Player.IsFlying, "Engine.Restart() in non-flying theme must reset IsFlying to false");
    }
}
