namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.3";
    public const string BuildTag = "Seamless Platform Textures, Hall of Fame, Alert AI & AdLib Music";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
