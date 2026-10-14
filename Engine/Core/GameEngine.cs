using System;
using System.Collections.Generic;
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

    // Pre-allocated combat pools (Zero heap allocation during gameplay)
    public readonly Projectile[] Projectiles = new Projectile[64];
    public readonly MonsterEntity[] Monsters = new MonsterEntity[32];
    public readonly PickupItem[] Pickups = new PickupItem[64];
    private readonly HashSet<int> _populatedChunks = new();

    public string CurrentSeed { get; private set; } = "RETRO1";
    public bool IsPaused { get; set; } = false;
    public int Fps { get; set; } = 60;

    public GameEngine(string seed = "RETRO1")
    {
        for (int i = 0; i < Monsters.Length; i++)
        {
            Monsters[i] = new MonsterEntity();
        }

        CurrentSeed = string.IsNullOrWhiteSpace(seed) ? "RETRO1" : seed.Trim();
        Generator = new WorldGenerator(CurrentSeed);
        Grid = new TileGrid(Generator);
        Player = new PlayerEntity(new Vector2f(32, 416));
        Camera.SnapTo(Player.Position);
        CurrentTheme = new DefaultTheme();

        PopulateInitialArea();
    }

    public void Restart(string? newSeed = null)
    {
        if (!string.IsNullOrWhiteSpace(newSeed))
        {
            CurrentSeed = newSeed.Trim();
        }

        Generator.SetSeed(CurrentSeed);
        Grid.Reset();
        _populatedChunks.Clear();

        for (int i = 0; i < Projectiles.Length; i++) Projectiles[i].IsActive = false;
        for (int i = 0; i < Monsters.Length; i++) Monsters[i].IsActive = false;
        for (int i = 0; i < Pickups.Length; i++) Pickups[i].IsActive = false;

        Player.Reset(new Vector2f(32, 416));
        Camera.SnapTo(Player.Position);
        IsPaused = false;

        PopulateInitialArea();
    }

    private void PopulateInitialArea()
    {
        for (int c = 0; c <= 3; c++)
        {
            PopulateChunkEntities(c);
        }
    }

    public void PopulateChunkEntities(int chunkIndex)
    {
        if (!_populatedChunks.Add(chunkIndex)) return;

        var chunk = Grid.GetOrCreateChunk(chunkIndex);
        var rng = new DeterministicRandom(Generator.Seed + "_ENTITIES_" + chunkIndex);
        int ts = PhysicsConstants.TileSize;
        float chunkWorldX = chunkIndex * TileChunk.ChunkWidth * ts;

        if (chunkIndex == 0)
        {
            // Starter area pickups on stepped climbing decks
            SpawnPickup(PickupType.BlueGem, new Vector2f(chunkWorldX + 3 * ts, 25 * ts - 14));
            SpawnPickup(PickupType.RedRuby, new Vector2f(chunkWorldX + 7 * ts, 22 * ts - 14));
            SpawnPickup(PickupType.GoldCartridge, new Vector2f(chunkWorldX + 7 * ts, 16 * ts - 14));
            SpawnPickup(PickupType.RapidFire, new Vector2f(chunkWorldX + 6 * ts, 10 * ts - 14));
            SpawnPickup(PickupType.MedKit, new Vector2f(chunkWorldX + 6 * ts, 4 * ts - 14));

            // Starter monsters with exact platform surface anchoring
            SpawnMonster(MonsterType.PatrolDroid, new Vector2f(chunkWorldX + 5 * ts, 22 * ts - 14), platformY: 22 * ts);
            SpawnMonster(MonsterType.MechaHound, new Vector2f(chunkWorldX + 6 * ts, 16 * ts - 14), platformY: 16 * ts);
            return;
        }

        // Scan chunk platforms to find valid standing surfaces
        var platformSpots = new List<(int LocalCol, int PlatformRow)>();
        var ceilingSpots = new List<(int LocalCol, int CeilingRow)>();

        for (int r = 1; r < TileChunk.ChunkHeight - 1; r++)
        {
            for (int c = 1; c < TileChunk.ChunkWidth - 1; c++)
            {
                TileType surface = chunk.GetTile(c, r);
                TileType above = chunk.GetTile(c, r - 1);
                TileType below = chunk.GetTile(c, r + 1);

                // Floor platform spot (Solid or OneWayPlatform with Empty above it, avoiding ladder tiles)
                if ((surface == TileType.Solid || surface == TileType.OneWayPlatform) && above == TileType.Empty)
                {
                    if (surface != TileType.Ladder && above != TileType.Ladder)
                    {
                        platformSpots.Add((c, r));
                    }
                }

                // Ceiling spot for mounted turrets (Solid block overhead with Empty below it)
                if (surface == TileType.Solid && below == TileType.Empty)
                {
                    ceilingSpots.Add((c, r));
                }
            }
        }

        if (platformSpots.Count == 0) return;

        // Shuffle platform spots deterministically
        for (int i = 0; i < platformSpots.Count; i++)
        {
            int swapIdx = rng.NextInt(0, platformSpots.Count);
            (platformSpots[i], platformSpots[swapIdx]) = (platformSpots[swapIdx], platformSpots[i]);
        }

        int spotIndex = 0;

        // 1. Deterministic Monsters (1 to 3 monsters per chunk)
        int monsterCount = rng.NextInt(1, 4);
        for (int m = 0; m < monsterCount; m++)
        {
            // Ceiling Turret candidate
            if (rng.NextBool(0.30f) && ceilingSpots.Count > 0)
            {
                var cSpot = ceilingSpots[rng.NextInt(0, ceilingSpots.Count)];
                float cmx = chunkWorldX + cSpot.LocalCol * ts;
                float ceilingBottom = (cSpot.CeilingRow + 1) * ts;
                SpawnMonster(MonsterType.CeilingTurret, new Vector2f(cmx, ceilingBottom), platformY: 0);
                continue;
            }

            if (spotIndex >= platformSpots.Count) break;

            var spot = platformSpots[spotIndex++];
            float mx = chunkWorldX + spot.LocalCol * ts;
            float platformTop = spot.PlatformRow * ts;

            // Pick monster type based on vertical height
            MonsterType mType;
            if (spot.PlatformRow <= 6)
            {
                mType = MonsterType.HoverDrone;
            }
            else if (spot.PlatformRow <= 18)
            {
                mType = rng.NextBool(0.6f) ? MonsterType.PatrolDroid : MonsterType.HoverDrone;
            }
            else
            {
                float roll = rng.NextFloat();
                if (roll < 0.40f) mType = MonsterType.PatrolDroid;
                else if (roll < 0.70f) mType = MonsterType.MechaHound;
                else mType = MonsterType.HeavyGuard;
            }

            float monsterHeight = mType switch
            {
                MonsterType.HeavyGuard => 26.0f,
                MonsterType.HoverDrone => 12.0f,
                _ => 14.0f
            };

            float my = mType == MonsterType.HoverDrone ? platformTop - 24.0f : platformTop - monsterHeight;
            float pY = mType == MonsterType.HoverDrone ? 0 : platformTop;

            SpawnMonster(mType, new Vector2f(mx, my), platformY: pY);
        }

        // 2. Deterministic Pickups (2 to 4 pickups per chunk)
        int pickupCount = rng.NextInt(2, 5);
        for (int p = 0; p < pickupCount && spotIndex < platformSpots.Count; p++)
        {
            var spot = platformSpots[spotIndex++];
            float platformTop = spot.PlatformRow * ts;
            float px = chunkWorldX + spot.LocalCol * ts + 2;
            float py = platformTop - 14; // Rest cleanly on top of the platform

            float roll = rng.NextFloat();
            PickupType pType;
            if (roll < 0.45f) pType = PickupType.BlueGem;
            else if (roll < 0.65f) pType = PickupType.RedRuby;
            else if (roll < 0.80f) pType = PickupType.GoldCartridge;
            else if (roll < 0.90f) pType = PickupType.MedKit;
            else
            {
                float powerRoll = rng.NextFloat();
                if (powerRoll < 0.35f) pType = PickupType.RapidFire;
                else if (powerRoll < 0.70f) pType = PickupType.HighJumpBoots;
                else pType = PickupType.SpeedDash;
            }

            SpawnPickup(pType, new Vector2f(px, py));
        }
    }

    private void SpawnMonster(MonsterType type, Vector2f pos, float platformY = 0)
    {
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (!Monsters[i].IsActive)
            {
                Monsters[i].Spawn(i, type, pos, platformY);
                return;
            }
        }
    }

    private void SpawnPickup(PickupType type, Vector2f pos)
    {
        for (int i = 0; i < Pickups.Length; i++)
        {
            if (!Pickups[i].IsActive)
            {
                Pickups[i].Spawn(type, pos);
                return;
            }
        }
    }

    public void SpawnProjectile(Vector2f pos, Vector2f vel, int damage, bool fromPlayer)
    {
        for (int i = 0; i < Projectiles.Length; i++)
        {
            if (!Projectiles[i].IsActive)
            {
                Projectiles[i].Spawn(pos, vel, damage, fromPlayer);
                return;
            }
        }
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

        // Player shooting trigger
        if (Player.TryConsumeShoot(out var bulletPos, out var bulletVel))
        {
            SpawnProjectile(bulletPos, bulletVel, damage: 1, fromPlayer: true);
        }

        // 2. Camera Tracking (Centered on Player)
        Camera.Follow(Player.Position, dt);

        // Ensure active chunk neighborhood is populated
        int currentChunk = (int)MathF.Floor(Player.Position.X / (TileChunk.ChunkWidth * PhysicsConstants.TileSize));
        for (int c = currentChunk - 1; c <= currentChunk + 3; c++)
        {
            if (c >= 0) PopulateChunkEntities(c);
        }

        // 3. Update Projectiles & Check Collisions
        for (int i = 0; i < Projectiles.Length; i++)
        {
            if (!Projectiles[i].IsActive) continue;
            Projectiles[i].Update(dt);
            if (!Projectiles[i].IsActive) continue;

            // Check tile collision
            int col = (int)MathF.Floor((Projectiles[i].Position.X + 4.0f) / PhysicsConstants.TileSize);
            int row = (int)MathF.Floor((Projectiles[i].Position.Y + 2.0f) / PhysicsConstants.TileSize);
            if (Grid.GetTile(col, row) is TileType.Solid or TileType.Border)
            {
                Projectiles[i].IsActive = false;
                continue;
            }

            // Player projectile vs Monsters
            if (Projectiles[i].FromPlayer)
            {
                var pBox = Projectiles[i].BoundingBox;
                for (int m = 0; m < Monsters.Length; m++)
                {
                    if (Monsters[m].IsActive && pBox.Overlaps(Monsters[m].BoundingBox))
                    {
                        Monsters[m].TakeDamage(Projectiles[i].Damage);
                        Projectiles[i].IsActive = false;
                        if (!Monsters[m].IsActive)
                        {
                            Player.Score += Monsters[m].ScoreReward;
                        }
                        break;
                    }
                }
            }
            else
            {
                // Enemy projectile vs Player
                if (Projectiles[i].BoundingBox.Overlaps(Player.BoundingBox) && Player.InvulnerabilityTimer <= 0)
                {
                    Player.TakeDamage(Projectiles[i].Damage);
                    Projectiles[i].IsActive = false;
                }
            }
        }

        // 4. Update Monsters & Check Overlaps
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (!Monsters[i].IsActive) continue;

            Monsters[i].Update(dt, Grid, Player.Position, out bool wantsShoot, out var shootPos, out var shootVel, out int shootDmg);
            if (wantsShoot)
            {
                SpawnProjectile(shootPos, shootVel, shootDmg, fromPlayer: false);
            }

            // Monster touch vs Player
            if (Monsters[i].BoundingBox.Overlaps(Player.BoundingBox) && Player.InvulnerabilityTimer <= 0)
            {
                Player.TakeDamage(Monsters[i].TouchDamage);
                // Recoil knockback
                Player.Velocity.X = (Player.Position.X < Monsters[i].Position.X) ? -130.0f : 130.0f;
                Player.Velocity.Y = -140.0f;
            }
        }

        // 5. Update Pickups & Check Collection
        for (int i = 0; i < Pickups.Length; i++)
        {
            if (!Pickups[i].IsActive) continue;
            Pickups[i].Update(dt);

            if (Pickups[i].BoundingBox.Overlaps(Player.BoundingBox))
            {
                Pickups[i].IsActive = false;
                switch (Pickups[i].Type)
                {
                    case PickupType.BlueGem:
                        Player.Score += 100;
                        break;
                    case PickupType.RedRuby:
                        Player.Score += 250;
                        break;
                    case PickupType.GoldCartridge:
                        Player.Score += 500;
                        break;
                    case PickupType.MedKit:
                        Player.Health = Math.Min(PlayerEntity.MaxHealth, Player.Health + 1);
                        break;
                    case PickupType.RapidFire:
                        Player.HasRapidFire = true;
                        break;
                    case PickupType.HighJumpBoots:
                        Player.HasHighJumpBoots = true;
                        break;
                    case PickupType.SpeedDash:
                        Player.HasSpeedDash = true;
                        break;
                }
            }
        }
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

        // 3. Collectible Pickups
        for (int i = 0; i < Pickups.Length; i++)
        {
            if (Pickups[i].IsActive)
            {
                int sx = Camera.WorldToScreenX(Pickups[i].Position.X);
                int sy = Camera.WorldToScreenY(Pickups[i].Position.Y);
                CurrentTheme.DrawPickup(FrameBuffer, in Pickups[i], sx, sy);
            }
        }

        // 4. Monsters
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (Monsters[i].IsActive)
            {
                int sx = Camera.WorldToScreenX(Monsters[i].Position.X);
                int sy = Camera.WorldToScreenY(Monsters[i].Position.Y);
                CurrentTheme.DrawMonster(FrameBuffer, Monsters[i], sx, sy);
            }
        }

        // 5. Player Sprite
        int playerSx = Camera.WorldToScreenX(Player.Position.X);
        int playerSy = Camera.WorldToScreenY(Player.Position.Y);
        CurrentTheme.DrawPlayer(FrameBuffer, Player, playerSx, playerSy);

        // 6. Projectiles
        for (int i = 0; i < Projectiles.Length; i++)
        {
            if (Projectiles[i].IsActive)
            {
                int sx = Camera.WorldToScreenX(Projectiles[i].Position.X);
                int sy = Camera.WorldToScreenY(Projectiles[i].Position.Y);
                CurrentTheme.DrawProjectile(FrameBuffer, in Projectiles[i], sx, sy);
            }
        }

        // 7. Top & Bottom HUD
        CurrentTheme.DrawHud(FrameBuffer, Player, CurrentSeed, Fps);

        // 8. Pause Banner
        if (IsPaused)
        {
            FrameBuffer.DrawBeveledBox(110, 80, 100, 24, EgaPalette.GetRgba(EgaColorIndex.Black),
                EgaPalette.GetRgba(EgaColorIndex.BrightWhite), EgaPalette.GetRgba(EgaColorIndex.DarkGray));
            FrameBuffer.DrawText(130, 88, "* PAUSED *", EgaColorIndex.Yellow);
        }
    }
}
