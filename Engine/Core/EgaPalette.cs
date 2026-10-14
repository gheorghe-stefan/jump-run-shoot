namespace JumpRunShoot.Engine.Core;

public enum EgaColorIndex : byte
{
    Black = 0,
    Blue = 1,
    Green = 2,
    Cyan = 3,
    Red = 4,
    Magenta = 5,
    Brown = 6,
    LightGray = 7,
    DarkGray = 8,
    BrightBlue = 9,
    BrightGreen = 10,
    BrightCyan = 11,
    BrightRed = 12,
    BrightMagenta = 13,
    Yellow = 14,
    BrightWhite = 15
}

public static class EgaPalette
{
    // Little-endian packed RGBA: (A << 24 | B << 16 | G << 8 | R)
    public static readonly uint[] Rgba = new uint[16]
    {
        Pack(0x00, 0x00, 0x00), // 0: Black
        Pack(0x00, 0x00, 0xAA), // 1: Blue
        Pack(0x00, 0xAA, 0x00), // 2: Green
        Pack(0x00, 0xAA, 0xAA), // 3: Cyan
        Pack(0xAA, 0x00, 0x00), // 4: Red
        Pack(0xAA, 0x00, 0xAA), // 5: Magenta
        Pack(0xAA, 0x55, 0x00), // 6: Brown
        Pack(0xAA, 0xAA, 0xAA), // 7: Light Gray
        Pack(0x55, 0x55, 0x55), // 8: Dark Gray
        Pack(0x55, 0x55, 0xFF), // 9: Bright Blue
        Pack(0x55, 0xFF, 0x55), // 10: Bright Green
        Pack(0x55, 0xFF, 0xFF), // 11: Bright Cyan
        Pack(0xFF, 0x55, 0x55), // 12: Bright Red
        Pack(0xFF, 0x55, 0xFF), // 13: Bright Magenta
        Pack(0xFF, 0xFF, 0x55), // 14: Yellow
        Pack(0xFF, 0xFF, 0xFF)  // 15: Bright White
    };

    public static readonly string[] HexColors = new string[16]
    {
        "#000000", "#0000AA", "#00AA00", "#00AAAA",
        "#AA0000", "#AA00AA", "#AA5500", "#AAAAAA",
        "#555555", "#5555FF", "#55FF55", "#55FFFF",
        "#FF5555", "#FF55FF", "#FFFF55", "#FFFFFF"
    };

    public static uint GetRgba(EgaColorIndex color) => Rgba[(int)color];

    public static string GetHex(EgaColorIndex color) => HexColors[(int)color];

    private static uint Pack(byte r, byte g, byte b, byte a = 0xFF)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }
}
