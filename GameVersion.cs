namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.3.2";
    public const string BuildTag = "Duke Nukem 1 Map Archetypes & Crates";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
