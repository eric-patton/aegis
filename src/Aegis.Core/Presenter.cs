namespace Aegis.Core;

/// <summary>
/// Renders game state into a <see cref="Frame"/>. Pure function of state; owns the
/// layout (header, map viewport, sidebar, message log). The map viewport flexes to
/// absorb any size beyond the 80x24 baseline; the sidebar keeps a fixed width on the
/// right and the log a fixed line count at the bottom. Below the baseline, layout is
/// computed at 80x24 and the frame simply crops.
/// </summary>
public static class Presenter
{
    public const int DefaultWidth = 80;
    public const int DefaultHeight = 24;

    private const int SidebarWidth = 23;
    private const int LogLines = 5;

    /// <summary>Computed per-render region geometry.</summary>
    private readonly record struct Layout(int MapX, int MapY, int MapW, int MapH, int SideX, int LogY)
    {
        public static Layout For(int width, int height)
        {
            int w = Math.Max(width, DefaultWidth);
            int h = Math.Max(height, DefaultHeight);
            int logY = h - LogLines;
            return new Layout(
                MapX: 0,
                MapY: 1,
                MapW: w - SidebarWidth - 2,
                MapH: logY - 2,
                SideX: w - SidebarWidth,
                LogY: logY);
        }
    }

    public static Frame Render(Game game) => Render(game, DefaultWidth, DefaultHeight);

    public static Frame Render(Game game, int width, int height)
    {
        var frame = new Frame(Math.Max(width, 1), Math.Max(height, 1));
        var layout = Layout.For(width, height);
        DrawHeader(frame, game);
        DrawMap(frame, game, layout);
        DrawSidebar(frame, game, layout);
        DrawLog(frame, game, layout);
        if (game.InShrineMenu) DrawShrineMenu(frame, game, layout);
        if (game.InTalkMenu) DrawTalkMenu(frame, game, layout);
        if (game.InUnbindMenu) DrawUnbindMenu(frame, game, layout);
        return frame;
    }

    private static void DrawTalkMenu(Frame frame, Game game, Layout layout)
    {
        var npc = game.TalkNpc;
        if (npc is null) return;

        bool unbinder = npc.Kind == NpcKind.Unbinder;
        int entries = game.Topics.Count + (unbinder ? 1 : 0);
        const int boxW = 46;
        int boxH = 5 + entries;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, unbinder
            ? $"{npc.Name}, a wandering {npc.Role}"
            : $"{npc.Name}, {npc.Role} of {game.World.SettlementName}", Hue.White);

        for (int i = 0; i < game.Topics.Count; i++)
            frame.Write(x0 + 2, y0 + 3 + i, $"{i + 1}) Ask about {game.Topics[i].Label}", Hue.Gray);
        if (unbinder)
            frame.Write(x0 + 2, y0 + 3 + game.Topics.Count,
                $"{entries}) The unbinding ({game.UnbindingsLeft} left this world)", Hue.Cyan);

        frame.Write(x0 + 2, y0 + boxH - 2, $"1-{entries} choose; any other key to part ways", Hue.DarkGray);
    }

    private static void DrawUnbindMenu(Frame frame, Game game, Layout layout)
    {
        var npc = game.TalkNpc;
        if (npc is null) return;

        const int boxW = 46;
        int boxH = 6 + AttributeSet.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, $"The unbinding | {npc.Name} the {npc.Role}", Hue.White);
        frame.Write(x0 + 2, y0 + 2, game.Player.Attributes.TotalRaises > 0
            ? $"Loosening returns {game.UnbindRefund} essence   {game.UnbindingsLeft} left"
            : "Nothing to loosen: you are as you began", Hue.Cyan);

        for (int i = 0; i < AttributeSet.Count; i++)
        {
            var attr = (Attr)i;
            bool bound = game.Player.Attributes[attr] > AttributeSet.Baseline;
            frame.Write(x0 + 2, y0 + 4 + i,
                $"{i + 1}) {AttributeSet.NameOf(attr),-9} {game.Player.Attributes[attr],2}",
                bound ? Hue.White : Hue.DarkGray);
        }

        frame.Write(x0 + 2, y0 + boxH - 2, "1-7 loosen; any other key to part ways", Hue.DarkGray);
    }

    private static void DrawBox(Frame frame, int x0, int y0, int boxW, int boxH)
    {
        for (int y = 0; y < boxH; y++)
            for (int x = 0; x < boxW; x++)
            {
                bool border = y == 0 || y == boxH - 1 || x == 0 || x == boxW - 1;
                frame.Put(x0 + x, y0 + y, border ? (y is 0 || y == boxH - 1 ? '-' : '|') : ' ',
                    border ? Hue.Cyan : Hue.Gray, Hue.Black);
            }
        frame.Put(x0, y0, '+', Hue.Cyan);
        frame.Put(x0 + boxW - 1, y0, '+', Hue.Cyan);
        frame.Put(x0, y0 + boxH - 1, '+', Hue.Cyan);
        frame.Put(x0 + boxW - 1, y0 + boxH - 1, '+', Hue.Cyan);
    }

    private static void DrawShrineMenu(Frame frame, Game game, Layout layout)
    {
        const int boxW = 42;
        int boxH = 6 + AttributeSet.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, $"The Shrine of {game.World.SettlementName}", Hue.White);
        frame.Write(x0 + 2, y0 + 2, $"Essence {game.Player.Essence}   next raise costs {game.NextRaiseCost}", Hue.Cyan);

        for (int i = 0; i < AttributeSet.Count; i++)
        {
            var attr = (Attr)i;
            bool affordable = game.Player.Essence >= game.NextRaiseCost;
            frame.Write(x0 + 2, y0 + 4 + i,
                $"{i + 1}) {AttributeSet.NameOf(attr),-9} {game.Player.Attributes[attr],2}",
                affordable ? Hue.White : Hue.DarkGray);
        }

        frame.Write(x0 + 2, y0 + boxH - 2, "1-7 raise; any other key to rise", Hue.DarkGray);
    }

    private static void DrawHeader(Frame frame, Game game)
    {
        string header = $" AEGIS | {game.World.Name} | {game.World.SettlementName} | Cycle {game.Cycle} | T{game.Turn}";
        frame.Write(0, 0, header.PadRight(frame.Width), Hue.Black, Hue.DarkCyan);
    }

    private static void DrawMap(Frame frame, Game game, Layout layout)
    {
        var map = game.CurrentMap;
        var (mapX, mapY, mapW, mapH) = (layout.MapX, layout.MapY, layout.MapW, layout.MapH);

        int camX = Math.Clamp(game.Player.Pos.X - mapW / 2, 0, Math.Max(0, map.Width - mapW));
        int camY = Math.Clamp(game.Player.Pos.Y - mapH / 2, 0, Math.Max(0, map.Height - mapH));

        for (int sy = 0; sy < mapH; sy++)
        {
            for (int sx = 0; sx < mapW; sx++)
            {
                var p = new Pos(camX + sx, camY + sy);
                if (!map.InBounds(p)) continue;
                var (ch, fg, bg) = Glyph(map[p]);
                frame.Put(mapX + sx, mapY + sy, ch, fg, bg);
            }
        }

        void PutWorld(Pos p, char ch, Hue fg, Hue bg = Hue.Black)
        {
            int sx = p.X - camX, sy = p.Y - camY;
            if (sx >= 0 && sx < mapW && sy >= 0 && sy < mapH)
                frame.Put(mapX + sx, mapY + sy, ch, fg, bg);
        }

        // Telegraphed intent cells: the readable danger the combat design runs on.
        foreach (var monster in game.LiveMonstersHere)
            if (monster.Intent is { } intent)
                PutWorld(intent.TargetCell, '!', Hue.White, Hue.DarkRed);

        if (game.Remnant is { } remnant && remnant.MapId == map.Id)
            PutWorld(remnant.Pos, '%', Hue.Magenta);

        if (game.CurrentSite is { ChestLooted: false } site)
            PutWorld(site.ChestPos, '$', Hue.Yellow);

        if (game.Mode == MapMode.Overworld)
            foreach (var npc in game.World.Npcs)
                PutWorld(npc.Pos, 'p', Hue.Green);

        foreach (var monster in game.LiveMonstersHere)
        {
            char ch = monster.Kind == MonsterKind.Wight ? 'w' : 'g';
            var calm = monster.Kind == MonsterKind.Wight ? Hue.Cyan : Hue.Red;
            PutWorld(monster.Pos, ch, monster.Intent is null ? calm : Hue.White,
                monster.Intent is null ? Hue.Black : Hue.DarkRed);
        }

        PutWorld(game.Player.Pos, '@', Hue.White);
    }

    private static (char, Hue, Hue) Glyph(Terrain t) => t switch
    {
        Terrain.Grass => ('.', Hue.DarkGreen, Hue.Black),
        Terrain.Forest => ('&', Hue.Green, Hue.Black),
        Terrain.Hills => ('^', Hue.DarkYellow, Hue.Black),
        Terrain.Water => ('~', Hue.Blue, Hue.Black),
        Terrain.House => ('#', Hue.Yellow, Hue.Black),
        Terrain.Shrine => ('+', Hue.Cyan, Hue.Black),
        Terrain.CampEntrance => ('>', Hue.Red, Hue.Black),
        Terrain.Wall => ('#', Hue.DarkGray, Hue.Black),
        Terrain.Floor => ('.', Hue.Gray, Hue.Black),
        Terrain.ExitLadder => ('<', Hue.White, Hue.Black),
        Terrain.Waygate => ('O', Hue.Magenta, Hue.Black),
        Terrain.BarrowEntrance => ('n', Hue.DarkYellow, Hue.Black),
        _ => ('?', Hue.Magenta, Hue.Black),
    };

    private static void DrawSidebar(Frame frame, Game game, Layout layout)
    {
        var p = game.Player;
        int y = 1;
        void Line(string text, Hue fg = Hue.Gray)
        {
            if (y < layout.LogY - 1) frame.Write(layout.SideX, y, text, fg);
            y++;
        }

        Line("The Bearer", Hue.White);
        Line(new string('-', 22), Hue.DarkGray);
        Line($"HP  {Bar(p.Hp, p.EffectiveMaxHp, 10)} {p.Hp}/{p.EffectiveMaxHp}", p.Hp * 3 <= p.EffectiveMaxHp ? Hue.Red : Hue.Gray);
        Line($"ST  {Bar(p.Stamina, p.MaxStamina, 10)} {p.Stamina}/{p.MaxStamina}", Hue.Gray);
        Line($"Coin    {p.Coin}", Hue.Yellow);
        Line($"Essence {p.Essence}", Hue.Cyan);
        if (p.Legend > 0) Line($"Legend  {p.Legend}", Hue.Magenta);
        if (p.WoundedTurns > 0) Line($"WOUNDED ({p.WoundedTurns})", Hue.Red);
        y++;

        if (game.Mode == MapMode.Site)
        {
            Line(game.CurrentSite!.Kind == SiteKind.Barrow ? "The barrow" : "Goblin cave", Hue.White);
            int alive = game.LiveMonstersHere.Count();
            Line($"Foes here: {alive}", alive > 0 ? Hue.Red : Hue.DarkGreen);
            foreach (var monster in game.LiveMonstersHere.Where(m => m.Intent is not null))
                Line(monster.Intent!.Kind == IntentKind.BarrowBlade
                    ? "! barrow blade poised" : "! crushing blow poised", Hue.Red);
        }
        else
        {
            Line(game.World.SettlementName, Hue.White);
            var here = game.CurrentMap[p.Pos];
            if (here == Terrain.Shrine) Line("At the shrine: r rests", Hue.Cyan);
            if (here == Terrain.CampEntrance) Line("Cave mouth: > enters", Hue.Red);
            if (here == Terrain.BarrowEntrance) Line("Barrow mouth: > enters", Hue.DarkYellow);
            if (here == Terrain.Waygate)
                Line(game.CampCleared ? "Waygate hums: > crosses" : "Waygate: shut", Hue.Magenta);
            foreach (var npc in game.World.Npcs)
                if (npc.Pos.Chebyshev(p.Pos) == 1)
                    Line($"{npc.Name}: bump to talk", Hue.Green);
        }

        if (game.Remnant is { } remnant)
        {
            y++;
            Line("Remnant dropped:", Hue.Magenta);
            Line($" {remnant.Coin}c {remnant.Essence}e ({remnant.MapId})", Hue.Magenta);
        }

        y++;
        Line("hjkl/yubn move  . wait", Hue.DarkGray);
        Line("g grab  >/< enter/exit", Hue.DarkGray);
        Line("q quit", Hue.DarkGray);
    }

    private static string Bar(int value, int max, int slots)
    {
        int filled = max <= 0 ? 0 : Math.Clamp(value * slots / max, 0, slots);
        return new string('=', filled) + new string(' ', slots - filled);
    }

    private static void DrawLog(Frame frame, Game game, Layout layout)
    {
        int logY = layout.LogY;
        frame.Write(0, logY - 1, new string('-', frame.Width), Hue.DarkGray);
        int y = logY;
        foreach (var entry in game.Log.Recent(LogLines))
        {
            var fg = entry.Tone switch
            {
                LogTone.Aegis => Hue.Cyan,
                LogTone.Danger => Hue.Red,
                LogTone.Reward => Hue.Yellow,
                LogTone.Combat => Hue.Gray,
                _ => Hue.Gray,
            };
            string text = entry.Text.Length > frame.Width ? entry.Text[..frame.Width] : entry.Text;
            frame.Write(0, y++, text, fg);
        }
    }
}
