namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.14";
    public const string BuildTag = "Polar Adventure theme: Arctic parallax, penguin hero, Arctic monsters, powerups, and circus polka chiptune BGM";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
