namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.7";
    public const string BuildTag = "Enlarge Pause Dialog Box";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
