using System;
using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// "Underwater" Theme (Deep Ocean & Sea Creatures):
/// A complete graphical and gameplay overhaul set in the ocean abyss.
/// Features a Deep-Sea Diver in brass helmet and flippers with a pneumatic speargun,
/// submerged Atlantean temple ruins, woven kelp platforms, sea urchins, and ocean predators
/// (Hermit Crabs, Bioluminescent Jellyfish, Ceiling Sea Anemones, Hunting Sharks, and Giant Anglerfish).
/// In this theme, zero-gravity physics is inherently active, granting full 4-way swimming freedom!
/// </summary>
public sealed class UnderwaterTheme : SpriteThemeBase
{
    public const string ThemeId = "underwater";
    public const string DisplayName = "Underwater";

    public override bool ZeroGravity => true;
    public override string BgmTrack => "audio/bgm_underwater.wav";

    public static readonly ThemeAssetPack StaticAssets = LoadOrBakeAssets();

    public override ThemeAssetPack Assets => StaticAssets;

    private static ThemeAssetPack LoadOrBakeAssets()
    {
        try
        {
            return EgaPngCodec.LoadThemePack(ThemeId, DisplayName, zeroGravity: true);
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
            ZeroGravity = true,

            // 1. Standard 3-Layer Horizontally-Repeating Parallax Background Images (Height: 168px)
            BackgroundFar = BakeOceanFarLayer(),
            BackgroundMid = BakeOceanMidLayer(),
            BackgroundNear = BakeOceanNearLayer(),

            // 2. Standard 16x16 Ocean World Tiles
            SolidTile = BakeSunkenTempleStone(),
            OneWayTileVariants = new[]
            {
                BakeCoralKelpPlatform(parity: 0),
                BakeCoralKelpPlatform(parity: 1)
            },
            HazardTile = BakeSeaUrchins(),
            BorderTile = BakeBasaltHull(),
            LadderTile = BakeAlgaeVineLadder(),
            CrateTile = BakeTreasureChest(),

            // 3. Player: Deep-Sea Scuba Diver (24x24 with OffsetX = -5; Climb 14x24)
            PlayerIdle = BakeDiverAction(finKick: 0, gunBob: 0, isHoverGlide: false),
            PlayerWalk = new[]
            {
                BakeDiverAction(finKick: 2, gunBob: 0, isHoverGlide: false),
                BakeDiverAction(finKick: 1, gunBob: 1, isHoverGlide: false),
                BakeDiverAction(finKick: -2, gunBob: 0, isHoverGlide: false),
                BakeDiverAction(finKick: -1, gunBob: 1, isHoverGlide: false)
            },
            PlayerJump = BakeDiverAction(finKick: 0, gunBob: 0, isHoverGlide: true),
            PlayerClimb = new[]
            {
                BakeDiverClimb(step: 1),
                BakeDiverClimb(step: -1)
            },

            // 4. Monsters: Deep-Sea Creatures & Ocean Predators (100% Swimming Creatures, Zero Walking Legs)
            PatrolDroid = new[]
            {
                BakeSwimmingManta(stroke: false),
                BakeSwimmingManta(stroke: true)
            },
            HoverDrone = new[]
            {
                BakeBioluminescentJellyfish(pulse: false),
                BakeBioluminescentJellyfish(pulse: true)
            },
            CeilingTurret = new[]
            {
                BakeSeaAnemoneTurret(open: false),
                BakeSeaAnemoneTurret(open: true)
            },
            MechaHound = new[]
            {
                BakeHuntingShark(step: 2),
                BakeHuntingShark(step: -2)
            },
            HeavyGuard = new[]
            {
                BakeAbyssalAnglerfish(jawOpen: 0),
                BakeAbyssalAnglerfish(jawOpen: 1)
            },

            // 5. Projectiles: Speargun Harpoon & Bio-electric Venom Orb
            PlayerProjectile = BakeSpeargunHarpoon(),
            MonsterProjectile = BakeBioElectricOrb(),

            // 6. Pickups: Sea Pearl, Red Coral Ruby, Gold Doubloon & Scuba Oxygen Tank
            BlueGem = new[]
            {
                BakeSeaPearl(glint: false),
                BakeSeaPearl(glint: true)
            },
            RedRuby = new[]
            {
                BakeCoralRuby(glint: false),
                BakeCoralRuby(glint: true)
            },
            GoldCartridge = new[]
            {
                BakeGoldDoubloon(glint: false),
                BakeGoldDoubloon(glint: true)
            },
            MedKit = new[]
            {
                BakeOxygenTank(glint: false),
                BakeOxygenTank(glint: true)
            },

            // Powerup HUD Badges (14x14)
            RapidFireBadge = BakeUnderwaterBadge("R", EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite),
            HighJumpBadge = BakeUnderwaterBadge("J", EgaColorIndex.BrightBlue, EgaColorIndex.Yellow),
            SpeedDashBadge = BakeUnderwaterBadge("S", EgaColorIndex.BrightGreen, EgaColorIndex.BrightCyan)
        };
    }

    // =========================================================================
    // 1. Horizontally-Repeating Parallax Background Layers (Height: 168px)
    // =========================================================================

    private static EgaSprite BakeOceanFarLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Ocean Abyss Depth Gradient (320x168)
            // Top rows (0..35): Sunlit ocean surface water (Cyan & Bright Blue dither)
            for (int y = 0; y < 35; y++)
            {
                for (int x = 0; x < 320; x++)
                {
                    bool dither = ((x + y) % 2 == 0);
                    b.SetPixel(x, y, dither ? EgaColorIndex.Cyan : EgaColorIndex.BrightBlue);
                }
            }

            // Mid rows (35..90): Deep Blue ocean water
            b.FillRect(0, 35, 320, 55, EgaColorIndex.Blue);

            // Deep rows (90..167): Abyssal transition into Black
            for (int y = 90; y < 140; y++)
            {
                int density = (y - 90) / 7;
                for (int x = 0; x < 320; x++)
                {
                    bool isBlack = ((x ^ (y * 3)) % 8) < density;
                    b.SetPixel(x, y, isBlack ? EgaColorIndex.Black : EgaColorIndex.Blue);
                }
            }
            b.FillRect(0, 140, 320, 28, EgaColorIndex.Black);

            // Diagonal Sunbeams / Light Caustics slicing down from top (rows 0..80)
            int[] rayPositions = { 35, 115, 195, 275 };
            foreach (int rx in rayPositions)
            {
                for (int y = 0; y < 80; y++)
                {
                    int xOffset = (y * 3) / 4;
                    int beamWidth = 14 - (y * 10) / 80;
                    for (int bw = 0; bw < beamWidth; bw++)
                    {
                        int px = (rx + xOffset + bw) % 320;
                        if ((px + y) % 3 == 0)
                        {
                            b.SetPixel(px, y, EgaColorIndex.BrightCyan);
                        }
                    }
                }
            }

            // Distant Blue Whale silhouette cruising at y: 45..62 (x: 130..195)
            b.FillRect(145, 50, 40, 8, EgaColorIndex.DarkGray);
            b.FillRect(152, 48, 26, 12, EgaColorIndex.DarkGray);
            b.FillRect(140, 52, 8, 4, EgaColorIndex.DarkGray);
            // Fluke tail
            b.SetPixel(132, 49, EgaColorIndex.DarkGray);
            b.SetPixel(133, 50, EgaColorIndex.DarkGray);
            b.FillRect(134, 51, 6, 2, EgaColorIndex.DarkGray);
            b.SetPixel(133, 53, EgaColorIndex.DarkGray);
            b.SetPixel(132, 54, EgaColorIndex.DarkGray);
            // Whale dorsal fin & belly
            b.FillRect(165, 46, 5, 2, EgaColorIndex.DarkGray);
            b.DrawHLine(150, 59, 28, EgaColorIndex.Blue);
            // Blowhole micro-bubbles
            b.SetPixel(172, 44, EgaColorIndex.BrightWhite);
            b.SetPixel(173, 42, EgaColorIndex.BrightCyan);

            // Distant Sunken Atlantean Ruins / Colonnade along y: 100..167
            for (int colX = 15; colX < 320; colX += 45)
            {
                // Stone pillar
                b.FillRect(colX, 105, 10, 63, EgaColorIndex.Blue);
                b.DrawVLine(colX, 105, 63, EgaColorIndex.DarkGray);
                b.DrawVLine(colX + 9, 105, 63, EgaColorIndex.Black);
                // Capital / architrave top
                b.FillRect(colX - 3, 101, 16, 4, EgaColorIndex.DarkGray);
            }
            // Continuous broken entablature across columns
            b.FillRect(0, 98, 320, 4, EgaColorIndex.DarkGray);
            b.DrawHLine(0, 98, 320, EgaColorIndex.Blue);

            // Floating air bubbles in background
            var bubbles = new (int X, int Y, EgaColorIndex C)[]
            {
                (22, 28, EgaColorIndex.BrightWhite), (75, 45, EgaColorIndex.BrightCyan),
                (140, 72, EgaColorIndex.BrightWhite), (210, 30, EgaColorIndex.BrightCyan),
                (265, 60, EgaColorIndex.BrightWhite), (305, 85, EgaColorIndex.BrightCyan),
                (50, 115, EgaColorIndex.BrightCyan), (180, 130, EgaColorIndex.BrightWhite)
            };
            foreach (var bub in bubbles)
            {
                b.SetPixel(bub.X, bub.Y, bub.C);
                b.SetPixel(bub.X, bub.Y - 1, EgaColorIndex.BrightWhite);
            }
        });

    private static EgaSprite BakeOceanMidLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Transparent midground backdrop
            // Giant Kelp Forest Stalks rising from bottom to top
            int[] stalkPositions = { 24, 88, 156, 224, 292 };
            foreach (int sx in stalkPositions)
            {
                // Sinuous vertical kelp stipe (stem)
                for (int y = 0; y < 168; y++)
                {
                    int sway = (int)(Math.Sin(y * 0.08) * 3);
                    int kx = sx + sway;
                    b.FillRect(kx, y, 3, 1, EgaColorIndex.Green);
                    b.SetPixel(kx, y, EgaColorIndex.BrightGreen);
                    b.SetPixel(kx + 2, y, EgaColorIndex.Black);

                    // Broad waving kelp leaves extending out every 24 pixels
                    if (y % 24 == 8)
                    {
                        // Right-extending frond
                        for (int fx = 0; fx < 16; fx++)
                        {
                            int fy = y + (fx / 3);
                            b.SetPixel(kx + 3 + fx, fy, EgaColorIndex.Green);
                            b.SetPixel(kx + 3 + fx, fy - 1, EgaColorIndex.BrightGreen);
                            b.SetPixel(kx + 3 + fx, fy + 1, EgaColorIndex.Black);
                        }
                        // Small yellow air bladder at frond base
                        b.FillRect(kx + 2, y - 2, 3, 3, EgaColorIndex.Yellow);
                        b.DrawRect(kx + 2, y - 2, 3, 3, EgaColorIndex.Black);
                    }
                    else if (y % 24 == 20)
                    {
                        // Left-extending frond
                        for (int fx = 0; fx < 16; fx++)
                        {
                            int fy = y + (fx / 3);
                            b.SetPixel(kx - 1 - fx, fy, EgaColorIndex.Green);
                            b.SetPixel(kx - 1 - fx, fy - 1, EgaColorIndex.BrightGreen);
                            b.SetPixel(kx - 1 - fx, fy + 1, EgaColorIndex.Black);
                        }
                        b.FillRect(kx - 4, y - 2, 3, 3, EgaColorIndex.Yellow);
                        b.DrawRect(kx - 4, y - 2, 3, 3, EgaColorIndex.Black);
                    }
                }
            }

            // Submerged Coral Arches at seabed (y: 120..167)
            for (int ax = 50; ax < 320; ax += 135)
            {
                // Left pillar
                b.FillRect(ax, 130, 7, 38, EgaColorIndex.Cyan);
                b.DrawRect(ax, 130, 7, 38, EgaColorIndex.Black);
                // Right pillar
                b.FillRect(ax + 21, 130, 7, 38, EgaColorIndex.Cyan);
                b.DrawRect(ax + 21, 130, 7, 38, EgaColorIndex.Black);
                // Arch lintel top
                b.FillRect(ax, 130, 28, 12, EgaColorIndex.Cyan);
                b.DrawRect(ax, 130, 28, 12, EgaColorIndex.Black);
                b.DrawHLine(ax + 2, 132, 24, EgaColorIndex.BrightCyan);
            }
        });

    private static EgaSprite BakeOceanNearLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Transparent foreground layer
            // Floating bubble clusters with bright highlights
            int[] clusterX = { 45, 110, 185, 255 };
            foreach (int cx in clusterX)
            {
                // Bubble 1 (3x3)
                b.DrawRect(cx, 40, 3, 3, EgaColorIndex.BrightCyan);
                b.SetPixel(cx, 40, EgaColorIndex.BrightWhite);

                // Bubble 2 (4x4)
                b.DrawRect(cx + 8, 25, 4, 4, EgaColorIndex.BrightCyan);
                b.SetPixel(cx + 8, 25, EgaColorIndex.BrightWhite);
                b.SetPixel(cx + 9, 25, EgaColorIndex.BrightWhite);

                // Bubble 3 (2x2)
                b.FillRect(cx + 5, 55, 2, 2, EgaColorIndex.BrightWhite);
            }

            // Seabed rock outcrop with Sea Anemones & Starfish (y: 140..167)
            for (int rx = 0; rx < 320; rx += 80)
            {
                // Coral ledge
                b.FillRect(rx + 10, 148, 45, 20, EgaColorIndex.DarkGray);
                b.DrawRect(rx + 10, 148, 45, 20, EgaColorIndex.Black);
                b.DrawHLine(rx + 11, 149, 43, EgaColorIndex.LightGray);

                // Sea Anemone (Magenta column + waving Bright Magenta tentacles)
                int ax = rx + 20;
                b.FillRect(ax, 143, 8, 5, EgaColorIndex.Magenta);
                b.DrawRect(ax, 143, 8, 5, EgaColorIndex.Black);
                b.SetPixel(ax + 3, 143, EgaColorIndex.Yellow); // Central mouth
                b.DrawVLine(ax, 138, 5, EgaColorIndex.BrightMagenta);
                b.DrawVLine(ax + 2, 136, 7, EgaColorIndex.BrightMagenta);
                b.DrawVLine(ax + 5, 136, 7, EgaColorIndex.BrightMagenta);
                b.DrawVLine(ax + 7, 138, 5, EgaColorIndex.BrightMagenta);

                // Starfish (5-pointed star in Bright Red & Yellow)
                int sx = rx + 36;
                b.FillRect(sx + 2, 154, 3, 3, EgaColorIndex.BrightRed);
                b.SetPixel(sx + 3, 155, EgaColorIndex.Yellow);
                b.SetPixel(sx + 3, 152, EgaColorIndex.BrightRed); // Top arm
                b.SetPixel(sx + 3, 158, EgaColorIndex.BrightRed); // Bottom arm
                b.SetPixel(sx, 155, EgaColorIndex.BrightRed);     // Left arm
                b.SetPixel(sx + 6, 155, EgaColorIndex.BrightRed); // Right arm
            }
        });

    // =========================================================================
    // 2. Deep-Sea 16x16 World Tiles
    // =========================================================================

    private static EgaSprite BakeSunkenTempleStone() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Atlantean Weathered Cyan Stone Block
            b.FillRect(0, 0, 16, 16, EgaColorIndex.Cyan);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);

            // Beveled inner highlights
            b.DrawHLine(1, 1, 14, EgaColorIndex.BrightCyan);
            b.DrawVLine(1, 1, 14, EgaColorIndex.BrightCyan);
            b.DrawHLine(1, 14, 14, EgaColorIndex.Blue);
            b.DrawVLine(14, 1, 14, EgaColorIndex.Blue);

            // Atlantean concentric wave glyph carving in center (rows 4..11)
            b.DrawRect(4, 4, 8, 8, EgaColorIndex.Blue);
            b.DrawHLine(5, 5, 6, EgaColorIndex.BrightCyan);
            b.DrawHLine(6, 8, 4, EgaColorIndex.BrightCyan);
            b.SetPixel(7, 7, EgaColorIndex.Blue);

            // White barnacle cluster on top-left corner
            b.FillRect(2, 2, 2, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(2, 2, EgaColorIndex.Black);
            b.FillRect(3, 4, 2, 2, EgaColorIndex.BrightWhite);

            // Green algae moss in bottom-right corner
            b.FillRect(11, 12, 3, 2, EgaColorIndex.Green);
            b.SetPixel(12, 11, EgaColorIndex.BrightGreen);
        });

    private static EgaSprite BakeCoralKelpPlatform(int parity) =>
        EgaSprite.Bake(16, 16, b =>
        {
            // 8px-tall floating woven kelp & coral shelf platform
            b.FillRect(0, 0, 16, 8, EgaColorIndex.Green);
            b.DrawRect(0, 0, 16, 8, EgaColorIndex.Black);

            // Top sunlit surface: alternating Bright Green and Bright Cyan
            for (int x = 1; x < 15; x++)
            {
                b.SetPixel(x, 1, ((x + parity) % 2 == 0) ? EgaColorIndex.BrightGreen : EgaColorIndex.BrightCyan);
            }

            // Woven pink coral ridge running through the middle (rows 3..5)
            int coralStart = (parity == 0) ? 2 : 5;
            b.FillRect(coralStart, 3, 8, 3, EgaColorIndex.Magenta);
            b.DrawHLine(coralStart, 3, 8, EgaColorIndex.BrightMagenta);
            b.DrawHLine(coralStart, 5, 8, EgaColorIndex.Black);

            // Underside hanging sea moss tendrils (rows 8..12)
            int[] mossX = (parity == 0) ? new[] { 3, 9, 13 } : new[] { 1, 6, 11 };
            foreach (int mx in mossX)
            {
                b.DrawVLine(mx, 8, 3, EgaColorIndex.Green);
                b.SetPixel(mx, 11, EgaColorIndex.BrightGreen);
                // Tiny rising bubble
                b.SetPixel(mx + 1, 13, EgaColorIndex.BrightCyan);
            }
        });

    private static EgaSprite BakeSeaUrchins() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Submerged basalt seabed floor (rows 13..15)
            b.FillRect(0, 13, 16, 3, EgaColorIndex.DarkGray);
            b.DrawHLine(0, 13, 16, EgaColorIndex.Black);

            // Two spiny sea urchins at centers x: 4 and x: 12
            int[] centers = { 4, 12 };
            foreach (int cx in centers)
            {
                // Spherical dark body (y: 6..12)
                b.FillRect(cx - 3, 7, 7, 6, EgaColorIndex.Black);
                b.FillRect(cx - 2, 6, 5, 8, EgaColorIndex.Black);
                b.FillRect(cx - 1, 8, 3, 4, EgaColorIndex.DarkGray);

                // Radiating venom spines pointing outward
                // Vertical top spine
                b.DrawVLine(cx, 1, 5, EgaColorIndex.Black);
                b.SetPixel(cx, 1, EgaColorIndex.BrightRed);     // Venom tip
                b.SetPixel(cx, 2, EgaColorIndex.BrightMagenta);

                // Diagonal spines
                b.SetPixel(cx - 3, 3, EgaColorIndex.BrightRed);
                b.SetPixel(cx - 2, 4, EgaColorIndex.Black);
                b.SetPixel(cx + 3, 3, EgaColorIndex.BrightRed);
                b.SetPixel(cx + 2, 4, EgaColorIndex.Black);

                // Horizontal side spines
                b.DrawHLine(cx - 4, 9, 2, EgaColorIndex.BrightRed);
                b.DrawHLine(cx + 3, 9, 2, EgaColorIndex.BrightRed);

                // Glowing bioluminescent center core
                b.SetPixel(cx, 9, EgaColorIndex.BrightMagenta);
            }
        });

    private static EgaSprite BakeBasaltHull() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Submerged Basalt Bedrock / Submarine Hull Plate
            b.FillRect(0, 0, 16, 16, EgaColorIndex.DarkGray);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);

            // Beveled steel border
            b.DrawHLine(1, 1, 14, EgaColorIndex.LightGray);
            b.DrawVLine(1, 1, 14, EgaColorIndex.LightGray);
            b.DrawHLine(1, 14, 14, EgaColorIndex.Blue);
            b.DrawVLine(14, 1, 14, EgaColorIndex.Blue);

            // Recessed inner panel
            b.FillRect(3, 3, 10, 10, EgaColorIndex.Blue);
            b.DrawRect(3, 3, 10, 10, EgaColorIndex.Black);

            // Heavy brass/steel corner rivets
            b.FillRect(2, 2, 2, 2, EgaColorIndex.Yellow);
            b.SetPixel(2, 2, EgaColorIndex.BrightWhite);
            b.FillRect(12, 2, 2, 2, EgaColorIndex.Yellow);
            b.SetPixel(12, 2, EgaColorIndex.BrightWhite);
            b.FillRect(2, 12, 2, 2, EgaColorIndex.Yellow);
            b.SetPixel(2, 12, EgaColorIndex.BrightWhite);
            b.FillRect(12, 12, 2, 2, EgaColorIndex.Yellow);
            b.SetPixel(12, 12, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeAlgaeVineLadder() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Vertical living Giant Kelp / Sea-Algae Climbing Vine
            // Left twisting kelp stipe (stem) at x: 2..4
            for (int y = 0; y < 16; y++)
            {
                int swayLeft = ((y / 4) % 2 == 0) ? 0 : 1;
                b.FillRect(2 + swayLeft, y, 3, 1, EgaColorIndex.Green);
                b.SetPixel(2 + swayLeft, y, EgaColorIndex.BrightGreen); // Sunlit vine highlight
                b.SetPixel(4 + swayLeft, y, EgaColorIndex.Black);       // Shadow border

                int swayRight = ((y / 4) % 2 == 0) ? 1 : 0;
                b.FillRect(11 + swayRight, y, 3, 1, EgaColorIndex.Green);
                b.SetPixel(11 + swayRight, y, EgaColorIndex.BrightGreen);
                b.SetPixel(13 + swayRight, y, EgaColorIndex.Black);
            }

            // Horizontal knotted algae vine rungs and leafy climbing nodes (rows 2, 6, 10, 14)
            for (int ry = 2; ry < 16; ry += 4)
            {
                // Vine rung connecting left and right stalks
                b.FillRect(4, ry, 8, 2, EgaColorIndex.Green);
                b.DrawHLine(4, ry, 8, EgaColorIndex.BrightGreen);
                b.DrawHLine(4, ry + 1, 8, EgaColorIndex.Black);

                // Small yellow/bright-green gas bladder / leaf node in center
                b.FillRect(7, ry - 1, 2, 3, EgaColorIndex.Yellow);
                b.SetPixel(7, ry, EgaColorIndex.BrightGreen);
            }

            // Sinuous side kelp leaflets curling off the stalks
            b.SetPixel(1, 4, EgaColorIndex.BrightGreen);
            b.SetPixel(0, 5, EgaColorIndex.Green);
            b.SetPixel(15, 8, EgaColorIndex.BrightGreen);
            b.SetPixel(15, 9, EgaColorIndex.Green);
            b.SetPixel(1, 12, EgaColorIndex.BrightGreen);

            // Escaping tiny air bubbles rising along the algae
            b.SetPixel(6, 1, EgaColorIndex.BrightCyan);
            b.SetPixel(9, 9, EgaColorIndex.BrightWhite);
            b.SetPixel(6, 13, EgaColorIndex.BrightCyan);
        });

    private static EgaSprite BakeTreasureChest() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Sunken Pirate Sea Chest in Mahogany & Brass
            b.FillRect(0, 0, 16, 16, EgaColorIndex.Brown);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);

            // Heavy brass corner brackets and vertical straps
            b.DrawHLine(1, 1, 14, EgaColorIndex.Yellow);
            b.DrawHLine(1, 5, 14, EgaColorIndex.Black);  // Lid seam
            b.DrawHLine(1, 6, 14, EgaColorIndex.Yellow); // Lower lid band
            b.DrawHLine(1, 14, 14, EgaColorIndex.Yellow);

            // Vertical brass bands
            b.FillRect(3, 1, 2, 14, EgaColorIndex.Yellow);
            b.FillRect(11, 1, 2, 14, EgaColorIndex.Yellow);

            // Heavy brass padlock in center (x: 7..8, y: 6..10)
            b.FillRect(6, 7, 4, 4, EgaColorIndex.Yellow);
            b.DrawRect(6, 7, 4, 4, EgaColorIndex.Black);
            b.SetPixel(7, 8, EgaColorIndex.Black); // Keyhole

            // White barnacle shell on lid
            b.FillRect(1, 2, 2, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(1, 2, EgaColorIndex.DarkGray);
        });

    // =========================================================================
    // 3. Player: Deep-Sea Scuba Diver (24x24 with OffsetX = -5; Climb 14x24)
    // =========================================================================

    private static EgaSprite BakeDiverAction(int finKick, int gunBob, bool isHoverGlide) =>
        EgaSprite.Bake(24, 24, b =>
        {
            const int ox = 5;

            // 1. Brass Dive Helmet (ox+2..ox+9, y: 0..6)
            b.FillRect(ox + 2, 0, 8, 6, EgaColorIndex.Yellow);
            b.DrawRect(ox + 2, 0, 8, 6, EgaColorIndex.Black);
            b.DrawHLine(ox + 3, 0, 6, EgaColorIndex.BrightWhite); // Brass crown gleam

            // Large circular glass faceplate visor (ox+6..ox+9, y: 1..4)
            b.FillRect(ox + 5, 1, 4, 4, EgaColorIndex.BrightCyan);
            b.DrawRect(ox + 5, 1, 4, 4, EgaColorIndex.Black);
            b.SetPixel(ox + 6, 1, EgaColorIndex.BrightWhite);      // Visor glare
            b.SetPixel(ox + 7, 2, EgaColorIndex.Black);            // Diver eye visible inside

            // Helmet neck collar with brass rivets (y: 5..6)
            b.FillRect(ox + 2, 5, 8, 2, EgaColorIndex.Brown);
            b.SetPixel(ox + 4, 5, EgaColorIndex.Yellow);
            b.SetPixel(ox + 7, 5, EgaColorIndex.Yellow);

            // 2. Scuba Oxygen Tank on back (ox-1..ox+1, y: 3..12)
            b.FillRect(ox - 1, 3, 3, 10, EgaColorIndex.LightGray);
            b.DrawRect(ox - 1, 3, 3, 10, EgaColorIndex.Black);
            b.DrawHLine(ox - 1, 3, 3, EgaColorIndex.BrightWhite);  // Tank valve
            // Black rubber breathing regulator hose looping into helmet
            b.SetPixel(ox, 2, EgaColorIndex.Black);
            b.SetPixel(ox + 1, 2, EgaColorIndex.Black);
            b.SetPixel(ox + 2, 3, EgaColorIndex.Black);
            // Escaping air bubbles from regulator
            b.SetPixel(ox + 1, 0, EgaColorIndex.BrightCyan);
            b.SetPixel(ox, -1, EgaColorIndex.BrightWhite);

            // 3. Neoprene Dive Suit & Torso (ox+2..ox+9, y: 7..14)
            b.FillRect(ox + 2, 7, 7, 7, EgaColorIndex.Blue);
            b.DrawRect(ox + 2, 7, 7, 7, EgaColorIndex.Black);
            // High-visibility yellow safety chest stripe
            b.DrawHLine(ox + 3, 9, 5, EgaColorIndex.Yellow);
            // Weight belt with brass buckle (y: 13..14)
            b.FillRect(ox + 2, 13, 7, 2, EgaColorIndex.Black);
            b.FillRect(ox + 5, 13, 2, 2, EgaColorIndex.Yellow);

            // 4. Pneumatic Speargun Harpoon Blaster (aimed right, y: 8+gunBob..11+gunBob)
            int gy = 8 + gunBob;
            // Diver hands / rubber gloves holding grip
            b.FillRect(ox + 7, gy + 1, 3, 3, EgaColorIndex.Black);
            // Speargun barrel & stock (x: ox+9..ox+16)
            b.FillRect(ox + 9, gy, 8, 2, EgaColorIndex.LightGray);
            b.DrawHLine(ox + 9, gy, 8, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 9, gy, 8, 2, EgaColorIndex.Black);
            // Barbed harpoon steel spear tip (x: ox+17..ox+18)
            b.DrawHLine(ox + 17, gy, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(ox + 18, gy - 1, EgaColorIndex.BrightCyan); // Spear barb
            // Pneumatic air pressure canister underneath barrel
            b.FillRect(ox + 11, gy + 2, 5, 2, EgaColorIndex.DarkGray);
            b.DrawRect(ox + 11, gy + 2, 5, 2, EgaColorIndex.Black);

            // 5. Scuba Fins & Swimming Flutter Kick (y: 15..23)
            b.FillRect(ox + 3, 15, 6, 3, EgaColorIndex.Blue); // Neoprene thighs
            b.DrawRect(ox + 3, 15, 6, 3, EgaColorIndex.Black);

            if (isHoverGlide)
            {
                // Streamlined horizontal swim / hover pose
                // Flippers extended straight back together (x: ox-3..ox+3, y: 17..20)
                b.FillRect(ox - 3, 17, 7, 3, EgaColorIndex.Yellow);
                b.DrawRect(ox - 3, 17, 7, 3, EgaColorIndex.Black);
                b.DrawHLine(ox - 3, 18, 7, EgaColorIndex.BrightWhite);
                // Trailing dual bubble wake stream
                b.SetPixel(ox - 4, 16, EgaColorIndex.BrightCyan);
                b.SetPixel(ox - 5, 19, EgaColorIndex.BrightWhite);
            }
            else
            {
                // 4-Frame Flutter Kick Animation
                int rightFinY = 18 + finKick;
                int leftFinY = 18 - finKick;

                // Right flipper
                b.FillRect(ox + 1, rightFinY, 4, 4, EgaColorIndex.Yellow);
                b.DrawRect(ox + 1, rightFinY, 4, 4, EgaColorIndex.Black);
                b.DrawHLine(ox + 1, rightFinY + 1, 4, EgaColorIndex.BrightWhite);

                // Left flipper
                b.FillRect(ox + 6, leftFinY, 4, 4, EgaColorIndex.Yellow);
                b.DrawRect(ox + 6, leftFinY, 4, 4, EgaColorIndex.Black);
                b.DrawHLine(ox + 6, leftFinY + 1, 4, EgaColorIndex.BrightWhite);

                // Bubble puff behind kicking flipper
                if (finKick != 0)
                {
                    b.SetPixel(ox, rightFinY + 2, EgaColorIndex.BrightCyan);
                    b.SetPixel(ox - 1, leftFinY + 2, EgaColorIndex.BrightWhite);
                }
            }
        }, offsetX: -5, offsetY: 0);

    private static EgaSprite BakeDiverClimb(int step) =>
        EgaSprite.Bake(14, 24, b =>
        {
            // Back of diver's brass helmet
            b.FillRect(3, 0, 8, 5, EgaColorIndex.Yellow);
            b.DrawRect(3, 0, 8, 5, EgaColorIndex.Black);
            b.DrawHLine(4, 0, 6, EgaColorIndex.BrightWhite);

            // Dual Scuba Oxygen Tanks on diver's back
            b.FillRect(2, 5, 4, 9, EgaColorIndex.LightGray);
            b.DrawRect(2, 5, 4, 9, EgaColorIndex.Black);
            b.FillRect(8, 5, 4, 9, EgaColorIndex.LightGray);
            b.DrawRect(8, 5, 4, 9, EgaColorIndex.Black);

            // Arms gripping nautical ladder rungs
            int leftArmY = 5 - (step * 2);
            int rightArmY = 5 + (step * 2);
            b.FillRect(0, leftArmY, 3, 5, EgaColorIndex.Black);
            b.FillRect(11, rightArmY, 3, 5, EgaColorIndex.Black);

            // Neoprene legs & yellow swim flippers kicking
            b.FillRect(3, 14, 8, 4, EgaColorIndex.Blue);
            b.DrawRect(3, 14, 8, 4, EgaColorIndex.Black);

            int leftFinY = 18 + (step * 2);
            int rightFinY = 18 - (step * 2);
            b.FillRect(2, leftFinY, 4, 4, EgaColorIndex.Yellow);
            b.DrawRect(2, leftFinY, 4, 4, EgaColorIndex.Black);
            b.FillRect(8, rightFinY, 4, 4, EgaColorIndex.Yellow);
            b.DrawRect(8, rightFinY, 4, 4, EgaColorIndex.Black);
        });

    // =========================================================================
    // 4. Monsters: Deep-Sea Creatures & Ocean Predators
    // =========================================================================

    private static EgaSprite BakeSwimmingManta(bool stroke) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Graceful Swimming Manta Ray (14x14) - Pure mid-water gliding creature, zero legs!
            // Forward cephalic horns flanking the mouth (x: 10..13, y: 5..8)
            b.SetPixel(12, 5, EgaColorIndex.BrightCyan); // Cephalic fin tip
            b.SetPixel(13, 6, EgaColorIndex.Blue);
            b.SetPixel(13, 7, EgaColorIndex.Blue);
            b.SetPixel(12, 8, EgaColorIndex.BrightCyan);
            b.FillRect(10, 6, 3, 2, EgaColorIndex.DarkGray); // Mouth/face

            // Glowing cyan eyes on side
            b.SetPixel(10, 5, EgaColorIndex.BrightCyan);
            b.SetPixel(10, 8, EgaColorIndex.BrightCyan);

            // Central hydrodynamic body (x: 4..10, y: 5..8)
            b.FillRect(4, 5, 7, 4, EgaColorIndex.Blue);
            b.DrawRect(4, 5, 7, 4, EgaColorIndex.Black);
            // Bioluminescent spots along dorsal spine
            b.SetPixel(6, 6, EgaColorIndex.BrightCyan);
            b.SetPixel(8, 7, EgaColorIndex.BrightCyan);
            b.SetPixel(6, 7, EgaColorIndex.BrightWhite);

            // Long trailing whip tail extending back (x: 0..4, y: 6..7)
            b.DrawHLine(0, 6, 4, EgaColorIndex.DarkGray);
            b.SetPixel(0, 6, EgaColorIndex.Black);

            if (!stroke)
            {
                // Frame 0: Pectoral wings undulating UPWARD in graceful glide
                // Upper wing
                b.FillRect(3, 2, 7, 3, EgaColorIndex.Blue);
                b.DrawHLine(3, 2, 7, EgaColorIndex.BrightCyan); // Wing tip glow
                b.DrawHLine(4, 1, 4, EgaColorIndex.Black);
                // Lower wing swept back
                b.FillRect(3, 9, 7, 3, EgaColorIndex.Blue);
                b.DrawHLine(3, 11, 7, EgaColorIndex.BrightCyan);
                b.DrawHLine(4, 12, 4, EgaColorIndex.Black);
            }
            else
            {
                // Frame 1: Pectoral wings curled in a powerful DOWNWARD swimming stroke
                b.FillRect(4, 3, 6, 2, EgaColorIndex.Blue);
                b.DrawHLine(4, 3, 6, EgaColorIndex.BrightCyan);
                b.FillRect(4, 9, 6, 2, EgaColorIndex.Blue);
                b.DrawHLine(4, 10, 6, EgaColorIndex.BrightCyan);
                // Bubble wake from wingtips
                b.SetPixel(2, 3, EgaColorIndex.BrightWhite);
                b.SetPixel(2, 10, EgaColorIndex.BrightCyan);
            }
        });

    private static EgaSprite BakeBioluminescentJellyfish(bool pulse) =>
        EgaSprite.Bake(14, 12, b =>
        {
            // Translucent glowing jellyfish dome bell
            if (!pulse)
            {
                // Relaxed floating dome (y: 1..6)
                b.FillRect(2, 1, 10, 6, EgaColorIndex.BrightCyan);
                b.DrawRect(2, 1, 10, 6, EgaColorIndex.Blue);
                b.DrawHLine(3, 1, 8, EgaColorIndex.BrightWhite); // Bell highlight
                // Glowing bioluminescent inner core
                b.FillRect(5, 3, 4, 3, EgaColorIndex.BrightMagenta);
                b.SetPixel(6, 4, EgaColorIndex.Yellow);

                // Undulating stinging oral tentacles (y: 7..11)
                b.DrawVLine(2, 7, 5, EgaColorIndex.BrightCyan);
                b.DrawVLine(5, 7, 5, EgaColorIndex.BrightWhite);
                b.DrawVLine(8, 7, 5, EgaColorIndex.BrightWhite);
                b.DrawVLine(11, 7, 5, EgaColorIndex.BrightCyan);
            }
            else
            {
                // Contracted muscular propulsion pulse
                b.FillRect(1, 2, 12, 5, EgaColorIndex.BrightCyan);
                b.DrawRect(1, 2, 12, 5, EgaColorIndex.Blue);
                b.DrawHLine(2, 2, 10, EgaColorIndex.BrightWhite);
                b.FillRect(4, 3, 6, 3, EgaColorIndex.Yellow); // Radiant flash

                // Curled contracted tentacles
                b.DrawVLine(3, 7, 3, EgaColorIndex.BrightWhite);
                b.DrawVLine(6, 7, 3, EgaColorIndex.BrightCyan);
                b.DrawVLine(9, 7, 3, EgaColorIndex.BrightWhite);
            }
        });

    private static EgaSprite BakeSeaAnemoneTurret(bool open) =>
        EgaSprite.Bake(14, 12, b =>
        {
            // Anchored to ceiling/underside of platforms!
            // Rocky calcareous barnacle cone shell (y: 0..3)
            b.FillRect(2, 0, 10, 3, EgaColorIndex.Brown);
            b.DrawRect(2, 0, 10, 3, EgaColorIndex.Black);
            b.DrawHLine(3, 0, 8, EgaColorIndex.DarkGray);

            // Fleshy sea anemone column (y: 3..6)
            b.FillRect(3, 3, 8, 4, EgaColorIndex.Magenta);
            b.DrawRect(3, 3, 8, 4, EgaColorIndex.Black);

            // Stinging tentacles dangling down to y: 11
            b.DrawVLine(1, 4, 7, EgaColorIndex.BrightMagenta);
            b.DrawVLine(3, 7, 5, EgaColorIndex.BrightRed);
            b.DrawVLine(10, 7, 5, EgaColorIndex.BrightRed);
            b.DrawVLine(12, 4, 7, EgaColorIndex.BrightMagenta);

            // Central predatory sphincter / mouth
            if (!open)
            {
                b.FillRect(5, 6, 4, 2, EgaColorIndex.Black);
                b.SetPixel(6, 6, EgaColorIndex.Red);
            }
            else
            {
                // Dilated open mouth spitting venom needle
                b.FillRect(5, 6, 4, 3, EgaColorIndex.BrightRed);
                b.DrawRect(5, 6, 4, 3, EgaColorIndex.Black);
                b.SetPixel(6, 7, EgaColorIndex.BrightGreen); // Glowing venom barb
                b.SetPixel(7, 7, EgaColorIndex.Yellow);
            }
        });

    private static EgaSprite BakeHuntingShark(int step) =>
        EgaSprite.Bake(24, 14, b =>
        {
            // Sleek ocean predator (Hunting Shark / Barracuda, 24x14) - Pure hydrodynamic swimming creature
            // Caudal tail fin swishing at left (x: 0..4)
            int tailY = 6 + (step / 2);
            b.FillRect(0, tailY - 3, 4, 3, EgaColorIndex.DarkGray); // Upper lobe
            b.FillRect(0, tailY + 3, 4, 3, EgaColorIndex.DarkGray); // Lower lobe
            b.DrawVLine(3, tailY - 1, 3, EgaColorIndex.Blue);

            // Main shark torso (x: 4..18, y: 3..10)
            b.FillRect(4, 3, 14, 5, EgaColorIndex.Blue);            // Dark Blue dorsal side
            b.FillRect(5, 8, 13, 2, EgaColorIndex.BrightWhite);     // Counter-shaded white underbelly
            b.DrawRect(4, 3, 14, 7, EgaColorIndex.Black);

            // Sharp dorsal fin on top (x: 9..13, y: 0..3)
            b.FillRect(9, 0, 4, 3, EgaColorIndex.Blue);
            b.DrawRect(9, 0, 4, 3, EgaColorIndex.Black);

            // Streamlined swimming pectoral fin angled horizontally along belly (x: 9..14, y: 9..11)
            b.DrawHLine(9, 9, 5, EgaColorIndex.Blue);
            b.DrawHLine(10, 10, 4, EgaColorIndex.DarkGray);

            // Menacing predator eye with white glint (x: 17..18, y: 4)
            b.FillRect(17, 4, 2, 2, EgaColorIndex.Black);
            b.SetPixel(17, 4, EgaColorIndex.BrightWhite);

            // Snout & Gaping jaw lined with razor fangs (x: 18..23, y: 4..9)
            b.FillRect(18, 3, 5, 3, EgaColorIndex.Blue);            // Upper snout
            b.DrawRect(18, 3, 5, 3, EgaColorIndex.Black);
            b.FillRect(18, 7, 4, 2, EgaColorIndex.BrightWhite);     // Lower jaw
            b.DrawRect(18, 7, 4, 2, EgaColorIndex.Black);

            // Lethal white razor teeth in gaping mouth
            b.SetPixel(19, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(21, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(20, 6, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeAbyssalAnglerfish(int jawOpen) =>
        EgaSprite.Bake(22, 26, b =>
        {
            const int ox = 4;

            // Massive Abyssal Anglerfish (22x26, OffsetX = -4) - Pure swimming abyssal fish
            // 1. Glowing Bioluminescent Esca (Lure) Antenna (y: 0..7)
            b.SetPixel(ox + 2, 6, EgaColorIndex.DarkGray);
            b.SetPixel(ox + 4, 4, EgaColorIndex.DarkGray);
            b.DrawHLine(ox + 5, 3, 7, EgaColorIndex.DarkGray);
            b.SetPixel(ox + 12, 4, EgaColorIndex.DarkGray);
            // Glowing lure bulb (ox+13..ox+16, y: 4..7)
            b.FillRect(ox + 13, 5, 4, 3, EgaColorIndex.Yellow);
            b.DrawRect(ox + 13, 5, 4, 3, EgaColorIndex.BrightCyan);
            b.SetPixel(ox + 14, 6, EgaColorIndex.BrightWhite);      // Core radiance

            // 2. Caudal swimming tail fin at rear (ox-2..ox+1, y: 12..18)
            b.FillRect(ox - 2, 12, 3, 7, EgaColorIndex.DarkGray);
            b.DrawVLine(ox - 2, 13, 5, EgaColorIndex.Blue);

            // 3. Massive round abyssal black body (ox..ox+13, y: 7..22)
            b.FillRect(ox, 7, 13, 15, EgaColorIndex.Black);
            b.DrawRect(ox, 7, 13, 15, EgaColorIndex.Blue);
            // Rounded lower belly (no flat bottom or ground contact)
            b.DrawHLine(ox + 2, 22, 9, EgaColorIndex.Black);
            b.DrawHLine(ox + 4, 21, 6, EgaColorIndex.DarkGray);

            // Glowing blue lateral photophores
            b.SetPixel(ox + 3, 12, EgaColorIndex.BrightCyan);
            b.SetPixel(ox + 6, 14, EgaColorIndex.BrightCyan);
            b.SetPixel(ox + 3, 17, EgaColorIndex.BrightCyan);

            // Small beady opaque white/yellow eye
            b.FillRect(ox + 6, 9, 2, 2, EgaColorIndex.Yellow);
            b.SetPixel(ox + 6, 9, EgaColorIndex.BrightWhite);

            // 4. Fluttering side pectoral swimming fin (ox+2..ox+5, y: 15..18)
            int finShift = (jawOpen == 0) ? 0 : 1;
            b.FillRect(ox + 1 + finShift, 15, 4, 3, EgaColorIndex.DarkGray);
            b.DrawHLine(ox + 1 + finShift, 15, 4, EgaColorIndex.BrightCyan);

            // 5. Huge Cavernous Jaws & Long Translucent Needle Fangs
            if (jawOpen == 0)
            {
                // Clamped jaws with interlocking fangs
                b.FillRect(ox + 9, 11, 7, 10, EgaColorIndex.DarkGray);
                b.DrawRect(ox + 9, 11, 7, 10, EgaColorIndex.Black);
                // Interlocking sharp needle fangs
                for (int fy = 12; fy <= 19; fy += 2)
                {
                    b.SetPixel(ox + 13, fy, EgaColorIndex.BrightWhite);
                    b.SetPixel(ox + 14, fy, EgaColorIndex.BrightCyan);
                }
            }
            else
            {
                // Hinged wide-open cavernous maw revealing red gullet!
                b.FillRect(ox + 8, 10, 8, 4, EgaColorIndex.DarkGray); // Upper jaw
                b.DrawRect(ox + 8, 10, 8, 4, EgaColorIndex.Black);
                b.FillRect(ox + 8, 18, 8, 4, EgaColorIndex.DarkGray); // Lower jaw
                b.DrawRect(ox + 8, 18, 8, 4, EgaColorIndex.Black);
                // Gaping mouth cavity
                b.FillRect(ox + 9, 13, 6, 6, EgaColorIndex.Red);
                // Needle fangs pointing inward
                b.DrawVLine(ox + 14, 13, 3, EgaColorIndex.BrightWhite);
                b.DrawVLine(ox + 12, 13, 2, EgaColorIndex.BrightCyan);
                b.DrawVLine(ox + 14, 17, 3, EgaColorIndex.BrightWhite);
                b.DrawVLine(ox + 12, 18, 2, EgaColorIndex.BrightCyan);
            }
        }, offsetX: -4, offsetY: 0);

    // =========================================================================
    // 5. Projectiles: Speargun Harpoon & Bio-electric Venom Orb
    // =========================================================================

    private static EgaSprite BakeSpeargunHarpoon() =>
        EgaSprite.Bake(8, 3, b =>
        {
            // Trailing bubble ring on left (x: 0..1)
            b.SetPixel(0, 0, EgaColorIndex.BrightCyan);
            b.SetPixel(0, 2, EgaColorIndex.BrightCyan);
            b.SetPixel(1, 1, EgaColorIndex.BrightWhite);

            // Barbed steel harpoon shaft & point (x: 2..7)
            b.DrawHLine(2, 1, 5, EgaColorIndex.LightGray);
            b.SetPixel(6, 0, EgaColorIndex.BrightWhite); // Upper barb
            b.SetPixel(6, 2, EgaColorIndex.BrightWhite); // Lower barb
            b.SetPixel(7, 1, EgaColorIndex.BrightWhite); // Piercing point
        });

    private static EgaSprite BakeBioElectricOrb() =>
        EgaSprite.Bake(6, 6, b =>
        {
            b.FillRect(1, 1, 4, 4, EgaColorIndex.BrightCyan);
            b.DrawRect(0, 0, 6, 6, EgaColorIndex.Blue);
            b.FillRect(2, 2, 2, 2, EgaColorIndex.Yellow); // High-voltage core
            b.SetPixel(2, 2, EgaColorIndex.BrightWhite);
        });

    // =========================================================================
    // 6. Pickups: Sea Pearl, Red Coral Ruby, Gold Doubloon & Scuba Oxygen Tank
    // =========================================================================

    private static EgaSprite BakeSeaPearl(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Open scallop shell base (y: 6..12)
            b.FillRect(2, 7, 10, 6, EgaColorIndex.DarkGray);
            b.DrawRect(2, 7, 10, 6, EgaColorIndex.Black);
            b.DrawHLine(3, 12, 8, EgaColorIndex.LightGray);

            // Shimmering iridescent sea pearl in center (x: 4..9, y: 3..8)
            b.FillRect(4, 3, 6, 6, EgaColorIndex.BrightCyan);
            b.DrawRect(4, 3, 6, 6, EgaColorIndex.Blue);
            b.FillRect(5, 4, 3, 3, EgaColorIndex.BrightWhite);

            if (glint)
            {
                // Radiant star glint on pearl
                b.SetPixel(6, 1, EgaColorIndex.BrightWhite);
                b.SetPixel(6, 5, EgaColorIndex.Yellow);
                b.SetPixel(2, 4, EgaColorIndex.BrightWhite);
                b.SetPixel(10, 4, EgaColorIndex.BrightWhite);
            }
        });

    private static EgaSprite BakeCoralRuby(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Radiant Red Sea Coral Gemstone
            b.FillRect(3, 3, 8, 8, EgaColorIndex.BrightRed);
            b.DrawRect(3, 3, 8, 8, EgaColorIndex.Black);
            b.DrawHLine(4, 4, 6, EgaColorIndex.Red);

            int size = glint ? 3 : 2;
            b.FillRect(5, 4, size, size, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeGoldDoubloon(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Sunken Spanish Gold Doubloon
            b.FillRect(2, 2, 10, 10, EgaColorIndex.Yellow);
            b.DrawRect(2, 2, 10, 10, EgaColorIndex.Black);
            b.DrawRect(3, 3, 8, 8, EgaColorIndex.Brown);

            // Stamped royal anchor crest in center
            b.DrawVLine(6, 4, 5, EgaColorIndex.Brown);
            b.DrawHLine(5, 5, 3, EgaColorIndex.Brown);
            b.DrawHLine(4, 8, 5, EgaColorIndex.Brown);

            if (glint)
            {
                b.FillRect(3, 3, 2, 2, EgaColorIndex.BrightWhite);
            }
        });

    private static EgaSprite BakeOxygenTank(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Scuba Emergency Oxygen Tank (Bright Cyan / White with pressure gauge)
            b.FillRect(2, 2, 10, 10, EgaColorIndex.BrightCyan);
            b.DrawRect(2, 2, 10, 10, EgaColorIndex.Black);
            b.DrawHLine(3, 3, 8, EgaColorIndex.BrightWhite);

            // Red emergency medical cross
            var crossCol = glint ? EgaColorIndex.BrightRed : EgaColorIndex.Red;
            b.FillRect(6, 5, 2, 4, crossCol);
            b.FillRect(5, 6, 4, 2, crossCol);

            // Tank valve top
            b.FillRect(5, 0, 4, 2, EgaColorIndex.DarkGray);
        });

    private static EgaSprite BakeUnderwaterBadge(string letter, EgaColorIndex borderCol, EgaColorIndex textCol) =>
        EgaSprite.Bake(14, 14, b =>
        {
            b.FillRect(0, 0, 14, 14, EgaColorIndex.Black);
            b.DrawRect(0, 0, 14, 14, borderCol);
            b.DrawRect(1, 1, 12, 12, EgaColorIndex.Blue);

            // Render center glyph
            b.FillRect(4, 4, 6, 6, textCol);
            b.SetPixel(6, 6, EgaColorIndex.Black);
        });
}
