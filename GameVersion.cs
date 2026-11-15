namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.15";
    public const string BuildTag = "Polar Adventure high-detail EGA art set, sweeping aurora borealis parallax, and 4-frame monster animations";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
