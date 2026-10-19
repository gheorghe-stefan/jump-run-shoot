using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Authentic Duke Nukem 1 (1991) Classic Theme.
/// Defined strictly as a static collection of standard-sized 16-color EGA sprites and palette indices.
/// All rendering logic, parallax math, and animations are handled by <see cref="GameRenderer"/>.
/// </summary>
public sealed class DefaultTheme : SpriteThemeBase
{
    public static readonly ThemeAssetPack StaticAssets = CreateAssets();

    public override ThemeAssetPack Assets => StaticAssets;

    private static ThemeAssetPack CreateAssets()
    {
        var rapidBadge = BakeBadge("R", EgaColorIndex.BrightRed, EgaColorIndex.Yellow);
        var jumpBadge = BakeBadge("J", EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite);
        var speedBadge = BakeBadge("S", EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite);

        return new ThemeAssetPack
        {
            Name = "Duke 1 Classic",

            // 1. Background Colors & Static Sprites
            SkyColor = EgaColorIndex.Black,
            WallFillColor = EgaColorIndex.DarkGray,
            WallTopBorderColor = EgaColorIndex.Black,
            WallHighlightColor = EgaColorIndex.LightGray,
            StarCrossColor = EgaColorIndex.BrightCyan,
            Stars = new (int, int, EgaColorIndex)[]
            {
                (20, 20, EgaColorIndex.BrightCyan), (25, 22, EgaColorIndex.BrightBlue), (28, 19, EgaColorIndex.BrightBlue),
                (65, 35, EgaColorIndex.BrightWhite), (68, 38, EgaColorIndex.BrightCyan), (72, 34, EgaColorIndex.BrightBlue),
                (110, 24, EgaColorIndex.BrightCyan), (115, 25, EgaColorIndex.BrightBlue), (118, 22, EgaColorIndex.BrightWhite),
                (160, 42, EgaColorIndex.BrightBlue), (164, 40, EgaColorIndex.BrightCyan), (168, 44, EgaColorIndex.BrightBlue),
                (210, 28, EgaColorIndex.BrightWhite), (214, 30, EgaColorIndex.BrightCyan), (218, 26, EgaColorIndex.BrightBlue),
                (255, 36, EgaColorIndex.BrightCyan), (260, 38, EgaColorIndex.BrightBlue), (265, 35, EgaColorIndex.BrightWhite),
                (295, 22, EgaColorIndex.BrightCyan), (300, 25, EgaColorIndex.BrightBlue), (305, 20, EgaColorIndex.BrightBlue),
                (45, 60, EgaColorIndex.BrightBlue), (135, 55, EgaColorIndex.BrightCyan), (230, 62, EgaColorIndex.BrightWhite)
            },
            BackgroundPillar = BakePillar(EgaColorIndex.DarkGray, EgaColorIndex.LightGray),
            BackgroundMonitor = BakeMonitor(EgaColorIndex.LightGray, EgaColorIndex.DarkGray, EgaColorIndex.BrightRed),
            BackgroundServerRack = BakeServerRack(EgaColorIndex.BrightWhite, EgaColorIndex.DarkGray, EgaColorIndex.BrightCyan, EgaColorIndex.BrightGreen),
            BackgroundWallCrack = BakeWallCrack(EgaColorIndex.Black),

            // 2. Standard 16x16 Tiles
            SolidTile = BakeSolidTile(EgaColorIndex.LightGray, EgaColorIndex.BrightWhite, EgaColorIndex.DarkGray),
            OneWayTileVariants = new[]
            {
                BakeOneWayTile(0, EgaColorIndex.Yellow, EgaColorIndex.BrightRed, EgaColorIndex.BrightWhite),
                BakeOneWayTile(1, EgaColorIndex.Yellow, EgaColorIndex.BrightRed, EgaColorIndex.BrightWhite)
            },
            HazardTile = BakeHazardSpikes(EgaColorIndex.LightGray, EgaColorIndex.BrightWhite, EgaColorIndex.BrightRed),
            BorderTile = BakeBorderBulkhead(EgaColorIndex.LightGray, EgaColorIndex.BrightWhite, EgaColorIndex.DarkGray, EgaColorIndex.Yellow),
            LadderTile = BakeLadder(EgaColorIndex.BrightWhite, EgaColorIndex.LightGray),
            CrateTile = BakeSupplyCrate(EgaColorIndex.Brown, EgaColorIndex.Yellow, EgaColorIndex.LightGray),

            // 3. Player Sprites
            PlayerIdle = BakePlayerAction(legStep: 0, gunBob: 0, EgaColorIndex.Yellow, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightBlue),
            PlayerWalk = new[]
            {
                BakePlayerAction(legStep: 2, gunBob: 0, EgaColorIndex.Yellow, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightBlue),
                BakePlayerAction(legStep: 0, gunBob: 1, EgaColorIndex.Yellow, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightBlue),
                BakePlayerAction(legStep: -2, gunBob: 0, EgaColorIndex.Yellow, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightBlue),
                BakePlayerAction(legStep: 0, gunBob: 1, EgaColorIndex.Yellow, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightBlue)
            },
            PlayerJump = BakePlayerAction(legStep: -1, gunBob: -1, EgaColorIndex.Yellow, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightBlue),
            PlayerClimb = new[]
            {
                BakePlayerClimb(step: 1, EgaColorIndex.Yellow, EgaColorIndex.BrightRed, EgaColorIndex.BrightBlue),
                BakePlayerClimb(step: -1, EgaColorIndex.Yellow, EgaColorIndex.BrightRed, EgaColorIndex.BrightBlue)
            },
            SpeedTrailColor = EgaColorIndex.BrightCyan,

            // 4. Monster Sprites
            PatrolDroid = new[]
            {
                BakePatrolDroid(visorGlintX: 5, EgaColorIndex.LightGray, EgaColorIndex.BrightRed, EgaColorIndex.Yellow),
                BakePatrolDroid(visorGlintX: 7, EgaColorIndex.LightGray, EgaColorIndex.BrightRed, EgaColorIndex.Yellow)
            },
            HoverDrone = new[]
            {
                BakeHoverDrone(flameHigh: true, EgaColorIndex.LightGray, EgaColorIndex.BrightCyan, EgaColorIndex.Yellow),
                BakeHoverDrone(flameHigh: false, EgaColorIndex.LightGray, EgaColorIndex.BrightCyan, EgaColorIndex.Yellow)
            },
            CeilingTurret = new[]
            {
                BakeCeilingTurret(opticBright: true, EgaColorIndex.LightGray, EgaColorIndex.BrightCyan),
                BakeCeilingTurret(opticBright: false, EgaColorIndex.LightGray, EgaColorIndex.BrightCyan)
            },
            MechaHound = new[]
            {
                BakeMechaHound(step: 2, EgaColorIndex.LightGray, EgaColorIndex.BrightRed, EgaColorIndex.DarkGray),
                BakeMechaHound(step: -2, EgaColorIndex.LightGray, EgaColorIndex.BrightRed, EgaColorIndex.DarkGray)
            },
            HeavyGuard = new[]
            {
                BakeHeavyGuard(stride: 1, EgaColorIndex.DarkGray, EgaColorIndex.LightGray, EgaColorIndex.Yellow, EgaColorIndex.BrightRed),
                BakeHeavyGuard(stride: -1, EgaColorIndex.DarkGray, EgaColorIndex.LightGray, EgaColorIndex.Yellow, EgaColorIndex.BrightRed)
            },

            // 5. Projectile Sprites
            PlayerProjectile = BakePlayerProjectile(EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite),
            MonsterProjectile = BakeMonsterProjectile(EgaColorIndex.BrightRed, EgaColorIndex.Yellow),

            // 6. Pickup Sprites (2 frames each for animated glow/glint)
            BlueGem = new[]
            {
                BakeGem(EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite, glintLarge: true),
                BakeGem(EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite, glintLarge: false)
            },
            RedRuby = new[]
            {
                BakeGem(EgaColorIndex.BrightRed, EgaColorIndex.Yellow, glintLarge: true),
                BakeGem(EgaColorIndex.BrightRed, EgaColorIndex.BrightWhite, glintLarge: false)
            },
            GoldCartridge = new[]
            {
                BakeCartridge(EgaColorIndex.BrightWhite, EgaColorIndex.BrightCyan),
                BakeCartridge(EgaColorIndex.BrightWhite, EgaColorIndex.Yellow)
            },
            MedKit = new[]
            {
                BakeMedKit(EgaColorIndex.BrightWhite, EgaColorIndex.BrightRed),
                BakeMedKit(EgaColorIndex.BrightWhite, EgaColorIndex.Red)
            },
            RapidFireBadge = rapidBadge,
            HighJumpBadge = jumpBadge,
            SpeedDashBadge = speedBadge,

            // 7. HUD Palette
            HudBgColor = EgaColorIndex.LightGray,
            HudBorderColor = EgaColorIndex.Black,
            HudBevelColor = EgaColorIndex.BrightWhite,
            HudInsetBorderColor = EgaColorIndex.DarkGray,
            ScoreLabelColor = EgaColorIndex.Yellow,
            HealthFullColor = EgaColorIndex.BrightGreen,
            HealthEmptyColor = EgaColorIndex.Red,
            SeedTextColor = EgaColorIndex.BrightCyan
        };
    }

    internal static EgaSprite BakePillar(EgaColorIndex fill, EgaColorIndex rib) =>
        EgaSprite.Bake(20, 64, b =>
        {
            b.FillRect(0, 0, 20, 64, fill);
            b.DrawVLine(0, 0, 64, rib);
            b.DrawVLine(4, 0, 64, rib);
            b.DrawVLine(8, 0, 64, rib);
            b.DrawVLine(12, 0, 64, rib);
            b.DrawVLine(16, 0, 64, rib);
            b.DrawVLine(19, 0, 64, EgaColorIndex.Black);
        });

    internal static EgaSprite BakeMonitor(EgaColorIndex frame, EgaColorIndex screen, EgaColorIndex trace) =>
        EgaSprite.Bake(20, 16, b =>
        {
            b.FillRect(0, 0, 20, 16, frame);
            b.DrawRect(0, 0, 20, 16, EgaColorIndex.Black);
            b.FillRect(2, 2, 16, 12, screen);
            b.DrawRect(2, 2, 16, 12, EgaColorIndex.Black);
            b.DrawHLine(3, 11, 14, EgaColorIndex.BrightWhite);
            b.SetPixel(4, 10, trace);
            b.SetPixel(5, 9, trace);
            b.SetPixel(6, 8, trace);
            b.SetPixel(7, 9, trace);
            b.SetPixel(8, 7, trace);
            b.SetPixel(9, 5, trace);
            b.SetPixel(10, 6, trace);
            b.SetPixel(11, 4, trace);
            b.SetPixel(12, 5, trace);
        });

    internal static EgaSprite BakeServerRack(EgaColorIndex chassis, EgaColorIndex bay, EgaColorIndex ledA, EgaColorIndex ledB) =>
        EgaSprite.Bake(28, 14, b =>
        {
            b.FillRect(0, 0, 28, 14, chassis);
            b.DrawRect(0, 0, 28, 14, EgaColorIndex.Black);
            b.FillRect(2, 2, 24, 10, EgaColorIndex.Black);
            b.FillRect(4, 4, 5, 6, bay);
            b.SetPixel(5, 6, ledA);
            b.FillRect(11, 4, 5, 6, bay);
            b.SetPixel(12, 6, ledA);
            b.FillRect(18, 4, 5, 6, bay);
            b.SetPixel(19, 6, ledB);
        });

    internal static EgaSprite BakeWallCrack(EgaColorIndex color) =>
        EgaSprite.Bake(6, 8, b =>
        {
            b.SetPixel(0, 0, color);
            b.SetPixel(1, 1, color);
            b.SetPixel(0, 2, color);
            b.SetPixel(2, 3, color);
            b.SetPixel(1, 4, color);
            b.SetPixel(2, 5, color);
        });

    internal static EgaSprite BakeSolidTile(EgaColorIndex body, EgaColorIndex topHighlight, EgaColorIndex groove) =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.FillRect(0, 0, 16, 16, body);
            b.DrawHLine(0, 0, 16, topHighlight);
            b.DrawHLine(0, 1, 16, topHighlight);
            b.SetPixel(0, 0, EgaColorIndex.Black);
            b.SetPixel(0, 1, groove);
            b.DrawVLine(0, 2, 14, EgaColorIndex.Black);
            b.DrawHLine(0, 4, 16, groove);
            b.DrawHLine(0, 15, 16, EgaColorIndex.Black);
        });

    internal static EgaSprite BakeOneWayTile(int colParity, EgaColorIndex core, EgaColorIndex border, EgaColorIndex stripeCol) =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.FillRect(0, 0, 16, 8, core);
            b.DrawHLine(0, 0, 16, border);
            b.DrawHLine(0, 7, 16, border);
            b.DrawVLine(0, 0, 8, border);
            b.DrawVLine(15, 0, 8, border);

            for (int py = 1; py < 7; py++)
            {
                for (int px = 1; px < 15; px++)
                {
                    int stripe = (colParity * 16 + px + py) % 8;
                    if (stripe < 3)
                    {
                        b.SetPixel(px, py, stripeCol);
                    }
                }
            }
        });

    internal static EgaSprite BakeHazardSpikes(EgaColorIndex body, EgaColorIndex edge, EgaColorIndex tip) =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.DrawHLine(0, 15, 16, EgaColorIndex.Black);
            b.DrawHLine(0, 14, 16, EgaColorIndex.DarkGray);

            int[] centers = { 4, 12 };
            foreach (int sx in centers)
            {
                for (int h = 0; h < 11; h++)
                {
                    int sy = 13 - h;
                    int halfW = (h * 3) / 11;
                    b.SetPixel(sx - halfW, sy, edge);
                    if (halfW > 0)
                    {
                        b.FillRect(sx - halfW + 1, sy, halfW * 2 - 1, 1, body);
                    }
                    b.SetPixel(sx + halfW, sy, EgaColorIndex.Black);
                }
                b.SetPixel(sx, 2, tip);
                b.SetPixel(sx, 3, edge);
            }
        });

    internal static EgaSprite BakeBorderBulkhead(EgaColorIndex face, EgaColorIndex highlight, EgaColorIndex shadow, EgaColorIndex bolt) =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.FillRect(0, 0, 16, 16, face);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            b.DrawHLine(1, 1, 14, highlight);
            b.DrawVLine(1, 1, 14, highlight);
            b.DrawHLine(1, 14, 14, shadow);
            b.DrawVLine(14, 1, 14, shadow);
            b.DrawRect(3, 3, 10, 10, shadow);
            b.DrawRect(4, 4, 8, 8, EgaColorIndex.Black);
            b.SetPixel(2, 2, bolt);
            b.SetPixel(13, 2, bolt);
            b.SetPixel(2, 13, bolt);
            b.SetPixel(13, 13, bolt);
        });

    internal static EgaSprite BakeLadder(EgaColorIndex railHighlight, EgaColorIndex railBody) =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.DrawVLine(2, 0, 16, railHighlight);
            b.DrawVLine(3, 0, 16, railBody);
            b.DrawVLine(4, 0, 16, EgaColorIndex.Black);

            b.DrawVLine(11, 0, 16, railHighlight);
            b.DrawVLine(12, 0, 16, railBody);
            b.DrawVLine(13, 0, 16, EgaColorIndex.Black);

            for (int ry = 2; ry < 16; ry += 4)
            {
                b.DrawHLine(4, ry, 8, railHighlight);
                b.DrawHLine(4, ry + 1, 8, EgaColorIndex.Black);
            }
        });

    internal static EgaSprite BakeSupplyCrate(EgaColorIndex wood, EgaColorIndex brace, EgaColorIndex bracket) =>
        EgaSprite.Bake(16, 16, b =>
        {
            b.FillRect(0, 0, 16, 16, wood);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);
            b.DrawHLine(1, 5, 14, EgaColorIndex.Black);
            b.DrawHLine(1, 10, 14, EgaColorIndex.Black);

            for (int d = 2; d < 14; d++)
            {
                b.SetPixel(d, d, brace);
                b.SetPixel(15 - d, d, brace);
            }

            b.FillRect(1, 1, 3, 3, bracket);
            b.FillRect(12, 1, 3, 3, bracket);
            b.FillRect(1, 12, 3, 3, bracket);
            b.FillRect(12, 12, 3, 3, bracket);
            b.SetPixel(2, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(2, 13, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 13, EgaColorIndex.BrightWhite);
        });

    internal static EgaSprite BakePlayerAction(int legStep, int gunBob, EgaColorIndex hairCol, EgaColorIndex vestCol, EgaColorIndex pantsCol) =>
        EgaSprite.Bake(24, 24, b =>
        {
            const int ox = 5; // Shifts player X=0..13 to centered sprite X=5..18

            // 1. Hair
            b.FillRect(ox + 2, 0, 8, 4, hairCol);
            b.DrawHLine(ox + 3, 0, 6, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 2, 0, 8, 4, EgaColorIndex.Black);

            // 2. Face
            b.FillRect(ox + 2, 4, 8, 4, EgaColorIndex.BrightRed);
            b.DrawRect(ox + 2, 4, 8, 4, EgaColorIndex.Black);
            b.FillRect(ox + 6, 4, 3, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(ox + 8, 4, EgaColorIndex.Black);
            b.SetPixel(ox + 9, 5, EgaColorIndex.Black);
            b.SetPixel(ox + 7, 6, EgaColorIndex.Black);

            // 3. Tank Top & Arm
            b.FillRect(ox + 3, 8, 8, 6, vestCol);
            b.DrawRect(ox + 3, 8, 8, 6, EgaColorIndex.Black);
            b.FillRect(ox + 7, 8, 2, 2, EgaColorIndex.BrightRed);

            b.FillRect(ox + 1, 8, 3, 6, EgaColorIndex.BrightRed);
            b.DrawRect(ox + 1, 8, 3, 6, EgaColorIndex.Black);

            // 4. Chrome Blaster (aimed right; GameRenderer flips automatically when facing left)
            int gunY = 10 + gunBob;
            b.FillRect(ox + 10, gunY, 7, 3, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 10, gunY, 7, 3, EgaColorIndex.Black);
            b.SetPixel(ox + 12, gunY + 1, EgaColorIndex.DarkGray);
            b.SetPixel(ox + 14, gunY + 1, EgaColorIndex.DarkGray);
            b.FillRect(ox + 10, gunY + 2, 2, 3, EgaColorIndex.Black);
            b.SetPixel(ox + 17, gunY + 1, EgaColorIndex.BrightCyan);

            // 5. Utility Belt
            b.FillRect(ox + 3, 14, 8, 2, EgaColorIndex.Black);
            b.SetPixel(ox + 5, 14, EgaColorIndex.Yellow);
            b.SetPixel(ox + 6, 14, EgaColorIndex.BrightWhite);

            // 6. Pants
            b.FillRect(ox + 3, 16, 8, 4, pantsCol);
            b.DrawRect(ox + 3, 16, 8, 4, EgaColorIndex.Black);
            b.DrawVLine(ox + 6, 16, 4, EgaColorIndex.Blue);

            // 7. Boots
            int bootLeftY = 20 + legStep;
            int bootRightY = 20 - legStep;
            b.FillRect(ox + 2, bootLeftY, 4, 4, pantsCol);
            b.DrawRect(ox + 2, bootLeftY, 4, 4, EgaColorIndex.Black);
            b.DrawHLine(ox + 2, bootLeftY + 3, 4, EgaColorIndex.Black);

            b.FillRect(ox + 8, bootRightY, 4, 4, pantsCol);
            b.DrawRect(ox + 8, bootRightY, 4, 4, EgaColorIndex.Black);
            b.DrawHLine(ox + 8, bootRightY + 3, 4, EgaColorIndex.Black);
        }, offsetX: -5, offsetY: 0);

    internal static EgaSprite BakePlayerClimb(int step, EgaColorIndex hairCol, EgaColorIndex vestCol, EgaColorIndex pantsCol) =>
        EgaSprite.Bake(14, 24, b =>
        {
            b.FillRect(3, 0, 8, 4, hairCol);
            b.DrawHLine(4, 0, 6, EgaColorIndex.BrightWhite);
            b.DrawRect(3, 0, 8, 4, EgaColorIndex.Black);

            b.FillRect(2, 4, 10, 9, vestCol);
            b.DrawRect(2, 4, 10, 9, EgaColorIndex.Black);
            b.DrawVLine(6, 5, 6, EgaColorIndex.Red);

            int leftArmY = 4 - (step * 2);
            int rightArmY = 4 + (step * 2);
            b.FillRect(0, leftArmY, 3, 5, EgaColorIndex.BrightRed);
            b.DrawRect(0, leftArmY, 3, 5, EgaColorIndex.Black);
            b.FillRect(11, rightArmY, 3, 5, EgaColorIndex.BrightRed);
            b.DrawRect(11, rightArmY, 3, 5, EgaColorIndex.Black);

            b.FillRect(2, 13, 10, 2, EgaColorIndex.Black);
            b.SetPixel(6, 13, EgaColorIndex.Yellow);
            b.SetPixel(7, 13, EgaColorIndex.BrightWhite);

            b.FillRect(3, 15, 8, 5, pantsCol);
            b.DrawRect(3, 15, 8, 5, EgaColorIndex.Black);

            int leftBootY = 20 + (step * 2);
            int rightBootY = 20 - (step * 2);
            b.FillRect(2, leftBootY, 4, 3, EgaColorIndex.DarkGray);
            b.DrawRect(2, leftBootY, 4, 3, EgaColorIndex.Black);
            b.FillRect(8, rightBootY, 4, 3, EgaColorIndex.DarkGray);
            b.DrawRect(8, rightBootY, 4, 3, EgaColorIndex.Black);
        });

    internal static EgaSprite BakePatrolDroid(int visorGlintX, EgaColorIndex headCol, EgaColorIndex visorCol, EgaColorIndex neckCol) =>
        EgaSprite.Bake(14, 14, b =>
        {
            b.FillRect(2, 1, 10, 5, headCol);
            b.DrawRect(2, 1, 10, 5, EgaColorIndex.Black);
            b.DrawHLine(3, 3, 8, visorCol);
            b.SetPixel(visorGlintX, 3, EgaColorIndex.BrightWhite);

            b.DrawHLine(4, 6, 6, neckCol);
            b.DrawHLine(4, 7, 6, EgaColorIndex.Black);
            b.DrawHLine(4, 8, 6, neckCol);
            b.DrawHLine(4, 9, 6, EgaColorIndex.Black);

            b.FillRect(2, 10, 10, 4, EgaColorIndex.Black);
            b.DrawHLine(3, 10, 8, headCol);
            b.DrawHLine(1, 13, 12, headCol);
            b.DrawRect(2, 10, 10, 4, EgaColorIndex.Black);
        });

    internal static EgaSprite BakeHoverDrone(bool flameHigh, EgaColorIndex bodyCol, EgaColorIndex lensCol, EgaColorIndex flameCol) =>
        EgaSprite.Bake(14, 12, b =>
        {
            b.FillRect(2, 2, 10, 6, bodyCol);
            b.DrawRect(2, 2, 10, 6, EgaColorIndex.Black);
            b.FillRect(4, 4, 2, 2, lensCol);
            b.FillRect(8, 4, 2, 2, lensCol);
            b.FillRect(0, 4, 2, 2, EgaColorIndex.DarkGray);
            b.FillRect(12, 4, 2, 2, EgaColorIndex.DarkGray);
            b.FillRect(5, 8, 4, flameHigh ? 4 : 2, flameCol);
            b.DrawHLine(6, 8, 2, EgaColorIndex.BrightWhite);
        });

    internal static EgaSprite BakeCeilingTurret(bool opticBright, EgaColorIndex camCol, EgaColorIndex lensCol) =>
        EgaSprite.Bake(14, 12, b =>
        {
            b.FillRect(3, 0, 8, 3, EgaColorIndex.Black);
            b.DrawHLine(2, 0, 10, EgaColorIndex.DarkGray);
            b.FillRect(4, 3, 6, 8, camCol);
            b.DrawRect(4, 3, 6, 8, EgaColorIndex.Black);
            b.FillRect(5, 9, 4, 3, lensCol);
            b.DrawRect(5, 9, 4, 3, EgaColorIndex.Black);
            if (opticBright)
            {
                b.SetPixel(6, 10, EgaColorIndex.BrightWhite);
            }
        });

    internal static EgaSprite BakeMechaHound(int step, EgaColorIndex bodyCol, EgaColorIndex eyeCol, EgaColorIndex legCol) =>
        EgaSprite.Bake(24, 14, b =>
        {
            // Baked facing Right; GameRenderer mirrors when facing Left
            b.FillRect(6, 3, 12, 6, bodyCol);
            b.DrawRect(6, 3, 12, 6, EgaColorIndex.Black);
            b.FillRect(16, 2, 8, 6, bodyCol);
            b.DrawRect(16, 2, 8, 6, EgaColorIndex.Black);
            b.SetPixel(22, 3, eyeCol);

            b.FillRect(17, 9 + step, 2, 5, legCol);
            b.FillRect(7, 9 - step, 2, 5, legCol);
        });

    internal static EgaSprite BakeHeavyGuard(int stride, EgaColorIndex torsoCol, EgaColorIndex armorCol, EgaColorIndex pauldronCol, EgaColorIndex visorCol) =>
        EgaSprite.Bake(22, 26, b =>
        {
            const int ox = 4; // Shifts 14px body to centered X=4..17
            b.FillRect(ox + 3, 1, 8, 6, armorCol);
            b.DrawRect(ox + 3, 1, 8, 6, EgaColorIndex.Black);
            b.DrawHLine(ox + 4, 3, 6, visorCol);

            b.FillRect(ox + 1, 7, 12, 4, pauldronCol);
            b.DrawRect(ox + 1, 7, 12, 4, EgaColorIndex.Black);

            b.FillRect(ox + 2, 11, 10, 8, torsoCol);
            b.DrawRect(ox + 2, 11, 10, 8, EgaColorIndex.Black);

            // Cannon aimed right; mirrored automatically when facing left
            b.FillRect(ox + 10, 10, 8, 5, armorCol);
            b.DrawRect(ox + 10, 10, 8, 5, EgaColorIndex.Black);

            b.FillRect(ox + 2, 19 + stride, 4, 7, armorCol);
            b.DrawRect(ox + 2, 19 + stride, 4, 7, EgaColorIndex.Black);
            b.FillRect(ox + 8, 19 - stride, 4, 7, armorCol);
            b.DrawRect(ox + 8, 19 - stride, 4, 7, EgaColorIndex.Black);
        }, offsetX: -4, offsetY: 0);

    internal static EgaSprite BakePlayerProjectile(EgaColorIndex outer, EgaColorIndex core) =>
        EgaSprite.Bake(8, 3, b =>
        {
            b.FillRect(0, 0, 8, 3, outer);
            b.DrawHLine(1, 1, 6, core);
            b.DrawRect(0, 0, 8, 3, EgaColorIndex.Black);
        });

    internal static EgaSprite BakeMonsterProjectile(EgaColorIndex outer, EgaColorIndex inner) =>
        EgaSprite.Bake(6, 6, b =>
        {
            b.FillRect(0, 0, 6, 6, outer);
            b.FillRect(1, 1, 4, 4, inner);
            b.FillRect(2, 2, 2, 2, EgaColorIndex.BrightWhite);
            b.DrawRect(0, 0, 6, 6, EgaColorIndex.Black);
        });

    internal static EgaSprite BakeGem(EgaColorIndex body, EgaColorIndex facet, bool glintLarge) =>
        EgaSprite.Bake(14, 14, b =>
        {
            b.FillRect(3, 2, 8, 8, body);
            b.DrawRect(3, 2, 8, 8, EgaColorIndex.Black);
            int size = glintLarge ? 3 : 2;
            b.FillRect(5, 3, size, size, facet);
        });

    internal static EgaSprite BakeCartridge(EgaColorIndex body, EgaColorIndex coil) =>
        EgaSprite.Bake(14, 14, b =>
        {
            b.FillRect(2, 2, 9, 8, body);
            b.DrawRect(2, 2, 9, 8, EgaColorIndex.Black);
            b.DrawVLine(11, 3, 6, coil);
            b.SetPixel(10, 2, coil);
            b.SetPixel(10, 9, coil);
        });

    internal static EgaSprite BakeMedKit(EgaColorIndex box, EgaColorIndex cross) =>
        EgaSprite.Bake(14, 14, b =>
        {
            b.FillRect(2, 3, 10, 8, box);
            b.DrawRect(2, 3, 10, 8, EgaColorIndex.Black);
            b.FillRect(6, 5, 2, 4, cross);
            b.FillRect(5, 6, 4, 2, cross);
        });

    internal static EgaSprite BakeBadge(string letter, EgaColorIndex bgCol, EgaColorIndex textCol) =>
        EgaSprite.Bake(14, 14, b =>
        {
            b.FillRect(1, 1, 12, 12, bgCol);
            b.DrawRect(1, 1, 12, 12, EgaColorIndex.Black);
            b.DrawHLine(2, 2, 10, EgaColorIndex.BrightWhite);
            b.DrawText(4, 3, letter, textCol);
        });
}
