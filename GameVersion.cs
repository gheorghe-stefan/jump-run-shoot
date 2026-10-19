namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.4.4";
    public const string BuildTag = "On-Disk PNG Theme Assets";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
