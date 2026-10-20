namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.4";
    public const string BuildTag = "Monster Anti-Jitter AI, Wide Platform Spawning & Pickup Separation";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
