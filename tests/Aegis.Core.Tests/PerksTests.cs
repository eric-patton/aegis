using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Knacks (D-046, the perk half of D-016): at a skill's level-2 threshold the
/// sheet puts a two-way question of style, a digit answers it forever, and the
/// answer changes later damage, wind, or wear. Choices are journaled keys, so
/// replay carries them; the sibling not taken is foreclosed for good.
/// </summary>
public class PerksTests
{
    [Fact]
    public void ThresholdRise_AnnouncesTheQuestion_AndSnapshotCarriesIt()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        for (int i = 0; i < 19; i++) game.Player.Skills.AddUse(SkillId.Hafted);

        var (_, key) = AdjacentGoblin(game);
        game.ApplyKey(key);
        Assert.Equal(2, game.Player.Skills.Level(SkillId.Hafted));
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("question of style"));

        var snap = game.TakeSnapshot();
        Assert.Equal("hafted", snap.PendingKnack);
        Assert.Equal("", snap.Perks);
    }

    [Fact]
    public void TheSheet_PutsTheQuestion_AndADigitAnswersForGood()
    {
        var game = new Game(42);
        for (int i = 0; i < 20; i++) game.Player.Skills.AddUse(SkillId.Hafted);

        int turn = game.Turn;
        game.ApplyKey('c');
        Assert.True(game.InSheetMenu);

        var sheet = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("Hafted has settled into a question", sheet);
        Assert.Contains("1) the follow-through", sheet);
        Assert.Contains("2) the kind grip", sheet);

        game.ApplyKey('1');
        Assert.True(game.InSheetMenu); // choosing keeps the sheet open
        Assert.True(game.Player.HasPerk(PerkId.FollowThrough));
        Assert.False(game.Player.HasPerk(PerkId.KindGrip));
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("the follow-through is yours"));

        // Answered: the question is gone, the knack shows on its skill's row.
        var snap = game.TakeSnapshot();
        Assert.Equal("follow_through", snap.Perks);
        Assert.Equal("", snap.PendingKnack);
        var after = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.DoesNotContain("settled into a question", after);
        Assert.Contains("the follow-through", after);

        game.ApplyKey(' ');
        Assert.False(game.InSheetMenu);
        Assert.Equal(turn, game.Turn); // the whole exchange cost no time
    }

    [Fact]
    public void TheSibling_IsForeclosed_ForGood()
    {
        var game = new Game(42);
        for (int i = 0; i < 20; i++) game.Player.Skills.AddUse(SkillId.Hafted);
        game.ApplyKey('c');
        game.ApplyKey('1');

        // With the question answered, '2' is just a key like any other: it closes.
        game.ApplyKey('2');
        Assert.False(game.InSheetMenu);
        Assert.Single(game.Player.Perks);
        Assert.False(game.Player.HasPerk(PerkId.KindGrip));
    }

    [Fact]
    public void TwoQuestions_ArePutOneAtATime_InTheLedgersOrder()
    {
        var game = new Game(42);
        for (int i = 0; i < 20; i++) game.Player.Skills.AddUse(SkillId.Blades);
        for (int i = 0; i < 20; i++) game.Player.Skills.AddUse(SkillId.Warding);

        game.ApplyKey('c');
        Assert.Equal(SkillId.Blades, game.PendingKnack!.Skill);
        game.ApplyKey('1');
        Assert.True(game.Player.HasPerk(PerkId.DrawnCut));

        // The sheet stays open and puts the next question without reopening.
        Assert.True(game.InSheetMenu);
        Assert.Equal(SkillId.Warding, game.PendingKnack!.Skill);
        game.ApplyKey('2');
        Assert.True(game.Player.HasPerk(PerkId.MendedStrap));
        Assert.Null(game.PendingKnack);

        // The Aegis remarked on the first knack only.
        Assert.Single(game.Log.Recent(8), e => e.Text.Contains("quite a book"));
    }

    [Fact]
    public void DrawnCut_DeepensTheBlade_AndSpareMotion_SavesWind()
    {
        // Same seed, same blade, same goblin; only the knack differs. The
        // goblin is thickened so the blade cannot end it before it testifies.
        var plain = BladeBearer(42);
        var (goblin, key) = AdjacentGoblin(plain);
        goblin.Hp = 99;
        plain.ApplyKey(key);

        var drawn = BladeBearer(42);
        drawn.Player.Perks.Add(PerkId.DrawnCut);
        (goblin, key) = AdjacentGoblin(drawn);
        goblin.Hp = 99;
        drawn.ApplyKey(key);
        Assert.Equal(LastStrikeDamage(plain) + 1, LastStrikeDamage(drawn));
        Assert.Equal(plain.Player.Stamina, drawn.Player.Stamina);

        var spare = BladeBearer(42);
        spare.Player.Perks.Add(PerkId.SpareMotion);
        (goblin, key) = AdjacentGoblin(spare);
        goblin.Hp = 99;
        spare.ApplyKey(key);
        Assert.Equal(LastStrikeDamage(plain), LastStrikeDamage(spare));
        Assert.Equal(plain.Player.Stamina + 1, spare.Player.Stamina);
    }

    [Fact]
    public void FollowThrough_HandsWindBack_OnTheKillingSwing()
    {
        var plain = new Game(42);
        plain.Player.Weapon = GearCatalog.Create("woodaxe");
        var (goblin, key) = AdjacentGoblin(plain);
        goblin.Hp = 1;
        plain.ApplyKey(key);
        Assert.False(goblin.Alive);

        var followed = new Game(42);
        followed.Player.Weapon = GearCatalog.Create("woodaxe");
        followed.Player.Perks.Add(PerkId.FollowThrough);
        (goblin, key) = AdjacentGoblin(followed);
        goblin.Hp = 1;
        followed.ApplyKey(key);
        Assert.False(goblin.Alive);

        Assert.Equal(plain.Player.Stamina + 2, followed.Player.Stamina);
    }

    [Fact]
    public void KindGrip_SparesTheEdge_EverySecondSwing()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        game.Player.Perks.Add(PerkId.KindGrip);
        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 99;

        for (int i = 0; i < 4; i++)
        {
            game.Player.Stamina = game.Player.MaxStamina;
            game.ApplyKey(key);
        }
        Assert.Equal(4, game.Player.Skills.Uses(SkillId.Hafted));
        Assert.Equal(2, game.Player.Weapon!.Wear);
    }

    [Fact]
    public void KnuckleAndBone_HardensBareFists_AndDeepBreath_WidensTheWind()
    {
        var plain = new Game(42);
        var (_, key) = AdjacentGoblin(plain);
        plain.ApplyKey(key);

        var knuckled = new Game(42);
        knuckled.Player.Perks.Add(PerkId.KnuckleAndBone);
        (_, key) = AdjacentGoblin(knuckled);
        knuckled.ApplyKey(key);
        Assert.Equal(LastStrikeDamage(plain) + 2, LastStrikeDamage(knuckled));

        // Knuckle and bone is a bare-hand craft: an axe in hand gets nothing.
        var armed = new Game(42);
        armed.Player.Weapon = GearCatalog.Create("woodaxe");
        armed.Player.Perks.Add(PerkId.KnuckleAndBone);
        var armedPlain = new Game(42);
        armedPlain.Player.Weapon = GearCatalog.Create("woodaxe");
        (_, key) = AdjacentGoblin(armed);
        armed.ApplyKey(key);
        (_, key) = AdjacentGoblin(armedPlain);
        armedPlain.ApplyKey(key);
        Assert.Equal(LastStrikeDamage(armedPlain), LastStrikeDamage(armed));

        var breathed = new Game(42);
        Assert.Equal(10, breathed.Player.MaxStamina);
        breathed.Player.Perks.Add(PerkId.DeepBreath);
        Assert.Equal(12, breathed.Player.MaxStamina);
    }

    [Fact]
    public void BracedShoulder_TurnsTelegraphedBlows_TwoFurther()
    {
        // Same seed, same jack, standing into the same blows; the braced arm
        // gives up 2 less on every telegraph the iron turns, and bites match.
        var plain = new Game(42);
        plain.Player.Armor = GearCatalog.Create("quilted_jack");
        AdjacentGoblin(plain);

        var braced = new Game(42);
        braced.Player.Armor = GearCatalog.Create("quilted_jack");
        braced.Player.Perks.Add(PerkId.BracedShoulder);
        AdjacentGoblin(braced);

        for (int i = 0; i < 40 && plain.Player.Hp > 8; i++)
        {
            plain.ApplyKey('.');
            braced.ApplyKey('.');
        }

        int plainBlow = FirstBlowDamage(plain);
        int bracedBlow = FirstBlowDamage(braced);
        Assert.Equal(plainBlow - 2, bracedBlow);
    }

    [Fact]
    public void MendedStrap_SparesTheStraps_EverySecondTurnedBlow()
    {
        var game = new Game(42);
        game.Player.Armor = GearCatalog.Create("quilted_jack");
        game.Player.Perks.Add(PerkId.MendedStrap);
        var (goblin, _) = AdjacentGoblin(game);

        for (int i = 0; i < 40 && goblin.Alive && game.Player.Hp > 8; i++)
        {
            game.ApplyKey('.');
            game.Player.Hp = game.Player.MaxHp; // stand in it as long as it takes
        }

        int turned = game.Player.Skills.Uses(SkillId.Warding);
        Assert.True(turned >= 2, "the goblin never landed enough turnable blows");
        Assert.Equal((turned + 1) / 2, game.Player.Armor!.Wear);
    }

    [Fact]
    public void KnackSession_ReplaysIdenticallyFromJournal()
    {
        const ulong seed = 42;
        var live = new Game(seed);
        live.Player.Weapon = GearCatalog.Create("woodaxe");
        for (int i = 0; i < 19; i++) live.Player.Skills.AddUse(SkillId.Hafted);
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);

        var (goblin, key) = AdjacentGoblin(live);
        goblin.Hp = 99;
        live.ApplyKey(key);            // the rise that opens the question
        live.ApplyKey('c');
        live.ApplyKey('1');            // the follow-through, chosen
        live.ApplyKey(' ');
        live.ApplyKey(key);
        Assert.True(live.Player.HasPerk(PerkId.FollowThrough));

        var replayed = new Game(seed);
        replayed.Player.Weapon = GearCatalog.Create("woodaxe");
        for (int i = 0; i < 19; i++) replayed.Player.Skills.AddUse(SkillId.Hafted);
        var (goblin2, _) = AdjacentGoblin(replayed);
        goblin2.Hp = 99;
        foreach (char k in journal.ToString()) replayed.ApplyKey(k);

        Assert.Equal(live.Player.Perks, replayed.Player.Perks);
        Assert.Equal(live.Player.Stamina, replayed.Player.Stamina);
        Assert.Equal(live.Turn, replayed.Turn);
        var (a, b) = (live.TakeSnapshot(), replayed.TakeSnapshot());
        Assert.Equal(a.Perks, b.Perks);
        Assert.Equal(a.PendingKnack, b.PendingKnack);
        Assert.Equal(a.Skills, b.Skills);
        Assert.Equal(a.RecentMessages, b.RecentMessages);
    }

    /// <summary>A bearer who meets the grave-iron's asking, so the blade tests read clean.</summary>
    private static Game BladeBearer(ulong seed)
    {
        var game = new Game(seed);
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("grave_iron");
        return game;
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
        int end = text.IndexOf('.', start);
        return int.Parse(text[start..end]);
    }

    /// <summary>Damage of the first telegraphed crushing blow that landed, from the log.</summary>
    private static int FirstBlowDamage(Game game)
    {
        var entry = game.Log.Recent(1000).First(e => e.Text.Contains("crushing blow lands for"));
        string text = entry.Text;
        int start = text.LastIndexOf("for ") + 4;
        int end = text.IndexOf('!', start);
        return int.Parse(text[start..end]);
    }
}
