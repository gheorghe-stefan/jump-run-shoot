namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.5";
    public const string BuildTag = "Add exact horizontal raycast clearance, Y-alignment aiming, jump-aiming for elevated targets, and HeavyGuard under-bullet sweet spot";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
