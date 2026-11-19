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

        // Radiant forcefield shield aura while invulnerability powerup is active
        if (player.HasInvulnerabilityPowerup)
        {
            int playerCx = screenX + 7;
            int playerCy = screenY + 10;
            bool pulse = ((int)(player.InvulnerabilityPowerupTimer * 8) % 2 == 0);
            int auraR = 14 + (int)(MathF.Sin(player.AnimTimer * 12.0f) * 2.0f);
            fb.DrawCircle(playerCx, playerCy, auraR, pulse ? EgaColorIndex.BrightRed : EgaColorIndex.Yellow);
            fb.DrawSparkle(playerCx - auraR, playerCy, EgaColorIndex.BrightWhite);
            fb.DrawSparkle(playerCx + auraR, playerCy, EgaColorIndex.BrightWhite);
        }
    }

    public static void DrawProjectile(FrameBuffer fb, ThemeAssetPack assets, in Projectile projectile, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 200) return;

        bool isCeilingShot = !projectile.FromPlayer
            && assets.CeilingProjectile != null
            && MathF.Abs(projectile.Velocity.X) < 0.01f
            && projectile.Velocity.Y > 0;

        EgaSprite sprite = projectile.FromPlayer
            ? assets.PlayerProjectile
            : (isCeilingShot ? assets.CeilingProjectile! : assets.MonsterProjectile);
        bool flipX = projectile.Velocity.X < 0;
        fb.DrawSprite(sprite, screenX, screenY, flipX);

        if (projectile.FromPlayer && projectile.Piercing)
        {
            // Glowing cyan/white plasma energy trail & sparkles
            int trailDir = flipX ? 1 : -1;
            int px = screenX + (flipX ? 8 : 0);
            int py = screenY + 1;
            fb.DrawHLine(px + trailDir * 2, py, 4, EgaColorIndex.BrightCyan);
            fb.DrawHLine(px + trailDir * 4, py, 3, EgaColorIndex.BrightWhite);
            fb.SetPixel(px + trailDir * 7, py - 1, EgaColorIndex.BrightCyan);
            fb.SetPixel(px + trailDir * 7, py + 1, EgaColorIndex.BrightCyan);
        }
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
            MonsterType.HoverDrone => 14,
            MonsterType.MechaHound => 14,
            MonsterType.PatrolDroid => 10,
            MonsterType.HeavyGuard => 10,
            _ => 8
        };

        int frameIdx = Math.Abs((int)(monster.AnimTimer * cadence)) % frames.Length;
        int hoverBobY = monster.Type == MonsterType.HoverDrone
            ? (int)MathF.Round(MathF.Sin(monster.AnimTimer * 6.0f) * 1.5f)
            : 0;
        fb.DrawSprite(frames[frameIdx], screenX, screenY + hoverBobY, flipX, hitFlash);
    }

    private static void DrawCrateBreakEffect(
        FrameBuffer fb,
        bool zeroGravity,
        bool isPolar,
        bool isForest,
        bool isUnderwater,
        bool isSpace,
        int screenX,
        int screenY,
        int cx,
        int cy,
        float t)
    {
        EgaColorIndex primary = isPolar ? EgaColorIndex.BrightWhite
            : isForest ? EgaColorIndex.Brown
            : isUnderwater ? EgaColorIndex.BrightCyan
            : isSpace ? EgaColorIndex.BrightCyan
            : EgaColorIndex.LightGray;

        EgaColorIndex accent = isPolar ? EgaColorIndex.BrightCyan
            : isForest ? EgaColorIndex.Yellow
            : isUnderwater ? EgaColorIndex.Cyan
            : isSpace ? EgaColorIndex.BrightMagenta
            : EgaColorIndex.Yellow;

        EgaColorIndex shade = isPolar ? EgaColorIndex.Cyan
            : isForest ? EgaColorIndex.DarkGray
            : isUnderwater ? EgaColorIndex.Blue
            : isSpace ? EgaColorIndex.BrightWhite
            : EgaColorIndex.DarkGray;

        // Phase 1: Brief crisp shatter flash + diagonal crack lines across the 16x16 box
        if (t < 0.22f)
        {
            fb.DrawRect(screenX + 1, screenY + 1, 14, 14, EgaColorIndex.BrightWhite);
            fb.DrawHLine(screenX + 2, cy, 12, EgaColorIndex.Black);
            fb.DrawVLine(cx, screenY + 2, 12, EgaColorIndex.Black);
            fb.DrawSparkle(cx, cy, accent);
        }

        // Phase 2: 4 tumbling quadrant box chunks flying outward (arching downward with gravity, drifting cleanly in zero-G)
        int[] dirX = { -1, 1, -1, 1 };
        int[] dirY = { -1, -1, 1, 1 };
        float gravArc = zeroGravity ? 0.0f : (26.0f * t * t);
        int chunkSize = t < 0.55f ? 5 : 3;

        for (int q = 0; q < 4; q++)
        {
            float spreadX = dirX[q] * (3.0f + 16.0f * t);
            float spreadY = dirY[q] * (3.0f + 10.0f * t) - (zeroGravity ? 0.0f : 6.0f * t) + gravArc;
            int qx = cx + (int)spreadX - chunkSize / 2;
            int qy = cy + (int)spreadY - chunkSize / 2;

            EgaColorIndex fill = (q % 2 == 0) ? primary : accent;
            fb.FillRect(qx, qy, chunkSize, chunkSize, fill);
            fb.DrawRect(qx, qy, chunkSize, chunkSize, EgaColorIndex.Black);
        }

        // Phase 3: Small crisp splinter / snow / spark shards radiating outward
        if (t < 0.85f)
        {
            for (int k = 0; k < 6; k++)
            {
                float angle = k * (MathF.Tau / 6.0f) + 0.35f;
                float dist = 4.0f + 22.0f * t;
                int sx = cx + (int)(MathF.Cos(angle) * dist);
                int sy = cy + (int)(MathF.Sin(angle) * dist + gravArc * 0.8f);
                EgaColorIndex shardCol = (k % 3 == 0) ? EgaColorIndex.BrightWhite : ((k % 2 == 0) ? accent : shade);
                fb.FillRect(sx, sy, 2, 2, shardCol);
            }
        }
    }

    public static void DrawDeathEffect(FrameBuffer fb, ThemeAssetPack assets, in DeathEffect effect, int screenX, int screenY)
    {
        if (screenX < -32 || screenX > 320 || screenY < -32 || screenY > 200) return;

        int cx = screenX + (int)(effect.Width * 0.5f);
        int cy = screenY + (int)(effect.Height * 0.5f);
        float t = Math.Clamp(effect.Timer / Math.Max(0.01f, effect.Duration), 0.0f, 1.0f);

        bool isUnderwater = assets.Name.Contains("Underwater", StringComparison.OrdinalIgnoreCase);
        bool isSpace = assets.Name.Contains("Space", StringComparison.OrdinalIgnoreCase);
        bool isForest = assets.Name.Contains("Forest", StringComparison.OrdinalIgnoreCase);
        bool isPolar = assets.Name.Contains("Polar", StringComparison.OrdinalIgnoreCase);

        if (effect.IsCrateBreak)
        {
            DrawCrateBreakEffect(fb, assets.ZeroGravity, isPolar, isForest, isUnderwater, isSpace, screenX, screenY, cx, cy, t);
            return;
        }

        bool isLarge = effect.MonsterType == MonsterType.HeavyGuard;
        bool isWide = effect.MonsterType == MonsterType.MechaHound;

        if (isSpace)
        {
            // === Space Invaders: Expanding arcade vector shockwave ring, neon starburst & flying pixel debris ===
            if (t < 0.28f)
            {
                // Radiant cross starburst & central core flash
                int starR = 2 + (int)((t / 0.28f) * (isLarge ? 18 : 10));
                fb.DrawHLine(cx - starR, cy, starR * 2 + 1, EgaColorIndex.BrightWhite);
                fb.DrawVLine(cx, cy - starR, starR * 2 + 1, EgaColorIndex.BrightWhite);
                fb.DrawHLine(cx - starR / 2, cy - 1, starR + 1, EgaColorIndex.BrightCyan);
                fb.DrawHLine(cx - starR / 2, cy + 1, starR + 1, EgaColorIndex.BrightCyan);
                fb.DrawSparkle(cx, cy, EgaColorIndex.Yellow);
            }

            // Expanding Circular Vector Shockwave Ring
            if (t >= 0.08f && t <= 0.85f)
            {
                float prog = (t - 0.08f) / 0.77f;
                int maxR = isLarge ? 22 : (isWide ? 15 : 12);
                int ringR = (int)(maxR * MathF.Sin(prog * MathF.PI * 0.5f));
                if (ringR > 1)
                {
                    EgaColorIndex ringCol = prog < 0.4f ? EgaColorIndex.BrightCyan : (prog < 0.7f ? EgaColorIndex.BrightMagenta : EgaColorIndex.Blue);
                    fb.DrawCircle(cx, cy, ringR, ringCol);
                }
            }

            // Flying Spacecraft Shrapnel & Pixel Vectors in 8 radial trajectories
            int particleCount = isLarge ? 16 : 8;
            for (int k = 0; k < particleCount; k++)
            {
                float angle = k * (MathF.Tau / particleCount);
                float speed = 35.0f + (k % 4) * 14.0f;
                float px = cx + MathF.Cos(angle) * speed * t;
                float py = cy + MathF.Sin(angle) * speed * t;
                int ipx = (int)px;
                int ipy = (int)py;

                if (t < 0.60f)
                {
                    EgaColorIndex pCol = (k % 3 == 0) ? EgaColorIndex.BrightWhite : ((k % 2 == 0) ? EgaColorIndex.BrightGreen : EgaColorIndex.Yellow);
                    fb.FillRect(ipx - 1, ipy - 1, 2, 2, pCol);
                    fb.SetPixel(ipx, ipy, EgaColorIndex.Black);
                }
                else
                {
                    fb.DrawSparkle(ipx, ipy, (k % 2 == 0) ? EgaColorIndex.BrightCyan : EgaColorIndex.BrightMagenta);
                }
            }
        }
        else if (isUnderwater)
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
        else if (isPolar)
        {
            // === Polar Adventure: Fluffy snow puff explosion & sparkling snowflakes ===
            if (t < 0.58f)
            {
                float puffProg = t / 0.58f;
                int maxR = isLarge ? 14 : 9;
                int rPuff = (int)(maxR * MathF.Sin(puffProg * MathF.PI));
                if (rPuff > 0)
                {
                    fb.FillCircle(cx, cy, rPuff, EgaColorIndex.BrightCyan);
                    if (rPuff > 2) fb.FillCircle(cx - 2, cy - 1, rPuff - 2, EgaColorIndex.BrightWhite);
                    if (rPuff > 1) fb.DrawCircle(cx, cy, rPuff, EgaColorIndex.Black);
                }
            }

            int flakeCount = isLarge ? 14 : 8;
            for (int k = 0; k < flakeCount; k++)
            {
                float angle = k * (MathF.Tau / flakeCount);
                float speed = 34.0f + (k % 3) * 14.0f;
                float fx = cx + MathF.Cos(angle) * speed * t;
                float fy = cy + MathF.Sin(angle) * speed * 0.65f * t + 26.0f * t;
                int ifx = (int)fx;
                int ify = (int)fy;

                if (k % 2 == 0)
                {
                    fb.DrawSparkle(ifx, ify, EgaColorIndex.BrightWhite);
                    fb.SetPixel(ifx, ify, EgaColorIndex.BrightCyan);
                }
                else
                {
                    fb.DrawSparkle(ifx, ify, EgaColorIndex.BrightCyan);
                    fb.SetPixel(ifx, ify, EgaColorIndex.BrightWhite);
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
            PickupType.HighJumpBoots => assets.ZeroGravity ? (assets.PiercingPlasmaBadge ?? assets.HighJumpBadge) : assets.HighJumpBadge,
            PickupType.SpeedDash => assets.SpeedDashBadge,
            PickupType.Invulnerability => assets.InvulnerabilityBadge,
            PickupType.PiercingPlasma => assets.PiercingPlasmaBadge ?? assets.HighJumpBadge,
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
        if (player.HasHighJumpBoots && !assets.ZeroGravity)
        {
            fb.DrawSprite(assets.HighJumpBadge, badgeX, 1);
            badgeX += 15;
        }
        if (player.HasPiercingPlasma && assets.ZeroGravity)
        {
            fb.DrawSprite(assets.PiercingPlasmaBadge ?? assets.HighJumpBadge, badgeX, 1);
            badgeX += 15;
        }
        if (player.HasSpeedDash)
        {
            fb.DrawSprite(assets.SpeedDashBadge, badgeX, 1);
            badgeX += 15;
        }
        if (player.HasInvulnerabilityPowerup)
        {
            fb.DrawSprite(assets.InvulnerabilityBadge, badgeX, 1);
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

        // Temporary Invulnerability HUD elements: red hue and on-screen countdown timer
        if (player.HasInvulnerabilityPowerup)
        {
            bool pulse = ((int)(player.InvulnerabilityPowerupTimer * 6) % 2 == 0);
            EgaColorIndex hueColor = pulse ? EgaColorIndex.BrightRed : EgaColorIndex.Red;

            // Red hue viewport frame
            fb.DrawRect(0, 16, 320, 184, hueColor);
            fb.DrawRect(1, 17, 318, 182, pulse ? EgaColorIndex.Red : EgaColorIndex.DarkGray);

            // Red hue corner accents
            fb.DrawDitherRect(0, 16, 16, 8, EgaColorIndex.Red, EgaColorIndex.Black);
            fb.DrawDitherRect(304, 16, 16, 8, EgaColorIndex.Red, EgaColorIndex.Black);
            fb.DrawDitherRect(0, 192, 16, 8, EgaColorIndex.Red, EgaColorIndex.Black);
            fb.DrawDitherRect(304, 192, 16, 8, EgaColorIndex.Red, EgaColorIndex.Black);

            // Cumulative counter display on the right (auto-sized to fit duration)
            string shieldText = $"SHIELD {player.InvulnerabilityPowerupTimer:F1}s";
            int textW = shieldText.Length * 8;
            int boxW = 18 + textW + 3;
            int counterX = 316 - boxW;
            int counterY = 20;
            fb.DrawBeveledBox(counterX, counterY, boxW, 14, EgaColorIndex.Black, EgaColorIndex.BrightRed, EgaColorIndex.Red);
            fb.DrawSprite(assets.InvulnerabilityBadge, counterX + 1, counterY);
            EgaColorIndex textCol = player.InvulnerabilityPowerupTimer > 3.0f
                ? EgaColorIndex.BrightRed
                : (pulse ? EgaColorIndex.Yellow : EgaColorIndex.BrightWhite);
            fb.DrawText(counterX + 17, counterY + 3, shieldText, textCol);
        }
    }
}
