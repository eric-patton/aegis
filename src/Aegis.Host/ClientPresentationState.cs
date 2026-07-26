namespace Aegis.Host;

using Aegis.Core;

public enum WorldRailPresentation
{
    Docked,
    Drawer,
}

public enum ConversationPresentation
{
    Split,
    Stacked,
}

public enum WorldDirectionalKey
{
    Up,
    Down,
    Left,
    Right,
}

public static class WorldInputChord
{
    public static char Map(WorldDirectionalKey key, bool control, bool alt)
    {
        if (control && key == WorldDirectionalKey.Left) return 'y';
        if (control && key == WorldDirectionalKey.Right) return 'u';
        if (alt && key == WorldDirectionalKey.Left) return 'b';
        if (alt && key == WorldDirectionalKey.Right) return 'n';
        return key switch
        {
            WorldDirectionalKey.Up => 'k',
            WorldDirectionalKey.Down => 'j',
            WorldDirectionalKey.Left => 'h',
            _ => 'l',
        };
    }
}

public readonly record struct ResponsiveClientLayout(
    WorldRailPresentation WorldRail,
    ConversationPresentation Conversation)
{
    public static ResponsiveClientLayout Resolve(int viewportWidth, float uiScale)
    {
        bool collapseSecondaryColumns = viewportWidth < 1220 || uiScale >= 1.5f;
        return new ResponsiveClientLayout(
            collapseSecondaryColumns
                ? WorldRailPresentation.Drawer
                : WorldRailPresentation.Docked,
            collapseSecondaryColumns
                ? ConversationPresentation.Stacked
                : ConversationPresentation.Split);
    }
}

public sealed record WorldHudPresentation(
    string PlayerName,
    int Health,
    int MaxHealth,
    int Stamina,
    int MaxStamina,
    int Focus,
    int MaxFocus,
    int Coin,
    int Essence,
    int Rations,
    int Cycle,
    int Turn,
    string Season,
    string Weather,
    string WorldName,
    string SettlementName)
{
    public static WorldHudPresentation From(Game game)
    {
        Player player = game.Player;
        return new WorldHudPresentation(
            player.Name.Length > 0 ? player.Name : "The bearer",
            player.Hp,
            player.EffectiveMaxHp,
            player.Stamina,
            player.MaxStamina,
            player.Focus,
            player.MaxFocus,
            player.Coin,
            player.Essence,
            player.Rations,
            game.Cycle,
            game.Turn,
            game.Season.ToString(),
            game.WeatherRead(game.LocalClimate),
            game.World.Name,
            game.World.SettlementName);
    }
}

public enum ActivityFilter
{
    All,
    Field,
    Combat,
    Words,
}

public static class ActivityLog
{
    public static bool Includes(ActivityFilter filter, LogTone tone) => filter switch
    {
        ActivityFilter.Field => tone is LogTone.Info or LogTone.Reward,
        ActivityFilter.Combat => tone is LogTone.Combat or LogTone.Danger,
        ActivityFilter.Words => tone == LogTone.Aegis,
        _ => true,
    };
}

public static class MapZoom
{
    public const int MinimumIndex = -2;
    public const int MaximumIndex = 4;

    public static int ClampIndex(int index) => Math.Clamp(index, MinimumIndex, MaximumIndex);

    public static float Factor(int index) => ClampIndex(index) switch
    {
        -2 => 0.75f,
        -1 => 0.875f,
        1 => 1.25f,
        2 => 1.5f,
        3 => 1.75f,
        4 => 2f,
        _ => 1f,
    };

    public static int Percent(int index) => (int)MathF.Round(Factor(index) * 100);
}

public sealed class FollowTailState
{
    public bool Following { get; private set; } = true;
    public bool HasNewEntries { get; private set; }
    public int EntryCount { get; private set; }

    public void Open(int entryCount)
    {
        EntryCount = Math.Max(0, entryCount);
        Following = true;
        HasNewEntries = false;
    }

    public void EntriesChanged(int entryCount)
    {
        int normalized = Math.Max(0, entryCount);
        if (normalized > EntryCount && !Following)
            HasNewEntries = true;
        EntryCount = normalized;
    }

    public void UserScrolled(double value, double maximum, double page)
    {
        double bottom = Math.Max(0, maximum - page);
        Following = value >= bottom - 2;
        if (Following)
            HasNewEntries = false;
    }

    public void Resume()
    {
        Following = true;
        HasNewEntries = false;
    }
}

public readonly record struct NormalizedFloatingPosition(float X, float Y)
{
    public static NormalizedFloatingPosition Default => new(0, 1);

    public static NormalizedFloatingPosition FromPixels(
        float x,
        float y,
        float availableWidth,
        float availableHeight)
    {
        return new NormalizedFloatingPosition(
            availableWidth > 0 ? Math.Clamp(x / availableWidth, 0, 1) : 0,
            availableHeight > 0 ? Math.Clamp(y / availableHeight, 0, 1) : 0);
    }

    public (float X, float Y) ToPixels(float availableWidth, float availableHeight)
    {
        return (
            Math.Clamp(X, 0, 1) * Math.Max(0, availableWidth),
            Math.Clamp(Y, 0, 1) * Math.Max(0, availableHeight));
    }
}
