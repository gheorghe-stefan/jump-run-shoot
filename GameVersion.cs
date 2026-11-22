namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.29";
    public const string BuildTag = "Fix spike-pit solid ground clipping, remove solid 4px fall-through bypass, and safe abyss bounce";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
