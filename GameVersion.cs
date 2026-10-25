namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.6";
    public const string BuildTag = "Add area-stuck vault forward, crate step obstacle height calculation, facing deadband, and priority bullet dodging";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
