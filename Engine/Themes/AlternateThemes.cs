using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Duke Nukem 1 "Red Reactor" theme: Industrial nuclear core aesthetic with crimson steel girders,
/// yellow/black caution catwalks, molten core monitors, and high-visibility hazard suits.
/// Pure static asset pack; zero rendering logic.
/// </summary>
public sealed class RedReactorTheme : SpriteThemeBase
{
    public static readonly ThemeAssetPack StaticAssets = CreateAssets();

    public override ThemeAssetPack Assets => StaticAssets;

    private static ThemeAssetPack CreateAssets()
    {
        return new ThemeAssetPack
        {
            Name = "Red Reactor",

            SkyColor = EgaColorIndex.Black,
            WallFillColor = EgaColorIndex.Red,
            WallTopBorderColor = EgaColorIndex.Black,
            WallHighlightColor = EgaColorIndex.BrightRed,
            StarCrossColor = EgaColorIndex.Yellow,
            Stars = new (int, int, EgaColorIndex)[]
            {
                (20, 20, EgaColorIndex.Yellow), (25, 22, EgaColorIndex.BrightRed), (28, 19, EgaColorIndex.Brown),
                (65, 35, EgaColorIndex.BrightWhite), (68, 38, EgaColorIndex.Yellow), (72, 34, EgaColorIndex.BrightRed),
                (110, 24, EgaColorIndex.Yellow), (115, 25, EgaColorIndex.BrightRed), (118, 22, EgaColorIndex.BrightWhite),
                (160, 42, EgaColorIndex.BrightRed), (164, 40, EgaColorIndex.Yellow), (168, 44, EgaColorIndex.Brown),
                (210, 28, EgaColorIndex.BrightWhite), (214, 30, EgaColorIndex.Yellow), (218, 26, EgaColorIndex.BrightRed),
                (255, 36, EgaColorIndex.Yellow), (260, 38, EgaColorIndex.BrightRed), (265, 35, EgaColorIndex.BrightWhite),
                (295, 22, EgaColorIndex.Yellow), (300, 25, EgaColorIndex.BrightRed), (305, 20, EgaColorIndex.Brown),
                (45, 60, EgaColorIndex.BrightRed), (135, 55, EgaColorIndex.Yellow), (230, 62, EgaColorIndex.BrightWhite)
            },
            BackgroundPillar = DefaultTheme.BakePillar(EgaColorIndex.Brown, EgaColorIndex.BrightRed),
            BackgroundMonitor = DefaultTheme.BakeMonitor(EgaColorIndex.DarkGray, EgaColorIndex.Black, EgaColorIndex.BrightGreen),
            BackgroundServerRack = DefaultTheme.BakeServerRack(EgaColorIndex.LightGray, EgaColorIndex.Brown, EgaColorIndex.Yellow, EgaColorIndex.BrightRed),
            BackgroundWallCrack = DefaultTheme.BakeWallCrack(EgaColorIndex.Black),

            SolidTile = DefaultTheme.BakeSolidTile(EgaColorIndex.BrightRed, EgaColorIndex.Yellow, EgaColorIndex.Brown),
            OneWayTileVariants = new[]
            {
                DefaultTheme.BakeOneWayTile(0, EgaColorIndex.Yellow, EgaColorIndex.Brown, EgaColorIndex.Black),
                DefaultTheme.BakeOneWayTile(1, EgaColorIndex.Yellow, EgaColorIndex.Brown, EgaColorIndex.Black)
            },
            HazardTile = DefaultTheme.BakeHazardSpikes(EgaColorIndex.Yellow, EgaColorIndex.BrightWhite, EgaColorIndex.BrightGreen),
            BorderTile = DefaultTheme.BakeBorderBulkhead(EgaColorIndex.Brown, EgaColorIndex.BrightRed, EgaColorIndex.Red, EgaColorIndex.Yellow),
            LadderTile = DefaultTheme.BakeLadder(EgaColorIndex.Yellow, EgaColorIndex.Brown),
            CrateTile = DefaultTheme.BakeSupplyCrate(EgaColorIndex.DarkGray, EgaColorIndex.BrightRed, EgaColorIndex.Yellow),

            PlayerIdle = DefaultTheme.BakePlayerAction(0, 0, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan, EgaColorIndex.DarkGray),
            PlayerWalk = new[]
            {
                DefaultTheme.BakePlayerAction(2, 0, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan, EgaColorIndex.DarkGray),
                DefaultTheme.BakePlayerAction(0, 1, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan, EgaColorIndex.DarkGray),
                DefaultTheme.BakePlayerAction(-2, 0, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan, EgaColorIndex.DarkGray),
                DefaultTheme.BakePlayerAction(0, 1, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan, EgaColorIndex.DarkGray)
            },
            PlayerJump = DefaultTheme.BakePlayerAction(-1, -1, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan, EgaColorIndex.DarkGray),
            PlayerClimb = new[]
            {
                DefaultTheme.BakePlayerClimb(1, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan, EgaColorIndex.DarkGray),
                DefaultTheme.BakePlayerClimb(-1, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan, EgaColorIndex.DarkGray)
            },
            SpeedTrailColor = EgaColorIndex.Yellow,

            PatrolDroid = new[]
            {
                DefaultTheme.BakePatrolDroid(5, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightRed),
                DefaultTheme.BakePatrolDroid(7, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightRed)
            },
            HoverDrone = new[]
            {
                DefaultTheme.BakeHoverDrone(true, EgaColorIndex.BrightRed, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan),
                DefaultTheme.BakeHoverDrone(false, EgaColorIndex.BrightRed, EgaColorIndex.Yellow, EgaColorIndex.BrightCyan)
            },
            CeilingTurret = new[]
            {
                DefaultTheme.BakeCeilingTurret(true, EgaColorIndex.Yellow, EgaColorIndex.BrightRed),
                DefaultTheme.BakeCeilingTurret(false, EgaColorIndex.Yellow, EgaColorIndex.BrightRed)
            },
            MechaHound = new[]
            {
                DefaultTheme.BakeMechaHound(2, EgaColorIndex.Brown, EgaColorIndex.Yellow, EgaColorIndex.DarkGray),
                DefaultTheme.BakeMechaHound(-2, EgaColorIndex.Brown, EgaColorIndex.Yellow, EgaColorIndex.DarkGray)
            },
            HeavyGuard = new[]
            {
                DefaultTheme.BakeHeavyGuard(1, EgaColorIndex.Red, EgaColorIndex.Yellow, EgaColorIndex.BrightRed, EgaColorIndex.BrightGreen),
                DefaultTheme.BakeHeavyGuard(-1, EgaColorIndex.Red, EgaColorIndex.Yellow, EgaColorIndex.BrightRed, EgaColorIndex.BrightGreen)
            },

            PlayerProjectile = DefaultTheme.BakePlayerProjectile(EgaColorIndex.Yellow, EgaColorIndex.BrightWhite),
            MonsterProjectile = DefaultTheme.BakeMonsterProjectile(EgaColorIndex.BrightGreen, EgaColorIndex.Yellow),

            BlueGem = new[]
            {
                DefaultTheme.BakeGem(EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite, true),
                DefaultTheme.BakeGem(EgaColorIndex.BrightGreen, EgaColorIndex.Yellow, false)
            },
            RedRuby = new[]
            {
                DefaultTheme.BakeGem(EgaColorIndex.BrightMagenta, EgaColorIndex.BrightWhite, true),
                DefaultTheme.BakeGem(EgaColorIndex.BrightMagenta, EgaColorIndex.Yellow, false)
            },
            GoldCartridge = new[]
            {
                DefaultTheme.BakeCartridge(EgaColorIndex.Yellow, EgaColorIndex.BrightRed),
                DefaultTheme.BakeCartridge(EgaColorIndex.Yellow, EgaColorIndex.BrightWhite)
            },
            MedKit = DefaultTheme.StaticAssets.MedKit,
            RapidFireBadge = DefaultTheme.StaticAssets.RapidFireBadge,
            HighJumpBadge = DefaultTheme.StaticAssets.HighJumpBadge,
            SpeedDashBadge = DefaultTheme.StaticAssets.SpeedDashBadge,

            HudBgColor = EgaColorIndex.Brown,
            HudBorderColor = EgaColorIndex.Black,
            HudBevelColor = EgaColorIndex.Yellow,
            HudInsetBorderColor = EgaColorIndex.Red,
            ScoreLabelColor = EgaColorIndex.Yellow,
            HealthFullColor = EgaColorIndex.BrightGreen,
            HealthEmptyColor = EgaColorIndex.Red,
            SeedTextColor = EgaColorIndex.Yellow
        };
    }
}

/// <summary>
/// Duke Nukem 1 "Cyber Moonbase" theme: Deep space cyan/magenta synthesis with titanium bulkheads,
/// plasma-cyan bridges, and neon telemetry panels.
/// Pure static asset pack; zero rendering logic.
/// </summary>
public sealed class CyberMoonbaseTheme : SpriteThemeBase
{
    public static readonly ThemeAssetPack StaticAssets = CreateAssets();

    public override ThemeAssetPack Assets => StaticAssets;

    private static ThemeAssetPack CreateAssets()
    {
        return new ThemeAssetPack
        {
            Name = "Cyber Moonbase",

            SkyColor = EgaColorIndex.Black,
            WallFillColor = EgaColorIndex.Blue,
            WallTopBorderColor = EgaColorIndex.Black,
            WallHighlightColor = EgaColorIndex.BrightCyan,
            StarCrossColor = EgaColorIndex.BrightMagenta,
            Stars = DefaultTheme.StaticAssets.Stars,
            BackgroundPillar = DefaultTheme.BakePillar(EgaColorIndex.Blue, EgaColorIndex.BrightCyan),
            BackgroundMonitor = DefaultTheme.BakeMonitor(EgaColorIndex.Cyan, EgaColorIndex.Black, EgaColorIndex.BrightMagenta),
            BackgroundServerRack = DefaultTheme.BakeServerRack(EgaColorIndex.LightGray, EgaColorIndex.Blue, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightCyan),
            BackgroundWallCrack = DefaultTheme.BakeWallCrack(EgaColorIndex.Black),

            SolidTile = DefaultTheme.BakeSolidTile(EgaColorIndex.Cyan, EgaColorIndex.BrightWhite, EgaColorIndex.Blue),
            OneWayTileVariants = new[]
            {
                DefaultTheme.BakeOneWayTile(0, EgaColorIndex.BrightCyan, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightWhite),
                DefaultTheme.BakeOneWayTile(1, EgaColorIndex.BrightCyan, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightWhite)
            },
            HazardTile = DefaultTheme.BakeHazardSpikes(EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite, EgaColorIndex.BrightMagenta),
            BorderTile = DefaultTheme.BakeBorderBulkhead(EgaColorIndex.DarkGray, EgaColorIndex.BrightCyan, EgaColorIndex.Blue, EgaColorIndex.BrightMagenta),
            LadderTile = DefaultTheme.BakeLadder(EgaColorIndex.BrightCyan, EgaColorIndex.Blue),
            CrateTile = DefaultTheme.BakeSupplyCrate(EgaColorIndex.Magenta, EgaColorIndex.BrightCyan, EgaColorIndex.BrightWhite),

            PlayerIdle = DefaultTheme.BakePlayerAction(0, 0, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite),
            PlayerWalk = new[]
            {
                DefaultTheme.BakePlayerAction(2, 0, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite),
                DefaultTheme.BakePlayerAction(0, 1, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite),
                DefaultTheme.BakePlayerAction(-2, 0, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite),
                DefaultTheme.BakePlayerAction(0, 1, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite)
            },
            PlayerJump = DefaultTheme.BakePlayerAction(-1, -1, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite),
            PlayerClimb = new[]
            {
                DefaultTheme.BakePlayerClimb(1, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite),
                DefaultTheme.BakePlayerClimb(-1, EgaColorIndex.Yellow, EgaColorIndex.BrightGreen, EgaColorIndex.BrightWhite)
            },
            SpeedTrailColor = EgaColorIndex.BrightMagenta,

            PatrolDroid = new[]
            {
                DefaultTheme.BakePatrolDroid(5, EgaColorIndex.BrightWhite, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightCyan),
                DefaultTheme.BakePatrolDroid(7, EgaColorIndex.BrightWhite, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightCyan)
            },
            HoverDrone = new[]
            {
                DefaultTheme.BakeHoverDrone(true, EgaColorIndex.BrightCyan, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightWhite),
                DefaultTheme.BakeHoverDrone(false, EgaColorIndex.BrightCyan, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightWhite)
            },
            CeilingTurret = new[]
            {
                DefaultTheme.BakeCeilingTurret(true, EgaColorIndex.BrightWhite, EgaColorIndex.BrightMagenta),
                DefaultTheme.BakeCeilingTurret(false, EgaColorIndex.BrightWhite, EgaColorIndex.BrightMagenta)
            },
            MechaHound = new[]
            {
                DefaultTheme.BakeMechaHound(2, EgaColorIndex.Cyan, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightWhite),
                DefaultTheme.BakeMechaHound(-2, EgaColorIndex.Cyan, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightWhite)
            },
            HeavyGuard = new[]
            {
                DefaultTheme.BakeHeavyGuard(1, EgaColorIndex.Blue, EgaColorIndex.BrightWhite, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightCyan),
                DefaultTheme.BakeHeavyGuard(-1, EgaColorIndex.Blue, EgaColorIndex.BrightWhite, EgaColorIndex.BrightMagenta, EgaColorIndex.BrightCyan)
            },

            PlayerProjectile = DefaultTheme.BakePlayerProjectile(EgaColorIndex.BrightMagenta, EgaColorIndex.BrightWhite),
            MonsterProjectile = DefaultTheme.BakeMonsterProjectile(EgaColorIndex.BrightCyan, EgaColorIndex.BrightMagenta),

            BlueGem = DefaultTheme.StaticAssets.BlueGem,
            RedRuby = DefaultTheme.StaticAssets.RedRuby,
            GoldCartridge = DefaultTheme.StaticAssets.GoldCartridge,
            MedKit = DefaultTheme.StaticAssets.MedKit,
            RapidFireBadge = DefaultTheme.StaticAssets.RapidFireBadge,
            HighJumpBadge = DefaultTheme.StaticAssets.HighJumpBadge,
            SpeedDashBadge = DefaultTheme.StaticAssets.SpeedDashBadge,

            HudBgColor = EgaColorIndex.Blue,
            HudBorderColor = EgaColorIndex.Black,
            HudBevelColor = EgaColorIndex.BrightCyan,
            HudInsetBorderColor = EgaColorIndex.Cyan,
            ScoreLabelColor = EgaColorIndex.BrightCyan,
            HealthFullColor = EgaColorIndex.BrightGreen,
            HealthEmptyColor = EgaColorIndex.Magenta,
            SeedTextColor = EgaColorIndex.BrightMagenta
        };
    }
}

public static class ThemeRegistry
{
    public static readonly IGameTheme[] AllThemes =
    {
        new DefaultTheme(),
        new RedReactorTheme(),
        new CyberMoonbaseTheme()
    };
}
