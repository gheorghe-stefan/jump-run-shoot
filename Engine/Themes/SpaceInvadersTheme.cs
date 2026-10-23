using System;
using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// "Space Invaders" Theme (Retro Arcade Sci-Fi & Starfighters):
/// A complete graphical, physics, and auditory overhaul inspired by classic 80s/90s arcade space shooters.
/// Features a sleek Starfighter defender spacecraft for the player, orbiting space station titanium hull
/// platforms, holographic hard-light girders, plasma defense vents, tractor beam gravity lifts,
/// and menacing alien invader starships (Scout Saucers, Flying UFOs, Orbital Defense Satellites,
/// Fast Attack Interceptors, and massive Alien Mothership Dreadnoughts).
/// Zero-gravity physics is inherently active, granting 4-way starfighter spaceflight while strictly respecting platforms!
/// </summary>
public sealed class SpaceInvadersTheme : SpriteThemeBase
{
    public const string ThemeId = "space";
    public const string DisplayName = "Space Invaders";

    public override bool ZeroGravity => true;
    public override string BgmTrack => "audio/bgm_space.wav";

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
            BackgroundFar = BakeSpaceFarLayer(),
            BackgroundMid = BakeSpaceMidLayer(),
            BackgroundNear = BakeSpaceNearLayer(),

            // 2. Standard 16x16 World Tiles
            SolidTile = BakeStationHullPlate(),
            OneWayTileVariants = new[]
            {
                BakeHardLightGirder(parity: 0),
                BakeHardLightGirder(parity: 1)
            },
            HazardTile = BakePlasmaDefenseVents(),
            BorderTile = BakeReinforcedBulkhead(),
            LadderTile = BakeTractorBeamConduit(),
            CrateTile = BakeAntimatterCargoPod(),

            // 3. Player: Starfighter Spacecraft (24x24 with OffsetX = -5; Climb 14x24)
            PlayerIdle = BakeStarfighterAction(thrustLevel: 0, pitchAngle: 0),
            PlayerWalk = new[]
            {
                BakeStarfighterAction(thrustLevel: 1, pitchAngle: 0),
                BakeStarfighterAction(thrustLevel: 2, pitchAngle: 1),
                BakeStarfighterAction(thrustLevel: 3, pitchAngle: 0),
                BakeStarfighterAction(thrustLevel: 2, pitchAngle: -1)
            },
            PlayerJump = BakeStarfighterAction(thrustLevel: 3, pitchAngle: 2), // High-thrust booster climb
            PlayerClimb = new[]
            {
                BakeStarfighterVertical(subFrame: 0),
                BakeStarfighterVertical(subFrame: 1)
            },

            // 4. Monsters: Alien Invader Vessels (100% Spacecraft / Flying Invaders, No Walking Legs)
            PatrolDroid = new[]
            {
                BakeAlienScoutSaucer(frame: 0),
                BakeAlienScoutSaucer(frame: 1)
            },
            HoverDrone = new[]
            {
                BakeFlyingSaucerUfo(frame: 0),
                BakeFlyingSaucerUfo(frame: 1)
            },
            CeilingTurret = new[]
            {
                BakeOrbitalSatellite(frame: 0),
                BakeOrbitalSatellite(frame: 1)
            },
            MechaHound = new[]
            {
                BakeFastAttackInterceptor(frame: 0),
                BakeFastAttackInterceptor(frame: 1)
            },
            HeavyGuard = new[]
            {
                BakeAlienMothership(frame: 0),
                BakeAlienMothership(frame: 1)
            },

            // 5. Projectiles: Photon Laser Bolt & Alien Plasma Torpedo
            PlayerProjectile = BakePhotonLaserBolt(),
            MonsterProjectile = BakeAlienPlasmaTorpedo(),

            // 6. Pickups: Starlight Crystal, Plasma Ruby, Gold Data Cartridge & Nanite Repair Capsule
            BlueGem = new[]
            {
                BakeStarlightCrystal(glint: false),
                BakeStarlightCrystal(glint: true)
            },
            RedRuby = new[]
            {
                BakePlasmaRuby(glint: false),
                BakePlasmaRuby(glint: true)
            },
            GoldCartridge = new[]
            {
                BakeDataCartridge(glint: false),
                BakeDataCartridge(glint: true)
            },
            MedKit = new[]
            {
                BakeNaniteRepairPod(glint: false),
                BakeNaniteRepairPod(glint: true)
            },

            // Powerup HUD Badges (14x14)
            RapidFireBadge = BakeSpaceRapidBadge(),
            HighJumpBadge = BakeSpaceJumpBadge(),
            SpeedDashBadge = BakeSpaceSpeedBadge(),
            InvulnerabilityBadge = BakeSpaceInvulnBadge()
        };
    }

    // =========================================================================
    // 1. Horizontally-Repeating Parallax Background Layers (Height: 168px)
    // =========================================================================

    private static EgaSprite BakeSpaceFarLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Deep cosmic space void (Black)
            b.FillRect(0, 0, 320, 168, EgaColorIndex.Black);

            // 1. Distant Glowing Cosmic Nebula (rows 20..90, x: 40..180)
            for (int y = 20; y < 90; y++)
            {
                for (int x = 40; x < 180; x++)
                {
                    float dx = (x - 110) / 70.0f;
                    float dy = (y - 55) / 35.0f;
                    float distSq = dx * dx + dy * dy;
                    if (distSq < 1.0f)
                    {
                        int hash = (x * 7919 + y * 6271) % 100;
                        if (distSq < 0.35f && hash < 55)
                        {
                            b.SetPixel(x, y, (hash % 3 == 0) ? EgaColorIndex.BrightMagenta : EgaColorIndex.Magenta);
                        }
                        else if (distSq < 0.65f && hash < 35)
                        {
                            b.SetPixel(x, y, (hash % 2 == 0) ? EgaColorIndex.Blue : EgaColorIndex.BrightBlue);
                        }
                        else if (hash < 18)
                        {
                            b.SetPixel(x, y, EgaColorIndex.DarkGray);
                        }
                    }
                }
            }

            // 2. Large Cratered Ringed Planet / Moon (center x: 260, y: 48, radius: 24)
            int pcx = 260, pcy = 48, pr = 22;
            for (int y = pcy - pr; y <= pcy + pr; y++)
            {
                for (int x = pcx - pr; x <= pcx + pr; x++)
                {
                    int d2 = (x - pcx) * (x - pcx) + (y - pcy) * (y - pcy);
                    if (d2 <= pr * pr)
                    {
                        // Shading from bright upper-left sunlight to dark shadow
                        int shadeX = x - pcx + (y - pcy) / 2;
                        if (shadeX < -10)
                        {
                            b.SetPixel(x, y, EgaColorIndex.BrightCyan);
                        }
                        else if (shadeX < 2)
                        {
                            b.SetPixel(x, y, EgaColorIndex.Cyan);
                        }
                        else if (shadeX < 12)
                        {
                            b.SetPixel(x, y, EgaColorIndex.Blue);
                        }
                        else
                        {
                            b.SetPixel(x, y, EgaColorIndex.DarkGray);
                        }
                    }
                }
            }
            // Lunar craters
            b.FillRect(pcx - 8, pcy - 6, 5, 4, EgaColorIndex.Blue);
            b.DrawRect(pcx - 8, pcy - 6, 5, 4, EgaColorIndex.DarkGray);
            b.FillRect(pcx + 4, pcy + 4, 6, 4, EgaColorIndex.DarkGray);
            b.DrawRect(pcx + 4, pcy + 4, 6, 4, EgaColorIndex.Black);

            // Planetary Ring Arc passing across equator
            for (int rx = pcx - 38; rx <= pcx + 38; rx++)
            {
                int ry = pcy + (rx - pcx) / 4;
                int ringDist = Math.Abs(rx - pcx);
                if (ringDist > 14 || ry > pcy) // Show front half over planet
                {
                    b.SetPixel(rx, ry, EgaColorIndex.Yellow);
                    b.SetPixel(rx, ry + 1, EgaColorIndex.BrightWhite);
                    b.SetPixel(rx, ry + 2, EgaColorIndex.Brown);
                }
            }

            // 3. Multi-Colored Twinkling Starfield across the entire sky
            // Deterministic pseudo-random stars
            for (int i = 0; i < 180; i++)
            {
                int sx = (i * 173 + 19) % 320;
                int sy = (i * 97 + 31) % 168;

                // Don't draw over the planet
                if (Math.Abs(sx - pcx) < 26 && Math.Abs(sy - pcy) < 26) continue;

                if (i % 12 == 0)
                {
                    // Large 4-point cross star
                    b.SetPixel(sx, sy, EgaColorIndex.BrightWhite);
                    b.SetPixel(sx - 1, sy, EgaColorIndex.BrightCyan);
                    b.SetPixel(sx + 1, sy, EgaColorIndex.BrightCyan);
                    b.SetPixel(sx, sy - 1, EgaColorIndex.BrightCyan);
                    b.SetPixel(sx, sy + 1, EgaColorIndex.BrightCyan);
                }
                else if (i % 6 == 0)
                {
                    b.SetPixel(sx, sy, EgaColorIndex.BrightCyan);
                }
                else if (i % 5 == 0)
                {
                    b.SetPixel(sx, sy, EgaColorIndex.Yellow);
                }
                else if (i % 7 == 0)
                {
                    b.SetPixel(sx, sy, EgaColorIndex.BrightMagenta);
                }
                else
                {
                    b.SetPixel(sx, sy, EgaColorIndex.LightGray);
                }
            }
        });

    private static EgaSprite BakeSpaceMidLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Transparent midground backdrop: Drifting Asteroid Belt & Stellar Outpost Silhouettes
            // Asteroids drifting at intervals
            int[] asteroidPositions = { 15, 65, 125, 185, 245, 295 };
            int[] asteroidY = { 55, 115, 45, 95, 35, 125 };
            int[] asteroidR = { 9, 14, 7, 12, 8, 11 };

            for (int k = 0; k < asteroidPositions.Length; k++)
            {
                int ax = asteroidPositions[k];
                int ay = asteroidY[k];
                int ar = asteroidR[k];

                for (int y = ay - ar; y <= ay + ar; y++)
                {
                    for (int x = ax - ar; x <= ax + ar; x++)
                    {
                        // Irregular rocky shape
                        int hash = (x * 37 + y * 53) % 5;
                        int modR = ar - (hash > 2 ? 1 : 0);
                        int d2 = (x - ax) * (x - ax) + (y - ay) * (y - ay);
                        if (d2 <= modR * modR)
                        {
                            if (x < ax - ar / 3 && y < ay - ar / 3)
                            {
                                b.SetPixel(x, y, EgaColorIndex.LightGray); // Sunlit rock facet
                            }
                            else if (x > ax + ar / 4 || y > ay + ar / 4)
                            {
                                b.SetPixel(x, y, EgaColorIndex.DarkGray);  // Shadow side
                            }
                            else
                            {
                                b.SetPixel(x, y, EgaColorIndex.Brown);     // Mineral vein
                            }
                        }
                    }
                }
                // Black crater holes
                b.FillRect(ax - 2, ay - 1, 3, 2, EgaColorIndex.Black);
                b.FillRect(ax + 2, ay + 2, 2, 2, EgaColorIndex.Black);
            }

            // Distant Space Outpost / Relay Station silhouetted in midground (x: 140..175, y: 120..155)
            int sx = 150, sy = 130;
            b.FillRect(sx, sy, 22, 10, EgaColorIndex.DarkGray);
            b.DrawRect(sx, sy, 22, 10, EgaColorIndex.Black);
            b.DrawHLine(sx + 2, sy + 3, 18, EgaColorIndex.BrightCyan); // Observation window strip
            // Left & Right Solar Panels
            b.FillRect(sx - 16, sy + 2, 14, 6, EgaColorIndex.Blue);
            b.DrawRect(sx - 16, sy + 2, 14, 6, EgaColorIndex.Black);
            b.DrawHLine(sx - 14, sy + 4, 10, EgaColorIndex.BrightCyan);
            b.FillRect(sx + 24, sy + 2, 14, 6, EgaColorIndex.Blue);
            b.DrawRect(sx + 24, sy + 2, 14, 6, EgaColorIndex.Black);
            b.DrawHLine(sx + 26, sy + 4, 10, EgaColorIndex.BrightCyan);
            // Communications Antenna Mast
            b.DrawVLine(sx + 10, sy - 14, 14, EgaColorIndex.LightGray);
            b.SetPixel(sx + 10, sy - 15, EgaColorIndex.BrightRed); // Blinking navigational beacon
        });

    private static EgaSprite BakeSpaceNearLayer() =>
        EgaSprite.Bake(320, 168, b =>
        {
            // Transparent foreground layer: Space Station Outer Scaffold Trusses & Docking Gantries
            int[] gantryX = { 30, 120, 210, 290 };
            foreach (int gx in gantryX)
            {
                // Vertical industrial station strut truss (rows 0..167)
                for (int y = 0; y < 168; y += 16)
                {
                    // Truss vertical members
                    b.DrawVLine(gx, y, 16, EgaColorIndex.DarkGray);
                    b.DrawVLine(gx + 12, y, 16, EgaColorIndex.DarkGray);
                    b.DrawHLine(gx, y, 13, EgaColorIndex.LightGray);

                    // X-brace diagonals
                    for (int dy = 0; dy < 16; dy++)
                    {
                        int dx = (dy * 12) / 16;
                        b.SetPixel(gx + dx, y + dy, EgaColorIndex.Black);
                        b.SetPixel(gx + 12 - dx, y + dy, EgaColorIndex.Black);
                    }

                    // Glowing status beacon
                    if (y % 48 == 0)
                    {
                        b.FillRect(gx + 5, y - 2, 3, 3, EgaColorIndex.BrightCyan);
                        b.SetPixel(gx + 6, y - 1, EgaColorIndex.BrightWhite);
                    }
                }
            }

            // High-tech conduit cables spanning horizontally across top (y: 6..10)
            b.DrawHLine(0, 6, 320, EgaColorIndex.Black);
            b.DrawHLine(0, 7, 320, EgaColorIndex.DarkGray);
            b.DrawHLine(0, 8, 320, EgaColorIndex.Cyan);
            b.DrawHLine(0, 9, 320, EgaColorIndex.DarkGray);
            b.DrawHLine(0, 10, 320, EgaColorIndex.Black);
        });

    // =========================================================================
    // 2. Standard 16x16 World Tiles
    // =========================================================================

    private static EgaSprite BakeStationHullPlate() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Heavy Space Station Titanium Hull Armor Plating (16x16)
            b.FillRect(0, 0, 16, 16, EgaColorIndex.LightGray);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);

            // Beveled light edges (top & left) and shadow edges (bottom & right)
            b.DrawHLine(1, 1, 14, EgaColorIndex.BrightWhite);
            b.DrawVLine(1, 1, 14, EgaColorIndex.BrightWhite);
            b.DrawHLine(1, 14, 14, EgaColorIndex.DarkGray);
            b.DrawVLine(14, 1, 14, EgaColorIndex.DarkGray);

            // Recessed center plate with cyan circuit inlay
            b.FillRect(3, 3, 10, 10, EgaColorIndex.Cyan);
            b.DrawRect(3, 3, 10, 10, EgaColorIndex.DarkGray);
            b.FillRect(5, 5, 6, 6, EgaColorIndex.Blue);
            b.SetPixel(7, 7, EgaColorIndex.BrightCyan);
            b.SetPixel(8, 8, EgaColorIndex.BrightWhite);

            // Heavy hex corner rivets
            b.FillRect(2, 2, 2, 2, EgaColorIndex.DarkGray);
            b.SetPixel(2, 2, EgaColorIndex.BrightWhite);
            b.FillRect(12, 2, 2, 2, EgaColorIndex.DarkGray);
            b.SetPixel(12, 2, EgaColorIndex.BrightWhite);
            b.FillRect(2, 12, 2, 2, EgaColorIndex.DarkGray);
            b.SetPixel(2, 12, EgaColorIndex.BrightWhite);
            b.FillRect(12, 12, 2, 2, EgaColorIndex.DarkGray);
            b.SetPixel(12, 12, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeHardLightGirder(int parity) =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Holographic Hard-Light Energy Girder / Forcefield Platform (16x16)
            // Left & right magnetic field emitter brackets
            b.FillRect(0, 0, 3, 7, EgaColorIndex.DarkGray);
            b.DrawRect(0, 0, 3, 7, EgaColorIndex.Black);
            b.SetPixel(1, 2, EgaColorIndex.BrightWhite);

            b.FillRect(13, 0, 3, 7, EgaColorIndex.DarkGray);
            b.DrawRect(13, 0, 3, 7, EgaColorIndex.Black);
            b.SetPixel(14, 2, EgaColorIndex.BrightWhite);

            // Top solid walk plane: brilliant glowing forcefield line (rows 1..4)
            EgaColorIndex fieldColor = (parity == 0) ? EgaColorIndex.BrightCyan : EgaColorIndex.BrightGreen;
            EgaColorIndex glowColor  = (parity == 0) ? EgaColorIndex.Cyan : EgaColorIndex.Green;

            b.DrawHLine(2, 1, 12, EgaColorIndex.BrightWhite);
            b.DrawHLine(2, 2, 12, fieldColor);
            b.DrawHLine(2, 3, 12, glowColor);
            b.DrawHLine(2, 4, 12, EgaColorIndex.Blue);

            // Vertical holographic laser scanlines projecting downwards (rows 5..12)
            int[] scanlineX = (parity == 0) ? new[] { 3, 7, 11 } : new[] { 4, 8, 12 };
            foreach (int sx in scanlineX)
            {
                b.DrawVLine(sx, 5, 5, fieldColor);
                b.SetPixel(sx, 10, glowColor);
                b.SetPixel(sx, 12, EgaColorIndex.BrightWhite); // Energy particle node
            }
        });

    private static EgaSprite BakePlasmaDefenseVents() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // High-voltage Plasma Defense Vent / Sizzling Thermal Hazard (16x16)
            // Bottom reinforced containment grate (rows 12..15)
            b.FillRect(0, 12, 16, 4, EgaColorIndex.DarkGray);
            b.DrawRect(0, 12, 16, 4, EgaColorIndex.Black);
            b.DrawHLine(1, 13, 14, EgaColorIndex.Yellow); // Warning yellow line

            // Two high-energy plasma venting nozzles at x: 4 and x: 12
            int[] nozzles = { 4, 12 };
            foreach (int nx in nozzles)
            {
                // Metal nozzle base
                b.FillRect(nx - 2, 10, 5, 3, EgaColorIndex.LightGray);
                b.DrawRect(nx - 2, 10, 5, 3, EgaColorIndex.Black);

                // Vertical lethal plasma discharge column (rows 0..9)
                b.DrawVLine(nx, 0, 10, EgaColorIndex.BrightWhite);      // Superheated core
                b.DrawVLine(nx - 1, 1, 9, EgaColorIndex.Yellow);
                b.DrawVLine(nx + 1, 1, 9, EgaColorIndex.Yellow);
                b.DrawVLine(nx - 2, 3, 7, EgaColorIndex.BrightRed);
                b.DrawVLine(nx + 2, 3, 7, EgaColorIndex.BrightRed);

                // Radiating plasma arc sparks
                b.SetPixel(nx - 3, 2, EgaColorIndex.BrightMagenta);
                b.SetPixel(nx + 3, 1, EgaColorIndex.BrightMagenta);
                b.SetPixel(nx, 0, EgaColorIndex.BrightWhite);
            }
        });

    private static BakeSpriteDel BakeReinforcedBulkhead => () =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Reinforced Space Station Bulkhead Blast Border (16x16)
            b.FillRect(0, 0, 16, 16, EgaColorIndex.DarkGray);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);

            // Diagonal Hazard Caution Stripes (Yellow & Black)
            for (int y = 2; y < 14; y++)
            {
                for (int x = 2; x < 14; x++)
                {
                    if ((x + y) % 6 < 3)
                    {
                        b.SetPixel(x, y, EgaColorIndex.Yellow);
                    }
                    else
                    {
                        b.SetPixel(x, y, EgaColorIndex.Black);
                    }
                }
            }

            // Beveled steel border frame
            b.DrawRect(1, 1, 14, 14, EgaColorIndex.LightGray);
            b.SetPixel(2, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 2, EgaColorIndex.BrightWhite);
            b.SetPixel(2, 13, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 13, EgaColorIndex.BrightWhite);
        });

    private delegate EgaSprite BakeSpriteDel();

    private static EgaSprite BakeTractorBeamConduit() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Vertical Anti-Gravity Tractor Beam / Turbolift Energy Column (16x16)
            // Left & right magnetic containment guide rails
            b.FillRect(1, 0, 3, 16, EgaColorIndex.DarkGray);
            b.DrawRect(1, 0, 3, 16, EgaColorIndex.Black);
            b.DrawVLine(2, 0, 16, EgaColorIndex.BrightCyan);

            b.FillRect(12, 0, 3, 16, EgaColorIndex.DarkGray);
            b.DrawRect(12, 0, 3, 16, EgaColorIndex.Black);
            b.DrawVLine(13, 0, 16, EgaColorIndex.BrightCyan);

            // Pulsing horizontal anti-gravity beam rings (rows 1, 5, 9, 13)
            for (int ry = 1; ry < 16; ry += 4)
            {
                b.DrawHLine(4, ry, 8, EgaColorIndex.BrightCyan);
                b.DrawHLine(5, ry, 6, EgaColorIndex.BrightWhite);
                b.SetPixel(7, ry + 1, EgaColorIndex.Cyan);
                b.SetPixel(8, ry + 1, EgaColorIndex.Cyan);
            }
        });

    private static EgaSprite BakeAntimatterCargoPod() =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Antimatter Fuel Cell / Space Cargo Container (16x16)
            b.FillRect(0, 0, 16, 16, EgaColorIndex.LightGray);
            b.DrawRect(0, 0, 16, 16, EgaColorIndex.Black);

            // Internal dark blue isolation cell
            b.FillRect(2, 2, 12, 12, EgaColorIndex.Blue);
            b.DrawRect(2, 2, 12, 12, EgaColorIndex.Black);

            // Glowing nuclear / radioactive trefoil hazard emblem in center
            b.FillRect(6, 6, 4, 4, EgaColorIndex.Yellow);
            b.DrawRect(6, 6, 4, 4, EgaColorIndex.Black);
            b.SetPixel(7, 7, EgaColorIndex.BrightWhite);
            b.SetPixel(8, 8, EgaColorIndex.BrightWhite);

            // Upper hazard indicator lights
            b.SetPixel(4, 4, EgaColorIndex.BrightRed);
            b.SetPixel(11, 4, EgaColorIndex.BrightGreen);
        });

    // =========================================================================
    // 3. Player: Starfighter Spacecraft (24x24 with OffsetX = -5; Climb 14x24)
    // =========================================================================

    private static EgaSprite BakeStarfighterAction(int thrustLevel, int pitchAngle) =>
        EgaSprite.Bake(24, 24, b =>
        {
            const int ox = 5;
            int py = 10 + pitchAngle;

            // 1. Sleek Starfighter Delta-Wing Hull (ox..ox+18, py-7..py+7)
            // Needle nosecone facing right (x: ox+15..ox+18)
            b.FillRect(ox + 15, py, 4, 2, EgaColorIndex.BrightWhite);
            b.DrawRect(ox + 15, py, 4, 2, EgaColorIndex.Black);
            b.SetPixel(ox + 18, py, EgaColorIndex.Yellow); // Laser sensor tip

            // Forward Cockpit Canopy (Glass reflection in BrightCyan & BrightWhite)
            b.FillRect(ox + 9, py - 2, 6, 4, EgaColorIndex.BrightCyan);
            b.DrawRect(ox + 9, py - 2, 6, 4, EgaColorIndex.Black);
            b.DrawHLine(ox + 10, py - 1, 4, EgaColorIndex.BrightWhite); // Glare line
            b.SetPixel(ox + 11, py, EgaColorIndex.Blue);                // Pilot helmet silhouette

            // Main Fuselage (Titanium White & Light Gray)
            b.FillRect(ox + 4, py - 4, 7, 8, EgaColorIndex.LightGray);
            b.DrawRect(ox + 4, py - 4, 7, 8, EgaColorIndex.Black);
            b.DrawHLine(ox + 5, py - 3, 5, EgaColorIndex.BrightWhite);

            // Red Squadron Racing Stripe across fuselage
            b.DrawHLine(ox + 4, py, 6, EgaColorIndex.BrightRed);

            // Upper & Lower Swept Delta Wings (x: ox..ox+8)
            // Upper Wing
            b.FillRect(ox + 1, py - 8, 6, 4, EgaColorIndex.Blue);
            b.DrawRect(ox + 1, py - 8, 6, 4, EgaColorIndex.Black);
            b.DrawHLine(ox + 2, py - 8, 5, EgaColorIndex.BrightBlue);
            // Upper Wingtip Laser Cannon Barrel
            b.FillRect(ox + 7, py - 8, 4, 2, EgaColorIndex.DarkGray);
            b.SetPixel(ox + 10, py - 8, EgaColorIndex.BrightCyan); // Cannon muzzle

            // Lower Wing
            b.FillRect(ox + 1, py + 4, 6, 4, EgaColorIndex.Blue);
            b.DrawRect(ox + 1, py + 4, 6, 4, EgaColorIndex.Black);
            b.DrawHLine(ox + 2, py + 7, 5, EgaColorIndex.BrightBlue);
            // Lower Wingtip Laser Cannon Barrel
            b.FillRect(ox + 7, py + 6, 4, 2, EgaColorIndex.DarkGray);
            b.SetPixel(ox + 10, py + 6, EgaColorIndex.BrightCyan);

            // 2. Dual Rear Ion Thruster Nozzles (x: ox..ox+3, y: py-3..py+3)
            b.FillRect(ox, py - 3, 4, 6, EgaColorIndex.DarkGray);
            b.DrawRect(ox, py - 3, 4, 6, EgaColorIndex.Black);

            // 3. Engine Exhaust Plumes & Plasma Thrust
            if (thrustLevel == 0)
            {
                // Idling Ion Glow (Gentle cyan/blue shimmer)
                b.DrawVLine(ox - 1, py - 2, 4, EgaColorIndex.BrightCyan);
                b.SetPixel(ox - 2, py, EgaColorIndex.BrightWhite);
            }
            else
            {
                // Active Flight Thrust (Roaring plasma rocket flame)
                int flameLen = thrustLevel switch
                {
                    1 => 4,
                    2 => 7,
                    3 => 10,
                    _ => 6
                };

                // Core white-hot flame
                b.FillRect(ox - flameLen / 2, py - 1, flameLen / 2, 2, EgaColorIndex.BrightWhite);
                // Yellow mid flame
                b.FillRect(ox - flameLen, py - 2, flameLen, 4, EgaColorIndex.Yellow);
                // Red outer shock plume
                b.DrawHLine(ox - flameLen - 2, py, 2, EgaColorIndex.BrightRed);
                b.SetPixel(ox - flameLen - 1, py - 1, EgaColorIndex.BrightRed);
                b.SetPixel(ox - flameLen - 1, py + 1, EgaColorIndex.BrightRed);
                // Exhaust smoke / ion sparkle
                b.SetPixel(ox - flameLen - 3, py, EgaColorIndex.BrightCyan);
            }
        }, offsetX: -5, offsetY: 0);

    private static EgaSprite BakeStarfighterVertical(int subFrame) =>
        EgaSprite.Bake(14, 24, b =>
        {
            // Starfighter in Vertical Tractor Beam Ascent Mode (14x24)
            // Nosecone facing up (x: 5..8, y: 1..4)
            b.FillRect(5, 1, 4, 3, EgaColorIndex.BrightWhite);
            b.DrawRect(5, 1, 4, 3, EgaColorIndex.Black);
            b.SetPixel(6, 0, EgaColorIndex.Yellow);
            b.SetPixel(7, 0, EgaColorIndex.Yellow);

            // Cockpit Canopy (x: 5..8, y: 4..9)
            b.FillRect(5, 4, 4, 5, EgaColorIndex.BrightCyan);
            b.DrawRect(5, 4, 4, 5, EgaColorIndex.Black);
            b.DrawVLine(6, 5, 3, EgaColorIndex.BrightWhite);

            // Main Fuselage (x: 3..10, y: 9..17)
            b.FillRect(3, 9, 8, 8, EgaColorIndex.LightGray);
            b.DrawRect(3, 9, 8, 8, EgaColorIndex.Black);
            b.FillRect(5, 11, 4, 3, EgaColorIndex.BrightRed); // Central insignia

            // Left & Right Swept Wings (x: 0..2 and 11..13)
            b.FillRect(0, 11, 3, 6, EgaColorIndex.Blue);
            b.DrawRect(0, 11, 3, 6, EgaColorIndex.Black);
            b.FillRect(11, 11, 3, 6, EgaColorIndex.Blue);
            b.DrawRect(11, 11, 3, 6, EgaColorIndex.Black);

            // Rear Engines & Downward Thrust Plume (x: 4..9, y: 17..23)
            b.FillRect(4, 17, 6, 2, EgaColorIndex.DarkGray);
            b.DrawRect(4, 17, 6, 2, EgaColorIndex.Black);

            int flameY = 19;
            EgaColorIndex flameCol = (subFrame == 0) ? EgaColorIndex.Yellow : EgaColorIndex.BrightWhite;
            b.FillRect(5, flameY, 4, 3, flameCol);
            b.DrawVLine(6, flameY + 2, 2, EgaColorIndex.BrightRed);
            b.DrawVLine(7, flameY + 2, 2, EgaColorIndex.BrightRed);
        });

    // =========================================================================
    // 4. Monsters: Alien Invader Vessels (100% Spacecraft, Zero Walking Legs)
    // =========================================================================

    private static EgaSprite BakeAlienScoutSaucer(int frame) =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Monster 1: Classic Space Invaders Crab / Alien Scout Saucer (16x16)
            // Retro arcade silhouette with glowing alien eyes and plasma pincers
            int eyeY = (frame == 0) ? 5 : 6;

            // Dome head & chassis
            b.FillRect(4, 2, 8, 8, EgaColorIndex.BrightGreen);
            b.DrawRect(4, 2, 8, 8, EgaColorIndex.Black);

            // Antennae / sensor prongs (pointing up or flared out)
            if (frame == 0)
            {
                b.DrawVLine(3, 0, 3, EgaColorIndex.Green);
                b.SetPixel(3, 0, EgaColorIndex.Yellow);
                b.DrawVLine(12, 0, 3, EgaColorIndex.Green);
                b.SetPixel(12, 0, EgaColorIndex.Yellow);
            }
            else
            {
                b.SetPixel(2, 2, EgaColorIndex.Green);
                b.SetPixel(3, 1, EgaColorIndex.Green);
                b.SetPixel(1, 0, EgaColorIndex.BrightWhite);
                b.SetPixel(13, 2, EgaColorIndex.Green);
                b.SetPixel(12, 1, EgaColorIndex.Green);
                b.SetPixel(14, 0, EgaColorIndex.BrightWhite);
            }

            // Menacing yellow alien eyes
            b.SetPixel(6, eyeY, EgaColorIndex.Yellow);
            b.SetPixel(9, eyeY, EgaColorIndex.Yellow);

            // Glowing central power cell
            b.FillRect(6, 8, 4, 2, (frame == 0) ? EgaColorIndex.BrightWhite : EgaColorIndex.BrightCyan);

            // Plasma blasters / lower arms (hovering, no legs!)
            if (frame == 0)
            {
                b.FillRect(2, 10, 3, 3, EgaColorIndex.Green);
                b.DrawRect(2, 10, 3, 3, EgaColorIndex.Black);
                b.SetPixel(1, 12, EgaColorIndex.BrightRed);

                b.FillRect(11, 10, 3, 3, EgaColorIndex.Green);
                b.DrawRect(11, 10, 3, 3, EgaColorIndex.Black);
                b.SetPixel(14, 12, EgaColorIndex.BrightRed);
            }
            else
            {
                b.FillRect(1, 8, 3, 3, EgaColorIndex.Green);
                b.DrawRect(1, 8, 3, 3, EgaColorIndex.Black);
                b.SetPixel(0, 7, EgaColorIndex.Yellow);

                b.FillRect(12, 8, 3, 3, EgaColorIndex.Green);
                b.DrawRect(12, 8, 3, 3, EgaColorIndex.Black);
                b.SetPixel(15, 7, EgaColorIndex.Yellow);
            }

            // Ventral anti-grav hover aura
            b.DrawHLine(5, 13, 6, EgaColorIndex.BrightCyan);
            b.SetPixel(7, 14, EgaColorIndex.Cyan);
            b.SetPixel(8, 14, EgaColorIndex.Cyan);
        });

    private static EgaSprite BakeFlyingSaucerUfo(int frame) =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Monster 2: Flying Saucer UFO / Spy Drone (16x16, sinus hover)
            // Upper transparent glass cockpit dome
            b.FillRect(5, 2, 6, 4, EgaColorIndex.BrightCyan);
            b.DrawRect(5, 2, 6, 4, EgaColorIndex.Black);
            b.SetPixel(6, 3, EgaColorIndex.BrightWhite); // Visor reflection
            b.SetPixel(7, 4, EgaColorIndex.Black);       // Alien pilot shadow

            // Wide elliptical flying saucer disc hull (x: 1..14, y: 6..10)
            b.FillRect(2, 6, 12, 4, EgaColorIndex.Magenta);
            b.FillRect(1, 7, 14, 2, EgaColorIndex.BrightMagenta);
            b.DrawRect(1, 6, 14, 4, EgaColorIndex.Black);

            // Rotating perimeter strobe lights
            EgaColorIndex colA = (frame == 0) ? EgaColorIndex.Yellow : EgaColorIndex.BrightCyan;
            EgaColorIndex colB = (frame == 0) ? EgaColorIndex.BrightRed : EgaColorIndex.Yellow;
            EgaColorIndex colC = (frame == 0) ? EgaColorIndex.BrightCyan : EgaColorIndex.BrightRed;

            b.SetPixel(3, 7, colA);
            b.SetPixel(6, 7, colB);
            b.SetPixel(9, 7, colC);
            b.SetPixel(12, 7, colA);

            // Underside Tractor / Pulse Emitter
            b.FillRect(5, 10, 6, 2, EgaColorIndex.DarkGray);
            b.DrawRect(5, 10, 6, 2, EgaColorIndex.Black);
            b.DrawHLine(6, 12, 4, (frame == 0) ? EgaColorIndex.BrightCyan : EgaColorIndex.BrightWhite);
            b.SetPixel(7, 13, EgaColorIndex.Cyan);
            b.SetPixel(8, 13, EgaColorIndex.Cyan);
        });

    private static EgaSprite BakeOrbitalSatellite(int frame) =>
        EgaSprite.Bake(16, 16, b =>
        {
            // Monster 3: Ceiling Orbital Defense Satellite / Laser Turret (16x16)
            // Ceiling magnetic mounting bracket (y: 0..2)
            b.FillRect(4, 0, 8, 3, EgaColorIndex.DarkGray);
            b.DrawRect(4, 0, 8, 3, EgaColorIndex.Black);

            // Central rotating spherical sensor chassis (x: 5..10, y: 3..8)
            b.FillRect(5, 3, 6, 6, EgaColorIndex.LightGray);
            b.DrawRect(5, 3, 6, 6, EgaColorIndex.Black);
            b.SetPixel(6, 4, EgaColorIndex.BrightWhite);

            // Left & Right Solar Array Wing Panels (y: 4..8)
            b.FillRect(0, 4, 4, 4, EgaColorIndex.Blue);
            b.DrawRect(0, 4, 4, 4, EgaColorIndex.Black);
            b.DrawHLine(1, 5, 2, EgaColorIndex.BrightCyan);

            b.FillRect(12, 4, 4, 4, EgaColorIndex.Blue);
            b.DrawRect(12, 4, 4, 4, EgaColorIndex.Black);
            b.DrawHLine(13, 5, 2, EgaColorIndex.BrightCyan);

            // Downward Heavy Laser Emitter Barrel (x: 6..9, y: 9..14)
            b.FillRect(6, 9, 4, 5, EgaColorIndex.DarkGray);
            b.DrawRect(6, 9, 4, 5, EgaColorIndex.Black);

            // Heavy Laser Targeting Lens & Muzzle Charge
            EgaColorIndex muzzleCol = (frame == 0) ? EgaColorIndex.BrightRed : EgaColorIndex.Yellow;
            b.FillRect(6, 14, 4, 2, muzzleCol);
            b.DrawHLine(7, 14, 2, EgaColorIndex.BrightWhite);
        });

    private static EgaSprite BakeFastAttackInterceptor(int frame) =>
        EgaSprite.Bake(24, 16, b =>
        {
            // Monster 4: Fast Attack Interceptor / Alien Viper Fighter (24x16)
            // Aggressive, razor-sharp swept-forward wings and dual plasma cannons
            // Forward needle nosecone (x: 18..23, y: 7..9)
            b.FillRect(18, 7, 5, 2, EgaColorIndex.BrightRed);
            b.DrawRect(18, 7, 5, 2, EgaColorIndex.Black);
            b.SetPixel(22, 7, EgaColorIndex.Yellow); // High-energy tip

            // Cockpit visor (x: 12..17, y: 6..8)
            b.FillRect(12, 6, 6, 3, EgaColorIndex.Yellow);
            b.DrawRect(12, 6, 6, 3, EgaColorIndex.Black);
            b.SetPixel(13, 6, EgaColorIndex.BrightWhite);

            // Armored Fuselage (x: 6..12, y: 4..11)
            b.FillRect(6, 4, 7, 8, EgaColorIndex.DarkGray);
            b.DrawRect(6, 4, 7, 8, EgaColorIndex.Black);
            b.DrawHLine(7, 5, 5, EgaColorIndex.LightGray);

            // Swept Forward Wings (Upper and Lower)
            // Upper Wing
            b.FillRect(2, 1, 10, 3, EgaColorIndex.Red);
            b.DrawRect(2, 1, 10, 3, EgaColorIndex.Black);
            b.SetPixel(11, 0, EgaColorIndex.Yellow); // Upper wingtip blaster

            // Lower Wing
            b.FillRect(2, 12, 10, 3, EgaColorIndex.Red);
            b.DrawRect(2, 12, 10, 3, EgaColorIndex.Black);
            b.SetPixel(11, 14, EgaColorIndex.Yellow); // Lower wingtip blaster

            // Twin Jet Afterburner Exhaust Plumes (x: 0..5, y: 5..10)
            b.FillRect(4, 5, 3, 6, EgaColorIndex.Black);
            int flameLen = (frame == 0) ? 4 : 6;
            b.FillRect(4 - flameLen, 6, flameLen, 4, (frame == 0) ? EgaColorIndex.Yellow : EgaColorIndex.BrightWhite);
            b.DrawHLine(4 - flameLen - 1, 7, 2, EgaColorIndex.BrightRed);
            b.DrawHLine(4 - flameLen - 1, 8, 2, EgaColorIndex.BrightRed);
        });

    private static EgaSprite BakeAlienMothership(int frame) =>
        EgaSprite.Bake(24, 26, b =>
        {
            // Monster 5: Heavy Alien Mothership / Dreadnought Flagship (24x26)
            // Massive armored command cruiser with bridge, plasma turrets & antimatter reactor
            // Forward Heavy Armored Prow (x: 16..22, y: 6..19)
            b.FillRect(16, 6, 7, 14, EgaColorIndex.DarkGray);
            b.DrawRect(16, 6, 7, 14, EgaColorIndex.Black);
            b.DrawVLine(18, 7, 12, EgaColorIndex.LightGray); // Beveled armor edge

            // Command Bridge Viewport Slit (x: 17..20, y: 11..13)
            b.FillRect(17, 11, 4, 3, (frame == 0) ? EgaColorIndex.BrightCyan : EgaColorIndex.BrightWhite);
            b.DrawRect(17, 11, 4, 3, EgaColorIndex.Black);

            // Main Battlecruiser Citadel (x: 6..16, y: 3..22)
            b.FillRect(6, 3, 11, 20, EgaColorIndex.Blue);
            b.DrawRect(6, 3, 11, 20, EgaColorIndex.Black);
            b.DrawHLine(7, 4, 9, EgaColorIndex.BrightBlue);

            // Upper Plasma Cannon Turret (x: 10..15, y: 0..3)
            b.FillRect(10, 0, 5, 3, EgaColorIndex.DarkGray);
            b.DrawRect(10, 0, 5, 3, EgaColorIndex.Black);
            b.SetPixel(15, 1, EgaColorIndex.BrightRed);

            // Lower Plasma Cannon Turret (x: 10..15, y: 23..25)
            b.FillRect(10, 23, 5, 3, EgaColorIndex.DarkGray);
            b.DrawRect(10, 23, 5, 3, EgaColorIndex.Black);
            b.SetPixel(15, 24, EgaColorIndex.BrightRed);

            // Central Antimatter Reactor Core (x: 8..13, y: 9..16)
            EgaColorIndex coreCol = (frame == 0) ? EgaColorIndex.BrightMagenta : EgaColorIndex.BrightRed;
            b.FillRect(8, 9, 6, 8, coreCol);
            b.DrawRect(8, 9, 6, 8, EgaColorIndex.Black);
            b.FillRect(9, 11, 4, 4, EgaColorIndex.BrightWhite); // Blazing core center

            // Rear Heavy Ion Propulsion Thrusters (x: 0..6, y: 5..20)
            b.FillRect(2, 5, 5, 16, EgaColorIndex.DarkGray);
            b.DrawRect(2, 5, 5, 16, EgaColorIndex.Black);

            // Triple Ion Engine Exhaust Plumes (Upper, Middle, Lower)
            int[] thrusterY = { 7, 12, 17 };
            foreach (int ty in thrusterY)
            {
                b.FillRect(0, ty, 3, 3, (frame == 0) ? EgaColorIndex.BrightCyan : EgaColorIndex.BrightWhite);
                b.SetPixel(0, ty + 1, EgaColorIndex.Cyan);
            }
        });

    // =========================================================================
    // 5. Projectiles: Photon Laser Bolt & Alien Plasma Torpedo
    // =========================================================================

    private static EgaSprite BakePhotonLaserBolt() =>
        EgaSprite.Bake(8, 8, b =>
        {
            // High-energy Starfighter Photon Laser Pulse (8x8)
            // Horizontal laser bolt moving right (y: 3..4, x: 0..7)
            b.DrawHLine(0, 3, 8, EgaColorIndex.BrightCyan);
            b.DrawHLine(1, 4, 6, EgaColorIndex.BrightWhite);
            b.SetPixel(7, 3, EgaColorIndex.Yellow); // Leading tip
            b.SetPixel(6, 2, EgaColorIndex.BrightCyan);
            b.SetPixel(6, 5, EgaColorIndex.BrightCyan);
        });

    private static EgaSprite BakeAlienPlasmaTorpedo() =>
        EgaSprite.Bake(8, 8, b =>
        {
            // Menacing Alien Plasma Torpedo / Energy Bomb (8x8)
            b.FillRect(2, 1, 4, 6, EgaColorIndex.BrightRed);
            b.FillRect(1, 2, 6, 4, EgaColorIndex.BrightRed);
            b.DrawRect(1, 2, 6, 4, EgaColorIndex.Black);
            b.DrawRect(2, 1, 4, 6, EgaColorIndex.Black);
            b.FillRect(3, 3, 2, 2, EgaColorIndex.Yellow);
            b.SetPixel(3, 3, EgaColorIndex.BrightWhite);
            b.SetPixel(0, 3, EgaColorIndex.BrightMagenta);
            b.SetPixel(7, 3, EgaColorIndex.BrightMagenta);
        });

    // =========================================================================
    // 6. Pickups: Starlight Crystal, Plasma Ruby, Gold Data Cartridge & Nanite Pod
    // =========================================================================

    private static EgaSprite BakeStarlightCrystal(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Faceted Starlight Space Crystal (14x14)
            b.FillRect(3, 2, 8, 10, EgaColorIndex.BrightCyan);
            b.DrawRect(3, 2, 8, 10, EgaColorIndex.Black);
            b.DrawHLine(4, 3, 6, EgaColorIndex.BrightWhite);
            b.FillRect(5, 5, 4, 4, EgaColorIndex.Cyan);
            if (glint)
            {
                b.DrawSparkle(7, 5, EgaColorIndex.BrightWhite);
                b.SetPixel(7, 5, EgaColorIndex.Yellow);
            }
        });

    private static EgaSprite BakePlasmaRuby(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Hyper-Matter Plasma Ruby (14x14)
            b.FillRect(3, 2, 8, 10, EgaColorIndex.BrightRed);
            b.DrawRect(3, 2, 8, 10, EgaColorIndex.Black);
            b.DrawHLine(4, 3, 6, EgaColorIndex.BrightMagenta);
            b.FillRect(5, 5, 4, 4, EgaColorIndex.Red);
            if (glint)
            {
                b.DrawSparkle(7, 5, EgaColorIndex.BrightWhite);
                b.SetPixel(7, 5, EgaColorIndex.Yellow);
            }
        });

    private static EgaSprite BakeDataCartridge(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Golden Star Map Data Cartridge (14x14)
            b.FillRect(2, 2, 10, 10, EgaColorIndex.Yellow);
            b.DrawRect(2, 2, 10, 10, EgaColorIndex.Black);
            b.DrawHLine(3, 3, 8, EgaColorIndex.BrightWhite);
            b.FillRect(4, 6, 6, 4, EgaColorIndex.Brown);
            b.SetPixel(9, 4, glint ? EgaColorIndex.BrightGreen : EgaColorIndex.Green); // Blinking LED
        });

    private static EgaSprite BakeNaniteRepairPod(bool glint) =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Shield Battery / Nanite Repair Pod (14x14)
            b.FillRect(2, 2, 10, 10, EgaColorIndex.LightGray);
            b.DrawRect(2, 2, 10, 10, EgaColorIndex.Black);
            b.FillRect(4, 4, 6, 6, EgaColorIndex.BrightGreen);
            // White medical cross
            b.DrawHLine(5, 6, 4, EgaColorIndex.BrightWhite);
            b.DrawVLine(6, 5, 4, EgaColorIndex.BrightWhite);
            if (glint)
            {
                b.SetPixel(3, 3, EgaColorIndex.Yellow);
            }
        });

    // =========================================================================
    // 7. Powerup HUD Badges (14x14 EGA Pixel Art)
    // =========================================================================

    public static EgaSprite BakeSpaceRapidBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Dual Rapid Photon Blasters (14x14)
            b.FillRect(1, 4, 4, 6, EgaColorIndex.DarkGray);
            b.FillRect(5, 3, 5, 8, EgaColorIndex.BrightCyan);
            b.DrawRect(5, 3, 5, 8, EgaColorIndex.Black);

            // Twin blaster barrels
            b.FillRect(10, 4, 3, 2, EgaColorIndex.LightGray);
            b.FillRect(10, 8, 3, 2, EgaColorIndex.LightGray);
            b.SetPixel(13, 4, EgaColorIndex.Yellow);
            b.SetPixel(13, 5, EgaColorIndex.BrightWhite);
            b.SetPixel(13, 8, EgaColorIndex.Yellow);
            b.SetPixel(13, 9, EgaColorIndex.BrightWhite);

            b.SetPixel(2, 2, EgaColorIndex.BrightCyan);
        });

    public static EgaSprite BakeSpaceJumpBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Ion Booster Thruster Bell (14x14)
            // Rocket nozzle
            b.FillRect(3, 2, 8, 4, EgaColorIndex.LightGray);
            b.DrawRect(3, 2, 8, 4, EgaColorIndex.Black);
            b.FillRect(2, 6, 10, 3, EgaColorIndex.DarkGray);
            b.DrawRect(2, 6, 10, 3, EgaColorIndex.Black);

            // Vertical plasma thrust plume
            b.FillRect(4, 9, 6, 3, EgaColorIndex.Yellow);
            b.FillRect(5, 9, 4, 2, EgaColorIndex.BrightWhite);
            b.DrawVLine(6, 12, 2, EgaColorIndex.BrightCyan);
            b.DrawVLine(7, 12, 2, EgaColorIndex.BrightCyan);
        });

    public static EgaSprite BakeSpaceSpeedBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Warp Drive / Hyper-Speed Afterburner (14x14)
            b.FillRect(2, 4, 9, 6, EgaColorIndex.BrightGreen);
            b.DrawRect(2, 4, 9, 6, EgaColorIndex.Black);
            b.DrawHLine(3, 5, 7, EgaColorIndex.BrightWhite);

            // Forward warp emitter dome
            b.FillRect(11, 5, 2, 4, EgaColorIndex.BrightCyan);
            b.SetPixel(13, 6, EgaColorIndex.BrightWhite);

            // Rear tachyon speed streaks
            b.DrawHLine(0, 3, 3, EgaColorIndex.Yellow);
            b.DrawHLine(0, 7, 2, EgaColorIndex.BrightWhite);
            b.DrawHLine(0, 11, 3, EgaColorIndex.Yellow);
        });

    public static EgaSprite BakeSpaceInvulnBadge() =>
        EgaSprite.Bake(14, 14, b =>
        {
            // Hexagonal Deflector Shield Generator (14x14)
            b.FillRect(3, 1, 8, 12, EgaColorIndex.Blue);
            b.FillRect(2, 3, 10, 8, EgaColorIndex.BrightBlue);
            b.DrawRect(2, 3, 10, 8, EgaColorIndex.Black);

            // Glowing shield energy core
            b.FillRect(4, 4, 6, 6, EgaColorIndex.BrightRed);
            b.FillRect(5, 5, 4, 4, EgaColorIndex.BrightMagenta);

            // Radiant central starburst
            b.DrawSparkle(7, 7, EgaColorIndex.BrightWhite);
            b.SetPixel(7, 7, EgaColorIndex.Yellow);
        });
}
