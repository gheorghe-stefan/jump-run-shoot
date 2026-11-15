namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.17";
    public const string BuildTag = "Prevent 1-tile pinched pickup alcoves and guarantee >=2-tile spawn headroom";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
