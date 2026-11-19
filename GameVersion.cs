namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.20";
    public const string BuildTag = "Bookmarkable ?seed=&theme= URL keywords (duke, forest, ocean, space, polar) with localStorage persistence";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
