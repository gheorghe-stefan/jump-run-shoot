using System;
using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Represents a static, immutable 16-color EGA bitmap sprite with transparency.
/// Pixel values 0..15 map to <see cref="EgaColorIndex"/>; 0xFF represents transparent.
/// </summary>
public sealed class EgaSprite
{
    public const byte Transparent = 0xFF;

    public int Width { get; }
    public int Height { get; }
    public int OffsetX { get; }
    public int OffsetY { get; }
    public byte[] Pixels { get; }

    public EgaSprite(int width, int height, int offsetX = 0, int offsetY = 0)
    {
        Width = width;
        Height = height;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Pixels = new byte[width * height];
        Array.Fill(Pixels, Transparent);
    }

    public EgaSprite(int width, int height, byte[] pixels, int offsetX = 0, int offsetY = 0)
    {
        if (pixels.Length != width * height)
            throw new ArgumentException($"Pixel array length ({pixels.Length}) must match width*height ({width * height}).");

        Width = width;
        Height = height;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Pixels = pixels;
    }

    /// <summary>
    /// Bakes a static sprite using drawing primitives once at theme initialization time.
    /// </summary>
    public static EgaSprite Bake(int width, int height, Action<SpriteBuilder> buildAction, int offsetX = 0, int offsetY = 0)
    {
        var builder = new SpriteBuilder(width, height);
        buildAction(builder);
        return new EgaSprite(width, height, builder.GetPixels(), offsetX, offsetY);
    }

    /// <summary>
    /// Creates a new static sprite by remapping specific EGA colors from this sprite.
    /// </summary>
    public EgaSprite RemapColors(params (EgaColorIndex From, EgaColorIndex To)[] mappings)
    {
        byte[] newPixels = new byte[Pixels.Length];
        Array.Copy(Pixels, newPixels, Pixels.Length);

        for (int i = 0; i < newPixels.Length; i++)
        {
            byte p = newPixels[i];
            if (p == Transparent) continue;

            for (int m = 0; m < mappings.Length; m++)
            {
                if (p == (byte)mappings[m].From)
                {
                    newPixels[i] = (byte)mappings[m].To;
                    break;
                }
            }
        }

        return new EgaSprite(Width, Height, newPixels, OffsetX, OffsetY);
    }
}

/// <summary>
/// Offscreen canvas used strictly at startup to bake static <see cref="EgaSprite"/> bitmaps.
/// </summary>
public sealed class SpriteBuilder
{
    private readonly int _width;
    private readonly int _height;
    private readonly byte[] _pixels;

    public SpriteBuilder(int width, int height)
    {
        _width = width;
        _height = height;
        _pixels = new byte[width * height];
        Array.Fill(_pixels, EgaSprite.Transparent);
    }

    public byte[] GetPixels() => _pixels;

    public void SetPixel(int x, int y, EgaColorIndex color)
    {
        if ((uint)x < (uint)_width && (uint)y < (uint)_height)
        {
            _pixels[y * _width + x] = (byte)color;
        }
    }

    public void FillRect(int x, int y, int w, int h, EgaColorIndex color)
    {
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(_width, x + w);
        int y1 = Math.Min(_height, y + h);

        byte col = (byte)color;
        for (int r = y0; r < y1; r++)
        {
            int rowStart = r * _width;
            for (int c = x0; c < x1; c++)
            {
                _pixels[rowStart + c] = col;
            }
        }
    }

    public void DrawHLine(int x, int y, int w, EgaColorIndex color)
    {
        if ((uint)y >= (uint)_height || w <= 0) return;
        int x0 = Math.Max(0, x);
        int x1 = Math.Min(_width, x + w);
        byte col = (byte)color;
        int rowStart = y * _width;
        for (int c = x0; c < x1; c++)
        {
            _pixels[rowStart + c] = col;
        }
    }

    public void DrawVLine(int x, int y, int h, EgaColorIndex color)
    {
        if ((uint)x >= (uint)_width || h <= 0) return;
        int y0 = Math.Max(0, y);
        int y1 = Math.Min(_height, y + h);
        byte col = (byte)color;
        for (int r = y0; r < y1; r++)
        {
            _pixels[r * _width + x] = col;
        }
    }

    public void DrawRect(int x, int y, int w, int h, EgaColorIndex color)
    {
        if (w <= 0 || h <= 0) return;
        DrawHLine(x, y, w, color);
        DrawHLine(x, y + h - 1, w, color);
        DrawVLine(x, y, h, color);
        DrawVLine(x + w - 1, y, h, color);
    }

    public void DrawText(int x, int y, string text, EgaColorIndex color)
    {
        if (string.IsNullOrEmpty(text)) return;
        int curX = x;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c < 128)
            {
                byte[] glyph = EgaFont.GlyphTable[c];
                for (int row = 0; row < 8; row++)
                {
                    byte rowBits = glyph[row];
                    if (rowBits == 0) continue;
                    for (int col = 0; col < 8; col++)
                    {
                        if ((rowBits & (0x80 >> col)) != 0)
                        {
                            SetPixel(curX + col, y + row, color);
                        }
                    }
                }
            }
            curX += 8;
        }
    }

    public void DrawSprite(EgaSprite sprite, int dstX, int dstY)
    {
        for (int sy = 0; sy < sprite.Height; sy++)
        {
            int py = dstY + sy;
            if ((uint)py >= (uint)_height) continue;

            for (int sx = 0; sx < sprite.Width; sx++)
            {
                byte p = sprite.Pixels[sy * sprite.Width + sx];
                if (p == EgaSprite.Transparent) continue;

                int px = dstX + sx;
                if ((uint)px < (uint)_width)
                {
                    _pixels[py * _width + px] = p;
                }
            }
        }
    }
}
