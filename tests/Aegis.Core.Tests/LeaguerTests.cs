using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The fen-leaguer and its sling-warders (D-057): the tier-6+ content band,
/// and the board-plus-sling kind D-053 deferred. The mere stops feet and not
/// eyes; the loft needs no line of sight and is dodged by feet that keep
/// moving; the board is the carl's rule at the sling's range; the watch wakes
/// as one to a horn, gives ground when crowded, and holds the works forever.
/// The cist on the holm holds the armor ladder's next rung.
/// </summary>
public class LeaguerTests
{
    [Fact]
    public void TheLeaguer_StandsAtTierSix_AndNotBefore()
    {
        var tierFive = new Game(42);
        for (int i = 0; i < 4; i++) Cross(tierFive);
        Assert.Null(tierFive.World.LeaguerSite);
        Assert.False(tierFive.World.Facts.Exists("site", "leaguer"));

        var game = TierSixGame();
        var mere = game.World.LeaguerSite!;
        Assert.Equal(Terrain.LeaguerEntrance, game.World.Overworld[mere.OverworldPos]);
        Assert.True(game.World.Facts.Exists("site", "leaguer"));

        // Five warders stand the works at tier 6, and no other kind at all:
        // the mere itself is the band's second tenant.
        Assert.Equal(5, mere.Spawns.Count);
        Assert.All(mere.Spawns, s => Assert.Equal(MonsterKind.Warder, s.Kind));
        Assert.All(mere.Spawns, s => Assert.True(mere.Map.Walkable(s.Pos)));

        // The ring, the causeway, and the holm connect: cist and every warder
        // are reachable from the entry on foot.
        var reached = new HashSet<Pos> { mere.EntryPos };
        var frontier = new Queue<Pos>();
        frontier.Enqueue(mere.EntryPos);
        while (frontier.Count > 0)
        {
            var p = frontier.Dequeue();
            foreach (var (dx, dy) in Directions.All8)
            {
                var next = p.Plus(dx, dy);
                if (mere.Map.Walkable(next) && reached.Add(next)) frontier.Enqueue(next);
            }
        }
        Assert.Contains(mere.ChestPos, reached);
        Assert.All(mere.Spawns, s => Assert.Contains(s.Pos, reached));
    }

    [Fact]
    public void TheMere_StopsFeet_AndNotEyes()
    {
        var map = TierSixGame().World.LeaguerSite!.Map;

        // Water bars the step and never the sight: a line straight across the
        // open mere holds, though not one cell of it can be walked.
        var west = new Pos(3, 6);
        var east = new Pos(WorldGen.LeaguerW - 4, 6);
        Assert.True(map.Walkable(west));
        Assert.True(map.Walkable(east));
        Assert.True(Enumerable.Range(4, WorldGen.LeaguerW - 8).Any(x => map[new Pos(x, 6)] == Terrain.Water));
        Assert.True(map.LineOfSight(west, east));
        Assert.False(map.Walkable(new Pos(10, 6)));

        // Stone still blinds: only walls and houses are opaque.
        Assert.True(TerrainInfo.Opaque(Terrain.Wall));
        Assert.True(TerrainInfo.Opaque(Terrain.House));
        Assert.False(TerrainInfo.Opaque(Terrain.Water));
    }

    [Fact]
    public void TheHorn_WakesTheWholeLeaguer_AsOne()
    {
        var game = TierSixGame();
        Assert.All(game.Monsters.Where(m => m.SiteId == "leaguer"), m => Assert.True(m.Dormant));

        game.Debug_SetPlayerPos(game.World.LeaguerSite!.OverworldPos);
        game.Apply(Command.Enter);
        for (int i = 0; i < 15 && !game.Log.Entries.Any(e => e.Text.Contains("horn sounds low")); i++)
        {
            game.Player.Hp = game.Player.MaxHp;
            game.ApplyKey('l');
        }

        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("horn sounds low")));
        Assert.All(game.Monsters.Where(m => m.Alive && m.SiteId == "leaguer"), m => Assert.False(m.Dormant));
    }

    [Fact]
    public void TheLoft_NeedsNoLine_AndWhirlsTwoTurns()
    {
        var game = EnterLeaguer();
        var warder = Alive(game, MonsterKind.Warder).First();
        Quiet(game, m => m == warder);
        warder.Dormant = false;

        // Ground the graven men taught safe: in range, out of sight. The
        // stone comes over the mound anyway; cover is no roof here.
        var map = game.CurrentSite!.Map;
        Pos? blind = null;
        for (int y = 1; y < map.Height - 1 && blind is null; y++)
            for (int x = 1; x < map.Width - 1 && blind is null; x++)
            {
                var p = new Pos(x, y);
                int d = p.Chebyshev(warder.Pos);
                if (map.Walkable(p) && d >= 3 && d <= Game.LoftRange
                    && !map.LineOfSight(warder.Pos, p))
                    blind = p;
            }
        Assert.NotNull(blind);
        game.Debug_SetPlayerPos(blind!.Value);

        int guard = 60;
        while (warder.Intent is null && guard-- > 0)
        {
            game.Player.Hp = game.Player.MaxHp;
            game.ApplyKey('.');
        }
        Assert.NotNull(warder.Intent);
        Assert.Equal(IntentKind.LoftedStone, warder.Intent!.Kind);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("into the whirl"));

        // Declared this turn, the whirl has two turns left to run: time
        // enough to walk two honest strides off the mark.
        Assert.Equal(2, warder.Intent.TurnsUntilResolve);
    }

    [Fact]
    public void TheStone_TakesTheMark_GrazesBeside_AndSparesTwoStridesOut()
    {
        var game = EnterLeaguer();
        var warder = Alive(game, MonsterKind.Warder).First();
        Quiet(game, m => m == warder);
        warder.Dormant = false;

        // Standing on the mark is the full price.
        game.Player.Hp = 20;
        warder.Intent = new Intent { Kind = IntentKind.LoftedStone, TargetCell = game.Player.Pos, TurnsUntilResolve = 1 };
        game.ApplyKey('.');
        int square = 20 - game.Player.Hp;
        Assert.InRange(square, 7, 11);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("takes you square"));

        // A stride off, the burst still grazes for half.
        warder.ExposedTurns = 0;
        game.Player.Hp = 20;
        warder.Intent = new Intent { Kind = IntentKind.LoftedStone, TargetCell = game.Player.Pos.Plus(1, 0), TurnsUntilResolve = 1 };
        game.ApplyKey('.');
        int graze = 20 - game.Player.Hp;
        Assert.InRange(graze, 3, 5);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("bursts a stride off"));

        // Two strides of honest walking is the whole dodge.
        warder.ExposedTurns = 0;
        game.Player.Hp = 20;
        warder.Intent = new Intent { Kind = IntentKind.LoftedStone, TargetCell = game.Player.Pos.Plus(2, 0), TurnsUntilResolve = 1 };
        game.ApplyKey('.');
        Assert.Equal(20, game.Player.Hp);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("where you stood"));

        // Every cast, landed or not, leaves the board hanging wide.
        Assert.Equal(2, warder.ExposedTurns);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("hangs wide"));
    }

    [Fact]
    public void TheBoard_TurnsTheShaft_UntilTheWork_AndASightedPoint_IsASighting()
    {
        var game = EnterLeaguer();
        game.Debug_GrantGear("hunting_bow");
        var (warder, from, key) = FindLine(game, minLen: 2, MonsterKind.Warder);
        game.Debug_SetPlayerPos(from);

        // A shaft at a watching warder stops in the linden: nothing bought,
        // nothing taught. But a shaft taken on the board is a sighting, and
        // a sighting is the horn.
        int hpBefore = warder.Hp;
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.Equal(hpBefore, warder.Hp);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Ranged));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("linden board"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("horn sounds low"));
        Assert.All(game.Monsters.Where(m => m.Alive && m.SiteId == "leaguer"), m => Assert.False(m.Dormant));

        // While the sling is about its work, the board has left its line.
        Quiet(game, m => m == warder);
        game.Debug_SetPlayerPos(from);
        game.Player.Stamina = 10;
        warder.Intent = new Intent { Kind = IntentKind.LoftedStone, TargetCell = game.Player.Pos, TurnsUntilResolve = 2 };
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.True(warder.Hp < hpBefore, "the whirling warder was not struck");
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Ranged));
    }

    [Fact]
    public void TheBoard_TurnsThePoint_TheSameWay()
    {
        var game = EnterLeaguer();
        game.Player.Attributes[Attr.Might] = 6;
        game.Player.Weapon = GearCatalog.Create("ash_spear");
        var (warder, from, key) = FindLine(game, minLen: 2, MonsterKind.Warder);
        game.Debug_SetPlayerPos(from);
        game.Player.Stamina = 10;

        int hpBefore = warder.Hp;
        game.ApplyKey('t');
        game.ApplyKey(key);
        Assert.Equal(hpBefore, warder.Hp);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Hafted));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("turned along the grain"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("horn sounds low"));

        // In the blown turns after a cast, the point goes in.
        Quiet(game, m => m == warder);
        game.Debug_SetPlayerPos(from);
        game.Player.Stamina = 10;
        warder.ExposedTurns = 2;
        game.ApplyKey('t');
        game.ApplyKey(key);
        Assert.True(warder.Hp < hpBefore, "the blown warder was not struck");
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Hafted));
    }

    [Fact]
    public void TheWarder_GivesGround_AndCornered_TheRimComesDown()
    {
        var game = EnterLeaguer();
        var warder = Alive(game, MonsterKind.Warder).First();
        Quiet(game, m => m == warder);
        warder.Dormant = false;

        // Crowded on open works with a lane at its back, it backs off to
        // reopen its range. (Against the water's edge it would stand; the
        // cornered half below proves that side.)
        game.Debug_SetPlayerPos(AdjacentWithEscape(game, warder));
        var stood = warder.Pos;
        int distBefore = warder.Pos.Chebyshev(game.Player.Pos);
        game.ApplyKey('.');
        Assert.NotEqual(stood, warder.Pos);
        Assert.True(warder.Pos.Chebyshev(game.Player.Pos) > distBefore, "the warder did not open the range");
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("backs off behind its board"));

        // Backed into its own works with nowhere better, the rim is all it has.
        var map = game.CurrentSite!.Map;
        var (corner, stand) = Corner(game);
        warder.Pos = corner;
        game.Debug_SetPlayerPos(stand);
        int hp = game.Player.Hp = game.Player.MaxHp;
        game.ApplyKey('.');
        Assert.Equal(corner, warder.Pos);
        Assert.True(game.Player.Hp < hp, "the cornered warder never fought");
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("cracks its board's rim"));
    }

    [Fact]
    public void ThePursuit_PinsTheSling_Silent()
    {
        var game = EnterLeaguer();
        var warder = Alive(game, MonsterKind.Warder).First();
        Quiet(game, m => m == warder);
        warder.Dormant = false;

        // A chased warder retreats or stands; it never lofts. Its fellows
        // are the ones who punish the chase, and they are quiet today.
        for (int i = 0; i < 12; i++)
        {
            game.Player.Hp = game.Player.MaxHp;
            game.Debug_SetPlayerPos(AdjacentOpen(game, warder.Pos));
            game.ApplyKey('.');
            Assert.Null(warder.Intent);
        }
    }

    [Fact]
    public void TheCist_HoldsTheByrnie_UnderTheTwinRule()
    {
        var byrnie = GearCatalog.Create("scaled_byrnie");
        Assert.Equal(GearSlot.Armor, byrnie.Slot);
        Assert.Equal(Attr.Vigor, byrnie.ReqAttr);
        Assert.Equal(11, byrnie.Req);
        Assert.True(byrnie.Bonus > GearCatalog.Create("wrights_mail").Bonus);
        Assert.Equal(MoveVerb.None, byrnie.Move);

        var game = EnterLeaguer();
        Quiet(game);
        game.Debug_SetPlayerPos(game.CurrentSite!.ChestPos);
        game.ApplyKey('g');
        Assert.True(game.Player.OwnsGear("scaled_byrnie"));
        Assert.Equal("scaled_byrnie", game.Player.Armor?.Id);

        // The next world's leaguer holds the twin racked deeper (one more
        // warder stands the works past tier 6), and the twin is left.
        Cross(game);
        Assert.Equal(6, game.World.LeaguerSite!.Spawns.Count);
        game.Debug_SetPlayerPos(game.World.LeaguerSite!.OverworldPos);
        game.Apply(Command.Enter);
        Quiet(game);
        game.Debug_SetPlayerPos(game.CurrentSite!.ChestPos);
        game.ApplyKey('g');
        Assert.Equal(1, game.Player.AllGear.Count(g => g.Id == "scaled_byrnie"));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("twin of your own"));
    }

    [Fact]
    public void LiftingTheSiege_WritesTheDeed_AndTheSteadAnswers()
    {
        var game = EnterLeaguer();
        game.Apply(Command.Exit);
        game.Debug_ClearSite(SiteKind.Leaguer);

        Assert.True(game.World.LeaguerSite!.Cleared);
        Assert.True(game.World.Facts.Exists("deed", "siege_lifted"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("it only emptied"));

        for (int i = 0; i < 12 && !game.Log.Entries.Any(e => e.Text.Contains("low road")); i++)
            StepNearHouse(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("low road"));
    }

    [Fact]
    public void TheArrival_KeepsTheHabit_NotACounsel()
    {
        var game = TierSixGame();
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Of the black mere"));
    }

    [Fact]
    public void TheBareHolm_TellsTheFurthestTurn()
    {
        var game = EnterLeaguer();
        Quiet(game);
        game.Debug_SetPlayerPos(new Pos(WorldGen.HolmMinX - 1, WorldGen.LeaguerH / 2));
        game.ApplyKey('l');
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("nothing to besiege"));
    }

    [Fact]
    public void TheMark_IsDrawnWhole_AllNineCells()
    {
        var game = EnterLeaguer();
        var warder = Alive(game, MonsterKind.Warder).First();
        Quiet(game, m => m == warder);
        warder.Dormant = false;

        // A mark with open ground all around it, and the bearer beside but
        // not inside it: the drawing must promise the graze ring too.
        var map = game.CurrentSite!.Map;
        Pos? center = null;
        for (int y = 2; y < map.Height - 2 && center is null; y++)
            for (int x = 2; x < map.Width - 2 && center is null; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p)) continue;
                if (Directions.All8.All(d => map.Walkable(p.Plus(d.dx, d.dy)))
                    && map.Walkable(p.Plus(2, 0)) && p.Plus(2, 0) != warder.Pos)
                    center = p;
            }
        Assert.NotNull(center);
        game.Debug_SetPlayerPos(center!.Value.Plus(2, 0));
        warder.Intent = new Intent { Kind = IntentKind.LoftedStone, TargetCell = center.Value, TurnsUntilResolve = 2 };

        var frame = Presenter.Render(game);
        int marked = 0;
        for (int y = 0; y < Presenter.DefaultHeight; y++)
            for (int x = 0; x < Presenter.DefaultWidth; x++)
                if (frame[x, y] is { Ch: '!', Bg: Hue.DarkRed or Hue.Red })
                    marked++;
        Assert.True(marked >= 9, $"only {marked} of the nine mark cells drawn");
    }

    [Fact]
    public void ALeaguerSession_ReplaysIdenticallyFromItsKeys()
    {
        var keys = new List<char>();
        var live = EnterLeaguer();
        live.KeyApplied += keys.Add;
        live.Debug_GrantGear("hunting_bow");
        var (_, from, key) = FindLine(live, minLen: 2, MonsterKind.Warder);
        live.Debug_SetPlayerPos(from);
        foreach (char k in (char[])['f', key, '.', '.', 'f', key, '.']) live.ApplyKey(k);

        var replayed = EnterLeaguer();
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

    /// <summary>Master 42 crossed five times: world 6, tier 6, the first leaguer world.</summary>
    private static Game TierSixGame()
    {
        var game = new Game(42);
        for (int i = 0; i < 5; i++) Cross(game);
        Assert.Equal(6, game.Cycle);
        Assert.NotNull(game.World.LeaguerSite);
        return game;
    }

    private static Game EnterLeaguer()
    {
        var game = TierSixGame();
        game.Debug_SetPlayerPos(game.World.LeaguerSite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("leaguer", game.CurrentSite!.Id);
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

    /// <summary>Quiets every tenant the predicate does not keep.</summary>
    private static void Quiet(Game game, Func<Monster, bool>? keep = null)
    {
        foreach (var m in game.Monsters.Where(m => m.SiteId == game.CurrentSite!.Id))
            if (keep is null || !keep(m)) m.Hp = 0;
    }

    /// <summary>
    /// A dead-end pocket of the works: a corner cell whose only ways out sit
    /// no further from the diagonal stand than the corner itself.
    /// </summary>
    private static (Pos Corner, Pos Stand) Corner(Game game)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (c, s) in ((Pos, Pos)[])[
            (new Pos(1, 1), new Pos(2, 2)),
            (new Pos(WorldGen.LeaguerW - 2, 1), new Pos(WorldGen.LeaguerW - 3, 2)),
            (new Pos(1, WorldGen.LeaguerH - 2), new Pos(2, WorldGen.LeaguerH - 3)),
            (new Pos(WorldGen.LeaguerW - 2, WorldGen.LeaguerH - 2), new Pos(WorldGen.LeaguerW - 3, WorldGen.LeaguerH - 3))])
            if (map.Walkable(c) && map.Walkable(s)) return (c, s);
        Assert.Fail("no open corner on the works");
        return default;
    }

    /// <summary>A cardinal cell beside the warder whose opposite cell is open: ground it can give.</summary>
    private static Pos AdjacentWithEscape(Game game, Monster warder)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.Cardinal)
        {
            var stand = warder.Pos.Plus(dx, dy);
            var back = warder.Pos.Plus(-dx, -dy);
            if (map.Walkable(stand) && map.Walkable(back)
                && !game.Monsters.Any(m => m.Alive && (m.Pos == stand || m.Pos == back)))
                return stand;
        }
        Assert.Fail($"no escape lane beside {warder.Pos}");
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
