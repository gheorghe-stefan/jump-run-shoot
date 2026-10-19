using System;
using JumpRunShoot.Engine.Themes;

namespace JumpRunShoot.Engine.Core;

public sealed class FrameBuffer
{
    public const int Width = 320;
    public const int Height = 200;
    public const int TotalPixels = Width * Height;

    public uint[] Pixels { get; } = new uint[TotalPixels];

    public void Clear(uint color)
    {
        Array.Fill(Pixels, color);
    }

    public void Clear(EgaColorIndex color)
    {
        Clear(EgaPalette.GetRgba(color));
    }

    public void SetPixel(int x, int y, uint color)
    {
        if ((uint)x < Width && (uint)y < Height)
        {
            Pixels[y * Width + x] = color;
        }
    }

    public void SetPixel(int x, int y, EgaColorIndex color)
    {
        SetPixel(x, y, EgaPalette.GetRgba(color));
    }

    public void FillRect(int x, int y, int w, int h, uint color)
    {
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(Width, x + w);
        int y1 = Math.Min(Height, y + h);

        int span = x1 - x0;
        if (span <= 0 || y0 >= y1) return;

        for (int row = y0; row < y1; row++)
        {
            Array.Fill(Pixels, color, row * Width + x0, span);
        }
    }

    public void FillRect(int x, int y, int w, int h, EgaColorIndex color)
    {
        FillRect(x, y, w, h, EgaPalette.GetRgba(color));
    }

    public void DrawRect(int x, int y, int w, int h, uint color)
    {
        if (w <= 0 || h <= 0) return;
        DrawHLine(x, y, w, color);
        DrawHLine(x, y + h - 1, w, color);
        DrawVLine(x, y, h, color);
        DrawVLine(x + w - 1, y, h, color);
    }

    public void DrawRect(int x, int y, int w, int h, EgaColorIndex color)
    {
        DrawRect(x, y, w, h, EgaPalette.GetRgba(color));
    }

    public void DrawHLine(int x, int y, int w, uint color)
    {
        if (y < 0 || y >= Height || w <= 0) return;
        int x0 = Math.Max(0, x);
        int x1 = Math.Min(Width, x + w);
        int span = x1 - x0;
        if (span > 0)
        {
            Array.Fill(Pixels, color, y * Width + x0, span);
        }
    }

    public void DrawHLine(int x, int y, int w, EgaColorIndex color)
    {
        DrawHLine(x, y, w, EgaPalette.GetRgba(color));
    }

    public void DrawVLine(int x, int y, int h, uint color)
    {
        if (x < 0 || x >= Width || h <= 0) return;
        int y0 = Math.Max(0, y);
        int y1 = Math.Min(Height, y + h);
        for (int r = y0; r < y1; r++)
        {
            Pixels[r * Width + x] = color;
        }
    }

    public void DrawVLine(int x, int y, int h, EgaColorIndex color)
    {
        DrawVLine(x, y, h, EgaPalette.GetRgba(color));
    }

    /// <summary>
    /// Draws a 50% checkerboard dither pattern between two EGA colors to achieve smooth shading.
    /// </summary>
    public void DrawDitherRect(int x, int y, int w, int h, EgaColorIndex colorA, EgaColorIndex colorB)
    {
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(Width, x + w);
        int y1 = Math.Min(Height, y + h);

        uint rgbaA = EgaPalette.GetRgba(colorA);
        uint rgbaB = EgaPalette.GetRgba(colorB);

        for (int r = y0; r < y1; r++)
        {
            int rowOffset = r * Width;
            for (int c = x0; c < x1; c++)
            {
                Pixels[rowOffset + c] = (((c + r) & 1) == 0) ? rgbaA : rgbaB;
            }
        }
    }

    /// <summary>
    /// Draws a 5-pixel star sparkle (+ cross) centered at (cx, cy).
    /// </summary>
    public void DrawSparkle(int cx, int cy, EgaColorIndex color)
    {
        SetPixel(cx, cy, color);
        SetPixel(cx - 1, cy, color);
        SetPixel(cx + 1, cy, color);
        SetPixel(cx, cy - 1, color);
        SetPixel(cx, cy + 1, color);
    }

    public void DrawBeveledBox(int x, int y, int w, int h, uint fillColor, uint lightBevel, uint darkBevel)
    {
        FillRect(x, y, w, h, fillColor);
        DrawHLine(x, y, w, lightBevel);
        DrawVLine(x, y, h, lightBevel);
        DrawHLine(x, y + h - 1, w, darkBevel);
        DrawVLine(x + w - 1, y, h, darkBevel);
    }

    public void DrawBeveledBox(int x, int y, int w, int h, EgaColorIndex fillColor, EgaColorIndex lightBevel, EgaColorIndex darkBevel)
    {
        DrawBeveledBox(x, y, w, h, EgaPalette.GetRgba(fillColor), EgaPalette.GetRgba(lightBevel), EgaPalette.GetRgba(darkBevel));
    }

    public void DrawText(int x, int y, string text, uint color, int scale = 1)
    {
        if (string.IsNullOrEmpty(text)) return;
        int curX = x;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                curX = x;
                y += 8 * scale;
                continue;
            }
            if (curX + 8 * scale > 0 && curX < Width && y + 8 * scale > 0 && y < Height)
            {
                DrawChar(curX, y, c, color, scale);
            }
            curX += 8 * scale;
        }
    }

    public void DrawText(int x, int y, string text, EgaColorIndex color, int scale = 1)
    {
        DrawText(x, y, text, EgaPalette.GetRgba(color), scale);
    }

    /// <summary>
    /// Zero-allocation integer rendering. Formats numbers directly without string allocations.
    /// </summary>
    public void DrawInt(int x, int y, int value, int minDigits, uint color, int scale = 1)
    {
        Span<char> buffer = stackalloc char[16];
        int pos = buffer.Length;
        int v = Math.Abs(value);

        do
        {
            buffer[--pos] = (char)('0' + (v % 10));
            v /= 10;
        } while (v > 0);

        while (buffer.Length - pos < minDigits)
        {
            buffer[--pos] = '0';
        }

        if (value < 0)
        {
            buffer[--pos] = '-';
        }

        int curX = x;
        for (int i = pos; i < buffer.Length; i++)
        {
            if (curX + 8 * scale > 0 && curX < Width && y + 8 * scale > 0 && y < Height)
            {
                DrawChar(curX, y, buffer[i], color, scale);
            }
            curX += 8 * scale;
        }
    }

    public void DrawInt(int x, int y, int value, int minDigits, EgaColorIndex color, int scale = 1)
    {
        DrawInt(x, y, value, minDigits, EgaPalette.GetRgba(color), scale);
    }

    private void DrawChar(int startX, int startY, char c, uint color, int scale)
    {
        if (c >= 128) return;
        byte[] glyph = EgaFont.GlyphTable[c];

        for (int row = 0; row < 8; row++)
        {
            byte rowBits = glyph[row];
            if (rowBits == 0) continue;

            for (int col = 0; col < 8; col++)
            {
                if ((rowBits & (0x80 >> col)) != 0)
                {
                    if (scale == 1)
                    {
                        SetPixel(startX + col, startY + row, color);
                    }
                    else
                    {
                        FillRect(startX + col * scale, startY + row * scale, scale, scale, color);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Blits a static <see cref="EgaSprite"/> onto the framebuffer with support for
    /// transparency, horizontal mirroring, and automatic damage hit-flash.
    /// </summary>
    public void DrawSprite(EgaSprite sprite, int screenX, int screenY, bool flipX = false, bool hitFlash = false)
    {
        int drawX = screenX + sprite.OffsetX;
        int drawY = screenY + sprite.OffsetY;
        int w = sprite.Width;
        int h = sprite.Height;

        if (drawX + w <= 0 || drawX >= Width || drawY + h <= 0 || drawY >= Height) return;

        int startSy = Math.Max(0, -drawY);
        int endSy = Math.Min(h, Height - drawY);
        int startSx = Math.Max(0, -drawX);
        int endSx = Math.Min(w, Width - drawX);

        byte[] src = sprite.Pixels;
        uint whiteRgba = EgaPalette.GetRgba(EgaColorIndex.BrightWhite);

        for (int sy = startSy; sy < endSy; sy++)
        {
            int dstRowOffset = (drawY + sy) * Width;
            int srcRowOffset = sy * w;

            for (int sx = startSx; sx < endSx; sx++)
            {
                int sampleX = flipX ? (w - 1 - sx) : sx;
                byte colorIdx = src[srcRowOffset + sampleX];

                if (colorIdx == EgaSprite.Transparent) continue;

                if (hitFlash && colorIdx != (byte)EgaColorIndex.Black)
                {
                    Pixels[dstRowOffset + drawX + sx] = whiteRgba;
                }
                else
                {
                    Pixels[dstRowOffset + drawX + sx] = EgaPalette.GetRgba((EgaColorIndex)(colorIdx & 0x0F));
                }
            }
        }
    }
}
