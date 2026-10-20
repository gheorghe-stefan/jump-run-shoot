namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.9";
    public const string BuildTag = "Flight Arrows & WASD Support";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
