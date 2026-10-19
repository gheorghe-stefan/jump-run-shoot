namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.4.3";
    public const string BuildTag = "Forest 3-Layer Parallax Art";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
