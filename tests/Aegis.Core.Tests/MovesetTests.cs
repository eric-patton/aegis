using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The iron's verbs (D-056, paying D-004's oldest promise: weapons change
/// verbs, not just numbers). The broad irons carry an arc through the ring;
/// the grave-iron answers a read blow stood through; the ash spear strikes
/// two strides out and pays like the swing it is. Bare fists keep their verbs
/// in the knacks, and the bow's verb has been the loosed line since D-050.
/// </summary>
public class MovesetTests
{
    [Fact]
    public void TheVerbs_HangOnTheIron_NotTheFamily()
    {
        Assert.Equal(MoveVerb.Arc, GearCatalog.Create("woodaxe").Move);
        Assert.Equal(MoveVerb.Arc, GearCatalog.Create("carvers_maul").Move);
        Assert.Equal(MoveVerb.Answer, GearCatalog.Create("grave_iron").Move);

        var spear = GearCatalog.Create("ash_spear");
        Assert.Equal(MoveVerb.Reach, spear.Move);
        Assert.Equal(SkillId.Hafted, spear.Family);
        Assert.Equal(Attr.Might, spear.ReqAttr);
        Assert.Equal(6, spear.Req);

        // The axe and the spear share a skill and not a verb; the bow keeps
        // the loosed line, and batting has no verb at all.
        Assert.Equal(MoveVerb.None, GearCatalog.Create("hunting_bow").Move);
        Assert.Equal(MoveVerb.None, GearCatalog.Create("warbow").Move);
        Assert.Equal(MoveVerb.None, GearCatalog.Create("quilted_jack").Move);

        // The smith's fifth ware, appended so the bow's digit never shifted.
        Assert.Equal(5, GearCatalog.SmithStock.Length);
        Assert.Equal("ash_spear", GearCatalog.SmithStock[^1]);
    }

    [Fact]
    public void TheArc_CarriesThroughTheRing_AtHalfItsWeight()
    {
        var game = CrowdedAxeBearer(42, out var second, out var far);
        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 99;
        second.Hp = 99;
        int farHp = far.Hp;

        game.ApplyKey(key);
        int struck = LastStrikeDamage(game);
        int carry = Math.Max(1, struck / 2);
        Assert.Equal(99 - carry, second.Hp);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains($"carries through into the goblin for {carry}"));
        Assert.Equal(farHp, far.Hp);
    }

    [Fact]
    public void TheArc_AsksAPaidSwing_AndBroadIron()
    {
        // Winded, the blow is feeble, and a feeble blow carries nowhere.
        var winded = CrowdedAxeBearer(42, out var second, out _);
        var (goblin, key) = AdjacentGoblin(winded);
        goblin.Hp = 99;
        second.Hp = 99;
        winded.Player.Stamina = 0;
        winded.ApplyKey(key);
        Assert.Equal(99, second.Hp);

        // A blade is quick iron, not broad: no arc. Bare fists neither.
        var bladed = CrowdedAxeBearer(42, out second, out _);
        bladed.Player.Attributes[Attr.Might] = 7;
        bladed.Player.Weapon = GearCatalog.Create("grave_iron");
        (goblin, key) = AdjacentGoblin(bladed);
        goblin.Hp = 99;
        second.Hp = 99;
        bladed.ApplyKey(key);
        Assert.Equal(99, second.Hp);

        var bare = CrowdedAxeBearer(42, out second, out _);
        bare.Player.Weapon = null;
        (goblin, key) = AdjacentGoblin(bare);
        goblin.Hp = 99;
        second.Hp = 99;
        bare.ApplyKey(key);
        Assert.Equal(99, second.Hp);
    }

    [Fact]
    public void TheArc_CanFell_AndPaysTheRemains()
    {
        var game = CrowdedAxeBearer(42, out var second, out _);
        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 99;
        second.Hp = 1;
        int essence = game.Player.Essence;

        game.ApplyKey(key);
        Assert.False(second.Alive);
        Assert.Equal(essence + 5, game.Player.Essence);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("The goblin falls."));

        // One swing, one wear, one counted use: the carry is the same blow.
        Assert.Equal(1, game.Player.Weapon!.Wear);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Hafted));
    }

    [Fact]
    public void TheAnswer_MeetsTheStoodBlow_ForFree()
    {
        var game = BladeBearer(42);
        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };
        _ = key;

        game.ApplyKey('.');
        // The blow landed, and the answer came back over the iron: 1 + the
        // blade's whole effective bonus, for no wind, no wear, no counted use.
        Assert.Equal(99 - 5, goblin.Hp);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("answer over the iron"));
        Assert.Equal(0, game.Player.Weapon!.Wear);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Blades));
    }

    [Fact]
    public void TheAnswer_AsksTheIron_AndTheTouch()
    {
        // No blade in hand: the blow is only taken.
        var bare = new Game(42);
        var (goblin, _) = AdjacentGoblin(bare);
        goblin.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = bare.Player.Pos };
        bare.ApplyKey('.');
        Assert.Equal(99, goblin.Hp);

        // A read blow from beyond arm's reach lands the same and is not
        // answered: the iron cannot reach what threw it.
        var far = BladeBearer(42);
        (goblin, _) = AdjacentGoblin(far);
        goblin.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = far.Player.Pos };
        goblin.Pos = OpenCellAway(far, from: far.Player.Pos, minDist: 3);
        int hpBefore = far.Player.Hp;
        far.ApplyKey('.');
        Assert.True(far.Player.Hp < hpBefore, "the staged blow did not land");
        Assert.Equal(99, goblin.Hp);

        // A blow dodged by feet is a blow never answered.
        var stepped = BladeBearer(42);
        var (gob, _) = AdjacentGoblin(stepped);
        gob.Hp = 99;
        gob.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = stepped.Player.Pos };
        StepAnywhereElse(stepped, avoid: gob.Pos);
        Assert.Equal(99, gob.Hp);
    }

    [Fact]
    public void TheAnswer_CanFell()
    {
        var game = BladeBearer(42);
        var (goblin, _) = AdjacentGoblin(game);
        goblin.Hp = 1;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };

        game.ApplyKey('.');
        Assert.False(goblin.Alive);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("The goblin falls."));
    }

    [Fact]
    public void TheAnswer_Holds_WhileTheLayingMomentStandsOpen()
    {
        var game = Crossed(new Game(42), 1);
        game.Player.Resolution = Resolution.Kept;
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("grave_iron");
        EnterHollow(game);
        var keeper = Keeper(game);
        keeper.Hp = 99;

        // The keeper's cut lands; the hand starts the answer and holds it.
        game.Debug_SetPlayerPos(AdjacentOpen(game, keeper.Pos));
        keeper.Intent = new Intent { Kind = IntentKind.SunderingCut, TargetCell = game.Player.Pos };
        game.Player.Hp = game.Player.MaxHp;
        game.ApplyKey('.');
        Assert.Equal(99, keeper.Hp);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("you hold it"));

        // The old way chosen, the moment closes, and the blade answers again.
        if (game.Player.Pos.Chebyshev(keeper.Pos) != 1)
            game.Debug_SetPlayerPos(AdjacentOpen(game, keeper.Pos));
        BumpInto(game, keeper.Pos);
        game.ApplyKey('1');
        int hp = keeper.Hp;
        keeper.Intent = new Intent { Kind = IntentKind.SunderingCut, TargetCell = game.Player.Pos };
        game.Player.Hp = game.Player.MaxHp;
        game.ApplyKey('.');
        Assert.Equal(hp - 5, keeper.Hp);
    }

    [Fact]
    public void TheThrust_StrikesAtTwoStrides_AndPaysLikeASwing()
    {
        var game = SpearBearer(42);
        var (mark, from, key) = FindLine(game, minLen: 2);
        mark.Hp = 99;
        game.Debug_SetPlayerPos(from);

        int wind = game.Player.Stamina;
        game.ApplyKey('t');
        Assert.True(game.InThrust);
        game.ApplyKey(key);
        Assert.False(game.InThrust);

        int dealt = 99 - mark.Hp;
        Assert.True(dealt > 0, "the thrust found no body");
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains($"at the spear's length for {dealt}"));
        Assert.Equal(wind - 4, game.Player.Stamina);
        Assert.Equal(1, game.Player.Weapon!.Wear);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Hafted));
    }

    [Fact]
    public void TheThrust_Refuses_OrSpendsHonestly()
    {
        // No spear: no reach, no turn.
        var bare = new Game(42);
        var (_, _) = AdjacentGoblin(bare);
        int turn = bare.Turn;
        bare.ApplyKey('t');
        Assert.False(bare.InThrust);
        Assert.Equal(turn, bare.Turn);
        Assert.Contains(bare.Log.Recent(2), e => e.Text.Contains("nothing with that kind of reach"));

        // Winded, the point refuses outright: tempo is the only cost.
        var winded = SpearBearer(42);
        winded.Player.Stamina = 3;
        turn = winded.Turn;
        winded.ApplyKey('t');
        Assert.False(winded.InThrust);
        Assert.Equal(turn, winded.Turn);

        // Any other key lowers the point free; a line chosen into empty air
        // spends the turn, the wind, and the edge, and teaches nothing.
        var game = SpearBearer(42);
        var (_, from, key) = FindLine(game, minLen: 3);
        game.Debug_SetPlayerPos(from);
        game.ApplyKey('t');
        game.ApplyKey(' ');
        Assert.False(game.InThrust);
        Assert.Equal(0, game.Player.Weapon!.Wear);

        game.ApplyKey('t');
        game.ApplyKey(key == 'l' ? 'h' : 'l');
        Assert.Equal(1, game.Player.Weapon!.Wear);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Hafted));
    }

    [Fact]
    public void TheThrust_MeetsTheBoard_AndFindsTheWindow()
    {
        var game = EnterFort();
        game.Player.Attributes[Attr.Might] = 6;
        game.Debug_GrantGear("ash_spear");
        Quiet(game, m => m.Kind == MonsterKind.Carl);

        var (carl, from, key) = FindLine(game, minLen: 2);
        int hp = carl.Hp;
        game.Debug_SetPlayerPos(from);
        game.ApplyKey('t');
        game.ApplyKey(key);
        Assert.Equal(hp, carl.Hp);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Hafted));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("turned along the grain"));

        // While the seax is about its blow, the board has left its line, and
        // the point goes in where the shaft would.
        game.Debug_SetPlayerPos(from);
        game.Player.Stamina = game.Player.MaxStamina;
        carl.Intent = new Intent { Kind = IntentKind.SeaxStab, TargetCell = game.Player.Pos, TurnsUntilResolve = 2 };
        game.ApplyKey('t');
        game.ApplyKey(key);
        Assert.True(carl.Hp < hp, "the open carl was not struck");
    }

    [Fact]
    public void TheThrust_HasTheOldWaysReach()
    {
        var game = Crossed(new Game(42), 1);
        game.Player.Resolution = Resolution.Kept;
        game.Player.Attributes[Attr.Might] = 6;
        game.Player.Weapon = GearCatalog.Create("ash_spear");
        EnterHollow(game);
        var keeper = Keeper(game);
        keeper.Hp = 99;

        var (mark, from, key) = FindLine(game, minLen: 2);
        Assert.Same(keeper, mark);
        game.Debug_SetPlayerPos(from);
        game.ApplyKey('t');
        game.ApplyKey(key);

        // The moment closes and the thrust lands: the old way has a reach.
        Assert.True(keeper.Hp < 99);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("The old way has a reach"));
        if (game.Player.Pos.Chebyshev(keeper.Pos) != 1)
            game.Debug_SetPlayerPos(AdjacentOpen(game, keeper.Pos));
        BumpInto(game, keeper.Pos);
        Assert.False(game.InLayingMenu);
    }

    [Fact]
    public void AMovesetSession_ReplaysIdenticallyFromJournal()
    {
        const ulong seed = 42;
        var live = SpearBearer(seed);
        var (mark, from, key) = FindLine(live, minLen: 2);
        mark.Hp = 99;
        live.Debug_SetPlayerPos(from);
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);
        live.ApplyKey('t');
        live.ApplyKey(key);
        live.ApplyKey('.');

        var replayed = SpearBearer(seed);
        var (mark2, from2, _) = FindLine(replayed, minLen: 2);
        mark2.Hp = 99;
        replayed.Debug_SetPlayerPos(from2);
        foreach (char k in journal.ToString()) replayed.ApplyKey(k);

        var (a, b) = (live.TakeSnapshot(), replayed.TakeSnapshot());
        Assert.Equal(a.Skills, b.Skills);
        Assert.Equal(a.WeaponWear, b.WeaponWear);
        Assert.Equal(a.Stamina, b.Stamina);
        Assert.Equal(a.RecentMessages, b.RecentMessages);
    }

    // ---- helpers ----

    /// <summary>A bearer who meets the ash spear's asking, spear in hand, stood in the camp.</summary>
    private static Game SpearBearer(ulong seed)
    {
        var game = new Game(seed);
        game.Player.Attributes[Attr.Might] = 6;
        game.Player.Weapon = GearCatalog.Create("ash_spear");
        game.Debug_SetMode(MapMode.Site);
        return game;
    }

    /// <summary>A bearer who meets the grave-iron's asking, blade in hand.</summary>
    private static Game BladeBearer(ulong seed)
    {
        var game = new Game(seed);
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("grave_iron");
        return game;
    }

    /// <summary>An axe-bearer with a second goblin moved to their side and a third left far.</summary>
    private static Game CrowdedAxeBearer(ulong seed, out Monster second, out Monster far)
    {
        var game = new Game(seed);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        AdjacentGoblin(game);
        var others = game.Monsters.Where(m => m.Alive && m.SiteId == "goblin-camp"
            && m.Pos.Chebyshev(game.Player.Pos) > 1).ToList();
        second = others[0];
        far = others[1];
        var map = game.World.Camp;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = game.Player.Pos.Plus(dx, dy);
            if (map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p))
            {
                second.Pos = p;
                return game;
            }
        }
        throw new InvalidOperationException("no room for a crowd");
    }

    private static Game Crossed(Game game, int times)
    {
        for (int i = 0; i < times; i++)
        {
            game.Debug_SetMode(MapMode.Overworld);
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.Apply(Command.Enter);
            game.Apply(Command.Enter);
        }
        return game;
    }

    private static void EnterHollow(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.HollowSite!.OverworldPos);
        game.Apply(Command.Enter);
    }

    private static Monster Keeper(Game game) =>
        game.Monsters.First(m => m.Alive && m.Kind == MonsterKind.Severed
            && m.SiteId == game.CurrentSite!.Id);

    private static Game EnterFort()
    {
        var game = new Game(42);
        Crossed(game, 4);
        game.Debug_SetPlayerPos(game.World.RingfortSite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("ringfort", game.CurrentSite!.Id);
        return game;
    }

    /// <summary>Quiets every tenant of the current site the predicate does not keep.</summary>
    private static void Quiet(Game game, Func<Monster, bool>? keep = null)
    {
        foreach (var m in game.Monsters.Where(m => m.SiteId == game.CurrentSite!.Id))
            if (keep is null || !keep(m)) m.Hp = 0;
    }

    /// <summary>A clear straight line of at least minLen cells to any live tenant.</summary>
    private static (Monster Mark, Pos From, char Key) FindLine(Game game, int minLen)
    {
        var map = game.CurrentSite!.Map;
        string siteId = game.CurrentSite.Id;
        foreach (var mark in game.Monsters.Where(m => m.Alive && m.SiteId == siteId))
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

    /// <summary>Teleports into the camp beside a goblin; returns it and the key that strikes it.</summary>
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

    /// <summary>An open cell at least minDist from the given point, for staging distance.</summary>
    private static Pos OpenCellAway(Game game, Pos from, int minDist)
    {
        var map = game.CurrentSite!.Map;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || p.Chebyshev(from) < minDist) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                return p;
            }
        Assert.Fail("no open cell far enough away");
        return default;
    }

    private static void BumpInto(Game game, Pos target)
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
            game.ApplyKey(KeyFor(dx, dy));
            return;
        }
        Assert.Fail("nowhere to sidestep");
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

    private static int LastStrikeDamage(Game game)
    {
        string text = game.Log.Recent(6).Last(e => e.Text.Contains("You strike the")).Text;
        int start = text.LastIndexOf("for ") + 4;
        return int.Parse(text[start..text.IndexOf('.', start)]);
    }
}
