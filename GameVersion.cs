namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.4.1";
    public const string BuildTag = "Live Theme Combobox & Forest Pack";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
