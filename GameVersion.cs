namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.8";
    public const string BuildTag = "Fix zero-gravity area-stuck handling, ceiling-blocked altitude alignment, and touch hazard sensitivity";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
