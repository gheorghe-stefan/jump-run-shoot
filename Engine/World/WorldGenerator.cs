namespace JumpRunShoot.Engine.World;

/// <summary>
/// Macro zone styles that span 2-4 consecutive chunks to give exploration changing rhythms and silhouettes.
/// </summary>
public enum ZoneStyle
{
    OpenHills,
    CanyonCliffs,
    OutpostRuins,
    RavineCrossings,
    SkyTerraces,
    SupplyVaults
}

/// <summary>
/// Parametric, deterministic endless-world generator powered by 64-bit SplitMix64 seeding.
/// Eliminates static chunk templates in favor of continuous ground elevation, macro zones,
/// horizontal mirroring, varied vertical tiers, and a post-generation traversal validation pass.
/// </summary>
public sealed class WorldGenerator
{
    public const string DefaultSeed = "FOO";
    public const int MinGroundRow = 23;
    public const int MaxGroundRow = 28;
    public const int MaxContiguousHazardWidth = 4;

    public string Seed { get; private set; } = DefaultSeed;
    private ulong _numericSeed;

    public WorldGenerator(string seed = DefaultSeed)
    {
        SetSeed(seed);
    }

    public void SetSeed(string seed)
    {
        Seed = string.IsNullOrWhiteSpace(seed) ? DefaultSeed : seed.Trim();
        var rng = new DeterministicRandom(Seed);
        _numericSeed = rng.NextULong();
    }

    /// <summary>
    /// Mixes the world 64-bit seed with chunk coordinates and an arbitrary salt so every chunk stream is independent.
    /// </summary>
    private DeterministicRandom CreateChunkRng(int chunkIndex, ulong salt)
    {
        ulong mixed = _numericSeed ^ ((ulong)(uint)chunkIndex * 0x9E3779B97F4A7C15UL) ^ salt;
        return new DeterministicRandom(mixed);
    }

    /// <summary>
    /// Computes the deterministic macro zone style for a chunk. Zones last 2-4 chunks.
    /// </summary>
    public ZoneStyle GetZoneForChunk(int chunkIndex)
    {
        if (chunkIndex <= 0) return ZoneStyle.OpenHills;

        // Walk deterministic variable-length segments starting from chunk 1
        int cursor = 1;
        int segmentIdx = 0;
        while (true)
        {
            var segRng = CreateChunkRng(segmentIdx, 0x5A0E_11B3_C7D9_4E21UL);
            int length = segRng.NextInt(2, 5); // 2, 3, or 4 chunks per zone
            ZoneStyle style = (ZoneStyle)segRng.NextInt(0, 6);
            if (chunkIndex < cursor + length)
            {
                return style;
            }
            cursor += length;
            segmentIdx++;
        }
    }

    /// <summary>
    /// Computes the shared terrain elevation row at the left boundary of <paramref name="chunkIndex"/>.
    /// Ensures chunk 0 and chunk 1 connect seamlessly at row 28 while later boundaries vary smoothly.
    /// </summary>
    public int GetBoundaryGroundRow(int chunkIndex)
    {
        if (chunkIndex <= 1) return TileChunk.FloorRow;

        int current = TileChunk.FloorRow;
        for (int i = 2; i <= chunkIndex; i++)
        {
            var stepRng = CreateChunkRng(i, 0xB471_8C2F_91A3_5D07UL);
            int targetBias = stepRng.NextInt(MinGroundRow, MaxGroundRow + 1);
            int delta = Math.Clamp(targetBias - current, -2, 2);
            current = Math.Clamp(current + delta, MinGroundRow, MaxGroundRow);
        }
        return current;
    }

    public TileChunk GenerateChunk(int chunkIndex)
    {
        var chunk = new TileChunk(chunkIndex);

        if (chunkIndex < 0)
        {
            GenerateLeftBoundary(chunk);
            return chunk;
        }

        if (chunkIndex == 0)
        {
            GenerateStartingChunk(chunk);
            return chunk;
        }

        var rng = CreateChunkRng(chunkIndex, 0x71D3_49E2_A85F_103BUL);
        ZoneStyle zone = GetZoneForChunk(chunkIndex);
        bool mirrored = rng.NextBool(0.5f);

        int[] floorRows = BuildTerrainProfile(chunkIndex, zone, ref rng);
        ApplyTerrainFloor(chunk, floorRows);

        switch (zone)
        {
            case ZoneStyle.OpenHills:
                PopulateOpenHills(chunk, floorRows, mirrored, ref rng);
                break;
            case ZoneStyle.CanyonCliffs:
                PopulateCanyonCliffs(chunk, floorRows, mirrored, ref rng);
                break;
            case ZoneStyle.OutpostRuins:
                PopulateOutpostRuins(chunk, floorRows, mirrored, ref rng);
                break;
            case ZoneStyle.RavineCrossings:
                PopulateRavineCrossings(chunk, floorRows, mirrored, ref rng);
                break;
            case ZoneStyle.SkyTerraces:
                PopulateSkyTerraces(chunk, floorRows, mirrored, ref rng);
                break;
            case ZoneStyle.SupplyVaults:
                PopulateSupplyVaults(chunk, floorRows, mirrored, ref rng);
                break;
        }

        // Sprinkle small asymmetric bonus details (floating secret caches, overhead anchors, crate steps)
        AddOrganicMicroDetails(chunk, floorRows, mirrored, ref rng);

        // Guarantee clean seams, capped hazard widths, traversable steps, and no ladder-pillar artifacts
        EnforcePlayabilityAndSeamPass(chunk, floorRows);

        // Add intentional 1-cell crouch-only crawlways (guarded by shootable crates, with open upper bypass for zero-G)
        // plus extra shootable crate clusters using an independent deterministic RNG stream
        TryAddCrouchCrawlwayAndExtraCrates(chunk, floorRows);

        return chunk;
    }

    private void GenerateLeftBoundary(TileChunk chunk)
    {
        for (int r = 0; r < TileChunk.ChunkHeight; r++)
        {
            chunk.SetTile(TileChunk.ChunkWidth - 1, r, TileType.Border);
        }
    }

    /// <summary>
    /// Starting chunk keeps row 28 continuous floor, col 0 boundary wall, and open cols 1..3 spawn safezone,
    /// while parametrically varying upper platforms, ladders, crates, and ceiling beams per seed.
    /// </summary>
    private void GenerateStartingChunk(TileChunk chunk)
    {
        var rng = CreateChunkRng(0, 0x33F1_8A9C_62D0_E741UL);

        int[] floorRows = new int[TileChunk.ChunkWidth];
        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            floorRows[c] = TileChunk.FloorRow;
        }
        ApplyTerrainFloor(chunk, floorRows);

        // Left world boundary wall at col 0
        for (int r = 0; r < TileChunk.FloorRow; r++)
        {
            chunk.SetTile(0, r, TileType.Border);
        }

        // Main low-to-mid platform beyond spawn cols 1..3
        int lowRow = rng.NextInt(21, 25);
        int lowCol = rng.NextInt(4, 7);
        int lowLen = rng.NextInt(5, 9);
        PlaceHorizontalSpan(chunk, lowCol, lowRow, lowLen, TileType.OneWayPlatform);

        // Rightward deck or terrace near exit
        int rightRow = rng.NextInt(17, 23);
        int rightCol = rng.NextInt(10, 13);
        PlaceHorizontalSpan(chunk, rightCol, rightRow, TileChunk.ChunkWidth - rightCol, TileType.OneWayPlatform);

        // High sky deck(s) with non-fixed heights
        int midRow = rng.NextInt(13, 18);
        int midCol = rng.NextInt(4, 9);
        int midLen = rng.NextInt(4, 7);
        PlaceHorizontalSpan(chunk, midCol, midRow, midLen, rng.NextBool(0.65f) ? TileType.OneWayPlatform : TileType.Solid);

        int topRow = rng.NextInt(6, 11);
        int topCol = rng.NextInt(2, 8);
        int topLen = rng.NextInt(5, 8);
        PlaceHorizontalSpan(chunk, topCol, topRow, topLen, rng.NextBool(0.45f) ? TileType.Solid : TileType.OneWayPlatform);

        // At least one primary ladder connecting floor toward low/mid/top decks (beyond safe cols 1..3)
        int ladderCol = Math.Clamp(lowCol + rng.NextInt(1, Math.Max(2, lowLen - 1)), 5, 13);
        int ladderTop = Math.Min(lowRow, midRow);
        TryPlaceSafeLadder(chunk, ladderCol, ladderTop, TileChunk.FloorRow - 1);

        // Optional upper ladder connecting mid/top if separate
        if (topRow < midRow - 2)
        {
            int upperLadderCol = Math.Clamp(rng.NextInt(topCol + 1, Math.Min(14, topCol + topLen)), 4, 13);
            if (Math.Abs(upperLadderCol - ladderCol) >= 2)
            {
                TryPlaceSafeLadder(chunk, upperLadderCol, topRow, midRow - 1);
            }
        }

        // Crate cluster on floor (always cols >= 5 so cols 1..3 remain unobstructed)
        int crateStart = rng.NextInt(5, 11);
        int crateCount = rng.NextInt(2, 5);
        PlaceCrateCluster(chunk, crateStart, floorRows, crateCount, rng.NextInt(1, 3), rng.NextBool());

        // Occasional upper crate reward
        if (rng.NextBool(0.6f))
        {
            int crateRow = topRow - 1;
            int crateC = Math.Clamp(topCol + rng.NextInt(1, Math.Max(2, topLen - 1)), 4, 14);
            if (chunk.GetTile(crateC, topRow) != TileType.Empty && chunk.GetTile(crateC, crateRow) == TileType.Empty)
            {
                chunk.SetTile(crateC, crateRow, TileType.Crate);
            }
        }

        EnforcePlayabilityAndSeamPass(chunk, floorRows);
    }

    private int[] BuildTerrainProfile(int chunkIndex, ZoneStyle zone, ref DeterministicRandom rng)
    {
        int[] profile = new int[TileChunk.ChunkWidth];
        int leftRow = GetBoundaryGroundRow(chunkIndex);
        int rightRow = GetBoundaryGroundRow(chunkIndex + 1);

        int midAnchorCol = rng.NextInt(5, 11);
        int midDelta = zone switch
        {
            ZoneStyle.CanyonCliffs => rng.NextInt(-3, 2),
            ZoneStyle.RavineCrossings => rng.NextInt(0, 3),
            ZoneStyle.OpenHills => rng.NextInt(-2, 3),
            ZoneStyle.SkyTerraces => rng.NextInt(-1, 2),
            _ => rng.NextInt(-2, 2)
        };
        int midRow = Math.Clamp((leftRow + rightRow) / 2 + midDelta, MinGroundRow, MaxGroundRow);

        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            float baseVal;
            if (c <= midAnchorCol)
            {
                float t = midAnchorCol == 0 ? 1f : (float)c / midAnchorCol;
                baseVal = leftRow + (midRow - leftRow) * t;
            }
            else
            {
                int span = (TileChunk.ChunkWidth - 1) - midAnchorCol;
                float t = span == 0 ? 1f : (float)(c - midAnchorCol) / span;
                baseVal = midRow + (rightRow - midRow) * t;
            }

            // Organic plateau terraces rather than noisy jaggedness every single tile
            if (zone == ZoneStyle.OpenHills && c >= 2 && c <= 13 && rng.NextBool(0.18f))
            {
                baseVal += rng.NextBool() ? -1 : 1;
            }

            profile[c] = Math.Clamp((int)MathF.Round(baseVal), MinGroundRow, MaxGroundRow);
        }

        // Enforce seam endpoints match adjacent chunk boundaries exactly
        profile[0] = leftRow;
        profile[TileChunk.ChunkWidth - 1] = rightRow;

        // Smooth terrain strictly to <= 1 tile per column so slopes never become steep vertical barriers
        for (int c = 1; c < TileChunk.ChunkWidth; c++)
        {
            profile[c] = Math.Clamp(profile[c], profile[c - 1] - 1, profile[c - 1] + 1);
        }
        profile[TileChunk.ChunkWidth - 1] = rightRow;
        for (int c = TileChunk.ChunkWidth - 2; c >= 1; c--)
        {
            profile[c] = Math.Clamp(profile[c], profile[c + 1] - 1, profile[c + 1] + 1);
        }

        return profile;
    }

    private static void ApplyTerrainFloor(TileChunk chunk, int[] floorRows)
    {
        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            int topRow = floorRows[c];
            for (int r = topRow; r < TileChunk.ChunkHeight; r++)
            {
                chunk.SetTile(c, r, TileType.Solid);
            }
        }
    }

    private static int MirrorCol(int col, int width, bool mirrored)
        => mirrored ? (TileChunk.ChunkWidth - col - width) : col;

    private static int MirrorSingle(int col, bool mirrored)
        => mirrored ? (TileChunk.ChunkWidth - 1 - col) : col;

    private void PopulateOpenHills(TileChunk chunk, int[] floorRows, bool mirrored, ref DeterministicRandom rng)
    {
        // Multi-elevation floating islands & gentle hillside stairs
        int tierCount = rng.NextInt(3, 5);
        int rowCursor = rng.NextInt(19, 23);

        for (int i = 0; i < tierCount; i++)
        {
            int len = rng.NextInt(4, 8);
            int rawStart = (i % 2 == 0) ? rng.NextInt(1, 5) : rng.NextInt(6, 15 - len + 1);
            int start = Math.Clamp(MirrorCol(rawStart, len, mirrored), 1, TileChunk.ChunkWidth - len - 1);
            int row = Math.Clamp(rowCursor, 5, floorRows[start + len / 2] - 3);
            TileType type = (i == tierCount - 1 && rng.NextBool(0.45f)) ? TileType.Solid : TileType.OneWayPlatform;

            PlaceHorizontalSpan(chunk, start, row, len, type);

            if (rng.NextBool(0.55f))
            {
                int lCol = Math.Clamp(start + rng.NextInt(1, Math.Max(2, len - 1)), 2, 13);
                int bottom = (i == 0) ? floorRows[lCol] - 1 : Math.Min(floorRows[lCol] - 1, row + rng.NextInt(3, 6));
                TryPlaceSafeLadder(chunk, lCol, row, bottom);
            }

            rowCursor -= rng.NextInt(3, 5);
        }

        if (rng.NextBool(0.40f))
        {
            int pitStart = rng.NextInt(4, 10);
            int pitLen = rng.NextInt(2, 4);
            TryPlaceHazardPocket(chunk, floorRows, pitStart, pitLen, addBridgeAbove: true, ref rng);
        }

        // Extra destructible crate cluster on hills for fun shooting/clearing
        int hillCrateCol = Math.Clamp(MirrorSingle(rng.NextInt(2, 11), mirrored), 2, 11);
        PlaceCrateCluster(chunk, hillCrateCol, floorRows, rng.NextInt(2, 4), rng.NextInt(1, 3), rng.NextBool());
    }

    private void PopulateCanyonCliffs(TileChunk chunk, int[] floorRows, bool mirrored, ref DeterministicRandom rng)
    {
        // High plateau ridges, cliff overhangs (ceiling mounts), and switchback ladders
        int overhangRow = rng.NextInt(10, 15);
        int overhangLen = rng.NextInt(5, 9);
        int overhangStart = Math.Clamp(MirrorCol(rng.NextInt(2, 6), overhangLen, mirrored), 1, TileChunk.ChunkWidth - overhangLen - 1);
        PlaceHorizontalSpan(chunk, overhangStart, overhangRow, overhangLen, TileType.Solid);

        // Optional second lower ledge beneath overhang
        int midRow = Math.Clamp(overhangRow + rng.NextInt(5, 8), 15, 22);
        int midLen = rng.NextInt(4, 7);
        int midStart = Math.Clamp(MirrorCol(rng.NextInt(7, 11), midLen, mirrored), 1, TileChunk.ChunkWidth - midLen - 1);
        PlaceHorizontalSpan(chunk, midStart, midRow, midLen, TileType.OneWayPlatform);

        // High perch above overhang
        int skyRow = rng.NextInt(5, 9);
        int skyLen = rng.NextInt(4, 7);
        int skyStart = Math.Clamp(overhangStart + rng.NextInt(-1, 3), 2, TileChunk.ChunkWidth - skyLen - 1);
        PlaceHorizontalSpan(chunk, skyStart, skyRow, skyLen, TileType.OneWayPlatform);

        // Connect cliff steps with a staggered ladder or boulder crates
        int lCol = Math.Clamp(overhangStart + rng.NextInt(0, 2) * (overhangLen - 1), 2, 13);
        TryPlaceSafeLadder(chunk, lCol, overhangRow, floorRows[lCol] - 1);

        int upperLadder = Math.Clamp(skyStart + rng.NextInt(1, Math.Max(2, skyLen - 1)), 2, 13);
        if (Math.Abs(upperLadder - lCol) >= 2)
        {
            TryPlaceSafeLadder(chunk, upperLadder, skyRow, overhangRow - 1);
        }

        // Boulder step crates against a cliff rise
        int crateCol = Math.Clamp(MirrorSingle(rng.NextInt(3, 12), mirrored), 2, 12);
        PlaceCrateCluster(chunk, crateCol, floorRows, rng.NextInt(2, 4), rng.NextInt(1, 3), ascending: rng.NextBool());
    }

    private void PopulateOutpostRuins(TileChunk chunk, int[] floorRows, bool mirrored, ref DeterministicRandom rng)
    {
        // Asymmetric multi-room fortified outpost with randomized walls, >=2 tile doorways, interior catwalks, roof access
        int width = rng.NextInt(5, 9);
        int rawLeft = rng.NextInt(1, TileChunk.ChunkWidth - width - 2);
        int leftWall = Math.Clamp(MirrorCol(rawLeft, width + 1, mirrored), 1, TileChunk.ChunkWidth - width - 2);
        int rightWall = leftWall + width;

        int leftFloor = floorRows[leftWall];
        int rightFloor = floorRows[rightWall];
        int minFloor = Math.Min(leftFloor, rightFloor);
        int roofRow = rng.NextInt(11, Math.Min(17, minFloor - 6));
        int doorHeight = rng.NextInt(3, 5);

        for (int r = roofRow; r <= leftFloor - doorHeight; r++)
        {
            chunk.SetTile(leftWall, r, TileType.Solid);
        }
        for (int r = roofRow; r <= rightFloor - doorHeight; r++)
        {
            chunk.SetTile(rightWall, r, TileType.Solid);
        }

        // Solid bunker roof with central hatch opening or ladder entry
        int hatchCol = rng.NextInt(leftWall + 2, rightWall - 1);
        for (int c = leftWall; c <= rightWall; c++)
        {
            chunk.SetTile(c, roofRow, c == hatchCol ? TileType.OneWayPlatform : TileType.Solid);
        }

        // Interior mezzanine deck
        int minMezz = roofRow + 4;
        int maxMezz = minFloor - 3;
        if (minMezz <= maxMezz && rightWall - leftWall >= 5)
        {
            int mezzRow = (minMezz + maxMezz) / 2;
            PlaceHorizontalSpan(chunk, leftWall + 1, mezzRow, rightWall - leftWall - 1, TileType.OneWayPlatform);
        }

        // Climbable hatch ladder from interior floor up onto roof
        TryPlaceSafeLadder(chunk, hatchCol, roofRow, floorRows[hatchCol] - 1);

        // Optional upper watchtower deck above roof
        if (rng.NextBool(0.70f))
        {
            int towerRow = rng.NextInt(5, Math.Max(6, roofRow - 2));
            int towerLen = rng.NextInt(4, 7);
            int towerStart = Math.Clamp(leftWall + rng.NextInt(0, Math.Max(1, rightWall - leftWall - towerLen + 1)), 1, TileChunk.ChunkWidth - towerLen - 1);
            PlaceHorizontalSpan(chunk, towerStart, towerRow, towerLen, TileType.OneWayPlatform);

            int topLadderCol = Math.Clamp(towerStart + rng.NextInt(1, Math.Max(2, towerLen - 1)), 2, 13);
            if (Math.Abs(topLadderCol - hatchCol) >= 2)
            {
                TryPlaceSafeLadder(chunk, topLadderCol, towerRow, roofRow - 1);
            }
        }

        // Optional exterior awning step so players arriving at mid-height can scale onto the roof
        if (leftWall >= 3 && rng.NextBool(0.8f))
        {
            PlaceHorizontalSpan(chunk, leftWall - 2, roofRow + 2, 2, TileType.OneWayPlatform);
        }

        // Defensive crates outside or inside ruins
        if (rng.NextBool(0.65f))
        {
            int crateCol = Math.Clamp(hatchCol + (rng.NextBool() ? -2 : 2), leftWall + 1, rightWall - 1);
            PlaceCrateCluster(chunk, crateCol, floorRows, rng.NextInt(1, 3), 1, ascending: true);
        }
    }

    private void PopulateRavineCrossings(TileChunk chunk, int[] floorRows, bool mirrored, ref DeterministicRandom rng)
    {
        // High and low parallel suspension bridges spanning short hazard dips (never > 4 tiles wide!)
        int mainBridgeRow = rng.NextInt(18, 22);
        int bridgeLen = rng.NextInt(8, 12);
        int bridgeStart = Math.Clamp(MirrorCol(rng.NextInt(2, 5), bridgeLen, mirrored), 1, TileChunk.ChunkWidth - bridgeLen - 1);
        PlaceHorizontalSpan(chunk, bridgeStart, mainBridgeRow, bridgeLen, TileType.OneWayPlatform);

        // Second higher skyway or solid gantry
        int highRow = rng.NextInt(9, 14);
        int highLen = rng.NextInt(5, 9);
        int highStart = Math.Clamp(MirrorCol(rng.NextInt(3, 7), highLen, mirrored), 1, TileChunk.ChunkWidth - highLen - 1);
        TileType highType = rng.NextBool(0.55f) ? TileType.Solid : TileType.OneWayPlatform;
        PlaceHorizontalSpan(chunk, highStart, highRow, highLen, highType);

        // Side access ladder onto lower bridge + staggered ladder up to high bridge
        int lCol1 = Math.Clamp(bridgeStart + rng.NextInt(1, 3), 2, 13);
        TryPlaceSafeLadder(chunk, lCol1, mainBridgeRow, floorRows[lCol1] - 1);

        int lCol2 = Math.Clamp(highStart + rng.NextInt(2, Math.Max(3, highLen - 1)), 2, 13);
        if (Math.Abs(lCol2 - lCol1) >= 3)
        {
            TryPlaceSafeLadder(chunk, lCol2, highRow, mainBridgeRow - 1);
        }

        // 1 or 2 compact, well-separated 2-3 tile recessed hazards beneath the bridge with safe rock between them
        int pit1Start = rng.NextInt(3, 6);
        int pit1Len = rng.NextInt(2, 4);
        TryPlaceHazardPocket(chunk, floorRows, pit1Start, pit1Len, addBridgeAbove: false, ref rng);

        if (rng.NextBool(0.55f))
        {
            int pit2Start = Math.Clamp(pit1Start + pit1Len + rng.NextInt(2, 4), 8, 11);
            int pit2Len = rng.NextInt(2, 4);
            TryPlaceHazardPocket(chunk, floorRows, pit2Start, pit2Len, addBridgeAbove: false, ref rng);
        }
    }

    private void PopulateSkyTerraces(TileChunk chunk, int[] floorRows, bool mirrored, ref DeterministicRandom rng)
    {
        // Staggered ascending islands with irregular Y steps — mixes jump-only routes & partial ladders
        int islandCount = rng.NextInt(4, 7);
        int currentRow = Math.Clamp(floorRows[7] - rng.NextInt(3, 5), 18, 25);
        int prevCenter = rng.NextInt(3, 12);

        for (int i = 0; i < islandCount && currentRow >= 5; i++)
        {
            int len = rng.NextInt(3, 6);
            int shift = rng.NextBool() ? rng.NextInt(3, 7) : -rng.NextInt(3, 7);
            int nextCenter = Math.Clamp(prevCenter + shift, 2, 13);
            int start = Math.Clamp(MirrorCol(nextCenter - len / 2, len, mirrored), 1, TileChunk.ChunkWidth - len - 1);
            TileType type = (i % 3 == 2 && currentRow >= 8) ? TileType.Solid : TileType.OneWayPlatform;

            PlaceHorizontalSpan(chunk, start, currentRow, len, type);

            if (i == 0 && rng.NextBool(0.6f))
            {
                int lCol = Math.Clamp(start + len / 2, 2, 13);
                TryPlaceSafeLadder(chunk, lCol, currentRow, floorRows[lCol] - 1);
            }
            else if (i > 0 && rng.NextBool(0.45f))
            {
                int step = rng.NextInt(3, 5);
                int lCol = Math.Clamp(start + rng.NextInt(0, len), 2, 13);
                TryPlaceSafeLadder(chunk, lCol, currentRow, Math.Min(floorRows[lCol] - 1, currentRow + step));
            }

            prevCenter = nextCenter;
            currentRow -= rng.NextInt(2, 4); // Jump-friendly 2-3 row vertical separation!
        }

        // Ground stepping crates at entry or exit
        PlaceCrateCluster(chunk, MirrorCol(rng.NextInt(2, 5), 2, mirrored), floorRows, 2, 1, true);
    }

    private void PopulateSupplyVaults(TileChunk chunk, int[] floorRows, bool mirrored, ref DeterministicRandom rng)
    {
        // Heavy destructible crate stockpiles, ceiling gantry beams, and split-height shelves
        int cluster1 = Math.Clamp(MirrorSingle(rng.NextInt(2, 6), mirrored), 2, 11);
        PlaceCrateCluster(chunk, cluster1, floorRows, rng.NextInt(3, 5), rng.NextInt(2, 4), rng.NextBool());

        int cluster2 = Math.Clamp(MirrorSingle(rng.NextInt(9, 13), mirrored), 2, 12);
        if (Math.Abs(cluster2 - cluster1) >= 4)
        {
            PlaceCrateCluster(chunk, cluster2, floorRows, rng.NextInt(2, 4), rng.NextInt(1, 3), rng.NextBool());
        }

        // Warehouse shelves across varied mid/high rows
        int shelf1Row = rng.NextInt(17, 22);
        int shelf1Start = Math.Clamp(MirrorCol(rng.NextInt(2, 6), 6, mirrored), 1, 9);
        PlaceHorizontalSpan(chunk, shelf1Start, shelf1Row, rng.NextInt(5, 8), TileType.OneWayPlatform);

        int shelf2Row = rng.NextInt(10, 15);
        int shelf2Len = rng.NextInt(5, 9);
        int shelf2Start = Math.Clamp(MirrorCol(rng.NextInt(5, 9), shelf2Len, mirrored), 1, TileChunk.ChunkWidth - shelf2Len - 1);
        PlaceHorizontalSpan(chunk, shelf2Start, shelf2Row, shelf2Len, TileType.Solid);

        // Top crates resting right on solid gantry shelf!
        int crateCountTop = rng.NextInt(2, 4);
        int crateTopCol = Math.Clamp(shelf2Start + rng.NextInt(1, Math.Max(2, shelf2Len - crateCountTop)), 2, 13);
        for (int c = crateTopCol; c < Math.Min(shelf2Start + shelf2Len, crateTopCol + crateCountTop); c++)
        {
            chunk.SetTile(c, shelf2Row - 1, TileType.Crate);
        }

        int lCol = Math.Clamp(shelf2Start + rng.NextInt(1, Math.Max(2, shelf2Len - 1)), 2, 13);
        TryPlaceSafeLadder(chunk, lCol, shelf2Row, floorRows[lCol] - 1);
    }

    private void AddOrganicMicroDetails(TileChunk chunk, int[] floorRows, bool mirrored, ref DeterministicRandom rng)
    {
        // High ceiling anchor beam so CeilingTurret / Seal has places to cling in open areas too
        if (rng.NextBool(0.65f))
        {
            int ceilRow = rng.NextInt(4, 8);
            int ceilLen = rng.NextInt(3, 7);
            int ceilStart = Math.Clamp(MirrorCol(rng.NextInt(3, 10), ceilLen, mirrored), 2, TileChunk.ChunkWidth - ceilLen - 2);
            bool spaceClear = true;
            for (int c = ceilStart; c < ceilStart + ceilLen; c++)
            {
                if (chunk.GetTile(c, ceilRow) != TileType.Empty ||
                    chunk.GetTile(c, ceilRow + 1) != TileType.Empty ||
                    chunk.GetTile(c, ceilRow + 2) != TileType.Empty)
                {
                    spaceClear = false;
                    break;
                }
            }
            if (spaceClear)
            {
                PlaceHorizontalSpan(chunk, ceilStart, ceilRow, ceilLen, TileType.Solid);
            }
        }

        // Abundant destructible crate clusters on floor + elevated decks so every chunk has fun shootable bricks
        if (rng.NextBool(0.85f))
        {
            int col = rng.NextInt(2, 12);
            if (chunk.GetTile(col, floorRows[col] - 1) == TileType.Empty &&
                chunk.GetTile(col, floorRows[col]) == TileType.Solid)
            {
                PlaceCrateCluster(chunk, col, floorRows, rng.NextInt(2, 4), rng.NextInt(1, 3), rng.NextBool());
            }
        }

        if (rng.NextBool(0.75f))
        {
            int col2 = rng.NextInt(3, 13);
            if (chunk.GetTile(col2, floorRows[col2] - 1) == TileType.Empty &&
                chunk.GetTile(col2, floorRows[col2]) == TileType.Solid)
            {
                PlaceCrateCluster(chunk, col2, floorRows, rng.NextInt(2, 4), rng.NextInt(1, 3), rng.NextBool());
            }
        }

        // Sprinkle 1-2 shootable crate mini-stacks atop elevated platforms (avoiding ladders & pinch ceilings)
        int upperPlaced = 0;
        for (int attempt = 0; attempt < 8 && upperPlaced < 2; attempt++)
        {
            int c = rng.NextInt(2, TileChunk.ChunkWidth - 2);
            int r = rng.NextInt(7, 23);
            if (r >= floorRows[c] - 2) continue;

            TileType support = chunk.GetTile(c, r);
            if (support is not (TileType.OneWayPlatform or TileType.Solid)) continue;
            if (chunk.GetTile(c, r - 1) != TileType.Empty || chunk.GetTile(c, r - 2) != TileType.Empty) continue;
            if (chunk.GetTile(c - 1, r - 1) == TileType.Ladder || chunk.GetTile(c + 1, r - 1) == TileType.Ladder) continue;

            int stackLen = rng.NextInt(1, 3);
            bool placedAny = false;
            for (int dx = 0; dx < stackLen && c + dx < TileChunk.ChunkWidth - 2; dx++)
            {
                int cc = c + dx;
                if (chunk.GetTile(cc, r) is TileType.OneWayPlatform or TileType.Solid &&
                    chunk.GetTile(cc, r - 1) == TileType.Empty &&
                    chunk.GetTile(cc, r - 2) == TileType.Empty &&
                    chunk.GetTile(cc, r - 3) != TileType.Solid)
                {
                    chunk.SetTile(cc, r - 1, TileType.Crate);
                    placedAny = true;
                }
            }
            if (placedAny) upperPlaced++;
        }
    }

    private static void PlaceHorizontalSpan(TileChunk chunk, int startCol, int row, int len, TileType type)
    {
        if ((uint)row >= TileChunk.ChunkHeight) return;
        int end = Math.Min(TileChunk.ChunkWidth, startCol + len);
        for (int c = Math.Max(0, startCol); c < end; c++)
        {
            if (chunk.GetTile(c, row) != TileType.Ladder && chunk.GetTile(c, row) != TileType.Border)
            {
                chunk.SetTile(c, row, type);
            }
        }
    }

    private static void PlaceCrateCluster(TileChunk chunk, int startCol, int[] floorRows, int width, int maxHeight, bool ascending)
    {
        for (int i = 0; i < width; i++)
        {
            int c = startCol + i;
            if (c <= 1 || c >= TileChunk.ChunkWidth - 2) continue;
            int floor = floorRows[c];
            if (chunk.GetTile(c, floor) != TileType.Solid) continue;

            int stackHeight = Math.Clamp(ascending ? (i + 1) : (width - i), 1, maxHeight);
            for (int h = 1; h <= stackHeight; h++)
            {
                int r = floor - h;
                if (r >= 4 && chunk.GetTile(c, r) == TileType.Empty)
                {
                    chunk.SetTile(c, r, TileType.Crate);
                }
            }
        }
    }

    private static void TryPlaceSafeLadder(TileChunk chunk, int col, int topRow, int bottomRow)
    {
        if (col < 1 || col >= TileChunk.ChunkWidth - 1) return;
        topRow = Math.Max(3, topRow);
        bottomRow = Math.Min(TileChunk.ChunkHeight - 4, bottomRow);
        if (topRow > bottomRow) return;

        // Avoid sandwiching ladders against an immediately adjacent solid pillar column
        for (int r = topRow; r <= bottomRow; r++)
        {
            TileType existing = chunk.GetTile(col, r);
            if (existing == TileType.Solid)
            {
                // At the very top exit of a ladder shaft, carve a hatch through a horizontal Solid deck
                bool hasDeckSide =
                    chunk.GetTile(col - 1, r) is TileType.Solid or TileType.OneWayPlatform ||
                    chunk.GetTile(col + 1, r) is TileType.Solid or TileType.OneWayPlatform;
                bool openAbove = chunk.GetTile(col, r - 1) != TileType.Solid &&
                                 chunk.GetTile(col, r - 2) != TileType.Solid;
                if (r == topRow && hasDeckSide && openAbove)
                {
                    chunk.SetTile(col, r, TileType.Ladder);
                }
                continue;
            }

            // Break any twin-ladder-over-pillar pattern (neither col-2 nor col+2 should already be Ladder across a solid col-1/col+1)
            bool leftPillarTrap = chunk.GetTile(col - 1, r) == TileType.Solid && chunk.GetTile(col - 2, r) == TileType.Ladder;
            bool rightPillarTrap = chunk.GetTile(col + 1, r) == TileType.Solid && chunk.GetTile(col + 2, r) == TileType.Ladder;
            if (leftPillarTrap || rightPillarTrap) continue;

            chunk.SetTile(col, r, TileType.Ladder);
        }
    }

    private void TryPlaceHazardPocket(TileChunk chunk, int[] floorRows, int startCol, int width, bool addBridgeAbove, ref DeterministicRandom rng)
    {
        width = Math.Clamp(width, 2, MaxContiguousHazardWidth);
        int endCol = Math.Min(TileChunk.ChunkWidth - 2, startCol + width);
        startCol = Math.Max(2, startCol);
        if (endCol - startCol < 2) return;

        // Ensure surrounding columns aren't already hazards so pockets never merge into wide >4-tile trenches
        if (chunk.GetTile(startCol - 1, floorRows[startCol - 1] + 1) == TileType.Hazard ||
            chunk.GetTile(endCol, floorRows[endCol] + 1) == TileType.Hazard)
        {
            return;
        }

        // Only place hazard on flat floor segments bounded by flat entry/exit so pit depth is <= 1 tile on both sides
        int baseRow = floorRows[startCol];
        if (floorRows[startCol - 1] != baseRow || floorRows[endCol] != baseRow) return;
        for (int c = startCol; c < endCol; c++)
        {
            if (floorRows[c] != baseRow) return;
            if (chunk.GetTile(c, baseRow - 1) == TileType.Ladder) return;
        }

        for (int c = startCol; c < endCol; c++)
        {
            int r = floorRows[c];
            chunk.SetTile(c, r, TileType.Empty);
            chunk.SetTile(c, r + 1, TileType.Hazard);
            chunk.SetTile(c, r + 2, TileType.Solid);
        }

        if (addBridgeAbove && rng.NextBool(0.65f))
        {
            int bridgeRow = baseRow - rng.NextInt(3, 5);
            if (bridgeRow >= 6)
            {
                PlaceHorizontalSpan(chunk, startCol - 1, bridgeRow, (endCol - startCol) + 2, TileType.OneWayPlatform);
            }
        }
    }

    /// <summary>
    /// Post-generation validation and cleanup pass guaranteeing:
    /// 1) Seamless 3-tile headroom at chunk entry/exit seams (col 0 and col 15) above terrain floor.
    /// 2) Contiguous Hazard runs strictly <= MaxContiguousHazardWidth (4 tiles).
    /// 3) No solid vertical barrier blocking left-to-right progress without a >=2-tile doorway.
    /// 4) No twin-ladders sandwiching a 1-tile solid column.
    /// </summary>
    private static void EnforcePlayabilityAndSeamPass(TileChunk chunk, int[] floorRows)
    {
        // 1. Keep chunk seam columns (col 0 for chunk >= 1, and col 15 for all chunks) clear above terrain
        int startSeam = chunk.ChunkIndex == 0 ? 15 : 0;
        for (int seamCol = startSeam; seamCol <= 15; seamCol += 15)
        {
            int fRow = floorRows[seamCol];
            for (int r = Math.Max(1, fRow - 3); r < fRow; r++)
            {
                if (chunk.GetTile(seamCol, r) == TileType.Solid || chunk.GetTile(seamCol, r) == TileType.Hazard)
                {
                    chunk.SetTile(seamCol, r, TileType.Empty);
                }
            }
        }

        // 2. Keep starter safe zone (chunk 0, cols 1..3) completely clear above floor
        if (chunk.ChunkIndex == 0)
        {
            for (int c = 1; c <= 3; c++)
            {
                for (int r = TileChunk.FloorRow - 4; r < TileChunk.FloorRow; r++)
                {
                    if (chunk.GetTile(c, r) == TileType.Solid ||
                        chunk.GetTile(c, r) == TileType.Crate ||
                        chunk.GetTile(c, r) == TileType.Hazard)
                    {
                        chunk.SetTile(c, r, TileType.Empty);
                    }
                }
            }
        }

        // 3. Enforce no contiguous Hazard run exceeds MaxContiguousHazardWidth (convert excess to safe floor)
        int streak = 0;
        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            bool hasHazardInCol = false;
            for (int r = 0; r < TileChunk.ChunkHeight; r++)
            {
                if (chunk.GetTile(c, r) == TileType.Hazard)
                {
                    hasHazardInCol = true;
                    break;
                }
            }

            if (hasHazardInCol)
            {
                streak++;
                if (streak > MaxContiguousHazardWidth)
                {
                    int fRow = floorRows[c];
                    chunk.SetTile(c, fRow, TileType.Solid);
                    if (fRow + 1 < TileChunk.ChunkHeight && chunk.GetTile(c, fRow + 1) == TileType.Hazard)
                    {
                        chunk.SetTile(c, fRow + 1, TileType.Solid);
                    }
                    streak = 0;
                }
            }
            else
            {
                streak = 0;
            }
        }

        // 4. Carve guaranteed >=2-tile doorway headroom above terrain and adjacent steps
        for (int c = 1; c < TileChunk.ChunkWidth - 1; c++)
        {
            int fRow = floorRows[c];
            int minNeighborFloor = Math.Min(fRow, Math.Min(floorRows[c - 1], floorRows[c + 1]));
            for (int r = Math.Max(1, minNeighborFloor - 2); r < fRow; r++)
            {
                if (chunk.GetTile(c, r) == TileType.Solid)
                {
                    chunk.SetTile(c, r, TileType.Empty);
                }
            }
        }

        // 5. Remove any accidental twin-ladder-sandwiching-a-solid-pillar artifact and dead-end ladder caps
        CleanLadderHygiene(chunk);

        // 6. Eliminate any 1-tile vertical pinch pockets where a surface sits only 1 empty row under a Solid ceiling
        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            if (chunk.ChunkIndex == 0 && c == 0) continue; // Keep left boundary border wall at col 0 intact

            for (int r = 3; r < TileChunk.ChunkHeight - 2; r++)
            {
                TileType surface = chunk.GetTile(c, r);
                if (surface is TileType.Solid or TileType.OneWayPlatform &&
                    chunk.GetTile(c, r - 1) != TileType.Solid &&
                    chunk.GetTile(c, r - 2) == TileType.Solid)
                {
                    if (surface == TileType.OneWayPlatform)
                    {
                        chunk.SetTile(c, r, TileType.Empty);
                    }
                    else if (r >= floorRows[c] || chunk.GetTile(c, r - 3) != TileType.Solid)
                    {
                        chunk.SetTile(c, r - 2, TileType.Empty);
                    }
                    else
                    {
                        chunk.SetTile(c, r, TileType.Empty);
                    }
                }
            }
        }

        // 7. Guarantee forgiving horizontal ground advance and ladder/jump access to all elevated decks
        EnsureForgivingGroundObstacles(chunk, floorRows);
        CleanLadderHygiene(chunk);
        EnsureElevatedPlatformsReachable(chunk, floorRows);
        CleanLadderHygiene(chunk);
        EnsureElevatedPlatformsReachable(chunk, floorRows);
    }

    private static void CleanLadderHygiene(TileChunk chunk)
    {
        for (int c = 1; c < TileChunk.ChunkWidth - 1; c++)
        {
            for (int r = 2; r < TileChunk.ChunkHeight - 4; r++)
            {
                if (chunk.GetTile(c, r) == TileType.Solid &&
                    chunk.GetTile(c - 1, r) == TileType.Ladder &&
                    chunk.GetTile(c + 1, r) == TileType.Ladder)
                {
                    chunk.SetTile(c + 1, r, TileType.Empty);
                }

                // Ensure ladder top exits never terminate dead-end against Solid or Crate blocks
                if (chunk.GetTile(c, r) == TileType.Ladder &&
                    chunk.GetTile(c, r - 1) != TileType.Ladder)
                {
                    TileType overhead = chunk.GetTile(c, r - 1);
                    if (overhead == TileType.Crate)
                    {
                        chunk.SetTile(c, r - 1, TileType.Empty);
                    }
                    else if (overhead == TileType.Solid)
                    {
                        bool hasDeckSide =
                            chunk.GetTile(c - 1, r - 1) is TileType.Solid or TileType.OneWayPlatform ||
                            chunk.GetTile(c + 1, r - 1) is TileType.Solid or TileType.OneWayPlatform;
                        bool openAboveDeck =
                            chunk.GetTile(c, r - 2) is TileType.Empty or TileType.OneWayPlatform &&
                            chunk.GetTile(c, r - 3) != TileType.Solid;
                        if (hasDeckSide && openAboveDeck)
                        {
                            chunk.SetTile(c, r - 1, TileType.Ladder);
                        }
                        else
                        {
                            chunk.SetTile(c, r, TileType.Empty);
                        }
                    }
                }
            }
        }
    }

    private static void EnsureForgivingGroundObstacles(TileChunk chunk, int[] floorRows)
    {
        int[] obstacleTop = new int[TileChunk.ChunkWidth];
        for (int c = 0; c < TileChunk.ChunkWidth; c++)
        {
            int top = floorRows[c];
            while (top > 1 && chunk.GetTile(c, top - 1) == TileType.Crate)
            {
                top--;
            }
            obstacleTop[c] = top;
        }

        int startCol = chunk.ChunkIndex == 0 ? 2 : 1;
        for (int pass = 0; pass < 3; pass++)
        {
            for (int c = startCol; c < TileChunk.ChunkWidth; c++)
            {
                int diff = obstacleTop[c] - obstacleTop[c - 1];
                if (Math.Abs(diff) <= 2) continue;

                bool hasAdjacentLadder =
                    chunk.GetTile(c - 1, obstacleTop[c - 1] - 1) == TileType.Ladder ||
                    chunk.GetTile(c, obstacleTop[c] - 1) == TileType.Ladder;
                if (hasAdjacentLadder) continue;

                int tallCol = diff < -2 ? c : c - 1;
                if (obstacleTop[tallCol] < floorRows[tallCol] &&
                    chunk.GetTile(tallCol, obstacleTop[tallCol]) == TileType.Crate)
                {
                    chunk.SetTile(tallCol, obstacleTop[tallCol], TileType.Empty);
                    obstacleTop[tallCol]++;
                }
            }
        }
    }

    private static void EnsureElevatedPlatformsReachable(TileChunk chunk, int[] floorRows)
    {
        for (int r = 26; r >= 4; r--)
        {
            int c = chunk.ChunkIndex == 0 ? 1 : 0;
            while (c < TileChunk.ChunkWidth)
            {
                bool isDeckTile = r < floorRows[c] &&
                                  chunk.GetTile(c, r) is TileType.OneWayPlatform or TileType.Solid &&
                                  chunk.GetTile(c, r - 1) != TileType.Solid &&
                                  chunk.GetTile(c, r - 2) != TileType.Solid;
                if (!isDeckTile)
                {
                    c++;
                    continue;
                }

                int segStart = c;
                while (c < TileChunk.ChunkWidth &&
                       r < floorRows[c] &&
                       chunk.GetTile(c, r) is TileType.OneWayPlatform or TileType.Solid &&
                       chunk.GetTile(c, r - 1) != TileType.Solid)
                {
                    c++;
                }
                int segEnd = c - 1;

                if (IsSegmentReachable(chunk, r, segStart, segEnd)) continue;

                // Try interior columns first, then edges (excluding seam col 0 / 15) to attach a connecting ladder
                int midCol = (segStart + segEnd) / 2;
                int minTry = Math.Max(1, segStart);
                int maxTry = Math.Min(TileChunk.ChunkWidth - 2, segEnd);
                for (int offset = 0; offset <= maxTry - minTry; offset++)
                {
                    int candidate = offset % 2 == 0
                        ? midCol + offset / 2
                        : midCol - (offset + 1) / 2;
                    if (candidate < minTry || candidate > maxTry) continue;
                    if (chunk.ChunkIndex == 0 && candidate <= 3) continue; // Keep starter spawn zone clean

                    // Skip solid walls below candidate so we don't drill through outpost pillars
                    int bottom = floorRows[candidate] - 1;
                    bool hitsVerticalWall = false;
                    for (int rr = r + 1; rr <= bottom; rr++)
                    {
                        TileType t = chunk.GetTile(candidate, rr);
                        if (t is TileType.OneWayPlatform or TileType.Solid)
                        {
                            // Stop ladder on top of the nearest deck/shelf underneath if there's clear space above it
                            if (rr > r + 1)
                            {
                                bottom = rr - 1;
                            }
                            else
                            {
                                hitsVerticalWall = true;
                            }
                            break;
                        }
                    }

                    if (hitsVerticalWall) continue;

                    TryPlaceSafeLadder(chunk, candidate, r, bottom);
                    if (IsSegmentReachable(chunk, r, segStart, segEnd)) break;
                }
            }
        }
    }

    private static bool IsSegmentReachable(TileChunk chunk, int r, int segStart, int segEnd)
    {
        for (int sc = Math.Max(0, segStart - 1); sc <= Math.Min(TileChunk.ChunkWidth - 1, segEnd + 1); sc++)
        {
            if (chunk.GetTile(sc, r) == TileType.Ladder || chunk.GetTile(sc, r + 1) == TileType.Ladder)
            {
                return true;
            }
        }

        for (int sc = Math.Max(0, segStart - 3); sc <= Math.Min(TileChunk.ChunkWidth - 1, segEnd + 3); sc++)
        {
            for (int sr = r + 1; sr <= Math.Min(TileChunk.ChunkHeight - 2, r + 2); sr++)
            {
                if (chunk.GetTile(sc, sr) is TileType.Solid or TileType.OneWayPlatform or TileType.Crate or TileType.Ladder)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Adds optional 1-cell-high crouch-only crawlways (Solid roof at floorRow - 2 with open >=2-tile bypass above
    /// so zero-gravity themes remain fully traversable) often sealed by shootable Crates, plus extra floor & upper crates.
    /// Uses an independent deterministic RNG stream so existing chunk layouts remain stable.
    /// </summary>
    private void TryAddCrouchCrawlwayAndExtraCrates(TileChunk chunk, int[] floorRows)
    {
        var rng = CreateChunkRng(chunk.ChunkIndex, 0x9C4E_3B71_F205_6D8AUL);

        // 1. Intentional 1-cell crouch tunnel (~65% of chunks >= 1)
        if (rng.NextBool(0.65f))
        {
            int tunnelLen = rng.NextInt(3, 5); // 3 or 4 tiles wide
            int minStart = 2;
            int maxStart = TileChunk.ChunkWidth - tunnelLen - 2;
            int spanCount = maxStart - minStart + 1;
            int offset = rng.NextInt(0, spanCount);

            for (int step = 0; step < spanCount; step++)
            {
                int startCol = minStart + ((offset + step) % spanCount);
                int baseRow = floorRows[startCol];
                if (baseRow < 8) continue;

                // Flanking entry/exit columns must allow walking right up to the tunnel mouth or jumping onto the roof
                int leftCol = startCol - 1;
                int rightCol = startCol + tunnelLen;
                if (Math.Abs(floorRows[leftCol] - baseRow) > 1 || Math.Abs(floorRows[rightCol] - baseRow) > 1) continue;
                if (chunk.GetTile(leftCol, baseRow - 1) is TileType.Solid or TileType.Ladder or TileType.Border) continue;
                if (chunk.GetTile(leftCol, baseRow - 2) is TileType.Solid or TileType.Ladder or TileType.Border) continue;
                if (chunk.GetTile(rightCol, baseRow - 1) is TileType.Solid or TileType.Ladder or TileType.Border) continue;
                if (chunk.GetTile(rightCol, baseRow - 2) is TileType.Solid or TileType.Ladder or TileType.Border) continue;

                bool canBuild = true;
                for (int c = startCol; c < startCol + tunnelLen; c++)
                {
                    if (floorRows[c] != baseRow || chunk.GetTile(c, baseRow) != TileType.Solid)
                    {
                        canBuild = false;
                        break;
                    }
                    // Inside crawlway (baseRow - 1) and roof row (baseRow - 2) must be Empty or Crate (never Solid/Ladder/OneWay)
                    if (chunk.GetTile(c, baseRow - 1) is not (TileType.Empty or TileType.Crate) ||
                        chunk.GetTile(c, baseRow - 2) is not (TileType.Empty or TileType.Crate))
                    {
                        canBuild = false;
                        break;
                    }
                    // Open >= 2-tile bypass directly above roof (baseRow - 3 and baseRow - 4)
                    if (chunk.GetTile(c, baseRow - 3) is TileType.Solid or TileType.Ladder or TileType.Border or TileType.OneWayPlatform ||
                        chunk.GetTile(c, baseRow - 4) is TileType.Solid or TileType.Ladder or TileType.Border)
                    {
                        canBuild = false;
                        break;
                    }
                }

                if (!canBuild) continue;

                // Clear interior & roof bypass, then lay the 1-tile-thick Solid tunnel roof at baseRow - 2
                for (int c = startCol; c < startCol + tunnelLen; c++)
                {
                    chunk.SetTile(c, baseRow - 1, TileType.Empty);
                    chunk.SetTile(c, baseRow - 2, TileType.Solid);
                    if (chunk.GetTile(c, baseRow - 3) == TileType.Crate)
                    {
                        chunk.SetTile(c, baseRow - 3, TileType.Empty);
                    }
                }

                // Place 1-2 shootable Crate blocks inside the crawlway so player crouches & shoots through
                int cratePos = startCol + (rng.NextBool() ? 0 : tunnelLen - 1);
                chunk.SetTile(cratePos, baseRow - 1, TileType.Crate);
                if (tunnelLen >= 4 && rng.NextBool(0.6f))
                {
                    chunk.SetTile(startCol + 1, baseRow - 1, TileType.Crate);
                }

                // Optional bonus crate sitting atop the middle of the tunnel roof (keeps 2-tile step-up at edges!)
                if (rng.NextBool(0.55f) && chunk.GetTile(startCol + 1, baseRow - 4) == TileType.Empty)
                {
                    chunk.SetTile(startCol + 1, baseRow - 3, TileType.Crate);
                }
                break;
            }
        }

        // 2. Extra scatter of shootable 1-high floor crates on clear ground columns
        int extraStacks = rng.NextInt(1, 3);
        for (int s = 0; s < extraStacks; s++)
        {
            int col = rng.NextInt(2, TileChunk.ChunkWidth - 3);
            int fRow = floorRows[col];
            if (chunk.GetTile(col, fRow) == TileType.Solid &&
                chunk.GetTile(col, fRow - 1) == TileType.Empty &&
                chunk.GetTile(col, fRow - 2) == TileType.Empty &&
                chunk.GetTile(col, fRow - 3) == TileType.Empty &&
                chunk.GetTile(col - 1, fRow - 1) != TileType.Ladder &&
                chunk.GetTile(col + 1, fRow - 1) != TileType.Ladder)
            {
                chunk.SetTile(col, fRow - 1, TileType.Crate);
                if (rng.NextBool(0.45f) && chunk.GetTile(col + 1, fRow) == TileType.Solid &&
                    chunk.GetTile(col + 1, fRow - 1) == TileType.Empty &&
                    chunk.GetTile(col + 1, fRow - 2) == TileType.Empty &&
                    chunk.GetTile(col + 1, fRow - 3) == TileType.Empty)
                {
                    chunk.SetTile(col + 1, fRow - 1, TileType.Crate);
                }
            }
        }

        // 3. Extra shootable crates atop elevated platforms
        int extraUpper = 0;
        for (int attempt = 0; attempt < 8 && extraUpper < 2; attempt++)
        {
            int c = rng.NextInt(2, TileChunk.ChunkWidth - 2);
            int r = rng.NextInt(7, 23);
            if (r >= floorRows[c] - 2) continue;

            if (chunk.GetTile(c, r) is TileType.OneWayPlatform or TileType.Solid &&
                chunk.GetTile(c, r - 1) == TileType.Empty &&
                chunk.GetTile(c, r - 2) == TileType.Empty &&
                chunk.GetTile(c, r - 3) != TileType.Solid &&
                chunk.GetTile(c - 1, r - 1) != TileType.Ladder &&
                chunk.GetTile(c + 1, r - 1) != TileType.Ladder)
            {
                chunk.SetTile(c, r - 1, TileType.Crate);
                extraUpper++;
            }
        }
    }
}
