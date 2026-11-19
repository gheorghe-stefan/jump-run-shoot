namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.25";
    public const string BuildTag = "Align HeavyGuard (bear) bullet Y with grounded player shots on gravity levels for head-on cancellation";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
