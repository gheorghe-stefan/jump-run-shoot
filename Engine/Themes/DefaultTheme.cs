using System;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Authentic Duke Nukem 1 (1991) theme.
/// Clean, razor-sharp pixel art with 100% solid 16-color EGA palette:
/// Bold black outlines, crisp white highlights, solid light-gray platform slabs,
/// vibrant hazard chevrons, and the iconic Duke Nukem sprite. Zero dithering / antialiasing.
/// </summary>
public sealed class DefaultTheme : IGameTheme
{
    public string Name => "Duke Nukem 1 Classic (Solid EGA)";

    // Star clusters for authentic Apogee night sky
    private static readonly (int X, int Y, EgaColorIndex Color)[] Stars = new (int, int, EgaColorIndex)[]
    {
        (20, 20, EgaColorIndex.BrightCyan), (25, 22, EgaColorIndex.BrightBlue), (28, 19, EgaColorIndex.BrightBlue),
        (65, 35, EgaColorIndex.BrightWhite), (68, 38, EgaColorIndex.BrightCyan), (72, 34, EgaColorIndex.BrightBlue),
        (110, 24, EgaColorIndex.BrightCyan), (115, 25, EgaColorIndex.BrightBlue), (118, 22, EgaColorIndex.BrightWhite),
        (160, 42, EgaColorIndex.BrightBlue), (164, 40, EgaColorIndex.BrightCyan), (168, 44, EgaColorIndex.BrightBlue),
        (210, 28, EgaColorIndex.BrightWhite), (214, 30, EgaColorIndex.BrightCyan), (218, 26, EgaColorIndex.BrightBlue),
        (255, 36, EgaColorIndex.BrightCyan), (260, 38, EgaColorIndex.BrightBlue), (265, 35, EgaColorIndex.BrightWhite),
        (295, 22, EgaColorIndex.BrightCyan), (300, 25, EgaColorIndex.BrightBlue), (305, 20, EgaColorIndex.BrightBlue),
        (45, 60, EgaColorIndex.BrightBlue), (135, 55, EgaColorIndex.BrightCyan), (230, 62, EgaColorIndex.BrightWhite)
    };

    public void DrawBackground(FrameBuffer fb, Camera2D camera)
    {
        // 1. Deep Black open sky in upper half
        fb.Clear(EgaColorIndex.Black);

        // 2. Distant industrial pillars (fluted vertical columns)
        int pillarOffset = (int)(camera.X * 0.2f);
        for (int i = 0; i < 7; i++)
        {
            int px = (i * 56 - (pillarOffset % 392));
            if (px < -30) px += 392;
            if (px >= -20 && px < 320)
            {
                // Fluted pillar with crisp vertical ribs
                fb.FillRect(px, 16, 20, 64, EgaColorIndex.DarkGray);
                fb.DrawVLine(px, 16, 64, EgaColorIndex.LightGray);
                fb.DrawVLine(px + 4, 16, 64, EgaColorIndex.LightGray);
                fb.DrawVLine(px + 8, 16, 64, EgaColorIndex.LightGray);
                fb.DrawVLine(px + 12, 16, 64, EgaColorIndex.LightGray);
                fb.DrawVLine(px + 16, 16, 64, EgaColorIndex.LightGray);
                fb.DrawVLine(px + 19, 16, 64, EgaColorIndex.Black);
            }
        }

        // 3. Crisp twinkling star clusters in deep space
        int starOffset = (int)(camera.X * 0.1f);
        for (int i = 0; i < Stars.Length; i++)
        {
            var star = Stars[i];
            int sx = (star.X - starOffset) % 320;
            if (sx < 0) sx += 320;
            if (sx < 320 && star.Y >= 16 && star.Y < 80)
            {
                fb.SetPixel(sx, star.Y, star.Color);
                if (star.Color == EgaColorIndex.BrightWhite)
                {
                    // 4-point star cross (+)
                    fb.SetPixel(sx - 1, star.Y, EgaColorIndex.BrightCyan);
                    fb.SetPixel(sx + 1, star.Y, EgaColorIndex.BrightCyan);
                    fb.SetPixel(sx, star.Y - 1, EgaColorIndex.BrightCyan);
                    fb.SetPixel(sx, star.Y + 1, EgaColorIndex.BrightCyan);
                }
            }
        }

        // 4. Solid DarkGray indoor facility wall (Y: 80..184)
        fb.FillRect(0, 80, 320, 104, EgaColorIndex.DarkGray);
        fb.DrawHLine(0, 80, 320, EgaColorIndex.Black);
        fb.DrawHLine(0, 81, 320, EgaColorIndex.LightGray);

        // 5. Wall details: Computer Terminals & Server Racks (parallax: 0.5x)
        int wallOffset = (int)(camera.X * 0.5f);
        for (int w = 0; w < 4; w++)
        {
            int wx = (w * 110 - (wallOffset % 440));
            if (wx < -50) wx += 440;
            if (wx > -40 && wx < 300)
            {
                if (w % 2 == 0)
                {
                    // Computer Monitor with Red Grid Graph (from Duke 1 screenshot)
                    fb.FillRect(wx, 110, 20, 16, EgaColorIndex.LightGray);
                    fb.DrawRect(wx, 110, 20, 16, EgaColorIndex.Black);
                    fb.FillRect(wx + 2, 112, 16, 12, EgaColorIndex.DarkGray);
                    fb.DrawRect(wx + 2, 112, 16, 12, EgaColorIndex.Black);
                    // Red grid line graph
                    fb.DrawHLine(wx + 3, 121, 14, EgaColorIndex.BrightWhite);
                    fb.SetPixel(wx + 4, 120, EgaColorIndex.BrightRed);
                    fb.SetPixel(wx + 5, 119, EgaColorIndex.BrightRed);
                    fb.SetPixel(wx + 6, 118, EgaColorIndex.BrightRed);
                    fb.SetPixel(wx + 7, 119, EgaColorIndex.BrightRed);
                    fb.SetPixel(wx + 8, 117, EgaColorIndex.BrightRed);
                    fb.SetPixel(wx + 9, 115, EgaColorIndex.BrightRed);
                    fb.SetPixel(wx + 10, 116, EgaColorIndex.BrightRed);
                    fb.SetPixel(wx + 11, 114, EgaColorIndex.BrightRed);
                    fb.SetPixel(wx + 12, 115, EgaColorIndex.BrightRed);
                }
                else
                {
                    // Server Rack / Drive Bay (from Duke 1 screenshot)
                    fb.FillRect(wx, 108, 28, 14, EgaColorIndex.BrightWhite);
                    fb.DrawRect(wx, 108, 28, 14, EgaColorIndex.Black);
                    fb.FillRect(wx + 2, 110, 24, 10, EgaColorIndex.Black);
                    // Server units with cyan/white LED lights
                    fb.FillRect(wx + 4, 112, 5, 6, EgaColorIndex.DarkGray);
                    fb.SetPixel(wx + 5, 114, EgaColorIndex.BrightCyan);
                    fb.FillRect(wx + 11, 112, 5, 6, EgaColorIndex.DarkGray);
                    fb.SetPixel(wx + 12, 114, EgaColorIndex.BrightCyan);
                    fb.FillRect(wx + 18, 112, 5, 6, EgaColorIndex.DarkGray);
                    fb.SetPixel(wx + 19, 114, EgaColorIndex.BrightGreen);
                }

                // Wall crack
                fb.SetPixel(wx + 45, 125, EgaColorIndex.Black);
                fb.SetPixel(wx + 46, 126, EgaColorIndex.Black);
                fb.SetPixel(wx + 45, 127, EgaColorIndex.Black);
                fb.SetPixel(wx + 47, 128, EgaColorIndex.Black);
                fb.SetPixel(wx + 46, 129, EgaColorIndex.Black);
                fb.SetPixel(wx + 47, 130, EgaColorIndex.Black);
            }
        }
    }

    public void DrawTile(FrameBuffer fb, TileType type, int screenX, int screenY, int col, int row)
    {
        if (screenX <= -16 || screenX >= 320 || screenY <= -16 || screenY >= 184) return;

        switch (type)
        {
            case TileType.Solid:
                DrawSolidPlatform(fb, screenX, screenY, col, row);
                break;

            case TileType.OneWayPlatform:
                DrawHazardPlatform(fb, screenX, screenY, col);
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
                DrawSupplyCrate(fb, screenX, screenY);
                break;
        }
    }

    private static void DrawSolidPlatform(FrameBuffer fb, int x, int y, int col, int row)
    {
        // Authentic Duke Nukem 1 solid floor slab:
        // Solid LightGray surface with a 1px pure BrightWhite top highlight line,
        // and segmented slab joints with 1px black vertical groove at block borders.
        fb.FillRect(x, y, 16, 16, EgaColorIndex.LightGray);

        // Crisp White walking surface highlight
        fb.DrawHLine(x, y, 16, EgaColorIndex.BrightWhite);
        fb.DrawHLine(x, y + 1, 16, EgaColorIndex.BrightWhite);

        // Segmented slab groove:
        // Left side joint
        fb.SetPixel(x, y, EgaColorIndex.Black);
        fb.SetPixel(x, y + 1, EgaColorIndex.DarkGray);
        fb.DrawVLine(x, y + 2, 14, EgaColorIndex.Black);

        // Under-slab beveled lip (row 4 and 15)
        fb.DrawHLine(x, y + 4, 16, EgaColorIndex.DarkGray);
        fb.DrawHLine(x, y + 15, 16, EgaColorIndex.Black);
    }

    private static void DrawHazardPlatform(FrameBuffer fb, int x, int y, int col)
    {
        // Authentic Duke Nukem 1 floating hazard catwalk:
        // Solid BrightRed outer border, solid Yellow body, thick diagonal hazard stripes
        fb.FillRect(x, y, 16, 8, EgaColorIndex.Yellow);

        // Bold BrightRed top and bottom borders
        fb.DrawHLine(x, y, 16, EgaColorIndex.BrightRed);
        fb.DrawHLine(x, y + 7, 16, EgaColorIndex.BrightRed);
        fb.DrawVLine(x, y, 8, EgaColorIndex.BrightRed);
        fb.DrawVLine(x + 15, y, 8, EgaColorIndex.BrightRed);

        // Crisp diagonal hazard stripes (alternating pure White and Yellow)
        for (int py = 1; py < 7; py++)
        {
            for (int px = 1; px < 15; px++)
            {
                int stripe = (col * 16 + px + py) % 8;
                if (stripe < 3)
                {
                    fb.SetPixel(x + px, y + py, EgaColorIndex.BrightWhite);
                }
            }
        }
    }

    private static void DrawHazardSpikes(FrameBuffer fb, int x, int y)
    {
        // Crisp triangular steel floor spikes with sharp white edges and red blood tips
        fb.DrawHLine(x, y + 15, 16, EgaColorIndex.Black);
        fb.DrawHLine(x, y + 14, 16, EgaColorIndex.DarkGray);

        // Spike 1 (X: 1..7)
        for (int h = 0; h < 11; h++)
        {
            int sy = y + 13 - h;
            int halfW = (h * 3) / 11;
            int sx = x + 4;
            // Left edge
            fb.SetPixel(sx - halfW, sy, EgaColorIndex.BrightWhite);
            // Center
            if (halfW > 0)
            {
                fb.FillRect(sx - halfW + 1, sy, halfW * 2 - 1, 1, EgaColorIndex.LightGray);
            }
            // Right edge
            fb.SetPixel(sx + halfW, sy, EgaColorIndex.Black);
        }
        fb.SetPixel(x + 4, y + 2, EgaColorIndex.BrightRed);
        fb.SetPixel(x + 4, y + 3, EgaColorIndex.BrightWhite);

        // Spike 2 (X: 9..15)
        for (int h = 0; h < 11; h++)
        {
            int sy = y + 13 - h;
            int halfW = (h * 3) / 11;
            int sx = x + 12;
            fb.SetPixel(sx - halfW, sy, EgaColorIndex.BrightWhite);
            if (halfW > 0)
            {
                fb.FillRect(sx - halfW + 1, sy, halfW * 2 - 1, 1, EgaColorIndex.LightGray);
            }
            fb.SetPixel(sx + halfW, sy, EgaColorIndex.Black);
        }
        fb.SetPixel(x + 12, y + 2, EgaColorIndex.BrightRed);
        fb.SetPixel(x + 12, y + 3, EgaColorIndex.BrightWhite);
    }

    private static void DrawBorderBulkhead(FrameBuffer fb, int x, int y)
    {
        // Duke 1 arcade bezel border block with industrial corner screw bolts (+)
        fb.FillRect(x, y, 16, 16, EgaColorIndex.LightGray);
        fb.DrawRect(x, y, 16, 16, EgaColorIndex.Black);
        fb.DrawHLine(x + 1, y + 1, 14, EgaColorIndex.BrightWhite);
        fb.DrawVLine(x + 1, y + 1, 14, EgaColorIndex.BrightWhite);
        fb.DrawHLine(x + 1, y + 14, 14, EgaColorIndex.DarkGray);
        fb.DrawVLine(x + 14, y + 1, 14, EgaColorIndex.DarkGray);

        // Recessed inner metal square
        fb.DrawRect(x + 3, y + 3, 10, 10, EgaColorIndex.DarkGray);
        fb.DrawRect(x + 4, y + 4, 8, 8, EgaColorIndex.Black);

        // Corner screw heads (+)
        fb.SetPixel(x + 2, y + 2, EgaColorIndex.Yellow);
        fb.SetPixel(x + 13, y + 2, EgaColorIndex.Yellow);
        fb.SetPixel(x + 2, y + 13, EgaColorIndex.Yellow);
        fb.SetPixel(x + 13, y + 13, EgaColorIndex.Yellow);
    }

    private static void DrawLadder(FrameBuffer fb, int x, int y)
    {
        // Crisp steel climbing ladder
        // Left rail (solid White with Black shadow)
        fb.DrawVLine(x + 2, y, 16, EgaColorIndex.BrightWhite);
        fb.DrawVLine(x + 3, y, 16, EgaColorIndex.LightGray);
        fb.DrawVLine(x + 4, y, 16, EgaColorIndex.Black);

        // Right rail
        fb.DrawVLine(x + 11, y, 16, EgaColorIndex.BrightWhite);
        fb.DrawVLine(x + 12, y, 16, EgaColorIndex.LightGray);
        fb.DrawVLine(x + 13, y, 16, EgaColorIndex.Black);

        // 4 Horizontal rungs
        for (int ry = 2; ry < 16; ry += 4)
        {
            fb.DrawHLine(x + 4, y + ry, 8, EgaColorIndex.BrightWhite);
            fb.DrawHLine(x + 4, y + ry + 1, 8, EgaColorIndex.Black);
        }
    }

    private static void DrawSupplyCrate(FrameBuffer fb, int x, int y)
    {
        // Solid wooden cargo crate with black outline and yellow cross-brace
        fb.FillRect(x, y, 16, 16, EgaColorIndex.Brown);
        fb.DrawRect(x, y, 16, 16, EgaColorIndex.Black);

        // Horizontal plank boards
        fb.DrawHLine(x + 1, y + 5, 14, EgaColorIndex.Black);
        fb.DrawHLine(x + 1, y + 10, 14, EgaColorIndex.Black);

        // Yellow diagonal cross-straps
        for (int d = 2; d < 14; d++)
        {
            fb.SetPixel(x + d, y + d, EgaColorIndex.Yellow);
            fb.SetPixel(x + (15 - d), y + d, EgaColorIndex.Yellow);
        }

        // Metal corner brackets
        fb.FillRect(x + 1, y + 1, 3, 3, EgaColorIndex.LightGray);
        fb.FillRect(x + 12, y + 1, 3, 3, EgaColorIndex.LightGray);
        fb.FillRect(x + 1, y + 12, 3, 3, EgaColorIndex.LightGray);
        fb.FillRect(x + 12, y + 12, 3, 3, EgaColorIndex.LightGray);
        fb.SetPixel(x + 2, y + 2, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 13, y + 2, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 2, y + 13, EgaColorIndex.BrightWhite);
        fb.SetPixel(x + 13, y + 13, EgaColorIndex.BrightWhite);
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

        if (player.HasSpeedDash && (player.State == PlayerState.Running || player.State == PlayerState.Walking))
        {
            int trailX = (player.Facing == Direction.Right) ? px - 3 : px + 3;
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
        // Duke Nukem 1 climbing rear view
        int step = ((int)(player.AnimTimer * 8) % 2) == 0 ? 1 : -1;

        // 1. Bright Blonde flat-top hair (solid Yellow, crisp outline)
        fb.FillRect(px + 3, py, 8, 4, EgaColorIndex.Yellow);
        fb.DrawHLine(px + 4, py, 6, EgaColorIndex.BrightWhite); // Top sheen
        fb.DrawRect(px + 3, py, 8, 4, EgaColorIndex.Black);

        // 2. Muscular back in BrightRed Tank Top
        fb.FillRect(px + 2, py + 4, 10, 9, EgaColorIndex.BrightRed);
        fb.DrawRect(px + 2, py + 4, 10, 9, EgaColorIndex.Black);
        fb.DrawVLine(px + 6, py + 5, 6, EgaColorIndex.Red); // Spine contour

        // 3. Bare muscular arms gripping ladder rungs
        int leftArmY = py + 4 - (step * 2);
        int rightArmY = py + 4 + (step * 2);
        fb.FillRect(px, leftArmY, 3, 5, EgaColorIndex.BrightRed);
        fb.DrawRect(px, leftArmY, 3, 5, EgaColorIndex.Black);
        fb.FillRect(px + 11, rightArmY, 3, 5, EgaColorIndex.BrightRed);
        fb.DrawRect(px + 11, rightArmY, 3, 5, EgaColorIndex.Black);

        // 4. Tactical Belt with Gold Buckle
        fb.FillRect(px + 2, py + 13, 10, 2, EgaColorIndex.Black);
        fb.SetPixel(px + 6, py + 13, EgaColorIndex.Yellow);
        fb.SetPixel(px + 7, py + 13, EgaColorIndex.BrightWhite);

        // 5. Solid BrightBlue Pants
        fb.FillRect(px + 3, py + 15, 8, 5, EgaColorIndex.BrightBlue);
        fb.DrawRect(px + 3, py + 15, 8, 5, EgaColorIndex.Black);

        // 6. Combat boots gripping rungs
        int leftBootY = py + 20 + (step * 2);
        int rightBootY = py + 20 - (step * 2);
        fb.FillRect(px + 2, leftBootY, 4, 3, EgaColorIndex.DarkGray);
        fb.DrawRect(px + 2, leftBootY, 4, 3, EgaColorIndex.Black);
        fb.FillRect(px + 8, rightBootY, 4, 3, EgaColorIndex.DarkGray);
        fb.DrawRect(px + 8, rightBootY, 4, 3, EgaColorIndex.Black);
    }

    private static void DrawPlayerAction(FrameBuffer fb, PlayerEntity player, int px, int py)
    {
        // Exact Duke Nukem 1 sprite layout:
        // Solid yellow flat-top hair, tan face with smirk, bright red tank top,
        // bare muscular arms, chrome blaster with ribbed barrel, bright blue pants, black outlines!
        bool flip = player.Facing == Direction.Left;

        int legStep = 0;
        int gunBob = 0;

        if (player.State == PlayerState.Walking || player.State == PlayerState.Running)
        {
            int cadence = (player.State == PlayerState.Running) ? 14 : 9;
            int phase = (int)(player.AnimTimer * cadence) % 4;
            legStep = (phase == 0) ? 2 : (phase == 2) ? -2 : 0;
            gunBob = (phase == 1 || phase == 3) ? 1 : 0;
        }
        else if (player.State == PlayerState.Jumping || player.State == PlayerState.Falling)
        {
            legStep = -1;
            gunBob = -1;
        }

        // 1. Signature Blonde Flat-top Hair (solid Yellow with black outline & white crown glint)
        int hairX = flip ? px + 3 : px + 2;
        fb.FillRect(hairX, py, 8, 4, EgaColorIndex.Yellow);
        fb.DrawHLine(hairX + 1, py, 6, EgaColorIndex.BrightWhite);
        fb.DrawRect(hairX, py, 8, 4, EgaColorIndex.Black);

        // 2. Face (authentic Duke 1 pinkish flesh in BrightRed, white eye & black pupil)
        int faceX = hairX;
        fb.FillRect(faceX, py + 4, 8, 4, EgaColorIndex.BrightRed);
        fb.DrawRect(faceX, py + 4, 8, 4, EgaColorIndex.Black);

        // Crisp eye: White sclera with Black pupil looking forward
        int eyeX = flip ? faceX + 1 : faceX + 4;
        fb.FillRect(eyeX, py + 4, 3, 2, EgaColorIndex.BrightWhite);
        fb.SetPixel(flip ? eyeX : eyeX + 2, py + 4, EgaColorIndex.Black); // Pupil

        // Nose and mouth / smirk
        fb.SetPixel(flip ? faceX : faceX + 7, py + 5, EgaColorIndex.Black); // Nose tip
        fb.SetPixel(flip ? faceX + 2 : faceX + 5, py + 6, EgaColorIndex.Black); // Smirk

        // 3. BrightMagenta / BrightRed Tank Top (exact Duke 1 vest from screenshot)
        fb.FillRect(px + 3, py + 8, 8, 6, EgaColorIndex.BrightMagenta);
        fb.DrawRect(px + 3, py + 8, 8, 6, EgaColorIndex.Black);
        fb.FillRect(flip ? px + 4 : px + 7, py + 8, 2, 2, EgaColorIndex.BrightRed); // Muscle shadow

        // Bare muscular arms in BrightRed flesh tone
        int armX = flip ? px + 9 : px + 1;
        fb.FillRect(armX, py + 8, 3, 6, EgaColorIndex.BrightRed);
        fb.DrawRect(armX, py + 8, 3, 6, EgaColorIndex.Black);

        // 4. Chrome Combat Blaster with Ribbed Barrel (exact Duke 1 gun from screenshot)
        int gunY = py + 10 + gunBob;
        if (flip)
        {
            // Aimed Left
            fb.FillRect(px - 5, gunY, 7, 3, EgaColorIndex.BrightWhite);
            fb.DrawRect(px - 5, gunY, 7, 3, EgaColorIndex.Black);
            // Barrel ribs (black vertical notches)
            fb.SetPixel(px - 3, gunY + 1, EgaColorIndex.DarkGray);
            fb.SetPixel(px - 1, gunY + 1, EgaColorIndex.DarkGray);
            fb.FillRect(px + 1, gunY + 2, 2, 3, EgaColorIndex.Black); // Grip
            fb.SetPixel(px - 6, gunY + 1, EgaColorIndex.BrightCyan);  // Muzzle spark
        }
        else
        {
            // Aimed Right
            fb.FillRect(px + 10, gunY, 7, 3, EgaColorIndex.BrightWhite);
            fb.DrawRect(px + 10, gunY, 7, 3, EgaColorIndex.Black);
            // Barrel ribs (black vertical notches)
            fb.SetPixel(px + 12, gunY + 1, EgaColorIndex.DarkGray);
            fb.SetPixel(px + 14, gunY + 1, EgaColorIndex.DarkGray);
            fb.FillRect(px + 10, gunY + 2, 2, 3, EgaColorIndex.Black); // Grip
            fb.SetPixel(px + 17, gunY + 1, EgaColorIndex.BrightCyan);  // Muzzle spark
        }

        // 5. Black Utility Belt with Golden Buckle
        fb.FillRect(px + 3, py + 14, 8, 2, EgaColorIndex.Black);
        int buckleX = flip ? px + 6 : px + 5;
        fb.SetPixel(buckleX, py + 14, EgaColorIndex.Yellow);
        fb.SetPixel(buckleX + 1, py + 14, EgaColorIndex.BrightWhite);

        // 6. Solid BrightBlue Pants
        fb.FillRect(px + 3, py + 16, 8, 4, EgaColorIndex.BrightBlue);
        fb.DrawRect(px + 3, py + 16, 8, 4, EgaColorIndex.Black);
        fb.DrawVLine(px + 6, py + 16, 4, EgaColorIndex.Blue); // Center crease

        // 7. Combat Boots (Solid DarkGray with Black soles)
        int bootLeftY = py + 20 + legStep;
        int bootRightY = py + 20 - legStep;
        // Left boot
        fb.FillRect(px + 2, bootLeftY, 4, 4, EgaColorIndex.BrightBlue);
        fb.DrawRect(px + 2, bootLeftY, 4, 4, EgaColorIndex.Black);
        fb.DrawHLine(px + 2, bootLeftY + 3, 4, EgaColorIndex.Black);
        // Right boot
        fb.FillRect(px + 8, bootRightY, 4, 4, EgaColorIndex.BrightBlue);
        fb.DrawRect(px + 8, bootRightY, 4, 4, EgaColorIndex.Black);
        fb.DrawHLine(px + 8, bootRightY + 3, 4, EgaColorIndex.Black);
    }

    public void DrawProjectile(FrameBuffer fb, in Projectile projectile, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 184) return;

        if (projectile.FromPlayer)
        {
            // Crisp high-energy laser bolt: solid BrightWhite core with BrightCyan outer outline
            fb.FillRect(screenX, screenY, 8, 3, EgaColorIndex.BrightCyan);
            fb.DrawHLine(screenX + 1, screenY + 1, 6, EgaColorIndex.BrightWhite);
            fb.DrawRect(screenX, screenY, 8, 3, EgaColorIndex.Black);
        }
        else
        {
            // Searing red enemy plasma orb
            fb.FillRect(screenX, screenY, 6, 6, EgaColorIndex.BrightRed);
            fb.FillRect(screenX + 1, screenY + 1, 4, 4, EgaColorIndex.Yellow);
            fb.FillRect(screenX + 2, screenY + 2, 2, 2, EgaColorIndex.BrightWhite);
            fb.DrawRect(screenX, screenY, 6, 6, EgaColorIndex.Black);
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
        // Exact Ground Patrol Robot from Duke Nukem 1 screenshot:
        // Dome head with red horizontal eye visor slit, striped spring neck, black/yellow roller pedestal!
        var headCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray;

        // 1. Dome Head (LightGray with Black outline)
        fb.FillRect(mx + 2, my + 1, 10, 5, headCol);
        fb.DrawRect(mx + 2, my + 1, 10, 5, EgaColorIndex.Black);
        // Horizontal glowing Red visor slit
        fb.DrawHLine(mx + 3, my + 3, 8, flash ? EgaColorIndex.BrightWhite : EgaColorIndex.BrightRed);
        fb.SetPixel(mx + 6, my + 3, EgaColorIndex.BrightWhite); // Glint

        // 2. Striped Spring Neck (horizontal Yellow and Black bands)
        fb.DrawHLine(mx + 4, my + 6, 6, EgaColorIndex.Yellow);
        fb.DrawHLine(mx + 4, my + 7, 6, EgaColorIndex.Black);
        fb.DrawHLine(mx + 4, my + 8, 6, EgaColorIndex.Yellow);
        fb.DrawHLine(mx + 4, my + 9, 6, EgaColorIndex.Black);

        // 3. Roller Base Pedestal (Black disc with LightGray trim)
        fb.FillRect(mx + 2, my + 10, 10, 4, EgaColorIndex.Black);
        fb.DrawHLine(mx + 3, my + 10, 8, EgaColorIndex.LightGray);
        fb.DrawHLine(mx + 1, my + 13, 12, EgaColorIndex.LightGray);
        fb.DrawRect(mx + 2, my + 10, 10, 4, EgaColorIndex.Black);
    }

    private static void DrawHoverDrone(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // Aerial surveillance drone with cyan visor and pulsing yellow thruster
        var bodyCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray;

        // Aerodynamic canopy
        fb.FillRect(mx + 2, my + 2, 10, 6, bodyCol);
        fb.DrawRect(mx + 2, my + 2, 10, 6, EgaColorIndex.Black);

        // Dual cyan optical lenses
        fb.FillRect(mx + 4, my + 4, 2, 2, EgaColorIndex.BrightCyan);
        fb.FillRect(mx + 8, my + 4, 2, 2, EgaColorIndex.BrightCyan);

        // Side wings
        fb.FillRect(mx, my + 4, 2, 2, EgaColorIndex.DarkGray);
        fb.FillRect(mx + 12, my + 4, 2, 2, EgaColorIndex.DarkGray);

        // Pulsing yellow/white ion thruster flame
        bool flameHigh = ((int)(monster.AnimTimer * 12) % 2) == 0;
        fb.FillRect(mx + 5, my + 8, 4, flameHigh ? 4 : 2, EgaColorIndex.Yellow);
        fb.DrawHLine(mx + 6, my + 8, 2, EgaColorIndex.BrightWhite);
    }

    private static void DrawCeilingTurret(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // Security camera / ceiling turret (exact design from Duke 1 screenshot!)
        // White/LightGray camera housing angled downward, cyan lens, black bracket mount
        var camCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray;

        // Ceiling mounting bracket
        fb.FillRect(mx + 3, my, 8, 3, EgaColorIndex.Black);
        fb.DrawHLine(mx + 2, my, 10, EgaColorIndex.DarkGray);

        // Angled camera cylinder pointing down-forward
        fb.FillRect(mx + 4, my + 3, 6, 8, camCol);
        fb.DrawRect(mx + 4, my + 3, 6, 8, EgaColorIndex.Black);

        // Glowing Cyan camera lens / optic
        fb.FillRect(mx + 5, my + 9, 4, 3, EgaColorIndex.BrightCyan);
        fb.DrawRect(mx + 5, my + 9, 4, 3, EgaColorIndex.Black);
        fb.SetPixel(mx + 6, my + 10, EgaColorIndex.BrightWhite);
    }

    private static void DrawMechaHound(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // Fast quadruped robo-hound
        var bodyCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray;
        bool flip = monster.Facing == Direction.Left;

        // Torso
        fb.FillRect(mx + 6, my + 3, 12, 6, bodyCol);
        fb.DrawRect(mx + 6, my + 3, 12, 6, EgaColorIndex.Black);

        // Head with red optic slit
        int headX = flip ? mx : mx + 16;
        fb.FillRect(headX, my + 2, 8, 6, bodyCol);
        fb.DrawRect(headX, my + 2, 8, 6, EgaColorIndex.Black);
        fb.SetPixel(flip ? headX + 1 : headX + 6, my + 3, EgaColorIndex.BrightRed);

        // Running legs
        int step = ((int)(monster.AnimTimer * 12) % 2 == 0) ? 2 : -2;
        int fLeg = flip ? mx + 3 : mx + 17;
        int rLeg = flip ? mx + 13 : mx + 7;
        fb.FillRect(fLeg, my + 9 + step, 2, 5, EgaColorIndex.DarkGray);
        fb.FillRect(rLeg, my + 9 - step, 2, 5, EgaColorIndex.DarkGray);
    }

    private static void DrawHeavyGuard(FrameBuffer fb, MonsterEntity monster, int mx, int my, bool flash)
    {
        // Heavy cyborg enforcer with shoulder cannon and red visor
        var bodyCol = flash ? EgaColorIndex.BrightWhite : EgaColorIndex.DarkGray;
        bool flip = monster.Facing == Direction.Left;

        // Helmet
        fb.FillRect(mx + 3, my + 1, 8, 6, EgaColorIndex.LightGray);
        fb.DrawRect(mx + 3, my + 1, 8, 6, EgaColorIndex.Black);
        fb.DrawHLine(mx + 4, my + 3, 6, EgaColorIndex.BrightRed); // Visor

        // Pauldron with Yellow hazard stripe
        fb.FillRect(mx + 1, my + 7, 12, 4, EgaColorIndex.Yellow);
        fb.DrawRect(mx + 1, my + 7, 12, 4, EgaColorIndex.Black);

        // Torso
        fb.FillRect(mx + 2, my + 11, 10, 8, bodyCol);
        fb.DrawRect(mx + 2, my + 11, 10, 8, EgaColorIndex.Black);

        // Gatling Cannon arm
        int cannonX = flip ? mx - 4 : mx + 10;
        fb.FillRect(cannonX, my + 10, 8, 5, EgaColorIndex.LightGray);
        fb.DrawRect(cannonX, my + 10, 8, 5, EgaColorIndex.Black);

        // Legs
        int stride = ((int)(monster.AnimTimer * 6) % 2 == 0) ? 1 : -1;
        fb.FillRect(mx + 2, my + 19 + stride, 4, 7, EgaColorIndex.LightGray);
        fb.DrawRect(mx + 2, my + 19 + stride, 4, 7, EgaColorIndex.Black);
        fb.FillRect(mx + 8, my + 19 - stride, 4, 7, EgaColorIndex.LightGray);
        fb.DrawRect(mx + 8, my + 19 - stride, 4, 7, EgaColorIndex.Black);
    }

    public void DrawPickup(FrameBuffer fb, in PickupItem pickup, int screenX, int screenY)
    {
        if (screenX < -16 || screenX > 320 || screenY < -16 || screenY > 184) return;

        int px = screenX;
        int py = screenY;

        switch (pickup.Type)
        {
            case PickupType.BlueGem:
                // Solid BrightCyan Gem with bold Black outline and White facet
                fb.FillRect(px + 3, py + 2, 8, 8, EgaColorIndex.BrightCyan);
                fb.DrawRect(px + 3, py + 2, 8, 8, EgaColorIndex.Black);
                fb.FillRect(px + 5, py + 3, 3, 3, EgaColorIndex.BrightWhite);
                break;

            case PickupType.RedRuby:
                // Solid BrightRed Ruby
                fb.FillRect(px + 3, py + 2, 8, 8, EgaColorIndex.BrightRed);
                fb.DrawRect(px + 3, py + 2, 8, 8, EgaColorIndex.Black);
                fb.FillRect(px + 5, py + 4, 3, 3, EgaColorIndex.Yellow);
                break;

            case PickupType.GoldCartridge:
                // White & Cyan Power Capsule (exact item from Duke 1 screenshot bottom right!)
                fb.FillRect(px + 2, py + 2, 9, 8, EgaColorIndex.BrightWhite);
                fb.DrawRect(px + 2, py + 2, 9, 8, EgaColorIndex.Black);
                // Cyan side coil cable
                fb.DrawVLine(px + 11, py + 3, 6, EgaColorIndex.BrightCyan);
                fb.SetPixel(px + 10, py + 2, EgaColorIndex.BrightCyan);
                fb.SetPixel(px + 10, py + 9, EgaColorIndex.BrightCyan);
                break;

            case PickupType.MedKit:
                // White First Aid Kit with BrightRed Cross
                fb.FillRect(px + 2, py + 3, 10, 8, EgaColorIndex.BrightWhite);
                fb.DrawRect(px + 2, py + 3, 10, 8, EgaColorIndex.Black);
                // Red cross
                fb.FillRect(px + 6, py + 5, 2, 4, EgaColorIndex.BrightRed);
                fb.FillRect(px + 5, py + 6, 4, 2, EgaColorIndex.BrightRed);
                break;

            case PickupType.RapidFire:
                DrawBadge(fb, px, py, "R", EgaColorIndex.BrightRed, EgaColorIndex.Yellow);
                break;

            case PickupType.HighJumpBoots:
                DrawBadge(fb, px, py, "J", EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite);
                break;

            case PickupType.SpeedDash:
                DrawBadge(fb, px, py, "S", EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite);
                break;
        }
    }

    private static void DrawBadge(FrameBuffer fb, int px, int py, string letter, EgaColorIndex bgCol, EgaColorIndex textCol)
    {
        // Solid arcade badge with bold black outline
        fb.FillRect(px + 1, py + 1, 12, 12, bgCol);
        fb.DrawRect(px + 1, py + 1, 12, 12, EgaColorIndex.Black);
        fb.DrawHLine(px + 2, py + 2, 10, EgaColorIndex.BrightWhite);
        fb.DrawText(px + 4, py + 3, letter, textCol);
    }

    public void DrawHud(FrameBuffer fb, PlayerEntity player, string seed, int fps)
    {
        // Authentic Duke Nukem 1 style frame & HUD:
        // Top status console (320x16):
        // Solid LightGray frame with Black border and corner bolts
        fb.FillRect(0, 0, 320, 16, EgaColorIndex.LightGray);
        fb.DrawRect(0, 0, 320, 16, EgaColorIndex.Black);
        fb.DrawHLine(1, 1, 318, EgaColorIndex.BrightWhite);

        // SCORE Display: Black inset box with White/Yellow digits
        fb.FillRect(4, 2, 94, 12, EgaColorIndex.Black);
        fb.DrawRect(4, 2, 94, 12, EgaColorIndex.DarkGray);
        fb.DrawText(6, 4, "SCORE:", EgaColorIndex.Yellow);
        fb.DrawInt(56, 4, player.Score, 6, EgaColorIndex.BrightWhite);

        // HEALTH Display: Vertical green rectangular bars (exact Duke 1 style!)
        fb.FillRect(102, 2, 80, 12, EgaColorIndex.Black);
        fb.DrawRect(102, 2, 80, 12, EgaColorIndex.DarkGray);
        fb.DrawText(105, 4, "HP:", EgaColorIndex.BrightRed);
        for (int i = 0; i < PlayerEntity.MaxHealth; i++)
        {
            int cellX = 130 + i * 9;
            if (i < player.Health)
            {
                // Solid green health bar with white highlight
                fb.FillRect(cellX, 4, 7, 8, EgaColorIndex.BrightGreen);
                fb.DrawRect(cellX, 4, 7, 8, EgaColorIndex.Black);
                fb.DrawHLine(cellX + 1, 5, 5, EgaColorIndex.BrightWhite);
            }
            else
            {
                fb.DrawRect(cellX, 4, 7, 8, EgaColorIndex.Red);
            }
        }

        // Active Powerup Badges
        int badgeX = 186;
        if (player.HasRapidFire)
        {
            DrawBadge(fb, badgeX, 1, "R", EgaColorIndex.BrightRed, EgaColorIndex.Yellow);
            badgeX += 13;
        }
        if (player.HasHighJumpBoots)
        {
            DrawBadge(fb, badgeX, 1, "J", EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite);
            badgeX += 13;
        }
        if (player.HasSpeedDash)
        {
            DrawBadge(fb, badgeX, 1, "S", EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite);
            badgeX += 13;
        }

        // SEED Display
        fb.FillRect(228, 2, 54, 12, EgaColorIndex.Black);
        fb.DrawRect(228, 2, 54, 12, EgaColorIndex.DarkGray);
        string displaySeed = seed.Length > 6 ? seed[..6] : seed;
        fb.DrawText(231, 4, displaySeed, EgaColorIndex.BrightCyan);

        // Version badge in corner
        fb.DrawText(286, 4, "v" + GameVersion.Version, EgaColorIndex.Yellow);

        // Bottom Controls Strip (320x16, Y: 184..200)
        fb.FillRect(0, 184, 320, 16, EgaColorIndex.LightGray);
        fb.DrawRect(0, 184, 320, 16, EgaColorIndex.Black);
        fb.DrawHLine(1, 185, 318, EgaColorIndex.BrightWhite);

        // Center help pill: PRESS F1 FOR HELP (Duke 1 style) or controls prompt
        fb.FillRect(4, 186, 312, 12, EgaColorIndex.Black);
        fb.DrawText(6, 188, "ARROWS/WASD:MOVE  CTRL/SPC:JUMP  ALT/X:SHOOT  SHIFT:RUN  DN+JMP:DROP", EgaColorIndex.BrightWhite);
    }
}
