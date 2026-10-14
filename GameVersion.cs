namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.3.6";
    public const string BuildTag = "Level Reachability, Dynamic Seeds & Powerup HUD";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
