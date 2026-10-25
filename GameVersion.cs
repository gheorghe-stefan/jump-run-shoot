namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.0";
    public const string BuildTag = "Add DEMO MODE: autonomous player bot with 5s countdown, bullet budgeting per monster, obstacle navigation, and Y/N exit confirmation";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
