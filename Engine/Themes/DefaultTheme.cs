using System;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// High-detail retro 16-color solid EGA theme inspired by Duke Nukem 1, Commander Keen, and Apogee classics.
/// Features rich dithered shading, beveled industrial panels, authentic Duke pixel art, animated pickups, and crisp HUD.
/// </summary>
public sealed class DefaultTheme : IGameTheme
{
    public string Name => "EGA Cyber Industrial (Revamped)";

    // Starfield definitions for distant parallax
    private static readonly (int X, int Y, EgaColorIndex Color)[] Stars = new (int, int, EgaColorIndex)[]
    {
        (15, 20, EgaColorIndex.BrightCyan), (45, 35, EgaColorIndex.BrightWhite), (80, 22, EgaColorIndex.BrightBlue),
        (120, 48, EgaColorIndex.BrightCyan), (160, 28, EgaColorIndex.BrightWhite), (205, 42, EgaColorIndex.BrightBlue),
        (240, 25, EgaColorIndex.BrightCyan), (280, 38, EgaColorIndex.BrightWhite), (310, 18, EgaColorIndex.BrightBlue),
        (30, 75, EgaColorIndex.BrightBlue), (70, 92, EgaColorIndex.BrightCyan), (115, 68, EgaColorIndex.BrightWhite),
        (150, 85, EgaColorIndex.BrightCyan), (190, 70, EgaColorIndex.BrightBlue), (230, 95, EgaColorIndex.BrightWhite),
        (270, 80, EgaColorIndex.BrightCyan), (305, 65, EgaColorIndex.BrightWhite), (50, 130, EgaColorIndex.BrightCyan),
        (140, 140, EgaColorIndex.BrightWhite), (215, 125, EgaColorIndex.BrightBlue), (285, 135, EgaColorIndex.BrightCyan)
    };

    public void DrawBackground(FrameBuffer fb, Camera2D camera)
    {
        // 1. Deep space / atmospheric night sky
        fb.Clear(EgaColorIndex.Black);

        // 2. Distant twinkling stars (slow parallax: 0.08x)
        int starOffset = (int)(camera.X * 0.08f);
        for (int i = 0; i < Stars.Length; i++)
        {
            var star = Stars[i];
            int sx = (star.X - starOffset) % 320;
            if (sx < 0) sx += 320;
            if (sx < 320 && star.Y >= 16 && star.Y < 184)
            {
                fb.SetPixel(sx, star.Y, star.Color);
            }
        }

        // 3. Far Cyberpunk Cityscape / Industrial Fortress Skyline (parallax: 0.15x)
        int skylineOffset = (int)(camera.X * 0.15f);
        for (int b = 0; b < 10; b++)
        {
            int bx = (b * 48 - (skylineOffset % 480));
            if (bx < -48) bx += 480;
            if (bx > 320) continue;

            int bHeight = 40 + ((b * 37) % 55);
            int by = 184 - bHeight;

            // Silhouette body in DarkGray
            fb.FillRect(bx, by, 36, bHeight, EgaColorIndex.DarkGray);
            fb.DrawVLine(bx, by, bHeight, EgaColorIndex.LightGray);
            fb.DrawHLine(bx, by, 36, EgaColorIndex.LightGray);
            fb.DrawVLine(bx + 35, by, bHeight, EgaColorIndex.Black);

            // Roof antenna with warning beacon
            if (b % 2 == 0)
            {
                fb.DrawVLine(bx + 18, by - 8, 8, EgaColorIndex.LightGray);
                fb.SetPixel(bx + 18, by - 9, (b % 4 == 0) ? EgaColorIndex.BrightRed : EgaColorIndex.Yellow);
            }

            // Illuminated window slits
            for (int wy = by + 6; wy < 180; wy += 8)
            {
                for (int wx = bx + 4; wx < bx + 32; wx += 6)
                {
                    if (((wx * 13 + wy * 7 + b) % 5) < 3)
                    {
                        var winCol = ((wx + wy) % 2 == 0) ? EgaColorIndex.Yellow : EgaColorIndex.BrightCyan;
                        fb.FillRect(wx, wy, 3, 2, winCol);
                    }
                }
            }
        }

        // 4. Mid-Ground Structural Girders and Columns (parallax: 0.35x)
        int girderOffset = (int)(camera.X * 0.35f);
        for (int i = 0; i < 5; i++)
        {
            int gx = (i * 80 - (girderOffset % 400));
            if (gx < -30) gx += 400;
            if (gx >= -20 && gx < 320)
            {
                // Vertical industrial column with 3D bevels
                fb.FillRect(gx, 16, 16, 168, EgaColorIndex.DarkGray);
                fb.DrawVLine(gx, 16, 168, EgaColorIndex.LightGray);
                fb.DrawVLine(gx + 1, 16, 168, EgaColorIndex.LightGray);
                fb.DrawVLine(gx + 15, 16, 168, EgaColorIndex.Black);

                // Dithered recessed center seam
                fb.DrawDitherRect(gx + 4, 16, 8, 168, EgaColorIndex.DarkGray, EgaColorIndex.Black);

                // Horizontal cross braces every 32 pixels
                for (int cy = 24; cy < 184; cy += 32)
                {
                    fb.DrawHLine(gx, cy, 16, EgaColorIndex.LightGray);
                    fb.DrawHLine(gx, cy + 1, 16, EgaColorIndex.Black);
                    // Rivets on brace joints
                    fb.SetPixel(gx + 2, cy, EgaColorIndex.BrightCyan);
                    fb.SetPixel(gx + 13, cy, EgaColorIndex.BrightCyan);
                }
            }
        }
    }

    public void DrawTile(FrameBuffer fb, TileType type, int screenX, int screenY, int col, int row)
    {
        if (screenX <= -16 || screenX >= 320 || screenY <= -16 || screenY >= 184) return;

        switch (type)
        {
            case TileType.Solid:
                DrawSolidTile(fb, screenX, screenY, col, row);
                break;

            case TileType.OneWayPlatform:
                DrawOneWayPlatform(fb, screenX, screenY, col);
                break;

            case TileType.Hazard:
                DrawHazardSpikes(fb, screenX, screenY);
                break;

            case TileType.Border:
                DrawBorderBulkhead(fb, screenX, screenY);
                break;

            case TileType.Ladder:
                DrawLadder(fb, screenX, screenY);
                break;

            case TileType.Crate:
                DrawCargoCrate(fb, screenX, screenY);
                break;
        }
    }

    private static void DrawSolidTile(FrameBuffer fb, int x, int y, int col, int row)
    {
        // High-tech modular steel floor deck with 3D chiseled bevels
        fb.FillRect(x, y, 16, 16, EgaColorIndex.LightGray);

        // Highlight rim (Top & Left) in pure BrightWhite
        fb.DrawHLine(x, y, 16, EgaColorIndex.BrightWhite);
        fb.DrawVLine(x, y, 16, EgaColorIndex.BrightWhite);

        // Shadow rim (Bottom & Right) in Black and DarkGray
        fb.DrawHLine(x, y + 15, 16, EgaColorIndex.Black);
        fb.DrawVLine(x + 15, y, 16, EgaColorIndex.Black);
        fb.DrawHLine(x + 1, y + 14, 14, EgaColorIndex.DarkGray);
        fb.DrawVLine(x + 14, y + 1, 14, EgaColorIndex.DarkGray);

        // Recessed interior deck panel (12x12)
        fb.FillRect(x + 2, y + 2, 12, 12, EgaColorIndex.DarkGray);
        fb.DrawHLine(x + 2, y + 2, 12, EgaColorIndex.Black);
        fb.DrawVLine(x + 2, y + 2, 12, EgaColorIndex.Black);
        fb.DrawHLine(x + 2, y + 13, 12, EgaColorIndex.LightGray);
        fb.DrawVLine(x + 13, y + 2, 12, EgaColorIndex.LightGray);

        // Micro diamond-tread dither texture across the center (8x8)
        fb.DrawDitherRect(x + 4, y + 4, 8, 8, EgaColorIndex.LightGray, EgaColorIndex.DarkGray);

        // 4 Corner hex-head industrial bolts with specular glint
        fb.SetPixel(x + 3, y + 3, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 12, y + 3, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 3, y + 12, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 12, y + 12, EgaColorIndex.BrightWhite);

        // Status LED on every 4th column
        if ((col % 4 == 0) && (row % 2 == 0))
        {
            fb.SetPixel(x + 7, y + 7, EgaColorIndex.BrightGreen);
            fb.SetPixel(x + 8, y + 7, EgaColorIndex.BrightGreen);
            fb.SetPixel(x + 7, y + 8, EgaColorIndex.Green);
            fb.SetPixel(x + 8, y + 8, EgaColorIndex.Green);
        }
    }

    private static void DrawOneWayPlatform(FrameBuffer fb, int x, int y, int col)
    {
        // Industrial suspension gantry catwalk (16x8)
        // Top steel grating surface
        fb.FillRect(x, y, 16, 2, EgaColorIndex.LightGray);
        fb.DrawHLine(x, y, 16, EgaColorIndex.BrightWhite);
        for (int s = 1; s < 16; s += 3)
        {
            fb.SetPixel(x + s, y + 1, EgaColorIndex.Black); // Grate slots
        }

        // Bold 45-degree yellow and black industrial hazard chevron stripes
        for (int py = 2; py < 6; py++)
        {
            for (int px = 0; px < 16; px++)
            {
                int stripe = (col * 16 + px + py) % 8;
                var colr = (stripe < 4) ? EgaColorIndex.Yellow : EgaColorIndex.Black;
                fb.SetPixel(x + px, y + py, colr);
            }
        }

        // Bottom structural flange and triangular under-brackets
        fb.DrawHLine(x, y + 6, 16, EgaColorIndex.DarkGray);
        fb.DrawHLine(x, y + 7, 16, EgaColorIndex.Black);

        // Truss struts
        fb.FillRect(x + 2, y + 6, 2, 2, EgaColorIndex.LightGray);
        fb.FillRect(x + 12, y + 6, 2, 2, EgaColorIndex.LightGray);
    }

    private static void DrawHazardSpikes(FrameBuffer fb, int x, int y)
    {
        // Dual razor-sharp titanium spikes with serrated edges and blood/rust flecks
        fb.DrawHLine(x, y + 15, 16, EgaColorIndex.DarkGray);
        fb.DrawHLine(x, y + 14, 16, EgaColorIndex.Black);

        // Spike 1 (X: 1..7)
        for (int h = 0; h < 10; h++)
        {
            int sy = y + 14 - h;
            int halfW = (h * 3) / 10;
            int sx = x + 4;
            // Left edge glint
            fb.SetPixel(sx - halfW, sy, EgaColorIndex.BrightWhite);
            // Center blade
            if (halfW > 0)
            {
                fb.FillRect(sx - halfW + 1, sy, halfW, 1, EgaColorIndex.LightGray);
                fb.FillRect(sx + 1, sy, halfW, 1, EgaColorIndex.DarkGray);
            }
            // Right edge shadow
            fb.SetPixel(sx + halfW, sy, EgaColorIndex.Black);
        }
        // Needle-sharp tip
        fb.SetPixel(x + 4, y + 3, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 4, y + 4, EgaColorIndex.BrightRed);

        // Spike 2 (X: 9..15)
        for (int h = 0; h < 10; h++)
        {
            int sy = y + 14 - h;
            int halfW = (h * 3) / 10;
            int sx = x + 12;
            fb.SetPixel(sx - halfW, sy, EgaColorIndex.BrightWhite);
            if (halfW > 0)
            {
                fb.FillRect(sx - halfW + 1, sy, halfW, 1, EgaColorIndex.LightGray);
                fb.FillRect(sx + 1, sy, halfW, 1, EgaColorIndex.DarkGray);
            }
            fb.SetPixel(sx + halfW, sy, EgaColorIndex.Black);
        }
        fb.SetPixel(x + 12, y + 3, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 12, y + 4, EgaColorIndex.BrightRed);

        // Rust flecks at the base
        fb.SetPixel(x + 2, y + 13, EgaColorIndex.Brown);
        fb.SetPixel(x + 8, y + 13, EgaColorIndex.Red);
        fb.SetPixel(x + 14, y + 13, EgaColorIndex.Brown);
    }

    private static void DrawBorderBulkhead(FrameBuffer fb, int x, int y)
    {
        // Heavy reinforced titanium blast bulkhead
        fb.FillRect(x, y, 16, 16, EgaColorIndex.DarkGray);
        fb.DrawRect(x, y, 16, 16, EgaColorIndex.Black);

        // Outer beveled frame
        fb.DrawHLine(x + 1, y + 1, 14, EgaColorIndex.LightGray);
        fb.DrawVLine(x + 1, y + 1, 14, EgaColorIndex.LightGray);
        fb.DrawHLine(x + 1, y + 14, 14, EgaColorIndex.Black);
        fb.DrawVLine(x + 14, y + 1, 14, EgaColorIndex.Black);

        // Heavy cross-brace diagonals
        for (int d = 2; d < 14; d++)
        {
            fb.SetPixel(x + d, y + d, EgaColorIndex.Black);
            fb.SetPixel(x + d + 1, y + d, EgaColorIndex.LightGray);
            fb.SetPixel(x + (15 - d), y + d, EgaColorIndex.Black);
            fb.SetPixel(x + (14 - d), y + d, EgaColorIndex.LightGray);
        }

        // Center hub plate with heavy rivet
        fb.FillRect(x + 6, y + 6, 4, 4, EgaColorIndex.LightGray);
        fb.DrawRect(x + 6, y + 6, 4, 4, EgaColorIndex.Black);
        fb.SetPixel(x + 7, y + 7, EgaColorIndex.BrightCyan);

        // 4 Corner hex bolts
        fb.SetPixel(x + 3, y + 3, EgaColorIndex.BrightCyan);
        fb.SetPixel(x + 12, y + 3, EgaColorIndex.BrightCyan);
        fb.SetPixel(x + 3, y + 12, EgaColorIndex.BrightCyan);
        fb.SetPixel(x + 12, y + 12, EgaColorIndex.BrightCyan);
    }

    private static void DrawLadder(FrameBuffer fb, int x, int y)
    {
        // Heavy tubular steel gantry ladder
        // Left Rail (X: 1..3)
        fb.DrawVLine(x + 1, y, 16, EgaColorIndex.BrightWhite); // Specular highlight
        fb.DrawVLine(x + 2, y, 16, EgaColorIndex.LightGray);   // Tubular body
        fb.DrawVLine(x + 3, y, 16, EgaColorIndex.DarkGray);    // Shadow

        // Right Rail (X: 12..14)
        fb.DrawVLine(x + 12, y, 16, EgaColorIndex.BrightWhite);
        fb.DrawVLine(x + 13, y, 16, EgaColorIndex.LightGray);
        fb.DrawVLine(x + 14, y, 16, EgaColorIndex.DarkGray);

        // Wall anchor brackets (at Y=0 and Y=14)
        fb.FillRect(x, y, 2, 2, EgaColorIndex.DarkGray);
        fb.FillRect(x + 14, y, 2, 2, EgaColorIndex.DarkGray);
        fb.SetPixel(x, y, EgaColorIndex.Black);
        fb.SetPixel(x + 15, y, EgaColorIndex.Black);

        // 4 Round steel rungs with knurled grip texture (at Y=2, 6, 10, 14)
        for (int ry = 2; ry < 16; ry += 4)
        {
            // Top highlight
            fb.DrawHLine(x + 4, y + ry, 8, EgaColorIndex.BrightWhite);
            // Main rung bar
            fb.DrawHLine(x + 4, y + ry + 1, 8, EgaColorIndex.LightGray);
            // Grip knurl notches
            for (int k = 0; k < 8; k += 2)
            {
                fb.SetPixel(x + 4 + k, y + ry + 1, EgaColorIndex.DarkGray);
            }
            // Bottom shadow
            fb.DrawHLine(x + 4, y + ry + 2, 8, EgaColorIndex.Black);
        }
    }

    private static void DrawCargoCrate(FrameBuffer fb, int x, int y)
    {
        // Apogee-style wooden cargo supply crate with iron straps and stencil logo
        // Wood plank body
        fb.FillRect(x, y, 16, 16, EgaColorIndex.Brown);
        fb.DrawRect(x, y, 16, 16, EgaColorIndex.Black);

        // Horizontal plank seams and fine grain highlights
        fb.DrawHLine(x + 1, y + 5, 14, EgaColorIndex.Black);
        fb.DrawHLine(x + 1, y + 10, 14, EgaColorIndex.Black);
        fb.DrawHLine(x + 2, y + 2, 12, EgaColorIndex.Yellow);
        fb.DrawHLine(x + 2, y + 7, 12, EgaColorIndex.Yellow);
        fb.DrawHLine(x + 2, y + 12, 12, EgaColorIndex.Yellow);

        // Steel diagonal cross-braces
        for (int d = 2; d < 14; d++)
        {
            fb.SetPixel(x + d, y + d, EgaColorIndex.DarkGray);
            fb.SetPixel(x + (15 - d), y + d, EgaColorIndex.DarkGray);
        }

        // Metal reinforced corner brackets with rivets
        fb.FillRect(x + 1, y + 1, 3, 3, EgaColorIndex.LightGray);
        fb.FillRect(x + 12, y + 1, 3, 3, EgaColorIndex.LightGray);
        fb.FillRect(x + 1, y + 12, 3, 3, EgaColorIndex.LightGray);
        fb.FillRect(x + 12, y + 12, 3, 3, EgaColorIndex.LightGray);
        fb.SetPixel(x + 2, y + 2, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 13, y + 2, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 2, y + 13, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 13, y + 13, EgaColorIndex.BrightWhite);

        // Center stenciled yellow hazard / tech emblem
        fb.FillRect(x + 7, y + 6, 2, 4, EgaColorIndex.Yellow);
        fb.SetPixel(x + 6, y + 7, EgaColorIndex.Yellow);
        fb.SetPixel(x + 9, y + 7, EgaColorIndex.Yellow);
        fb.SetPixel(x + 7, y + 7, EgaColorIndex.BrightWhite);
    }

    public void DrawPlayer(FrameBuffer fb, PlayerEntity player, int screenX, int screenY)
    {
        // 1.2s I-frame damage flicker: blinks on/off every 4 frames
        if (player.InvulnerabilityTimer > 0 && ((int)(player.InvulnerabilityTimer * 20) % 2 == 0))
        {
            return;
        }

        int px = screenX;
        int py = screenY;

        // Speed dash visual trail effect (cyan energy shadow)
        if (player.HasSpeedDash && (player.State == PlayerState.Running || player.State == PlayerState.Walking))
        {
            int trailX = (player.Facing == Direction.Right) ? px - 4 : px + 4;
            fb.DrawVLine(trailX + 6, py + 4, 16, EgaColorIndex.BrightCyan);
        }

        if (player.State == PlayerState.Climbing)
        {
            DrawPlayerClimbing(fb, player, px, py);
            return;
        }

        DrawPlayerAction(fb, player, px, py);
    }

    private static void DrawPlayerClimbing(FrameBuffer fb, PlayerEntity player, int px, int py)
    {
        // Authentic Duke Nukem rear-view ladder climbing sprite
        int step = ((int)(player.AnimTimer * 8) % 2) == 0 ? 1 : -1;

        // 1. Blonde flat-top hair (viewed from back)
        fb.FillRect(px + 3, py, 8, 4, EgaColorIndex.Yellow);
        fb.DrawHLine(px + 3, py, 8, EgaColorIndex.BrightWhite); // Shiny crown flat top
        fb.DrawHLine(px + 4, py + 3, 6, EgaColorIndex.Brown);  // Hairline shadow
        fb.SetPixel(px + 3, py + 1, EgaColorIndex.Brown);      // Side taper
        fb.SetPixel(px + 10, py + 1, EgaColorIndex.Brown);

        // 2. Muscular back in Red Tank Top
        fb.FillRect(px + 2, py + 4, 10, 9, EgaColorIndex.BrightRed);
        fb.DrawVLine(px + 6, py + 5, 6, EgaColorIndex.Red); // Scapular contour
        fb.DrawVLine(px + 7, py + 5, 6, EgaColorIndex.Red);

        // Tactical cross-harness straps on back
        fb.SetPixel(px + 3, py + 5, EgaColorIndex.Black);
        fb.SetPixel(px + 10, py + 5, EgaColorIndex.Black);
        fb.SetPixel(px + 5, py + 7, EgaColorIndex.Black);
        fb.SetPixel(px + 8, py + 7, EgaColorIndex.Black);
        fb.SetPixel(px + 6, py + 9, EgaColorIndex.DarkGray); // Center ring

        // 3. Bare muscular arms gripping ladder rungs (alternating reach)
        int leftArmY = py + 4 - (step * 2);
        int rightArmY = py + 4 + (step * 2);
        fb.FillRect(px, leftArmY, 3, 5, EgaColorIndex.Brown);
        fb.SetPixel(px + 1, leftArmY, EgaColorIndex.Yellow); // Deltoid highlight
        fb.FillRect(px + 11, rightArmY, 3, 5, EgaColorIndex.Brown);
        fb.SetPixel(px + 12, rightArmY, EgaColorIndex.Yellow);

        // 4. Utility belt with holster and brass buckle
        fb.FillRect(px + 2, py + 13, 10, 2, EgaColorIndex.DarkGray);
        fb.SetPixel(px + 6, py + 13, EgaColorIndex.Yellow); // Buckle
        fb.SetPixel(px + 7, py + 13, EgaColorIndex.BrightWhite);

        // 5. Blue Denim Jeans
        fb.FillRect(px + 3, py + 15, 8, 5, EgaColorIndex.BrightBlue);
        fb.DrawVLine(px + 6, py + 15, 5, EgaColorIndex.Blue); // Center seam

        // 6. Combat boots gripping ladder rungs
        int leftBootY = py + 20 + (step * 2);
        int rightBootY = py + 20 - (step * 2);
        fb.FillRect(px + 2, leftBootY, 4, 3, EgaColorIndex.DarkGray);
        fb.SetPixel(px + 2, leftBootY + 1, EgaColorIndex.LightGray); // Steel toe
        fb.FillRect(px + 8, rightBootY, 4, 3, EgaColorIndex.DarkGray);
        fb.SetPixel(px + 11, rightBootY + 1, EgaColorIndex.LightGray);
        fb.DrawHLine(px + 2, leftBootY + 2, 4, EgaColorIndex.Black); // Rubber tread
        fb.DrawHLine(px + 8, rightBootY + 2, 4, EgaColorIndex.Black);
    }

    private static void DrawPlayerAction(FrameBuffer fb, PlayerEntity player, int px, int py)
    {
        bool flip = player.Facing == Direction.Left;

        // Animation stride offset
        int legStep = 0;
        int gunBob = 0;
        int leanX = 0;

        if (player.State == PlayerState.Walking || player.State == PlayerState.Running)
        {
            int cadence = (player.State == PlayerState.Running) ? 14 : 9;
            int phase = (int)(player.AnimTimer * cadence) % 4;
            legStep = (phase == 0) ? 2 : (phase == 2) ? -2 : 0;
            gunBob = (phase == 1 || phase == 3) ? 1 : 0;
            if (player.State == PlayerState.Running) leanX = flip ? -1 : 1;
        }
        else if (player.State == PlayerState.Jumping || player.State == PlayerState.Falling)
        {
            legStep = -1; // Tucked legs
            gunBob = -1;  // Aimed forward
        }

        px += leanX;

        // 1. Signature Blonde Flat-top Hair
        int hairX = flip ? px + 3 : px + 2;
        // Flat top surface with bright white glint
        fb.DrawHLine(hairX, py, 8, EgaColorIndex.BrightWhite);
        fb.FillRect(hairX, py + 1, 8, 3, EgaColorIndex.Yellow);
        // Neck/back-of-head shadow
        int backHeadX = flip ? hairX + 6 : hairX;
        fb.FillRect(backHeadX, py + 1, 2, 3, EgaColorIndex.Brown);

        // 2. Face, Sunglasses & Strong Jaw
        int faceX = hairX;
        fb.FillRect(faceX, py + 4, 8, 4, EgaColorIndex.Brown); // Tan skin base
        // Cheekbone & Jaw highlight
        fb.DrawHLine(faceX + 1, py + 7, 6, EgaColorIndex.Yellow);

        // Iconic sunglasses with white specular reflection lens glint
        int glassesX = flip ? faceX + 1 : faceX + 3;
        fb.FillRect(glassesX, py + 4, 5, 2, EgaColorIndex.Black);
        // Specular glint on forward lens
        int glintX = flip ? glassesX + 1 : glassesX + 3;
        fb.SetPixel(glintX, py + 4, EgaColorIndex.BrightWhite);
        fb.SetPixel(glintX, py + 5, EgaColorIndex.BrightCyan);

        // Smirk
        fb.SetPixel(flip ? faceX + 2 : faceX + 5, py + 6, EgaColorIndex.Black);

        // 3. Muscular Torso in Red Tank Top
        fb.FillRect(px + 3, py + 8, 8, 6, EgaColorIndex.BrightRed);
        // Scoop neck showing tan chest
        fb.FillRect(flip ? px + 4 : px + 7, py + 8, 2, 1, EgaColorIndex.Brown);
        // Pectoral / rib definition
        fb.DrawVLine(flip ? px + 8 : px + 5, py + 9, 4, EgaColorIndex.Red);

        // Diagonal black tactical bandolier across chest
        fb.SetPixel(flip ? px + 4 : px + 9, py + 9, EgaColorIndex.Black);
        fb.SetPixel(flip ? px + 5 : px + 8, py + 10, EgaColorIndex.Yellow); // Brass bullet head!
        fb.SetPixel(flip ? px + 6 : px + 7, py + 11, EgaColorIndex.Black);
        fb.SetPixel(flip ? px + 7 : px + 6, py + 12, EgaColorIndex.Yellow);
        fb.SetPixel(flip ? px + 8 : px + 5, py + 13, EgaColorIndex.Black);

        // Bare muscular arms
        int armX = flip ? px + 9 : px + 1;
        fb.FillRect(armX, py + 9, 3, 5, EgaColorIndex.Brown);
        fb.SetPixel(armX + 1, py + 9, EgaColorIndex.Yellow); // Bicep highlight

        // 4. Chrome Combat Blaster Pistol
        int gunY = py + 10 + gunBob;
        if (flip)
        {
            // Aimed Left
            fb.FillRect(px - 4, gunY, 6, 3, EgaColorIndex.LightGray);
            fb.DrawHLine(px - 4, gunY, 6, EgaColorIndex.BrightWhite); // Chrome top
            fb.SetPixel(px - 2, gunY + 1, EgaColorIndex.DarkGray);   // Slide ejection port
            fb.FillRect(px - 1, gunY + 2, 2, 3, EgaColorIndex.Black); // Textured grip
            fb.SetPixel(px - 5, gunY + 1, EgaColorIndex.BrightCyan);  // Muzzle energy dot
        }
        else
        {
            // Aimed Right
            fb.FillRect(px + 10, gunY, 6, 3, EgaColorIndex.LightGray);
            fb.DrawHLine(px + 10, gunY, 6, EgaColorIndex.BrightWhite); // Chrome top
            fb.SetPixel(px + 13, gunY + 1, EgaColorIndex.DarkGray);   // Slide ejection port
            fb.FillRect(px + 11, gunY + 2, 2, 3, EgaColorIndex.Black); // Textured grip
            fb.SetPixel(px + 16, gunY + 1, EgaColorIndex.BrightCyan);  // Muzzle energy dot
        }

        // 5. Tactical Utility Belt with Golden Buckle
        fb.FillRect(px + 3, py + 14, 8, 2, EgaColorIndex.DarkGray);
        int buckleX = flip ? px + 6 : px + 5;
        fb.FillRect(buckleX, py + 14, 2, 2, EgaColorIndex.Yellow);
        fb.SetPixel(buckleX, py + 14, EgaColorIndex.BrightWhite);

        // 6. Blue Denim Jeans
        fb.FillRect(px + 3, py + 16, 8, 4, EgaColorIndex.BrightBlue);
        fb.DrawVLine(px + 6, py + 16, 4, EgaColorIndex.Blue); // Center crease

        // 7. High Combat Boots with Steel Toe Caps
        int bootLeftY = py + 20 + legStep;
        int bootRightY = py + 20 - legStep;
        // Left boot
        fb.FillRect(px + 2, bootLeftY, 4, 4, EgaColorIndex.DarkGray);
        fb.SetPixel(px + 2, bootLeftY + 1, EgaColorIndex.LightGray); // Steel toe cap
        fb.DrawHLine(px + 2, bootLeftY + 3, 4, EgaColorIndex.Black);  // Tread
        // Right boot
        fb.FillRect(px + 8, bootRightY, 4, 4, EgaColorIndex.DarkGray);
        fb.SetPixel(px + 11, bootRightY + 1, EgaColorIndex.LightGray);
        fb.DrawHLine(px + 8, bootRightY + 3, 4, EgaColorIndex.Black);
    }

    public void DrawProjectile(FrameBuffer fb, in Projectile projectile, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 184) return;

        if (projectile.FromPlayer)
        {
            // High-velocity laser bolt: 10px long, bright white core, neon yellow plasma shell, cyan nose cone
            fb.FillRect(screenX, screenY, 10, 3, EgaColorIndex.Yellow);
            fb.DrawHLine(screenX + 1, screenY + 1, 8, EgaColorIndex.BrightWhite);
            fb.SetPixel(screenX + 9, screenY + 1, EgaColorIndex.BrightCyan);
            // Trailing plasma ionization embers
            fb.SetPixel(screenX - 1, screenY + 1, EgaColorIndex.BrightCyan);
            fb.SetPixel(screenX - 2, screenY, EgaColorIndex.Blue);
        }
        else
        {
            // Volatile Alien Plasma Orb: pulsating red/yellow plasma sphere with white center
            fb.FillRect(screenX, screenY, 6, 6, EgaColorIndex.BrightRed);
            fb.FillRect(screenX + 1, screenY + 1, 4, 4, EgaColorIndex.Yellow);
            fb.FillRect(screenX + 2, screenY + 2, 2, 2, EgaColorIndex.BrightWhite);
            // Crackling sparks
            fb.SetPixel(screenX - 1, screenY + 2, EgaColorIndex.BrightRed);
            fb.SetPixel(screenX + 6, screenY + 3, EgaColorIndex.BrightRed);
        }
    }

    public void DrawMonster(FrameBuffer fb, MonsterEntity monster, int screenX, int screenY)
    {
        if (screenX < -32 || screenX > 320 || screenY < -32 || screenY > 184) return;

        bool flash = monster.HitFlashTimer > 0;
        int mx = screenX;
        int my = screenY;

        switch (monster.Type)
        {
            case MonsterType.PatrolDroid:
                DrawPatrolDroid(fb, monster, mx, my, flash);
                break;

            case MonsterType.HoverDrone:
                DrawHoverDrone(fb, monster, mx, my, flash);
                break;

            case MonsterType.CeilingTurret:
                DrawCeilingTurret(fb, monster, mx, my, flash);
                break;

            case MonsterType.MechaHound:
                DrawMechaHound(fb, monster, mx, my, flash);
                break;

            case MonsterType.HeavyGuard:
                DrawHeavyGuard(fb, monster, mx, my, flash);
                break;
        }
    }

    private static void DrawPatrolDroid(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // 14x14 Spherical chrome patrol bot with glowing cyclops optic & rotating tank treads
        var bodyCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray;
        var shadowCol = flash ? EgaColorIndex.BrightRed : EgaColorIndex.DarkGray;
        bool flip = monster.Facing == Direction.Left;

        // Top beacon antenna
        fb.DrawVLine(mx + 7, my, 2, EgaColorIndex.DarkGray);
        fb.SetPixel(mx + 7, my, ((int)(monster.AnimTimer * 8) % 2 == 0) ? EgaColorIndex.BrightRed : EgaColorIndex.Yellow);

        // Spherical armored hull
        fb.FillRect(mx + 2, my + 2, 10, 8, bodyCol);
        fb.DrawHLine(mx + 3, my + 2, 8, EgaColorIndex.BrightWhite); // Specular highlight
        fb.DrawHLine(mx + 3, my + 9, 8, shadowCol);
        fb.DrawVLine(mx + 2, my + 3, 6, shadowCol);
        fb.DrawVLine(mx + 11, my + 3, 6, shadowCol);

        // Glowing red cyclops optic lens
        int eyeX = flip ? mx + 3 : mx + 7;
        fb.FillRect(eyeX, my + 4, 4, 3, EgaColorIndex.Black); // Bezel
        fb.FillRect(eyeX + 1, my + 5, 2, 2, flash ? EgaColorIndex.BrightWhite : EgaColorIndex.BrightRed);
        fb.SetPixel(eyeX + 1, my + 5, EgaColorIndex.BrightWhite); // Pupil glint

        // Caterpillar tank tread roller unit on bottom
        fb.FillRect(mx + 1, my + 10, 12, 4, EgaColorIndex.Black);
        fb.DrawHLine(mx + 2, my + 10, 10, EgaColorIndex.DarkGray);
        // Rotating road wheels / tread links
        int treadPhase = ((int)(monster.AnimTimer * 12) % 3);
        for (int w = 2 + treadPhase; w < 12; w += 3)
        {
            fb.SetPixel(mx + w, my + 12, EgaColorIndex.LightGray);
        }
    }

    private static void DrawHoverDrone(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // 14x12 High-tech hunter-seeker drone with glowing visor and pulsing plasma thruster
        var hullCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.DarkGray;
        var trimCol = flash ? EgaColorIndex.BrightRed : EgaColorIndex.LightGray;

        // Swept stealth canopy
        fb.FillRect(mx + 2, my + 2, 10, 6, hullCol);
        fb.DrawHLine(mx + 3, my + 2, 8, EgaColorIndex.BrightWhite);
        fb.DrawHLine(mx + 2, my + 7, 10, trimCol);

        // Side stabilizer wings with red navigation tips
        fb.FillRect(mx, my + 4, 2, 2, trimCol);
        fb.SetPixel(mx, my + 4, EgaColorIndex.BrightRed);
        fb.FillRect(mx + 12, my + 4, 2, 2, trimCol);
        fb.SetPixel(mx + 13, my + 4, EgaColorIndex.BrightGreen);

        // Dual cyan optical lenses
        fb.FillRect(mx + 4, my + 4, 2, 2, EgaColorIndex.BrightCyan);
        fb.SetPixel(mx + 4, my + 4, EgaColorIndex.BrightWhite);
        fb.FillRect(mx + 8, my + 4, 2, 2, EgaColorIndex.BrightCyan);
        fb.SetPixel(mx + 8, my + 4, EgaColorIndex.BrightWhite);

        // Animated ion plasma thruster plume beneath
        int thrusterPhase = ((int)(monster.AnimTimer * 16) % 3);
        int flameLen = (thrusterPhase == 0) ? 4 : (thrusterPhase == 1) ? 3 : 2;
        // White core
        fb.FillRect(mx + 6, my + 8, 2, 2, EgaColorIndex.BrightWhite);
        // Cyan plume
        fb.FillRect(mx + 5, my + 9, 4, flameLen, EgaColorIndex.BrightCyan);
        // Blue outer flame
        fb.SetPixel(mx + 6, my + 9 + flameLen, EgaColorIndex.BrightBlue);
        fb.SetPixel(mx + 7, my + 9 + flameLen, EgaColorIndex.BrightBlue);
    }

    private static void DrawCeilingTurret(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // 14x14 Heavy ceiling mount swivel turret with twin autocannon barrels
        var plateCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.DarkGray;
        var trimCol = flash ? EgaColorIndex.BrightRed : EgaColorIndex.LightGray;

        // Ceiling mounting plate with anchor bolts
        fb.FillRect(mx + 1, my, 12, 3, plateCol);
        fb.DrawHLine(mx + 1, my + 2, 12, trimCol);
        fb.SetPixel(mx + 2, my + 1, EgaColorIndex.BrightCyan); // Bolts
        fb.SetPixel(mx + 11, my + 1, EgaColorIndex.BrightCyan);

        // Swivel sphere housing
        fb.FillRect(mx + 3, my + 3, 8, 5, flash ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray);
        fb.DrawRect(mx + 3, my + 3, 8, 5, EgaColorIndex.DarkGray);

        // Center targeting sensor with glowing red lens
        fb.FillRect(mx + 6, my + 5, 2, 2, EgaColorIndex.BrightRed);
        fb.SetPixel(mx + 6, my + 5, EgaColorIndex.BrightWhite);

        // Twin fluted autocannon barrels extending downward
        fb.FillRect(mx + 4, my + 8, 2, 5, EgaColorIndex.DarkGray);
        fb.DrawVLine(mx + 4, my + 8, 5, EgaColorIndex.LightGray);
        fb.SetPixel(mx + 4, my + 13, EgaColorIndex.Black); // Muzzle

        fb.FillRect(mx + 8, my + 8, 2, 5, EgaColorIndex.DarkGray);
        fb.DrawVLine(mx + 8, my + 8, 5, EgaColorIndex.LightGray);
        fb.SetPixel(mx + 8, my + 13, EgaColorIndex.Black);

        // Targeting laser guide dot
        if ((int)(monster.AnimTimer * 4) % 2 == 0)
        {
            fb.SetPixel(mx + 7, my + 15, EgaColorIndex.BrightRed);
        }
    }

    private static void DrawMechaHound(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // 24x14 Lethal quadruped cyber-hound with hydraulic legs and glowing optic
        var bodyCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.DarkGray;
        var armorCol = flash ? EgaColorIndex.BrightRed : EgaColorIndex.LightGray;
        bool flip = monster.Facing == Direction.Left;

        // Armored spinal torso with hydraulic pistons
        fb.FillRect(mx + 6, my + 3, 12, 6, bodyCol);
        fb.DrawHLine(mx + 6, my + 3, 12, armorCol); // Spine plate
        fb.DrawDitherRect(mx + 7, my + 5, 10, 3, armorCol, bodyCol);

        // Aggressive predatory skull
        int headX = flip ? mx : mx + 16;
        fb.FillRect(headX, my + 2, 8, 6, armorCol);
        fb.DrawRect(headX, my + 2, 8, 6, EgaColorIndex.Black);

        // Glowing red optic slit
        int eyeX = flip ? headX + 1 : headX + 5;
        fb.FillRect(eyeX, my + 3, 2, 2, EgaColorIndex.BrightRed);
        fb.SetPixel(eyeX, my + 3, EgaColorIndex.BrightWhite);

        // Razor-toothed metallic jaw
        fb.FillRect(flip ? headX : headX + 4, my + 7, 4, 2, EgaColorIndex.Black);
        fb.SetPixel(flip ? headX + 1 : headX + 5, my + 7, EgaColorIndex.BrightWhite); // Fang

        // 4 Running hydraulic legs (rapid gallop cycle)
        int step = ((int)(monster.AnimTimer * 12) % 2 == 0) ? 2 : -2;
        // Front legs
        int frontLegX = flip ? mx + 3 : mx + 17;
        fb.FillRect(frontLegX, my + 9 + step, 2, 5, armorCol);
        fb.FillRect(frontLegX + 3, my + 9 - step, 2, 5, bodyCol);
        // Rear legs
        int rearLegX = flip ? mx + 13 : mx + 7;
        fb.FillRect(rearLegX, my + 9 - step, 2, 5, armorCol);
        fb.FillRect(rearLegX + 3, my + 9 + step, 2, 5, bodyCol);
    }

    private static void DrawHeavyGuard(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // 14x26 Walking cyborg dreadnought enforcer with rotary Gatling cannon
        var armorCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.DarkGray;
        var plateCol = flash ? EgaColorIndex.BrightRed : EgaColorIndex.LightGray;
        bool flip = monster.Facing == Direction.Left;

        // Heavy Combat Helmet
        fb.FillRect(mx + 3, my + 1, 8, 6, plateCol);
        fb.DrawHLine(mx + 4, my + 1, 6, EgaColorIndex.BrightWhite);
        // Visor slit
        int visorX = flip ? mx + 3 : mx + 7;
        fb.FillRect(visorX, my + 4, 4, 2, EgaColorIndex.BrightRed);
        fb.SetPixel(visorX + 1, my + 4, EgaColorIndex.BrightWhite);

        // Massive Pauldron with caution hazard stripes
        fb.FillRect(mx + 1, my + 7, 12, 4, EgaColorIndex.Yellow);
        fb.DrawRect(mx + 1, my + 7, 12, 4, EgaColorIndex.Black);
        fb.SetPixel(mx + 3, my + 8, EgaColorIndex.Black);
        fb.SetPixel(mx + 7, my + 8, EgaColorIndex.Black);
        fb.SetPixel(mx + 11, my + 8, EgaColorIndex.Black);

        // Armored Torso with reactor core grille
        fb.FillRect(mx + 2, my + 11, 10, 8, armorCol);
        fb.DrawRect(mx + 2, my + 11, 10, 8, plateCol);
        fb.FillRect(mx + 5, my + 13, 4, 3, EgaColorIndex.BrightCyan); // Reactor glow
        fb.SetPixel(mx + 6, my + 14, EgaColorIndex.BrightWhite);

        // Heavy Rotary Gatling Arm Cannon
        int cannonX = flip ? mx - 4 : mx + 10;
        fb.FillRect(cannonX, my + 10, 8, 5, EgaColorIndex.DarkGray);
        fb.DrawHLine(cannonX, my + 10, 8, EgaColorIndex.LightGray);
        // Barrels
        fb.FillRect(flip ? cannonX - 2 : cannonX + 8, my + 11, 2, 3, EgaColorIndex.Black);

        // Heavy Piston-Driven Legs & Blast Boots
        int stride = ((int)(monster.AnimTimer * 6) % 2 == 0) ? 1 : -1;
        fb.FillRect(mx + 2, my + 19 + stride, 4, 7, plateCol);
        fb.FillRect(mx + 8, my + 19 - stride, 4, 7, plateCol);
        // Steel toe stomp plates
        fb.FillRect(mx + 1, my + 24 + stride, 5, 2, EgaColorIndex.Black);
        fb.FillRect(mx + 8, my + 24 - stride, 5, 2, EgaColorIndex.Black);
    }

    public void DrawPickup(FrameBuffer fb, in PickupItem pickup, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 184) return;

        int px = screenX;
        int py = screenY;
        bool sparkle = ((int)(pickup.AnimTimer * 8) % 2) == 0;

        switch (pickup.Type)
        {
            case PickupType.BlueGem:
                // Royal cut sapphire crystal (+100 score) with sparkling twinkle
                fb.FillRect(px + 4, py + 2, 6, 8, EgaColorIndex.BrightCyan);
                fb.FillRect(px + 5, py + 1, 4, 10, EgaColorIndex.BrightCyan);
                // Deep blue facets
                fb.DrawVLine(px + 4, py + 3, 6, EgaColorIndex.Blue);
                fb.DrawVLine(px + 9, py + 3, 6, EgaColorIndex.Blue);
                fb.DrawHLine(px + 5, py + 10, 4, EgaColorIndex.Blue);
                // Specular highlight facet
                fb.FillRect(px + 5, py + 3, 2, 3, EgaColorIndex.BrightWhite);
                if (sparkle) fb.DrawSparkle(px + 8, py + 2, EgaColorIndex.BrightWhite);
                break;

            case PickupType.RedRuby:
                // Emerald-cut radiant ruby (+250 score)
                fb.FillRect(px + 3, py + 2, 8, 8, EgaColorIndex.BrightRed);
                fb.DrawRect(px + 3, py + 2, 8, 8, EgaColorIndex.Red);
                // Brilliant faceted inner diamond
                fb.FillRect(px + 5, py + 4, 4, 4, EgaColorIndex.Yellow);
                fb.FillRect(px + 6, py + 5, 2, 2, EgaColorIndex.BrightWhite);
                if (sparkle) fb.DrawSparkle(px + 4, py + 3, EgaColorIndex.BrightWhite);
                break;

            case PickupType.GoldCartridge:
                // Retro 1990s Game Cartridge (+500 score)
                fb.FillRect(px + 2, py + 1, 10, 11, EgaColorIndex.Yellow);
                fb.DrawRect(px + 2, py + 1, 10, 11, EgaColorIndex.Brown);
                // Grip ridges on top
                fb.SetPixel(px + 4, py + 2, EgaColorIndex.Brown);
                fb.SetPixel(px + 6, py + 2, EgaColorIndex.Brown);
                fb.SetPixel(px + 8, py + 2, EgaColorIndex.Brown);
                // Microchip circuit board label
                fb.FillRect(px + 4, py + 4, 6, 5, EgaColorIndex.Black);
                fb.FillRect(px + 5, py + 5, 4, 3, EgaColorIndex.BrightCyan);
                // Gold connector pins at bottom
                fb.DrawHLine(px + 4, py + 11, 6, EgaColorIndex.Yellow);
                if (sparkle) fb.DrawSparkle(px + 9, py + 2, EgaColorIndex.BrightWhite);
                break;

            case PickupType.MedKit:
                // Military trauma kit case (+1 HP)
                fb.FillRect(px + 2, py + 3, 11, 9, EgaColorIndex.BrightWhite);
                fb.DrawRect(px + 2, py + 3, 11, 9, EgaColorIndex.LightGray);
                // Steel carry handle
                fb.DrawHLine(px + 5, py + 1, 4, EgaColorIndex.DarkGray);
                fb.DrawVLine(px + 5, py + 2, 2, EgaColorIndex.DarkGray);
                fb.DrawVLine(px + 8, py + 2, 2, EgaColorIndex.DarkGray);
                // Corner protective guards
                fb.SetPixel(px + 2, py + 3, EgaColorIndex.DarkGray);
                fb.SetPixel(px + 12, py + 3, EgaColorIndex.DarkGray);
                fb.SetPixel(px + 2, py + 11, EgaColorIndex.DarkGray);
                fb.SetPixel(px + 12, py + 11, EgaColorIndex.DarkGray);
                // Embossed Red Cross emblem
                fb.FillRect(px + 6, py + 5, 3, 5, EgaColorIndex.BrightRed);
                fb.FillRect(px + 5, py + 6, 5, 3, EgaColorIndex.BrightRed);
                break;

            case PickupType.RapidFire:
                DrawBadge(fb, px, py, "R", EgaColorIndex.BrightRed, EgaColorIndex.Yellow, EgaColorIndex.Yellow);
                break;

            case PickupType.HighJumpBoots:
                DrawBadge(fb, px, py, "J", EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite, EgaColorIndex.BrightWhite);
                break;

            case PickupType.SpeedDash:
                DrawBadge(fb, px, py, "S", EgaColorIndex.BrightGreen, EgaColorIndex.Green, EgaColorIndex.BrightWhite);
                break;
        }
    }

    private static void DrawBadge(FrameBuffer fb, int px, int py, string letter, EgaColorIndex bgCol, EgaColorIndex borderCol, EgaColorIndex textCol)
    {
        // 3D beveled tactile arcade powerup badge
        fb.FillRect(px + 1, py + 1, 12, 12, bgCol);
        fb.DrawRect(px + 1, py + 1, 12, 12, borderCol);
        fb.DrawHLine(px + 2, py + 2, 10, EgaColorIndex.BrightWhite); // Top bevel
        fb.DrawVLine(px + 2, py + 2, 10, EgaColorIndex.BrightWhite);
        fb.DrawText(px + 4, py + 3, letter, textCol);
    }

    public void DrawHud(FrameBuffer fb, PlayerEntity player, string seed, int fps)
    {
        // 1. Top Minimalist Status Console (320x16, Y: 0..16)
        // Brushed metallic bezel
        fb.DrawBeveledBox(0, 0, 320, 16, EgaColorIndex.DarkGray, EgaColorIndex.LightGray, EgaColorIndex.Black);

        // SCORE Display with beveled inset panel
        fb.FillRect(4, 2, 94, 12, EgaColorIndex.Black);
        fb.DrawRect(4, 2, 94, 12, EgaColorIndex.LightGray);
        fb.DrawText(6, 4, "SCORE:", EgaColorIndex.Yellow);
        fb.DrawInt(56, 4, player.Score, 6, EgaColorIndex.BrightWhite);

        // HP Display with digital LED cells
        fb.FillRect(102, 2, 80, 12, EgaColorIndex.Black);
        fb.DrawRect(102, 2, 80, 12, EgaColorIndex.LightGray);
        fb.DrawText(105, 4, "HP:", EgaColorIndex.BrightRed);
        for (int i = 0; i < PlayerEntity.MaxHealth; i++)
        {
            int cellX = 130 + i * 9;
            if (i < player.Health)
            {
                // Full high-energy cell
                fb.FillRect(cellX, 4, 7, 8, EgaColorIndex.BrightGreen);
                fb.DrawRect(cellX, 4, 7, 8, EgaColorIndex.Green);
                fb.DrawHLine(cellX + 1, 5, 5, EgaColorIndex.BrightWhite); // Core glow
            }
            else
            {
                // Depleted dark red slot
                fb.DrawRect(cellX, 4, 7, 8, EgaColorIndex.Red);
                fb.SetPixel(cellX + 3, 7, EgaColorIndex.DarkGray);
            }
        }

        // Active Powerup Badges (Lost on damage)
        int badgeX = 186;
        if (player.HasRapidFire)
        {
            DrawBadge(fb, badgeX, 1, "R", EgaColorIndex.BrightRed, EgaColorIndex.Yellow, EgaColorIndex.Yellow);
            badgeX += 13;
        }
        if (player.HasHighJumpBoots)
        {
            DrawBadge(fb, badgeX, 1, "J", EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite, EgaColorIndex.BrightWhite);
            badgeX += 13;
        }
        if (player.HasSpeedDash)
        {
            DrawBadge(fb, badgeX, 1, "S", EgaColorIndex.BrightGreen, EgaColorIndex.Green, EgaColorIndex.BrightWhite);
            badgeX += 13;
        }

        // SEED Display (truncated cleanly if longer than 6 characters)
        fb.FillRect(228, 2, 54, 12, EgaColorIndex.Black);
        fb.DrawRect(228, 2, 54, 12, EgaColorIndex.LightGray);
        string displaySeed = seed.Length > 6 ? seed[..6] : seed;
        fb.DrawText(231, 4, displaySeed, EgaColorIndex.BrightCyan);

        // Version badge in corner
        fb.DrawText(286, 4, "v" + GameVersion.Version, EgaColorIndex.Yellow);

        // 2. Bottom Controls Strip (320x16, Y: 184..200)
        fb.DrawBeveledBox(0, 184, 320, 16, EgaColorIndex.Blue, EgaColorIndex.BrightCyan, EgaColorIndex.Black);
        fb.DrawText(4, 188, "ARROWS/WASD:MOVE/CLIMB  CTRL/SPC:JUMP  ALT/X/J:SHOOT  SHIFT:RUN  DN+JMP:DROP", EgaColorIndex.BrightWhite);
    }
}
