using System;
using JumpRunShoot.Engine.Audio;
using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class MonsterDeathAndScoringTests
{
    [Fact]
    public void QuadraticMonsterScoring_CalculatesExactFormula_AndSpawnsWithCorrectRewards()
    {
        // Formula: 50 * maxHealth^2
        Assert.Equal(200, MonsterEntity.CalculateScoreReward(2));
        Assert.Equal(450, MonsterEntity.CalculateScoreReward(3));
        Assert.Equal(800, MonsterEntity.CalculateScoreReward(4));
        Assert.Equal(1250, MonsterEntity.CalculateScoreReward(5));
        Assert.Equal(1800, MonsterEntity.CalculateScoreReward(6));

        var m = new MonsterEntity();

        m.Spawn(0, MonsterType.PatrolDroid, new Vector2f(10, 10));
        Assert.Equal(2, m.MaxHealth);
        Assert.Equal(200, m.ScoreReward);

        m.Spawn(1, MonsterType.HoverDrone, new Vector2f(10, 10));
        Assert.Equal(2, m.MaxHealth);
        Assert.Equal(200, m.ScoreReward);

        m.Spawn(2, MonsterType.CeilingTurret, new Vector2f(10, 10));
        Assert.Equal(3, m.MaxHealth);
        Assert.Equal(450, m.ScoreReward);

        m.Spawn(3, MonsterType.MechaHound, new Vector2f(10, 10));
        Assert.Equal(3, m.MaxHealth);
        Assert.Equal(450, m.ScoreReward);

        m.Spawn(4, MonsterType.HeavyGuard, new Vector2f(10, 10));
        Assert.Equal(6, m.MaxHealth);
        Assert.Equal(1800, m.ScoreReward);
    }

    [Fact]
    public void QuadraticMonsterScoring_BonusHealthInDeepChunks_ScalesQuadratically()
    {
        var engine = new GameEngine("DEEP_CHUNK_SCORING");

        // Clear active monsters
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        // Simulate spawning a hound at chunk 6 (where bonusHealth would be chunkIndex / 2 = 3)
        // Base health 3 + bonus 3 = 6 HP -> 50 * 6^2 = 1800 pts
        var hound = engine.Monsters[0];
        hound.Spawn(0, MonsterType.MechaHound, new Vector2f(100, 100));
        hound.MaxHealth += 3;
        hound.Health = hound.MaxHealth;
        hound.ScoreReward = MonsterEntity.CalculateScoreReward(hound.MaxHealth);

        Assert.Equal(6, hound.MaxHealth);
        Assert.Equal(1800, hound.ScoreReward);
    }

    [Fact]
    public void DistanceScoring_IsSmallComparedToMonsterKills()
    {
        var engine = new GameEngine("DISTANCE_VS_COMBAT_TEST");
        engine.Player.Score = 0;
        float startX = engine.Player.Position.X;

        // Advance 1 full chunk (32 tiles = 512 pixels) to the right
        engine.Player.Position = new Vector2f(startX + (32 * PhysicsConstants.TileSize), engine.Player.Position.Y);
        engine.Update(0.016f);

        int chunkAdvancementScore = engine.Player.Score;
        // In chunk 0, 32 tiles * 1 pt = 32 points
        Assert.Equal(32, chunkAdvancementScore);

        // Killing even the lowest-tier 2-HP monster awards 200 pts (over 6x an entire chunk of walking)
        var droid = new MonsterEntity();
        droid.Spawn(0, MonsterType.PatrolDroid, new Vector2f(0, 0));
        Assert.True(droid.ScoreReward > chunkAdvancementScore * 5,
            $"Monster defeat ({droid.ScoreReward} pts) must greatly outweigh chunk distance advancement ({chunkAdvancementScore} pts)");

        // Killing a heavy guard (1800 pts) awards over 50x an entire chunk of walking
        var guard = new MonsterEntity();
        guard.Spawn(1, MonsterType.HeavyGuard, new Vector2f(0, 0));
        Assert.True(guard.ScoreReward > chunkAdvancementScore * 50,
            $"Heavy guard defeat ({guard.ScoreReward} pts) must dwarf chunk distance advancement ({chunkAdvancementScore} pts)");
    }

    [Fact]
    public void MonsterDeath_SpawnsDeathEffect_WithCorrectDurationAndBounds()
    {
        var engine = new GameEngine("DEATH_EFFECT_SPAWN");
        for (int i = 0; i < engine.DeathEffects.Length; i++) engine.DeathEffects[i].IsActive = false;

        var monster = engine.Monsters[0];
        monster.Spawn(0, MonsterType.MechaHound, new Vector2f(200, 120));

        engine.OnMonsterKilled(monster);

        // Verify death effect spawned
        bool found = false;
        for (int i = 0; i < engine.DeathEffects.Length; i++)
        {
            if (engine.DeathEffects[i].IsActive)
            {
                found = true;
                Assert.Equal(MonsterType.MechaHound, engine.DeathEffects[i].MonsterType);
                Assert.Equal(200f, engine.DeathEffects[i].Position.X);
                Assert.Equal(120f, engine.DeathEffects[i].Position.Y);
                Assert.Equal(0.45f, engine.DeathEffects[i].Duration);
                break;
            }
        }
        Assert.True(found, "Defeating a monster must spawn an active DeathEffect in the engine pool");
    }

    [Fact]
    public void MonsterDeath_HeavyGuardHasLongerDeathAnimationDuration()
    {
        var engine = new GameEngine("HEAVY_DEATH_EFFECT");
        for (int i = 0; i < engine.DeathEffects.Length; i++) engine.DeathEffects[i].IsActive = false;

        var guard = engine.Monsters[0];
        guard.Spawn(0, MonsterType.HeavyGuard, new Vector2f(300, 100));

        engine.OnMonsterKilled(guard);

        bool found = false;
        for (int i = 0; i < engine.DeathEffects.Length; i++)
        {
            if (engine.DeathEffects[i].IsActive && engine.DeathEffects[i].MonsterType == MonsterType.HeavyGuard)
            {
                found = true;
                Assert.Equal(0.60f, engine.DeathEffects[i].Duration);
                break;
            }
        }
        Assert.True(found, "HeavyGuard must have an extended death duration of 0.60s");
    }

    [Fact]
    public void MonsterDeath_EffectsUpdateAndExpireOverTime()
    {
        var engine = new GameEngine("EFFECT_EXPIRATION");
        for (int i = 0; i < engine.DeathEffects.Length; i++) engine.DeathEffects[i].IsActive = false;

        engine.SpawnDeathEffect(MonsterType.PatrolDroid, new Vector2f(50, 50), 14, 14, Direction.Left);

        // Advance 0.20s: effect should still be active and timer incremented
        engine.Update(0.20f);
        Assert.True(engine.DeathEffects[0].IsActive);
        Assert.True(engine.DeathEffects[0].Timer >= 0.19f);

        // Advance past total duration (0.45s): effect must deactivate
        engine.Update(0.35f);
        Assert.False(engine.DeathEffects[0].IsActive, "DeathEffect must deactivate once Timer exceeds Duration");
    }

    [Theory]
    [InlineData(MonsterType.PatrolDroid, AudioEvent.MonsterDiePatrolDroid)]
    [InlineData(MonsterType.HoverDrone, AudioEvent.MonsterDieHoverDrone)]
    [InlineData(MonsterType.CeilingTurret, AudioEvent.MonsterDieCeilingTurret)]
    [InlineData(MonsterType.MechaHound, AudioEvent.MonsterDieMechaHound)]
    [InlineData(MonsterType.HeavyGuard, AudioEvent.MonsterDieHeavyGuard)]
    public void MonsterDeath_AudioFlags_TriggerSpecificMonsterTypeFlagAlongWithMonsterDie(MonsterType type, AudioEvent expectedFlag)
    {
        var engine = new GameEngine("AUDIO_TEST_" + type);
        engine.ConsumeAudioFlags(); // clear startup flags

        var monster = new MonsterEntity();
        monster.Spawn(0, type, new Vector2f(100, 100));

        engine.OnMonsterKilled(monster);

        int flags = engine.ConsumeAudioFlags();

        // 1. Must include general MonsterDie flag for backwards compatibility
        Assert.True((flags & (int)AudioEvent.MonsterDie) != 0, "MonsterDie flag must be set for all defeated monsters");

        // 2. Must include specific monster type flag for unique sound dispatch
        Assert.True((flags & (int)expectedFlag) != 0, $"Audio flags must include {expectedFlag} for {type}");
    }

    [Fact]
    public void MonsterDeath_DrawDeathEffect_RendersWithoutErrorsAcrossAllThemes()
    {
        var fb = new FrameBuffer();

        var effect = new DeathEffect
        {
            IsActive = true,
            MonsterType = MonsterType.PatrolDroid,
            Position = new Vector2f(100, 100),
            Width = 14,
            Height = 14,
            Facing = Direction.Left,
            Timer = 0.20f,
            Duration = 0.45f
        };

        foreach (var theme in ThemeRegistry.AllThemes)
        {
            fb.Clear(EgaColorIndex.Black);

            // Test normal progress
            theme.DrawDeathEffect(fb, effect, 100, 100);

            // Verify some pixels were written to the buffer
            bool hasPixel = false;
            for (int p = 0; p < fb.Pixels.Length; p++)
            {
                if (fb.Pixels[p] != EgaPalette.GetRgba(EgaColorIndex.Black))
                {
                    hasPixel = true;
                    break;
                }
            }
            Assert.True(hasPixel, $"{theme.Name} must render visible pixels during death effect");

            // Test start flash phase
            effect.Timer = 0.05f;
            theme.DrawDeathEffect(fb, effect, 100, 100);

            // Test late dissipation phase
            effect.Timer = 0.40f;
            theme.DrawDeathEffect(fb, effect, 100, 100);

            // Test HeavyGuard large effect
            var heavyEffect = effect;
            heavyEffect.MonsterType = MonsterType.HeavyGuard;
            heavyEffect.Width = 14;
            heavyEffect.Height = 26;
            heavyEffect.Duration = 0.60f;
            heavyEffect.Timer = 0.30f;
            theme.DrawDeathEffect(fb, heavyEffect, 100, 100);
        }
    }

    [Fact]
    public void MonsterDeath_RenderStoryboardShowcase_SavesArtifact()
    {
        var fb = new FrameBuffer();
        fb.Clear(EgaColorIndex.Black);

        // Header Title
        fb.FillRect(0, 0, 320, 14, EgaColorIndex.DarkGray);
        fb.DrawHLine(0, 14, 320, EgaColorIndex.BrightWhite);
        fb.DrawText(12, 3, "MONSTER DEATH ANIMATIONS: THEME SHOWCASE", EgaColorIndex.Yellow);

        IGameTheme[] themes = [
            ThemeRegistry.AllThemes[0], // Duke
            ThemeRegistry.AllThemes[1], // Forest
            ThemeRegistry.AllThemes[2]  // Underwater
        ];

        string[] labels = [
            "ROBOT EXPLOSION (DUKE 1)",
            "WOODLAND ENCHANTED PUFF (FOREST)",
            "AQUATIC CAVITATION & BUBBLES (UNDERWATER)"
        ];

        float[] times = [0.04f, 0.12f, 0.22f, 0.34f, 0.44f];

        for (int row = 0; row < 3; row++)
        {
            int rowY = 18 + row * 60;
            fb.DrawText(8, rowY, labels[row], EgaColorIndex.BrightWhite);

            for (int col = 0; col < 5; col++)
            {
                int snapX = 38 + col * 60;
                int snapY = rowY + (row == 2 ? 30 : 20);

                // Sub-box border for animation frame
                fb.DrawRect(snapX - 22, snapY - (row == 2 ? 26 : 14), 44, 40, EgaColorIndex.DarkGray);
                fb.DrawText(snapX - 18, snapY + 16, $"t={times[col]:F2}s", EgaColorIndex.LightGray);

                var effect = new DeathEffect
                {
                    IsActive = true,
                    MonsterType = col == 4 ? MonsterType.HeavyGuard : MonsterType.PatrolDroid,
                    Position = new Vector2f(snapX, snapY),
                    Width = 14,
                    Height = col == 4 ? 26 : 14,
                    Facing = Direction.Left,
                    Timer = times[col],
                    Duration = col == 4 ? 0.60f : 0.45f
                };

                themes[row].DrawDeathEffect(fb, effect, snapX - 7, snapY - 7);
            }
        }

        SaveBmp("monster_death_showcase.bmp", fb);
        Assert.True(System.IO.File.Exists("monster_death_showcase.bmp"));
    }

    private static void SaveBmp(string path, FrameBuffer fb)
    {
        using var stream = new System.IO.FileStream(path, System.IO.FileMode.Create);
        using var writer = new System.IO.BinaryWriter(stream);

        int width = FrameBuffer.Width;
        int height = FrameBuffer.Height;
        int imageSize = width * height * 4;
        int fileSize = 54 + imageSize;

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((short)0);
        writer.Write((short)0);
        writer.Write(54);

        writer.Write(40);
        writer.Write(width);
        writer.Write(-height);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(0);
        writer.Write(imageSize);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        byte[] bgraBytes = new byte[imageSize];
        for (int i = 0; i < fb.Pixels.Length; i++)
        {
            uint c = fb.Pixels[i];
            bgraBytes[i * 4 + 0] = (byte)((c >> 16) & 0xFF); // Blue
            bgraBytes[i * 4 + 1] = (byte)((c >> 8) & 0xFF);  // Green
            bgraBytes[i * 4 + 2] = (byte)(c & 0xFF);         // Red
            bgraBytes[i * 4 + 3] = (byte)((c >> 24) & 0xFF); // Alpha
        }
        writer.Write(bgraBytes);
    }
}
