using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Zero-dependency 32-bit RGBA PNG encoder/decoder and theme folder loader.
/// Allows themes to be stored on disk as standard .png image files under wwwroot/themes/{themeId}/
/// and loaded identically in both Blazor WebAssembly and unit tests.
/// </summary>
public static class EgaPngCodec
{
    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    /// <summary>
    /// Loads a full <see cref="ThemeAssetPack"/> from standard .png files in wwwroot/themes/{themeId}/.
    /// Checks local disk first (for live editing/tests), falling back to embedded assembly resources (for WASM).
    /// </summary>
    public static ThemeAssetPack LoadThemePack(string themeId, string displayName, bool zeroGravity = false)
    {
        EgaSprite Load(string fileName, int offsetX = 0, int offsetY = 0)
            => LoadThemeSprite(themeId, fileName, offsetX, offsetY);

        EgaSprite LoadOptional(string fileName, Func<EgaSprite> fallback)
        {
            try
            {
                return LoadThemeSprite(themeId, fileName);
            }
            catch
            {
                return fallback();
            }
        }

        return new ThemeAssetPack
        {
            Name = displayName,
            ZeroGravity = zeroGravity,

            // 1. 3-Layer Parallax Background Images
            BackgroundFar  = Load("bg_far.png"),
            BackgroundMid  = Load("bg_mid.png"),
            BackgroundNear = Load("bg_near.png"),

            // 2. Standard 16x16 Tiles
            SolidTile = Load("tile_solid.png"),
            OneWayTileVariants = new[]
            {
                Load("tile_oneway_0.png"),
                Load("tile_oneway_1.png")
            },
            HazardTile = Load("tile_hazard.png"),
            BorderTile = Load("tile_border.png"),
            LadderTile = Load("tile_ladder.png"),
            CrateTile  = Load("tile_crate.png"),

            // 3. Player Sprites (24x24 with OffsetX = -5, Climb 14x24)
            PlayerIdle = Load("player_idle.png", offsetX: -5),
            PlayerWalk = new[]
            {
                Load("player_walk_0.png", offsetX: -5),
                Load("player_walk_1.png", offsetX: -5),
                Load("player_walk_2.png", offsetX: -5),
                Load("player_walk_3.png", offsetX: -5)
            },
            PlayerJump = Load("player_jump.png", offsetX: -5),
            PlayerClimb = new[]
            {
                Load("player_climb_0.png"),
                Load("player_climb_1.png")
            },

            // 4. Monsters 1..5 (2 animation frames each)
            PatrolDroid = new[]
            {
                Load("monster_1_0.png"),
                Load("monster_1_1.png")
            },
            HoverDrone = new[]
            {
                Load("monster_2_0.png"),
                Load("monster_2_1.png")
            },
            CeilingTurret = new[]
            {
                Load("monster_3_0.png"),
                Load("monster_3_1.png")
            },
            MechaHound = new[]
            {
                Load("monster_4_0.png"),
                Load("monster_4_1.png")
            },
            HeavyGuard = new[]
            {
                Load("monster_5_0.png", offsetX: -4),
                Load("monster_5_1.png", offsetX: -4)
            },

            // 5. Projectiles
            PlayerProjectile  = Load("proj_player.png"),
            MonsterProjectile = Load("proj_monster.png"),

            // 6. Pickups & Powerup Badges
            BlueGem = new[]
            {
                Load("pickup_gem_0.png"),
                Load("pickup_gem_1.png")
            },
            RedRuby = new[]
            {
                Load("pickup_ruby_0.png"),
                Load("pickup_ruby_1.png")
            },
            GoldCartridge = new[]
            {
                Load("pickup_gold_0.png"),
                Load("pickup_gold_1.png")
            },
            MedKit = new[]
            {
                Load("pickup_medkit_0.png"),
                Load("pickup_medkit_1.png")
            },
            RapidFireBadge = Load("badge_rapid.png"),
            HighJumpBadge  = Load("badge_jump.png"),
            SpeedDashBadge = Load("badge_speed.png"),
            InvulnerabilityBadge = LoadOptional("badge_invuln.png", () => themeId switch
            {
                "space" => SpaceInvadersTheme.BakeSpaceInvulnBadge(),
                "underwater" => UnderwaterTheme.BakeUnderwaterInvulnBadge(),
                "forest" => ForestHunterTheme.BakeForestInvulnBadge(),
                _ => DefaultTheme.BakeDukeInvulnBadge()
            })
        };
    }

    /// <summary>
    /// Exports all sprites of a <see cref="ThemeAssetPack"/> as standard .png files into a directory on disk.
    /// </summary>
    public static void ExportThemePackToDirectory(ThemeAssetPack pack, string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        void Save(EgaSprite s, string name) => SavePng(s, Path.Combine(directoryPath, name));

        Save(pack.BackgroundFar,  "bg_far.png");
        Save(pack.BackgroundMid,  "bg_mid.png");
        Save(pack.BackgroundNear, "bg_near.png");

        Save(pack.SolidTile,             "tile_solid.png");
        Save(pack.OneWayTileVariants[0], "tile_oneway_0.png");
        Save(pack.OneWayTileVariants[1], "tile_oneway_1.png");
        Save(pack.HazardTile,            "tile_hazard.png");
        Save(pack.BorderTile,            "tile_border.png");
        Save(pack.LadderTile,            "tile_ladder.png");
        Save(pack.CrateTile,             "tile_crate.png");

        Save(pack.PlayerIdle,     "player_idle.png");
        Save(pack.PlayerWalk[0],  "player_walk_0.png");
        Save(pack.PlayerWalk[1],  "player_walk_1.png");
        Save(pack.PlayerWalk[2],  "player_walk_2.png");
        Save(pack.PlayerWalk[3],  "player_walk_3.png");
        Save(pack.PlayerJump,     "player_jump.png");
        Save(pack.PlayerClimb[0], "player_climb_0.png");
        Save(pack.PlayerClimb[1], "player_climb_1.png");

        Save(pack.PatrolDroid[0],   "monster_1_0.png");
        Save(pack.PatrolDroid[1],   "monster_1_1.png");
        Save(pack.HoverDrone[0],    "monster_2_0.png");
        Save(pack.HoverDrone[1],    "monster_2_1.png");
        Save(pack.CeilingTurret[0], "monster_3_0.png");
        Save(pack.CeilingTurret[1], "monster_3_1.png");
        Save(pack.MechaHound[0],    "monster_4_0.png");
        Save(pack.MechaHound[1],    "monster_4_1.png");
        Save(pack.HeavyGuard[0],    "monster_5_0.png");
        Save(pack.HeavyGuard[1],    "monster_5_1.png");

        Save(pack.PlayerProjectile,  "proj_player.png");
        Save(pack.MonsterProjectile, "proj_monster.png");

        Save(pack.BlueGem[0],       "pickup_gem_0.png");
        Save(pack.BlueGem[1],       "pickup_gem_1.png");
        Save(pack.RedRuby[0],       "pickup_ruby_0.png");
        Save(pack.RedRuby[1],       "pickup_ruby_1.png");
        Save(pack.GoldCartridge[0], "pickup_gold_0.png");
        Save(pack.GoldCartridge[1], "pickup_gold_1.png");
        Save(pack.MedKit[0],        "pickup_medkit_0.png");
        Save(pack.MedKit[1],        "pickup_medkit_1.png");
        Save(pack.RapidFireBadge,       "badge_rapid.png");
        Save(pack.HighJumpBadge,        "badge_jump.png");
        Save(pack.SpeedDashBadge,       "badge_speed.png");
        Save(pack.InvulnerabilityBadge, "badge_invuln.png");
    }

    public static EgaSprite LoadThemeSprite(string themeId, string fileName, int offsetX = 0, int offsetY = 0)
    {
        // 1. Try local disk paths first (allows live editing of .png files on disk)
        string relativePath = Path.Combine("wwwroot", "themes", themeId, fileName);
        if (File.Exists(relativePath))
        {
            using var fs = File.OpenRead(relativePath);
            return LoadPng(fs, offsetX, offsetY);
        }

        // 2. Try EmbeddedResource (works in Blazor WASM in browser & tests from any working dir)
        var asm = typeof(EgaPngCodec).Assembly;
        string resourceName = $"JumpRunShoot.wwwroot.themes.{themeId}.{fileName}";
        using Stream? resStream = asm.GetManifestResourceStream(resourceName);
        if (resStream != null)
        {
            return LoadPng(resStream, offsetX, offsetY);
        }

        throw new FileNotFoundException($"Theme image '{fileName}' for theme '{themeId}' not found at '{relativePath}' or as embedded resource '{resourceName}'.");
    }

    public static void SavePng(EgaSprite sprite, string filePath)
    {
        using var fs = File.Create(filePath);
        WritePng(sprite, fs);
    }

    public static void WritePng(EgaSprite sprite, Stream output)
    {
        output.Write(PngSignature);

        // 1. IHDR chunk (13 bytes)
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr[0..4], sprite.Width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..8], sprite.Height);
        ihdr[8] = 8;  // Bit depth: 8
        ihdr[9] = 6;  // Color type: 6 (RGBA)
        ihdr[10] = 0; // Compression: Deflate
        ihdr[11] = 0; // Filter: None
        ihdr[12] = 0; // Interlace: None
        WriteChunk(output, "IHDR"u8, ihdr);

        // 2. IDAT chunk (unfiltered scanlines compressed with ZLib)
        int stride = sprite.Width * 4 + 1;
        byte[] rawScanlines = new byte[stride * sprite.Height];

        for (int y = 0; y < sprite.Height; y++)
        {
            int rowOffset = y * stride;
            rawScanlines[rowOffset] = 0; // Filter type 0 (None)
            for (int x = 0; x < sprite.Width; x++)
            {
                byte colorIdx = sprite.Pixels[y * sprite.Width + x];
                int pxOffset = rowOffset + 1 + x * 4;
                if (colorIdx == EgaSprite.Transparent)
                {
                    rawScanlines[pxOffset + 0] = 0;
                    rawScanlines[pxOffset + 1] = 0;
                    rawScanlines[pxOffset + 2] = 0;
                    rawScanlines[pxOffset + 3] = 0;
                }
                else
                {
                    uint rgba = EgaPalette.GetRgba((EgaColorIndex)colorIdx);
                    rawScanlines[pxOffset + 0] = (byte)(rgba & 0xFF);
                    rawScanlines[pxOffset + 1] = (byte)((rgba >> 8) & 0xFF);
                    rawScanlines[pxOffset + 2] = (byte)((rgba >> 16) & 0xFF);
                    rawScanlines[pxOffset + 3] = 0xFF;
                }
            }
        }

        using var compressedStream = new MemoryStream();
        using (var zlib = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(rawScanlines, 0, rawScanlines.Length);
        }
        WriteChunk(output, "IDAT"u8, compressedStream.ToArray());

        // 3. IEND chunk
        WriteChunk(output, "IEND"u8, ReadOnlySpan<byte>.Empty);
    }

    public static EgaSprite LoadPng(Stream input, int offsetX = 0, int offsetY = 0)
    {
        Span<byte> sig = stackalloc byte[8];
        input.ReadExactly(sig);
        if (!sig.SequenceEqual(PngSignature))
            throw new InvalidDataException("Invalid PNG signature.");

        int width = 0;
        int height = 0;
        using var idatBuffer = new MemoryStream();
        Span<byte> headerBuf = stackalloc byte[8];
        Span<byte> crcBuf = stackalloc byte[4];

        while (true)
        {
            if (input.Read(headerBuf) < 8) break;
            int length = BinaryPrimitives.ReadInt32BigEndian(headerBuf[0..4]);
            uint chunkType = BinaryPrimitives.ReadUInt32BigEndian(headerBuf[4..8]);

            byte[] data = new byte[length];
            if (length > 0) input.ReadExactly(data);

            input.ReadExactly(crcBuf);

            // 'IHDR' = 0x49484452, 'IDAT' = 0x49444154, 'IEND' = 0x49454E44
            if (chunkType == 0x49484452u)
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
                height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(4, 4));
            }
            else if (chunkType == 0x49444154u)
            {
                idatBuffer.Write(data, 0, data.Length);
            }
            else if (chunkType == 0x49454E44u)
            {
                break;
            }
        }

        idatBuffer.Position = 0;
        int bpp = 4;
        int rowBytes = width * bpp;
        int stride = rowBytes + 1;
        byte[] raw = new byte[stride * height];

        using (var zlib = new ZLibStream(idatBuffer, CompressionMode.Decompress))
        {
            zlib.ReadExactly(raw);
        }

        byte[] pixels = new byte[width * height];
        byte[] prevRow = new byte[rowBytes];
        byte[] currRow = new byte[rowBytes];

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * stride;
            byte filter = raw[rowStart];

            for (int i = 0; i < rowBytes; i++)
            {
                byte xByte = raw[rowStart + 1 + i];
                byte a = i >= bpp ? currRow[i - bpp] : (byte)0;
                byte b = prevRow[i];
                byte c = i >= bpp ? prevRow[i - bpp] : (byte)0;

                currRow[i] = filter switch
                {
                    0 => xByte,
                    1 => (byte)(xByte + a),
                    2 => (byte)(xByte + b),
                    3 => (byte)(xByte + ((a + b) >> 1)),
                    4 => (byte)(xByte + PaethPredictor(a, b, c)),
                    _ => xByte
                };
            }

            for (int x = 0; x < width; x++)
            {
                int px = x * 4;
                byte r = currRow[px + 0];
                byte g = currRow[px + 1];
                byte bl = currRow[px + 2];
                byte al = currRow[px + 3];

                if (al < 128)
                {
                    pixels[y * width + x] = EgaSprite.Transparent;
                }
                else
                {
                    pixels[y * width + x] = MapRgbToNearestEgaIndex(r, g, bl);
                }
            }

            Buffer.BlockCopy(currRow, 0, prevRow, 0, rowBytes);
        }

        return new EgaSprite(width, height, pixels, offsetX, offsetY);
    }

    private static byte MapRgbToNearestEgaIndex(byte r, byte g, byte b)
    {
        int bestIdx = 0;
        int bestDist = int.MaxValue;

        for (int i = 0; i < 16; i++)
        {
            uint egaRgba = EgaPalette.GetRgba((EgaColorIndex)i);
            int er = (int)(egaRgba & 0xFF);
            int eg = (int)((egaRgba >> 8) & 0xFF);
            int eb = (int)((egaRgba >> 16) & 0xFF);

            int dr = r - er;
            int dg = g - eg;
            int db = b - eb;
            int dist = dr * dr + dg * dg + db * db;
            if (dist == 0) return (byte)i;
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIdx = i;
            }
        }

        return (byte)bestIdx;
    }

    private static int PaethPredictor(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> lenBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lenBytes, data.Length);
        output.Write(lenBytes);
        output.Write(type);
        if (!data.IsEmpty) output.Write(data);

        uint crc = ComputeCrc32(type, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFFu;
        for (int i = 0; i < type.Length; i++)
            crc = UpdateCrcByte(crc, type[i]);
        for (int i = 0; i < data.Length; i++)
            crc = UpdateCrcByte(crc, data[i]);
        return ~crc;
    }

    private static uint UpdateCrcByte(uint crc, byte b)
    {
        crc ^= b;
        for (int k = 0; k < 8; k++)
            crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        return crc;
    }
}
