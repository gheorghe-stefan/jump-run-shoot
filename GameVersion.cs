namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.1";
    public const string BuildTag = "8-Pip HP, Escapeable Pits, Random Elevation Spawns, Seed Cheats & Player Profile";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
