using System;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Themes;

public sealed class DefaultTheme : IGameTheme
{
    public string Name => "EGA Cyber Industrial";

    private static readonly (int X, int Y, EgaColorIndex Color)[] Stars = new (int, int, EgaColorIndex)[]
    {
        (20, 25, EgaColorIndex.BrightCyan), (65, 40, EgaColorIndex.BrightWhite), (110, 20, EgaColorIndex.BrightBlue),
        (160, 50, EgaColorIndex.BrightCyan), (210, 30, EgaColorIndex.BrightWhite), (260, 45, EgaColorIndex.BrightBlue),
        (40, 80, EgaColorIndex.BrightBlue), (95, 95, EgaColorIndex.BrightCyan), (145, 75, EgaColorIndex.BrightWhite),
        (195, 90, EgaColorIndex.BrightCyan), (245, 68, EgaColorIndex.BrightBlue), (290, 85, EgaColorIndex.BrightWhite),
        (75, 120, EgaColorIndex.BrightWhite), (130, 135, EgaColorIndex.BrightCyan), (220, 125, EgaColorIndex.BrightBlue)
    };

    public void DrawBackground(FrameBuffer fb, Camera2D camera)
    {
        // Deep arcade background (black/dark-blue tone)
        fb.Clear(EgaColorIndex.Black);

        // Distant parallax structural pillars
        int camOffset = (int)(camera.X * 0.2f);
        for (int i = 0; i < 7; i++)
        {
            int pillarX = (i * 64 - (camOffset % 64));
            if (pillarX > -32 && pillarX < 320)
            {
                fb.FillRect(pillarX, 16, 20, 168, EgaColorIndex.DarkGray);
                fb.DrawVLine(pillarX, 16, 168, EgaPalette.GetRgba(EgaColorIndex.LightGray));
                fb.DrawVLine(pillarX + 19, 16, 168, EgaPalette.GetRgba(EgaColorIndex.Black));
            }
        }

        // Stars with parallax
        int starOffset = (int)(camera.X * 0.1f);
        foreach (var star in Stars)
        {
            int sx = (star.X - starOffset) % 320;
            if (sx < 0) sx += 320;
            if (sx < 320 && star.Y >= 16 && star.Y < 184)
            {
                fb.SetPixel(sx, star.Y, star.Color);
            }
        }
    }

    public void DrawTile(FrameBuffer fb, TileType type, int screenX, int screenY, int col, int row)
    {
        if (screenX <= -16 || screenX >= 320 || screenY <= -16 || screenY >= 184) return;
        int ts = PhysicsConstants.TileSize;

        switch (type)
        {
            case TileType.Solid:
                // Metallic industrial platform block
                fb.FillRect(screenX, screenY, ts, ts, EgaColorIndex.LightGray);
                fb.DrawHLine(screenX, screenY, ts, EgaPalette.GetRgba(EgaColorIndex.BrightWhite));
                fb.DrawVLine(screenX, screenY, ts, EgaPalette.GetRgba(EgaColorIndex.BrightWhite));
                fb.DrawHLine(screenX, screenY + ts - 1, ts, EgaPalette.GetRgba(EgaColorIndex.DarkGray));
                fb.DrawVLine(screenX + ts - 1, screenY, ts, EgaPalette.GetRgba(EgaColorIndex.DarkGray));
                // Center recessed panel
                fb.FillRect(screenX + 3, screenY + 3, ts - 6, ts - 6, EgaColorIndex.DarkGray);
                fb.FillRect(screenX + 4, screenY + 4, ts - 8, ts - 8, EgaColorIndex.LightGray);
                break;

            case TileType.OneWayPlatform:
                // Yellow & Red diagonal hazard stripe ledge
                fb.FillRect(screenX, screenY, ts, 8, EgaColorIndex.Yellow);
                for (int s = 0; s < ts; s += 4)
                {
                    fb.SetPixel(screenX + s, screenY + 1, EgaColorIndex.BrightRed);
                    fb.SetPixel(screenX + s + 1, screenY + 2, EgaColorIndex.BrightRed);
                    fb.SetPixel(screenX + s + 2, screenY + 3, EgaColorIndex.BrightRed);
                }
                fb.DrawRect(screenX, screenY, ts, 8, EgaColorIndex.BrightRed);
                break;

            case TileType.Hazard:
                // Sharp steel floor spikes with white tips
                for (int i = 0; i < ts; i += 4)
                {
                    fb.FillRect(screenX + i + 1, screenY + 10, 2, 6, EgaColorIndex.BrightRed);
                    fb.SetPixel(screenX + i + 1, screenY + 9, EgaColorIndex.BrightWhite);
                    fb.SetPixel(screenX + i + 2, screenY + 9, EgaColorIndex.BrightWhite);
                    fb.SetPixel(screenX + i + 1, screenY + 8, EgaColorIndex.BrightWhite);
                }
                break;

            case TileType.Border:
                // Heavy perimeter industrial boundary block
                fb.FillRect(screenX, screenY, ts, ts, EgaColorIndex.DarkGray);
                fb.DrawRect(screenX, screenY, ts, ts, EgaColorIndex.Black);
                fb.DrawHLine(screenX + 1, screenY + 1, ts - 2, EgaPalette.GetRgba(EgaColorIndex.LightGray));
                fb.DrawVLine(screenX + 1, screenY + 1, ts - 2, EgaPalette.GetRgba(EgaColorIndex.LightGray));
                // Cross brace diagonals
                for (int d = 2; d < ts - 2; d++)
                {
                    fb.SetPixel(screenX + d, screenY + d, EgaColorIndex.Black);
                    fb.SetPixel(screenX + (ts - 1 - d), screenY + d, EgaColorIndex.Black);
                }
                // Rivets in four corners
                fb.SetPixel(screenX + 3, screenY + 3, EgaColorIndex.BrightCyan);
                fb.SetPixel(screenX + ts - 4, screenY + 3, EgaColorIndex.BrightCyan);
                fb.SetPixel(screenX + 3, screenY + ts - 4, EgaColorIndex.BrightCyan);
                fb.SetPixel(screenX + ts - 4, screenY + ts - 4, EgaColorIndex.BrightCyan);
                break;

            case TileType.Ladder:
                // Industrial steel climbing ladder
                // Vertical side rails (2px wide each)
                fb.FillRect(screenX + 2, screenY, 2, ts, EgaColorIndex.LightGray);
                fb.FillRect(screenX + 12, screenY, 2, ts, EgaColorIndex.LightGray);
                fb.DrawVLine(screenX + 2, screenY, ts, EgaPalette.GetRgba(EgaColorIndex.BrightWhite));
                fb.DrawVLine(screenX + 12, screenY, ts, EgaPalette.GetRgba(EgaColorIndex.BrightWhite));
                fb.DrawVLine(screenX + 3, screenY, ts, EgaPalette.GetRgba(EgaColorIndex.DarkGray));
                fb.DrawVLine(screenX + 13, screenY, ts, EgaPalette.GetRgba(EgaColorIndex.DarkGray));

                // 4 Horizontal rungs with highlight & shadow
                for (int rungY = 2; rungY < ts; rungY += 4)
                {
                    fb.FillRect(screenX + 4, screenY + rungY, 8, 2, EgaColorIndex.LightGray);
                    fb.DrawHLine(screenX + 4, screenY + rungY, 8, EgaPalette.GetRgba(EgaColorIndex.BrightWhite));
                    fb.DrawHLine(screenX + 4, screenY + rungY + 1, 8, EgaPalette.GetRgba(EgaColorIndex.DarkGray));
                }
                break;
        }
    }

    public void DrawPlayer(FrameBuffer fb, PlayerEntity player, int screenX, int screenY)
    {
        // 1.2s I-frame damage flicker: blinks on/off every 4 frames
        if (player.InvulnerabilityTimer > 0 && ((int)(player.InvulnerabilityTimer * 20) % 2 == 0))
        {
            return;
        }

        bool flip = player.Facing == Direction.Left;
        int px = screenX;
        int py = screenY;

        if (player.State == PlayerState.Climbing)
        {
            // Duke climbing ladder (rear perspective)
            int climbOffset = ((int)(player.AnimTimer * 10) % 2) == 0 ? 1 : -1;

            // Helmet (Yellow rear view)
            fb.FillRect(px + 2, py, 10, 6, EgaColorIndex.Yellow);
            fb.DrawHLine(px + 3, py + 1, 8, EgaPalette.GetRgba(EgaColorIndex.BrightWhite));

            // Torso / Red Vest
            fb.FillRect(px + 3, py + 7, 8, 8, EgaColorIndex.BrightRed);

            // Hands gripping ladder rungs
            fb.FillRect(px + 1, py + 8 + climbOffset * 2, 2, 3, EgaColorIndex.Brown);
            fb.FillRect(px + 11, py + 8 - climbOffset * 2, 2, 3, EgaColorIndex.Brown);

            // Blue Pants
            fb.FillRect(px + 3, py + 15, 8, 4, EgaColorIndex.BrightBlue);

            // Boots gripping rungs
            fb.FillRect(px + 2, py + 19 + climbOffset * 2, 4, 3, EgaColorIndex.DarkGray);
            fb.FillRect(px + 8, py + 19 - climbOffset * 2, 4, 3, EgaColorIndex.DarkGray);
            return;
        }

        int legOffset = 0;
        if (player.State == PlayerState.Walking || player.State == PlayerState.Running)
        {
            legOffset = ((int)(player.AnimTimer * (player.State == PlayerState.Running ? 14 : 8)) % 2) == 0 ? 1 : -1;
        }

        // Helmet (Yellow)
        fb.FillRect(flip ? px + 3 : px + 1, py, 10, 5, EgaColorIndex.Yellow);
        fb.FillRect(flip ? px + 1 : px + 7, py + 1, 4, 3, EgaColorIndex.Yellow);

        // Face & Cyan Visor
        fb.FillRect(px + 3, py + 5, 8, 5, EgaColorIndex.Brown);
        fb.FillRect(flip ? px + 2 : px + 8, py + 6, 3, 2, EgaColorIndex.BrightCyan);

        // Torso / Red Vest
        fb.FillRect(px + 3, py + 10, 8, 6, EgaColorIndex.BrightRed);

        // Blaster
        if (flip)
        {
            fb.FillRect(px - 3, py + 11, 6, 3, EgaColorIndex.LightGray);
            fb.SetPixel(px - 4, py + 12, EgaColorIndex.BrightCyan);
        }
        else
        {
            fb.FillRect(px + 10, py + 11, 6, 3, EgaColorIndex.LightGray);
            fb.SetPixel(px + 16, py + 12, EgaColorIndex.BrightCyan);
        }

        // Blue Pants
        fb.FillRect(px + 3, py + 16, 8, 4, EgaColorIndex.BrightBlue);

        // Boots
        fb.FillRect(px + 2, py + 20 + legOffset, 4, 3, EgaColorIndex.DarkGray);
        fb.FillRect(px + 8, py + 20 - legOffset, 4, 3, EgaColorIndex.DarkGray);
    }

    public void DrawProjectile(FrameBuffer fb, in Projectile projectile, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 184) return;

        if (projectile.FromPlayer)
        {
            // Player Blaster Laser: Neon Yellow with Bright White core
            fb.FillRect(screenX, screenY, 8, 3, EgaColorIndex.Yellow);
            fb.DrawHLine(screenX + 1, screenY + 1, 6, EgaPalette.GetRgba(EgaColorIndex.BrightWhite));
            fb.SetPixel(screenX + 7, screenY + 1, EgaColorIndex.BrightCyan);
        }
        else
        {
            // Enemy Plasma Bolt: Bright Red with Yellow center
            fb.FillRect(screenX, screenY, 6, 4, EgaColorIndex.BrightRed);
            fb.DrawHLine(screenX + 1, screenY + 1, 4, EgaPalette.GetRgba(EgaColorIndex.Yellow));
            fb.SetPixel(screenX + 2, screenY + 2, EgaColorIndex.BrightWhite);
        }
    }

    public void DrawMonster(FrameBuffer fb, MonsterEntity monster, int screenX, int screenY)
    {
        if (screenX < -32 || screenX > 320 || screenY < -32 || screenY > 184) return;

        bool flash = monster.HitFlashTimer > 0;
        EgaColorIndex bodyCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.DarkGray;
        EgaColorIndex trimCol = flash ? EgaColorIndex.BrightRed : EgaColorIndex.LightGray;
        bool flip = monster.Facing == Direction.Left;

        int mx = screenX;
        int my = screenY;

        switch (monster.Type)
        {
            case MonsterType.PatrolDroid:
                // 14x14 Spherical patrol bot with rotating red eye and bottom roller
                fb.FillRect(mx + 2, my + 1, 10, 10, bodyCol);
                fb.DrawRect(mx + 2, my + 1, 10, 10, trimCol);
                // Glowing visor / eye
                int eyeX = flip ? mx + 3 : mx + 8;
                fb.FillRect(eyeX, my + 4, 3, 3, flash ? EgaColorIndex.Yellow : EgaColorIndex.BrightRed);
                // Bottom tread roller
                fb.FillRect(mx + 3, my + 11, 8, 3, EgaColorIndex.Black);
                fb.DrawHLine(mx + 4, my + 12, 6, EgaPalette.GetRgba(EgaColorIndex.LightGray));
                break;

            case MonsterType.HoverDrone:
                // 14x12 Flying drone with top sensor and bottom ion thruster
                fb.FillRect(mx + 2, my + 2, 10, 7, bodyCol);
                fb.DrawRect(mx + 2, my + 2, 10, 7, trimCol);
                // Dual sensor lenses
                fb.SetPixel(mx + 4, my + 4, EgaColorIndex.BrightCyan);
                fb.SetPixel(mx + 9, my + 4, EgaColorIndex.BrightCyan);
                // Ion thruster flickering flame
                bool flameHigh = ((int)(monster.AnimTimer * 12) % 2) == 0;
                fb.FillRect(mx + 5, my + 9, 4, flameHigh ? 3 : 2, EgaColorIndex.BrightCyan);
                fb.SetPixel(mx + 6, my + 10, EgaColorIndex.BrightWhite);
                break;

            case MonsterType.CeilingTurret:
                // 14x12 Ceiling mount base with downward aiming dual barrels
                fb.FillRect(mx + 1, my, 12, 4, trimCol);
                fb.FillRect(mx + 3, my + 4, 8, 4, bodyCol);
                // Twin downward cannon barrels
                fb.FillRect(mx + 4, my + 8, 2, 4, EgaColorIndex.LightGray);
                fb.FillRect(mx + 8, my + 8, 2, 4, EgaColorIndex.LightGray);
                // Center targeting sensor
                fb.SetPixel(mx + 6, my + 5, EgaColorIndex.BrightRed);
                break;

            case MonsterType.MechaHound:
                // 24x14 Fast quadruped robotic hound
                fb.FillRect(mx + 6, my + 3, 12, 7, bodyCol);
                fb.DrawRect(mx + 6, my + 3, 12, 7, trimCol);
                // Head with aggressive snout
                int headX = flip ? mx : mx + 16;
                fb.FillRect(headX, my + 2, 8, 6, bodyCol);
                fb.SetPixel(flip ? headX + 1 : headX + 6, my + 3, EgaColorIndex.BrightRed);
                // 4 Running legs
                int legStep = ((int)(monster.AnimTimer * 10) % 2) == 0 ? 1 : -1;
                fb.FillRect(mx + 2, my + 10 + legStep, 3, 4, trimCol);
                fb.FillRect(mx + 7, my + 10 - legStep, 3, 4, trimCol);
                fb.FillRect(mx + 14, my + 10 + legStep, 3, 4, trimCol);
                fb.FillRect(mx + 19, my + 10 - legStep, 3, 4, trimCol);
                break;

            case MonsterType.HeavyGuard:
                // 14x26 Heavy armored mech enforcer
                fb.FillRect(mx + 2, my + 2, 10, 6, trimCol); // Helm
                fb.SetPixel(flip ? mx + 3 : mx + 10, my + 4, EgaColorIndex.BrightRed); // Red Visor
                fb.FillRect(mx + 1, my + 8, 12, 10, bodyCol); // Heavy Chestplate
                fb.DrawRect(mx + 1, my + 8, 12, 10, trimCol);
                // Shoulder cannon
                int cannonX = flip ? mx - 2 : mx + 10;
                fb.FillRect(cannonX, my + 8, 6, 4, EgaColorIndex.LightGray);
                // Heavy legs
                fb.FillRect(mx + 2, my + 18, 4, 8, bodyCol);
                fb.FillRect(mx + 8, my + 18, 4, 8, bodyCol);
                break;
        }
    }

    public void DrawPickup(FrameBuffer fb, in PickupItem pickup, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 184) return;

        int px = screenX;
        int py = screenY;
        bool sparkle = ((int)(pickup.AnimTimer * 6) % 2) == 0;

        switch (pickup.Type)
        {
            case PickupType.BlueGem:
                // Sparkling cyan crystal (+100 score)
                fb.FillRect(px + 3, py + 2, 6, 8, EgaColorIndex.BrightCyan);
                fb.FillRect(px + 4, py + 1, 4, 10, EgaColorIndex.BrightCyan);
                fb.DrawRect(px + 3, py + 2, 6, 8, EgaColorIndex.Blue);
                if (sparkle) fb.SetPixel(px + 5, py + 3, EgaColorIndex.BrightWhite);
                break;

            case PickupType.RedRuby:
                // Brilliant red jewel (+250 score)
                fb.FillRect(px + 2, py + 3, 8, 6, EgaColorIndex.BrightRed);
                fb.FillRect(px + 3, py + 2, 6, 8, EgaColorIndex.BrightRed);
                fb.DrawRect(px + 2, py + 3, 8, 6, EgaColorIndex.Red);
                if (sparkle) fb.SetPixel(px + 4, py + 4, EgaColorIndex.BrightWhite);
                break;

            case PickupType.GoldCartridge:
                // Gold retro game cartridge (+500 score)
                fb.FillRect(px + 2, py + 1, 8, 10, EgaColorIndex.Yellow);
                fb.DrawRect(px + 2, py + 1, 8, 10, EgaColorIndex.Brown);
                fb.FillRect(px + 4, py + 3, 4, 4, EgaColorIndex.Black); // Cartridge label
                if (sparkle) fb.SetPixel(px + 3, py + 2, EgaColorIndex.BrightWhite);
                break;

            case PickupType.MedKit:
                // White first aid case with red cross (+1 HP)
                fb.FillRect(px + 1, py + 2, 10, 8, EgaColorIndex.BrightWhite);
                fb.DrawRect(px + 1, py + 2, 10, 8, EgaColorIndex.LightGray);
                // Red cross
                fb.FillRect(px + 5, py + 4, 2, 4, EgaColorIndex.BrightRed);
                fb.FillRect(px + 4, py + 5, 4, 2, EgaColorIndex.BrightRed);
                break;

            case PickupType.RapidFire:
                // Powerup Badge: Red [R] Rapid Fire
                fb.FillRect(px + 1, py + 1, 10, 10, EgaColorIndex.BrightRed);
                fb.DrawRect(px + 1, py + 1, 10, 10, EgaColorIndex.Yellow);
                fb.DrawText(px + 3, py + 3, "R", EgaColorIndex.Yellow);
                break;

            case PickupType.HighJumpBoots:
                // Powerup Badge: Cyan [J] Jump Boots
                fb.FillRect(px + 1, py + 1, 10, 10, EgaColorIndex.BrightCyan);
                fb.DrawRect(px + 1, py + 1, 10, 10, EgaColorIndex.BrightWhite);
                fb.DrawText(px + 3, py + 3, "J", EgaColorIndex.BrightWhite);
                break;

            case PickupType.SpeedDash:
                // Powerup Badge: Green [S] Speed Dash
                fb.FillRect(px + 1, py + 1, 10, 10, EgaColorIndex.BrightGreen);
                fb.DrawRect(px + 1, py + 1, 10, 10, EgaColorIndex.Green);
                fb.DrawText(px + 3, py + 3, "S", EgaColorIndex.BrightWhite);
                break;
        }
    }

    public void DrawHud(FrameBuffer fb, PlayerEntity player, string seed, int fps)
    {
        uint lightBevel = EgaPalette.GetRgba(EgaColorIndex.BrightWhite);
        uint darkBevel = EgaPalette.GetRgba(EgaColorIndex.DarkGray);
        uint panelColor = EgaPalette.GetRgba(EgaColorIndex.DarkGray);
        uint blueBg = EgaPalette.GetRgba(EgaColorIndex.Blue);

        // 1. Top Minimalist Status Bar (320x16, Y: 0..16)
        fb.DrawBeveledBox(0, 0, 320, 16, panelColor, lightBevel, darkBevel);

        // Score display (zero allocations)
        fb.DrawText(6, 4, "SCORE: ", EgaColorIndex.BrightWhite);
        fb.DrawInt(62, 4, player.Score, 6, EgaColorIndex.BrightWhite);

        // 5 Health Pips [■■■■■]
        fb.DrawText(124, 4, "HP:", EgaColorIndex.Yellow);
        for (int i = 0; i < PlayerEntity.MaxHealth; i++)
        {
            int pipX = 152 + i * 8;
            if (i < player.Health)
            {
                fb.FillRect(pipX, 5, 6, 6, EgaColorIndex.BrightGreen);
                fb.DrawRect(pipX, 5, 6, 6, EgaColorIndex.Green);
            }
            else
            {
                fb.DrawRect(pipX, 5, 6, 6, EgaColorIndex.Red);
            }
        }

        // Active Powerup Badges (Lost on damage)
        int badgeX = 196;
        if (player.HasRapidFire)
        {
            fb.FillRect(badgeX, 3, 8, 10, EgaColorIndex.BrightRed);
            fb.DrawText(badgeX + 1, 4, "R", EgaColorIndex.Yellow);
            badgeX += 10;
        }
        if (player.HasHighJumpBoots)
        {
            fb.FillRect(badgeX, 3, 8, 10, EgaColorIndex.BrightCyan);
            fb.DrawText(badgeX + 1, 4, "J", EgaColorIndex.BrightWhite);
            badgeX += 10;
        }
        if (player.HasSpeedDash)
        {
            fb.FillRect(badgeX, 3, 8, 10, EgaColorIndex.BrightGreen);
            fb.DrawText(badgeX + 1, 4, "S", EgaColorIndex.BrightWhite);
            badgeX += 10;
        }

        // Seed & Version
        fb.DrawText(230, 4, "SEED:", EgaColorIndex.BrightCyan);
        fb.DrawText(264, 4, seed, EgaColorIndex.BrightCyan);
        fb.DrawText(294, 4, GameVersion.Version, EgaColorIndex.Yellow);

        // 2. Bottom Controls Strip (320x16, Y: 184..200)
        fb.DrawBeveledBox(0, 184, 320, 16, blueBg, lightBevel, darkBevel);
        fb.DrawText(6, 188, "ARROWS/WASD:MOVE/CLIMB  CTRL/SPC:JUMP  ALT/X/J:SHOOT  SHIFT:RUN  DN+JMP:DROP", EgaColorIndex.BrightWhite);
    }
}
