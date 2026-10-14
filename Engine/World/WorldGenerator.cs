using System;

namespace JumpRunShoot.Engine.World;

public sealed class WorldGenerator
{
    public string Seed { get; private set; } = "RETRO1";
    private uint _numericSeed;

    public WorldGenerator(string seed = "RETRO1")
    {
        SetSeed(seed);
    }

    public void SetSeed(string seed)
    {
        Seed = string.IsNullOrWhiteSpace(seed) ? "RETRO1" : seed.Trim();
        var rng = new DeterministicRandom(Seed);
        _numericSeed = rng.NextUInt();
    }

    public TileChunk GenerateChunk(int chunkIndex)
    {
        var chunk = new TileChunk(chunkIndex);

        if (chunkIndex == 0)
        {
            GenerateStartingChunk(chunk);
            return chunk;
        }

        if (chunkIndex < 0)
        {
            GenerateLeftBoundary(chunk);
            return chunk;
        }

        // Deterministic generation for this chunk
        uint chunkSeed = _numericSeed ^ ((uint)chunkIndex * 2654435761u);
        var rng = new DeterministicRandom(chunkSeed);

        // Ground foundation layer (rows 28-31)
        bool hasPit = rng.NextBool(0.20f);
        int pitStart = hasPit ? rng.NextInt(3, 11) : -1;
        int pitWidth = hasPit ? rng.NextInt(2, 4) : 0;

        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            if (c >= pitStart && c < pitStart + pitWidth)
            {
                // Spikes in the pit floor
                chunk.SetTile(c, 31, TileType.Hazard);
            }
            else
            {
                chunk.SetTile(c, 28, TileType.Solid);
                chunk.SetTile(c, 29, TileType.Solid);
                chunk.SetTile(c, 30, TileType.Solid);
                chunk.SetTile(c, 31, TileType.Solid);
            }
        }

        // Select architectural archetype based on chunk seed
        int archetype = (int)(chunkSeed % 5);

        switch (archetype)
        {
            case 0:
                BuildTechBunker(chunk, rng);
                break;
            case 1:
                BuildCrateDepot(chunk, rng);
                break;
            case 2:
                BuildChasmGantry(chunk, rng);
                break;
            case 3:
                BuildElevatorTower(chunk, rng);
                break;
            case 4:
            default:
                BuildSplitPathFactory(chunk, rng);
                break;
        }

        return chunk;
    }

    // Archetype 0: 2-Story Enclosed Tech Bunker
    private static void BuildTechBunker(TileChunk chunk, DeterministicRandom rng)
    {
        // Bunker outer walls (cols 2 and 13, rows 16 to 25) with 2-tile high doorway at rows 26-27
        for (int r = 16; r < 26; r++)
        {
            chunk.SetTile(2, r, TileType.Solid);
            chunk.SetTile(13, r, TileType.Solid);
        }

        // Interior mid-floor at row 22 with one-way hatch
        GenerateLedge(chunk, startCol: 3, row: 22, len: 10, TileType.OneWayPlatform);

        // Bunker rooftop at row 16
        GenerateLedge(chunk, startCol: 1, row: 16, len: 14, TileType.Solid);
        chunk.SetTile(7, 16, TileType.OneWayPlatform); // Roof access hatch

        // Internal climbing ladder from ground floor up to roof
        PlaceLadder(chunk, col: 7, topRow: 16, bottomRow: 28);

        // Rooftop antenna and upper gantry (row 10)
        GenerateLedge(chunk, startCol: 4, row: 10, len: 7, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 5, topRow: 10, bottomRow: 16);

        // Outside stepping ledge at row 25
        GenerateLedge(chunk, startCol: 0, row: 25, len: 2, TileType.OneWayPlatform);

        // Rooftop high rafter at row 4
        GenerateLedge(chunk, startCol: 5, row: 4, len: 6, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 8, topRow: 4, bottomRow: 10);
    }

    // Archetype 1: Crate Depot with Stepped Cargo Pyramids
    private static void BuildCrateDepot(TileChunk chunk, DeterministicRandom rng)
    {
        // Ground crate pyramid forming natural stairs: 1-high, 2-high, 3-high
        chunk.SetTile(2, 27, TileType.Crate);
        chunk.SetTile(3, 26, TileType.Crate);
        chunk.SetTile(3, 27, TileType.Crate);
        chunk.SetTile(4, 25, TileType.Crate);
        chunk.SetTile(4, 26, TileType.Crate);
        chunk.SetTile(4, 27, TileType.Crate);

        // Tier 1 Catwalk at row 22
        GenerateLedge(chunk, startCol: 4, row: 22, len: 8, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 6, topRow: 22, bottomRow: 28);

        // Second crate stack on Tier 1 leading to Tier 2
        chunk.SetTile(9, 21, TileType.Crate);
        chunk.SetTile(10, 20, TileType.Crate);
        chunk.SetTile(10, 21, TileType.Crate);
        chunk.SetTile(11, 19, TileType.Crate);
        chunk.SetTile(11, 20, TileType.Crate);
        chunk.SetTile(11, 21, TileType.Crate);

        // Tier 2 Catwalk at row 16
        GenerateLedge(chunk, startCol: 4, row: 16, len: 8, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 8, topRow: 16, bottomRow: 22);

        // Tier 3 Deck at row 10
        GenerateLedge(chunk, startCol: 3, row: 10, len: 7, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 5, topRow: 10, bottomRow: 16);

        // Stepped crate at row 13
        chunk.SetTile(12, 13, TileType.Crate);

        // Tier 4 Rooftop at row 4
        GenerateLedge(chunk, startCol: 5, row: 4, len: 7, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 7, topRow: 4, bottomRow: 10);
    }

    // Archetype 2: Deep Hazard Chasm with Gantry Bridge
    private static void BuildChasmGantry(TileChunk chunk, DeterministicRandom rng)
    {
        // Deep chasm across center
        for (int c = 4; c <= 11; c++)
        {
            chunk.SetTile(c, 28, TileType.Empty);
            chunk.SetTile(c, 29, TileType.Empty);
            chunk.SetTile(c, 30, TileType.Empty);
            chunk.SetTile(c, 31, TileType.Hazard); // Spikes on chasm floor
        }

        // Suspended gantry bridge spanning across the chasm at row 22
        GenerateLedge(chunk, startCol: 2, row: 22, len: 12, TileType.OneWayPlatform);

        // Emergency descent ladder into chasm
        PlaceLadder(chunk, col: 5, topRow: 22, bottomRow: 30);

        // Stepping ledges flanking the chasm
        GenerateLedge(chunk, startCol: 0, row: 25, len: 3, TileType.OneWayPlatform);
        GenerateLedge(chunk, startCol: 13, row: 25, len: 3, TileType.OneWayPlatform);

        // Tier 2 Observation balcony at row 16
        GenerateLedge(chunk, startCol: 4, row: 16, len: 8, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 9, topRow: 16, bottomRow: 22);

        // Tier 3 Upper deck at row 10
        GenerateLedge(chunk, startCol: 3, row: 10, len: 7, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 6, topRow: 10, bottomRow: 16);

        // Tier 4 High rafters at row 4
        GenerateLedge(chunk, startCol: 5, row: 4, len: 6, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 8, topRow: 4, bottomRow: 10);
    }

    // Archetype 3: Vertical Elevator / Tower Shaft
    private static void BuildElevatorTower(TileChunk chunk, DeterministicRandom rng)
    {
        // Structural tower walls at cols 6 and 10 (rows 4 to 25, leaving rows 26-27 open as ground entrances)
        for (int r = 4; r < 26; r++)
        {
            // Doorway openings at each deck
            if (r != 21 && r != 22 && r != 15 && r != 16 && r != 9 && r != 10)
            {
                chunk.SetTile(6, r, TileType.Solid);
                chunk.SetTile(10, r, TileType.Solid);
            }
        }

        // Continuous climbing ladder through the center of the tower shaft (rows 4 to 28)
        PlaceLadder(chunk, col: 8, topRow: 4, bottomRow: 28);

        // Left & Right Balconies branching off the tower
        // Tier 1 Balconies (row 22)
        GenerateLedge(chunk, startCol: 1, row: 22, len: 5, TileType.OneWayPlatform);
        GenerateLedge(chunk, startCol: 11, row: 22, len: 5, TileType.OneWayPlatform);

        // Stepping ledges at row 25
        GenerateLedge(chunk, startCol: 2, row: 25, len: 3, TileType.OneWayPlatform);

        // Tier 2 Balconies (row 16)
        GenerateLedge(chunk, startCol: 1, row: 16, len: 5, TileType.OneWayPlatform);
        GenerateLedge(chunk, startCol: 11, row: 16, len: 5, TileType.OneWayPlatform);

        // Tier 3 Balconies (row 10)
        GenerateLedge(chunk, startCol: 2, row: 10, len: 4, TileType.OneWayPlatform);
        GenerateLedge(chunk, startCol: 11, row: 10, len: 4, TileType.OneWayPlatform);

        // Tier 4 Rooftop Spire (row 4)
        GenerateLedge(chunk, startCol: 4, row: 4, len: 9, TileType.Solid);
    }

    // Archetype 4: Split-Path Factory Corridor
    private static void BuildSplitPathFactory(TileChunk chunk, DeterministicRandom rng)
    {
        // Lower enclosed security corridor: solid ceiling at row 24 (cols 2 to 14)
        for (int c = 2; c <= 13; c++)
        {
            chunk.SetTile(c, 24, TileType.Solid);
        }

        // One-way access hatch in lower corridor ceiling
        chunk.SetTile(5, 24, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 5, topRow: 22, bottomRow: 28);

        // Mid-deck outdoor gantry at row 18
        GenerateLedge(chunk, startCol: 3, row: 18, len: 9, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 9, topRow: 18, bottomRow: 24);

        // Crate stack on gantry
        chunk.SetTile(10, 17, TileType.Crate);

        // Upper factory floor at row 12
        GenerateLedge(chunk, startCol: 2, row: 12, len: 10, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 6, topRow: 12, bottomRow: 18);

        // Rooftop rafters at row 5
        GenerateLedge(chunk, startCol: 4, row: 5, len: 8, TileType.OneWayPlatform);
        PlaceLadder(chunk, col: 8, topRow: 5, bottomRow: 12);
    }

    private static void GenerateLedge(TileChunk chunk, int startCol, int row, int len, TileType type)
    {
        for (int c = startCol; c < Math.Min(TileChunk.ChunkWidth, startCol + len); c++)
        {
            // Do not overwrite existing ladders
            if (chunk.GetTile(c, row) != TileType.Ladder)
            {
                chunk.SetTile(c, row, type);
            }
        }
    }

    private static void PlaceLadder(TileChunk chunk, int col, int topRow, int bottomRow)
    {
        if ((uint)col >= TileChunk.ChunkWidth) return;
        for (int r = topRow; r <= bottomRow; r++)
        {
            // Keep solid blocks solid, turn empty/oneway space into climbable ladder
            if (chunk.GetTile(col, r) != TileType.Solid)
            {
                chunk.SetTile(col, r, TileType.Ladder);
            }
        }
    }

    private void GenerateStartingChunk(TileChunk chunk)
    {
        var rng = new DeterministicRandom(_numericSeed ^ 0x5A17C0DEu);

        // 1. Continuous solid ground floor across chunk 0 (no hazardous pits in starter zone)
        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            chunk.SetTile(c, 28, TileType.Solid);
            chunk.SetTile(c, 29, TileType.Solid);
            chunk.SetTile(c, 30, TileType.Solid);
            chunk.SetTile(c, 31, TileType.Solid);
        }

        // Left perimeter boundary wall
        for (int r = 0; r < 28; r++)
        {
            chunk.SetTile(0, r, TileType.Solid);
        }

        // Safe spawn landing area (cols 1..3, rows 25..27)
        for (int r = 25; r < 28; r++)
        {
            for (int c = 1; c <= 3; c++)
            {
                chunk.SetTile(c, r, TileType.Empty);
            }
        }

        // Seed-driven Starting Room Variant (determines initial visible room layout)
        int startVariant = (int)(_numericSeed % 4);

        switch (startVariant)
        {
            case 0:
                // Variant 0: Stepped Crate Depot with Catwalks
                int cCol = rng.NextInt(3, 5);
                chunk.SetTile(cCol, 27, TileType.Crate);
                chunk.SetTile(cCol + 1, 26, TileType.Crate);
                chunk.SetTile(cCol + 1, 27, TileType.Crate);
                chunk.SetTile(cCol + 2, 25, TileType.Crate);
                chunk.SetTile(cCol + 2, 26, TileType.Crate);
                chunk.SetTile(cCol + 2, 27, TileType.Crate);

                int t1Col = rng.NextInt(5, 7);
                int t1Len = rng.NextInt(6, 8);
                GenerateLedge(chunk, startCol: t1Col, row: 22, len: t1Len, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: t1Col + 1, topRow: 22, bottomRow: 28);

                int t2Col = rng.NextInt(4, 6);
                int t2Len = rng.NextInt(6, 8);
                GenerateLedge(chunk, startCol: t2Col, row: 16, len: t2Len, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: t2Col + t2Len - 2, topRow: 16, bottomRow: 22);

                int t3Col = rng.NextInt(5, 7);
                GenerateLedge(chunk, startCol: t3Col, row: 10, len: 6, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: t3Col + 1, topRow: 10, bottomRow: 16);

                GenerateLedge(chunk, startCol: 5, row: 4, len: 8, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 9, topRow: 4, bottomRow: 10);
                break;

            case 1:
                // Variant 1: Tech Outpost with Central Structure
                chunk.SetTile(4, 27, TileType.Crate);
                chunk.SetTile(4, 26, TileType.Crate);

                // Lower Security Catwalk (row 23)
                GenerateLedge(chunk, startCol: 5, row: 23, len: 8, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 6, topRow: 23, bottomRow: 28);

                // Tech Enclosure Outer Wall at col 13 (doorway at rows 26-27)
                for (int r = 17; r < 26; r++) chunk.SetTile(13, r, TileType.Solid);

                // Mid-tier Deck at row 17
                GenerateLedge(chunk, startCol: 4, row: 17, len: 10, TileType.Solid);
                chunk.SetTile(8, 17, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 8, topRow: 17, bottomRow: 23);

                // High Observation Deck at row 11
                GenerateLedge(chunk, startCol: 5, row: 11, len: 7, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 6, topRow: 11, bottomRow: 17);

                // Rooftop Gantry at row 5
                GenerateLedge(chunk, startCol: 6, row: 5, len: 7, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 9, topRow: 5, bottomRow: 11);
                break;

            case 2:
                // Variant 2: Industrial Elevator Shaft with Symmetrical Balconies
                chunk.SetTile(4, 27, TileType.Crate);

                // Central climbing shaft (ladder rows 4 to 28 at col 8)
                PlaceLadder(chunk, col: 8, topRow: 4, bottomRow: 28);

                // Stepping ledges
                GenerateLedge(chunk, startCol: 4, row: 22, len: 4, TileType.OneWayPlatform);
                GenerateLedge(chunk, startCol: 9, row: 22, len: 5, TileType.OneWayPlatform);

                GenerateLedge(chunk, startCol: 3, row: 16, len: 5, TileType.OneWayPlatform);
                GenerateLedge(chunk, startCol: 9, row: 16, len: 5, TileType.OneWayPlatform);

                GenerateLedge(chunk, startCol: 4, row: 10, len: 4, TileType.OneWayPlatform);
                GenerateLedge(chunk, startCol: 9, row: 10, len: 4, TileType.OneWayPlatform);

                GenerateLedge(chunk, startCol: 5, row: 4, len: 7, TileType.Solid);
                break;

            default:
                // Variant 3: Stepped Industrial Catwalks & Pyramids
                chunk.SetTile(3, 27, TileType.Crate);
                chunk.SetTile(4, 26, TileType.Crate);
                chunk.SetTile(4, 27, TileType.Crate);

                GenerateLedge(chunk, startCol: 5, row: 22, len: 8, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 6, topRow: 22, bottomRow: 28);

                chunk.SetTile(11, 21, TileType.Crate);
                chunk.SetTile(12, 20, TileType.Crate);
                chunk.SetTile(12, 21, TileType.Crate);

                GenerateLedge(chunk, startCol: 4, row: 16, len: 8, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 8, topRow: 16, bottomRow: 22);

                GenerateLedge(chunk, startCol: 5, row: 10, len: 7, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 6, topRow: 10, bottomRow: 16);

                GenerateLedge(chunk, startCol: 4, row: 4, len: 8, TileType.OneWayPlatform);
                PlaceLadder(chunk, col: 9, topRow: 4, bottomRow: 10);
                break;
        }
    }

    private static void GenerateLeftBoundary(TileChunk chunk)
    {
        for (int r = 0; r < TileChunk.ChunkHeight; r++)
        {
            for (int c = 0; c < TileChunk.ChunkWidth; c++)
            {
                chunk.SetTile(c, r, TileType.Solid);
            }
        }
    }
}
