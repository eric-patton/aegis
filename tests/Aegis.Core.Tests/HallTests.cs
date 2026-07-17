using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The third band (D-044): the fallen hall at tier 4+, the iron hounds (the first
/// pack: full walking speed, bites that grow with flankers, a throat-lunge only
/// attempted while flanked), the door-slots that deny the flank, the coffer's
/// mail, the pack deed, and the lintel script.
/// </summary>
public class HallTests
{
    [Fact]
    public void Hall_ExistsAtTierFourPlus_Deterministic_AndReachable()
    {
        for (ulong seed = 1; seed <= 25; seed++)
        {
            Assert.Null(WorldGen.Generate(seed, tier: 3).HallSite);

            foreach (int tier in (int[])[4, 7])
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);

                var hall = a.HallSite;
                Assert.NotNull(hall);
                Assert.Equal(Terrain.HallEntrance, a.Overworld[hall!.OverworldPos]);
                Assert.Equal(hall.OverworldPos, b.HallSite!.OverworldPos);
                Assert.True(Reachable(a.Overworld, a.ShrinePos, hall.OverworldPos),
                    $"seed {seed} tier {tier}: hall unreachable");

                Assert.Equal(Math.Min(5 + (tier - 4), 8), hall.Spawns.Count);
                Assert.All(hall.Spawns, s => Assert.Equal(MonsterKind.Hound, s.Kind));
                Assert.All(hall.Spawns, s => Assert.Equal(10 + 2 * (tier - 4), s.Hp));

                // Porch, room, and both chambers hang together despite the rubble.
                Assert.True(Reachable(hall.Map, hall.EntryPos, hall.ChestPos),
                    $"seed {seed} tier {tier}: coffer unreachable");
                foreach (var s in hall.Spawns)
                    Assert.True(Reachable(hall.Map, hall.EntryPos, s.Pos),
                        $"seed {seed} tier {tier}: a hound is walled in");
                Assert.True(a.Facts.Exists("site", "hall"));
            }
        }
    }

    [Fact]
    public void TheGround_HoldsChokepoints_OneBodyWide()
    {
        // The counterplay is structural: the porch and the chamber door-slots
        // admit one body at a time, so the pack cannot flank a bearer in them.
        var hall = WorldGen.Generate(11, tier: 4).HallSite!;
        int mid = WorldGen.HallH / 2;

        foreach (int x in (int[])[3, 4, 5])
        {
            Assert.True(hall.Map.Walkable(new Pos(x, mid)));
            Assert.Equal(Terrain.Wall, hall.Map[new Pos(x, mid - 1)]);
            Assert.Equal(Terrain.Wall, hall.Map[new Pos(x, mid + 1)]);
        }

        foreach (int cy in (int[])[4, WorldGen.HallH - 5])
            foreach (int x in (int[])[25, 26])
            {
                Assert.True(hall.Map.Walkable(new Pos(x, cy)));
                Assert.Equal(Terrain.Wall, hall.Map[new Pos(x, cy - 1)]);
                Assert.Equal(Terrain.Wall, hall.Map[new Pos(x, cy + 1)]);
            }
    }

    [Fact]
    public void ALoneHound_OnlyBites_AndNeverLunges()
    {
        var game = EnterHall();
        var mark = game.Monsters.First(m => m.Kind == MonsterKind.Hound);
        foreach (var other in game.Monsters.Where(m => m.Kind == MonsterKind.Hound && m != mark))
            other.Hp = 0;

        game.Player.Hp = 999;
        game.Debug_SetPlayerPos(AdjacentOpen(game, mark.Pos));
        for (int i = 0; i < 50; i++)
        {
            game.ApplyKey('.');
            Assert.Null(mark.Intent);
        }
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("gathers itself low"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("bite worries you"));
    }

    [Fact]
    public void AFlankedBearer_FacesTheThroatLunge_WhichLandsOnTheCell()
    {
        var game = EnterHall();
        var hounds = game.Monsters.Where(m => m.Kind == MonsterKind.Hound).ToList();
        var first = hounds[0];
        var second = hounds[1];
        foreach (var other in hounds.Skip(2)) other.Hp = 0;

        // Two hounds at your sides in the open room: the pack's real teeth.
        game.Player.Hp = 999;
        game.Debug_SetPlayerPos(AdjacentOpen(game, first.Pos));
        second.Pos = AdjacentOpen(game, game.Player.Pos);

        Monster? lunger = null;
        for (int i = 0; i < 80 && lunger is null; i++)
        {
            game.ApplyKey('.');
            lunger = new[] { first, second }.FirstOrDefault(m => m.Intent?.Kind == IntentKind.ThroatLunge);
        }
        Assert.NotNull(lunger);
        Assert.Equal(game.Player.Pos, lunger!.Intent!.TargetCell);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("gathers itself low"));

        // The lunge is a telegraph like any other: step off the cell and live whole.
        StepAnywhereElse(game, lunger);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the space you left"));

        // The pack keeps coming at full speed, and the teeth find you again.
        bool Bitten() => game.Log.Entries.Any(e => e.Text.Contains("Teeth from more than one side")
            || e.Text.Contains("bite worries you"));
        for (int i = 0; i < 40 && !Bitten(); i++) game.ApplyKey('.');
        Assert.True(Bitten(), "two live hounds never landed a bite");
    }

    [Fact]
    public void Hounds_RunAtFullWalkingSpeed()
    {
        var game = EnterHall();
        var mark = game.Monsters.First(m => m.Kind == MonsterKind.Hound);
        foreach (var other in game.Monsters.Where(m => m.Kind == MonsterKind.Hound && m != mark))
            other.Hp = 0;

        game.Debug_SetPlayerPos(OpenCellAt(game, mark.Pos, distance: 6));

        // A wight steps every other turn and a graven man every third; a hound
        // takes ground on both of two consecutive turns.
        var start = mark.Pos;
        game.ApplyKey('.');
        var afterOne = mark.Pos;
        Assert.NotEqual(start, afterOne);
        game.ApplyKey('.');
        Assert.NotEqual(afterOne, mark.Pos);
    }

    [Fact]
    public void TheLintelScript_SpeaksOnceInTheChambers()
    {
        var game = EnterHall();
        foreach (var hound in game.Monsters.Where(m => m.Kind == MonsterKind.Hound))
            hound.Hp = 0;

        game.Debug_SetPlayerPos(new Pos(26, 4));
        game.ApplyKey('l');
        Assert.Equal(1, Count(game, "I do not remember learning this script"));

        // Once per world, like the quarry's downed tools.
        game.ApplyKey('h');
        game.ApplyKey('l');
        Assert.Equal(1, Count(game, "I do not remember learning this script"));
    }

    [Fact]
    public void TheCoffer_YieldsCoin_AndTheWrightsMail()
    {
        var game = EnterHall();
        foreach (var hound in game.Monsters.Where(m => m.Kind == MonsterKind.Hound))
            hound.Hp = 0;

        int coinBefore = game.Player.Coin;
        game.Debug_SetPlayerPos(game.World.HallSite!.ChestPos);
        game.ApplyKey('g');

        Assert.True(game.CurrentSite!.ChestLooted);
        Assert.True(game.Player.Coin > coinBefore);
        Assert.True(game.Player.OwnsGear("wrights_mail"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("rings of grey iron"));
    }

    [Fact]
    public void ClearingTheHall_WritesTheDeed_AndTheSteadAnswers()
    {
        var game = EnterHall();
        game.Apply(Command.Exit);
        game.Debug_ClearSite(SiteKind.Hall);

        Assert.True(game.World.HallSite!.Cleared);
        Assert.True(game.World.Facts.Exists("deed", "pack_broken"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("They were not wicked"));

        for (int i = 0; i < 12 && !game.Log.Entries.Any(e => e.Text.Contains("Quiet at dusk")); i++)
            StepNearHouse(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Quiet at dusk"));
    }

    [Fact]
    public void Presenter_DrawsTheGate_TheHounds_AndTheSidebar()
    {
        var game = TierFourGame();
        game.Debug_SetPlayerPos(game.World.HallSite!.OverworldPos.Plus(-1, 0));
        var overworld = Presenter.Render(game);
        bool gate = false;
        for (int y = 0; y < Presenter.DefaultHeight; y++)
            for (int x = 0; x < Presenter.DefaultWidth; x++)
                if (overworld[x, y] is { Ch: 'H', Fg: Hue.DarkCyan }) gate = true;
        Assert.True(gate, "the fallen gate is not drawn on the overworld");

        game.Debug_SetPlayerPos(game.World.HallSite.OverworldPos);
        game.Apply(Command.Enter);
        var inside = Presenter.Render(game);
        Assert.Contains(inside.ToTextLines(), line => line.Contains("The fallen hall"));

        bool hound = false;
        for (int y = 0; y < Presenter.DefaultHeight; y++)
            for (int x = 0; x < Presenter.DefaultWidth; x++)
                if (inside[x, y] is { Ch: 'd', Fg: Hue.DarkCyan }) hound = true;
        Assert.True(hound, "no iron hound drawn in the hall");
    }

    // ---- helpers ----

    private static int Count(Game game, string marker) =>
        game.Log.Entries.Count(e => e.Text.Contains(marker));

    /// <summary>Master 42 crossed three times: world 4, tier 4, the first hall world.</summary>
    private static Game TierFourGame()
    {
        var game = new Game(42);
        Cross(game);
        Cross(game);
        Cross(game);
        Assert.Equal(4, game.Cycle);
        Assert.NotNull(game.World.HallSite);
        return game;
    }

    private static Game EnterHall()
    {
        var game = TierFourGame();
        game.Debug_SetPlayerPos(game.World.HallSite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("hall", game.CurrentSite!.Id);
        return game;
    }

    /// <summary>A walkable cell beside a target, free of other monsters.</summary>
    private static Pos AdjacentOpen(Game game, Pos target)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = target.Plus(dx, dy);
            if (map.Walkable(p) && p != game.Player.Pos
                && !game.Monsters.Any(m => m.Alive && m.Pos == p)) return p;
        }
        Assert.Fail($"no open cell beside {target}");
        return default;
    }

    /// <summary>A walkable cell at an exact Chebyshev distance from the mark.</summary>
    private static Pos OpenCellAt(Game game, Pos mark, int distance)
    {
        var map = game.CurrentSite!.Map;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (p.Chebyshev(mark) != distance || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                return p;
            }
        Assert.Fail($"no open cell at distance {distance} from {mark}");
        return default;
    }

    private static void StepInto(Game game, Pos target)
    {
        int dx = Math.Sign(target.X - game.Player.Pos.X), dy = Math.Sign(target.Y - game.Player.Pos.Y);
        char key = (dx, dy) switch
        {
            (-1, 0) => 'h', (1, 0) => 'l', (0, -1) => 'k', (0, 1) => 'j',
            (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', _ => 'n',
        };
        game.ApplyKey(key);
    }

    private static void StepAnywhereElse(Game game, Monster mark)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = game.Player.Pos.Plus(dx, dy);
            if (!map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
            StepInto(game, p);
            Assert.Equal(p, game.Player.Pos);
            return;
        }
        Assert.Fail("nowhere to sidestep");
    }

    private static void StepNearHouse(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        var map = game.World.Overworld;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || game.World.Npcs.Any(n => n.Pos == p)) continue;
                if (!Directions.All8.Any(d => map.InBounds(p.Plus(d.dx, d.dy)) && map[p.Plus(d.dx, d.dy)] == Terrain.House)) continue;
                foreach (var (dx, dy, key) in ((int, int, char)[])[(-1, 0, 'l'), (1, 0, 'h'), (0, -1, 'j'), (0, 1, 'k')])
                {
                    var from = p.Plus(dx, dy);
                    if (!map.Walkable(from) || game.World.Npcs.Any(n => n.Pos == from)) continue;
                    game.Debug_SetPlayerPos(from);
                    game.ApplyKey(key);
                    if (game.Player.Pos == p) return;
                }
            }
        Assert.Fail("no walkable tile beside a house");
    }

    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
    }

    private static bool Reachable(GameMap map, Pos from, Pos to)
    {
        var seen = new HashSet<Pos> { from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (p == to) return true;
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var next = p.Plus(dx, dy);
                if (map.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return false;
    }
}
