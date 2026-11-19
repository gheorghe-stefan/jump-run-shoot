namespace JumpRunShoot.Engine.Themes;

public static class ThemeRegistry
{
    public static readonly IGameTheme[] AllThemes =
    {
        new DefaultTheme(),
        new ForestHunterTheme(),
        new UnderwaterTheme(),
        new SpaceInvadersTheme(),
        new PolarAdventureTheme()
    };

    /// <summary>
    /// Stable URL/storage keywords aligned 1-to-1 with <see cref="AllThemes"/>.
    /// </summary>
    public static readonly string[] ThemeSlugs =
    {
        "duke",
        "forest",
        "ocean",
        "space",
        "polar"
    };

    public static string GetSlug(int themeIndex)
    {
        if ((uint)themeIndex < (uint)ThemeSlugs.Length)
        {
            return ThemeSlugs[themeIndex];
        }
        return ThemeSlugs[0];
    }

    /// <summary>
    /// Resolves a theme keyword (duke, forest, ocean, space, polar) or display name to its index.
    /// Raw numbers are intentionally not matched so URLs stay human-readable keywords.
    /// </summary>
    public static bool TryResolveThemeIndex(string? value, out int themeIndex)
    {
        themeIndex = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        string normalized = value.Trim();

        for (int i = 0; i < ThemeSlugs.Length; i++)
        {
            if (normalized.Equals(ThemeSlugs[i], StringComparison.OrdinalIgnoreCase))
            {
                themeIndex = i;
                return true;
            }
        }

        if (normalized.Equals("underwater", StringComparison.OrdinalIgnoreCase))
        {
            themeIndex = 2;
            return true;
        }

        for (int i = 0; i < AllThemes.Length; i++)
        {
            if (normalized.Equals(AllThemes[i].Name, StringComparison.OrdinalIgnoreCase))
            {
                themeIndex = i;
                return true;
            }
        }

        return false;
    }
}
