using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The ringfort and the old watch (D-053): the tier-5+ content band, and the
/// bow's answer. Shield-carls stop shafts with the linden board except in the
/// windows their own blows open; war-boars charge the clean lanes a bowman
/// wants and cannot charge from beside you; the arms-chest holds the yew
/// warbow; and a felled boar pays in meat, never coin.
/// </summary>
public class RingfortTests
{
    [Fact]
    public void TheRingfort_StandsAtTierFive_AndNotBefore()
    {
        var tierFour = new Game(42);
        Cross(tierFour);
        Cross(tierFour);
        Cross(tierFour);
        Assert.Null(tierFour.World.RingfortSite);
        Assert.False(tierFour.World.Facts.Exists("site", "ringfort"));

        var game = TierFiveGame();
        var fort = game.World.RingfortSite!;
        Assert.Equal(Terrain.RingfortEntrance, game.World.Overworld[fort.OverworldPos]);
        Assert.True(game.World.Facts.Exists("site", "ringfort"));

        // The garrison: four carls in the ward, two boars at the lanes.
        Assert.Equal(4, fort.Spawns.Count(s => s.Kind == MonsterKind.Carl));
        Assert.Equal(2, fort.Spawns.Count(s => s.Kind == MonsterKind.Boar));
        Assert.All(fort.Spawns, s => Assert.True(fort.Map.Walkable(s.Pos)));

        // The heart is reachable from the gate: the one inner gate connects.
        var reached = new HashSet<Pos> { fort.EntryPos };
        var frontier = new Queue<Pos>();
        frontier.Enqueue(fort.EntryPos);
        while (frontier.Count > 0)
        {
            var p = frontier.Dequeue();
            foreach (var (dx, dy) in Directions.All8)
            {
                var next = p.Plus(dx, dy);
                if (fort.Map.Walkable(next) && reached.Add(next)) frontier.Enqueue(next);
            }
        }
        Assert.Contains(fort.ChestPos, reached);
        Assert.All(fort.Spawns, s => Assert.Contains(s.Pos, reached));
    }

    [Fact]
    public void TheBoard_StopsTheShaft_AndTheBlownWindows_OpenIt()
    {
        var game = EnterFort();
        game.Debug_GrantGear("hunting_bow");
        Quiet(game, m => m.Kind == MonsterKind.Carl);

        var (carl, from, key) = FindLine(game, minLen: 2, MonsterKind.Carl);
        var post = carl.Pos;
        game.Debug_SetPlayerPos(from);

        // Walking watch: the shaft stops in the wood. Wind and string are
        // spent, nothing is bought, and a board-school teaches no one.
        int hpBefore = carl.Hp;
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.Equal(hpBefore, carl.Hp);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Ranged));
        Assert.Equal(1, game.Player.Bow!.Wear);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("linden board"));

        // While the seax is about its blow, the board has left its line.
        carl.Pos = post;
        game.Debug_SetPlayerPos(from);
        game.Player.Stamina = 10;
        carl.Intent = new Intent { Kind = IntentKind.SeaxStab, TargetCell = game.Player.Pos };
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.True(carl.Hp < hpBefore, "the open carl was not struck");
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Ranged));

        // The blow resolved into empty air behind the shaft; the carl stands
        // blown, and the second window is open.
        Assert.Null(carl.Intent);
        Assert.Equal(2, carl.ExposedTurns);
        int hpOpen = carl.Hp;
        game.Player.Stamina = 10;
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.True(carl.Hp < hpOpen, "the blown carl was not struck");
    }

    [Fact]
    public void TheSeax_SpendsTheBoard_AndTheCarl_HoldsItsGroundBlown()
    {
        var game = EnterFort();
        var carl = Alive(game, MonsterKind.Carl).First();
        Quiet(game, m => m == carl);

        game.Debug_SetPlayerPos(AdjacentOpen(game, carl.Pos));
        int guard = 80;
        while (carl.Intent is null && guard-- > 0)
        {
            game.Player.Hp = game.Player.MaxHp;
            game.ApplyKey('.');
        }
        Assert.NotNull(carl.Intent);
        Assert.Equal(IntentKind.SeaxStab, carl.Intent!.Kind);

        // Step off the marked cell: the blow spends itself on air, and the
        // board hangs wide either way.
        StepAnywhereElse(game, carl.Intent.TargetCell);
        Assert.Null(carl.Intent);
        Assert.Equal(2, carl.ExposedTurns);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("hangs wide"));

        // Blown, it neither steps nor strikes until the board is gathered back.
        var stood = carl.Pos;
        int hp = game.Player.Hp;
        game.ApplyKey('.');
        game.ApplyKey('.');
        Assert.Equal(stood, carl.Pos);
        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(0, carl.ExposedTurns);
    }

    [Fact]
    public void TheCharge_RunsTheLane_AndTakesWhoeverStandsOnIt()
    {
        var game = EnterFort();
        var boar = Alive(game, MonsterKind.Boar).First();
        Quiet(game, m => m == boar);

        var (start, stand) = OpenLane(game, length: 6);
        boar.Pos = start;
        game.Debug_SetPlayerPos(stand);
        boar.Intent = new Intent { Kind = IntentKind.BoarCharge, TargetCell = stand };

        // Standing your ground on the lane is the mistake the fort punishes.
        int hpBefore = game.Player.Hp;
        game.ApplyKey('.');
        Assert.True(game.Player.Hp < hpBefore, "the charge did not land");
        Assert.Equal(1, boar.Pos.Chebyshev(game.Player.Pos));
        Assert.Equal(0, boar.ExposedTurns);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("full on the tusks"));
    }

    [Fact]
    public void TheCharge_IsDodgedSideways_AndAMiss_LeavesTheBoarBlown()
    {
        var game = EnterFort();
        var boar = Alive(game, MonsterKind.Boar).First();
        Quiet(game, m => m == boar);

        var (start, stand) = OpenLane(game, length: 6);
        boar.Pos = start;
        game.Debug_SetPlayerPos(stand);
        boar.Intent = new Intent { Kind = IntentKind.BoarCharge, TargetCell = stand };

        // One step off the line, not one step back along it.
        var map = game.CurrentSite!.Map;
        var side = stand.Plus(0, map.Walkable(stand.Plus(0, 1)) ? 1 : -1);
        Assert.True(map.Walkable(side));
        StepInto(game, side);

        Assert.Equal(game.Player.MaxHp, game.Player.Hp);
        Assert.Equal(2, boar.ExposedTurns);
        Assert.True(boar.Pos.X > start.X, "the boar never ran its lane");
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("blown"));
    }

    [Fact]
    public void TheCharge_NeedsARunUp_SoClosing_IsTheCounterplay()
    {
        var game = EnterFort();
        var boar = Alive(game, MonsterKind.Boar).First();
        Quiet(game, m => m == boar);

        game.Debug_SetPlayerPos(AdjacentOpen(game, boar.Pos));
        for (int i = 0; i < 40; i++)
        {
            game.Player.Hp = game.Player.MaxHp;
            game.ApplyKey('.');
            Assert.True(boar.Intent is null or { Kind: not IntentKind.BoarCharge },
                "the boar charged without room for a run");
            Assert.Equal(1, boar.Pos.Chebyshev(game.Player.Pos));
        }
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the tusks can only rake")
            || e.Text.Contains("tusks hook air"));
    }

    [Fact]
    public void TheBoar_PaysInMeat_NeverCoin()
    {
        var game = EnterFort();
        var boar = Alive(game, MonsterKind.Boar).First();
        Quiet(game, m => m == boar);

        boar.Hp = 1;
        game.Player.Rations = 0;
        game.Debug_SetPlayerPos(AdjacentOpen(game, boar.Pos));
        int coin = game.Player.Coin, essence = game.Player.Essence;
        game.Player.Stamina = 10;
        StepInto(game, boar.Pos);

        Assert.False(boar.Alive);
        Assert.Equal(coin, game.Player.Coin);
        Assert.Equal(essence + 6, game.Player.Essence);
        Assert.Equal(1, game.Player.Rations);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("meat for the road"));

        // At the cap the beast still owes no coin, and the rest goes to the crows.
        var second = EnterFort();
        var other = Alive(second, MonsterKind.Boar).First();
        Quiet(second, m => m == other);
        other.Hp = 1;
        second.Player.Rations = Game.RationCap;
        second.Debug_SetPlayerPos(AdjacentOpen(second, other.Pos));
        second.Player.Stamina = 10;
        StepInto(second, other.Pos);
        Assert.Equal(Game.RationCap, second.Player.Rations);
        Assert.Contains(second.Log.Entries, e => e.Text.Contains("leave more meat"));
    }

    [Fact]
    public void TheWarbow_WaitsInTheArmsChest_UnderTheTwinRule()
    {
        var warbow = GearCatalog.Create("warbow");
        Assert.Equal(GearSlot.Ranged, warbow.Slot);
        Assert.Equal(Attr.Grace, warbow.ReqAttr);
        Assert.Equal(9, warbow.Req);
        Assert.True(warbow.Bonus > GearCatalog.Create("hunting_bow").Bonus);

        var game = EnterFort();
        Quiet(game);
        game.Debug_SetPlayerPos(game.CurrentSite!.ChestPos);
        game.ApplyKey('g');
        Assert.True(game.Player.OwnsGear("warbow"));
        Assert.Equal("warbow", game.Player.Bow?.Id);

        // The next world's fort holds the twin, and the twin is left racked.
        Cross(game);
        game.Debug_SetPlayerPos(game.World.RingfortSite!.OverworldPos);
        game.Apply(Command.Enter);
        Quiet(game);
        game.Debug_SetPlayerPos(game.CurrentSite!.ChestPos);
        game.ApplyKey('g');
        Assert.Equal(1, game.Player.AllGear.Count(g => g.Id == "warbow"));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("twin of your own"));
    }

    [Fact]
    public void RelievingTheWatch_WritesTheDeed_AndTheSteadAnswers()
    {
        var game = EnterFort();
        game.Apply(Command.Exit);
        game.Debug_ClearSite(SiteKind.Ringfort);

        Assert.True(game.World.RingfortSite!.Cleared);
        Assert.True(game.World.Facts.Exists("deed", "watch_relieved"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("so you are it"));

        for (int i = 0; i < 12 && !game.Log.Entries.Any(e => e.Text.Contains("Good grass")); i++)
            StepNearHouse(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Good grass"));
    }

    [Fact]
    public void TheArrival_SpeaksTheOldestCounsel()
    {
        var game = TierFiveGame();
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Of the ringfort"));
    }

    [Fact]
    public void TheLane_IsMarkedWhole_WhenTheBoarWheelsOntoIt()
    {
        var game = EnterFort();
        var boar = Alive(game, MonsterKind.Boar).First();
        Quiet(game, m => m == boar);

        var (start, stand) = OpenLane(game, length: 5);
        boar.Pos = start;
        game.Debug_SetPlayerPos(stand);
        boar.Intent = new Intent { Kind = IntentKind.BoarCharge, TargetCell = stand };

        // Every cell of the run is marked, not only where the bearer stood:
        // sideways is the only honest dodge, and the drawing must say so.
        var frame = Presenter.Render(game);
        int marked = 0;
        for (int y = 0; y < Presenter.DefaultHeight; y++)
            for (int x = 0; x < Presenter.DefaultWidth; x++)
                if (frame[x, y] is { Ch: '!', Bg: Hue.DarkRed })
                    marked++;
        Assert.True(marked >= 5, $"only {marked} lane cells marked; the run is longer than that");
    }

    [Fact]
    public void AFortSession_ReplaysIdenticallyFromItsKeys()
    {
        var keys = new List<char>();
        var live = EnterFort();
        live.KeyApplied += keys.Add;
        live.Debug_GrantGear("hunting_bow");
        var (_, from, key) = FindLine(live, minLen: 2, MonsterKind.Carl);
        live.Debug_SetPlayerPos(from);
        foreach (char k in (char[])['f', key, '.', '.', 'f', key, '.']) live.ApplyKey(k);

        var replayed = EnterFort();
        replayed.Debug_GrantGear("hunting_bow");
        replayed.Debug_SetPlayerPos(from);
        foreach (char k in keys) replayed.ApplyKey(k);

        Assert.Equal(live.Turn, replayed.Turn);
        Assert.Equal(live.Player.Hp, replayed.Player.Hp);
        Assert.Equal(live.Player.Pos, replayed.Player.Pos);
        Assert.Equal(live.Player.Bow!.Wear, replayed.Player.Bow!.Wear);
        Assert.Equal(live.TakeSnapshot().MonstersAlive, replayed.TakeSnapshot().MonstersAlive);
    }

    // ---- helpers ----

    /// <summary>Master 42 crossed four times: world 5, tier 5, the first fort world.</summary>
    private static Game TierFiveGame()
    {
        var game = new Game(42);
        Cross(game);
        Cross(game);
        Cross(game);
        Cross(game);
        Assert.Equal(5, game.Cycle);
        Assert.NotNull(game.World.RingfortSite);
        return game;
    }

    private static Game EnterFort()
    {
        var game = TierFiveGame();
        game.Debug_SetPlayerPos(game.World.RingfortSite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("ringfort", game.CurrentSite!.Id);
        return game;
    }

    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
    }

    private static IEnumerable<Monster> Alive(Game game, MonsterKind kind) =>
        game.Monsters.Where(m => m.Alive && m.SiteId == game.CurrentSite!.Id && m.Kind == kind);

    /// <summary>Quiets every fort tenant the predicate does not keep.</summary>
    private static void Quiet(Game game, Func<Monster, bool>? keep = null)
    {
        foreach (var m in game.Monsters.Where(m => m.SiteId == game.CurrentSite!.Id))
            if (keep is null || !keep(m)) m.Hp = 0;
    }

    /// <summary>A straight open west-to-east run: the boar's start and the bearer's stand.</summary>
    private static (Pos Start, Pos Stand) OpenLane(Game game, int length)
    {
        var map = game.CurrentSite!.Map;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1 - length; x++)
            {
                bool clear = true;
                for (int i = 0; i <= length && clear; i++)
                {
                    var p = new Pos(x + i, y);
                    if (!map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) clear = false;
                }
                // Room to overrun past the stand, and a sidestep row beside it.
                if (!clear || !map.Walkable(new Pos(x + length + 1, y))) continue;
                var stand = new Pos(x + length, y);
                if (!map.Walkable(stand.Plus(0, 1)) && !map.Walkable(stand.Plus(0, -1))) continue;
                return (new Pos(x, y), stand);
            }
        Assert.Fail("no open lane in the fort");
        return default;
    }

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

    /// <summary>A clear straight line to a mark of the wanted kind (RangedTests' finder).</summary>
    private static (Monster Mark, Pos From, char Key) FindLine(Game game, int minLen, MonsterKind kind)
    {
        var map = game.CurrentSite!.Map;
        foreach (var mark in Alive(game, kind))
            foreach (var (dx, dy) in Directions.All8)
                for (int len = 1; len <= Game.BowRange; len++)
                {
                    var p = mark.Pos.Plus(dx * len, dy * len);
                    if (!map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) break;
                    if (len >= minLen) return (mark, p, KeyFor(-dx, -dy));
                }
        Assert.Fail("no clear line to any mark");
        return default;
    }

    private static void StepInto(Game game, Pos target)
    {
        int dx = Math.Sign(target.X - game.Player.Pos.X), dy = Math.Sign(target.Y - game.Player.Pos.Y);
        game.ApplyKey(KeyFor(dx, dy));
    }

    private static void StepAnywhereElse(Game game, Pos avoid)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = game.Player.Pos.Plus(dx, dy);
            if (p == avoid || !map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
            StepInto(game, p);
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

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (-1, 0) => 'h', (1, 0) => 'l', (0, -1) => 'k', (0, 1) => 'j',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', _ => 'n',
    };
}
