namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.6.8";
    public const string BuildTag = "Space Invaders theme: starfighter ship, alien fleet, zero-gravity 4-way spaceflight, and arcade chiptune BGM";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
