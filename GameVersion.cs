namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.13";
    public const string BuildTag = "Bidirectional Entity Streaming Verification";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
