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

        // Must have non-zero pixels in HUD areas
        uint topPixel = fb.Pixels[5 * FrameBuffer.Width + 10];
        uint bottomPixel = fb.Pixels[190 * FrameBuffer.Width + 10];
        Assert.NotEqual(0u, topPixel);
        Assert.NotEqual(0u, bottomPixel);
    }

    [Fact]
    public void RenderShowcase_ProducesValidBmp()
    {
        var theme = new DefaultTheme();
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
                theme.DrawTile(fb, TileType.Hazard, c * 16, 160, c, 10);
            }
            else
            {
                theme.DrawTile(fb, TileType.Solid, c * 16, 160, c, 10);
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
        theme.DrawHud(fb, hudPlayer, "EGA_SHOWCASE", 60);

        // Save BMP
        SaveBmp("showcase.bmp", fb);
        Assert.True(System.IO.File.Exists("showcase.bmp"));
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
}
