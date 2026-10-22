using System;
using System.Collections.Generic;
using JumpRunShoot.Engine.Audio;
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
    public int CurrentThemeIndex { get; private set; } = 0;
    public bool IsFlightCheatActive { get; set; } = false;

    public void SetTheme(int themeIndex)
    {
        if (themeIndex < 0 || themeIndex >= ThemeRegistry.AllThemes.Length) return;
        CurrentThemeIndex = themeIndex;
        CurrentTheme = ThemeRegistry.AllThemes[CurrentThemeIndex];
        if (CurrentTheme.ZeroGravity)
        {
            Player.IsFlying = true;
        }
        else if (!IsFlightCheatActive)
        {
            Player.IsFlying = false;
            Player.Velocity.Y = 0;
        }
        ShowNotification($"THEME: {CurrentTheme.Name.ToUpperInvariant()}", 2.0f, EgaColorIndex.BrightCyan);
    }

    public void CycleTheme()
    {
        SetTheme((CurrentThemeIndex + 1) % ThemeRegistry.AllThemes.Length);
    }

    // Pre-allocated combat pools (Zero heap allocation during gameplay)
    public readonly Projectile[] Projectiles = new Projectile[64];
    public readonly MonsterEntity[] Monsters = new MonsterEntity[64];
    public readonly PickupItem[] Pickups = new PickupItem[64];
    private readonly HashSet<int> _populatedChunks = new();

    public const string DefaultSeed = "FOO";
    public string CurrentSeed { get; private set; } = DefaultSeed;
    public bool IsPaused { get; set; } = false;
    public bool IsGameOver { get; private set; } = false;
    public bool CheatsEnabled { get; set; } = false;
    public string PlayerName { get; set; } = "Unknown";
    public string DeathMessage { get; private set; } = string.Empty;
    public int Fps { get; set; } = 60;
    public bool IsSoundEnabled { get; set; } = true;
    public int PendingAudioFlags;

    public float MaxDistanceX { get; private set; }
    public int MaxTileColumnReached { get; private set; }

    public void TriggerAudio(AudioEvent evt)
    {
        if (!IsSoundEnabled) return;
        PendingAudioFlags |= (int)evt;
    }

    public int ConsumeAudioFlags()
    {
        int flags = PendingAudioFlags;
        PendingAudioFlags = 0;
        return flags;
    }

    private void EnterGameOver(string message)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        DeathMessage = message;
        ActiveNotification.Timer = 0f;
        TriggerAudio(AudioEvent.PlayerDie);
    }

    public struct GameNotification
    {
        public string Text;
        public float Timer;
        public EgaColorIndex Color;
    }

    public GameNotification ActiveNotification;

    public void ShowNotification(string text, float duration = 2.0f, EgaColorIndex color = EgaColorIndex.Yellow)
    {
        ActiveNotification = new GameNotification
        {
            Text = text,
            Timer = duration,
            Color = color
        };
    }

    public Vector2f FindRandomSpawnPosition(string seed)
    {
        // Search first 1-2 screens (cols 2..36) across elevation tiers (rows 4..28)
        var candidates = new List<(int Col, int Row)>();
        for (int c = 2; c <= 36; c++)
        {
            for (int r = 4; r <= 28; r++)
            {
                TileType floor = Grid.GetTile(c, r);
                if (floor == TileType.Solid || floor == TileType.OneWayPlatform || floor == TileType.Crate)
                {
                    // Check clearance above floor: 2 empty tiles, not hazard
                    if (Grid.GetTile(c, r - 1) == TileType.Empty &&
                        Grid.GetTile(c, r - 2) == TileType.Empty &&
                        Grid.GetTile(c, r) != TileType.Hazard &&
                        Grid.GetTile(c, r - 1) != TileType.Hazard)
                    {
                        candidates.Add((c, r));
                    }
                }
            }
        }

        if (candidates.Count > 0)
        {
            var rng = new DeterministicRandom(seed + "_PLAYER_SPAWN");
            var chosen = candidates[rng.NextInt(0, candidates.Count)];
            return new Vector2f(chosen.Col * PhysicsConstants.TileSize + 2, chosen.Row * PhysicsConstants.TileSize - 24);
        }

        return new Vector2f(32, 416);
    }

    public GameEngine(string seed = DefaultSeed)
    {
        for (int i = 0; i < Monsters.Length; i++)
        {
            Monsters[i] = new MonsterEntity();
        }

        CurrentSeed = string.IsNullOrWhiteSpace(seed) ? DefaultSeed : seed.Trim();
        Generator = new WorldGenerator(CurrentSeed);
        Grid = new TileGrid(Generator);
        Vector2f spawnPos = FindRandomSpawnPosition(CurrentSeed);
        Player = new PlayerEntity(spawnPos);
        Camera.SnapTo(Player.Position);
        CurrentTheme = new DefaultTheme();
        if (CurrentTheme.ZeroGravity)
        {
            Player.IsFlying = true;
        }
        MaxDistanceX = Player.Position.X;
        MaxTileColumnReached = (int)MathF.Floor(Player.Position.X / PhysicsConstants.TileSize);

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

        Vector2f spawnPos = FindRandomSpawnPosition(CurrentSeed);
        Player.Reset(spawnPos);
        if (CurrentTheme.ZeroGravity || IsFlightCheatActive)
        {
            Player.IsFlying = true;
        }
        Camera.SnapTo(Player.Position);
        MaxDistanceX = Player.Position.X;
        MaxTileColumnReached = (int)MathF.Floor(Player.Position.X / PhysicsConstants.TileSize);
        IsPaused = false;
        IsGameOver = false;
        DeathMessage = string.Empty;
        PendingAudioFlags = 0;

        PopulateInitialArea();
    }

    private void PopulateInitialArea()
    {
        int currentChunk = (int)MathF.Floor(Player.Position.X / (TileChunk.ChunkWidth * PhysicsConstants.TileSize));
        StreamChunkNeighborhood(currentChunk);
    }

    public void StreamChunkNeighborhood(int currentChunk)
    {
        int minActiveChunk = Math.Max(0, currentChunk - 2);
        int maxActiveChunk = currentChunk + 3;
        int ts = PhysicsConstants.TileSize;
        float minActiveX = (minActiveChunk - 1) * TileChunk.ChunkWidth * ts;
        float maxActiveX = (maxActiveChunk + 2) * TileChunk.ChunkWidth * ts;

        // 1. Recycle distant monsters outside the active streaming window
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (Monsters[i].IsActive && (Monsters[i].Position.X < minActiveX || Monsters[i].Position.X > maxActiveX))
            {
                Monsters[i].IsActive = false;
            }
        }

        // 2. Recycle distant pickups outside the active streaming window
        for (int i = 0; i < Pickups.Length; i++)
        {
            if (Pickups[i].IsActive && (Pickups[i].Position.X < minActiveX || Pickups[i].Position.X > maxActiveX))
            {
                Pickups[i].IsActive = false;
            }
        }

        // 3. Recycle distant projectiles outside the active streaming window
        for (int i = 0; i < Projectiles.Length; i++)
        {
            if (Projectiles[i].IsActive && (Projectiles[i].Position.X < minActiveX || Projectiles[i].Position.X > maxActiveX))
            {
                Projectiles[i].IsActive = false;
            }
        }

        // 4. Evict distant chunk indices from population tracker so chunks can re-stream if visited again
        _populatedChunks.RemoveWhere(c => c < minActiveChunk || c > maxActiveChunk);

        // 5. Evict distant procedural tile chunks from TileGrid (bidirectional memory capping)
        Grid.CullDistantChunks(currentChunk, 6);

        // 6. Ensure all chunks in the active neighborhood are populated
        for (int c = minActiveChunk; c <= maxActiveChunk; c++)
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

        // Scan chunk platforms to find valid standing surfaces and measure contiguous width
        var platformSpots = new List<(int LocalCol, int PlatformRow, int Width)>();
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
                        // Measure contiguous walkable platform width around this tile
                        int w = 1;
                        int left = c - 1;
                        while (left >= 0 && (chunk.GetTile(left, r) == TileType.Solid || chunk.GetTile(left, r) == TileType.OneWayPlatform) && chunk.GetTile(left, r) != TileType.Ladder && chunk.GetTile(left, r - 1) == TileType.Empty)
                        {
                            w++;
                            left--;
                        }
                        int right = c + 1;
                        while (right < TileChunk.ChunkWidth && (chunk.GetTile(right, r) == TileType.Solid || chunk.GetTile(right, r) == TileType.OneWayPlatform) && chunk.GetTile(right, r) != TileType.Ladder && chunk.GetTile(right, r - 1) == TileType.Empty)
                        {
                            w++;
                            right++;
                        }
                        platformSpots.Add((c, r, w));
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

        if (chunkIndex == 0)
        {
            // Starter area: distribute items & monsters on validated wide platforms
            var widePlatforms = platformSpots.FindAll(s => s.Width >= 2);
            if (widePlatforms.Count > 0)
            {
                var s1 = widePlatforms[0];
                SpawnMonster(MonsterType.PatrolDroid, new Vector2f(chunkWorldX + s1.LocalCol * ts, s1.PlatformRow * ts - 14), platformY: s1.PlatformRow * ts);
            }
            var houndPlatforms = platformSpots.FindAll(s => s.Width >= 3);
            if (houndPlatforms.Count > 1)
            {
                var s2 = houndPlatforms[houndPlatforms.Count - 1];
                SpawnMonster(MonsterType.MechaHound, new Vector2f(chunkWorldX + s2.LocalCol * ts, s2.PlatformRow * ts - 14), platformY: s2.PlatformRow * ts);
            }

            var starterTypes = new[] { PickupType.BlueGem, PickupType.RedRuby, PickupType.GoldCartridge, PickupType.RapidFire, PickupType.MedKit };
            int pCount = 0;
            for (int i = 0; i < platformSpots.Count && pCount < starterTypes.Length; i++)
            {
                var spot = platformSpots[i];
                Vector2f pPos = new Vector2f(chunkWorldX + spot.LocalCol * ts + 2, spot.PlatformRow * ts - 14);
                SpawnPickup(starterTypes[pCount++], pPos);
            }
            return;
        }

        // Shuffle platform spots deterministically
        for (int i = 0; i < platformSpots.Count; i++)
        {
            int swapIdx = rng.NextInt(0, platformSpots.Count);
            (platformSpots[i], platformSpots[swapIdx]) = (platformSpots[swapIdx], platformSpots[i]);
        }

        int spotIndex = 0;

        // 1. Deterministic Monsters with linear horizontal difficulty scaling
        // As player advances horizontally, monster count, threat types, and durability scale up linearly
        int minMonsters = Math.Clamp(1 + chunkIndex / 3, 1, 8);
        int maxMonsters = Math.Clamp(2 + (chunkIndex * 2) / 3, 2, 14);
        int monsterCount = rng.NextInt(minMonsters, maxMonsters + 1);

        float turretChance = Math.Clamp(0.20f + chunkIndex * 0.025f, 0.20f, 0.50f);
        float heavyGuardThreshold = Math.Clamp(0.05f + chunkIndex * 0.035f, 0.05f, 0.40f);
        float houndThreshold = Math.Clamp(0.20f + chunkIndex * 0.030f, 0.20f, 0.45f);
        int bonusHealth = chunkIndex >= 14 ? 2 : (chunkIndex >= 8 ? 1 : 0);

        for (int m = 0; m < monsterCount; m++)
        {
            // Ceiling Turret candidate
            if (rng.NextBool(turretChance) && ceilingSpots.Count > 0)
            {
                var cSpot = ceilingSpots[rng.NextInt(0, ceilingSpots.Count)];
                float cmx = chunkWorldX + cSpot.LocalCol * ts;
                float ceilingBottom = (cSpot.CeilingRow + 1) * ts;
                SpawnMonster(MonsterType.CeilingTurret, new Vector2f(cmx, ceilingBottom), platformY: 0, bonusHealth: bonusHealth);
                continue;
            }

            if (spotIndex >= platformSpots.Count)
            {
                // Airspace monster (HoverDrone) when platform spots are exhausted
                float hx = chunkWorldX + rng.NextInt(2, TileChunk.ChunkWidth - 2) * ts;
                float hy = rng.NextInt(4, 18) * ts;
                SpawnMonster(MonsterType.HoverDrone, new Vector2f(hx, hy), platformY: 0, bonusHealth: bonusHealth);
                continue;
            }

            var spot = platformSpots[spotIndex++];
            float mx = chunkWorldX + spot.LocalCol * ts;
            float platformTop = spot.PlatformRow * ts;

            // Pick monster type based on vertical height, platform width, and horizontal difficulty
            MonsterType mType;
            if (spot.PlatformRow <= 6)
            {
                mType = MonsterType.HoverDrone;
            }
            else if (spot.PlatformRow <= 16)
            {
                float midRoll = rng.NextFloat();
                if (midRoll < 0.35f || spot.Width < 2) mType = MonsterType.HoverDrone;
                else if (midRoll < 0.35f + houndThreshold && spot.Width >= 3) mType = MonsterType.MechaHound;
                else mType = MonsterType.PatrolDroid;
            }
            else
            {
                if (spot.Width < 2)
                {
                    mType = MonsterType.HoverDrone;
                }
                else
                {
                    float roll = rng.NextFloat();
                    if (roll < heavyGuardThreshold)
                    {
                        mType = MonsterType.HeavyGuard;
                    }
                    else if (roll < heavyGuardThreshold + houndThreshold && spot.Width >= 3)
                    {
                        mType = MonsterType.MechaHound;
                    }
                    else
                    {
                        mType = MonsterType.PatrolDroid;
                    }
                }
            }

            float monsterHeight = mType switch
            {
                MonsterType.HeavyGuard => 26.0f,
                MonsterType.HoverDrone => 12.0f,
                _ => 14.0f
            };

            float my = mType == MonsterType.HoverDrone ? platformTop - 24.0f : platformTop - monsterHeight;
            float pY = mType == MonsterType.HoverDrone ? 0 : platformTop;

            SpawnMonster(mType, new Vector2f(mx, my), platformY: pY, bonusHealth: bonusHealth);
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

    private void SpawnMonster(MonsterType type, Vector2f pos, float platformY = 0, int bonusHealth = 0)
    {
        if (Player != null && MathF.Abs(pos.X - Player.Position.X) < 48 && MathF.Abs(pos.Y - Player.Position.Y) < 48)
        {
            return;
        }

        // Avoid spawning directly on top of an active pickup
        for (int p = 0; p < Pickups.Length; p++)
        {
            if (Pickups[p].IsActive && MathF.Abs(pos.X - Pickups[p].Position.X) < 32 && MathF.Abs(pos.Y - Pickups[p].Position.Y) < 20)
            {
                return;
            }
        }

        int targetIndex = -1;
        for (int i = 0; i < Monsters.Length; i++)
        {
            if (!Monsters[i].IsActive)
            {
                targetIndex = i;
                break;
            }
        }

        // Fallback eviction: if pool is full, recycle the monster furthest away from the player
        if (targetIndex < 0 && Player != null)
        {
            float maxDistSq = -1f;
            for (int i = 0; i < Monsters.Length; i++)
            {
                float dx = Monsters[i].Position.X - Player.Position.X;
                float dy = Monsters[i].Position.Y - Player.Position.Y;
                float distSq = dx * dx + dy * dy;
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    targetIndex = i;
                }
            }
        }

        if (targetIndex >= 0)
        {
            Monsters[targetIndex].Spawn(targetIndex, type, pos, platformY);
            if (bonusHealth > 0)
            {
                Monsters[targetIndex].Health += bonusHealth;
                Monsters[targetIndex].MaxHealth += bonusHealth;
            }
            int chunk = (int)MathF.Floor(pos.X / (TileChunk.ChunkWidth * PhysicsConstants.TileSize));
            if (chunk > 0)
            {
                Monsters[targetIndex].ScoreReward += chunk * 50;
            }
        }
    }

    private void SpawnPickup(PickupType type, Vector2f pos)
    {
        if (Player != null && MathF.Abs(pos.X - Player.Position.X) < 24 && MathF.Abs(pos.Y - Player.Position.Y) < 24)
        {
            return;
        }

        // Avoid spawning directly on top of an active monster
        for (int m = 0; m < Monsters.Length; m++)
        {
            if (Monsters[m].IsActive && MathF.Abs(pos.X - Monsters[m].Position.X) < 32 && MathF.Abs(pos.Y - Monsters[m].Position.Y) < 20)
            {
                return;
            }
        }

        int targetIndex = -1;
        for (int i = 0; i < Pickups.Length; i++)
        {
            if (!Pickups[i].IsActive)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0 && Player != null)
        {
            float maxDistSq = -1f;
            for (int i = 0; i < Pickups.Length; i++)
            {
                float dx = Pickups[i].Position.X - Player.Position.X;
                float dy = Pickups[i].Position.Y - Player.Position.Y;
                float distSq = dx * dx + dy * dy;
                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    targetIndex = i;
                }
            }
        }

        if (targetIndex >= 0)
        {
            Pickups[targetIndex].Spawn(type, pos);
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

        if (IsGameOver)
        {
            if (Input.AnyKeyJustPressed)
            {
                Restart(CurrentSeed);
            }
            return;
        }

        // Generic Pause: KeyP exclusively toggles pause state
        // When paused, game freezes and only pressing P again can unpause it
        if (Input.IsKeyJustPressed("KeyP"))
        {
            IsPaused = !IsPaused;
        }

        if (IsPaused) return;

        if (CheatsEnabled)
        {
            if (Input.IsKeyJustPressed("KeyG"))
            {
                Player.IsGodMode = !Player.IsGodMode;
                ShowNotification(Player.IsGodMode ? "CHEAT: GOD MODE ON!" : "CHEAT: GOD MODE OFF!", 2.0f,
                    Player.IsGodMode ? EgaColorIndex.Yellow : EgaColorIndex.LightGray);
            }
            if (Input.IsKeyJustPressed("KeyF"))
            {
                IsFlightCheatActive = !IsFlightCheatActive;
                Player.IsFlying = IsFlightCheatActive || CurrentTheme.ZeroGravity;
                if (!Player.IsFlying)
                {
                    Player.Velocity.Y = 0;
                }
                ShowNotification(Player.IsFlying ? "CHEAT: FLY MODE ON! [WASD / ARROWS]" : "CHEAT: FLY MODE OFF!", 2.0f,
                    Player.IsFlying ? EgaColorIndex.BrightCyan : EgaColorIndex.LightGray);
            }
            if (Input.IsKeyJustPressed("KeyN"))
            {
                Player.IsNoClip = !Player.IsNoClip;
                ShowNotification(Player.IsNoClip ? "CHEAT: NOCLIP ON! (PASS WALLS)" : "CHEAT: NOCLIP OFF!", 2.0f,
                    Player.IsNoClip ? EgaColorIndex.BrightMagenta : EgaColorIndex.LightGray);
            }
            if (Input.IsKeyJustPressed("KeyU"))
            {
                Player.HasRapidFire = true;
                Player.HasHighJumpBoots = true;
                Player.HasSpeedDash = true;
                ShowNotification("CHEAT: ALL POWERUPS ACTIVE!", 2.0f, EgaColorIndex.BrightGreen);
                TriggerAudio(AudioEvent.Pickup);
            }
            if (Input.IsKeyJustPressed("KeyH"))
            {
                Player.Health = PlayerEntity.MaxHealth;
                ShowNotification("CHEAT: FULL HEALTH RESTORED!", 2.0f, EgaColorIndex.BrightRed);
                TriggerAudio(AudioEvent.Pickup);
            }
            if (Input.IsKeyJustPressed("KeyK"))
            {
                int count = 0;
                for (int i = 0; i < Monsters.Length; i++)
                {
                    if (Monsters[i].IsActive)
                    {
                        Monsters[i].IsActive = false;
                        Player.Score += 250;
                        count++;
                    }
                }
                TriggerAudio(AudioEvent.MonsterDie);
                ShowNotification($"CHEAT: SCREEN NUKE! ({count} FOES)", 2.0f, EgaColorIndex.BrightMagenta);
            }
        }

        if (ActiveNotification.Timer > 0f)
        {
            ActiveNotification.Timer -= dt;
        }

        // Check if player fell below the world or ran out of health prior to frame update
        if (Player.Position.Y > TileChunk.ChunkHeight * PhysicsConstants.TileSize)
        {
            if (Player.IsGodMode || Player.IsFlying)
            {
                Player.Position = new Vector2f(Player.Position.X, 18 * PhysicsConstants.TileSize);
                Player.Velocity = new Vector2f(Player.Velocity.X, Player.IsFlying ? 0 : -220f);
                ShowNotification(Player.IsFlying ? "FLY MODE: ABYSS HOVER!" : "GOD MODE: ABYSS BOUNCE!", 1.5f,
                    Player.IsFlying ? EgaColorIndex.BrightCyan : EgaColorIndex.Yellow);
            }
            else
            {
                EnterGameOver("FELL INTO THE ABYSS!");
                return;
            }
        }
        if (Player.Health <= 0)
        {
            EnterGameOver("OUT OF HEALTH!");
            return;
        }

        // 1. Update Player Locomotion & Physics
        Player.Update(dt, Input, Grid);
        if (Player.PendingAudioEvents != AudioEvent.None)
        {
            TriggerAudio(Player.PendingAudioEvents);
            Player.PendingAudioEvents = AudioEvent.None;
        }

        // Distance scoring: advancing right into new tile columns awards score
        // Each tile step gives 10 + (chunk * 2) pts -> linear step increase, quadratic cumulative score
        int currentTileCol = (int)MathF.Floor(Player.Position.X / PhysicsConstants.TileSize);
        if (currentTileCol > MaxTileColumnReached)
        {
            for (int c = MaxTileColumnReached + 1; c <= currentTileCol; c++)
            {
                int chunk = c / TileChunk.ChunkWidth;
                int stepScore = 10 + chunk * 2;
                Player.Score += stepScore;
            }
            MaxTileColumnReached = currentTileCol;
            MaxDistanceX = Player.Position.X;
        }

        if (Player.Position.Y > TileChunk.ChunkHeight * PhysicsConstants.TileSize)
        {
            if (Player.IsGodMode || Player.IsFlying)
            {
                Player.Position = new Vector2f(Player.Position.X, 18 * PhysicsConstants.TileSize);
                Player.Velocity = new Vector2f(Player.Velocity.X, Player.IsFlying ? 0 : -220f);
                ShowNotification(Player.IsFlying ? "FLY MODE: ABYSS HOVER!" : "GOD MODE: ABYSS BOUNCE!", 1.5f,
                    Player.IsFlying ? EgaColorIndex.BrightCyan : EgaColorIndex.Yellow);
            }
            else
            {
                EnterGameOver("FELL INTO THE ABYSS!");
                return;
            }
        }
        if (Player.Health <= 0)
        {
            EnterGameOver("IMPALED BY HAZARDS!");
            return;
        }

        // Player shooting trigger
        if (Player.TryConsumeShoot(out var bulletPos, out var bulletVel))
        {
            SpawnProjectile(bulletPos, bulletVel, damage: 1, fromPlayer: true);
            TriggerAudio(AudioEvent.PlayerShoot);
        }

        // 2. Camera Tracking & Entity Streaming Neighborhood
        Camera.Follow(Player.Position, dt);
        int currentChunk = (int)MathF.Floor(Player.Position.X / (TileChunk.ChunkWidth * PhysicsConstants.TileSize));
        StreamChunkNeighborhood(currentChunk);

        // 3. Update Projectiles & Check Collisions
        for (int i = 0; i < Projectiles.Length; i++)
        {
            if (!Projectiles[i].IsActive) continue;
            Projectiles[i].Update(dt);
            if (!Projectiles[i].IsActive) continue;

            // Check tile collision: bullets stop against solid walls, borders, and crates
            int col = (int)MathF.Floor((Projectiles[i].Position.X + 4.0f) / PhysicsConstants.TileSize);
            int row = (int)MathF.Floor((Projectiles[i].Position.Y + 2.0f) / PhysicsConstants.TileSize);
            if (Grid.GetTile(col, row) is TileType.Solid or TileType.Border or TileType.Crate)
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
                        Monsters[m].TakeDamage(Projectiles[i].Damage, Player.Position);
                        Projectiles[i].IsActive = false;
                        if (!Monsters[m].IsActive)
                        {
                            Player.Score += Monsters[m].ScoreReward;
                            TriggerAudio(AudioEvent.MonsterDie);
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
                    if (Player.Health <= 0)
                    {
                        EnterGameOver("BLASTED BY ENEMY FIRE!");
                        return;
                    }
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
                TriggerAudio(AudioEvent.MonsterShoot);
            }

            // Monster touch vs Player
            if (Monsters[i].BoundingBox.Overlaps(Player.BoundingBox) && Player.InvulnerabilityTimer <= 0)
            {
                Player.TakeDamage(Monsters[i].TouchDamage);
                // Recoil knockback
                Player.Velocity.X = (Player.Position.X < Monsters[i].Position.X) ? -130.0f : 130.0f;
                Player.Velocity.Y = -140.0f;
                if (Player.Health <= 0)
                {
                    EnterGameOver("DEFEATED BY A MONSTER!");
                    return;
                }
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
                TriggerAudio(AudioEvent.Pickup);
                switch (Pickups[i].Type)
                {
                    case PickupType.BlueGem:
                        Player.Score += 100;
                        ShowNotification("BLUE GEM +100 PTS", 1.5f, EgaColorIndex.BrightCyan);
                        break;
                    case PickupType.RedRuby:
                        Player.Score += 250;
                        ShowNotification("RED RUBY +250 PTS", 1.5f, EgaColorIndex.BrightRed);
                        break;
                    case PickupType.GoldCartridge:
                        Player.Score += 500;
                        ShowNotification("GOLD CARTRIDGE +500 PTS", 2.0f, EgaColorIndex.Yellow);
                        break;
                    case PickupType.MedKit:
                        Player.Health = Math.Min(PlayerEntity.MaxHealth, Player.Health + 1);
                        ShowNotification("FIRST AID! +1 HEALTH RESTORED", 2.0f, EgaColorIndex.BrightWhite);
                        break;
                    case PickupType.RapidFire:
                        Player.HasRapidFire = true;
                        ShowNotification("RAPID FIRE! HOLD SHOOT BUTTON", 2.5f, EgaColorIndex.BrightRed);
                        break;
                    case PickupType.HighJumpBoots:
                        Player.HasHighJumpBoots = true;
                        ShowNotification("HIGH JUMP BOOTS! LEAP HIGHER", 2.5f, EgaColorIndex.BrightCyan);
                        break;
                    case PickupType.SpeedDash:
                        Player.HasSpeedDash = true;
                        ShowNotification("SPEED DASH! +25% SPRINT SPEED", 2.5f, EgaColorIndex.BrightGreen);
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

        // 7. Top HUD
        CurrentTheme.DrawHud(FrameBuffer, Player, CurrentSeed, Fps);

        // 8. On-Screen Powerup / Pickup Notification Banner
        if (ActiveNotification.Timer > 0f && !string.IsNullOrEmpty(ActiveNotification.Text))
        {
            int textLen = ActiveNotification.Text.Length;
            int bannerW = Math.Min(310, textLen * 8 + 16);
            int bannerX = (320 - bannerW) / 2;
            int bannerY = 20; // Below top status bar
            FrameBuffer.FillRect(bannerX, bannerY, bannerW, 14, EgaColorIndex.Black);
            FrameBuffer.DrawRect(bannerX, bannerY, bannerW, 14, ActiveNotification.Color);
            FrameBuffer.DrawText(bannerX + 8, bannerY + 3, ActiveNotification.Text, ActiveNotification.Color);
        }

        // 9. Game Over / Death Banner (Wait for Key to Respawn)
        if (IsGameOver)
        {
            FrameBuffer.DrawBeveledBox(44, 48, 232, 104, EgaPalette.GetRgba(EgaColorIndex.Black),
                EgaPalette.GetRgba(EgaColorIndex.BrightRed), EgaPalette.GetRgba(EgaColorIndex.Red));

            const string title = "* GAME OVER - YOU DIED *";
            FrameBuffer.DrawText((320 - title.Length * 8) / 2, 56, title, EgaColorIndex.BrightRed);

            string playerLine = $"PLAYER: {PlayerName.ToUpperInvariant()}";
            FrameBuffer.DrawText((320 - playerLine.Length * 8) / 2, 72, playerLine, EgaColorIndex.BrightWhite);

            string cause = string.IsNullOrEmpty(DeathMessage) ? "OUT OF HEALTH!" : DeathMessage;
            FrameBuffer.DrawText((320 - cause.Length * 8) / 2, 88, cause, EgaColorIndex.Yellow);

            string scoreLine = $"FINAL SCORE: {Player.Score:D6}";
            FrameBuffer.DrawText((320 - scoreLine.Length * 8) / 2, 104, scoreLine, EgaColorIndex.BrightGreen);

            const string prompt = "PRESS ANY KEY TO RESPAWN";
            FrameBuffer.DrawText((320 - prompt.Length * 8) / 2, 126, prompt, EgaColorIndex.BrightCyan);
        }
        else if (IsPaused)
        {
            FrameBuffer.DrawBeveledBox(68, 71, 184, 40, EgaPalette.GetRgba(EgaColorIndex.Black),
                EgaPalette.GetRgba(EgaColorIndex.BrightWhite), EgaPalette.GetRgba(EgaColorIndex.DarkGray));
            const string pauseText = "* PAUSED *";
            FrameBuffer.DrawText((320 - pauseText.Length * 8) / 2, 80, pauseText, EgaColorIndex.Yellow);
            const string resumePrompt = "PRESS [P] TO RESUME";
            FrameBuffer.DrawText((320 - resumePrompt.Length * 8) / 2, 94, resumePrompt, EgaColorIndex.BrightCyan);
        }
    }
}
