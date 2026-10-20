namespace JumpRunShoot;

/// <summary>
/// Centralized version information displayed in the frontend and bumped with commits.
/// </summary>
public static class GameVersion
{
    public const string Version = "0.5.2";
    public const string BuildTag = "Local Hall of Fame, Distance Difficulty, Alert AI, 8 Chunk Archetypes & AdLib BGM";

    public static string FullDisplay => $"v{Version} ({BuildTag})";
}
