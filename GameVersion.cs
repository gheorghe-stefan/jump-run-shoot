namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.8";
    public const string BuildTag = "Flight WASD & NoClip Cheats";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
