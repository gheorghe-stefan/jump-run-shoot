namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.7.31";
    public const string BuildTag = "Replace Speed Dash with universal Piercing Shots (custom badges for all 5 themes) and add Zero-G Item Magnet / Tractor Beam";

    public static string DisplayVersion => $"v{Version}";
    public static string FullDisplay => $"v{Version}";
}
