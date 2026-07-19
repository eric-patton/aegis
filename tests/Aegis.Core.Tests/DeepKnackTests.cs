using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The level-4 knack wave (D-055, the deferral D-046 left standing): every
/// second question pits the read moment against the even hand. One answer pays
/// only when the fight is read (a wind-up answered, a crowd weathered, an
/// opening picked); the other pays a little on every exchange regardless. The
/// queue puts older questions first, and the deep answer gets its own remark.
/// </summary>
public class DeepKnackTests
{
    [Fact]
    public void TheSecondWave_StandsInTheCatalog_OneQuestionPerSkill()
    {
        var wave = PerkCatalog.Choices.Where(c => c.Level == 4).ToList();
        Assert.Equal(5, wave.Count);
        // One deep question per combat skill; Hunting (D-070) and Cooking (D-073)
        // carry no knacks yet.
        Assert.Equal(
            Enum.GetValues<SkillId>().Where(s => s is not (SkillId.Hunting or SkillId.Cooking)),
            wave.Select(c => c.Skill));
        Assert.All(wave, c => Assert.Equal(2, c.Options.Length));

        // The older wave is put first: every level-2 question precedes every
        // level-4 question in the ledger's order.
        int firstDeep = PerkCatalog.Choices.ToList().FindIndex(c => c.Level == 4);
        Assert.All(PerkCatalog.Choices.Take(firstDeep), c => Assert.Equal(2, c.Level));

        // Twenty perks, twenty distinct stable ids.
        var ids = Enum.GetValues<PerkId>().Select(PerkCatalog.IdOf).ToList();
        Assert.Equal(20, ids.Count);
        Assert.Equal(20, ids.Distinct().Count());
        Assert.Contains("caught_arm", ids);
        Assert.Contains("waxed_string", ids);
    }

    [Fact]
    public void TheRiseToFour_AnnouncesTheDeeperQuestion()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        game.Player.Perks.Add(PerkId.FollowThrough); // level 2, long answered
        for (int i = 0; i < 55; i++) game.Player.Skills.AddUse(SkillId.Hafted);

        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 99;
        game.ApplyKey(key);
        Assert.Equal(4, game.Player.Skills.Level(SkillId.Hafted));
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("deepened into a second question"));
        Assert.Equal("hafted 4", game.TakeSnapshot().PendingKnack);
    }

    [Fact]
    public void TheQueue_PutsTheOlderQuestionFirst_AndTheDeepAnswer_IsRemarkedOnce()
    {
        var game = new Game(42);
        for (int i = 0; i < 56; i++) game.Player.Skills.AddUse(SkillId.Hafted);

        // Nothing answered at level 4: the level-2 question is put first.
        game.ApplyKey('c');
        Assert.Equal(2, game.PendingKnack!.Level);
        game.ApplyKey('1');
        Assert.True(game.Player.HasPerk(PerkId.FollowThrough));

        // The sheet stays open and puts the deep question, named as such.
        Assert.Equal(4, game.PendingKnack!.Level);
        var sheet = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("Hafted has deepened into a second question:", sheet);
        Assert.Contains("1) the checked swing", sheet);
        Assert.Contains("2) the true arc", sheet);

        game.ApplyKey('1');
        Assert.True(game.Player.HasPerk(PerkId.CheckedSwing));
        Assert.Null(game.PendingKnack);

        // One remark for the first knack, one for the first deep knack.
        Assert.Single(game.Log.Recent(12), e => e.Text.Contains("quite a book"));
        Assert.Single(game.Log.Recent(12), e => e.Text.Contains("I mean that as praise"));
    }

    [Fact]
    public void TheAnsweredCut_CollectsOnTheWindUp_AndOnlyThere()
    {
        // Same seed, same blade, same goblin mid wind-up; only the knack differs.
        var plain = BladeBearer(42);
        var (goblin, key) = AdjacentGoblin(plain);
        goblin.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = plain.Player.Pos };
        plain.ApplyKey(key);

        var answered = BladeBearer(42);
        answered.Player.Perks.Add(PerkId.AnsweredCut);
        (goblin, key) = AdjacentGoblin(answered);
        goblin.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = answered.Player.Pos };
        answered.ApplyKey(key);
        Assert.Equal(LastStrikeDamage(plain) + 2, LastStrikeDamage(answered));

        // No wind-up, no collection.
        var idlePlain = BladeBearer(42);
        (goblin, key) = AdjacentGoblin(idlePlain);
        goblin.Hp = 99;
        idlePlain.ApplyKey(key);
        var idle = BladeBearer(42);
        idle.Player.Perks.Add(PerkId.AnsweredCut);
        (goblin, key) = AdjacentGoblin(idle);
        goblin.Hp = 99;
        idle.ApplyKey(key);
        Assert.Equal(LastStrikeDamage(idlePlain), LastStrikeDamage(idle));
    }

    [Fact]
    public void TheStroppedEdge_SparesTheBlade_EverySecondSwing()
    {
        var game = BladeBearer(42);
        game.Player.Perks.Add(PerkId.StroppedEdge);
        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 99;

        for (int i = 0; i < 4; i++)
        {
            game.Player.Stamina = game.Player.MaxStamina;
            game.ApplyKey(key);
        }
        Assert.Equal(4, game.Player.Skills.Uses(SkillId.Blades));
        Assert.Equal(2, game.Player.Weapon!.Wear);
    }

    [Fact]
    public void TheCheckedSwing_BreaksTheWindUp_ButOnlyAPaidBlowHasTheWeight()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        game.Player.Perks.Add(PerkId.CheckedSwing);
        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 99;

        // Two turns out, so an unchecked wind-up survives the goblin's turn.
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos, TurnsUntilResolve = 2 };
        game.ApplyKey(key);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("dies unthrown"));

        // Winded, the blow is feeble, and feeble checks nothing.
        var raised = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos, TurnsUntilResolve = 2 };
        goblin.Intent = raised;
        game.Player.Stamina = 0;
        game.ApplyKey(key);
        Assert.Same(raised, goblin.Intent);
        Assert.Single(game.Log.Recent(20), e => e.Text.Contains("dies unthrown"));
    }

    [Fact]
    public void TheCaughtArm_WalksIn_BareHandedOnly_AndTheShortPath_SavesWind()
    {
        var plain = new Game(42);
        var (goblin, key) = AdjacentGoblin(plain);
        goblin.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = plain.Player.Pos };
        plain.ApplyKey(key);

        var caught = new Game(42);
        caught.Player.Perks.Add(PerkId.CaughtArm);
        (goblin, key) = AdjacentGoblin(caught);
        goblin.Hp = 99;
        goblin.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = caught.Player.Pos };
        caught.ApplyKey(key);
        Assert.Equal(LastStrikeDamage(plain) + 3, LastStrikeDamage(caught));

        // An axe in hand is not a fist: the door does not open for iron.
        var armed = new Game(42);
        armed.Player.Weapon = GearCatalog.Create("woodaxe");
        armed.Player.Perks.Add(PerkId.CaughtArm);
        var armedPlain = new Game(42);
        armedPlain.Player.Weapon = GearCatalog.Create("woodaxe");
        foreach (var g in new[] { armed, armedPlain })
        {
            var (gob, k) = AdjacentGoblin(g);
            gob.Hp = 99;
            gob.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = g.Player.Pos };
            g.ApplyKey(k);
        }
        Assert.Equal(LastStrikeDamage(armedPlain), LastStrikeDamage(armed));

        var shortPath = new Game(42);
        shortPath.Player.Perks.Add(PerkId.ShortPath);
        (goblin, key) = AdjacentGoblin(shortPath);
        goblin.Hp = 99;
        int wind = shortPath.Player.Stamina;
        shortPath.ApplyKey(key);
        Assert.Equal(wind - 2, shortPath.Player.Stamina);
    }

    [Fact]
    public void TheFittedIron_TurnsOneMore_Always()
    {
        var plain = new Game(42);
        plain.Player.Armor = GearCatalog.Create("quilted_jack");
        AdjacentGoblin(plain);

        var fitted = new Game(42);
        fitted.Player.Armor = GearCatalog.Create("quilted_jack");
        fitted.Player.Perks.Add(PerkId.FittedIron);
        AdjacentGoblin(fitted);

        for (int i = 0; i < 40 && plain.Player.Hp > 8; i++)
        {
            plain.ApplyKey('.');
            fitted.ApplyKey('.');
        }
        Assert.Equal(FirstBlowDamage(plain) - 1, FirstBlowDamage(fitted));
    }

    [Fact]
    public void TheShieldWall_ClosesRanks_OnlyWhenCrowded()
    {
        // Two goblins beside the bearer: the second head is worth 1 more turn.
        var plain = CrowdedBearer(42, out _);
        var walled = CrowdedBearer(42, out _);
        walled.Player.Perks.Add(PerkId.ShieldWall);
        for (int i = 0; i < 40; i++)
        {
            plain.Player.Hp = plain.Player.MaxHp;
            walled.Player.Hp = walled.Player.MaxHp;
            plain.ApplyKey('.');
            walled.ApplyKey('.');
        }
        Assert.Equal(FirstBlowDamage(plain) - 1, FirstBlowDamage(walled));

        // One goblin alone is no crowd: the knack turns nothing extra.
        var single = new Game(42);
        single.Player.Armor = GearCatalog.Create("quilted_jack");
        AdjacentGoblin(single);
        var singleWalled = new Game(42);
        singleWalled.Player.Armor = GearCatalog.Create("quilted_jack");
        singleWalled.Player.Perks.Add(PerkId.ShieldWall);
        AdjacentGoblin(singleWalled);
        for (int i = 0; i < 40 && single.Player.Hp > 8; i++)
        {
            single.ApplyKey('.');
            singleWalled.ApplyKey('.');
        }
        Assert.Equal(FirstBlowDamage(single), FirstBlowDamage(singleWalled));
    }

    [Fact]
    public void ThePickedMoment_FindsTheBodyMidMove()
    {
        // A mark mid wind-up takes the shaft 2 deeper.
        int winding = ShaftDamage(42, PerkId.PickedMoment, m => m.Intent =
            new Intent { Kind = IntentKind.CrushingBlow, TargetCell = new Pos(1, 1) });
        int windingPlain = ShaftDamage(42, null, m => m.Intent =
            new Intent { Kind = IntentKind.CrushingBlow, TargetCell = new Pos(1, 1) });
        Assert.Equal(windingPlain + 2, winding);

        // A mark standing open takes it 2 deeper too; a settled one does not.
        int open = ShaftDamage(42, PerkId.PickedMoment, m => m.ExposedTurns = 2);
        int openPlain = ShaftDamage(42, null, m => m.ExposedTurns = 2);
        Assert.Equal(openPlain + 2, open);

        int settled = ShaftDamage(42, PerkId.PickedMoment, _ => { });
        int settledPlain = ShaftDamage(42, null, _ => { });
        Assert.Equal(settledPlain, settled);
    }

    [Fact]
    public void TheWaxedString_FraysHalfAsFast_HitOrMiss()
    {
        var plain = WallShooter(42, waxed: false, shots: 4);
        Assert.Equal(4, plain.Player.Bow!.Wear);
        Assert.Equal(4, plain.Player.Looses);

        var waxed = WallShooter(42, waxed: true, shots: 4);
        Assert.Equal(2, waxed.Player.Bow!.Wear);
        Assert.Equal(4, waxed.Player.Looses);
    }

    [Fact]
    public void ADeepSession_ReplaysIdenticallyFromJournal()
    {
        const ulong seed = 42;
        var live = DeepBrawler(seed);
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);

        var (goblin, key) = AdjacentGoblin(live);
        goblin.Hp = 99;
        live.ApplyKey(key);            // the rise that opens the deep question
        live.ApplyKey('c');
        live.ApplyKey('1');            // the caught arm, chosen
        live.ApplyKey(' ');
        live.ApplyKey(key);
        Assert.True(live.Player.HasPerk(PerkId.CaughtArm));

        var replayed = DeepBrawler(seed);
        var (goblin2, _) = AdjacentGoblin(replayed);
        goblin2.Hp = 99;
        foreach (char k in journal.ToString()) replayed.ApplyKey(k);

        Assert.Equal(live.Player.Perks, replayed.Player.Perks);
        var (a, b) = (live.TakeSnapshot(), replayed.TakeSnapshot());
        Assert.Equal(a.Perks, b.Perks);
        Assert.Equal(a.PendingKnack, b.PendingKnack);
        Assert.Equal(a.Skills, b.Skills);
        Assert.Equal(a.RecentMessages, b.RecentMessages);
    }

    /// <summary>A brawler one swing short of level 4, level 2 long answered.</summary>
    private static Game DeepBrawler(ulong seed)
    {
        var game = new Game(seed);
        game.Player.Perks.Add(PerkId.KnuckleAndBone);
        for (int i = 0; i < 55; i++) game.Player.Skills.AddUse(SkillId.Brawling);
        return game;
    }

    /// <summary>A bearer who meets the grave-iron's asking, so blade tests read clean.</summary>
    private static Game BladeBearer(ulong seed)
    {
        var game = new Game(seed);
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("grave_iron");
        return game;
    }

    /// <summary>An armored bearer with two goblins stood beside them.</summary>
    private static Game CrowdedBearer(ulong seed, out Monster second)
    {
        var game = new Game(seed);
        game.Player.Armor = GearCatalog.Create("quilted_jack");
        AdjacentGoblin(game);
        second = game.Monsters.First(m => m.Alive && m.SiteId == "goblin-camp"
            && m.Pos.Chebyshev(game.Player.Pos) > 1);
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

    /// <summary>Looses once at a camp goblin prepared by stage; returns the shaft's damage.</summary>
    private static int ShaftDamage(ulong seed, PerkId? perk, Action<Monster> stage)
    {
        var game = new Game(seed);
        game.Debug_SetPlayerPos(game.World.CampPos);
        game.Apply(Command.Enter);
        game.Debug_GrantGear("hunting_bow");
        if (perk is { } p) game.Player.Perks.Add(p);

        var (mark, from, key) = FindLine(game, minLen: 2);
        mark.Hp = 99;
        stage(mark);
        game.Debug_SetPlayerPos(from);
        game.ApplyKey('f');
        game.ApplyKey(key);

        string text = game.Log.Recent(4).Last(e => e.Text.Contains("Your shaft takes the")).Text;
        int start = text.LastIndexOf("for ") + 4;
        return int.Parse(text[start..text.IndexOf('.', start)]);
    }

    /// <summary>Splinters shafts against camp stone: draws that fray and teach nothing.</summary>
    private static Game WallShooter(ulong seed, bool waxed, int shots)
    {
        var game = new Game(seed);
        game.Debug_SetPlayerPos(game.World.CampPos);
        game.Apply(Command.Enter);
        game.Debug_GrantGear("hunting_bow");
        if (waxed) game.Player.Perks.Add(PerkId.WaxedString);

        var map = game.CurrentSite!.Map;
        (Pos from, char key)? shot = null;
        for (int y = 1; y < map.Height - 1 && shot is null; y++)
            for (int x = 1; x < map.Width - 1 && shot is null; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                if (!map.Walkable(p.Plus(1, 0))) shot = (p, 'l');
                else if (!map.Walkable(p.Plus(-1, 0))) shot = (p, 'h');
            }
        Assert.NotNull(shot);
        game.Debug_SetPlayerPos(shot!.Value.from);

        for (int i = 0; i < shots; i++)
        {
            game.Player.Stamina = game.Player.MaxStamina;
            game.ApplyKey('f');
            game.ApplyKey(shot.Value.key);
        }
        return game;
    }

    /// <summary>A monster with a clear straight line of at least minLen cells.</summary>
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
        string text = game.Log.Recent(5).Last(e => e.Text.Contains("You strike the")).Text;
        int start = text.LastIndexOf("for ") + 4;
        return int.Parse(text[start..text.IndexOf('.', start)]);
    }

    /// <summary>Damage of the first telegraphed crushing blow that landed, from the log.</summary>
    private static int FirstBlowDamage(Game game)
    {
        string text = game.Log.Recent(1000).First(e => e.Text.Contains("crushing blow lands for")).Text;
        int start = text.LastIndexOf("for ") + 4;
        return int.Parse(text[start..text.IndexOf('!', start)]);
    }
}
