using JumpRunShoot.Engine.Core;
using JumpRunShoot.Engine.Themes;
using Xunit;

namespace JumpRunShoot.Tests;

public class LaunchParametersTests
{
    [Fact]
    public void ThemeRegistry_SlugsAlignOneToOneWithThemes()
    {
        Assert.Equal(ThemeRegistry.AllThemes.Length, ThemeRegistry.ThemeSlugs.Length);
        Assert.Equal("duke", ThemeRegistry.GetSlug(0));
        Assert.Equal("forest", ThemeRegistry.GetSlug(1));
        Assert.Equal("ocean", ThemeRegistry.GetSlug(2));
        Assert.Equal("space", ThemeRegistry.GetSlug(3));
        Assert.Equal("polar", ThemeRegistry.GetSlug(4));
        Assert.Equal("duke", ThemeRegistry.GetSlug(-1));
        Assert.Equal("duke", ThemeRegistry.GetSlug(999));
    }

    [Theory]
    [InlineData("duke", 0)]
    [InlineData("DUKE", 0)]
    [InlineData("forest", 1)]
    [InlineData("Forest", 1)]
    [InlineData("ocean", 2)]
    [InlineData("OCEAN", 2)]
    [InlineData("underwater", 2)]
    [InlineData("space", 3)]
    [InlineData("polar", 4)]
    [InlineData("  POLAR  ", 4)]
    [InlineData("Polar Adventure", 4)]
    public void ThemeRegistry_TryResolveThemeIndex_MatchesKeywordsCaseInsensitively(string input, int expectedIndex)
    {
        Assert.True(ThemeRegistry.TryResolveThemeIndex(input, out int resolved));
        Assert.Equal(expectedIndex, resolved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("4")]
    [InlineData("invalid_theme")]
    public void ThemeRegistry_TryResolveThemeIndex_RejectsNumbersAndUnknownValues(string? input)
    {
        Assert.False(ThemeRegistry.TryResolveThemeIndex(input, out _));
    }

    [Fact]
    public void ParseQuery_ParsesSeedAndThemeInAnyOrderWithDecodingAndFragments()
    {
        var q1 = LaunchParameters.ParseQuery("https://example.com/jump-run-shoot/?seed=PENGUIN99&theme=polar#top");
        Assert.Equal("PENGUIN99", q1.Seed);
        Assert.Equal(4, q1.ThemeIndex);

        var q2 = LaunchParameters.ParseQuery("https://example.com/?THEME=OCEAN&SEED=MY%20WORLD");
        Assert.Equal("MY WORLD", q2.Seed);
        Assert.Equal(2, q2.ThemeIndex);

        var q3 = LaunchParameters.ParseQuery("https://example.com/?theme=space");
        Assert.Null(q3.Seed);
        Assert.Equal(3, q3.ThemeIndex);

        var q4 = LaunchParameters.ParseQuery("https://example.com/?seed=ABC123&theme=4");
        Assert.Equal("ABC123", q4.Seed);
        Assert.Null(q4.ThemeIndex); // Numeric theme rejected
    }

    [Fact]
    public void ResolveStartupState_QueryParamsOverrideSavedLocalStorage()
    {
        var query = LaunchParameters.ParseQuery("https://example.com/?seed=NEWSEED&theme=polar");
        var (seed, themeIdx) = LaunchParameters.ResolveStartupState(query, savedSeed: "OLDSEED", savedTheme: "forest");

        Assert.Equal("NEWSEED", seed);
        Assert.Equal(4, themeIdx);
    }

    [Fact]
    public void ResolveStartupState_PartialQueryOverridesOnlyProvidedFieldAndRestoresOtherFromSaved()
    {
        var themeOnly = LaunchParameters.ParseQuery("https://example.com/?theme=space");
        var res1 = LaunchParameters.ResolveStartupState(themeOnly, savedSeed: "SAVED99", savedTheme: "forest");
        Assert.Equal("SAVED99", res1.Seed);
        Assert.Equal(3, res1.ThemeIndex);

        var seedOnly = LaunchParameters.ParseQuery("https://example.com/?seed=FRESH01");
        var res2 = LaunchParameters.ResolveStartupState(seedOnly, savedSeed: "SAVED99", savedTheme: "ocean");
        Assert.Equal("FRESH01", res2.Seed);
        Assert.Equal(2, res2.ThemeIndex);
    }

    [Fact]
    public void ResolveStartupState_WithoutQueryParams_RestoresLocalStorageThenDefaults()
    {
        var emptyQuery = LaunchParameters.ParseQuery("https://example.com/jump-run-shoot/");

        var restored = LaunchParameters.ResolveStartupState(emptyQuery, savedSeed: "FAV_SEED", savedTheme: "polar");
        Assert.Equal("FAV_SEED", restored.Seed);
        Assert.Equal(4, restored.ThemeIndex);

        var defaults = LaunchParameters.ResolveStartupState(emptyQuery, savedSeed: "", savedTheme: "");
        Assert.Equal(GameEngine.DefaultSeed, defaults.Seed);
        Assert.Equal(0, defaults.ThemeIndex);
    }
}
