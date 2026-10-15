namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.3.8";
    public const string BuildTag = "Web Audio API Retro Sound Effects";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
