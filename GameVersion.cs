namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.10";
    public const string BuildTag = "Ceiling Collision Fix & Linear Distance Monster & Score Scaling";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
