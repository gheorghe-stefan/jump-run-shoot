namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.2.0";
    public const string BuildTag = "Endless Platformer Prototype";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
