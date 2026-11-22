using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Input;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.Themes;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class CrouchAndPolishTests
{
    [Fact]
    public void Crouch_GravityTheme_EntersTransitionsAndStandsBackUpWhenClear()
    {
        var engine = new GameEngine("CROUCH_BASIC");
        engine.SetTheme(4); // Polar Adventure (gravity)
        int ts = PhysicsConstants.TileSize;

        for (int c = 4; c <= 12; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        engine.Player.Position = new Vector2f(6 * ts, 24 * ts - 24f);
        engine.Player.IsGrounded = true;

        // Hold Down -> enters crouch
        engine.Input.HandleKeyDown("ArrowDown");
        engine.Update(0.016f);

        Assert.True(engine.Player.IsCrouching);
        Assert.Equal(PlayerState.Crouching, engine.Player.State);
        Assert.Equal(PlayerEntity.CrouchBoxHeight, engine.Player.BoundingBox.Height);
        Assert.True(engine.Player.CrouchTimer < PlayerEntity.CrouchTransitionDuration);

        // Hold Down past transition duration -> frozen crouched pose
        for (int f = 0; f < 12; f++) engine.Update(0.016f);
        Assert.True(engine.Player.IsCrouching);
        Assert.True(engine.Player.CrouchTimer >= PlayerEntity.CrouchTransitionDuration);

        // Release Down in open space -> stands back up to full 2-cell height
        engine.Input.HandleKeyUp("ArrowDown");
        engine.Update(0.016f);

        Assert.False(engine.Player.IsCrouching);
        Assert.Equal(PlayerEntity.BoxHeight, engine.Player.BoundingBox.Height);
    }

    [Fact]
    public void Crouch_UnderOneCellTunnel_StaysCrouchedAfterReleasingDown_UntilEmerging()
    {
        var engine = new GameEngine("CROUCH_TUNNEL");
        engine.SetTheme(4); // Polar Adventure
        int ts = PhysicsConstants.TileSize;

        // Clear cols 3..14, floor at row 24, 1-cell-high tunnel roof at row 22 across cols 7..9
        for (int c = 3; c <= 14; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int c = 7; c <= 9; c++) engine.Grid.SetTile(c, 22, TileType.Solid);
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;

        engine.Player.Position = new Vector2f(6 * ts, 24 * ts - 24f);
        engine.Player.IsGrounded = true;

        // Crouch and crawl right into the 1-cell tunnel (under col 8, row 22 roof)
        engine.Input.HandleKeyDown("ArrowDown");
        engine.Input.HandleKeyDown("ArrowRight");
        for (int f = 0; f < 35; f++) engine.Update(0.016f);

        Assert.True(engine.Player.Position.X > 7 * ts, "Crouched player should slide cleanly into 1-cell tunnel");

        // Release Down while STILL underneath the 1-cell roof: must stay crouched!
        engine.Input.HandleKeyUp("ArrowDown");
        engine.Input.HandleKeyUp("ArrowRight");
        engine.Update(0.016f);
        Assert.True(engine.Player.IsCrouching, "Player must stay crouched while upper cell is blocked by tunnel roof");

        // Continue moving right until past col 9 -> automatically stands back up!
        engine.Input.HandleKeyDown("ArrowRight");
        for (int f = 0; f < 50; f++) engine.Update(0.016f);
        engine.Input.HandleKeyUp("ArrowRight");
        engine.Update(0.016f);

        Assert.True(engine.Player.Position.X >= 10 * ts);
        Assert.False(engine.Player.IsCrouching, "Player should automatically stand up after exiting low tunnel");
    }

    [Fact]
    public void Crouch_ThreeSimultaneousKeys_CrouchMoveAndShootAllWorkTogether()
    {
        var engine = new GameEngine("CROUCH_3KEYS");
        engine.SetTheme(4); // Polar Adventure
        int ts = PhysicsConstants.TileSize;

        for (int c = 3; c <= 14; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int c = 7; c <= 10; c++) engine.Grid.SetTile(c, 22, TileType.Solid);
        engine.Grid.SetTile(9, 23, TileType.Crate); // Shootable box blocking the 1-cell tunnel!

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;

        engine.Player.Position = new Vector2f(6 * ts, 24 * ts - 24f);
        engine.Player.IsGrounded = true;
        float startX = engine.Player.Position.X;

        // Press all 3 keys simultaneously: Down (crouch) + Right (move) + KeyX (shoot)
        engine.Input.HandleKeyDown("ArrowDown");
        engine.Input.HandleKeyDown("ArrowRight");
        engine.Input.HandleKeyDown("KeyX");

        for (int f = 0; f < 25; f++) engine.Update(0.016f);

        Assert.True(engine.Player.IsCrouching, "Player must stay crouched while moving and shooting");
        Assert.True(engine.Player.Position.X > startX + 8f, "Crouched player must move right while holding Down + Right + Shoot");
        Assert.Equal(TileType.Empty, engine.Grid.GetTile(9, 23));
    }

    [Fact]
    public void Crouch_DodgesHeavyGuardTwoCellMonsterBullet_OnSamePlatform()
    {
        var engine = new GameEngine("CROUCH_DODGE_BEAR");
        engine.SetTheme(4); // Polar Adventure (gravity)
        int ts = PhysicsConstants.TileSize;
        float platformTop = 24 * ts;

        for (int c = 3; c <= 14; c++)
        {
            for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;

        engine.Player.Position = new Vector2f(5 * ts, platformTop - 24f);
        engine.Player.IsGrounded = true;
        engine.Player.Health = 8;
        engine.Input.HandleKeyDown("ArrowDown"); // Crouch!

        var bear = engine.Monsters[0];
        bear.Spawn(0, MonsterType.HeavyGuard, new Vector2f(10 * ts, platformTop - 26f), platformY: platformTop);
        bear.Facing = Direction.Left;
        bear.Velocity = Vector2f.Zero;
        bear.ActionTimer = 0.001f;

        // Let bear shoot and let bullet travel past crouched player's position
        for (int f = 0; f < 45; f++) engine.Update(0.016f);

        Assert.True(engine.Player.IsCrouching);
        Assert.Equal(8, engine.Player.Health);
    }

    [Theory]
    [InlineData(2)] // Ocean (ZeroGravity)
    [InlineData(3)] // Space Invaders (ZeroGravity)
    public void Crouch_IsDisabledInZeroGravityThemes(int themeIdx)
    {
        var engine = new GameEngine("NO_CROUCH_ZEROG");
        engine.SetTheme(themeIdx);
        engine.Player.IsGrounded = true;

        engine.Input.HandleKeyDown("ArrowDown");
        for (int f = 0; f < 5; f++) engine.Update(0.016f);

        Assert.False(engine.Player.IsCrouching, $"Crouch must be disabled in zero-gravity theme {themeIdx}");
    }

    [Fact]
    public void ClimbingLadder_AnimTimerAdvancesOnlyWhenPressingUpOrDown()
    {
        var gen = new WorldGenerator("CLIMB_PAUSE_TEST");
        var grid = new TileGrid(gen);
        var input = new InputManager();

        for (int r = 10; r <= 20; r++) grid.SetTile(5, r, TileType.Ladder);
        var player = new PlayerEntity(new Vector2f(5 * 16, 15 * 16));
        player.IsGrounded = false;

        // Climb up -> ClimbAnimTimer advances
        input.HandleKeyDown("ArrowUp");
        input.AdvanceFrame();
        player.Update(0.1f, input, grid);
        Assert.True(player.IsClimbing);
        float afterMove = player.ClimbAnimTimer;
        Assert.True(afterMove > 0.0f);

        // Release Up -> holding still on ladder must NOT advance ClimbAnimTimer!
        input.HandleKeyUp("ArrowUp");
        input.AdvanceFrame();
        player.Update(0.25f, input, grid);
        Assert.True(player.IsClimbing);
        Assert.Equal(afterMove, player.ClimbAnimTimer);
    }

    [Fact]
    public void GravityThemes_LoadCrouchSprites_AndPolarLoadsShootFrame()
    {
        foreach (int idx in new[] { 0, 1, 4 }) // Duke, Forest, Polar
        {
            var assets = ThemeRegistry.AllThemes[idx].Assets;
            Assert.NotNull(assets.PlayerCrouch);
            Assert.Equal(2, assets.PlayerCrouch!.Length);
        }

        var polar = new PolarAdventureTheme();
        Assert.NotNull(polar.Assets.PlayerShoot);

        var engine = new GameEngine("SHOOT_ANIM_TEST");
        engine.SetTheme(4);
        engine.Player.PendingShoot = true;
        engine.Update(0.016f);
        Assert.True(engine.Player.ShootAnimTimer > 0.0f);
    }

    [Fact]
    public void MechaHound_Elk_NeverSpawnsEmbeddedInSolidBlockAcrossSeeds()
    {
        var seeds = new[] { "FOO", "DEMO", "ELK_SAFE_1", "POLAR_ICE_88", "OUTPOST_ALPHA", "SUPPLY_007" };
        int ts = PhysicsConstants.TileSize;

        foreach (var seed in seeds)
        {
            var engine = new GameEngine(seed);
            engine.SetTheme(4);
            for (int c = 0; c <= 20; c++) engine.PopulateChunkEntities(c);

            for (int m = 0; m < engine.Monsters.Length; m++)
            {
                var monster = engine.Monsters[m];
                if (!monster.IsActive || monster.Type != MonsterType.MechaHound) continue;

                int minCol = (int)MathF.Floor((monster.Position.X + 0.5f) / ts);
                int maxCol = (int)MathF.Floor((monster.Position.X + monster.Width - 0.5f) / ts);
                int row = (int)MathF.Floor((monster.Position.Y + monster.Height * 0.5f) / ts);

                for (int c = minCol; c <= maxCol; c++)
                {
                    TileType t = engine.Grid.GetTile(c, row);
                    Assert.False(t is TileType.Solid or TileType.Border or TileType.Crate,
                        $"Seed '{seed}': Elk spawned embedded in {t} at ({c},{row})!");
                }
            }
        }
    }

    [Fact]
    public void WorldGenerator_GeneratesCrouchCrawlwaysWithOpenBypassAbove()
    {
        var gen = new WorldGenerator("CROUCH_LEVEL_GEN");
        int crouchTunnelColumns = 0;

        for (int chunkIdx = 1; chunkIdx <= 30; chunkIdx++)
        {
            var chunk = gen.GenerateChunk(chunkIdx);
            for (int c = 2; c < TileChunk.ChunkWidth - 2; c++)
            {
                // Find bedrock floor top
                int floorRow = TileChunk.ChunkHeight - 1;
                while (floorRow > 0 && chunk.GetTile(c, floorRow - 1) == TileType.Solid) floorRow--;

                if (floorRow >= 5 &&
                    chunk.GetTile(c, floorRow - 1) is TileType.Empty or TileType.Crate &&
                    chunk.GetTile(c, floorRow - 2) == TileType.Solid &&
                    chunk.GetTile(c, floorRow - 3) != TileType.Solid &&
                    chunk.GetTile(c, floorRow - 4) != TileType.Solid)
                {
                    crouchTunnelColumns++;
                }
            }
        }

        Assert.True(crouchTunnelColumns >= 12, $"Expected intentional crouch crawlways across 30 chunks, found only {crouchTunnelColumns} tunnel tiles.");
    }
}
