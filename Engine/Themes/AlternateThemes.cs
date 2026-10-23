namespace JumpRunShoot.Engine.Themes;

public static class ThemeRegistry
{
    public static readonly IGameTheme[] AllThemes =
    {
        new DefaultTheme(),
        new ForestHunterTheme(),
        new UnderwaterTheme(),
        new SpaceInvadersTheme()
    };
}
