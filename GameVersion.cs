namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.6.5";
    public const string BuildTag = "Clean in-tune 90s DOS music tracks for Duke, Forest, and Underwater without pitch wobble";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
