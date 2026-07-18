using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The heave and the sword-thegn (D-058, paying D-004's commitment clause:
/// heavy player attacks take a visible wind-up the field reacts to). 'w' and a
/// line wind the biggest single blow a hand throws; it stands one turn, visible,
/// and the next act looses it on the cell chosen, hit or miss. The sword-thegn
/// is the field reading the bearer back: it breaks a heave wound up in its face,
/// but cannot answer a blow it must still close on, and its spent counter opens
/// it. Commitment runs both ways.
/// </summary>
public class HeaveTests
{
    [Fact]
    public void TheHeave_WantsIronInHand_AndABodyUnderARoof()
    {
        // Bare fists keep their verbs in the knacks (D-056): no heave.
        var bare = new Game(42);
        bare.Debug_SetMode(MapMode.Site);
        bare.Player.Weapon = null;
        int turn = bare.Turn;
        bare.ApplyKey('w');
        Assert.False(bare.InHeave);
        Assert.Equal(turn, bare.Turn);
        Assert.Contains(bare.Log.Recent(2), e => e.Text.Contains("wants iron in the hand"));

        // Under the open sky there is nothing worth the weight.
        var field = BladeBearer(42);
        Assert.Equal(MapMode.Overworld, field.Mode);
        turn = field.Turn;
        field.ApplyKey('w');
        Assert.False(field.InHeave);
        Assert.Equal(turn, field.Turn);
        Assert.Contains(field.Log.Recent(2), e => e.Text.Contains("worth so heavy a blow"));

        // Winded, the blow stays in the shoulder: the wind is the whole price.
        var winded = BladeBearer(42);
        winded.Debug_SetMode(MapMode.Site);
        winded.Player.Stamina = 4;
        turn = winded.Turn;
        winded.ApplyKey('w');
        Assert.False(winded.InHeave);
        Assert.Equal(turn, winded.Turn);
        Assert.Contains(winded.Log.Recent(2), e => e.Text.Contains("not the wind to wind it"));
    }

    [Fact]
    public void TheHeave_WindsUp_AndLandsBigOnTheLockedCell()
    {
        var game = BladeBearer(42);
        var (goblin, key) = AdjacentGoblin(game);
        Quiet(game, m => m == goblin);
        goblin.Hp = 99;

        int wind = game.Player.Stamina;
        game.ApplyKey('w');
        Assert.True(game.InHeave);
        game.ApplyKey(key);
        Assert.False(game.InHeave);

        // The wind is spent now, the cell is locked now, and the blow is loud.
        Assert.NotNull(game.Player.HeaveTarget);
        Assert.Equal(wind - 5, game.Player.Stamina);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("all your weight gathering behind it"));

        // The next act looses it, and a heave is bigger than any swing that iron
        // makes: a normal grave-iron cut cannot reach this deep.
        int before = goblin.Hp;
        game.ApplyKey('.');
        Assert.Null(game.Player.HeaveTarget);
        int dealt = before - goblin.Hp;
        Assert.True(dealt >= 14, $"the heave landed for only {dealt}");
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("heave lands full on the goblin for"));
    }

    [Fact]
    public void TheHeave_IsDodgedByFeet_TheBodyThatLeftTheCell()
    {
        // A cell gone empty is a blow spent on nothing (aimed at open ground).
        var open = BladeBearer(42);
        open.Debug_SetMode(MapMode.Site);
        Quiet(open);
        var map = open.World.Camp;
        var stand = FindOpenWithOpenNeighbor(open, map);
        open.Debug_SetPlayerPos(stand.From);
        open.ApplyKey('w');
        open.ApplyKey(stand.Key);
        int wear = open.Player.Weapon!.Wear;
        open.ApplyKey('.');
        Assert.Null(open.Player.HeaveTarget);
        Assert.Contains(open.Log.Recent(4), e => e.Text.Contains("ground gone empty"));
        // A full swing's wear is paid hit or miss (the thrust's rule).
        Assert.Equal(wear + 1, open.Player.Weapon!.Wear);

        // A body that steps off the marked cell is a body the blow never touches.
        var game = BladeBearer(42);
        var (goblin, key) = AdjacentGoblin(game);
        Quiet(game, m => m == goblin);
        goblin.Hp = 99;
        game.ApplyKey('w');
        game.ApplyKey(key);
        var marked = game.Player.HeaveTarget!.Value;
        // It moved: the graven rule of feet answering feet, made manual.
        var elsewhere = FindOpenWithOpenNeighbor(game, game.World.Camp).From;
        goblin.Pos = elsewhere;
        game.ApplyKey('.');
        Assert.Equal(99, goblin.Hp);
        Assert.NotEqual(marked, goblin.Pos);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("ground gone empty"));
    }

    [Fact]
    public void TheHeave_TeachesOnABody_AndAWhiffTeachesNothing()
    {
        // A heave that fells a body is a full paid swing: it teaches, and pays.
        var fell = BladeBearer(42);
        var (goblin, key) = AdjacentGoblin(fell);
        Quiet(fell, m => m == goblin);
        goblin.Hp = 6;
        int coin = fell.Player.Coin;
        fell.ApplyKey('w');
        fell.ApplyKey(key);
        fell.ApplyKey('.');
        Assert.False(goblin.Alive);
        Assert.Equal(1, fell.Player.Skills.Uses(SkillId.Blades));
        Assert.True(fell.Player.Coin >= coin, "no remains taken");

        // A heave into empty air wears the edge but teaches nothing.
        var whiff = BladeBearer(42);
        whiff.Debug_SetMode(MapMode.Site);
        Quiet(whiff);
        var stand = FindOpenWithOpenNeighbor(whiff, whiff.World.Camp);
        whiff.Debug_SetPlayerPos(stand.From);
        whiff.ApplyKey('w');
        whiff.ApplyKey(stand.Key);
        whiff.ApplyKey('.');
        Assert.Equal(0, whiff.Player.Skills.Uses(SkillId.Blades));
        Assert.Equal(1, whiff.Player.Weapon!.Wear);
    }

    [Fact]
    public void TheHeave_EasesOff_OnAnyOtherKey()
    {
        var game = BladeBearer(42);
        game.Debug_SetMode(MapMode.Site);
        int turn = game.Turn;
        int wind = game.Player.Stamina;
        game.ApplyKey('w');
        Assert.True(game.InHeave);
        game.ApplyKey(' ');
        Assert.False(game.InHeave);
        Assert.Null(game.Player.HeaveTarget);
        Assert.Equal(turn, game.Turn);
        Assert.Equal(wind, game.Player.Stamina);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("keep your feet under you"));
    }

    [Fact]
    public void TheThegn_StandsAtTierSeven_AndNotBefore()
    {
        // Tier 6 forts are the carls' and boars' alone; no thegn, no note.
        var tierSix = new Game(42);
        for (int i = 0; i < 5; i++) Cross(tierSix);
        Assert.Equal(6, tierSix.Cycle);
        Assert.NotNull(tierSix.World.RingfortSite);
        Assert.DoesNotContain(tierSix.World.RingfortSite!.Spawns, s => s.Kind == MonsterKind.Thegn);
        Assert.False(tierSix.World.Facts.Exists("site_note", "sword_thegn"));

        var tierSeven = TierSevenGame();
        Assert.Contains(tierSeven.World.RingfortSite!.Spawns, s => s.Kind == MonsterKind.Thegn);
        Assert.True(tierSeven.World.Facts.Exists("site_note", "sword_thegn"));
    }

    [Fact]
    public void TheThegn_ReadsThePointBlankHeave_AndBreaksIt()
    {
        var game = EnterFortAt(TierSevenGame());
        var thegn = Thegn(game);
        Quiet(game, m => m == thegn);
        thegn.Hp = 99;
        game.Player.Hp = game.Player.MaxHp;
        game.Player.Stamina = game.Player.MaxStamina;
        game.Debug_SetPlayerPos(AdjacentOpen(game, thegn.Pos));

        int hp = game.Player.Hp;
        int thegnHp = thegn.Hp;
        char key = KeyFor(thegn.Pos.X - game.Player.Pos.X, thegn.Pos.Y - game.Player.Pos.Y);
        game.ApplyKey('w');
        game.ApplyKey(key);

        // The wind-up died half-drawn; the point came back on the bearer.
        Assert.Null(game.Player.HeaveTarget);
        Assert.Equal(thegnHp, thegn.Hp);
        Assert.True(game.Player.Hp < hp, "the counter drew no blood");
        Assert.Equal(1, thegn.ExposedTurns);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("steps inside the winding blow"));
    }

    [Fact]
    public void TheThegn_CannotAnswerABlowItMustCloseOn()
    {
        var game = EnterFortAt(TierSevenGame());
        var thegn = Thegn(game);
        Quiet(game, m => m == thegn);
        thegn.Hp = 99;
        thegn.ExposedTurns = 0;
        game.Player.Hp = game.Player.MaxHp;
        game.Player.Stamina = game.Player.MaxStamina;

        var gap = CardinalGap(game, thegn);
        game.Debug_SetPlayerPos(gap.From);
        int hp = game.Player.Hp;

        // Wound up a stride out, it must walk onto the blow: there is nothing yet
        // to step inside, so it closes into the gap and the heave falls on it.
        game.ApplyKey('w');
        game.ApplyKey(gap.Key);
        Assert.NotNull(game.Player.HeaveTarget);
        Assert.Equal(1, thegn.Pos.Chebyshev(game.Player.Pos));  // it closed
        game.ApplyKey('.');

        Assert.Null(game.Player.HeaveTarget);
        Assert.True(thegn.Hp <= 99 - 14, $"the closing thegn took only {99 - thegn.Hp}");
        Assert.DoesNotContain(game.Log.Recent(8), e => e.Text.Contains("steps inside the winding blow"));
        Assert.Equal(hp, game.Player.Hp);  // no counter drew blood
    }

    [Fact]
    public void TheCounterSpent_TheGuardIsCracked_ByABaitedHeave()
    {
        var game = EnterFortAt(TierSevenGame());
        var thegn = Thegn(game);
        Quiet(game, m => m == thegn);
        thegn.Hp = 99;
        thegn.ExposedTurns = 0;
        game.Player.Hp = game.Player.MaxHp;
        game.Player.Stamina = game.Player.MaxStamina;
        game.Debug_SetPlayerPos(AdjacentOpen(game, thegn.Pos));
        char key = KeyFor(thegn.Pos.X - game.Player.Pos.X, thegn.Pos.Y - game.Player.Pos.Y);

        // First heave: the bait. It is broken, and the counter leaves it open.
        game.ApplyKey('w');
        game.ApplyKey(key);
        Assert.Equal(1, thegn.ExposedTurns);

        // Second heave, wound up while it is open: it cannot answer this one.
        game.Player.Stamina = game.Player.MaxStamina;
        int thegnHp = thegn.Hp;
        game.ApplyKey('w');
        game.ApplyKey(key);
        Assert.NotNull(game.Player.HeaveTarget);  // not broken this time
        game.ApplyKey('.');

        Assert.True(thegn.Hp <= thegnHp - 14, $"the opened thegn took only {thegnHp - thegn.Hp}");
        Assert.True(thegn.Alive);
        // The answer came exactly once, to the bait, and never to the real blow.
        Assert.Single(game.Log.Entries, e => e.Text.Contains("steps inside the winding blow"));
    }

    [Fact]
    public void TheThegn_IsPatient_AndNeverTelegraphs()
    {
        var game = EnterFortAt(TierSevenGame());
        var thegn = Thegn(game);
        Quiet(game, m => m == thegn);
        game.Player.Hp = game.Player.MaxHp;
        game.Debug_SetPlayerPos(AdjacentOpen(game, thegn.Pos));

        // It reads, it does not declare: no intent ever hangs over a thegn.
        for (int i = 0; i < 8; i++)
        {
            game.ApplyKey('.');
            Assert.Null(thegn.Intent);
        }
    }

    [Fact]
    public void AHeaveSession_ReplaysIdenticallyFromItsKeys()
    {
        const ulong seed = 7;
        var live = BladeBearer(seed);
        var (goblin, key) = AdjacentGoblin(live);
        Quiet(live, m => m == goblin);
        goblin.Hp = 99;
        var stand = live.Player.Pos;

        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);
        live.ApplyKey('w');
        live.ApplyKey(key);
        live.ApplyKey('.');

        var replay = BladeBearer(seed);
        var target = replay.Monsters.First(m => m.Alive && m.SiteId == "goblin-camp" && m.Pos == goblin.Pos);
        foreach (var m in replay.Monsters.Where(m => m.SiteId == "goblin-camp" && m != target)) m.Hp = 0;
        target.Hp = 99;
        replay.Debug_SetMode(MapMode.Site);
        replay.Debug_SetPlayerPos(stand);
        foreach (char k in journal.ToString()) replay.ApplyKey(k);

        var (a, b) = (live.TakeSnapshot(), replay.TakeSnapshot());
        Assert.Equal(a.Stamina, b.Stamina);
        Assert.Equal(a.WeaponWear, b.WeaponWear);
        Assert.Equal(a.Skills, b.Skills);
        Assert.Equal(a.RecentMessages, b.RecentMessages);
        Assert.Equal(99 - goblin.Hp, 99 - target.Hp);
    }

    [Fact]
    public void ClosingOnTheThegn_TellsTheEvenHand()
    {
        var game = EnterFortAt(TierSevenGame());
        var thegn = Thegn(game);
        Quiet(game, m => m == thegn);
        var map = game.CurrentSite!.Map;

        // Stand a step off a floor cell within two of the thegn, and walk onto it.
        foreach (var (dx, dy) in Directions.All8)
        {
            var onto = thegn.Pos.Plus(dx, dy);
            if (onto.Chebyshev(thegn.Pos) > 2 || map[onto] != Terrain.Floor
                || game.Monsters.Any(m => m.Alive && m.Pos == onto)) continue;
            var from = FindStepSource(game, onto, thegn.Pos);
            if (from is null) continue;
            game.Debug_SetPlayerPos(from.Value);
            game.ApplyKey(KeyFor(onto.X - from.Value.X, onto.Y - from.Value.Y));
            Assert.Contains(game.Log.Entries, e => e.Text.Contains("settles its weight"));
            return;
        }
        Assert.Fail("no approach cell within two of the thegn");
    }

    // ---- helpers ----

    private static Game BladeBearer(ulong seed)
    {
        var game = new Game(seed);
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("grave_iron");
        return game;
    }

    private static Game TierSevenGame(ulong seed = 42)
    {
        var game = new Game(seed);
        for (int i = 0; i < 6; i++) Cross(game);
        Assert.Equal(7, game.Cycle);
        Assert.NotNull(game.World.RingfortSite);
        // A bearer fit for the endless country: grave-iron in hand, so a heave
        // is a heave and a step is a step.
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("grave_iron");
        return game;
    }

    private static Game EnterFortAt(Game game)
    {
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

    private static Monster Thegn(Game game) =>
        game.Monsters.First(m => m.Alive && m.Kind == MonsterKind.Thegn && m.SiteId == game.CurrentSite!.Id);

    private static void Quiet(Game game, Func<Monster, bool>? keep = null)
    {
        foreach (var m in game.Monsters.Where(m => m.SiteId == game.CurrentSite!.Id))
            if (keep is null || !keep(m)) m.Hp = 0;
    }

    private static (Monster Goblin, char Key) AdjacentGoblin(Game game)
    {
        game.Debug_SetMode(MapMode.Site);
        var goblin = game.Monsters.First(m => m.Alive && m.SiteId == "goblin-camp");
        var map = game.World.Camp;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = goblin.Pos.Plus(dx, dy);
            if (map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p))
            {
                game.Debug_SetPlayerPos(p);
                return (goblin, KeyFor(goblin.Pos.X - p.X, goblin.Pos.Y - p.Y));
            }
        }
        throw new InvalidOperationException("goblin has no open neighbor");
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

    /// <summary>A stand two cardinal cells off the thegn, with the gap between open, so it must close.</summary>
    private static (Pos From, char Key) CardinalGap(Game game, Monster thegn)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.Cardinal)
        {
            var gap = thegn.Pos.Plus(-dx, -dy);
            var from = thegn.Pos.Plus(-2 * dx, -2 * dy);
            if (map.Walkable(gap) && map.Walkable(from)
                && !game.Monsters.Any(m => m.Alive && (m.Pos == gap || m.Pos == from)))
                return (from, KeyFor(dx, dy));
        }
        Assert.Fail("no cardinal gap to the thegn");
        return default;
    }

    /// <summary>An open cell with an open cardinal neighbor to heave into: (stand, key toward the empty cell).</summary>
    private static (Pos From, char Key) FindOpenWithOpenNeighbor(Game game, GameMap map)
    {
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                foreach (var (dx, dy) in Directions.Cardinal)
                {
                    var into = p.Plus(dx, dy);
                    if (map.Walkable(into) && !game.Monsters.Any(m => m.Alive && m.Pos == into))
                        return (p, KeyFor(dx, dy));
                }
            }
        Assert.Fail("no open cell in the camp");
        return default;
    }

    /// <summary>A walkable cell adjacent to <paramref name="onto"/> that is not the thegn's and stays within reach.</summary>
    private static Pos? FindStepSource(Game game, Pos onto, Pos avoid)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var from = onto.Plus(dx, dy);
            if (from == avoid || !map.Walkable(from)
                || game.Monsters.Any(m => m.Alive && m.Pos == from)) continue;
            if (from.Chebyshev(avoid) <= 3) return from;
        }
        return null;
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
