namespace Aegis.Core;

/// <summary>
/// Renders game state into a <see cref="Frame"/>. Pure function of state; owns the
/// layout (header, map viewport, sidebar, message log) at a fixed 80x24 baseline.
/// </summary>
public static class Presenter
{
    public const int DefaultWidth = 80;
    public const int DefaultHeight = 24;

    private const int MapX = 0;
    private const int MapY = 1;
    private const int MapW = 55;
    private const int MapH = 17;
    private const int SideX = 57;
    private const int LogY = MapY + MapH + 1;

    public static Frame Render(Game game) => Render(game, DefaultWidth, DefaultHeight);

    public static Frame Render(Game game, int width, int height)
    {
        var frame = new Frame(width, height);
        DrawHeader(frame, game);
        DrawMap(frame, game);
        DrawSidebar(frame, game);
        DrawLog(frame, game, height);
        return frame;
    }

    private static void DrawHeader(Frame frame, Game game)
    {
        string header = $" AEGIS | {game.World.Name} | {game.World.SettlementName} | T{game.Turn}";
        frame.Write(0, 0, header.PadRight(frame.Width), Hue.Black, Hue.DarkCyan);
    }

    private static void DrawMap(Frame frame, Game game)
    {
        var map = game.CurrentMap;

        int camX = Math.Clamp(game.Player.Pos.X - MapW / 2, 0, Math.Max(0, map.Width - MapW));
        int camY = Math.Clamp(game.Player.Pos.Y - MapH / 2, 0, Math.Max(0, map.Height - MapH));

        for (int sy = 0; sy < MapH; sy++)
        {
            for (int sx = 0; sx < MapW; sx++)
            {
                var p = new Pos(camX + sx, camY + sy);
                if (!map.InBounds(p)) continue;
                var (ch, fg, bg) = Glyph(map[p]);
                frame.Put(MapX + sx, MapY + sy, ch, fg, bg);
            }
        }

        void PutWorld(Pos p, char ch, Hue fg, Hue bg = Hue.Black)
        {
            int sx = p.X - camX, sy = p.Y - camY;
            if (sx >= 0 && sx < MapW && sy >= 0 && sy < MapH)
                frame.Put(MapX + sx, MapY + sy, ch, fg, bg);
        }

        // Telegraphed intent cells: the readable danger the combat design runs on.
        foreach (var monster in game.LiveMonstersHere)
            if (monster.Intent is { } intent)
                PutWorld(intent.TargetCell, '!', Hue.White, Hue.DarkRed);

        if (game.Remnant is { } remnant && remnant.MapId == map.Id)
            PutWorld(remnant.Pos, '%', Hue.Magenta);

        if (game.Mode == MapMode.Site && !game.ChestLooted)
            PutWorld(game.World.ChestPos, '$', Hue.Yellow);

        foreach (var monster in game.LiveMonstersHere)
            PutWorld(monster.Pos, 'g', monster.Intent is null ? Hue.Red : Hue.White,
                monster.Intent is null ? Hue.Black : Hue.DarkRed);

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
        _ => ('?', Hue.Magenta, Hue.Black),
    };

    private static void DrawSidebar(Frame frame, Game game)
    {
        var p = game.Player;
        int y = 1;
        void Line(string text, Hue fg = Hue.Gray)
        {
            if (y < LogY - 1) frame.Write(SideX, y, text, fg);
            y++;
        }

        Line("The Bearer", Hue.White);
        Line(new string('-', 22), Hue.DarkGray);
        Line($"HP  {Bar(p.Hp, p.EffectiveMaxHp, 10)} {p.Hp}/{p.EffectiveMaxHp}", p.Hp * 3 <= p.EffectiveMaxHp ? Hue.Red : Hue.Gray);
        Line($"ST  {Bar(p.Stamina, p.MaxStamina, 10)} {p.Stamina}/{p.MaxStamina}", Hue.Gray);
        Line($"Coin    {p.Coin}", Hue.Yellow);
        Line($"Essence {p.Essence}", Hue.Cyan);
        if (p.WoundedTurns > 0) Line($"WOUNDED ({p.WoundedTurns})", Hue.Red);
        y++;

        if (game.Mode == MapMode.Site)
        {
            Line("Goblin cave", Hue.White);
            int alive = game.Monsters.Count(m => m.Alive);
            Line($"Foes here: {alive}", alive > 0 ? Hue.Red : Hue.DarkGreen);
            foreach (var monster in game.LiveMonstersHere.Where(m => m.Intent is not null))
                Line("! crushing blow poised", Hue.Red);
        }
        else
        {
            Line(game.World.SettlementName, Hue.White);
            var here = game.CurrentMap[p.Pos];
            if (here == Terrain.Shrine) Line("At the shrine (+)", Hue.Cyan);
            if (here == Terrain.CampEntrance) Line("Cave mouth: > enters", Hue.Red);
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

    private static void DrawLog(Frame frame, Game game, int height)
    {
        int lines = height - LogY;
        frame.Write(0, LogY - 1, new string('-', frame.Width), Hue.DarkGray);
        int y = LogY;
        foreach (var entry in game.Log.Recent(lines))
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
