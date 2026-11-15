namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.16";
    public const string BuildTag = "64-bit SplitMix64 procedural world generator with continuous elevation, 6 macro zones, and traversal validation";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
