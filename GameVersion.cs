namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.5";
    public const string BuildTag = "Crate Solid Blocking, Dedicated P-Key Pause & U-Key Powerup Cheat";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
