namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.21";
    public const string BuildTag = "Fix invulnerability edge-case deaths (abyss, shield expiry, pickup order) and bullet-vs-bullet cancellation";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
