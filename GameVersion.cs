namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.4";
    public const string BuildTag = "Add monster proximity evasion, kiting, and obstacle vaulting to prevent bot from getting stuck on enemies";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
