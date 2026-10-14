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

    public void DrawHud(FrameBuffer fb, PlayerEntity player, string seed, int fps)
    {
        uint lightBevel = EgaPalette.GetRgba(EgaColorIndex.BrightWhite);
        uint darkBevel = EgaPalette.GetRgba(EgaColorIndex.DarkGray);
        uint panelColor = EgaPalette.GetRgba(EgaColorIndex.DarkGray);
        uint blueBg = EgaPalette.GetRgba(EgaColorIndex.Blue);

        // 1. Top Minimalist Status Bar (320x16, Y: 0..16)
        fb.DrawBeveledBox(0, 0, 320, 16, panelColor, lightBevel, darkBevel);

        // Score display
        fb.DrawText(6, 4, $"SCORE: {player.Score:D6}", EgaColorIndex.BrightWhite);

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

        // Seed & Version
        fb.DrawText(200, 4, $"SEED:{seed}", EgaColorIndex.BrightCyan);
        fb.DrawText(282, 4, $"{GameVersion.Version}", EgaColorIndex.Yellow);

        // 2. Bottom Controls Strip (320x16, Y: 184..200)
        fb.DrawBeveledBox(0, 184, 320, 16, blueBg, lightBevel, darkBevel);
        fb.DrawText(8, 188, "ARROWS/WASD:MOVE  CTRL/SPACE:JUMP  SHIFT/K:RUN  DOWN+JUMP:DROP", EgaColorIndex.BrightWhite);
    }
}
