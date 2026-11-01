namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.13";
    public const string BuildTag = "Responsive screen scaling, fullscreen mode, widescreen dashboard, and complete zero-gravity boot removal";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
