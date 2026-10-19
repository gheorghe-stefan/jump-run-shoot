namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.0";
    public const string BuildTag = "EGA Theme Art, Death Screen & UI Polish";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
