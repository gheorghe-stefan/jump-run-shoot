using System.IO;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class UnderwaterThemeTests
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
    public void ExportAndVerifyAllUnderwaterAssets()
    {
        // 1. Bake procedural assets and export to wwwroot/themes/underwater/
        var bakedPack = UnderwaterTheme.BakeProceduralAssets();
        Assert.NotNull(bakedPack);
        Assert.True(bakedPack.ZeroGravity);

        string repoRoot = FindRepoRoot();
        string targetDir = Path.Combine(repoRoot, "wwwroot", "themes", "underwater");

        EgaPngCodec.ExportThemePackToDirectory(bakedPack, targetDir);

        // 2. Verify all 41 expected PNG files exist
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
    public void UnderwaterTheme_IsRegisteredInThemeRegistry()
    {
        bool found = false;
        foreach (var theme in ThemeRegistry.AllThemes)
        {
            if (theme is UnderwaterTheme)
            {
                found = true;
                Assert.Equal("Underwater", theme.Name);
                Assert.True(theme.ZeroGravity);
                break;
            }
        }
        Assert.True(found, "UnderwaterTheme should be registered in ThemeRegistry.AllThemes.");
    }

    [Fact]
    public void ThemeSwitching_TogglesZeroGravityFlightMechanics()
    {
        var engine = new GameEngine();

        // Initially on default theme (gravity active)
        Assert.Equal(0, engine.CurrentThemeIndex);
        Assert.False(engine.CurrentTheme.ZeroGravity);
        Assert.False(engine.Player.IsFlying);

        // Switch to theme 2 (Underwater)
        engine.SetTheme(2);
        Assert.Equal(2, engine.CurrentThemeIndex);
        Assert.Equal("Underwater", engine.CurrentTheme.Name);
        Assert.True(engine.CurrentTheme.ZeroGravity);
        Assert.True(engine.Player.IsFlying, "Player should automatically enter flight/swim mode in zero gravity.");

        // Switch back to theme 0 (Duke 1 Classic)
        engine.SetTheme(0);
        Assert.Equal(0, engine.CurrentThemeIndex);
        Assert.False(engine.CurrentTheme.ZeroGravity);
        Assert.False(engine.Player.IsFlying, "Player should exit flight mode and restore gravity when returning to standard theme.");
        Assert.Equal(0f, engine.Player.Velocity.Y);
    }

    [Fact]
    public void ThemeSwitching_PreservesCheatFlightIfExplicitlyEnabled()
    {
        var engine = new GameEngine();
        engine.CheatsEnabled = true;
        engine.IsFlightCheatActive = true;
        engine.Player.IsFlying = true;

        // Switch to Underwater theme
        engine.SetTheme(2);
        Assert.True(engine.Player.IsFlying);

        // Switch back to Duke theme
        engine.SetTheme(0);
        Assert.True(engine.Player.IsFlying, "Cheat flight should remain enabled if player had cheat fly active.");
    }

    [Fact]
    public void Restart_WithUnderwaterTheme_RetainsZeroGravityFlight()
    {
        var engine = new GameEngine();
        engine.SetTheme(2);
        Assert.True(engine.Player.IsFlying);

        engine.Restart("RETRO_OCEAN");
        Assert.True(engine.Player.IsFlying, "Player should spawn with fly/swim mode active when underwater theme is selected.");
    }

    [Fact]
    public void PlayerSwimming_FourWayLocomotionAndHover()
    {
        var engine = new GameEngine();
        engine.SetTheme(2);
        var player = engine.Player;
        Assert.True(player.IsFlying);

        var input = new InputManager();

        // Test Upward Swimming (W / Up / Jump)
        input.HandleKeyDown("KeyW");
        input.AdvanceFrame();
        player.Update(0.016f, input, engine.Grid);
        Assert.True(player.Velocity.Y < 0, "Moving up in zero gravity should give negative Y velocity (swimming upwards).");
        Assert.Equal(PlayerState.Walking, player.State);

        // Test Downward Swimming (S / Down)
        input.Reset();
        input.HandleKeyDown("KeyS");
        input.AdvanceFrame();
        player.Update(0.016f, input, engine.Grid);
        Assert.True(player.Velocity.Y > 0, "Moving down in zero gravity should give positive Y velocity (swimming downwards).");
        Assert.Equal(PlayerState.Walking, player.State);

        // Test Hover in Place (No vertical input)
        input.Reset();
        input.AdvanceFrame();
        player.Position = new Vector2f(player.Position.X, 64f); // Mid-air
        player.IsGrounded = false;
        player.Update(0.016f, input, engine.Grid);
        Assert.Equal(0f, player.Velocity.X);
        Assert.Equal(0f, player.Velocity.Y);
        Assert.Equal(PlayerState.Jumping, player.State); // Streamlined hover glide pose
    }

    [Fact]
    public void UnderwaterThemeRendering_AllDrawMethodsExecuteCleanly()
    {
        var theme = new UnderwaterTheme();
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
        var player = new PlayerEntity(new Vector2f(100f, 100f));
        theme.DrawPlayer(fb, player, 100, 100);

        // 4. Projectiles
        var playerProj = new Projectile();
        playerProj.Spawn(new Vector2f(120f, 100f), new Vector2f(300f, 0f), 1, fromPlayer: true);
        var monsterProj = new Projectile();
        monsterProj.Spawn(new Vector2f(140f, 100f), new Vector2f(-200f, 0f), 1, fromPlayer: false);
        theme.DrawProjectile(fb, in playerProj, 120, 100);
        theme.DrawProjectile(fb, in monsterProj, 140, 100);

        // 5. Monsters
        var monsters = new[]
        {
            (MonsterType.PatrolDroid, new Vector2f(50f, 100f)),
            (MonsterType.HoverDrone, new Vector2f(80f, 100f)),
            (MonsterType.CeilingTurret, new Vector2f(110f, 50f)),
            (MonsterType.MechaHound, new Vector2f(150f, 100f)),
            (MonsterType.HeavyGuard, new Vector2f(200f, 100f))
        };
        for (int i = 0; i < monsters.Length; i++)
        {
            var m = new MonsterEntity();
            m.Spawn(i, monsters[i].Item1, monsters[i].Item2);
            theme.DrawMonster(fb, m, (int)m.Position.X, (int)m.Position.Y);
        }

        // 6. Pickups
        var pickupTypes = new[]
        {
            (PickupType.BlueGem, new Vector2f(30f, 100f)),
            (PickupType.RedRuby, new Vector2f(45f, 100f)),
            (PickupType.GoldCartridge, new Vector2f(60f, 100f)),
            (PickupType.MedKit, new Vector2f(75f, 100f))
        };
        foreach (var p in pickupTypes)
        {
            var item = new PickupItem();
            item.Spawn(p.Item1, p.Item2);
            theme.DrawPickup(fb, in item, (int)item.Position.X, (int)item.Position.Y);
        }

        // 7. HUD
        theme.DrawHud(fb, player, "RETRO_OCEAN", 60);

        // Ensure framebuffer contains drawn data
        bool hasPixels = false;
        for (int i = 0; i < fb.Pixels.Length; i++)
        {
            if (fb.Pixels[i] != 0)
            {
                hasPixels = true;
                break;
            }
        }
        Assert.True(hasPixels, "Framebuffer should contain rendered pixels.");
    }
}
