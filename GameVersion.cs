namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.3.3";
    public const string BuildTag = "Revamped High-Detail EGA Visuals";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
