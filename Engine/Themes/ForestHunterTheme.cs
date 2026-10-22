using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// "Forest & Creatures" Theme:
/// A complete graphical overhaul using only static <see cref="EgaSprite"/> assets.
/// Features a Woodland Ranger shooting feathered arrows from a bow, giant tree trunks,
/// mossy grass terrain, climbing vines, and forest creatures (Hornets, Timber Wolves,
/// Canopy Spiders, Hedge-Boars, and Grizzly Beasts) sharing the exact same engine logic.
/// </summary>
public sealed class ForestHunterTheme : SpriteThemeBase
{
    public static readonly ThemeAssetPack StaticAssets = EgaPngCodec.LoadThemePack("forest", "Forest & Creatures");

    public override ThemeAssetPack Assets => StaticAssets;
    public override string BgmTrack => "audio/bgm_forest.wav";

    public static ThemeAssetPack BakeProceduralAssets()
    {
        return new ThemeAssetPack
        {
            Name = "Forest & Creatures",

            // 1. Standard 3-Layer Parallax Background Images (Height: 168px)
            BackgroundFar = BakeForestFarLayer(),
            BackgroundMid = BakeForestMidLayer(),
            BackgroundNear = BakeForestNearLayer(),

            // 2. Standard 16x16 Forest Tiles
            SolidTile = BakeGrassEarthTile(),
            OneWayTileVariants = new[]
            {
                BakeLogBranchTile(0),
                BakeLogBranchTile(1)
            },
            HazardTile = BakeThornyBrambles(),
            BorderTile = BakeMossyBedrock(),
            LadderTile = BakeVineLadder(),
            CrateTile = BakeWickerSupplyCrate(),

            // 3. Player: Woodland Archer with feathered cap, tunic, quiver & bow
            PlayerIdle = BakeArcherAction(legStep: 0, bowBob: 0),
            PlayerWalk = new[]
            {
                BakeArcherAction(legStep: 2, bowBob: 0),
                BakeArcherAction(legStep: 0, bowBob: 1),
                BakeArcherAction(legStep: -2, bowBob: 0),
                BakeArcherAction(legStep: 0, bowBob: 1)
            },
            PlayerJump = BakeArcherAction(legStep: -1, bowBob: -1),
            PlayerClimb = new[]
            {
                BakeArcherClimb(step: 1),
                BakeArcherClimb(step: -1)
            },

            // 4. Monsters: Woodland Creatures & Animals
            PatrolDroid = new[]
            {
                BakeSpikyBoar(step: 0),
                BakeSpikyBoar(step: 1)
            },
            HoverDrone = new[]
            {
                BakeGiantHornet(wingsUp: true),
                BakeGiantHornet(wingsUp: false)
            },
            CeilingTurret = new[]
            {
                BakeCanopySpider(fangsOpen: true),
                BakeCanopySpider(fangsOpen: false)
            },
            MechaHound = new[]
            {
                BakeTimberWolf(step: 2),
                BakeTimberWolf(step: -2)
            },
            HeavyGuard = new[]
            {
                BakeGrizzlyBeast(stride: 1),
                BakeGrizzlyBeast(stride: -1)
            },

            // 5. Projectiles: Feathered Hunting Arrow & Venom Spit
            PlayerProjectile = BakeHuntingArrow(),
            MonsterProjectile = BakeVenomSpit(),

            // 6. Pickups: Forest Berries, Honeycomb & Herbal Pouches
            BlueGem = new[]
            {
                BakeDewBerry(EgaColorIndex.BrightCyan, glintLarge: true),
                BakeDewBerry(EgaColorIndex.BrightCyan, glintLarge: false)
            },
            RedRuby = new[]
            {
                BakeDewBerry(EgaColorIndex.BrightRed, glintLarge: true),
                BakeDewBerry(EgaColorIndex.BrightRed, glintLarge: false)
            },
            GoldCartridge = new[]
            {
                BakeHoneycomb(bright: true),
                BakeHoneycomb(bright: false)
            },
            MedKit = new[]
            {
                BakeHerbPouch(bright: true),
                BakeHerbPouch(bright: false)
            },
            RapidFireBadge = DefaultTheme.BakeBadge("R", EgaColorIndex.Brown, EgaColorIndex.Yellow),
            HighJumpBadge = DefaultTheme.BakeBadge("J", EgaColorIndex.Green, EgaColorIndex.BrightWhite),
            SpeedDashBadge = DefaultTheme.BakeBadge("S", EgaColorIndex.BrightGreen, EgaColorIndex.Black)
        };
    }

    // --- 3 Forest Parallax Background Layer Images (Height: 168px) ---

    private static EgaSprite BakeForestFarLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Night sky above canopy (0..63)
            b.FillRect(0, 0, 320, 64, EgaColorIndex.Black);

            // Crescent / full woodland moon in the far sky
            b.FillRect(242, 10, 14, 14, EgaColorIndex.Yellow);
            b.FillRect(244, 8, 10, 18, EgaColorIndex.Yellow);
            b.FillRect(240, 12, 18, 10, EgaColorIndex.Yellow);
            b.FillRect(244, 12, 8, 8, EgaColorIndex.BrightWhite);

            // Distant pine silhouettes along the horizon (y: 34..63)
            for (int tx = 8; tx < 320; tx += 32)
            {
                int h = (tx % 64 == 8) ? 26 : 18;
                int topY = 64 - h;
                b.FillRect(tx + 6, topY, 4, h, EgaColorIndex.Green);
                b.FillRect(tx + 3, topY + 6, 10, h - 6, EgaColorIndex.Green);
                b.FillRect(tx, topY + 12, 16, h - 12, EgaColorIndex.Green);
            }

            // Glowing fireflies in the night air
            var fireflies = new (int X, int Y, EgaColorIndex Color)[]
            {
                (22, 14, EgaColorIndex.Yellow), (58, 26, EgaColorIndex.BrightGreen),
                (95, 12, EgaColorIndex.Yellow), (140, 22, EgaColorIndex.BrightGreen),
                (185, 16, EgaColorIndex.Yellow), (215, 30, EgaColorIndex.BrightGreen),
                (288, 18, EgaColorIndex.Yellow), (306, 28, EgaColorIndex.BrightGreen)
            };
            for (int i = 0; i < fireflies.Length; i++)
            {
                b.SetPixel(fireflies[i].X, fireflies[i].Y, fireflies[i].Color);
            }

            // Deep woodland undergrowth backdrop (64..167)
            b.FillRect(0, 64, 320, 104, EgaColorIndex.Green);
            b.DrawHLine(0, 64, 320, EgaColorIndex.Black);
            b.DrawHLine(0, 65, 320, EgaColorIndex.BrightGreen);
        });

    private static EgaSprite BakeForestMidLayer() =>
        EgaSprite.Bake(64, 168, b =>
        {
            // Tall ancient tree trunk with bark grooves (0..64)
            b.FillRect(6, 0, 16, 64, EgaColorIndex.Brown);
            b.DrawVLine(6, 0, 64, EgaColorIndex.Black);
            b.DrawVLine(21, 0, 64, EgaColorIndex.Black);
            b.DrawVLine(10, 0, 64, EgaColorIndex.Black);
            b.DrawVLine(16, 0, 64, EgaColorIndex.Black);
            b.DrawVLine(8, 0, 64, EgaColorIndex.Yellow);

            // Spreading leafy canopy crown at top of trunk
            b.FillRect(0, 0, 28, 12, EgaColorIndex.Green);
            b.FillRect(4, 0, 20, 6, EgaColorIndex.BrightGreen);
            b.DrawHLine(0, 12, 28, EgaColorIndex.Black);
        });

    private static EgaSprite BakeForestNearLayer() =>
        EgaSprite.Bake(220, 168, b =>
        {
            var bush = BakeBerryBush();
            var log = BakeMushroomLog();
            var vine = BakeHangingVine();

            b.DrawSprite(bush, 0, 94);
            b.DrawSprite(vine, 45, 76);
            b.DrawSprite(vine, 45, 84);
            b.DrawSprite(log, 110, 92);
            b.DrawSprite(vine, 165, 76);
            b.DrawSprite(vine, 165, 84);
        });

    private static EgaSprite BakeBerryBush() =>
        EgaSprite.Bake(20, 16, b =>
        {
            // Rounded woodland shrub with red and yellow berries
            b.FillRect(2, 4, 16, 12, EgaColorIndex.Green);
            b.FillRect(4, 2, 12, 12, EgaColorIndex.Green);
            b.DrawRect(2, 4, 16, 12, EgaColorIndex.Black);
            b.FillRect(4, 3, 10, 4, EgaColorIndex.BrightGreen);

            // Berries
            b.FillRect(5, 7, 2, 2, EgaColorIndex.BrightRed);
            b.FillRect(12, 6, 2, 2, EgaColorIndex.BrightRed);
            b.FillRect(8, 11, 2, 2, EgaColorIndex.Yellow);
            b.FillRect(14, 11, 2, 2, EgaColorIndex.BrightRed);
        });

    private static EgaSprite BakeMushroomLog() =>
        EgaSprite.Bake(28, 14, b =>
        {
            // Fallen hollow log on bottom
            b.FillRect(0, 6, 28, 8, EgaColorIndex.Brown);
            b.DrawRect(0, 6, 28, 8, EgaColorIndex.Black);
            b.DrawHLine(2, 8, 24, EgaColorIndex.Yellow);
            b.DrawHLine(1, 6, 26, EgaColorIndex.BrightGreen); // Moss strip

            // Two red-capped toadstools growing on top
            b.FillRect(6, 3, 2, 3, EgaColorIndex.BrightWhite);
            b.FillRect(4, 0, 6, 4, EgaColorIndex.BrightRed);
            b.DrawRect(4, 0, 6, 4, EgaColorIndex.Black);
            b.SetPixel(5, 1, EgaColorIndex.BrightWhite);
            b.SetPixel(8, 2, EgaColorIndex.BrightWhite);

            b.FillRect(19, 3, 2, 3, EgaColorIndex.BrightWhite);
            b.FillRect(17, 0, 6, 4, EgaColorIndex.BrightRed);
            b.DrawRect(17, 0, 6, 4, EgaColorIndex.Black);
            b.SetPixel(18, 1, EgaColorIndex.BrightWhite);
            b.SetPixel(21, 1, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeHangingVine() =>
        EgaSprite.Bake(6, 8, b =>
        {
            b.DrawVLine(2, 0, 8, EgaColorIndex.Black);
            b.SetPixel(1, 1, EgaColorIndex.BrightGreen);
            b.SetPixel(3, 3, EgaColorIndex.BrightGreen);
            b.SetPixel(1, 5, EgaColorIndex.BrightGreen);
            b.SetPixel(3, 7, EgaColorIndex.BrightGreen);
        });

    // --- Tile Sprites (16x16) ---

    private static EgaSprite BakeGrassEarthTile() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Rich brown soil body
            b.FillRect(0, 0, 16, 16, EgaColorIndex.Brown);
            b.DrawVLine(0, 4, 12, EgaColorIndex.Black);
            b.DrawHLine(0, 15, 16, EgaColorIndex.Black);

            // Soil pebbles
            b.FillRect(4, 9, 2, 2, EgaColorIndex.DarkGray);
            b.FillRect(11, 12, 2, 2, EgaColorIndex.DarkGray);
            b.SetPixel(8, 7, EgaColorIndex.Black);

            // Lush green grass turf top (rows 0..4)
            b.FillRect(0, 0, 16, 4, EgaColorIndex.Green);
            b.DrawHLine(0, 0, 16, EgaColorIndex.BrightGreen);
            b.DrawHLine(0, 1, 16, EgaColorIndex.BrightGreen);
            // Grass blades hanging down at row 4
            for (int x = 1; x < 15; x += 3)
            {
                b.SetPixel(x, 4, EgaColorIndex.BrightGreen);
                b.SetPixel(x + 1, 4, EgaColorIndex.Green);
            }
            b.DrawHLine(0, 5, 16, EgaColorIndex.Black);
        });

    private static EgaSprite BakeLogBranchTile(int parity) =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Horizontal tree branch / log platform (8px tall)
            b.FillRect(0, 0, 16, 8, EgaColorIndex.Brown);
            b.DrawRect(0, 0, 16, 8, EgaColorIndex.Black);
            b.DrawHLine(1, 1, 14, EgaColorIndex.Yellow); // Sunlit bark top
            b.DrawHLine(1, 4, 14, EgaColorIndex.Black);  // Bark groove

            // Green moss patches along the branch
            int mossX = (parity == 0) ? 3 : 8;
            b.FillRect(mossX, 0, 5, 2, EgaColorIndex.BrightGreen);
            b.SetPixel(mossX + 2, 2, EgaColorIndex.Green);
        });

    private static EgaSprite BakeThornyBrambles() =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.DrawHLine(0, 15, 16, EgaColorIndex.Black);
            b.DrawHLine(0, 14, 16, EgaColorIndex.Green);

            int[] centers = { 4, 12 };
            foreach (int sx in centers)
            {
                for (int h = 0; h < 11; h++)
                {
                    int sy = 13 - h;
                    int halfW = (h * 3) / 11;
                    b.SetPixel(sx - halfW, sy, EgaColorIndex.BrightGreen);
                    if (halfW > 0)
                    {
                        b.FillRect(sx - halfW + 1, sy, halfW * 2 - 1, 1, EgaColorIndex.Green);
                    }
                    b.SetPixel(sx + halfW, sy, EgaColorIndex.Black);
                }
                // Poisonous yellow/red thorn tips
                b.SetPixel(sx, 2, EgaColorIndex.BrightRed);
                b.SetPixel(sx, 3, EgaColorIndex.Yellow);
            }
        });

    private static EgaSprite BakeMossyBedrock() =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.FillRect(0, 0, 16, 16, EgaColorIndex.DarkGray);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            b.DrawHLine(1, 1, 14, EgaColorIndex.LightGray);
            b.DrawVLine(1, 1, 14, EgaColorIndex.LightGray);
            b.DrawRect(3, 3, 10, 10, EgaColorIndex.Black);
            // Moss in stone corners
            b.SetPixel(2, 2, EgaColorIndex.BrightGreen);
            b.SetPixel(13, 2, EgaColorIndex.BrightGreen);
            b.SetPixel(2, 13, EgaColorIndex.BrightGreen);
            b.SetPixel(13, 13, EgaColorIndex.BrightGreen);
        });

    private static EgaSprite BakeVineLadder() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Left & right twisted green vines
            b.DrawVLine(2, 0, 16, EgaColorIndex.BrightGreen);
            b.DrawVLine(3, 0, 16, EgaColorIndex.Green);
            b.DrawVLine(4, 0, 16, EgaColorIndex.Black);

            b.DrawVLine(11, 0, 16, EgaColorIndex.BrightGreen);
            b.DrawVLine(12, 0, 16, EgaColorIndex.Green);
            b.DrawVLine(13, 0, 16, EgaColorIndex.Black);

            // Wooden branch rungs
            for (int ry = 2; ry < 16; ry += 4)
            {
                b.DrawHLine(4, ry, 8, EgaColorIndex.Yellow);
                b.DrawHLine(4, ry + 1, 8, EgaColorIndex.Brown);
            }
        });

    private static EgaSprite BakeWickerSupplyCrate() =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.FillRect(0, 0, 16, 16, EgaColorIndex.Brown);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            b.DrawRect(2, 2, 12, 12, EgaColorIndex.Yellow);
            b.DrawHLine(3, 7, 10, EgaColorIndex.Black);
            // Green leaf emblem in center
            b.FillRect(6, 5, 4, 5, EgaColorIndex.BrightGreen);
            b.DrawRect(6, 5, 4, 5, EgaColorIndex.Black);
        });

    // --- Player Sprites: Woodland Ranger with Bow & Arrow ---

    private static EgaSprite BakeArcherAction(int legStep, int bowBob) =>
        EgaSprite.Bake(24, 24, b =>
        {
            const int ox = 5;

            // 1. Hunter's Green Cap with Red Feather Plume
            b.FillRect(ox + 2, 0, 8, 4, EgaColorIndex.Green);
            b.DrawHLine(ox + 3, 0, 6, EgaColorIndex.BrightGreen);
            b.DrawRect(ox + 2, 0, 8, 4, EgaColorIndex.Black);
            // Red feather sticking back
            b.FillRect(ox, 0, 3, 2, EgaColorIndex.BrightRed);
            b.SetPixel(ox + 1, 0, EgaColorIndex.Yellow);

            // 2. Ranger Face
            b.FillRect(ox + 2, 4, 8, 4, EgaColorIndex.BrightRed);
            b.DrawRect(ox + 2, 4, 8, 4, EgaColorIndex.Black);
            b.FillRect(ox + 6, 4, 3, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(ox + 8, 4, EgaColorIndex.Black);

            // 3. Woodland Green Tunic & Brown Quiver Strap
            b.FillRect(ox + 3, 8, 8, 6, EgaColorIndex.BrightGreen);
            b.DrawRect(ox + 3, 8, 8, 6, EgaColorIndex.Black);
            b.DrawVLine(ox + 5, 9, 4, EgaColorIndex.Brown); // Quiver strap

            b.FillRect(ox + 1, 8, 3, 6, EgaColorIndex.Green);
            b.DrawRect(ox + 1, 8, 3, 6, EgaColorIndex.Black);

            // 4. Curved Wooden Hunting Bow & Notched Arrow (aimed right)
            int bowY = 8 + bowBob;
            // Bow stave curve
            b.DrawVLine(ox + 11, bowY, 7, EgaColorIndex.BrightWhite); // Bowstring
            b.SetPixel(ox + 12, bowY, EgaColorIndex.Brown);
            b.SetPixel(ox + 13, bowY + 1, EgaColorIndex.Yellow);
            b.DrawVLine(ox + 14, bowY + 2, 3, EgaColorIndex.Brown);
            b.SetPixel(ox + 13, bowY + 5, EgaColorIndex.Yellow);
            b.SetPixel(ox + 12, bowY + 6, EgaColorIndex.Brown);
            // Drawn arrow shaft & gleaming white arrowhead
            b.DrawHLine(ox + 10, bowY + 3, 7, EgaColorIndex.Yellow);
            b.FillRect(ox + 16, bowY + 2, 2, 3, EgaColorIndex.BrightWhite);

            // 5. Leather Belt
            b.FillRect(ox + 3, 14, 8, 2, EgaColorIndex.Brown);
            b.SetPixel(ox + 6, 14, EgaColorIndex.Yellow);

            // 6. Forest Leggings
            b.FillRect(ox + 3, 16, 8, 4, EgaColorIndex.Green);
            b.DrawRect(ox + 3, 16, 8, 4, EgaColorIndex.Black);

            // 7. Leather Hunting Boots
            int bootLeftY = 20 + legStep;
            int bootRightY = 20 - legStep;
            b.FillRect(ox + 2, bootLeftY, 4, 4, EgaColorIndex.Brown);
            b.DrawRect(ox + 2, bootLeftY, 4, 4, EgaColorIndex.Black);
            b.FillRect(ox + 8, bootRightY, 4, 4, EgaColorIndex.Brown);
            b.DrawRect(ox + 8, bootRightY, 4, 4, EgaColorIndex.Black);
        }, offsetX: -5, offsetY: 0);

    private static EgaSprite BakeArcherClimb(int step) =>
        EgaSprite.Bake(14, 24, b =>
        {
            b.FillRect(3, 0, 8, 4, EgaColorIndex.Green);
            b.DrawHLine(4, 0, 6, EgaColorIndex.BrightGreen);
            b.DrawRect(3, 0, 8, 4, EgaColorIndex.Black);

            // Back of green tunic with brown arrow quiver
            b.FillRect(2, 4, 10, 9, EgaColorIndex.Green);
            b.DrawRect(2, 4, 10, 9, EgaColorIndex.Black);
            b.FillRect(5, 5, 4, 7, EgaColorIndex.Brown); // Quiver on back
            b.SetPixel(6, 4, EgaColorIndex.BrightRed);   // Arrow feathers sticking out
            b.SetPixel(8, 4, EgaColorIndex.BrightWhite);

            int leftArmY = 4 - (step * 2);
            int rightArmY = 4 + (step * 2);
            b.FillRect(0, leftArmY, 3, 5, EgaColorIndex.BrightGreen);
            b.DrawRect(0, leftArmY, 3, 5, EgaColorIndex.Black);
            b.FillRect(11, rightArmY, 3, 5, EgaColorIndex.BrightGreen);
            b.DrawRect(11, rightArmY, 3, 5, EgaColorIndex.Black);

            b.FillRect(2, 13, 10, 2, EgaColorIndex.Brown);
            b.FillRect(3, 15, 8, 5, EgaColorIndex.Green);
            b.DrawRect(3, 15, 8, 5, EgaColorIndex.Black);

            int leftBootY = 20 + (step * 2);
            int rightBootY = 20 - (step * 2);
            b.FillRect(2, leftBootY, 4, 3, EgaColorIndex.Brown);
            b.DrawRect(2, leftBootY, 4, 3, EgaColorIndex.Black);
            b.FillRect(8, rightBootY, 4, 3, EgaColorIndex.Brown);
            b.DrawRect(8, rightBootY, 4, 3, EgaColorIndex.Black);
        });

    // --- Monster Sprites: Forest Creatures & Animals ---

    private static EgaSprite BakeSpikyBoar(int step) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Spiky woodland boar / hedgehog (14x14)
            // Yellow dorsal spines along the back
            for (int x = 2; x <= 10; x += 2)
            {
                b.SetPixel(x, 1, EgaColorIndex.Yellow);
                b.SetPixel(x, 2, EgaColorIndex.BrightWhite);
            }
            // Rounded brown body
            b.FillRect(1, 3, 12, 8, EgaColorIndex.Brown);
            b.DrawRect(1, 3, 12, 8, EgaColorIndex.Black);
            // Glowing red eye & white tusk on right side (mirrored when facing left)
            b.SetPixel(10, 5, EgaColorIndex.BrightRed);
            b.FillRect(12, 7, 2, 2, EgaColorIndex.BrightWhite); // Tusk

            // Scuttling hooves
            int offset = step == 0 ? 0 : 1;
            b.FillRect(2 + offset, 11, 3, 3, EgaColorIndex.Black);
            b.FillRect(8 - offset, 11, 3, 3, EgaColorIndex.Black);
        });

    private static EgaSprite BakeGiantHornet(bool wingsUp) =>
        EgaSprite.Bake(14, 12, b =>
        {
            // Translucent white/cyan wings flapping on top
            int wingY = wingsUp ? 0 : 2;
            b.FillRect(4, wingY, 6, 4, EgaColorIndex.BrightWhite);
            b.DrawRect(4, wingY, 6, 4, EgaColorIndex.BrightCyan);

            // Striped Yellow & Black hornet body
            b.FillRect(2, 4, 10, 6, EgaColorIndex.Yellow);
            b.DrawVLine(4, 4, 6, EgaColorIndex.Black);
            b.DrawVLine(7, 4, 6, EgaColorIndex.Black);
            b.DrawVLine(10, 4, 6, EgaColorIndex.Black);
            b.DrawRect(2, 4, 10, 6, EgaColorIndex.Black);

            // Red compound eye & stinger
            b.FillRect(10, 5, 2, 2, EgaColorIndex.BrightRed);
            b.DrawHLine(0, 7, 2, EgaColorIndex.BrightRed); // Rear stinger
        });

    private static EgaSprite BakeCanopySpider(bool fangsOpen) =>
        EgaSprite.Bake(14, 12, b =>
        {
            // White web silk anchor at top
            b.DrawVLine(6, 0, 3, EgaColorIndex.BrightWhite);
            b.DrawVLine(7, 0, 3, EgaColorIndex.BrightWhite);

            // Black widow abdomen with red hourglass
            b.FillRect(3, 2, 8, 7, EgaColorIndex.Black);
            b.DrawRect(3, 2, 8, 7, EgaColorIndex.DarkGray);
            b.FillRect(6, 4, 2, 3, EgaColorIndex.BrightRed);

            // Spindly legs on sides
            b.DrawHLine(0, 4, 3, EgaColorIndex.Black);
            b.DrawHLine(11, 4, 3, EgaColorIndex.Black);
            b.DrawHLine(0, 7, 3, EgaColorIndex.Black);
            b.DrawHLine(11, 7, 3, EgaColorIndex.Black);

            // Glowing green venom chelicerae/fangs pointing down
            int fangSpread = fangsOpen ? 1 : 0;
            b.FillRect(5 - fangSpread, 9, 2, 3, EgaColorIndex.BrightGreen);
            b.FillRect(7 + fangSpread, 9, 2, 3, EgaColorIndex.BrightGreen);
        });

    private static EgaSprite BakeTimberWolf(int step) =>
        EgaSprite.Bake(24, 14, b =>
        {
            // Bushy tail on left
            b.FillRect(1, 4, 5, 3, EgaColorIndex.DarkGray);
            b.DrawRect(1, 4, 5, 3, EgaColorIndex.Black);

            // Wolf torso
            b.FillRect(5, 3, 12, 6, EgaColorIndex.LightGray);
            b.DrawHLine(6, 4, 10, EgaColorIndex.DarkGray);
            b.DrawRect(5, 3, 12, 6, EgaColorIndex.Black);

            // Pointed ears, snout & yellow predator eye (facing right)
            b.FillRect(16, 0, 3, 3, EgaColorIndex.DarkGray); // Ear
            b.FillRect(16, 2, 8, 6, EgaColorIndex.LightGray);
            b.DrawRect(16, 2, 8, 6, EgaColorIndex.Black);
            b.SetPixel(20, 3, EgaColorIndex.Yellow);          // Eye
            b.SetPixel(22, 6, EgaColorIndex.BrightWhite);     // Fang

            // Galloping wolf paws
            b.FillRect(17, 9 + step, 2, 5, EgaColorIndex.DarkGray);
            b.FillRect(7, 9 - step, 2, 5, EgaColorIndex.DarkGray);
        });

    private static EgaSprite BakeGrizzlyBeast(int stride) =>
        EgaSprite.Bake(22, 26, b =>
        {
            const int ox = 4;

            // Round bear ears & fierce head
            b.FillRect(ox + 2, 0, 3, 3, EgaColorIndex.Brown);
            b.FillRect(ox + 9, 0, 3, 3, EgaColorIndex.Brown);
            b.FillRect(ox + 3, 1, 8, 6, EgaColorIndex.Brown);
            b.DrawRect(ox + 3, 1, 8, 6, EgaColorIndex.Black);
            b.SetPixel(ox + 8, 3, EgaColorIndex.BrightRed);  // Glowing eye
            b.FillRect(ox + 9, 4, 3, 3, EgaColorIndex.Yellow); // Snout/fangs

            // Massive fur shoulders & torso
            b.FillRect(ox + 1, 7, 12, 12, EgaColorIndex.Brown);
            b.DrawRect(ox + 1, 7, 12, 12, EgaColorIndex.Black);
            b.FillRect(ox + 4, 9, 6, 8, EgaColorIndex.Yellow); // Chest fur patch

            // Outstretched heavy claw arm (hurling forest projectiles)
            b.FillRect(ox + 10, 10, 7, 5, EgaColorIndex.Brown);
            b.DrawRect(ox + 10, 10, 7, 5, EgaColorIndex.Black);
            b.DrawVLine(ox + 17, 10, 5, EgaColorIndex.BrightWhite); // Sharp claws

            // Heavy bear legs
            b.FillRect(ox + 2, 19 + stride, 4, 7, EgaColorIndex.Brown);
            b.DrawRect(ox + 2, 19 + stride, 4, 7, EgaColorIndex.Black);
            b.FillRect(ox + 8, 19 - stride, 4, 7, EgaColorIndex.Brown);
            b.DrawRect(ox + 8, 19 - stride, 4, 7, EgaColorIndex.Black);
        }, offsetX: -4, offsetY: 0);

    // --- Projectiles: Hunting Arrow & Venom Spit ---

    private static EgaSprite BakeHuntingArrow() =>
        EgaSprite.Bake(8, 3, b =>
        {
            // White fletching feathers on left (x=0..1)
            b.SetPixel(0, 0, EgaColorIndex.BrightWhite);
            b.SetPixel(0, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(1, 0, EgaColorIndex.BrightRed);
            b.SetPixel(1, 2, EgaColorIndex.BrightRed);

            // Wooden shaft down the middle (y=1)
            b.DrawHLine(0, 1, 6, EgaColorIndex.Yellow);

            // Sharp steel arrowhead on right (x=5..7)
            b.DrawVLine(5, 0, 3, EgaColorIndex.BrightWhite);
            b.SetPixel(6, 1, EgaColorIndex.BrightWhite);
            b.SetPixel(7, 1, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeVenomSpit() =>
        EgaSprite.Bake(6, 6, b =>
        {
            b.FillRect(0, 0, 6, 6, EgaColorIndex.Green);
            b.FillRect(1, 1, 4, 4, EgaColorIndex.BrightGreen);
            b.FillRect(2, 2, 2, 2, EgaColorIndex.Yellow);
            b.DrawRect(0, 0, 6, 6, EgaColorIndex.Black);
        });

    // --- Pickups: Forest Berries, Honeycomb & Herb Pouch ---

    private static EgaSprite BakeDewBerry(EgaColorIndex berryCol, bool glintLarge) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Green leafy stem on top
            b.FillRect(5, 1, 4, 2, EgaColorIndex.BrightGreen);
            // Plump berry body
            b.FillRect(3, 3, 8, 8, berryCol);
            b.DrawRect(3, 3, 8, 8, EgaColorIndex.Black);
            int size = glintLarge ? 3 : 2;
            b.FillRect(5, 4, size, size, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeHoneycomb(bool bright) =>
        EgaSprite.Bake(14, 14, b =>
        {
            b.FillRect(2, 2, 10, 9, EgaColorIndex.Yellow);
            b.DrawRect(2, 2, 10, 9, EgaColorIndex.Black);
            // Hexagonal comb cells
            b.SetPixel(4, 4, EgaColorIndex.Brown);
            b.SetPixel(7, 4, EgaColorIndex.Brown);
            b.SetPixel(5, 7, EgaColorIndex.Brown);
            b.SetPixel(8, 7, EgaColorIndex.Brown);
            if (bright)
            {
                b.FillRect(3, 3, 2, 2, EgaColorIndex.BrightWhite);
            }
        });

    private static EgaSprite BakeHerbPouch(bool bright) =>
        EgaSprite.Bake(14, 14, b =>
        {
            b.FillRect(2, 3, 10, 8, EgaColorIndex.BrightWhite);
            b.DrawRect(2, 3, 10, 8, EgaColorIndex.Black);
            var crossCol = bright ? EgaColorIndex.BrightGreen : EgaColorIndex.Green;
            b.FillRect(6, 5, 2, 4, crossCol);
            b.FillRect(5, 6, 4, 2, crossCol);
        });
}
