using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Entities;
using JumpRunShoot.Engine.Physics;
using JumpRunShoot.Engine.World;
using Xunit;

namespace JumpRunShoot.Tests;

public class CrateDestructionAndMonsterCollisionTests
{
    [Fact]
    public void HoverDrone_ChasingPlayerWhenShotNearWalls_NeverClipsIntoSolidBlocks()
    {
        var gen = new WorldGenerator("REINDEER_SOLID_CLIP");
        var grid = new TileGrid(gen);
        int ts = PhysicsConstants.TileSize;

        // Build a narrow room: solid walls at col 5 and col 9, floor at row 20, ceiling at row 14
        for (int c = 5; c <= 9; c++)
        {
            for (int r = 14; r <= 20; r++)
            {
                bool isBoundary = c == 5 || c == 9 || r == 14 || r == 20;
                grid.SetTile(c, r, isBoundary ? TileType.Solid : TileType.Empty);
            }
        }
        // Extra protruding solid block inside the room at (7, 17)
        grid.SetTile(7, 17, TileType.Solid);

        var reindeer = new MonsterEntity();
        reindeer.Spawn(0, MonsterType.HoverDrone, new Vector2f(6 * ts + 1, 17 * ts + 2));

        // Simulate player shooting reindeer from behind the solid wall (trying to make it chase into solid blocks)
        reindeer.TakeDamage(1, new Vector2f(11 * ts, 17 * ts));
        Assert.True(reindeer.IsActive);

        for (int f = 0; f < 180; f++)
        {
            reindeer.Update(0.016f, grid, new Vector2f(11 * ts, 17 * ts), out _, out _, out _, out _);

            int minCol = (int)MathF.Floor((reindeer.Position.X + 0.5f) / ts);
            int maxCol = (int)MathF.Floor((reindeer.Position.X + reindeer.Width - 0.5f) / ts);
            int minRow = (int)MathF.Floor((reindeer.Position.Y + 0.5f) / ts);
            int maxRow = (int)MathF.Floor((reindeer.Position.Y + reindeer.Height - 0.5f) / ts);

            for (int c = minCol; c <= maxCol; c++)
            {
                for (int r = minRow; r <= maxRow; r++)
                {
                    TileType t = grid.GetTile(c, r);
                    Assert.False(t is TileType.Solid or TileType.Border or TileType.Crate,
                        $"Frame {f}: Reindeer clipped into {t} at ({c},{r}) pos=({reindeer.Position.X:F1},{reindeer.Position.Y:F1})");
                }
            }
        }
    }

    [Fact]
    public void MechaHound_SprintingOnNarrowPlatform_NeverRunsIntoSolidWallDuringTurnCooldown()
    {
        var gen = new WorldGenerator("ELK_WALL_CLIP");
        var grid = new TileGrid(gen);
        int ts = PhysicsConstants.TileSize;

        // 3-tile wide corridor (cols 6..8 open) bounded by solid walls at col 5 and col 9, floor at row 20
        for (int c = 5; c <= 9; c++)
        {
            for (int r = 16; r <= 20; r++)
            {
                bool isSolid = r == 20 || c == 5 || c == 9;
                grid.SetTile(c, r, isSolid ? TileType.Solid : TileType.Empty);
            }
        }

        var elk = new MonsterEntity();
        elk.Spawn(0, MonsterType.MechaHound, new Vector2f(6 * ts + 2, 20 * ts - 14), platformY: 20 * ts);
        // Alert elk into high-speed sprint (125 px/s) toward right wall
        elk.TakeDamage(1, new Vector2f(12 * ts, 20 * ts - 14));

        for (int f = 0; f < 180; f++)
        {
            elk.Update(0.016f, grid, new Vector2f(12 * ts, 20 * ts - 14), out _, out _, out _, out _);

            int minCol = (int)MathF.Floor((elk.Position.X + 0.5f) / ts);
            int maxCol = (int)MathF.Floor((elk.Position.X + elk.Width - 0.5f) / ts);
            int minRow = (int)MathF.Floor((elk.Position.Y + 0.5f) / ts);
            int maxRow = (int)MathF.Floor((elk.Position.Y + elk.Height - 0.5f) / ts);

            for (int c = minCol; c <= maxCol; c++)
            {
                for (int r = minRow; r <= maxRow; r++)
                {
                    TileType t = grid.GetTile(c, r);
                    Assert.False(t is TileType.Solid or TileType.Border or TileType.Crate,
                        $"Frame {f}: Elk ran into {t} at ({c},{r}) pos=({elk.Position.X:F1},{elk.Position.Y:F1})");
                }
            }
        }
    }

    [Fact]
    public void PlayerBullet_HittingCrate_DestroysCrateAndSpawnsBreakAnimation_ButLeavesSolidIntact()
    {
        var engine = new GameEngine("CRATE_BREAK_TEST");
        engine.SetTheme(4); // Polar Adventure (gravity)
        int ts = PhysicsConstants.TileSize;

        for (int c = 4; c <= 12; c++)
        {
            for (int r = 15; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }
        engine.Grid.SetTile(8, 23, TileType.Crate);
        engine.Grid.SetTile(10, 23, TileType.Solid);

        engine.Player.Position = new Vector2f(4 * ts, 24 * ts - 24);
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;
        for (int i = 0; i < engine.DeathEffects.Length; i++) engine.DeathEffects[i].IsActive = false;

        // Shoot the crate at (8, 23)
        engine.SpawnProjectile(new Vector2f(7 * ts, 23 * ts + 6), new Vector2f(260f, 0f), damage: 1, fromPlayer: true);
        for (int f = 0; f < 10; f++) engine.Update(0.016f);

        Assert.Equal(TileType.Empty, engine.Grid.GetTile(8, 23));
        Assert.False(engine.Projectiles[0].IsActive);

        bool foundCrateAnim = false;
        for (int i = 0; i < engine.DeathEffects.Length; i++)
        {
            if (engine.DeathEffects[i].IsActive && engine.DeathEffects[i].IsCrateBreak)
            {
                foundCrateAnim = true;
                break;
            }
        }
        Assert.True(foundCrateAnim, "Destroying a crate should spawn an active crate-break animation effect.");

        // Shoot the Solid block at (10, 23): must NOT be destroyed
        engine.SpawnProjectile(new Vector2f(9 * ts, 23 * ts + 6), new Vector2f(260f, 0f), damage: 1, fromPlayer: true);
        for (int f = 0; f < 10; f++) engine.Update(0.016f);

        Assert.Equal(TileType.Solid, engine.Grid.GetTile(10, 23));
    }

    [Fact]
    public void DestroyCrate_InGravityTheme_DropsStackedCratesPowerupsAndFootMonstersOneCell()
    {
        var engine = new GameEngine("CRATE_GRAVITY_DROP");
        engine.SetTheme(4); // Polar Adventure (gravity enabled)
        int ts = PhysicsConstants.TileSize;

        for (int c = 4; c <= 12; c++)
        {
            for (int r = 15; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }

        // Stack of 2 crates at col 8 (rows 23 and 22)
        engine.Grid.SetTile(8, 23, TileType.Crate);
        engine.Grid.SetTile(8, 22, TileType.Crate);

        // Clear all default entities
        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;

        // Place a Powerup & a Foot Monster (Walrus / PatrolDroid) on top of the upper crate (row 22)
        float topSurfaceY = 22 * ts;
        engine.Pickups[0].Spawn(PickupType.RapidFire, new Vector2f(8 * ts + 2, topSurfaceY - 14));
        engine.Monsters[0].Spawn(0, MonsterType.PatrolDroid, new Vector2f(8 * ts + 1, topSurfaceY - 14), platformY: topSurfaceY);
        engine.Monsters[0].Velocity = Vector2f.Zero;

        // Also place a flying HoverDrone above: must NOT drop
        engine.Monsters[1].Spawn(1, MonsterType.HoverDrone, new Vector2f(8 * ts + 1, 18 * ts), platformY: 0);
        float hoverStartY = engine.Monsters[1].Position.Y;

        engine.Player.Position = new Vector2f(4 * ts, 24 * ts - 24);

        // Shoot bottom crate at (8, 23)
        engine.SpawnProjectile(new Vector2f(7 * ts + 8, 23 * ts + 6), new Vector2f(260f, 0f), damage: 1, fromPlayer: true);
        for (int f = 0; f < 5; f++) engine.Update(0.016f);

        // Upper crate moved from row 22 -> row 23; row 22 is now Empty
        Assert.Equal(TileType.Crate, engine.Grid.GetTile(8, 23));
        Assert.Equal(TileType.Empty, engine.Grid.GetTile(8, 22));

        // Powerup fell 1 cell down (+16px)
        Assert.Equal(topSurfaceY - 14 + ts, engine.Pickups[0].Position.Y, tolerance: 0.5f);

        // Foot monster fell 1 cell down (+16px) along with its PlatformY
        Assert.Equal(topSurfaceY + ts, engine.Monsters[0].PlatformY, tolerance: 0.5f);
        Assert.Equal(topSurfaceY + ts - engine.Monsters[0].Height, engine.Monsters[0].Position.Y, tolerance: 0.5f);

        // HoverDrone did NOT fall by a cell
        Assert.True(MathF.Abs(engine.Monsters[1].Position.Y - hoverStartY) < 4.0f);
    }

    [Fact]
    public void DestroyCrate_InZeroGravityTheme_DestroysCrateWithoutDroppingObjectsAbove()
    {
        var engine = new GameEngine("CRATE_ZEROG_TEST");
        engine.SetTheme(3); // Space Invaders (ZeroGravity = true)
        int ts = PhysicsConstants.TileSize;

        for (int c = 4; c <= 12; c++)
        {
            for (int r = 15; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
            engine.Grid.SetTile(c, 24, TileType.Solid);
        }

        // Stack of 2 crates at col 8 (rows 23 and 22)
        engine.Grid.SetTile(8, 23, TileType.Crate);
        engine.Grid.SetTile(8, 22, TileType.Crate);

        for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
        for (int i = 0; i < engine.Pickups.Length; i++) engine.Pickups[i].IsActive = false;
        for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;

        float topSurfaceY = 22 * ts;
        engine.Pickups[0].Spawn(PickupType.PiercingPlasma, new Vector2f(8 * ts + 2, topSurfaceY - 14));
        engine.Monsters[0].Spawn(0, MonsterType.PatrolDroid, new Vector2f(8 * ts + 1, topSurfaceY - 14), platformY: topSurfaceY);
        engine.Monsters[0].Velocity = Vector2f.Zero;

        engine.Player.Position = new Vector2f(4 * ts, 24 * ts - 24);

        // Shoot bottom crate at (8, 23)
        engine.SpawnProjectile(new Vector2f(7 * ts + 8, 23 * ts + 6), new Vector2f(260f, 0f), damage: 1, fromPlayer: true);
        for (int f = 0; f < 5; f++) engine.Update(0.016f);

        // Bottom crate destroyed, but upper crate, powerup, and monster stay floating at their exact original positions!
        Assert.Equal(TileType.Empty, engine.Grid.GetTile(8, 23));
        Assert.Equal(TileType.Crate, engine.Grid.GetTile(8, 22));
        Assert.Equal(topSurfaceY - 14, engine.Pickups[0].Position.Y, tolerance: 0.5f);
        Assert.Equal(topSurfaceY, engine.Monsters[0].PlatformY, tolerance: 0.5f);
    }

    [Fact]
    public void HeavyGuard_TwoCellTallMonster_TakesDamageFromSameLevelAndOneCellHigherPlatform()
    {
        int ts = PhysicsConstants.TileSize;
        float bearFloorY = 24 * ts; // Row 24 floor: bear occupies rows 22 & 23 ([352..384])

        foreach (int playerRowOffset in new[] { 0, -1 })
        {
            var engine = new GameEngine("TWO_TALL_HITBOX_TEST");
            engine.SetTheme(4); // Polar Adventure

            for (int c = 3; c <= 15; c++)
            {
                for (int r = 16; r < 24; r++) engine.Grid.SetTile(c, r, TileType.Empty);
                engine.Grid.SetTile(c, 24, TileType.Solid);
            }
            if (playerRowOffset == -1)
            {
                // 1-cell-higher platform at row 23 for player to stand on
                engine.Grid.SetTile(5, 23, TileType.Solid);
                engine.Grid.SetTile(6, 23, TileType.Solid);
            }

            for (int i = 0; i < engine.Monsters.Length; i++) engine.Monsters[i].IsActive = false;
            for (int i = 0; i < engine.Projectiles.Length; i++) engine.Projectiles[i].IsActive = false;

            float playerFloorY = (24 + playerRowOffset) * ts;
            engine.Player.Position = new Vector2f(5 * ts, playerFloorY - 24f);
            engine.Player.IsGrounded = true;
            engine.Player.Facing = Direction.Right;
            engine.Player.PendingShoot = true;

            var bear = engine.Monsters[0];
            bear.Spawn(0, MonsterType.HeavyGuard, new Vector2f(10 * ts, bearFloorY - 26f), platformY: bearFloorY);
            bear.Velocity = Vector2f.Zero;
            bear.ActionTimer = 99f;
            int initialHp = bear.Health;

            for (int f = 0; f < 30; f++) engine.Update(0.016f);

            Assert.True(bear.Health < initialHp,
                $"Shooting at 2-cell-tall bear from platform offset {playerRowOffset} must register damage (HP remained {bear.Health}/{initialHp}).");
        }
    }

    [Fact]
    public void WorldGenerator_GeneratesAbundantShootableCratesAcrossChunks()
    {
        var gen = new WorldGenerator("CRATE_ABUNDANCE_SEED");
        int totalCrates = 0;
        int chunksWithCrates = 0;

        for (int c = 1; c <= 20; c++)
        {
            var chunk = gen.GenerateChunk(c);
            int chunkCrates = 0;
            for (int i = 0; i < TileChunk.TotalTiles; i++)
            {
                if (chunk.Tiles[i] == TileType.Crate) chunkCrates++;
            }
            totalCrates += chunkCrates;
            if (chunkCrates > 0) chunksWithCrates++;
        }

        Assert.True(chunksWithCrates >= 18, $"Expected shootable crates in almost every chunk, got {chunksWithCrates}/20.");
        Assert.True(totalCrates >= 120, $"Expected abundant shootable crates across 20 chunks (>= 120), got {totalCrates}.");
    }
}
