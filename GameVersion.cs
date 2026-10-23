namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.6.7";
    public const string BuildTag = "Temporary 10s invulnerability shield, screen red hue, upscaled deep-chunk pickups, and enhanced powerup icons";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
