using System.IO;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class PolarAdventureThemeTests
{
    private static string FindRepoRoot()
    {
        string? dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "JumpRunShoot.csproj")) && Directory.Exists(Path.Combine(dir, "wwwroot", "themes")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return Directory.GetCurrentDirectory();
    }

    [Fact]
    public void ExportAndVerifyAllPolarAssets()
    {
        // 1. Verify procedural fallback still builds cleanly AND verify all 44 PNG files in wwwroot/themes/polar/
        var fallbackPack = PolarAdventureTheme.BakeProceduralAssets();
        Assert.NotNull(fallbackPack);
        Assert.False(fallbackPack.ZeroGravity);
        Assert.NotNull(fallbackPack.CeilingProjectile);

        string repoRoot = FindRepoRoot();
        string targetDir = Path.Combine(repoRoot, "wwwroot", "themes", "polar");

        // Ensure directory exists & load embedded/exported high-detail theme pack without overwriting PNGs
        var loadedPack = EgaPngCodec.LoadThemePack(PolarAdventureTheme.ThemeIdentifier, "Polar Adventure", zeroGravity: false);
        Assert.NotNull(loadedPack);
        Assert.NotNull(loadedPack.CeilingProjectile);

        // 2. Verify all 54 PNG files (including 4-frame monster animations + proj_ceiling.png) exist and are non-empty
        string[] expectedFiles =
        {
            "bg_far.png", "bg_mid.png", "bg_near.png",
            "tile_solid.png", "tile_oneway_0.png", "tile_oneway_1.png",
            "tile_hazard.png", "tile_border.png", "tile_ladder.png", "tile_crate.png",
            "player_idle.png", "player_walk_0.png", "player_walk_1.png", "player_walk_2.png", "player_walk_3.png",
            "player_jump.png", "player_climb_0.png", "player_climb_1.png",
            "monster_1_0.png", "monster_1_1.png", "monster_1_2.png", "monster_1_3.png",
            "monster_2_0.png", "monster_2_1.png", "monster_2_2.png", "monster_2_3.png",
            "monster_3_0.png", "monster_3_1.png", "monster_3_2.png", "monster_3_3.png",
            "monster_4_0.png", "monster_4_1.png", "monster_4_2.png", "monster_4_3.png",
            "monster_5_0.png", "monster_5_1.png", "monster_5_2.png", "monster_5_3.png",
            "proj_player.png", "proj_monster.png", "proj_ceiling.png",
            "pickup_gem_0.png", "pickup_gem_1.png",
            "pickup_ruby_0.png", "pickup_ruby_1.png",
            "pickup_gold_0.png", "pickup_gold_1.png",
            "pickup_medkit_0.png", "pickup_medkit_1.png",
            "badge_rapid.png", "badge_jump.png", "badge_speed.png", "badge_invuln.png", "badge_plasma.png"
        };

        Assert.Equal(54, expectedFiles.Length);
        foreach (var file in expectedFiles)
        {
            string filePath = Path.Combine(targetDir, file);
            Assert.True(File.Exists(filePath), $"Expected theme PNG '{file}' to exist at '{filePath}'.");
            var fileInfo = new FileInfo(filePath);
            Assert.True(fileInfo.Length > 0, $"File '{file}' should not be empty.");
        }
    }

    [Fact]
    public void PolarAdventureTheme_IsRegisteredInThemeRegistry_AndHasNormalGravity()
    {
        int index = -1;
        IGameTheme? polar = null;
        for (int i = 0; i < ThemeRegistry.AllThemes.Length; i++)
        {
            if (ThemeRegistry.AllThemes[i] is PolarAdventureTheme)
            {
                index = i;
                polar = ThemeRegistry.AllThemes[i];
                break;
            }
        }

        Assert.NotNull(polar);
        Assert.Equal("Polar Adventure", polar!.Name);
        Assert.False(polar.ZeroGravity);
        Assert.Equal("audio/bgm_polar.wav", polar.BgmTrack);

        var engine = new GameEngine();
        engine.SetTheme(index);
        Assert.Equal(index, engine.CurrentThemeIndex);
        Assert.False(engine.CurrentTheme.ZeroGravity);
        Assert.False(engine.Player.IsFlying);
    }

    [Fact]
    public void PolarAdventureTheme_CeilingSealDrawsSnowflake_WhileHorizontalMonsterDrawsSnowball()
    {
        var pack = PolarAdventureTheme.BakeProceduralAssets();
        Assert.NotNull(pack.CeilingProjectile);

        // Downward ceiling seal projectile should use CeilingProjectile (snowflake: BrightCyan/BrightWhite)
        var fbSeal = new FrameBuffer();
        fbSeal.Clear(EgaColorIndex.Black);
        var sealProj = new Projectile();
        sealProj.Spawn(new Vector2f(100, 100), new Vector2f(0, 160), 1, fromPlayer: false);
        GameRenderer.DrawProjectile(fbSeal, pack, sealProj, 100, 100);

        // Horizontal reindeer/polar bear projectile should use MonsterProjectile (snowball: 6x6 without BrightCyan tips at corners)
        var fbBear = new FrameBuffer();
        fbBear.Clear(EgaColorIndex.Black);
        var bearProj = new Projectile();
        bearProj.Spawn(new Vector2f(100, 100), new Vector2f(120, 0), 1, fromPlayer: false);
        GameRenderer.DrawProjectile(fbBear, pack, bearProj, 100, 100);

        // Confirm rendered pixel buffers differ (snowflake 7x7 vs snowball 6x6)
        bool anyDiff = false;
        for (int i = 0; i < fbSeal.Pixels.Length; i++)
        {
            if (fbSeal.Pixels[i] != fbBear.Pixels[i])
            {
                anyDiff = true;
                break;
            }
        }
        Assert.True(anyDiff, "Ceiling seal downward projectile (snowflake) should render differently from horizontal snowball projectile.");
    }

    [Fact]
    public void PolarAdventureTheme_RenderShowcasePreview()
    {
        var pack = new PolarAdventureTheme().Assets;

        // Build a 320x200 scene + sprite sheet preview using EgaSprite.Bake
        var showcase = EgaSprite.Bake(320, 200, b =>
        {
            // Sky & 3-layer Aurora / Iceberg / Igloo parallax backdrop across full width (168px tall at y=16..184)
            b.DrawSprite(pack.BackgroundFar, 0, 16);
            b.DrawSprite(pack.BackgroundMid, 0, 16);
            b.DrawSprite(pack.BackgroundNear, 0, 16);

            // Ground row of snow/ice solid tiles across y=184, plus Frost border left/right
            for (int x = 0; x < 320; x += 16)
            {
                b.DrawSprite(pack.SolidTile, x, 184);
            }
            b.DrawSprite(pack.BorderTile, 0, 168);
            b.DrawSprite(pack.BorderTile, 0, 152);
            b.DrawSprite(pack.BorderTile, 304, 168);

            // Platform with snow + icicles, Snow Crate, Snowy Ladder, Ice Spike Hazard
            for (int x = 16; x < 80; x += 16)
            {
                b.DrawSprite(pack.OneWayTileVariants[(x / 16) % 2], x, 136);
            }
            b.DrawSprite(pack.LadderTile, 48, 136);
            b.DrawSprite(pack.LadderTile, 48, 152);
            b.DrawSprite(pack.LadderTile, 48, 168);
            b.DrawSprite(pack.CrateTile, 80, 168);
            b.DrawSprite(pack.HazardTile, 112, 168);

            // Penguin + Player Snowball
            b.DrawSprite(pack.PlayerIdle, 24, 160);
            b.DrawSprite(pack.PlayerProjectile, 44, 168);

            // Monsters: Walrus, Christmas Reindeer, Ceiling Seal + Snowflake, Elk, Polar Bear + Snowball
            b.DrawSprite(pack.PatrolDroid[0], 136, 170);
            b.DrawSprite(pack.HoverDrone[0], 130, 112);
            b.DrawSprite(pack.CeilingTurret[0], 168, 96);
            b.DrawSprite(pack.CeilingProjectile!, 171, 114);
            b.DrawSprite(pack.MechaHound[0], 190, 170);
            b.DrawSprite(pack.HeavyGuard[0], 246, 158);
            b.DrawSprite(pack.MonsterProjectile, 232, 168);

            // Dark shelf along top strip (y=0..34) showing crisp silhouettes of Pickups & Powerups
            b.FillRect(0, 0, 320, 34, EgaColorIndex.Black);
            b.DrawHLine(0, 33, 320, EgaColorIndex.Cyan);

            b.DrawSprite(pack.BlueGem[0], 8, 10);       // Turquoise triangle
            b.DrawSprite(pack.RedRuby[0], 30, 10);      // Dark green triangle
            b.DrawSprite(pack.GoldCartridge[0], 52, 10);// Golden triangle
            b.DrawSprite(pack.MedKit[0], 82, 10);       // Fish health case
            b.DrawSprite(pack.RapidFireBadge, 112, 10); // Glowing golden snowball
            b.DrawSprite(pack.HighJumpBadge, 142, 10);  // Glowing black penguin wing
            b.DrawSprite(pack.SpeedDashBadge, 172, 10); // Sled
            b.DrawSprite(pack.InvulnerabilityBadge, 202, 10); // Golden fish

            // Also show Walk & Jump frames up top right for quick inspection
            b.DrawSprite(pack.PlayerWalk[0], 232, 6);
            b.DrawSprite(pack.PlayerJump, 260, 6);
            b.DrawSprite(pack.PlayerClimb[0], 288, 6);
        });

        string previewPath = Path.Combine(Path.GetTempPath(), "polar_showcase.png");
        EgaPngCodec.SavePng(showcase, previewPath);
        Assert.True(File.Exists(previewPath));
    }

    [Fact]
    public void PolarAdventureTheme_WalrusWalkFrames_HaveNoVerticalBodyTearSeam()
    {
        var theme = new PolarAdventureTheme();
        var frames = theme.Assets.PatrolDroid;
        Assert.Equal(4, frames.Length);

        for (int f = 0; f < frames.Length; f++)
        {
            var sprite = frames[f];
            for (int x = 1; x < sprite.Width - 1; x++)
            {
                int verticalGapCount = 0;
                for (int y = 0; y < sprite.Height; y++)
                {
                    byte cur = sprite.Pixels[y * sprite.Width + x];
                    byte left = sprite.Pixels[y * sprite.Width + (x - 1)];
                    byte right = sprite.Pixels[y * sprite.Width + (x + 1)];
                    if (cur == EgaSprite.Transparent && left != EgaSprite.Transparent && right != EgaSprite.Transparent)
                    {
                        verticalGapCount++;
                    }
                }
                Assert.True(verticalGapCount < 4,
                    $"Walrus frame {f} has a {verticalGapCount}px vertical transparent tear at column x={x}.");
            }
        }
    }

    [Fact]
    public void PolarAdventureTheme_FarSkyBackdrop_HasNoSharpBlackToBlueHorizontalCut()
    {
        var theme = new PolarAdventureTheme();
        var bg = theme.Assets.BackgroundFar;

        // Upper sky band (y=35..75, around the crescent moon and upper aurora) must not switch into solid Blue sky
        int blueSkyPixels = 0;
        for (int y = 35; y < 75; y++)
        {
            for (int x = 0; x < bg.Width; x++)
            {
                if (bg.Pixels[y * bg.Width + x] == (byte)EgaColorIndex.Blue)
                {
                    blueSkyPixels++;
                }
            }
        }

        Assert.True(blueSkyPixels < 200,
            $"Polar far sky backdrop still contains a large dark-blue band ({blueSkyPixels} px) creating a sharp black-blue sky cut.");
    }
}
