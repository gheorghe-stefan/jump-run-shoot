namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.3.9";
    public const string BuildTag = "Data-Driven Sprite Themes & Swappable Asset Packs";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
