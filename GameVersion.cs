namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.30";
    public const string BuildTag = "Crouch-walk feet animation, air-crouch/crouch-jump into tunnels, spaced ceiling seals, and compact dot-decimal shield counter";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
