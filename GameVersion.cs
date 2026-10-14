namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.3.1";
    public const string BuildTag = "Climbing Ladders & Platform Fixes";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
