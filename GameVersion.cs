namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.6";
    public const string BuildTag = "Enter Key Seed Submission & Canvas Autofocus";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
