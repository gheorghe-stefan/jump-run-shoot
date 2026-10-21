using JumpRunShoot.Engine.Core;

namespace JumpRunShoot.Engine.Themes;

/// <summary>
/// Pure static visual asset pack for a theme.
/// Contains strictly standard-sized <see cref="EgaSprite"/> bitmaps and no custom rendering parameters.
/// All parallax scrolling, animation timing, hit-flashes, item glow/bobbing, and HUD rendering
/// work identically across all themes via <see cref="GameRenderer"/>.
/// </summary>
public sealed class ThemeAssetPack
{
    public required string Name { get; init; }
    public bool ZeroGravity { get; init; } = false;

    // 1. Standard Horizontally-Repeating Parallax Background Layers (Height: 168px, Y: 16..184)
    public required EgaSprite BackgroundFar { get; init; }         // 0.1x parallax (opaque base backdrop)
    public required EgaSprite BackgroundMid { get; init; }         // 0.2x parallax (midground structures/trees)
    public required EgaSprite BackgroundNear { get; init; }        // 0.5x parallax (near wall/foliage details)

    // 2. Standard 16x16 Tiles
    public required EgaSprite SolidTile { get; init; }             // 16x16
    public required EgaSprite[] OneWayTileVariants { get; init; }  // 16x16 (even/odd col alignment)
    public required EgaSprite HazardTile { get; init; }            // 16x16
    public required EgaSprite BorderTile { get; init; }            // 16x16
    public required EgaSprite LadderTile { get; init; }            // 16x16
    public required EgaSprite CrateTile { get; init; }             // 16x16

    // 3. Player Sprites (Standard 24x24 with OffsetX = -5 so 14px body is centered and weapon fits)
    public required EgaSprite PlayerIdle { get; init; }            // 24x24
    public required EgaSprite[] PlayerWalk { get; init; }          // 4 frames of 24x24
    public required EgaSprite PlayerJump { get; init; }            // 24x24
    public required EgaSprite[] PlayerClimb { get; init; }         // 2 frames of 14x24

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
}
