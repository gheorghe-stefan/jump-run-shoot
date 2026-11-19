using JumpRunShoot.Engine.Themes;

namespace JumpRunShoot.Engine.Core;

/// <summary>
/// Parsed startup query parameters extracted from a browser URL.
/// </summary>
public readonly record struct ParsedLaunchQuery(string? Seed, int? ThemeIndex);

/// <summary>
/// Parses bookmarkable ?seed=...&amp;theme=... query parameters and resolves startup precedence
/// against localStorage saved preferences.
/// </summary>
public static class LaunchParameters
{
    public static ParsedLaunchQuery ParseQuery(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return new ParsedLaunchQuery(null, null);
        }

        int queryIdx = uri.IndexOf('?');
        if (queryIdx < 0 || queryIdx >= uri.Length - 1)
        {
            return new ParsedLaunchQuery(null, null);
        }

        string queryStr = uri.Substring(queryIdx + 1);
        int hashIdx = queryStr.IndexOf('#');
        if (hashIdx >= 0)
        {
            queryStr = queryStr.Substring(0, hashIdx);
        }

        string? seed = null;
        int? themeIndex = null;

        foreach (string part in queryStr.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eqIdx = part.IndexOf('=');
            if (eqIdx <= 0) continue;

            string rawKey = part.Substring(0, eqIdx);
            string rawVal = part.Substring(eqIdx + 1);

            string key = DecodeComponent(rawKey).Trim();
            string val = DecodeComponent(rawVal).Trim();

            if (key.Equals("seed", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(val))
            {
                seed = val;
            }
            else if (key.Equals("theme", StringComparison.OrdinalIgnoreCase) &&
                     ThemeRegistry.TryResolveThemeIndex(val, out int resolvedIdx))
            {
                themeIndex = resolvedIdx;
            }
        }

        return new ParsedLaunchQuery(seed, themeIndex);
    }

    /// <summary>
    /// Resolves startup seed and theme index with precedence:
    /// explicit query parameter &gt; localStorage saved value &gt; engine defaults ("FOO", duke/0).
    /// </summary>
    public static (string Seed, int ThemeIndex) ResolveStartupState(
        ParsedLaunchQuery query,
        string? savedSeed,
        string? savedTheme)
    {
        string seed = !string.IsNullOrWhiteSpace(query.Seed)
            ? query.Seed.Trim()
            : !string.IsNullOrWhiteSpace(savedSeed)
                ? savedSeed.Trim()
                : GameEngine.DefaultSeed;

        int themeIndex = 0;
        if (query.ThemeIndex.HasValue)
        {
            themeIndex = query.ThemeIndex.Value;
        }
        else if (ThemeRegistry.TryResolveThemeIndex(savedTheme, out int savedIdx))
        {
            themeIndex = savedIdx;
        }

        return (seed, themeIndex);
    }

    private static string DecodeComponent(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        try
        {
            return Uri.UnescapeDataString(raw.Replace('+', ' '));
        }
        catch (UriFormatException)
        {
            return raw;
        }
    }
}
