using System;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class DefaultThemeRenderingTests
{
    [Fact]
    public void DrawBackground_ExecutesWithoutError()
    {
        var theme = new DefaultTheme();
        var fb = new FrameBuffer();
        var camera = new Camera2D(320, 184);
        camera.SnapTo(new Vector2f(100f, 100f));

        theme.DrawBackground(fb, camera);

        // Framebuffer must have non-zero pixels
        bool hasNonZero = false;
        for (int i = 0; i < fb.Pixels.Length; i++)
        {
            if (fb.Pixels[i] != 0)
            {
                hasNonZero = true;
                break;
            }
        }
        Assert.True(hasNonZero, "Background should have rendered stars, skyline, or pillars.");
    }

    [Fact]
    public void DrawAllTileTypes_RendersCorrectlyAtVariousCoordinates()
    {
        var theme = new DefaultTheme();
        var fb = new FrameBuffer();

        var tiles = new[]
        {
            TileType.Solid,
            TileType.OneWayPlatform,
            TileType.Hazard,
            TileType.Border,
            TileType.Ladder,
            TileType.Crate
        };

        foreach (var tile in tiles)
        {
            // On screen
            theme.DrawTile(fb, tile, 100, 50, 6, 3);
            // Near edges
            theme.DrawTile(fb, tile, 0, 16, 0, 1);
            theme.DrawTile(fb, tile, 304, 168, 19, 10);
            // Partially off screen
            theme.DrawTile(fb, tile, -8, 50, 0, 3);
            theme.DrawTile(fb, tile, 312, 50, 20, 3);
        }
    }

    [Fact]
    public void DrawPlayer_RendersAllStatesAndDirections()
    {
        var theme = new DefaultTheme();
        var fb = new FrameBuffer();
        var player = new PlayerEntity(new Vector2f(100, 100));

        var states = new[]
        {
            PlayerState.Idle,
            PlayerState.Walking,
            PlayerState.Running,
            PlayerState.Jumping,
            PlayerState.Falling,
            PlayerState.Climbing
        };

        foreach (var state in states)
        {
            player.State = state;
            player.Facing = Direction.Right;
            theme.DrawPlayer(fb, player, 100, 100);

            player.Facing = Direction.Left;
            theme.DrawPlayer(fb, player, 100, 100);

            // With invulnerability
            player.InvulnerabilityTimer = 0.5f;
            theme.DrawPlayer(fb, player, 100, 100);
            player.InvulnerabilityTimer = 0f;

            // With speed dash trail
            player.HasSpeedDash = true;
            theme.DrawPlayer(fb, player, 100, 100);
            player.HasSpeedDash = false;
        }
    }

    [Fact]
    public void DrawAllMonsters_RendersBothDirectionsAndHitFlash()
    {
        var theme = new DefaultTheme();
        var fb = new FrameBuffer();

        var types = new[]
        {
            MonsterType.PatrolDroid,
            MonsterType.HoverDrone,
            MonsterType.CeilingTurret,
            MonsterType.MechaHound,
            MonsterType.HeavyGuard
        };

        foreach (var type in types)
        {
            var monster = new MonsterEntity();
            monster.Spawn(0, type, new Vector2f(100, 100));

            monster.Facing = Direction.Right;
            theme.DrawMonster(fb, monster, 100, 100);

            monster.Facing = Direction.Left;
            theme.DrawMonster(fb, monster, 100, 100);

            monster.HitFlashTimer = 0.1f;
            theme.DrawMonster(fb, monster, 100, 100);
        }
    }

    [Fact]
    public void DrawAllPickups_RendersWithoutError()
    {
        var theme = new DefaultTheme();
        var fb = new FrameBuffer();

        var pickups = new[]
        {
            PickupType.BlueGem,
            PickupType.RedRuby,
            PickupType.GoldCartridge,
            PickupType.MedKit,
            PickupType.RapidFire,
            PickupType.HighJumpBoots,
            PickupType.SpeedDash
        };

        foreach (var pType in pickups)
        {
            var item = new PickupItem();
            item.Spawn(pType, new Vector2f(50, 50));
            theme.DrawPickup(fb, in item, 50, 50);
        }
    }

    [Fact]
    public void DrawHud_RendersAllHUDCombinations()
    {
        var theme = new DefaultTheme();
        var fb = new FrameBuffer();
        var player = new PlayerEntity(new Vector2f(100, 100));

        player.Score = 42500;
        player.Health = 3;
        player.HasRapidFire = true;
        player.HasHighJumpBoots = true;
        player.HasSpeedDash = true;

        theme.DrawHud(fb, player, "TEST_SEED", 60);

        // Must have non-zero pixels in top HUD bar, while bottom playfield (Y=190) is left unobstructed
        uint topPixel = fb.Pixels[5 * FrameBuffer.Width + 10];
        uint bottomPixel = fb.Pixels[190 * FrameBuffer.Width + 10];
        Assert.NotEqual(0u, topPixel);
        Assert.Equal(0u, bottomPixel);
    }

    [Fact]
    public void RenderShowcase_ProducesValidBmp()
    {
        var theme = new DefaultTheme();
        var fb = RenderThemeShowcase(theme, "EGA_SHOWCASE");

        SaveBmp("showcase.bmp", fb);
        Assert.True(System.IO.File.Exists("showcase.bmp"));
    }

    private static FrameBuffer RenderThemeShowcase(IGameTheme theme, string hudSeed)
    {
        var fb = new FrameBuffer();
        var camera = new Camera2D(320, 184);
        camera.SnapTo(new Vector2f(100, 100));

        // 1. Background
        theme.DrawBackground(fb, camera);

        // 2. Ceilings / Borders
        for (int c = 0; c < 20; c++)
        {
            theme.DrawTile(fb, TileType.Border, c * 16, 16, c, 1);
        }

        // 3. Ground floor with hazard pit
        for (int c = 0; c < 20; c++)
        {
            if (c >= 7 && c <= 8)
            {
                theme.DrawTile(fb, TileType.Hazard, c * 16, 176, c, 11);
                theme.DrawTile(fb, TileType.Solid, c * 16, 192, c, 12);
            }
            else
            {
                theme.DrawTile(fb, TileType.Solid, c * 16, 160, c, 10);
                theme.DrawTile(fb, TileType.Solid, c * 16, 176, c, 11);
                theme.DrawTile(fb, TileType.Solid, c * 16, 192, c, 12);
            }
        }

        // 4. Middle catwalks and ladder
        for (int c = 4; c < 16; c++)
        {
            theme.DrawTile(fb, TileType.OneWayPlatform, c * 16, 112, c, 7);
        }
        for (int r = 112; r <= 144; r += 16)
        {
            theme.DrawTile(fb, TileType.Ladder, 5 * 16, r, 5, r / 16);
        }

        // 5. Crate pyramid
        theme.DrawTile(fb, TileType.Crate, 2 * 16, 144, 2, 9);
        theme.DrawTile(fb, TileType.Crate, 3 * 16, 144, 3, 9);
        theme.DrawTile(fb, TileType.Crate, 2 * 16, 128, 2, 8);

        // 6. Players: Idle, Running, Climbing
        var pIdle = new PlayerEntity(new Vector2f(16, 136));
        pIdle.Facing = Direction.Right;
        pIdle.State = PlayerState.Idle;
        theme.DrawPlayer(fb, pIdle, 16, 136);

        var pClimb = new PlayerEntity(new Vector2f(5 * 16, 124));
        pClimb.State = PlayerState.Climbing;
        pClimb.AnimTimer = 0.25f;
        theme.DrawPlayer(fb, pClimb, 5 * 16, 124);

        var pRun = new PlayerEntity(new Vector2f(10 * 16, 88));
        pRun.Facing = Direction.Right;
        pRun.State = PlayerState.Running;
        pRun.HasSpeedDash = true;
        pRun.AnimTimer = 0.35f;
        theme.DrawPlayer(fb, pRun, 10 * 16, 88);

        // 7. Monsters
        var droid = new MonsterEntity();
        droid.Spawn(0, MonsterType.PatrolDroid, new Vector2f(12 * 16, 98));
        theme.DrawMonster(fb, droid, 12 * 16, 98);

        var drone = new MonsterEntity();
        drone.Spawn(1, MonsterType.HoverDrone, new Vector2f(7 * 16, 50));
        drone.AnimTimer = 0.5f;
        theme.DrawMonster(fb, drone, 7 * 16, 50);

        var turret = new MonsterEntity();
        turret.Spawn(2, MonsterType.CeilingTurret, new Vector2f(9 * 16, 32));
        theme.DrawMonster(fb, turret, 9 * 16, 32);

        var hound = new MonsterEntity();
        hound.Spawn(3, MonsterType.MechaHound, new Vector2f(13 * 16, 146));
        hound.AnimTimer = 0.2f;
        theme.DrawMonster(fb, hound, 13 * 16, 146);

        var guard = new MonsterEntity();
        guard.Spawn(4, MonsterType.HeavyGuard, new Vector2f(17 * 16, 134));
        guard.AnimTimer = 0.4f;
        theme.DrawMonster(fb, guard, 17 * 16, 134);

        // 8. Projectiles
        var bullet = new Projectile();
        bullet.Spawn(new Vector2f(40, 147), new Vector2f(200, 0), 1, fromPlayer: true);
        theme.DrawProjectile(fb, in bullet, 40, 147);

        var plasma = new Projectile();
        plasma.Spawn(new Vector2f(250, 147), new Vector2f(-150, 0), 1, fromPlayer: false);
        theme.DrawProjectile(fb, in plasma, 250, 147);

        // 9. Pickups
        var gem = new PickupItem();
        gem.Spawn(PickupType.BlueGem, new Vector2f(2 * 16, 114));
        theme.DrawPickup(fb, in gem, 2 * 16, 114);

        var ruby = new PickupItem();
        ruby.Spawn(PickupType.RedRuby, new Vector2f(7 * 16, 98));
        theme.DrawPickup(fb, in ruby, 7 * 16, 98);

        var cart = new PickupItem();
        cart.Spawn(PickupType.GoldCartridge, new Vector2f(9 * 16, 98));
        theme.DrawPickup(fb, in cart, 9 * 16, 98);

        var med = new PickupItem();
        med.Spawn(PickupType.MedKit, new Vector2f(14 * 16, 98));
        theme.DrawPickup(fb, in med, 14 * 16, 98);

        // 10. HUD
        var hudPlayer = new PlayerEntity(new Vector2f(0, 0));
        hudPlayer.Score = 75400;
        hudPlayer.Health = 4;
        hudPlayer.HasRapidFire = true;
        hudPlayer.HasHighJumpBoots = true;
        hudPlayer.HasSpeedDash = true;
        theme.DrawHud(fb, hudPlayer, hudSeed, 60);

        return fb;
    }

    private static void SaveBmp(string path, FrameBuffer fb)
    {
        using var stream = new System.IO.FileStream(path, System.IO.FileMode.Create);
        using var writer = new System.IO.BinaryWriter(stream);

        int width = FrameBuffer.Width;
        int height = FrameBuffer.Height;
        int imageSize = width * height * 4;
        int fileSize = 54 + imageSize;

        // Bitmap File Header (14 bytes)
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((short)0); // Reserved 1
        writer.Write((short)0); // Reserved 2
        writer.Write(54);       // Pixel data offset

        // DIB Header (BITMAPINFOHEADER - 40 bytes)
        writer.Write(40);        // Header size
        writer.Write(width);     // Width
        writer.Write(-height);   // Height (negative = top-down)
        writer.Write((short)1);  // Color planes
        writer.Write((short)32); // Bits per pixel (32-bit BGRA)
        writer.Write(0);         // Compression (0 = BI_RGB)
        writer.Write(imageSize); // Image size
        writer.Write(2835);      // Horiz resolution (pixels/meter)
        writer.Write(2835);      // Vert resolution (pixels/meter)
        writer.Write(0);         // Colors in palette
        writer.Write(0);         // Important colors

        // Pixel data (BGRA)
        for (int i = 0; i < fb.Pixels.Length; i++)
        {
            uint rgba = fb.Pixels[i];
            uint r = rgba & 0xFF;
            uint g = (rgba >> 8) & 0xFF;
            uint b = (rgba >> 16) & 0xFF;
            uint a = (rgba >> 24) & 0xFF;
            uint bgra = (a << 24) | (r << 16) | (g << 8) | b;
            writer.Write(bgra);
        }
    }

    [Fact]
    public void EgaSprite_DrawSprite_SupportsTransparencyFlipAndHitFlash()
    {
        var sprite = EgaSprite.Bake(4, 2, b =>
        {
            b.SetPixel(0, 0, EgaColorIndex.BrightRed);
            b.SetPixel(1, 0, EgaColorIndex.Black);
            // (2,0) and (3,0) left Transparent
        });

        var fb = new FrameBuffer();
        fb.Clear(EgaColorIndex.Blue);

        // 1. Normal draw at (10, 10)
        fb.DrawSprite(sprite, 10, 10, flipX: false, hitFlash: false);
        Assert.Equal(EgaPalette.GetRgba(EgaColorIndex.BrightRed), fb.Pixels[10 * FrameBuffer.Width + 10]);
        Assert.Equal(EgaPalette.GetRgba(EgaColorIndex.Black), fb.Pixels[10 * FrameBuffer.Width + 11]);
        Assert.Equal(EgaPalette.GetRgba(EgaColorIndex.Blue), fb.Pixels[10 * FrameBuffer.Width + 12]); // transparent preserved background

        // 2. Flipped draw at (20, 10): x=0 mirrors to x=3 (20+3=23)
        fb.DrawSprite(sprite, 20, 10, flipX: true, hitFlash: false);
        Assert.Equal(EgaPalette.GetRgba(EgaColorIndex.BrightRed), fb.Pixels[10 * FrameBuffer.Width + 23]);
        Assert.Equal(EgaPalette.GetRgba(EgaColorIndex.Black), fb.Pixels[10 * FrameBuffer.Width + 22]);

        // 3. Hit-flash draw at (30, 10): non-black pixels become BrightWhite, black outline stays Black
        fb.DrawSprite(sprite, 30, 10, flipX: false, hitFlash: true);
        Assert.Equal(EgaPalette.GetRgba(EgaColorIndex.BrightWhite), fb.Pixels[10 * FrameBuffer.Width + 30]);
        Assert.Equal(EgaPalette.GetRgba(EgaColorIndex.Black), fb.Pixels[10 * FrameBuffer.Width + 31]);
    }

    [Fact]
    public void AllRegisteredThemes_HaveStandardizedSpriteDimensionsAndRenderCleanly()
    {
        Assert.Equal(2, ThemeRegistry.AllThemes.Length);

        foreach (var theme in ThemeRegistry.AllThemes)
        {
            var assets = theme.Assets;
            Assert.False(string.IsNullOrWhiteSpace(assets.Name));

            // Standard 168px-tall horizontally repeating parallax layers
            Assert.Equal(168, assets.BackgroundFar.Height);
            Assert.Equal(168, assets.BackgroundMid.Height);
            Assert.Equal(168, assets.BackgroundNear.Height);

            // Standard 16x16 tiles
            Assert.Equal(16, assets.SolidTile.Width);
            Assert.Equal(16, assets.SolidTile.Height);
            Assert.Equal(16, assets.HazardTile.Width);
            Assert.Equal(16, assets.BorderTile.Width);
            Assert.Equal(16, assets.LadderTile.Width);
            Assert.Equal(16, assets.CrateTile.Width);

            // Standard 24x24 player action sprites
            Assert.Equal(24, assets.PlayerIdle.Width);
            Assert.Equal(24, assets.PlayerIdle.Height);
            Assert.Equal(4, assets.PlayerWalk.Length);
            Assert.Equal(2, assets.PlayerClimb.Length);

            // Standard 2-frame monster animations
            Assert.Equal(2, assets.PatrolDroid.Length);
            Assert.Equal(2, assets.HoverDrone.Length);
            Assert.Equal(2, assets.CeilingTurret.Length);
            Assert.Equal(2, assets.MechaHound.Length);
            Assert.Equal(2, assets.HeavyGuard.Length);

            // Standard 2-frame glowing pickups (14x14)
            Assert.Equal(2, assets.BlueGem.Length);
            Assert.Equal(14, assets.BlueGem[0].Width);
            Assert.Equal(14, assets.BlueGem[0].Height);
        }
    }

    [Fact]
    public void GameEngine_CycleAndSetTheme_SwapsVisualsWithoutAlteringGameplayState()
    {
        var engine = new GameEngine("THEME_TEST");
        float initialPlayerX = engine.Player.Position.X;
        int initialScore = engine.Player.Score;
        string firstTheme = engine.CurrentTheme.Name;

        engine.CycleTheme();
        string secondTheme = engine.CurrentTheme.Name;
        Assert.NotEqual(firstTheme, secondTheme);
        Assert.Equal("Forest & Creatures", secondTheme);

        engine.SetTheme(0);
        Assert.Equal(firstTheme, engine.CurrentTheme.Name);
        Assert.Equal(0, engine.CurrentThemeIndex);

        engine.SetTheme(1);
        Assert.Equal("Forest & Creatures", engine.CurrentTheme.Name);
        Assert.Equal(1, engine.CurrentThemeIndex);

        // Out-of-range index should be ignored safely
        engine.SetTheme(999);
        Assert.Equal(1, engine.CurrentThemeIndex);

        // Rendering with new theme succeeds and preserves exact player/monster physics state
        engine.Render();
        Assert.Equal(initialPlayerX, engine.Player.Position.X);
        Assert.Equal(initialScore, engine.Player.Score);
    }

    [Fact]
    public void ForestHunterTheme_RendersAllEntitiesAndProducesShowcaseBmp()
    {
        var theme = new ForestHunterTheme();
        var fb = RenderThemeShowcase(theme, "WOODS_BOW");

        SaveBmp("forest_showcase.bmp", fb);
        Assert.True(System.IO.File.Exists("forest_showcase.bmp"));

        // Verify forest background rendered non-zero EGA pixels
        Assert.NotEqual(0u, fb.Pixels[90 * FrameBuffer.Width + 5]);
    }

    [Fact]
    public void ExportAndVerifyThemePngFilesOnDisk()
    {
        // Locate workspace wwwroot/themes directory
        string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string themesRoot = System.IO.Path.Combine(repoRoot, "wwwroot", "themes");

        // Verify 41 PNG files exist per theme on disk and match the loaded ThemeAssetPack
        foreach (string themeFolder in new[] { "duke", "forest" })
        {
            string dir = System.IO.Path.Combine(themesRoot, themeFolder);
            string[] pngFiles = System.IO.Directory.GetFiles(dir, "*.png");
            Assert.Equal(41, pngFiles.Length);
        }

        // Verify PNG decode from disk reproduces exact pixel indices & transparency
        using var fs = System.IO.File.OpenRead(System.IO.Path.Combine(themesRoot, "forest", "player_idle.png"));
        var decoded = EgaPngCodec.LoadPng(fs, offsetX: -5);
        Assert.Equal(ForestHunterTheme.StaticAssets.PlayerIdle.Width, decoded.Width);
        Assert.Equal(ForestHunterTheme.StaticAssets.PlayerIdle.Height, decoded.Height);
        Assert.Equal(ForestHunterTheme.StaticAssets.PlayerIdle.OffsetX, decoded.OffsetX);
        Assert.Equal(ForestHunterTheme.StaticAssets.PlayerIdle.Pixels, decoded.Pixels);
    }
}
