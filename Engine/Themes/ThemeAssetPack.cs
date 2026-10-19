using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Pure static visual asset pack for a theme.
/// Contains only standard-sized <see cref="EgaSprite"/> bitmaps and palette indices.
/// All parallax movement, animation timing, hit-flashes, item glow/bobbing, and HUD layout
/// are handled centrally by <see cref="GameRenderer"/>.
/// </summary>
public sealed class ThemeAssetPack
{
    public required string Name { get; init; }

    // 1. Background Layer Colors & Static Sprites
    public EgaColorIndex SkyColor { get; init; } = EgaColorIndex.Black;
    public EgaColorIndex WallFillColor { get; init; } = EgaColorIndex.DarkGray;
    public EgaColorIndex WallTopBorderColor { get; init; } = EgaColorIndex.Black;
    public EgaColorIndex WallHighlightColor { get; init; } = EgaColorIndex.LightGray;
    public EgaColorIndex StarCrossColor { get; init; } = EgaColorIndex.BrightCyan;
    public required (int X, int Y, EgaColorIndex Color)[] Stars { get; init; }
    public required EgaSprite BackgroundPillar { get; init; }      // 20x64
    public required EgaSprite BackgroundMonitor { get; init; }     // 20x16
    public required EgaSprite BackgroundServerRack { get; init; }  // 28x14
    public required EgaSprite BackgroundWallCrack { get; init; }   // 6x8

    // 2. Standard 16x16 Tiles
    public required EgaSprite SolidTile { get; init; }             // 16x16
    public required EgaSprite[] OneWayTileVariants { get; init; }  // 16x16 (even/odd col alignment)
    public required EgaSprite HazardTile { get; init; }            // 16x16
    public required EgaSprite BorderTile { get; init; }            // 16x16
    public required EgaSprite LadderTile { get; init; }            // 16x16
    public required EgaSprite CrateTile { get; init; }             // 16x16

    // 3. Player Sprites (Standard 24x24 with OffsetX = -5 so 14px body is centered and gun barrel fits)
    public required EgaSprite PlayerIdle { get; init; }            // 24x24
    public required EgaSprite[] PlayerWalk { get; init; }          // 4 frames of 24x24
    public required EgaSprite PlayerJump { get; init; }            // 24x24
    public required EgaSprite[] PlayerClimb { get; init; }         // 2 frames of 14x24
    public EgaColorIndex SpeedTrailColor { get; init; } = EgaColorIndex.BrightCyan;

    // 4. Monster Sprites (2 animation frames each; engine controls cadence, flip, and hit-flash)
    public required EgaSprite[] PatrolDroid { get; init; }         // 2 frames of 14x14
    public required EgaSprite[] HoverDrone { get; init; }          // 2 frames of 14x12
    public required EgaSprite[] CeilingTurret { get; init; }       // 2 frames of 14x12
    public required EgaSprite[] MechaHound { get; init; }          // 2 frames of 24x14
    public required EgaSprite[] HeavyGuard { get; init; }          // 2 frames of 22x26 (OffsetX = -4)

    // 5. Projectile Sprites
    public required EgaSprite PlayerProjectile { get; init; }      // 8x3
    public required EgaSprite MonsterProjectile { get; init; }     // 6x6

    // 6. Pickup Sprites (Standard 14x14; 2 frames for animated item glint/glow)
    public required EgaSprite[] BlueGem { get; init; }             // 2 frames of 14x14
    public required EgaSprite[] RedRuby { get; init; }             // 2 frames of 14x14
    public required EgaSprite[] GoldCartridge { get; init; }       // 2 frames of 14x14
    public required EgaSprite[] MedKit { get; init; }              // 2 frames of 14x14
    public required EgaSprite RapidFireBadge { get; init; }        // 14x14
    public required EgaSprite HighJumpBadge { get; init; }         // 14x14
    public required EgaSprite SpeedDashBadge { get; init; }        // 14x14

    // 7. HUD Palette
    public EgaColorIndex HudBgColor { get; init; } = EgaColorIndex.LightGray;
    public EgaColorIndex HudBorderColor { get; init; } = EgaColorIndex.Black;
    public EgaColorIndex HudBevelColor { get; init; } = EgaColorIndex.BrightWhite;
    public EgaColorIndex HudInsetBorderColor { get; init; } = EgaColorIndex.DarkGray;
    public EgaColorIndex ScoreLabelColor { get; init; } = EgaColorIndex.Yellow;
    public EgaColorIndex HealthFullColor { get; init; } = EgaColorIndex.BrightGreen;
    public EgaColorIndex HealthEmptyColor { get; init; } = EgaColorIndex.Red;
    public EgaColorIndex SeedTextColor { get; init; } = EgaColorIndex.BrightCyan;
}
