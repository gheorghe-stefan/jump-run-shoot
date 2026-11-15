namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.19";
    public const string BuildTag = "Easier traversal with 1-tile terrain slopes, <=2-tile obstacle steps, and guaranteed ladder/jump platform access";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
