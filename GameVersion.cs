namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.2";
    public const string BuildTag = "Prioritize health pickups below threshold, add LOS check against walls for monster targeting, and fix barrier/crate wall navigation";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
