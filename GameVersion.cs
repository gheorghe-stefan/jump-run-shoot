namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.22";
    public const string BuildTag = "Fix Polar Adventure walrus/elk/penguin sprite seams and seamless black night sky behind aurora & moon";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
