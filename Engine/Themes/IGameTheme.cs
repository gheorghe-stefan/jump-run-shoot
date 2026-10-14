using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Themes;

public interface IGameTheme
{
    string Name { get; }
    void DrawBackground(FrameBuffer fb, Camera2D camera);
    void DrawTile(FrameBuffer fb, TileType type, int screenX, int screenY, int col, int row);
    void DrawPlayer(FrameBuffer fb, PlayerEntity player, int screenX, int screenY);
    void DrawHud(FrameBuffer fb, PlayerEntity player, string seed, int fps);
}
