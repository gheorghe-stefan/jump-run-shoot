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

    public static void DrawDeathEffect(FrameBuffer fb, ThemeAssetPack assets, in DeathEffect effect, int screenX, int screenY)
    {
        if (screenX < -32 || screenX > 320 || screenY < -32 || screenY > 200) return;

        int cx = screenX + (int)(effect.Width * 0.5f);
        int cy = screenY + (int)(effect.Height * 0.5f);
        float t = Math.Clamp(effect.Timer / Math.Max(0.01f, effect.Duration), 0.0f, 1.0f);

        bool isLarge = effect.MonsterType == MonsterType.HeavyGuard;
        bool isWide = effect.MonsterType == MonsterType.MechaHound;
        bool isUnderwater = assets.ZeroGravity || assets.Name.Contains("Underwater", StringComparison.OrdinalIgnoreCase);
        bool isForest = assets.Name.Contains("Forest", StringComparison.OrdinalIgnoreCase);

        if (isUnderwater)
        {
            // === Underwater: Cavitation flash ring, buoyant rising bubbles & aquatic pop ===
            if (t < 0.22f)
            {
                int rCav = 2 + (int)((t / 0.22f) * (isLarge ? 16 : 10));
                fb.DrawCircle(cx, cy, rCav, EgaColorIndex.BrightCyan);
                if (rCav > 2) fb.DrawCircle(cx, cy, rCav - 1, EgaColorIndex.BrightWhite);
            }

            int count = isLarge ? 14 : 9;
            for (int k = 0; k < count; k++)
            {
                float angle = k * (MathF.Tau / count);
                float speed = 24.0f + (k % 3) * 12.0f;
                float bx = cx + MathF.Cos(angle) * speed * t * (1.0f - 0.4f * t) + MathF.Sin(t * 14.0f + k) * 3.0f;
                float by = cy + MathF.Sin(angle) * speed * 0.35f * t - (50.0f + (k % 4) * 12.0f) * t;
                int ibx = (int)bx;
                int iby = (int)by;

                if (t < 0.72f)
                {
                    if (k % 3 == 0)
                    {
                        fb.DrawRect(ibx - 1, iby - 1, 3, 3, EgaColorIndex.Cyan);
                        fb.SetPixel(ibx, iby - 1, EgaColorIndex.BrightWhite);
                        fb.SetPixel(ibx, iby, EgaColorIndex.BrightCyan);
                    }
                    else
                    {
                        fb.SetPixel(ibx, iby, EgaColorIndex.BrightCyan);
                        fb.SetPixel(ibx, iby - 1, EgaColorIndex.BrightWhite);
                    }
                }
                else
                {
                    fb.DrawSparkle(ibx, iby, EgaColorIndex.BrightCyan);
                }
            }
        }
        else if (isForest)
        {
            // === Forest: Magical enchanted woodland smoke puffs, fluttering leaves & spores ===
            if (t < 0.60f)
            {
                float puffProg = t / 0.60f;
                int maxR = isLarge ? 14 : 8;
                int rPuff = (int)(maxR * MathF.Sin(puffProg * MathF.PI));
                if (rPuff > 0)
                {
                    EgaColorIndex puffColor = puffProg < 0.35f ? EgaColorIndex.BrightGreen : (puffProg < 0.7f ? EgaColorIndex.Green : EgaColorIndex.DarkGray);
                    fb.FillCircle(cx, cy, rPuff, puffColor);
                    if (rPuff > 2) fb.FillCircle(cx - 3, cy - 2, rPuff - 2, puffProg < 0.35f ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray);
                    if (rPuff > 3) fb.FillCircle(cx + 3, cy + 1, rPuff - 3, puffProg < 0.4f ? EgaColorIndex.Yellow : EgaColorIndex.Brown);
                }
            }

            int leafCount = isLarge ? 14 : 8;
            for (int k = 0; k < leafCount; k++)
            {
                float angle = k * (MathF.Tau / leafCount);
                float speed = 36.0f + (k % 3) * 14.0f;
                float lx = cx + MathF.Cos(angle) * speed * t + MathF.Sin(t * 12.0f + k) * 4.0f;
                float ly = cy + MathF.Sin(angle) * speed * 0.6f * t + 24.0f * t;
                int ilx = (int)lx;
                int ily = (int)ly;

                if (k % 2 == 0)
                {
                    fb.SetPixel(ilx, ily, EgaColorIndex.BrightGreen);
                    fb.SetPixel(ilx + 1, ily, EgaColorIndex.Green);
                    fb.SetPixel(ilx, ily + 1, EgaColorIndex.DarkGray);
                }
                else
                {
                    fb.DrawSparkle(ilx, ily, EgaColorIndex.Yellow);
                }
            }
        }
        else
        {
            // === Duke 1 Classic: Multi-stage retro mechanical explosion & ballistic shrapnel ===
            if (t < 0.22f)
            {
                int starR = 2 + (int)((t / 0.22f) * (isLarge ? 12 : 7));
                fb.DrawHLine(cx - starR, cy, starR * 2 + 1, EgaColorIndex.BrightWhite);
                fb.DrawVLine(cx, cy - starR, starR * 2 + 1, EgaColorIndex.BrightWhite);
                fb.DrawHLine(cx - starR / 2, cy - 1, starR + 1, EgaColorIndex.Yellow);
                fb.DrawHLine(cx - starR / 2, cy + 1, starR + 1, EgaColorIndex.Yellow);
            }

            if (t >= 0.06f && t <= 0.65f)
            {
                float blastProg = (t - 0.06f) / 0.59f;
                int maxBlastR = isLarge ? 16 : (isWide ? 11 : 9);
                int rBlast = (int)(maxBlastR * MathF.Sin(blastProg * MathF.PI));
                if (rBlast > 0)
                {
                    fb.FillCircle(cx, cy, rBlast, blastProg < 0.4f ? EgaColorIndex.BrightRed : EgaColorIndex.Red);
                    if (rBlast > 2) fb.FillCircle(cx, cy, rBlast - 2, blastProg < 0.4f ? EgaColorIndex.Yellow : EgaColorIndex.BrightRed);
                    if (rBlast > 4) fb.FillCircle(cx, cy, rBlast - 4, EgaColorIndex.BrightWhite);
                    if (blastProg > 0.5f && rBlast > 1)
                    {
                        fb.DrawCircle(cx, cy, rBlast, EgaColorIndex.DarkGray);
                    }
                }
            }

            int sparkCount = isLarge ? 14 : 8;
            for (int k = 0; k < sparkCount; k++)
            {
                float angle = k * (MathF.Tau / sparkCount) + 0.25f;
                float speed = 42.0f + (k % 4) * 14.0f;
                float sx = cx + MathF.Cos(angle) * speed * t;
                float sy = cy + MathF.Sin(angle) * speed * t + 0.5f * 130.0f * t * t;
                int isx = (int)sx;
                int isy = (int)sy;

                EgaColorIndex col = t < 0.4f ? EgaColorIndex.BrightWhite : (t < 0.7f ? EgaColorIndex.Yellow : (t < 0.88f ? EgaColorIndex.BrightRed : EgaColorIndex.DarkGray));
                fb.SetPixel(isx, isy, col);
                if (isLarge || (k % 2 == 0)) fb.SetPixel(isx + 1, isy, col);
            }
        }
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

        string cheatStatus = "";
        if (player.IsGodMode) cheatStatus += "G";
        if (player.IsFlying && !assets.ZeroGravity) cheatStatus += "F";
        if (player.IsNoClip) cheatStatus += "N";
        if (!string.IsNullOrEmpty(cheatStatus))
        {
            fb.DrawText(badgeX, 4, $"[{cheatStatus}]", EgaColorIndex.Yellow);
        }
    }
}
