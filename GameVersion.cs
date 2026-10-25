namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.1";
    public const string BuildTag = "Fix DEMO MODE bot navigation: solve continuous flipping, combat aim, ladder climbing, and dead-end avoidance";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
