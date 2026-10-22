using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Themes;

public interface IGameTheme
{
    string Name { get; }
    string BgmTrack => "audio/bgm_duke.wav";
    ThemeAssetPack Assets { get; }
    bool ZeroGravity => false;
    void DrawBackground(FrameBuffer fb, Camera2D camera);
    void DrawTile(FrameBuffer fb, TileType type, int screenX, int screenY, int col, int row);
    void DrawPlayer(FrameBuffer fb, PlayerEntity player, int screenX, int screenY);
    void DrawProjectile(FrameBuffer fb, in Projectile projectile, int screenX, int screenY);
    void DrawMonster(FrameBuffer fb, MonsterEntity monster, int screenX, int screenY);
    void DrawPickup(FrameBuffer fb, in PickupItem pickup, int screenX, int screenY);
    void DrawHud(FrameBuffer fb, PlayerEntity player, string seed, int fps);
}

/// <summary>
/// Base class for all static asset-driven themes.
/// Concrete themes only supply a static <see cref="ThemeAssetPack"/>; all rendering logic,
/// parallax scrolling, animation frames, and HUD layout are executed by <see cref="GameRenderer"/>.
/// </summary>
public abstract class SpriteThemeBase : IGameTheme
{
    public abstract ThemeAssetPack Assets { get; }
    public string Name => Assets.Name;
    public virtual string BgmTrack => Assets.Name.Contains("Underwater", System.StringComparison.OrdinalIgnoreCase) ? "audio/bgm_underwater.wav" :
                                      Assets.Name.Contains("Forest", System.StringComparison.OrdinalIgnoreCase) ? "audio/bgm_forest.wav" :
                                      "audio/bgm_duke.wav";
    public virtual bool ZeroGravity => Assets.ZeroGravity;

    public void DrawBackground(FrameBuffer fb, Camera2D camera) =>
        GameRenderer.DrawBackground(fb, Assets, camera);

    public void DrawTile(FrameBuffer fb, TileType type, int screenX, int screenY, int col, int row) =>
        GameRenderer.DrawTile(fb, Assets, type, screenX, screenY, col, row);

    public void DrawPlayer(FrameBuffer fb, PlayerEntity player, int screenX, int screenY) =>
        GameRenderer.DrawPlayer(fb, Assets, player, screenX, screenY);

    public void DrawProjectile(FrameBuffer fb, in Projectile projectile, int screenX, int screenY) =>
        GameRenderer.DrawProjectile(fb, Assets, in projectile, screenX, screenY);

    public void DrawMonster(FrameBuffer fb, MonsterEntity monster, int screenX, int screenY) =>
        GameRenderer.DrawMonster(fb, Assets, monster, screenX, screenY);

    public void DrawPickup(FrameBuffer fb, in PickupItem pickup, int screenX, int screenY) =>
        GameRenderer.DrawPickup(fb, Assets, in pickup, screenX, screenY);

    public void DrawHud(FrameBuffer fb, PlayerEntity player, string seed, int fps) =>
        GameRenderer.DrawHud(fb, Assets, player, seed, fps);
}
