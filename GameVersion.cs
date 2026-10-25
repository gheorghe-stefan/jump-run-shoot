namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.7";
    public const string BuildTag = "Fix zero-gravity ceiling-aware bullet evasion, rear medkit fleeing threshold, and jellyfish combat standoff";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
