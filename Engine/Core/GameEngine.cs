using System;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;

namespace JumpRunShoot.Engine.Core;

public sealed class GameEngine
{
    public FrameBuffer FrameBuffer { get; } = new();
    public InputManager Input { get; } = new();
    public WorldGenerator Generator { get; private set; }
    public TileGrid Grid { get; private set; }
    public Camera2D Camera { get; } = new(viewportWidth: 320, viewportHeight: 184);
    public PlayerEntity Player { get; private set; }
    public IGameTheme CurrentTheme { get; set; }

    public string CurrentSeed { get; private set; } = "RETRO1";
    public bool IsPaused { get; set; } = false;
    public int Fps { get; set; } = 60;

    public GameEngine(string seed = "RETRO1")
    {
        CurrentSeed = string.IsNullOrWhiteSpace(seed) ? "RETRO1" : seed.Trim();
        Generator = new WorldGenerator(CurrentSeed);
        Grid = new TileGrid(Generator);
        Player = new PlayerEntity(new Vector2f(32, 120));
        CurrentTheme = new DefaultTheme();
    }

    public void Restart(string? newSeed = null)
    {
        if (!string.IsNullOrWhiteSpace(newSeed))
        {
            CurrentSeed = newSeed.Trim();
        }

        Generator.SetSeed(CurrentSeed);
        Grid.Reset();
        Camera.Reset();
        Player.Reset(new Vector2f(32, 120));
        IsPaused = false;
    }

    public void Update(float dt)
    {
        Input.AdvanceFrame();

        if (Input.JustPressed(GameAction.Pause))
        {
            IsPaused = !IsPaused;
        }

        if (IsPaused) return;

        // If player falls below the world or runs out of health, restart from spawn
        if (Player.Position.Y > TileChunk.ChunkHeight * PhysicsConstants.TileSize || Player.Health <= 0)
        {
            Restart(CurrentSeed);
            return;
        }

        // 1. Update Player Locomotion & Physics
        Player.Update(dt, Input, Grid);

        // 2. Camera Tracking
        Camera.Follow(Player.Position, dt);
    }

    public void Render()
    {
        // 1. Theme Background
        CurrentTheme.DrawBackground(FrameBuffer, Camera);

        // 2. Visible Tiles inside 320x184 viewport
        int minCol = (int)MathF.Floor(Camera.X / PhysicsConstants.TileSize);
        int maxCol = (int)MathF.Ceiling((Camera.X + Camera.ViewportWidth) / PhysicsConstants.TileSize);
        int minRow = (int)MathF.Floor(Camera.Y / PhysicsConstants.TileSize);
        int maxRow = (int)MathF.Ceiling((Camera.Y + Camera.ViewportHeight) / PhysicsConstants.TileSize);

        for (int r = minRow; r <= maxRow; r++)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                TileType type = Grid.GetTile(c, r);
                if (type != TileType.Empty)
                {
                    int screenX = Camera.WorldToScreenX(c * PhysicsConstants.TileSize);
                    int screenY = Camera.WorldToScreenY(r * PhysicsConstants.TileSize);
                    CurrentTheme.DrawTile(FrameBuffer, type, screenX, screenY, c, r);
                }
            }
        }

        // 3. Player Sprite
        int playerSx = Camera.WorldToScreenX(Player.Position.X);
        int playerSy = Camera.WorldToScreenY(Player.Position.Y);
        CurrentTheme.DrawPlayer(FrameBuffer, Player, playerSx, playerSy);

        // 4. Top & Bottom HUD
        CurrentTheme.DrawHud(FrameBuffer, Player, CurrentSeed, Fps);

        // 5. Pause Banner
        if (IsPaused)
        {
            FrameBuffer.DrawBeveledBox(110, 80, 100, 24, EgaPalette.GetRgba(EgaColorIndex.Black),
                EgaPalette.GetRgba(EgaColorIndex.BrightWhite), EgaPalette.GetRgba(EgaColorIndex.DarkGray));
            FrameBuffer.DrawText(130, 88, "* PAUSED *", EgaColorIndex.Yellow);
        }
    }
}
