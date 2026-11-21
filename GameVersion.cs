namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.26";
    public const string BuildTag = "Upgraded Polar Bear & climbing penguin back art, fuller cell monster shooting hitboxes, and more shootable crates";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
