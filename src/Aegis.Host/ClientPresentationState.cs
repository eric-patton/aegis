namespace Aegis.Host;

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
        bool collapseSecondaryColumns = viewportWidth < 1400 || uiScale >= 1.5f;
        return new ResponsiveClientLayout(
            collapseSecondaryColumns
                ? WorldRailPresentation.Drawer
                : WorldRailPresentation.Docked,
            collapseSecondaryColumns
                ? ConversationPresentation.Stacked
                : ConversationPresentation.Split);
    }
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
