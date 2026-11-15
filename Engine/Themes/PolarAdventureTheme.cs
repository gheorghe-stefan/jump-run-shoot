using System;
using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// "Polar Adventure" Theme (North/South Pole, Snow &amp; Ice, Penguins &amp; Arctic Wildlife):
/// Designed with crisp 16-color EGA pixel art and zero anti-aliasing.
/// Features an Aurora Borealis night sky backdrop, snow-topped glacial tiles, icicle platforms,
/// snow-block crates, frosty stone borders, snowy ladders, a smaller penguin hero throwing snowballs,
/// Arctic wildlife enemies (Walrus, Ceiling Seal with snowflakes, Elk, Holiday Reindeer, Polar Bear),
/// colored triangle collectibles, fish medkits, golden fish shield, glowing snowball rapid-fire,
/// glowing black penguin wing high-jump, and sled speed dash.
/// </summary>
public sealed class PolarAdventureTheme : SpriteThemeBase
{
    public const string ThemeId = "polar";
    public const string DisplayName = "Polar Adventure";

    public override string BgmTrack => "audio/bgm_polar.wav";

    public static readonly ThemeAssetPack StaticAssets = LoadOrBakeAssets();

    public override ThemeAssetPack Assets => StaticAssets;

    private static ThemeAssetPack LoadOrBakeAssets()
    {
        try
        {
            return EgaPngCodec.LoadThemePack(ThemeId, DisplayName);
        }
        catch
        {
            return BakeProceduralAssets();
        }
    }

    public static ThemeAssetPack BakeProceduralAssets()
    {
        return new ThemeAssetPack
        {
            Name = DisplayName,
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
                BakeElk(step: 2),
                BakeElk(step: -2)
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

            // 6. Pickups: Colored Triangles & Fish MedKit
            BlueGem = new[]
            {
                BakeTriangleGem(EgaColorIndex.BrightCyan, EgaColorIndex.Cyan, EgaColorIndex.BrightWhite, glint: true),
                BakeTriangleGem(EgaColorIndex.BrightCyan, EgaColorIndex.Cyan, EgaColorIndex.BrightWhite, glint: false)
            },
            RedRuby = new[]
            {
                BakeTriangleGem(EgaColorIndex.Green, EgaColorIndex.Cyan, EgaColorIndex.BrightGreen, glint: true),
                BakeTriangleGem(EgaColorIndex.Green, EgaColorIndex.Cyan, EgaColorIndex.BrightGreen, glint: false)
            },
            GoldCartridge = new[]
            {
                BakeTriangleGem(EgaColorIndex.Yellow, EgaColorIndex.Brown, EgaColorIndex.BrightWhite, glint: true),
                BakeTriangleGem(EgaColorIndex.Yellow, EgaColorIndex.Brown, EgaColorIndex.BrightWhite, glint: false)
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
            // Deep Arctic midnight-blue sky (keeps 1px Black silhouettes crisp everywhere!)
            b.FillRect(0, 0, 320, 168, EgaColorIndex.Blue);

            // Crisp stars scattered non-linearly across the upper night sky
            for (int i = 0; i < 38; i++)
            {
                int sx = (i * 89 + i * i * 37 + 13) % 320;
                int sy = (i * 47 + i * i * 11 + 5) % 86;
                EgaColorIndex sc = (i % 4 == 0) ? EgaColorIndex.BrightWhite : ((i % 2 == 0) ? EgaColorIndex.BrightCyan : EgaColorIndex.LightGray);
                if (i % 9 == 0)
                {
                    b.DrawSparkle(sx, sy, EgaColorIndex.BrightWhite);
                }
                else
                {
                    b.SetPixel(sx, sy, sc);
                }
            }

            // Aurora Borealis: Sweeping solid vertical curtain ribbons wrapping seamlessly over 320px
            for (int x = 0; x < 320; x++)
            {
                float t = x * (MathF.Tau / 320.0f);
                int baseY = 26 + (int)(MathF.Sin(t * 2.0f) * 10.0f + MathF.Cos(t * 3.0f) * 5.0f);
                int ribbonH = 12 + (int)(MathF.Sin(t * 4.0f + 1.0f) * 5.0f);

                // Curtain vertical rays (pure solid bands, zero checkerboard)
                b.DrawVLine(x, baseY, ribbonH, EgaColorIndex.Green);
                b.DrawVLine(x, baseY + ribbonH - 4, 4, EgaColorIndex.BrightGreen);
                b.DrawVLine(x, baseY + ribbonH - 2, 2, EgaColorIndex.BrightCyan);

                // Occasional magenta shimmer peaks along high crests
                if ((x % 16) < 4 && MathF.Sin(t * 2.0f) < -0.3f)
                {
                    b.DrawVLine(x, baseY - 3, 4, EgaColorIndex.Magenta);
                    b.SetPixel(x, baseY - 3, EgaColorIndex.BrightMagenta);
                }

                // Secondary thinner aurora arc higher in the sky
                int secY = 10 + (int)(MathF.Cos(t + 0.8f) * 5.0f);
                b.DrawVLine(x, secY, 4, EgaColorIndex.Cyan);
                b.SetPixel(x, secY + 3, EgaColorIndex.BrightCyan);
            }

            // Distant Snow-Capped Polar Mountain Range (wraps seamlessly across 320px)
            for (int x = 0; x < 320; x++)
            {
                float t = x * (MathF.Tau / 320.0f);
                float raw = MathF.Sin(t * 2.0f) * 26.0f + MathF.Sin(t * 5.0f + 0.6f) * 10.0f;
                int peakY = 104 - (int)MathF.Abs(raw);
                int h = 168 - peakY;

                // True derivative of |raw| ensures lighting strictly flips along peak/valley ridges with zero vertical seams
                float deriv = MathF.Cos(t * 2.0f) * 52.0f + MathF.Cos(t * 5.0f + 0.6f) * 50.0f;
                float ridgeSlope = (raw >= 0 ? 1.0f : -1.0f) * deriv;
                EgaColorIndex rockCol = ridgeSlope >= 0 ? EgaColorIndex.Cyan : EgaColorIndex.DarkGray;
                b.DrawVLine(x, peakY, h, rockCol);

                // Thick snow cap near ridge top
                int snowDepth = Math.Clamp((114 - peakY) / 2 + 4, 3, 14);
                b.DrawVLine(x, peakY, snowDepth, ridgeSlope >= 0 ? EgaColorIndex.BrightWhite : EgaColorIndex.LightGray);
                b.SetPixel(x, peakY, EgaColorIndex.Black);
            }
        });

    private static EgaSprite BakePolarMidLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Glacial Icebergs & Rolling Snow Dunes along the mid horizon (transparent sky above)
            int[] bergStarts = { 18, 118, 224 };
            int[] bergWidths = { 54, 62, 48 };
            int[] bergHeights = { 34, 42, 30 };

            for (int idx = 0; idx < bergStarts.Length; idx++)
            {
                int bx = bergStarts[idx];
                int bw = bergWidths[idx];
                int bh = bergHeights[idx];
                int topY = 152 - bh;

                for (int dx = 0; dx < bw; dx++)
                {
                    float norm = (dx / (float)bw) * 2.0f - 1.0f; // -1..+1
                    int colTop = topY + (int)(MathF.Abs(norm) * bh * 0.65f);
                    int colH = 164 - colTop;
                    if (colH <= 0) continue;

                    EgaColorIndex face = norm < 0.1f ? EgaColorIndex.BrightCyan : EgaColorIndex.Cyan;
                    b.DrawVLine(bx + dx, colTop, colH, face);
                    b.DrawVLine(bx + dx, colTop, Math.Min(4, colH), EgaColorIndex.BrightWhite);
                    b.SetPixel(bx + dx, colTop, EgaColorIndex.Black);
                }
            }

            // Rolling foreground snowbanks across the bottom edge (152..167)
            for (int x = 0; x < 320; x++)
            {
                float t = x * (MathF.Tau / 320.0f);
                int snowY = 152 + (int)(MathF.Sin(t * 3.0f) * 4.0f);
                b.DrawVLine(x, snowY, 168 - snowY, EgaColorIndex.LightGray);
                b.DrawVLine(x, snowY, 4, EgaColorIndex.BrightWhite);
                b.SetPixel(x, snowY, EgaColorIndex.Black);
            }
        });

    private static EgaSprite BakePolarNearLayer() =>
        EgaSprite.Bake(220, 168, b =>
        {
            // Repeating 220px foreground polar strip: Snowbank base, cozy Igloo, snowy Arctic Pines & Ice Pillar
            for (int x = 0; x < 220; x++)
            {
                b.DrawVLine(x, 161, 7, EgaColorIndex.BrightWhite);
                b.SetPixel(x, 160, EgaColorIndex.BrightCyan);
                b.SetPixel(x, 161, EgaColorIndex.Black);
            }

            // 1. Cozy Dome Igloo (x = 24..68, y = 134..160)
            for (int dx = -18; dx <= 18; dx++)
            {
                int domeY = 160 - (int)MathF.Sqrt(Math.Max(0, 18 * 18 - dx * dx));
                int h = 161 - domeY;
                b.DrawVLine(44 + dx, domeY, h, EgaColorIndex.BrightWhite);
                b.SetPixel(44 + dx, domeY, EgaColorIndex.Black);
            }
            // Igloo ice-block course seams
            for (int rowY = 148; rowY <= 156; rowY += 4)
            {
                b.DrawHLine(28, rowY, 32, EgaColorIndex.Cyan);
            }
            // Protruding Igloo arched tunnel & dark cozy doorway on right
            b.FillRect(56, 149, 11, 12, EgaColorIndex.BrightWhite);
            b.DrawRect(56, 149, 11, 12, EgaColorIndex.Black);
            b.FillRect(59, 152, 6, 9, EgaColorIndex.Black);
            b.SetPixel(61, 155, EgaColorIndex.Yellow); // Warm lantern glow inside

            // 2. Frosted Arctic Pines (x = 112 and x = 182)
            foreach (int tx in new[] { 112, 182 })
            {
                b.FillRect(tx - 2, 144, 5, 17, EgaColorIndex.Brown);
                b.DrawRect(tx - 2, 144, 5, 17, EgaColorIndex.Black);

                for (int tier = 0; tier < 3; tier++)
                {
                    int w = 18 - tier * 4;
                    int ty = 138 - tier * 10;
                    int left = tx - w / 2;
                    b.FillRect(left, ty, w, 9, EgaColorIndex.Green);
                    b.DrawRect(left, ty, w, 9, EgaColorIndex.Black);
                    // Thick snow cap resting on each evergreen tier
                    b.FillRect(left + 1, ty, w - 2, 3, EgaColorIndex.BrightWhite);
                    b.DrawHLine(left + 1, ty + 3, w - 2, EgaColorIndex.BrightCyan);
                }
            }

            // 3. Glacial Ice Pillar between pines (x = 148)
            b.FillRect(145, 140, 8, 21, EgaColorIndex.BrightCyan);
            b.FillRect(149, 142, 4, 19, EgaColorIndex.Cyan);
            b.DrawRect(145, 140, 8, 21, EgaColorIndex.Black);
            b.DrawHLine(146, 141, 6, EgaColorIndex.BrightWhite);
        });

    // =========================================================================
    // 2. Standard 16x16 Polar World Tiles
    // =========================================================================

    private static EgaSprite BakePolarSolidTile() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Glacial ice blocks below
            b.FillRect(0, 5, 16, 11, EgaColorIndex.Blue);
            b.FillRect(0, 5, 16, 5, EgaColorIndex.Cyan);
            // Ice block seams & crisp black outline
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            b.DrawHLine(1, 5, 14, EgaColorIndex.Black);
            b.DrawHLine(1, 10, 14, EgaColorIndex.Black);
            b.DrawVLine(8, 5, 6, EgaColorIndex.Black);
            b.DrawVLine(3, 10, 6, EgaColorIndex.Black);
            b.DrawVLine(12, 10, 6, EgaColorIndex.Black);

            // Crisp ice bevel glints
            b.DrawHLine(2, 6, 4, EgaColorIndex.BrightCyan);
            b.DrawHLine(10, 6, 4, EgaColorIndex.BrightCyan);
            b.DrawHLine(5, 11, 5, EgaColorIndex.BrightBlue);

            // Thick fluffy white snow cap on top (rows 0..4)
            b.FillRect(1, 1, 14, 3, EgaColorIndex.BrightWhite);
            b.DrawHLine(1, 0, 14, EgaColorIndex.BrightWhite);
            b.DrawHLine(1, 4, 14, EgaColorIndex.BrightCyan);
            // Wavy overhanging snow dips
            b.SetPixel(3, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(4, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(10, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(11, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(12, 5, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakePolarOneWayTile(int parity) =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Top snow-covered ice shelf (y=0..6)
            b.FillRect(0, 1, 16, 3, EgaColorIndex.BrightWhite);
            b.DrawHLine(0, 0, 16, EgaColorIndex.Black);
            b.FillRect(0, 4, 16, 2, EgaColorIndex.BrightCyan);
            b.DrawHLine(0, 6, 16, EgaColorIndex.Black);
            b.DrawHLine(0, 5, 16, EgaColorIndex.Cyan);

            // Crisp hanging ice icicles underneath (y=7..15)
            int[] xs = parity == 0 ? new[] { 2, 7, 12 } : new[] { 1, 6, 11 };
            int[] hs = parity == 0 ? new[] { 8, 6, 9 }  : new[] { 7, 9, 5 };

            for (int i = 0; i < 3; i++)
            {
                int ix = xs[i];
                int ih = hs[i];
                // Base (3px wide at y=7..9), narrow middle (2px), sharp tip (1px)
                for (int dy = 0; dy < ih; dy++)
                {
                    int py = 7 + dy;
                    if (py >= 16) break;
                    int half = dy < 2 ? 2 : (dy < ih - 2 ? 1 : 0);
                    b.SetPixel(ix - 1, py, EgaColorIndex.Black);
                    b.SetPixel(ix + half + 1, py, EgaColorIndex.Black);
                    for (int w = 0; w <= half; w++)
                    {
                        b.SetPixel(ix + w, py, w == 0 ? EgaColorIndex.BrightWhite : EgaColorIndex.BrightCyan);
                    }
                    if (dy == ih - 1)
                    {
                        b.SetPixel(ix, py + 1, EgaColorIndex.Black);
                    }
                }
            }
        });

    private static EgaSprite BakeIceSpikesHazard() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Thin frozen ice floor base at bottom
            b.FillRect(0, 13, 16, 3, EgaColorIndex.Blue);
            b.DrawHLine(0, 13, 16, EgaColorIndex.Cyan);
            b.DrawRect(0, 13, 16, 3, EgaColorIndex.Black);

            // 3 Sharp jagged crystal ice spikes rising from bottom
            int[] peaks = { 2, 7, 12 };
            int[] tops  = { 3, 1, 4 };

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
                        EgaColorIndex col = dx <= 0 ? EgaColorIndex.BrightWhite : EgaColorIndex.BrightCyan;
                        if (y > ty + 5 && dx > 0) col = EgaColorIndex.Cyan;
                        b.SetPixel(cx + dx, y, col);
                    }
                }
                b.SetPixel(cx, ty - 1, EgaColorIndex.Black);
            }
        });

    private static EgaSprite BakeFrostStoneBorder() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Dark chiseled stone bedrock base
            b.FillRect(0, 0, 16, 16, EgaColorIndex.DarkGray);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            b.DrawRect(2, 3, 12, 11, EgaColorIndex.LightGray);
            b.FillRect(3, 4, 10, 9, EgaColorIndex.DarkGray);
            b.DrawHLine(4, 8, 8, EgaColorIndex.Black);
            b.DrawVLine(8, 4, 4, EgaColorIndex.Black);
            b.DrawVLine(6, 9, 4, EgaColorIndex.Black);

            // Crystalline frost & snow clinging across top & upper-left edges
            b.FillRect(1, 1, 14, 2, EgaColorIndex.BrightWhite);
            b.DrawHLine(1, 3, 14, EgaColorIndex.BrightCyan);
            b.FillRect(1, 3, 2, 6, EgaColorIndex.BrightWhite);
            b.DrawVLine(3, 3, 5, EgaColorIndex.BrightCyan);
            b.SetPixel(13, 4, EgaColorIndex.BrightWhite);
            b.SetPixel(14, 4, EgaColorIndex.BrightCyan);
        });

    private static EgaSprite BakeSnowyLadder() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Two vertical frosted ice/wood rails
            b.FillRect(2, 0, 3, 16, EgaColorIndex.Cyan);
            b.DrawVLine(2, 0, 16, EgaColorIndex.Black);
            b.DrawVLine(4, 0, 16, EgaColorIndex.Black);
            b.DrawVLine(3, 0, 16, EgaColorIndex.BrightBlue);

            b.FillRect(11, 0, 3, 16, EgaColorIndex.Cyan);
            b.DrawVLine(11, 0, 16, EgaColorIndex.Black);
            b.DrawVLine(13, 0, 16, EgaColorIndex.Black);
            b.DrawVLine(12, 0, 16, EgaColorIndex.BrightBlue);

            // Snowy ladder rungs every 4px (y = 1, 5, 9, 13)
            for (int ry = 1; ry < 16; ry += 4)
            {
                b.DrawHLine(2, ry, 12, EgaColorIndex.BrightWhite);     // Snowy step top
                b.DrawHLine(2, ry + 1, 12, EgaColorIndex.BrightCyan);  // Icy step core
                b.DrawHLine(2, ry + 2, 12, EgaColorIndex.Black);       // Crisp outline
                // Tiny frost flecks on rails
                b.SetPixel(3, ry, EgaColorIndex.BrightWhite);
                b.SetPixel(12, ry, EgaColorIndex.BrightWhite);
            }
        });

    private static EgaSprite BakeSnowBlockCrate() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Packed destructible block of snow
            b.FillRect(1, 1, 14, 14, EgaColorIndex.BrightWhite);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            // Crisp cyan inner shadow bevel on bottom & right
            b.DrawHLine(1, 14, 14, EgaColorIndex.BrightCyan);
            b.DrawVLine(14, 1, 14, EgaColorIndex.BrightCyan);
            b.DrawRect(2, 2, 12, 12, EgaColorIndex.LightGray);
            // Diagonal ice-seam brace so it reads cleanly as a snow crate
            for (int i = 3; i <= 12; i++)
            {
                b.SetPixel(i, i, EgaColorIndex.Cyan);
                b.SetPixel(i, 15 - i, EgaColorIndex.BrightCyan);
            }
        });

    // =========================================================================
    // 3. Player: Smaller Penguin with Orange Beak throwing Snowballs (24x24, OffsetX=-5)
    // =========================================================================

    private static EgaSprite BakePenguinAction(int legStep, int throwBob, bool wingUp) =>
        EgaSprite.Bake(24, 24, b =>
        {
            const int ox = 5;

            // Smaller chubby cute penguin (y = 6..23) so he feels compact & lovable!
            // 1. Rounded Penguin Head (y=6..12)
            b.FillRect(ox + 3, 6, 8, 7, EgaColorIndex.DarkGray);
            b.DrawRect(ox + 3, 6, 8, 7, EgaColorIndex.Black);
            b.DrawHLine(ox + 4, 6, 6, EgaColorIndex.Black);
            b.SetTransparent(ox + 3, 6);
            b.SetTransparent(ox + 10, 6);

            // Cute white cheek patch & crisp dark eye
            b.FillRect(ox + 7, 8, 3, 3, EgaColorIndex.BrightWhite);
            b.SetPixel(ox + 8, 9, EgaColorIndex.Black);

            // Cheerful Bright Orange Beak protruding right (Yellow top + Brown bottom)
            b.FillRect(ox + 10, 9, 4, 2, EgaColorIndex.Brown);
            b.DrawHLine(ox + 11, 9, 3, EgaColorIndex.Yellow);
            b.DrawHLine(ox + 11, 11, 3, EgaColorIndex.Black);
            b.SetPixel(ox + 14, 10, EgaColorIndex.Black);

            // 2. Chubby Penguin Body (y=12..20)
            b.FillRect(ox + 2, 12, 9, 9, EgaColorIndex.DarkGray);
            b.DrawRect(ox + 2, 12, 9, 9, EgaColorIndex.Black);
            // Plump snow-white belly in front
            b.FillRect(ox + 6, 13, 4, 7, EgaColorIndex.BrightWhite);

            // 3. Penguin Flipper Wing + Held Snowball aimed right
            int armY = wingUp ? 11 : (13 + throwBob);
            b.FillRect(ox + 3, armY, 6, 4, EgaColorIndex.DarkGray);
            b.DrawRect(ox + 3, armY, 6, 4, EgaColorIndex.Black);
            b.DrawHLine(ox + 4, armY + 1, 4, EgaColorIndex.Black);

            // Snowball gripped in flipper tip ready to throw!
            int ballX = ox + 10;
            int ballY = wingUp ? 10 : (13 + throwBob);
            b.FillRect(ballX, ballY, 4, 4, EgaColorIndex.BrightWhite);
            b.DrawRect(ballX, ballY, 4, 4, EgaColorIndex.Black);
            b.SetPixel(ballX + 1, ballY + 2, EgaColorIndex.BrightCyan);
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
            // Rear climbing view matching the compact penguin height (y=6..23)
            b.FillRect(3, 6, 8, 7, EgaColorIndex.DarkGray);
            b.DrawRect(3, 6, 8, 7, EgaColorIndex.Black);
            b.SetTransparent(3, 6);
            b.SetTransparent(10, 6);

            // Plump black back
            b.FillRect(2, 12, 10, 9, EgaColorIndex.DarkGray);
            b.DrawRect(2, 12, 10, 9, EgaColorIndex.Black);
            b.FillRect(4, 13, 6, 7, EgaColorIndex.Black);

            // Alternating flippers grabbing ladder rungs
            int leftFlipperY  = 11 - (step * 2);
            int rightFlipperY = 11 + (step * 2);
            b.FillRect(0, leftFlipperY, 3, 5, EgaColorIndex.DarkGray);
            b.DrawRect(0, leftFlipperY, 3, 5, EgaColorIndex.Black);
            b.FillRect(11, rightFlipperY, 3, 5, EgaColorIndex.DarkGray);
            b.DrawRect(11, rightFlipperY, 3, 5, EgaColorIndex.Black);

            // Alternating orange feet
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
    // 4. Monsters: Walrus, Christmas Reindeer, Ceiling Seal, Elk, Polar Bear
    // =========================================================================

    private static EgaSprite BakeWalrus(int step) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Small 1x1 Patrol Monster: Plump Walrus with big white tusks (facing Right)
            b.FillRect(1, 5, 10, 7, EgaColorIndex.Brown);
            b.DrawRect(1, 5, 10, 7, EgaColorIndex.Black);

            // Rounded head & whiskered muzzle on right
            b.FillRect(7, 2, 6, 7, EgaColorIndex.Brown);
            b.DrawRect(7, 2, 6, 7, EgaColorIndex.Black);
            b.FillRect(10, 5, 3, 3, EgaColorIndex.LightGray);
            b.SetPixel(10, 3, EgaColorIndex.Black);     // Dark eye
            b.SetPixel(9, 3, EgaColorIndex.BrightWhite); // Eye glint

            // Prominent curved white tusks pointing down!
            b.FillRect(10, 8, 2, 4, EgaColorIndex.BrightWhite);
            b.DrawVLine(10, 8, 4, EgaColorIndex.BrightWhite);
            b.SetPixel(9, 9, EgaColorIndex.Black);
            b.SetPixel(12, 8, EgaColorIndex.BrightWhite);
            b.SetPixel(12, 9, EgaColorIndex.BrightWhite);
            b.SetPixel(12, 10, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 8, EgaColorIndex.Black);

            // Waddling flippers
            int off = step == 0 ? 0 : 1;
            b.FillRect(2 + off, 11, 3, 3, EgaColorIndex.DarkGray);
            b.DrawRect(2 + off, 11, 3, 3, EgaColorIndex.Black);
            b.FillRect(7 - off, 11, 3, 3, EgaColorIndex.DarkGray);
            b.DrawRect(7 - off, 11, 3, 3, EgaColorIndex.Black);
        });

    private static EgaSprite BakeHolidayReindeer(bool glint) =>
        EgaSprite.Bake(14, 12, b =>
        {
            // Flying Shooter: Holiday-decorated Christmas Reindeer shooting Snowballs!
            // Golden antlers rising at top
            b.DrawVLine(9, 0, 3, EgaColorIndex.Yellow);
            b.SetPixel(8, 0, EgaColorIndex.Yellow);
            b.DrawVLine(11, 0, 3, EgaColorIndex.Yellow);
            b.SetPixel(12, 1, EgaColorIndex.Yellow);

            // Leaping reindeer torso
            b.FillRect(2, 4, 8, 5, EgaColorIndex.Brown);
            b.DrawRect(2, 4, 8, 5, EgaColorIndex.Black);
            b.SetPixel(1, 5, EgaColorIndex.BrightWhite); // Fluffy tail

            // Reindeer head & bright glowing Red Rudolph nose!
            b.FillRect(8, 2, 5, 4, EgaColorIndex.Brown);
            b.DrawRect(8, 2, 5, 4, EgaColorIndex.Black);
            b.SetPixel(10, 3, EgaColorIndex.Black);     // Eye
            b.SetPixel(13, 4, glint ? EgaColorIndex.BrightRed : EgaColorIndex.Yellow); // Glowing Red Nose!

            // Festive green Christmas collar + golden bell!
            b.FillRect(8, 5, 2, 3, EgaColorIndex.BrightGreen);
            b.SetPixel(10, 7, EgaColorIndex.Yellow);

            // Flying prancing hooves & magic sparkle dust
            int legLift = glint ? 1 : 0;
            b.FillRect(2, 9 - legLift, 2, 3, EgaColorIndex.Black);
            b.FillRect(8, 8 + legLift, 2, 3, EgaColorIndex.Black);
            if (glint)
            {
                b.SetPixel(0, 7, EgaColorIndex.BrightCyan);
                b.SetPixel(5, 10, EgaColorIndex.Yellow);
            }
        });

    private static EgaSprite BakeCeilingSeal(bool throwing) =>
        EgaSprite.Bake(14, 12, b =>
        {
            // Ceiling Shooter (14x12): Sleek Spotted Arctic Seal clinging to ceiling & dropping Snowflakes
            // Wide sleek seal torso & tail fin gripping the ceiling
            b.FillRect(2, 0, 10, 5, EgaColorIndex.LightGray);
            b.DrawHLine(2, 0, 10, EgaColorIndex.Black);
            b.DrawVLine(2, 0, 5, EgaColorIndex.Black);
            b.DrawVLine(11, 0, 5, EgaColorIndex.Black);

            // Mid-side flippers bracing outward (no ear silhouette!)
            b.FillRect(0, 2, 3, 3, EgaColorIndex.DarkGray);
            b.DrawRect(0, 2, 3, 3, EgaColorIndex.Black);
            b.FillRect(11, 2, 3, 3, EgaColorIndex.DarkGray);
            b.DrawRect(11, 2, 3, 3, EgaColorIndex.Black);

            // Rounded seal head peaking down with bright white muzzle
            b.FillRect(3, 4, 8, 5, EgaColorIndex.LightGray);
            b.DrawRect(3, 4, 8, 5, EgaColorIndex.Black);
            b.SetTransparent(3, 8);
            b.SetTransparent(10, 8);
            b.FillRect(5, 6, 4, 2, EgaColorIndex.BrightWhite);

            // Seal spots, big glossy eyes, nose & long whiskers
            b.SetPixel(4, 2, EgaColorIndex.DarkGray);
            b.SetPixel(9, 1, EgaColorIndex.DarkGray);
            b.SetPixel(5, 5, EgaColorIndex.Black);
            b.SetPixel(8, 5, EgaColorIndex.Black);
            b.DrawHLine(6, 7, 2, EgaColorIndex.Black);
            b.SetPixel(3, 7, EgaColorIndex.BrightWhite);
            b.SetPixel(10, 7, EgaColorIndex.BrightWhite);

            // Dropping crisp crystal snowflake downward
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

    private static EgaSprite BakeElk(int step) =>
        EgaSprite.Bake(24, 14, b =>
        {
            // 2x1 Fast Floor Runner: Charging Elk with tall antlers!
            // Large branching antlers at upper right (x=15..22, y=0..3)
            b.DrawVLine(18, 0, 4, EgaColorIndex.Yellow);
            b.SetPixel(17, 1, EgaColorIndex.Yellow);
            b.SetPixel(16, 0, EgaColorIndex.Brown);
            b.SetPixel(19, 1, EgaColorIndex.Yellow);
            b.SetPixel(20, 0, EgaColorIndex.Yellow);
            b.SetPixel(21, 1, EgaColorIndex.Brown);

            // Sleek elongated brown elk body
            b.FillRect(3, 4, 13, 6, EgaColorIndex.Brown);
            b.DrawRect(3, 4, 13, 6, EgaColorIndex.Black);
            b.FillRect(1, 5, 3, 2, EgaColorIndex.LightGray); // Pale short tail
            b.FillRect(13, 5, 3, 4, EgaColorIndex.LightGray); // Thick neck ruff

            // Long elk head & dark muzzle on right
            b.FillRect(16, 3, 7, 5, EgaColorIndex.Brown);
            b.DrawRect(16, 3, 7, 5, EgaColorIndex.Black);
            b.SetPixel(19, 4, EgaColorIndex.Black);      // Eye
            b.SetPixel(18, 4, EgaColorIndex.BrightWhite);
            b.FillRect(21, 5, 2, 2, EgaColorIndex.DarkGray); // Snout

            // Fast galloping slender legs with black hooves
            b.FillRect(4, 10 - (step / 2), 2, 4, EgaColorIndex.Brown);
            b.SetPixel(4, 13, EgaColorIndex.Black);
            b.SetPixel(5, 13, EgaColorIndex.Black);
            b.FillRect(14, 10 + (step / 2), 2, 4, EgaColorIndex.Brown);
            b.SetPixel(14, 13, EgaColorIndex.Black);
            b.SetPixel(15, 13, EgaColorIndex.Black);
        });

    private static EgaSprite BakePolarBear(int stride) =>
        EgaSprite.Bake(22, 26, b =>
        {
            const int ox = 4;

            // Big 1x2 Heavy Monster: White Polar Bear throwing Snowballs!
            // Round ears
            b.FillRect(ox + 2, 0, 3, 3, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 2, 0, 3, 3, EgaColorIndex.Black);
            b.FillRect(ox + 9, 0, 3, 3, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 9, 0, 3, 3, EgaColorIndex.Black);

            // Large white bear head with crisp black silhouette
            b.FillRect(ox + 2, 1, 9, 7, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 2, 1, 9, 7, EgaColorIndex.Black);
            b.SetPixel(ox + 8, 3, EgaColorIndex.Black);       // Eye
            b.FillRect(ox + 10, 4, 3, 3, EgaColorIndex.LightGray); // Snout
            b.DrawRect(ox + 10, 4, 3, 3, EgaColorIndex.Black);
            b.SetPixel(ox + 12, 4, EgaColorIndex.Black);      // Nose tip

            // Massive white fur shoulders & torso with subtle ice-blue/gray shading
            b.FillRect(ox + 1, 7, 12, 12, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 1, 7, 12, 12, EgaColorIndex.Black);
            b.FillRect(ox + 3, 14, 8, 4, EgaColorIndex.LightGray);

            // Outstretched heavy paw holding a Snowball to hurl!
            b.FillRect(ox + 10, 10, 6, 4, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 10, 10, 6, 4, EgaColorIndex.Black);
            b.FillRect(ox + 15, 9, 5, 5, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 15, 9, 5, 5, EgaColorIndex.Black);
            b.SetPixel(ox + 16, 12, EgaColorIndex.BrightCyan); // Snowball shading

            // Heavy bear legs & dark claws
            b.FillRect(ox + 2, 19 + stride, 4, 7, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 2, 19 + stride, 4, 7, EgaColorIndex.Black);
            b.DrawHLine(ox + 3, 24 + stride, 2, EgaColorIndex.LightGray);

            b.FillRect(ox + 8, 19 - stride, 4, 7, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 8, 19 - stride, 4, 7, EgaColorIndex.Black);
            b.DrawHLine(ox + 9, 24 - stride, 2, EgaColorIndex.LightGray);
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
            // Symmetrical crystal snowflake for Ceiling Seal!
            // Subtle dark/cyan outer anchor pixels so it stays crisp against bright sky or snow
            b.DrawSparkle(3, 3, EgaColorIndex.Cyan);
            b.DrawHLine(0, 3, 7, EgaColorIndex.BrightCyan);
            b.DrawVLine(3, 0, 7, EgaColorIndex.BrightCyan);
            // Diagonal crystal arms
            b.SetPixel(1, 1, EgaColorIndex.BrightWhite);
            b.SetPixel(5, 1, EgaColorIndex.BrightWhite);
            b.SetPixel(1, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(5, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(2, 2, EgaColorIndex.Cyan);
            b.SetPixel(4, 2, EgaColorIndex.Cyan);
            b.SetPixel(2, 4, EgaColorIndex.Cyan);
            b.SetPixel(4, 4, EgaColorIndex.Cyan);
            // Crisp white core & cardinal tips
            b.SetPixel(3, 3, EgaColorIndex.BrightWhite);
            b.SetPixel(3, 0, EgaColorIndex.BrightWhite);
            b.SetPixel(3, 6, EgaColorIndex.BrightWhite);
            b.SetPixel(0, 3, EgaColorIndex.BrightWhite);
            b.SetPixel(6, 3, EgaColorIndex.BrightWhite);
        });

    // =========================================================================
    // 6. Pickups & Powerup Badges (14x14): Triangles, Fish Box & Polar Powerups
    // =========================================================================

    private static EgaSprite BakeTriangleGem(EgaColorIndex bodyCol, EgaColorIndex shadowCol, EgaColorIndex glintCol, bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Crisp equilateral triangle pickup (Turquoise, Dark Green, or Golden!)
            // Rows y=2..11 expanding symmetrically around x=6..7
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
                    b.SetPixel(px, py, r >= 8 ? shadowCol : bodyCol);
                }
            }
            // Top & bottom triangle outline
            b.SetPixel(6, 1, EgaColorIndex.Black);
            b.SetPixel(7, 1, EgaColorIndex.Black);
            b.DrawHLine(2, 12, 10, EgaColorIndex.Black);

            // Animated inner facet highlight / sparkle
            b.SetPixel(6, 4, glintCol);
            b.SetPixel(6, 5, glintCol);
            if (glint)
            {
                b.SetPixel(7, 5, glintCol);
                b.SetPixel(5, 6, glintCol);
            }
        });

    private static EgaSprite BakeFishMedKit(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Health Case: Box with a clear Fish icon on it!
            b.FillRect(1, 2, 12, 10, EgaColorIndex.BrightWhite);
            b.DrawRect(1, 2, 12, 10, EgaColorIndex.Black);
            b.DrawHLine(2, 10, 10, EgaColorIndex.LightGray);
            // Case top handle
            b.FillRect(5, 0, 4, 2, EgaColorIndex.Cyan);
            b.DrawRect(5, 0, 4, 2, EgaColorIndex.Black);

            // Clear Fish Icon centered inside box (x=3..10, y=5..8)
            EgaColorIndex fishCol = glint ? EgaColorIndex.BrightCyan : EgaColorIndex.Blue;
            b.FillRect(5, 5, 5, 4, fishCol);
            b.DrawHLine(6, 4, 3, fishCol);
            // Forked fish tail on left
            b.DrawVLine(3, 5, 4, fishCol);
            b.SetPixel(4, 6, fishCol);
            b.SetPixel(4, 7, fishCol);
            // Dark fish eye on right
            b.SetPixel(8, 6, EgaColorIndex.Black);
        });

    public static EgaSprite BakePolarRapidBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Shoot Faster Powerup: Bigger Snowball with an outer Golden Circle which glows!
            for (int y = 0; y < 14; y++)
            {
                for (int x = 0; x < 14; x++)
                {
                    float dx = x - 6.5f;
                    float dy = y - 6.5f;
                    float r2 = (dx * dx) + (dy * dy);

                    if (r2 <= 9.5f)
                    {
                        // Crisp white snowball core with icy cyan/gray bottom rim
                        EgaColorIndex color = dy > 1.8f ? EgaColorIndex.BrightCyan
                            : (dx > 1.6f && dy > 0.5f) ? EgaColorIndex.LightGray
                            : EgaColorIndex.BrightWhite;
                        b.SetPixel(x, y, color);
                    }
                    else if (r2 <= 16.5f)
                    {
                        // 1px black silhouette around the snowball
                        b.SetPixel(x, y, EgaColorIndex.Black);
                    }
                    else if (r2 <= 32.5f)
                    {
                        // Outer golden circle
                        EgaColorIndex ringColor = r2 >= 26.5f ? EgaColorIndex.Yellow : EgaColorIndex.Brown;
                        if (dy < 0 && r2 >= 21.0f) ringColor = EgaColorIndex.Yellow;
                        b.SetPixel(x, y, ringColor);
                    }
                }
            }

            // Glowing outer cardinal & diagonal sparkles around the golden circle
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
            // Jump Higher Powerup: Penguin Black Wing which glows!
            // Glowing golden/cyan halo sparkles surrounding wing silhouette
            b.SetPixel(2, 1, EgaColorIndex.Yellow);
            b.SetPixel(6, 0, EgaColorIndex.BrightWhite);
            b.SetPixel(10, 2, EgaColorIndex.Yellow);
            b.SetPixel(12, 6, EgaColorIndex.BrightCyan);
            b.SetPixel(11, 11, EgaColorIndex.Yellow);
            b.SetPixel(4, 12, EgaColorIndex.BrightCyan);
            b.SetPixel(0, 6, EgaColorIndex.BrightCyan);

            // Crisp glowing golden edge halo directly framing the black wing
            b.FillRect(3, 2, 4, 4, EgaColorIndex.Yellow);
            b.FillRect(4, 4, 5, 5, EgaColorIndex.Yellow);
            b.FillRect(6, 6, 5, 5, EgaColorIndex.Yellow);

            // Sleek curved Black Penguin Wing sweeping diagonally down-right
            b.FillRect(3, 3, 3, 3, EgaColorIndex.Black);
            b.FillRect(4, 4, 4, 4, EgaColorIndex.Black);
            b.FillRect(6, 6, 4, 4, EgaColorIndex.Black);
            b.FillRect(8, 8, 3, 3, EgaColorIndex.Black);
            b.SetPixel(10, 10, EgaColorIndex.Black);

            // Subtle dark-gray feather sheen & blue edge highlight so wing pops boldly
            b.SetPixel(4, 4, EgaColorIndex.DarkGray);
            b.SetPixel(5, 5, EgaColorIndex.DarkGray);
            b.SetPixel(7, 7, EgaColorIndex.DarkGray);
            b.SetPixel(8, 8, EgaColorIndex.BrightBlue);
        });

    public static EgaSprite BakePolarSpeedBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Speed Dash Powerup: Classic Wooden & Red Sled!
            // Red curved metal runners along bottom with upturned front bow
            b.DrawHLine(2, 10, 9, EgaColorIndex.BrightRed);
            b.DrawHLine(2, 11, 9, EgaColorIndex.Black);
            b.SetPixel(11, 9, EgaColorIndex.BrightRed);
            b.SetPixel(12, 8, EgaColorIndex.BrightRed);
            b.SetPixel(12, 7, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 8, EgaColorIndex.Black);
            b.SetPixel(12, 10, EgaColorIndex.Black);

            // Struts supporting seat
            b.DrawVLine(4, 7, 3, EgaColorIndex.DarkGray);
            b.DrawVLine(9, 7, 3, EgaColorIndex.DarkGray);

            // Wooden seat deck
            b.FillRect(2, 5, 9, 2, EgaColorIndex.Brown);
            b.DrawHLine(2, 4, 9, EgaColorIndex.Black);
            b.DrawHLine(3, 5, 7, EgaColorIndex.Yellow); // Wood slat highlight
            b.DrawHLine(2, 7, 9, EgaColorIndex.Black);
            b.SetPixel(1, 5, EgaColorIndex.Black);
            b.SetPixel(11, 5, EgaColorIndex.Black);

            // Speed wind streaks behind sled
            b.DrawHLine(0, 3, 2, EgaColorIndex.BrightCyan);
            b.DrawHLine(0, 8, 2, EgaColorIndex.BrightWhite);
        });

    public static EgaSprite BakePolarInvulnBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Invulnerability Powerup: Bigger & Golden Fish! (14x14)
            // Golden fish silhouette outlined crisply in Black
            // Dorsal fin on top
            b.FillRect(6, 1, 3, 2, EgaColorIndex.Yellow);
            b.DrawHLine(6, 0, 3, EgaColorIndex.Black);
            b.SetPixel(5, 1, EgaColorIndex.Black);
            b.SetPixel(9, 1, EgaColorIndex.Black);

            // Plump golden body (x=4..12, y=3..10)
            b.FillRect(4, 3, 8, 8, EgaColorIndex.Yellow);
            b.DrawHLine(5, 2, 6, EgaColorIndex.Black);
            b.DrawHLine(5, 11, 6, EgaColorIndex.Black);
            b.DrawVLine(12, 4, 6, EgaColorIndex.Black);
            b.DrawVLine(13, 5, 4, EgaColorIndex.Yellow);
            b.SetPixel(13, 4, EgaColorIndex.Black);
            b.SetPixel(13, 9, EgaColorIndex.Black);

            // Warm gold belly & fin shading
            b.DrawHLine(5, 9, 6, EgaColorIndex.Brown);
            b.DrawHLine(6, 10, 4, EgaColorIndex.Brown);

            // Forked golden tail firmly joined on left (x=0..4)
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

            // Crisp black fish eye & shiny bright-white gleam
            b.FillRect(9, 4, 2, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(10, 5, EgaColorIndex.Black);
            b.SetPixel(6, 4, EgaColorIndex.BrightWhite);
            b.SetPixel(7, 4, EgaColorIndex.BrightWhite);
        });
}
