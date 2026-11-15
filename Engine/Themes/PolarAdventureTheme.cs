using System;
using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Polar Adventure theme: Arctic night sky with shimmering Aurora Borealis, sculpted snow-capped mountains,
/// cozy igloo &amp; frosted pines, icy snow platforms with crystal icicles, compact orange-beak penguin throwing
/// snowballs, Arctic creatures (walrus, Christmas reindeer, ceiling seal with snowflakes, elk, polar bear),
/// faceted colored triangles, fish medkit, and polar powerups.
/// </summary>
public sealed class PolarAdventureTheme : SpriteThemeBase
{
    public const string ThemeIdentifier = "polar";
    public override string BgmTrack => "audio/bgm_polar.wav";

    private static readonly Lazy<ThemeAssetPack> _assets = new(LoadOrBakeAssets);
    public override ThemeAssetPack Assets => _assets.Value;

    private static ThemeAssetPack LoadOrBakeAssets()
    {
        try
        {
            return EgaPngCodec.LoadThemePack(ThemeIdentifier, "Polar Adventure", zeroGravity: false);
        }
        catch
        {
            return BakeProceduralAssets();
        }
    }

    /// <summary>
    /// Bakes all Polar Adventure theme visual assets into clean, unblurred 16-color EGA sprites.
    /// Strictly uses solid EGA colors with crisp 1px black outlines (no checkerboard dithering or anti-aliasing).
    /// </summary>
    public static ThemeAssetPack BakeProceduralAssets()
    {
        return new ThemeAssetPack
        {
            Name = "Polar Adventure",
            ZeroGravity = false,

            // 1. Standard 3-Layer Parallax Background Images (Height: 168px)
            BackgroundFar = BakePolarFarLayer(),
            BackgroundMid = BakePolarMidLayer(),
            BackgroundNear = BakePolarNearLayer(),

            // 2. Standard 16x16 Polar World Tiles
            SolidTile = BakePolarSolidTile(),
            OneWayTileVariants = new[]
            {
                BakePolarOneWayTile(parity: 0),
                BakePolarOneWayTile(parity: 1)
            },
            HazardTile = BakeIceSpikesHazard(),
            BorderTile = BakeFrostStoneBorder(),
            LadderTile = BakeSnowyLadder(),
            CrateTile = BakeSnowBlockCrate(),

            // 3. Player: Smaller Cute Penguin with Orange Beak throwing Snowballs
            PlayerIdle = BakePenguinAction(legStep: 0, throwBob: 0, wingUp: false),
            PlayerWalk = new[]
            {
                BakePenguinAction(legStep: 2, throwBob: 0, wingUp: false),
                BakePenguinAction(legStep: 0, throwBob: 1, wingUp: false),
                BakePenguinAction(legStep: -2, throwBob: 0, wingUp: false),
                BakePenguinAction(legStep: 0, throwBob: 1, wingUp: false)
            },
            PlayerJump = BakePenguinAction(legStep: -1, throwBob: -1, wingUp: true),
            PlayerClimb = new[]
            {
                BakePenguinClimb(step: 1),
                BakePenguinClimb(step: -1)
            },

            // 4. Monsters: Polar Creatures (Walrus, Holiday Reindeer, Ceiling Seal, Elk, White Polar Bear)
            PatrolDroid = new[]
            {
                BakeWalrus(step: 0),
                BakeWalrus(step: 1)
            },
            HoverDrone = new[]
            {
                BakeHolidayReindeer(glint: true),
                BakeHolidayReindeer(glint: false)
            },
            CeilingTurret = new[]
            {
                BakeCeilingSeal(throwing: false),
                BakeCeilingSeal(throwing: true)
            },
            MechaHound = new[]
            {
                BakeElk(stride: 0),
                BakeElk(stride: 1)
            },
            HeavyGuard = new[]
            {
                BakePolarBear(stride: 1),
                BakePolarBear(stride: -1)
            },

            // 5. Projectiles: Snowballs & Seal Snowflakes
            PlayerProjectile = BakeSnowballProjectile(),
            MonsterProjectile = BakeSnowballProjectile(),
            CeilingProjectile = BakeSnowflakeProjectile(),

            // 6. Pickups: Faceted Colored Triangles & Fish MedKit
            BlueGem = new[]
            {
                BakeTriangleGem(EgaColorIndex.BrightCyan, EgaColorIndex.Cyan, EgaColorIndex.Blue, EgaColorIndex.BrightWhite, glint: true),
                BakeTriangleGem(EgaColorIndex.BrightCyan, EgaColorIndex.Cyan, EgaColorIndex.Blue, EgaColorIndex.BrightWhite, glint: false)
            },
            RedRuby = new[]
            {
                BakeTriangleGem(EgaColorIndex.BrightGreen, EgaColorIndex.Green, EgaColorIndex.Green, EgaColorIndex.BrightWhite, glint: true),
                BakeTriangleGem(EgaColorIndex.BrightGreen, EgaColorIndex.Green, EgaColorIndex.Green, EgaColorIndex.BrightWhite, glint: false)
            },
            GoldCartridge = new[]
            {
                BakeTriangleGem(EgaColorIndex.BrightWhite, EgaColorIndex.Yellow, EgaColorIndex.Brown, EgaColorIndex.BrightWhite, glint: true),
                BakeTriangleGem(EgaColorIndex.BrightWhite, EgaColorIndex.Yellow, EgaColorIndex.Brown, EgaColorIndex.BrightWhite, glint: false)
            },
            MedKit = new[]
            {
                BakeFishMedKit(glint: true),
                BakeFishMedKit(glint: false)
            },

            // 7. Powerup Badges (14x14)
            RapidFireBadge = BakePolarRapidBadge(),
            HighJumpBadge = BakePolarJumpBadge(),
            SpeedDashBadge = BakePolarSpeedBadge(),
            InvulnerabilityBadge = BakePolarInvulnBadge(),
            PiercingPlasmaBadge = BakePolarRapidBadge()
        };
    }

    // =========================================================================
    // 1. Horizontally-Repeating Parallax Background Layers (Height: 168px)
    // =========================================================================

    private static EgaSprite BakePolarFarLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Arctic Twilight Sky: Deep midnight zenith stepping cleanly into polar blue
            b.FillRect(0, 0, 320, 22, EgaColorIndex.Black);
            b.FillRect(0, 22, 320, 146, EgaColorIndex.Blue);

            // Crisp non-linear polar starfield
            for (int i = 0; i < 40; i++)
            {
                int sx = (i * 89 + i * i * 37 + 19) % 320;
                int sy = (i * 43 + i * i * 13 + 3) % 86;
                if (i % 10 == 0)
                {
                    b.DrawSparkle(sx, sy, EgaColorIndex.BrightWhite);
                }
                else
                {
                    EgaColorIndex sc = (i % 3 == 0) ? EgaColorIndex.BrightWhite : ((i % 2 == 0) ? EgaColorIndex.BrightCyan : EgaColorIndex.LightGray);
                    b.SetPixel(sx, sy, sc);
                }
            }

            // Shimmering Aurora Borealis flowing veil & soft ribbed glow (smooth, never jagged)
            for (int x = 0; x < 320; x++)
            {
                float t = x * (MathF.Tau / 320.0f);
                int bottomY = 38 + (int)(MathF.Sin(t * 2.0f) * 8.0f + MathF.Cos(t * 3.0f) * 4.0f);
                float veilMod = 0.5f + 0.5f * MathF.Sin(t * 5.0f + MathF.Sin(t * 2.0f) * 1.5f);
                int height = 7 + (int)(veilMod * 11.0f);
                int topY = Math.Max(6, bottomY - height);

                b.DrawVLine(x, topY, height, EgaColorIndex.Green);
                if (height > 6)
                {
                    b.DrawVLine(x, bottomY - 4, 4, EgaColorIndex.BrightGreen);
                }
                b.DrawVLine(x, bottomY - 1, 2, EgaColorIndex.BrightCyan);

                // Delicate magenta crest wisps on sweeping folds
                if (veilMod > 0.72f)
                {
                    b.DrawVLine(x, Math.Max(4, topY - 2), 3, EgaColorIndex.Magenta);
                    if (veilMod > 0.88f)
                    {
                        b.SetPixel(x, Math.Max(4, topY - 2), EgaColorIndex.BrightMagenta);
                    }
                }
            }

            // Glowing Crescent Moon clear of the aurora band (centered at x=278, y=54)
            for (int dy = -8; dy <= 8; dy++)
            {
                for (int dx = -8; dx <= 8; dx++)
                {
                    int r2 = dx * dx + dy * dy;
                    int cutR2 = (dx - 3) * (dx - 3) + (dy + 1) * (dy + 1);
                    if (r2 <= 64 && cutR2 > 42)
                    {
                        b.SetPixel(278 + dx, 54 + dy, dx < -1 ? EgaColorIndex.BrightWhite : EgaColorIndex.Yellow);
                    }
                }
            }

            // Distant Craggy Arctic Mountain Range (wraps seamlessly across 320px)
            for (int x = 0; x < 320; x++)
            {
                float t = x * (MathF.Tau / 320.0f);
                float primary = MathF.Sin(t * 2.0f) * 24.0f + MathF.Sin(t * 5.0f + 0.7f) * 9.0f;
                float crag = MathF.Sin(t * 11.0f) * 2.5f;
                float val = primary + crag;
                int peakY = 104 - (int)MathF.Abs(val);
                int h = 168 - peakY;

                float dVal = MathF.Cos(t * 2.0f) * 48.0f + MathF.Cos(t * 5.0f + 0.7f) * 45.0f + MathF.Cos(t * 11.0f) * 27.5f;
                float ridgeSlope = (val >= 0 ? 1.0f : -1.0f) * dVal;
                bool sunlit = ridgeSlope >= 0;

                // Clean slope shading following contour depth (no flat horizontal banding!)
                b.DrawVLine(x, peakY, h, sunlit ? EgaColorIndex.Cyan : EgaColorIndex.DarkGray);
                int deepStart = peakY + 28;
                if (deepStart < 168)
                {
                    b.DrawVLine(x, deepStart, 168 - deepStart, sunlit ? EgaColorIndex.Blue : EgaColorIndex.Black);
                }

                // Multi-tiered jagged snowcap with LightGray shadow fold
                int snowDepth = Math.Clamp((116 - peakY) / 2 + 5 + (int)MathF.Abs(MathF.Sin(t * 7.0f) * 3.0f), 4, 16);
                b.DrawVLine(x, peakY, snowDepth, sunlit ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray);
                if (sunlit && snowDepth > 4)
                {
                    b.DrawVLine(x, peakY + snowDepth - 2, 2, EgaColorIndex.BrightCyan);
                }
                b.SetPixel(x, peakY, EgaColorIndex.Black);
            }
        });

    private static EgaSprite BakePolarMidLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Sculpted Multi-Spired Glacial Icebergs with crystalline facet shading
            int[] bergCenters = { 48, 152, 256 };
            int[] mainHeights = { 42, 50, 38 };
            int[] mainWidths  = { 30, 36, 28 };

            for (int idx = 0; idx < bergCenters.Length; idx++)
            {
                int cx = bergCenters[idx];
                int h = mainHeights[idx];
                int hw = mainWidths[idx];

                // Draw asymmetric double-peaked glacier: tall spire + shoulder shelf
                DrawGlacierSpire(b, cx - 8, 164, hw - 4, h, litLeft: true);
                DrawGlacierSpire(b, cx + 10, 164, hw - 8, (int)(h * 0.72f), litLeft: true);
            }

            // Rolling Foreground Snowbanks across bottom horizon (y=150..167)
            for (int x = 0; x < 320; x++)
            {
                float t = x * (MathF.Tau / 320.0f);
                int snowY = 152 + (int)(MathF.Sin(t * 3.0f) * 4.0f + MathF.Cos(t * 6.0f) * 2.0f);
                b.DrawVLine(x, snowY, 168 - snowY, EgaColorIndex.LightGray);
                b.DrawVLine(x, snowY, 4, EgaColorIndex.BrightWhite);
                b.SetPixel(x, snowY + 4, EgaColorIndex.BrightCyan);
                b.SetPixel(x, snowY, EgaColorIndex.Black);
            }
        });

    private static void DrawGlacierSpire(SpriteBuilder b, int cx, int baseY, int halfW, int height, bool litLeft)
    {
        int topY = baseY - height;
        int crestOffset = litLeft ? -3 : 3;
        int crestX = cx + crestOffset;

        for (int dx = -halfW; dx <= halfW; dx++)
        {
            int px = (cx + dx + 320) % 320;
            bool leftSide = dx < crestOffset;
            float frac = leftSide
                ? (crestOffset - dx) / (float)Math.Max(1, halfW + crestOffset)
                : (dx - crestOffset) / (float)Math.Max(1, halfW - crestOffset);
            int shoulderStep = (frac > 0.55f) ? 4 : 0;
            int colTop = topY + (int)(frac * height * 0.85f) + shoulderStep;
            int colH = baseY - colTop;
            if (colH <= 0) continue;

            EgaColorIndex bodyCol = leftSide
                ? (frac < 0.4f ? EgaColorIndex.BrightWhite : EgaColorIndex.BrightCyan)
                : (frac < 0.35f ? EgaColorIndex.Cyan : EgaColorIndex.Blue);

            b.DrawVLine(px, colTop, colH, bodyCol);
            // Crisp snow rim & glacier ridge spine highlight
            b.DrawVLine(px, colTop, Math.Min(3, colH), leftSide ? EgaColorIndex.BrightWhite : EgaColorIndex.BrightCyan);
            if (dx == crestOffset)
            {
                b.DrawVLine(px, colTop, colH, EgaColorIndex.BrightWhite);
            }
            b.SetPixel(px, colTop, EgaColorIndex.Black);
        }
        // Highlight ridge apex
        b.SetPixel((crestX + 320) % 320, topY, EgaColorIndex.Black);
    }

    private static EgaSprite BakePolarNearLayer() =>
        EgaSprite.Bake(220, 168, b =>
        {
            // Crisp snowbank foreground rim (y=160..167)
            for (int x = 0; x < 220; x++)
            {
                int wave = (int)(MathF.Sin(x * (MathF.Tau / 220.0f) * 2.0f) * 1.5f);
                int sy = 160 + wave;
                b.DrawVLine(x, sy + 1, 168 - (sy + 1), EgaColorIndex.BrightWhite);
                b.DrawHLine(x, 165, 1, EgaColorIndex.LightGray);
                b.SetPixel(x, sy + 1, EgaColorIndex.BrightCyan);
                b.SetPixel(x, sy, EgaColorIndex.Black);
            }

            // 1. Cozy Ice-Brick Igloo with glowing warm doorway (x=18..72)
            const int iglooCx = 42;
            const int iglooR = 20;
            for (int dx = -iglooR; dx <= iglooR; dx++)
            {
                int dyMax = (int)MathF.Sqrt(Math.Max(0, iglooR * iglooR - dx * dx * 0.85f));
                int topY = 160 - dyMax;
                int h = 161 - topY;
                EgaColorIndex col = (dx > 6) ? EgaColorIndex.LightGray : EgaColorIndex.BrightWhite;
                b.DrawVLine(iglooCx + dx, topY, h, col);
                b.SetPixel(iglooCx + dx, topY, EgaColorIndex.Black);
            }
            // Igloo ice-block horizontal & staggered vertical seams
            foreach (int rowY in new[] { 145, 150, 155 })
            {
                int halfSpan = (int)MathF.Sqrt(Math.Max(0, iglooR * iglooR - (160 - rowY) * (160 - rowY)));
                b.DrawHLine(iglooCx - halfSpan + 1, rowY, Math.Max(1, halfSpan * 2 - 2), EgaColorIndex.BrightCyan);
                for (int sx = iglooCx - halfSpan + ((rowY % 2 == 0) ? 4 : 8); sx < iglooCx + halfSpan - 2; sx += 8)
                {
                    b.DrawVLine(sx, rowY, 4, EgaColorIndex.Cyan);
                }
            }
            // Igloo arched entrance tunnel & warm lantern glow
            b.FillRect(55, 148, 13, 13, EgaColorIndex.BrightWhite);
            b.DrawRect(55, 148, 13, 13, EgaColorIndex.Black);
            b.SetTransparent(67, 148);
            b.FillRect(59, 151, 7, 10, EgaColorIndex.Black);
            b.DrawHLine(59, 150, 6, EgaColorIndex.BrightCyan);
            b.FillRect(61, 155, 3, 4, EgaColorIndex.Brown);
            b.SetPixel(62, 156, EgaColorIndex.Yellow);

            // 2. Sculpted Tapered Frosted Arctic Pine Trees (x=108 and x=186)
            DrawFrostedPine(b, cx: 108, groundY: 161, scale: 1);
            DrawFrostedPine(b, cx: 186, groundY: 161, scale: 0);

            // 3. Sparkling Translucent Ice Crystal Cluster between trees (x=148)
            DrawIceCrystalCluster(b, cx: 148, groundY: 161);
        });

    private static void DrawFrostedPine(SpriteBuilder b, int cx, int groundY, int scale)
    {
        int trunkH = 11 + scale * 3;
        b.FillRect(cx - 2, groundY - trunkH, 5, trunkH, EgaColorIndex.Brown);
        b.DrawVLine(cx - 2, groundY - trunkH, trunkH, EgaColorIndex.Black);
        b.DrawVLine(cx + 2, groundY - trunkH, trunkH, EgaColorIndex.Black);
        b.DrawVLine(cx + 1, groundY - trunkH, trunkH, EgaColorIndex.Black); // Trunk shadow bark

        // 4 layered conical evergreen bough tiers topped with sculpted snow
        int baseTierY = groundY - trunkH + 1;
        for (int tier = 0; tier < 4; tier++)
        {
            int tierH = 9 - tier;
            int maxHalf = (11 + scale * 2) - tier * 2;
            int tierBottom = baseTierY - tier * 7;
            int tierTop = tierBottom - tierH;

            for (int row = 0; row <= tierH; row++)
            {
                int py = tierTop + row;
                int half = Math.Max(1, (row * maxHalf) / Math.Max(1, tierH));
                b.SetPixel(cx - half - 1, py, EgaColorIndex.Black);
                b.SetPixel(cx + half + 1, py, EgaColorIndex.Black);

                for (int dx = -half; dx <= half; dx++)
                {
                    bool snowCap = (row <= 2) || (row == 3 && Math.Abs(dx) > half - 2);
                    EgaColorIndex col = snowCap
                        ? (dx > 0 && row == 2 ? EgaColorIndex.BrightCyan : EgaColorIndex.BrightWhite)
                        : (dx < 0 ? EgaColorIndex.BrightGreen : EgaColorIndex.Green);
                    b.SetPixel(cx + dx, py, col);
                }
            }
            b.DrawHLine(cx - maxHalf, tierBottom + 1, maxHalf * 2 + 1, EgaColorIndex.Black);
        }
        b.SetPixel(cx, baseTierY - 3 * 7 - 7, EgaColorIndex.Black);
    }

    private static void DrawIceCrystalCluster(SpriteBuilder b, int cx, int groundY)
    {
        int[] ox = { -5, 0, 5 };
        int[] hh = { 12, 19, 10 };
        int[] hw = { 2, 3, 2 };

        for (int i = 0; i < 3; i++)
        {
            int x0 = cx + ox[i];
            int h = hh[i];
            int w = hw[i];
            for (int dy = 0; dy < h; dy++)
            {
                int py = groundY - h + dy;
                int half = dy < 4 ? (dy * w) / 4 : w;
                b.SetPixel(x0 - half - 1, py, EgaColorIndex.Black);
                b.SetPixel(x0 + half + 1, py, EgaColorIndex.Black);
                for (int dx = -half; dx <= half; dx++)
                {
                    EgaColorIndex c = dx < 0 ? EgaColorIndex.BrightWhite : (dx == 0 ? EgaColorIndex.BrightCyan : EgaColorIndex.Cyan);
                    b.SetPixel(x0 + dx, py, c);
                }
            }
            b.SetPixel(x0, groundY - h - 1, EgaColorIndex.Black);
        }
    }

    // =========================================================================
    // 2. Standard 16x16 Polar World Tiles
    // =========================================================================

    private static EgaSprite BakePolarSolidTile() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Glacial permafrost ice blocks underneath with diagonal specular crystal glints
            b.FillRect(0, 4, 16, 12, EgaColorIndex.Cyan);
            b.FillRect(0, 10, 16, 6, EgaColorIndex.Blue);

            // Clean horizontal & staggered vertical ice block joints
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            b.DrawHLine(1, 5, 14, EgaColorIndex.Black);
            b.DrawHLine(1, 10, 14, EgaColorIndex.Black);
            b.DrawVLine(7, 5, 5, EgaColorIndex.Black);
            b.DrawVLine(3, 10, 6, EgaColorIndex.Black);
            b.DrawVLine(12, 10, 6, EgaColorIndex.Black);

            // Chiseled cyan/bright-blue ice facet highlights
            b.DrawHLine(2, 6, 4, EgaColorIndex.BrightCyan);
            b.SetPixel(2, 7, EgaColorIndex.BrightCyan);
            b.DrawHLine(9, 6, 5, EgaColorIndex.BrightCyan);
            b.DrawHLine(5, 11, 5, EgaColorIndex.BrightBlue);
            b.SetPixel(5, 12, EgaColorIndex.Cyan);

            // Sculpted overhanging fluffy snow drift along the top edge (y=0..4)
            b.FillRect(0, 0, 16, 4, EgaColorIndex.BrightWhite);
            b.DrawHLine(0, 4, 16, EgaColorIndex.BrightCyan);
            b.DrawHLine(0, 5, 16, EgaColorIndex.Black);
            // Undulating snow overhang dips into ice face
            int[] snowDips = { 1, 2, 3, 7, 8, 12, 13 };
            foreach (int sx in snowDips)
            {
                b.SetPixel(sx, 5, EgaColorIndex.BrightWhite);
                b.SetPixel(sx, 6, EgaColorIndex.Black);
            }
            b.SetPixel(2, 6, EgaColorIndex.BrightWhite);
            b.SetPixel(2, 7, EgaColorIndex.Black);
        });

    private static EgaSprite BakePolarOneWayTile(int parity) =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Fluffy undulating snow shelf (y=0..6)
            b.FillRect(0, 1, 16, 3, EgaColorIndex.BrightWhite);
            b.DrawHLine(0, 0, 16, EgaColorIndex.Black);
            b.FillRect(0, 4, 16, 2, EgaColorIndex.BrightCyan);
            b.DrawHLine(0, 5, 16, EgaColorIndex.Cyan);
            b.DrawHLine(0, 6, 16, EgaColorIndex.Black);
            b.SetPixel(parity == 0 ? 3 : 11, 4, EgaColorIndex.BrightWhite);
            b.SetPixel(parity == 0 ? 12 : 4, 4, EgaColorIndex.BrightWhite);

            // Hanging multi-faceted translucent ice icicles underneath
            int[] xs = parity == 0 ? new[] { 2, 7, 12 } : new[] { 1, 6, 11 };
            int[] hs = parity == 0 ? new[] { 8, 6, 9 }  : new[] { 7, 9, 5 };

            for (int i = 0; i < 3; i++)
            {
                int ix = xs[i];
                int ih = hs[i];
                for (int dy = 0; dy < ih; dy++)
                {
                    int py = 7 + dy;
                    if (py >= 16) break;
                    int span = dy < 2 ? 2 : (dy < ih - 2 ? 1 : 0);
                    b.SetPixel(ix - 1, py, EgaColorIndex.Black);
                    b.SetPixel(ix + span + 1, py, EgaColorIndex.Black);
                    for (int w = 0; w <= span; w++)
                    {
                        EgaColorIndex ic = w == 0 ? EgaColorIndex.BrightWhite : (w == 1 ? EgaColorIndex.BrightCyan : EgaColorIndex.Cyan);
                        b.SetPixel(ix + w, py, ic);
                    }
                    if (dy == ih - 1 && py + 1 < 16)
                    {
                        b.SetPixel(ix, py + 1, EgaColorIndex.Black);
                    }
                }
            }
        });

    private static EgaSprite BakeIceSpikesHazard() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Frozen glacial ridge base
            b.FillRect(0, 13, 16, 3, EgaColorIndex.Blue);
            b.DrawHLine(0, 13, 16, EgaColorIndex.Cyan);
            b.DrawRect(0, 13, 16, 3, EgaColorIndex.Black);

            // 3 Faceted crystal ice stalagmites rising sharply
            int[] peaks = { 2, 7, 13 };
            int[] tops  = { 3, 0, 4 };

            for (int i = 0; i < 3; i++)
            {
                int cx = peaks[i];
                int ty = tops[i];
                for (int y = ty; y <= 13; y++)
                {
                    int spread = (y - ty) / 3;
                    b.SetPixel(cx - spread - 1, y, EgaColorIndex.Black);
                    b.SetPixel(cx + spread + 1, y, EgaColorIndex.Black);
                    for (int dx = -spread; dx <= spread; dx++)
                    {
                        EgaColorIndex col = dx < 0 ? EgaColorIndex.BrightWhite
                            : (dx == 0 ? EgaColorIndex.BrightCyan : EgaColorIndex.Cyan);
                        if (y > ty + 6 && dx > 0) col = EgaColorIndex.Blue;
                        b.SetPixel(cx + dx, y, col);
                    }
                }
                if (ty > 0) b.SetPixel(cx, ty - 1, EgaColorIndex.Black);
            }
        });

    private static EgaSprite BakeFrostStoneBorder() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Chiseled dark basalt bedrock stone with frosted bevels
            b.FillRect(0, 0, 16, 16, EgaColorIndex.DarkGray);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            b.DrawRect(2, 3, 12, 11, EgaColorIndex.LightGray);
            b.FillRect(3, 4, 10, 9, EgaColorIndex.DarkGray);
            b.DrawHLine(4, 8, 8, EgaColorIndex.Black);
            b.DrawVLine(8, 4, 4, EgaColorIndex.Black);
            b.DrawVLine(6, 9, 4, EgaColorIndex.Black);
            b.SetPixel(4, 5, EgaColorIndex.LightGray);
            b.SetPixel(10, 10, EgaColorIndex.LightGray);

            // Crystalline frost & icicle tendrils clinging to top & left seams
            b.FillRect(1, 1, 14, 2, EgaColorIndex.BrightWhite);
            b.DrawHLine(1, 3, 14, EgaColorIndex.BrightCyan);
            b.FillRect(1, 3, 2, 6, EgaColorIndex.BrightWhite);
            b.DrawVLine(3, 3, 5, EgaColorIndex.BrightCyan);
            b.DrawVLine(12, 3, 3, EgaColorIndex.BrightCyan);
            b.SetPixel(12, 4, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeSnowyLadder() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Frosted vertical rails with inner ice highlight & dark edge
            b.FillRect(2, 0, 3, 16, EgaColorIndex.Cyan);
            b.DrawVLine(2, 0, 16, EgaColorIndex.Black);
            b.DrawVLine(4, 0, 16, EgaColorIndex.Black);
            b.DrawVLine(3, 0, 16, EgaColorIndex.BrightCyan);

            b.FillRect(11, 0, 3, 16, EgaColorIndex.Cyan);
            b.DrawVLine(11, 0, 16, EgaColorIndex.Black);
            b.DrawVLine(13, 0, 16, EgaColorIndex.Black);
            b.DrawVLine(12, 0, 16, EgaColorIndex.BrightCyan);

            // Snowy cushioned rungs every 4px + tiny icicle nubs underneath
            for (int ry = 1; ry < 16; ry += 4)
            {
                b.DrawHLine(2, ry, 12, EgaColorIndex.BrightWhite);
                b.DrawHLine(2, ry + 1, 12, EgaColorIndex.BrightCyan);
                b.DrawHLine(2, ry + 2, 12, EgaColorIndex.Black);
                b.SetPixel(5, ry + 2, EgaColorIndex.BrightWhite);
                b.SetPixel(10, ry + 2, EgaColorIndex.BrightCyan);
            }
        });

    private static EgaSprite BakeSnowBlockCrate() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Packed carved ice-and-snow crate with crystal bevel & diagonal cross seam
            b.FillRect(1, 1, 14, 14, EgaColorIndex.BrightWhite);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);

            // Inner cyan/light-gray chiseled block frame
            b.DrawHLine(1, 1, 14, EgaColorIndex.BrightWhite);
            b.DrawHLine(1, 14, 14, EgaColorIndex.Cyan);
            b.DrawVLine(14, 1, 14, EgaColorIndex.Cyan);
            b.DrawRect(2, 2, 12, 12, EgaColorIndex.BrightCyan);
            b.DrawRect(3, 3, 10, 10, EgaColorIndex.Black);
            b.FillRect(4, 4, 8, 8, EgaColorIndex.BrightWhite);

            // Crystalline ice brace X inside recessed panel
            for (int i = 4; i <= 11; i++)
            {
                b.SetPixel(i, i, EgaColorIndex.BrightCyan);
                b.SetPixel(i, 15 - i, EgaColorIndex.Cyan);
            }
            b.SetPixel(6, 5, EgaColorIndex.LightGray);
            b.SetPixel(9, 10, EgaColorIndex.LightGray);
        });

    // =========================================================================
    // 3. Player: Smaller Cute Organic Penguin with Orange Beak (24x24, OffsetX=-5)
    // =========================================================================

    private static EgaSprite BakePenguinAction(int legStep, int throwBob, bool wingUp) =>
        EgaSprite.Bake(24, 24, b =>
        {
            const int ox = 5;

            // Compact cute rounded silhouette (y = 6..23)
            // 1. Rounded Penguin Head (y=6..12) with subtle blue plumage sheen on dark crown
            b.FillRect(ox + 3, 6, 8, 7, EgaColorIndex.Black);
            b.FillRect(ox + 4, 7, 6, 5, EgaColorIndex.DarkGray);
            b.DrawHLine(ox + 5, 7, 3, EgaColorIndex.Blue); // Sleek feather crown highlight
            b.SetTransparent(ox + 3, 6);
            b.SetTransparent(ox + 10, 6);
            b.SetTransparent(ox + 3, 12);

            // Big friendly white cheek patch & expressive eye with catchlight glint
            b.FillRect(ox + 7, 8, 3, 4, EgaColorIndex.BrightWhite);
            b.SetPixel(ox + 8, 9, EgaColorIndex.Black);
            b.SetPixel(ox + 9, 9, EgaColorIndex.Black);
            b.SetPixel(ox + 8, 8, EgaColorIndex.BrightCyan);

            // Sculpted Curved Bright Orange Beak protruding forward
            b.FillRect(ox + 10, 9, 4, 2, EgaColorIndex.Brown);
            b.DrawHLine(ox + 11, 9, 3, EgaColorIndex.Yellow);
            b.DrawHLine(ox + 11, 8, 2, EgaColorIndex.Black);
            b.DrawHLine(ox + 11, 11, 3, EgaColorIndex.Black);
            b.SetPixel(ox + 14, 10, EgaColorIndex.Black);

            // 2. Plump Pear-Shaped Penguin Body (y=12..20)
            b.FillRect(ox + 2, 12, 9, 9, EgaColorIndex.DarkGray);
            b.DrawRect(ox + 2, 12, 9, 9, EgaColorIndex.Black);
            b.SetTransparent(ox + 2, 12);
            b.SetTransparent(ox + 10, 12);
            // Smooth oval snow-white belly with subtle light-gray bottom curve
            b.FillRect(ox + 6, 13, 4, 7, EgaColorIndex.BrightWhite);
            b.DrawHLine(ox + 6, 19, 3, EgaColorIndex.LightGray);
            b.DrawVLine(ox + 3, 14, 4, EgaColorIndex.Blue); // Back feather contour

            // Little tapered penguin tail flick on back-left
            b.FillRect(ox + 0, 18, 2, 2, EgaColorIndex.Black);
            b.SetPixel(ox + 1, 18, EgaColorIndex.DarkGray);

            // 3. Curved Penguin Flipper + Shaded Snowball in hand
            int armY = wingUp ? 11 : (13 + throwBob);
            b.FillRect(ox + 4, armY, 6, 4, EgaColorIndex.DarkGray);
            b.DrawRect(ox + 4, armY, 6, 4, EgaColorIndex.Black);
            b.DrawHLine(ox + 5, armY + 1, 4, EgaColorIndex.Black);
            b.SetTransparent(ox + 4, armY + 3);

            // Round shaded snowball tucked cleanly at flipper tip
            int ballX = ox + 10;
            int ballY = wingUp ? 10 : (13 + throwBob);
            b.FillRect(ballX, ballY, 4, 4, EgaColorIndex.BrightWhite);
            b.DrawRect(ballX, ballY, 4, 4, EgaColorIndex.Black);
            b.SetPixel(ballX + 1, ballY + 2, EgaColorIndex.BrightCyan);
            b.SetPixel(ballX + 2, ballY + 2, EgaColorIndex.BrightCyan);
            b.SetTransparent(ballX, ballY);
            b.SetTransparent(ballX + 3, ballY);
            b.SetTransparent(ballX, ballY + 3);
            b.SetTransparent(ballX + 3, ballY + 3);

            // 4. Webbed Orange Feet (y=20..23) stepping cleanly with legStep
            int leftFootY  = Math.Clamp(20 + legStep, 18, 21);
            int rightFootY = Math.Clamp(20 - legStep, 18, 21);
            b.FillRect(ox + 2, leftFootY, 4, 3, EgaColorIndex.Brown);
            b.DrawHLine(ox + 3, leftFootY, 3, EgaColorIndex.Yellow);
            b.DrawRect(ox + 2, leftFootY, 4, 3, EgaColorIndex.Black);

            b.FillRect(ox + 7, rightFootY, 5, 3, EgaColorIndex.Brown);
            b.DrawHLine(ox + 8, rightFootY, 4, EgaColorIndex.Yellow);
            b.DrawRect(ox + 7, rightFootY, 5, 3, EgaColorIndex.Black);
        }, offsetX: -5, offsetY: 0);

    private static EgaSprite BakePenguinClimb(int step) =>
        EgaSprite.Bake(14, 24, b =>
        {
            // Rear climbing view matching compact penguin height (y=6..23)
            b.FillRect(3, 6, 8, 7, EgaColorIndex.DarkGray);
            b.DrawRect(3, 6, 8, 7, EgaColorIndex.Black);
            b.SetTransparent(3, 6);
            b.SetTransparent(10, 6);
            b.DrawHLine(5, 7, 4, EgaColorIndex.Blue);

            b.FillRect(2, 12, 10, 9, EgaColorIndex.DarkGray);
            b.DrawRect(2, 12, 10, 9, EgaColorIndex.Black);
            b.FillRect(4, 13, 6, 7, EgaColorIndex.Black);
            b.DrawVLine(6, 14, 5, EgaColorIndex.Blue);

            int leftFlipperY  = 11 - (step * 2);
            int rightFlipperY = 11 + (step * 2);
            b.FillRect(0, leftFlipperY, 3, 5, EgaColorIndex.DarkGray);
            b.DrawRect(0, leftFlipperY, 3, 5, EgaColorIndex.Black);
            b.FillRect(11, rightFlipperY, 3, 5, EgaColorIndex.DarkGray);
            b.DrawRect(11, rightFlipperY, 3, 5, EgaColorIndex.Black);

            int leftFootY  = 20 + step;
            int rightFootY = 20 - step;
            b.FillRect(2, leftFootY, 4, 3, EgaColorIndex.Brown);
            b.DrawHLine(3, leftFootY, 2, EgaColorIndex.Yellow);
            b.DrawRect(2, leftFootY, 4, 3, EgaColorIndex.Black);

            b.FillRect(8, rightFootY, 4, 3, EgaColorIndex.Brown);
            b.DrawHLine(9, rightFootY, 2, EgaColorIndex.Yellow);
            b.DrawRect(8, rightFootY, 4, 3, EgaColorIndex.Black);
        });

    // =========================================================================
    // 4. Monsters: Sculpted Walrus, Holiday Reindeer, Ceiling Seal, Elk, Polar Bear
    // =========================================================================

    private static EgaSprite BakeWalrus(int step) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Chunky wide plump Walrus with creased skin, bristled muzzle & two curved ivory tusks (facing Right)
            // Hind tail flipper flopping at left
            b.FillRect(0, 9 + (step == 0 ? 1 : 0), 3, 3, EgaColorIndex.DarkGray);
            b.DrawRect(0, 9 + (step == 0 ? 1 : 0), 3, 3, EgaColorIndex.Black);

            // Plump fat body (x=2..10, y=5..12) with highlight fold & belly shade
            b.FillRect(2, 5, 9, 7, EgaColorIndex.Brown);
            b.DrawRect(2, 5, 9, 7, EgaColorIndex.Black);
            b.DrawHLine(3, 6, 6, EgaColorIndex.Yellow);   // Smooth back sheen
            b.DrawHLine(3, 10, 6, EgaColorIndex.DarkGray);// Heavy underside shadow
            b.SetTransparent(2, 5);

            // Thick domed head & whiskered puffy muzzle on right
            b.FillRect(7, 2, 6, 7, EgaColorIndex.Brown);
            b.DrawRect(7, 2, 6, 7, EgaColorIndex.Black);
            b.SetTransparent(7, 2);
            b.SetTransparent(12, 2);
            b.DrawHLine(8, 3, 3, EgaColorIndex.Yellow);

            // Glossy dark eye & white whisker-studded snout cushion
            b.SetPixel(10, 4, EgaColorIndex.Black);
            b.SetPixel(9, 4, EgaColorIndex.BrightWhite);
            b.FillRect(9, 6, 4, 3, EgaColorIndex.LightGray);
            b.DrawRect(9, 6, 4, 3, EgaColorIndex.Black);
            b.SetPixel(10, 7, EgaColorIndex.DarkGray);
            b.SetPixel(12, 7, EgaColorIndex.DarkGray);

            // Two long bold curved ivory tusks pointing down!
            b.FillRect(9, 8, 2, 5, EgaColorIndex.BrightWhite);
            b.DrawVLine(8, 9, 4, EgaColorIndex.Black);
            b.FillRect(11, 8, 2, 5, EgaColorIndex.BrightWhite);
            b.DrawVLine(13, 9, 4, EgaColorIndex.Black);
            b.SetPixel(10, 11, EgaColorIndex.LightGray);
            b.SetPixel(12, 11, EgaColorIndex.LightGray);
            b.SetPixel(9, 13, EgaColorIndex.Black);
            b.SetPixel(12, 13, EgaColorIndex.Black);

            // Front paddle flipper alternating slightly for lively waddle
            int flipX = 4 + step;
            b.FillRect(flipX, 11, 4, 3, EgaColorIndex.Brown);
            b.DrawRect(flipX, 11, 4, 3, EgaColorIndex.Black);
        });

    private static EgaSprite BakeHolidayReindeer(bool glint) =>
        EgaSprite.Bake(14, 12, b =>
        {
            // Flying Shooter (14x12): Festive Prancing Christmas Reindeer shooting Snowballs!
            // Golden multi-tined antlers
            b.SetPixel(7, 0, EgaColorIndex.Yellow);
            b.SetPixel(8, 1, EgaColorIndex.Yellow);
            b.SetPixel(10, 0, EgaColorIndex.Yellow);
            b.SetPixel(11, 1, EgaColorIndex.Yellow);
            b.SetPixel(12, 0, EgaColorIndex.Brown);

            // Streamlined chestnut body + holiday green saddle blanket trimmed in bright white fluff!
            b.FillRect(2, 4, 8, 5, EgaColorIndex.Brown);
            b.DrawRect(2, 4, 8, 5, EgaColorIndex.Black);
            b.FillRect(4, 4, 4, 4, EgaColorIndex.BrightGreen);
            b.DrawHLine(4, 7, 4, EgaColorIndex.BrightWhite); // White fur saddle trim
            b.SetPixel(1, 5, EgaColorIndex.BrightWhite);     // Fluffy tail

            // Head, eye & glowing Red Rudolph nose!
            b.FillRect(8, 2, 5, 4, EgaColorIndex.Brown);
            b.DrawRect(8, 2, 5, 4, EgaColorIndex.Black);
            b.SetPixel(10, 3, EgaColorIndex.Black);
            b.SetPixel(13, 4, glint ? EgaColorIndex.BrightRed : EgaColorIndex.Red);
            b.SetPixel(13, 3, EgaColorIndex.Black);
            // Red scarf + golden sleigh bell
            b.DrawHLine(8, 5, 3, EgaColorIndex.BrightRed);
            b.SetPixel(11, 6, EgaColorIndex.Yellow);

            // Prancing galloping hooves + sleigh magic stardust
            if (glint)
            {
                b.FillRect(1, 9, 2, 3, EgaColorIndex.Black);  // Extended rear kick
                b.FillRect(9, 8, 2, 3, EgaColorIndex.Black);  // Raised front leap
                b.SetPixel(0, 7, EgaColorIndex.BrightCyan);
                b.SetPixel(5, 10, EgaColorIndex.Yellow);
            }
            else
            {
                b.FillRect(3, 9, 2, 3, EgaColorIndex.Black);
                b.FillRect(7, 9, 2, 3, EgaColorIndex.Black);
                b.SetPixel(1, 8, EgaColorIndex.Yellow);
            }
        });

    private static EgaSprite BakeCeilingSeal(bool throwing) =>
        EgaSprite.Bake(14, 12, b =>
        {
            // Ceiling Shooter (14x12): Plump Spotted Harp Seal perched overhead dropping Snowflakes!
            // Smooth arched snow-ice seal belly/torso hanging from ceiling without ear-like brackets
            b.FillRect(3, 0, 8, 4, EgaColorIndex.LightGray);
            b.DrawHLine(3, 0, 8, EgaColorIndex.Black);
            b.SetPixel(2, 1, EgaColorIndex.Black);
            b.SetPixel(11, 1, EgaColorIndex.Black);
            // Leopard seal spots on back
            b.SetPixel(4, 1, EgaColorIndex.DarkGray);
            b.SetPixel(9, 2, EgaColorIndex.DarkGray);

            // Side flippers tucked nicely along lower flank (y=3..6)
            b.FillRect(1, 4, 2, 3, EgaColorIndex.DarkGray);
            b.DrawRect(1, 4, 2, 3, EgaColorIndex.Black);
            b.FillRect(11, 4, 2, 3, EgaColorIndex.DarkGray);
            b.DrawRect(11, 4, 2, 3, EgaColorIndex.Black);

            // Cute round seal face looking straight down (y=2..9)
            b.FillRect(3, 2, 8, 7, EgaColorIndex.BrightWhite);
            b.DrawRect(3, 2, 8, 7, EgaColorIndex.Black);
            b.SetTransparent(3, 2);
            b.SetTransparent(10, 2);
            b.SetTransparent(3, 8);
            b.SetTransparent(10, 8);
            // Soft gray forehead contour
            b.DrawHLine(4, 2, 6, EgaColorIndex.LightGray);

            // Big glossy puppy-seal eyes, snout & delicate white whiskers
            b.SetPixel(5, 4, EgaColorIndex.Black);
            b.SetPixel(8, 4, EgaColorIndex.Black);
            b.FillRect(6, 6, 2, 2, EgaColorIndex.Black);
            b.DrawHLine(4, 6, 2, EgaColorIndex.LightGray);
            b.DrawHLine(8, 6, 2, EgaColorIndex.LightGray);

            // Crystal snowflake held right below muzzle!
            if (throwing)
            {
                b.DrawSparkle(7, 10, EgaColorIndex.BrightWhite);
                b.SetPixel(7, 10, EgaColorIndex.BrightCyan);
            }
            else
            {
                b.SetPixel(7, 10, EgaColorIndex.BrightCyan);
                b.SetPixel(6, 10, EgaColorIndex.BrightWhite);
                b.SetPixel(8, 10, EgaColorIndex.BrightWhite);
            }
        });

    private static EgaSprite BakeElk(int stride) =>
        EgaSprite.Bake(24, 14, b =>
        {
            // 2x1 Fast Floor Runner (24x14): Muscular Arctic Elk with grand antlers & dynamic gallop!
            // Sweeping palmate golden-brown antlers (x=15..22, y=0..4)
            b.DrawVLine(17, 1, 3, EgaColorIndex.Brown);
            b.SetPixel(16, 0, EgaColorIndex.Yellow);
            b.SetPixel(17, 0, EgaColorIndex.Yellow);
            b.SetPixel(19, 1, EgaColorIndex.Yellow);
            b.SetPixel(20, 0, EgaColorIndex.Yellow);
            b.SetPixel(21, 1, EgaColorIndex.Brown);
            b.SetPixel(22, 0, EgaColorIndex.Yellow);

            // Arched back, white rump tail & thick frosted neck mane
            b.FillRect(3, 4, 13, 5, EgaColorIndex.Brown);
            b.DrawRect(3, 4, 13, 5, EgaColorIndex.Black);
            b.DrawHLine(5, 5, 8, EgaColorIndex.Yellow); // Sunlit muscle back highlight
            b.FillRect(1, 4, 3, 3, EgaColorIndex.LightGray);
            b.DrawRect(1, 4, 3, 3, EgaColorIndex.Black);
            b.FillRect(12, 4, 4, 5, EgaColorIndex.DarkGray);
            b.DrawHLine(13, 5, 3, EgaColorIndex.LightGray);

            // Long proud elk head & dark snout (x=15..22, y=2..7)
            b.FillRect(16, 3, 7, 4, EgaColorIndex.Brown);
            b.DrawRect(16, 3, 7, 4, EgaColorIndex.Black);
            b.SetPixel(19, 4, EgaColorIndex.Black);
            b.SetPixel(18, 4, EgaColorIndex.BrightWhite);
            b.FillRect(21, 5, 2, 2, EgaColorIndex.DarkGray);

            // Opposing galloping legs for true speedy runner animation
            if (stride == 0)
            {
                // Extended open stride
                b.FillRect(3, 9, 2, 4, EgaColorIndex.Brown);
                b.SetPixel(2, 12, EgaColorIndex.Brown);
                b.SetPixel(2, 13, EgaColorIndex.Black);
                b.SetPixel(3, 13, EgaColorIndex.Black);

                b.FillRect(6, 9, 2, 4, EgaColorIndex.DarkGray);

                b.FillRect(14, 9, 2, 4, EgaColorIndex.Brown);
                b.SetPixel(16, 12, EgaColorIndex.Brown);
                b.SetPixel(16, 13, EgaColorIndex.Black);
                b.SetPixel(17, 13, EgaColorIndex.Black);
            }
            else
            {
                // Gathered mid-bound
                b.FillRect(5, 9, 2, 4, EgaColorIndex.Brown);
                b.SetPixel(5, 13, EgaColorIndex.Black);
                b.SetPixel(6, 13, EgaColorIndex.Black);

                b.FillRect(12, 9, 2, 4, EgaColorIndex.Brown);
                b.SetPixel(12, 13, EgaColorIndex.Black);
                b.SetPixel(13, 13, EgaColorIndex.Black);
            }
        });

    private static EgaSprite BakePolarBear(int stride) =>
        EgaSprite.Bake(22, 26, b =>
        {
            const int ox = 4;

            // Big 1x2 Heavy Monster (22x26): Muscular Towering Polar Bear throwing a big Snowball!
            // Rounded white bear ears with pink-gray inner dimple
            b.FillRect(ox + 2, 0, 3, 3, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 2, 0, 3, 3, EgaColorIndex.Black);
            b.SetPixel(ox + 3, 1, EgaColorIndex.LightGray);
            b.FillRect(ox + 8, 0, 3, 3, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 8, 0, 3, 3, EgaColorIndex.Black);
            b.SetPixel(ox + 9, 1, EgaColorIndex.LightGray);

            // Sculpted bear head & protruding snout
            b.FillRect(ox + 2, 2, 9, 6, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 2, 2, 9, 6, EgaColorIndex.Black);
            b.DrawHLine(ox + 3, 6, 7, EgaColorIndex.LightGray);
            b.SetPixel(ox + 8, 3, EgaColorIndex.Black); // Determined bear eye
            b.FillRect(ox + 10, 4, 4, 3, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 10, 4, 4, 3, EgaColorIndex.Black);
            b.SetPixel(ox + 13, 4, EgaColorIndex.Black); // Black bear nose
            b.DrawHLine(ox + 11, 6, 2, EgaColorIndex.LightGray);

            // Broad muscular snowy shoulders & chest fur contours
            b.FillRect(ox + 0, 7, 13, 12, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 0, 7, 13, 12, EgaColorIndex.Black);
            b.SetTransparent(ox + 0, 7);
            b.SetTransparent(ox + 12, 7);
            // Muscular belly & flank shadow so the white fur has bold 3D bulk!
            b.FillRect(ox + 2, 13, 9, 5, EgaColorIndex.LightGray);
            b.DrawHLine(ox + 3, 17, 7, EgaColorIndex.BrightCyan);
            b.DrawVLine(ox + 1, 9, 7, EgaColorIndex.LightGray);

            // Heavy front forearm raised holding a shaded icy snowball!
            b.FillRect(ox + 9, 10, 6, 4, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 9, 10, 6, 4, EgaColorIndex.Black);
            b.DrawHLine(ox + 10, 12, 4, EgaColorIndex.LightGray);
            // Big snowball ready to hurl
            b.FillRect(ox + 14, 8, 5, 5, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 14, 8, 5, 5, EgaColorIndex.Black);
            b.SetPixel(ox + 15, 11, EgaColorIndex.BrightCyan);
            b.SetPixel(ox + 16, 11, EgaColorIndex.BrightCyan);

            // Thick lumbering legs with ice shading & dark claws
            b.FillRect(ox + 1, 18 + stride, 5, 7, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 1, 18 + stride, 5, 7, EgaColorIndex.Black);
            b.DrawVLine(ox + 2, 19 + stride, 4, EgaColorIndex.LightGray);
            b.DrawHLine(ox + 2, 24 + stride, 3, EgaColorIndex.DarkGray);

            b.FillRect(ox + 7, 18 - stride, 5, 7, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 7, 18 - stride, 5, 7, EgaColorIndex.Black);
            b.DrawVLine(ox + 8, 19 - stride, 4, EgaColorIndex.LightGray);
            b.DrawHLine(ox + 8, 24 - stride, 3, EgaColorIndex.DarkGray);
        }, offsetX: -4, offsetY: 0);

    // =========================================================================
    // 5. Projectiles: Crisp Snowballs & 6-Point Crystal Snowflakes
    // =========================================================================

    public static EgaSprite BakeSnowballProjectile() =>
        EgaSprite.Bake(6, 6, b =>
        {
            b.FillRect(1, 0, 4, 6, EgaColorIndex.Black);
            b.FillRect(0, 1, 6, 4, EgaColorIndex.Black);
            b.FillRect(1, 1, 4, 4, EgaColorIndex.BrightWhite);
            b.SetPixel(1, 3, EgaColorIndex.BrightCyan);
            b.SetPixel(2, 4, EgaColorIndex.BrightCyan);
            b.SetPixel(3, 4, EgaColorIndex.BrightCyan);
        });

    public static EgaSprite BakeSnowflakeProjectile() =>
        EgaSprite.Bake(7, 7, b =>
        {
            b.DrawSparkle(3, 3, EgaColorIndex.Cyan);
            b.DrawHLine(0, 3, 7, EgaColorIndex.BrightCyan);
            b.DrawVLine(3, 0, 7, EgaColorIndex.BrightCyan);
            b.SetPixel(1, 1, EgaColorIndex.BrightWhite);
            b.SetPixel(5, 1, EgaColorIndex.BrightWhite);
            b.SetPixel(1, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(5, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(2, 2, EgaColorIndex.Cyan);
            b.SetPixel(4, 2, EgaColorIndex.Cyan);
            b.SetPixel(2, 4, EgaColorIndex.Cyan);
            b.SetPixel(4, 4, EgaColorIndex.Cyan);
            b.SetPixel(3, 3, EgaColorIndex.BrightWhite);
            b.SetPixel(3, 0, EgaColorIndex.BrightWhite);
            b.SetPixel(3, 6, EgaColorIndex.BrightWhite);
            b.SetPixel(0, 3, EgaColorIndex.BrightWhite);
            b.SetPixel(6, 3, EgaColorIndex.BrightWhite);
        });

    // =========================================================================
    // 6. Pickups & Powerup Badges (14x14): Faceted Triangles, Fish Box & Polar Badges
    // =========================================================================

    private static EgaSprite BakeTriangleGem(EgaColorIndex litCol, EgaColorIndex bodyCol, EgaColorIndex deepCol, EgaColorIndex glintCol, bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Faceted crystal prism Triangle (Turquoise, Dark Green, or Golden!)
            for (int r = 0; r < 10; r++)
            {
                int py = 2 + r;
                int halfSpan = r / 2;
                int xLeft = 6 - halfSpan;
                int xRight = 7 + halfSpan;

                b.SetPixel(xLeft - 1, py, EgaColorIndex.Black);
                b.SetPixel(xRight + 1, py, EgaColorIndex.Black);
                for (int px = xLeft; px <= xRight; px++)
                {
                    // Multi-facet crystal shading: sunlit left ridge, core body, dark right/bottom bevel
                    EgaColorIndex c;
                    if (r >= 8) c = deepCol;
                    else if (px <= 6 && r <= 7) c = litCol;
                    else c = bodyCol;
                    b.SetPixel(px, py, c);
                }
            }
            b.SetPixel(6, 1, EgaColorIndex.Black);
            b.SetPixel(7, 1, EgaColorIndex.Black);
            b.DrawHLine(2, 12, 10, EgaColorIndex.Black);

            // Animated inner specular sparkle
            b.SetPixel(6, 4, glintCol);
            b.SetPixel(5, 5, glintCol);
            if (glint)
            {
                b.DrawSparkle(5, 5, glintCol);
            }
        });

    private static EgaSprite BakeFishMedKit(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Health Case: Sturdy frosty supply chest with bold Fish Emblem!
            b.FillRect(1, 2, 12, 10, EgaColorIndex.BrightWhite);
            b.DrawRect(1, 2, 12, 10, EgaColorIndex.Black);
            b.DrawHLine(2, 3, 10, EgaColorIndex.BrightCyan);
            b.DrawHLine(2, 10, 10, EgaColorIndex.LightGray);
            // Handle on top
            b.FillRect(5, 0, 4, 2, EgaColorIndex.Cyan);
            b.DrawRect(5, 0, 4, 2, EgaColorIndex.Black);

            // Crisp bold Blue/Cyan Fish icon inside chest
            EgaColorIndex fishCol = glint ? EgaColorIndex.BrightCyan : EgaColorIndex.Blue;
            EgaColorIndex finCol  = glint ? EgaColorIndex.Cyan : EgaColorIndex.DarkGray;
            b.FillRect(5, 5, 5, 4, fishCol);
            b.DrawHLine(6, 4, 3, finCol);
            b.DrawVLine(3, 5, 4, fishCol);
            b.SetPixel(4, 6, fishCol);
            b.SetPixel(4, 7, fishCol);
            // Fish eye & white scale shine
            b.SetPixel(8, 6, EgaColorIndex.Black);
            b.SetPixel(7, 5, EgaColorIndex.BrightWhite);
        });

    public static EgaSprite BakePolarRapidBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Shoot Faster Powerup: Bigger Shaded Snowball inside a Radiant Outer Golden Circle!
            for (int y = 0; y < 14; y++)
            {
                for (int x = 0; x < 14; x++)
                {
                    float dx = x - 6.5f;
                    float dy = y - 6.5f;
                    float r2 = (dx * dx) + (dy * dy);

                    if (r2 <= 10.5f)
                    {
                        EgaColorIndex color = dy > 1.8f ? EgaColorIndex.BrightCyan
                            : (dx > 1.5f && dy > 0.2f) ? EgaColorIndex.LightGray
                            : EgaColorIndex.BrightWhite;
                        b.SetPixel(x, y, color);
                    }
                    else if (r2 <= 17.5f)
                    {
                        b.SetPixel(x, y, EgaColorIndex.Black);
                    }
                    else if (r2 <= 34.5f)
                    {
                        EgaColorIndex ringColor = r2 >= 26.5f ? EgaColorIndex.Yellow : EgaColorIndex.Brown;
                        if (dy < 0 && r2 >= 21.0f) ringColor = EgaColorIndex.Yellow;
                        b.SetPixel(x, y, ringColor);
                    }
                }
            }

            // Crisp radiant outer sparkles
            b.SetPixel(6, 0, EgaColorIndex.BrightWhite);
            b.SetPixel(7, 0, EgaColorIndex.BrightWhite);
            b.SetPixel(6, 13, EgaColorIndex.BrightWhite);
            b.SetPixel(7, 13, EgaColorIndex.BrightWhite);
            b.SetPixel(0, 6, EgaColorIndex.BrightWhite);
            b.SetPixel(0, 7, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 6, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 7, EgaColorIndex.BrightWhite);
            b.SetPixel(1, 1, EgaColorIndex.Yellow);
            b.SetPixel(12, 1, EgaColorIndex.Yellow);
            b.SetPixel(1, 12, EgaColorIndex.Yellow);
            b.SetPixel(12, 12, EgaColorIndex.Yellow);
        });

    public static EgaSprite BakePolarJumpBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Jump Higher Powerup: Sleek Feathered Black Penguin Wing with clean Luminous Halo!
            // Golden & Cyan clean glow ring tightly hugging the sweeping wing silhouette
            b.FillRect(3, 1, 4, 4, EgaColorIndex.Yellow);
            b.FillRect(4, 3, 5, 5, EgaColorIndex.Yellow);
            b.FillRect(6, 5, 5, 5, EgaColorIndex.Yellow);
            b.FillRect(8, 7, 4, 4, EgaColorIndex.BrightCyan);

            // Feathered black penguin flipper wing inside the glow
            b.FillRect(3, 2, 3, 3, EgaColorIndex.Black);
            b.FillRect(4, 3, 4, 4, EgaColorIndex.Black);
            b.FillRect(6, 5, 4, 4, EgaColorIndex.Black);
            b.FillRect(8, 7, 3, 3, EgaColorIndex.Black);
            b.SetPixel(11, 10, EgaColorIndex.Black);

            // Crisp blue-gray feather layers so the black wing reads cleanly as a wing
            b.DrawHLine(4, 3, 2, EgaColorIndex.DarkGray);
            b.SetPixel(5, 5, EgaColorIndex.BrightBlue);
            b.SetPixel(6, 6, EgaColorIndex.BrightBlue);
            b.SetPixel(7, 7, EgaColorIndex.DarkGray);
            b.SetPixel(9, 8, EgaColorIndex.BrightBlue);

            // Radiant tip sparkles
            b.DrawSparkle(2, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(11, 12, EgaColorIndex.BrightWhite);
        });

    public static EgaSprite BakePolarSpeedBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Speed Dash Powerup: Classic Curved Oak & Red Runner Sled with Wind Streaks!
            // Red curved steel runner rail + black outline + upturned curl bow
            b.DrawHLine(2, 10, 9, EgaColorIndex.BrightRed);
            b.DrawHLine(2, 11, 9, EgaColorIndex.Black);
            b.SetPixel(11, 9, EgaColorIndex.BrightRed);
            b.SetPixel(12, 8, EgaColorIndex.BrightRed);
            b.SetPixel(12, 7, EgaColorIndex.BrightWhite);
            b.SetPixel(11, 6, EgaColorIndex.Yellow); // Bow trim
            b.SetPixel(13, 8, EgaColorIndex.Black);
            b.SetPixel(12, 10, EgaColorIndex.Black);

            // Vertical struts
            b.DrawVLine(4, 7, 3, EgaColorIndex.DarkGray);
            b.DrawVLine(9, 7, 3, EgaColorIndex.DarkGray);

            // Polished wooden slat deck
            b.FillRect(2, 5, 9, 2, EgaColorIndex.Brown);
            b.DrawHLine(2, 4, 9, EgaColorIndex.Black);
            b.DrawHLine(3, 5, 7, EgaColorIndex.Yellow);
            b.DrawHLine(2, 7, 9, EgaColorIndex.Black);
            b.SetPixel(1, 5, EgaColorIndex.Black);
            b.SetPixel(11, 5, EgaColorIndex.Black);

            // Crisp wind motion streaks trailing behind
            b.DrawHLine(0, 3, 2, EgaColorIndex.BrightCyan);
            b.DrawHLine(0, 8, 2, EgaColorIndex.BrightWhite);
        });

    public static EgaSprite BakePolarInvulnBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Invulnerability Powerup: Bigger Shimmering Golden Trophy Fish! (14x14)
            // Dorsal fin on top
            b.FillRect(6, 1, 3, 2, EgaColorIndex.Yellow);
            b.DrawHLine(6, 0, 3, EgaColorIndex.Black);
            b.SetPixel(5, 1, EgaColorIndex.Black);
            b.SetPixel(9, 1, EgaColorIndex.Black);

            // Plump gleaming golden fish body (x=4..12, y=3..10)
            b.FillRect(4, 3, 8, 8, EgaColorIndex.Yellow);
            b.DrawHLine(5, 2, 6, EgaColorIndex.Black);
            b.DrawHLine(5, 11, 6, EgaColorIndex.Black);
            b.DrawVLine(12, 4, 6, EgaColorIndex.Black);
            b.DrawVLine(13, 5, 4, EgaColorIndex.Yellow);
            b.SetPixel(13, 4, EgaColorIndex.Black);
            b.SetPixel(13, 9, EgaColorIndex.Black);

            // Shaded lower gold belly & pectoral fin stripe
            b.DrawHLine(5, 9, 6, EgaColorIndex.Brown);
            b.DrawHLine(6, 10, 4, EgaColorIndex.Brown);
            b.SetPixel(7, 7, EgaColorIndex.Brown);
            b.SetPixel(8, 8, EgaColorIndex.Brown);

            // Connected forked golden tail fin on left
            b.FillRect(1, 3, 3, 8, EgaColorIndex.Yellow);
            b.SetPixel(0, 3, EgaColorIndex.Black);
            b.SetPixel(0, 4, EgaColorIndex.Black);
            b.SetPixel(0, 5, EgaColorIndex.Black);
            b.SetTransparent(0, 6);
            b.SetTransparent(0, 7);
            b.SetPixel(0, 8, EgaColorIndex.Black);
            b.SetPixel(0, 9, EgaColorIndex.Black);
            b.SetPixel(0, 10, EgaColorIndex.Black);
            b.DrawHLine(1, 2, 3, EgaColorIndex.Black);
            b.DrawHLine(1, 11, 3, EgaColorIndex.Black);
            b.SetPixel(1, 6, EgaColorIndex.Black);
            b.SetPixel(1, 7, EgaColorIndex.Black);
            b.SetPixel(2, 6, EgaColorIndex.Brown);
            b.SetPixel(2, 7, EgaColorIndex.Brown);

            // Glossy fish eye & metallic white scale highlights
            b.FillRect(9, 4, 2, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(10, 5, EgaColorIndex.Black);
            b.DrawHLine(6, 4, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(5, 5, EgaColorIndex.BrightWhite);
        });
}
