namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.28";
    public const string BuildTag = "Crouch mode (all gravity themes) with crouch tunnels, standing polar bear, bigger elk, throw animation, and ladder/elk fixes";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
