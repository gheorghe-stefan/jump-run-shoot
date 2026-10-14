namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.3.4";
    public const string BuildTag = "Duke Nukem 1 Solid EGA Aesthetics";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
