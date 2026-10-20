using System;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Centralized, theme-agnostic rendering engine.
/// Executes all parallax scrolling math, animation frame selection, directional sprite flipping,
/// hit-flash tinting, item glow/bobbing, and HUD layout using any <see cref="ThemeAssetPack"/>.
/// </summary>
public static class GameRenderer
{
    public static void DrawBackground(FrameBuffer fb, ThemeAssetPack assets, Camera2D camera)
    {
        DrawParallaxLayer(fb, assets.BackgroundFar,  camera.X, 0.1f);
        DrawParallaxLayer(fb, assets.BackgroundMid,  camera.X, 0.2f);
        DrawParallaxLayer(fb, assets.BackgroundNear, camera.X, 0.5f);
    }

    private static void DrawParallaxLayer(FrameBuffer fb, EgaSprite layer, float cameraX, float parallaxSpeed)
    {
        int width = layer.Width;
        int height = layer.Height;
        if (width <= 0 || height <= 0) return;

        int scroll = (int)(cameraX * parallaxSpeed) % width;
        if (scroll < 0) scroll += width;

        int playfieldHeight = FrameBuffer.Height - 16;
        for (int py = 16; py < FrameBuffer.Height; py++)
        {
            int sy = ((py - 16) * height) / playfieldHeight;
            int srcRowOffset = sy * width;
            int dstRowOffset = py * FrameBuffer.Width;

            for (int px = 0; px < FrameBuffer.Width; px++)
            {
                int sx = (px + scroll) % width;
                byte colorIdx = layer.Pixels[srcRowOffset + sx];
                if (colorIdx != EgaSprite.Transparent)
                {
                    fb.Pixels[dstRowOffset + px] = EgaPalette.GetRgba((EgaColorIndex)colorIdx);
                }
            }
        }
    }

    public static void DrawTile(FrameBuffer fb, ThemeAssetPack assets, TileType type, int screenX, int screenY, int col, int row)
    {
        if (screenX <= -16 || screenX >= 320 || screenY <= -16 || screenY >= 200) return;

        EgaSprite? tileSprite = type switch
        {
            TileType.Solid => assets.SolidTile,
            TileType.OneWayPlatform => assets.OneWayTileVariants[Math.Abs(col) % assets.OneWayTileVariants.Length],
            TileType.Hazard => assets.HazardTile,
            TileType.Border => assets.BorderTile,
            TileType.Ladder => assets.LadderTile,
            TileType.Crate => assets.CrateTile,
            _ => null
        };

        if (tileSprite != null)
        {
            fb.DrawSprite(tileSprite, screenX, screenY);
        }
    }

    public static void DrawPlayer(FrameBuffer fb, ThemeAssetPack assets, PlayerEntity player, int screenX, int screenY)
    {
        // 1.2s I-frame damage flicker: blinks on/off every 4 frames
        if (player.InvulnerabilityTimer > 0 && ((int)(player.InvulnerabilityTimer * 20) % 2 == 0))
        {
            return;
        }

        // Speed Dash afterimage trail
        if (player.HasSpeedDash && (player.State == PlayerState.Running || player.State == PlayerState.Walking))
        {
            int trailX = (player.Facing == Direction.Right) ? screenX - 3 : screenX + 3;
            fb.DrawVLine(trailX + 6, screenY + 4, 16, EgaColorIndex.BrightCyan);
        }

        if (player.State == PlayerState.Climbing)
        {
            int climbFrame = Math.Abs((int)(player.AnimTimer * 8)) % assets.PlayerClimb.Length;
            fb.DrawSprite(assets.PlayerClimb[climbFrame], screenX, screenY);
            return;
        }

        bool flipX = player.Facing == Direction.Left;
        EgaSprite sprite;

        if (player.State == PlayerState.Walking || player.State == PlayerState.Running)
        {
            int cadence = (player.State == PlayerState.Running) ? 14 : 9;
            int phase = Math.Abs((int)(player.AnimTimer * cadence)) % assets.PlayerWalk.Length;
            sprite = assets.PlayerWalk[phase];
        }
        else if (player.State == PlayerState.Jumping || player.State == PlayerState.Falling)
        {
            sprite = assets.PlayerJump;
        }
        else
        {
            sprite = assets.PlayerIdle;
        }

        fb.DrawSprite(sprite, screenX, screenY, flipX);
    }

    public static void DrawProjectile(FrameBuffer fb, ThemeAssetPack assets, in Projectile projectile, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 200) return;

        EgaSprite sprite = projectile.FromPlayer ? assets.PlayerProjectile : assets.MonsterProjectile;
        bool flipX = projectile.Velocity.X < 0;
        fb.DrawSprite(sprite, screenX, screenY, flipX);
    }

    public static void DrawMonster(FrameBuffer fb, ThemeAssetPack assets, MonsterEntity monster, int screenX, int screenY)
    {
        if (screenX < -32 || screenX > 320 || screenY < -32 || screenY > 200) return;

        bool hitFlash = monster.HitFlashTimer > 0;
        bool flipX = monster.Facing == Direction.Left;

        EgaSprite[] frames = monster.Type switch
        {
            MonsterType.PatrolDroid => assets.PatrolDroid,
            MonsterType.HoverDrone => assets.HoverDrone,
            MonsterType.CeilingTurret => assets.CeilingTurret,
            MonsterType.MechaHound => assets.MechaHound,
            MonsterType.HeavyGuard => assets.HeavyGuard,
            _ => assets.PatrolDroid
        };

        int cadence = monster.Type switch
        {
            MonsterType.HoverDrone => 12,
            MonsterType.MechaHound => 12,
            MonsterType.HeavyGuard => 6,
            _ => 8
        };

        int frameIdx = Math.Abs((int)(monster.AnimTimer * cadence)) % frames.Length;
        fb.DrawSprite(frames[frameIdx], screenX, screenY, flipX, hitFlash);
    }

    public static void DrawPickup(FrameBuffer fb, ThemeAssetPack assets, in PickupItem pickup, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 200) return;

        // Shared item glow/glint animation cycle across all themes
        int glintFrame = Math.Abs((int)(pickup.AnimTimer * 5)) % 2;
        // Subtle 1px vertical hover bob for collectible items
        int bobOffset = ((int)(pickup.AnimTimer * 4) % 2 == 0) ? 0 : -1;

        EgaSprite sprite = pickup.Type switch
        {
            PickupType.BlueGem => assets.BlueGem[glintFrame % assets.BlueGem.Length],
            PickupType.RedRuby => assets.RedRuby[glintFrame % assets.RedRuby.Length],
            PickupType.GoldCartridge => assets.GoldCartridge[glintFrame % assets.GoldCartridge.Length],
            PickupType.MedKit => assets.MedKit[glintFrame % assets.MedKit.Length],
            PickupType.RapidFire => assets.RapidFireBadge,
            PickupType.HighJumpBoots => assets.HighJumpBadge,
            PickupType.SpeedDash => assets.SpeedDashBadge,
            _ => assets.BlueGem[0]
        };

        fb.DrawSprite(sprite, screenX, screenY + bobOffset);
    }

    public static void DrawHud(FrameBuffer fb, ThemeAssetPack assets, PlayerEntity player, string seed, int fps)
    {
        // Top status console (320x16)
        fb.FillRect(0, 0, 320, 16, EgaColorIndex.LightGray);
        fb.DrawRect(0, 0, 320, 16, EgaColorIndex.Black);
        fb.DrawHLine(1, 1, 318, EgaColorIndex.BrightWhite);

        // SCORE Display (widened to 106px so all 6 digits fit cleanly inside the inset box)
        fb.FillRect(4, 2, 106, 12, EgaColorIndex.Black);
        fb.DrawRect(4, 2, 106, 12, EgaColorIndex.DarkGray);
        fb.DrawText(6, 4, "SCORE:", EgaColorIndex.Yellow);
        fb.DrawInt(56, 4, player.Score, 6, EgaColorIndex.BrightWhite);

        // HEALTH Display: 8 vertical bars
        fb.FillRect(114, 2, 98, 12, EgaColorIndex.Black);
        fb.DrawRect(114, 2, 98, 12, EgaColorIndex.DarkGray);
        fb.DrawText(117, 4, "HP:", EgaColorIndex.BrightRed);
        for (int i = 0; i < PlayerEntity.MaxHealth; i++)
        {
            int cellX = 142 + i * 8;
            if (i < player.Health)
            {
                fb.FillRect(cellX, 4, 6, 8, EgaColorIndex.BrightGreen);
                fb.DrawRect(cellX, 4, 6, 8, EgaColorIndex.Black);
                fb.DrawHLine(cellX + 1, 5, 4, EgaColorIndex.BrightWhite);
            }
            else
            {
                fb.DrawRect(cellX, 4, 6, 8, EgaColorIndex.Red);
            }
        }

        // Active Powerup Badges (using theme's static badge sprites)
        int badgeX = 218;
        if (player.HasRapidFire)
        {
            fb.DrawSprite(assets.RapidFireBadge, badgeX, 1);
            badgeX += 15;
        }
        if (player.HasHighJumpBoots)
        {
            fb.DrawSprite(assets.HighJumpBadge, badgeX, 1);
            badgeX += 15;
        }
        if (player.HasSpeedDash)
        {
            fb.DrawSprite(assets.SpeedDashBadge, badgeX, 1);
            badgeX += 15;
        }

        if (player.IsGodMode)
        {
            fb.DrawText(badgeX, 4, "[GOD]", EgaColorIndex.Yellow);
        }
    }
}
