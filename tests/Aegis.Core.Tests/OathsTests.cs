using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The terms of the crossing (D-047, the covenants of D-011): oaths sworn at an
/// open waygate are generation inputs on the next world, lapse at its far gate,
/// and buy Legend and a louder echo, never raw power. The choosing keys are
/// journaled, so an oath-bound world replays like any other.
/// </summary>
public class OathsTests
{
    [Fact]
    public void TheGate_PutsTheTerms_AndASecondKey_CrossesPlain()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);

        game.ApplyKey('>');
        Assert.True(game.InCrossingMenu);
        Assert.Equal(1, game.Cycle);
        Assert.True(game.TakeSnapshot().InCrossingMenu);

        var screen = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("The terms of the crossing", screen);
        Assert.Contains("1) - the crowded dark", screen);
        Assert.Contains("4) - the slow mending", screen);
        Assert.Contains("No terms taken up", screen);

        game.ApplyKey('>');
        Assert.False(game.InCrossingMenu);
        Assert.Equal(2, game.Cycle);
        Assert.Empty(game.World.Oaths);
        var snap = game.TakeSnapshot();
        Assert.Equal("", snap.Oaths);
        Assert.Equal(0, snap.Burden);
    }

    [Fact]
    public void APlainCrossing_GeneratesTheWorld_ItAlwaysDid()
    {
        var game = new Game(42);
        string? prevStory = game.World.Facts.OfType("story").FirstOrDefault()?.Subject;
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');

        var expected = WorldGen.Generate(SeedTree.Derive(42, "cycle", 2), tier: 2, prevStory: prevStory, takenNames: game.Player.WorldsWalked);
        Assert.Equal(expected.Name, game.World.Name);
        Assert.Equal(expected.Overworld.ContentHash(), game.World.Overworld.ContentHash());
        Assert.Equal(expected.Camp.ContentHash(), game.World.Camp.ContentHash());
        Assert.Equal(expected.CampSite.Spawns.Count, game.World.CampSite.Spawns.Count);
    }

    [Fact]
    public void Digits_SwearAndUnswear_AndSteppingBack_LetsTheChoiceGo()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);

        game.ApplyKey('>');
        game.ApplyKey('1');
        Assert.Contains(OathId.CrowdedDark, game.ChosenOaths);
        game.ApplyKey('1');
        Assert.Empty(game.ChosenOaths);

        game.ApplyKey('1');
        game.ApplyKey('3');
        var screen = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("1) x the crowded dark", screen);
        Assert.Contains("3) x the spent edge", screen);
        Assert.Contains("The burden you take up: 2", screen);

        game.ApplyKey(' ');
        Assert.False(game.InCrossingMenu);
        Assert.Equal(1, game.Cycle); // stepped back, not crossed

        game.ApplyKey('>');
        Assert.Empty(game.ChosenOaths); // the selection was let go
    }

    [Fact]
    public void TheCrowdedDark_PutsOneMore_InEveryDen()
    {
        var game = CrossUnder(42, '1');

        Assert.Equal(new[] { OathId.CrowdedDark }, game.World.Oaths);
        var snap = game.TakeSnapshot();
        Assert.Equal("crowded_dark", snap.Oaths);
        Assert.Equal(1, snap.Burden);

        // Tier 2 asks 4 goblins and 2 wights; the oath adds one more to each den.
        Assert.Equal(5, game.World.CampSite.Spawns.Count);
        Assert.Equal(3, game.World.BarrowSite!.Spawns.Count);
        Assert.Contains(game.Log.Recent(12), e => e.Text.Contains("The terms you took up hold here"));
    }

    [Fact]
    public void TheHungryRoad_DoublesBread()
    {
        var game = CrossUnder(42, '2');

        bool blightStands = game.World.Facts.Exists("story", CreepingBlightTemplate.Id)
            && !game.World.Facts.Exists("story_complete", CreepingBlightTemplate.Id);
        Assert.Equal(blightStands ? 12 : 8, game.RationPrice);
    }

    [Fact]
    public void TheSpentEdge_WearsIron_TwiceAsFast()
    {
        var game = CrossUnder(42, '3');
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("woodaxe");

        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 99;
        game.ApplyKey(key);
        Assert.Equal(2, game.Player.Weapon.Wear);
    }

    [Fact]
    public void TheSlowMending_HoldsTheWound_TwiceAsLong()
    {
        var game = CrossUnder(42, '4');
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Equal(160, game.Player.WoundedTurns);

        var plain = CrossUnder(43, ' ');
        plain.Debug_HurtPlayer(999);
        plain.Debug_ForceDeathCheck();
        Assert.Equal(80, plain.Player.WoundedTurns);
    }

    [Fact]
    public void TheBurden_IsHonoredInLegend_AtTheFarGate()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('1');
        game.ApplyKey('2');
        game.ApplyKey('>');
        Assert.Equal(2, game.Burden);

        int legendBefore = game.Player.Legend;
        game.Player.Coin = 7;
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');

        // 7 coin converted plus 10 Legend per point of burden carried through.
        Assert.Equal(legendBefore + 7 + 20, game.Player.Legend);
        Assert.Contains(game.Log.Recent(16), e => e.Text.Contains("Legend grows by 20 more"));
        Assert.Equal(0, game.Burden); // the terms lapsed at the far gate
    }

    [Fact]
    public void TheEcho_SingsTheHarderWalking()
    {
        // The echo speaks of the world left behind, so the oath must be on the
        // world being LEFT: swear into world 2, then cross out of it plainly.
        var game = CrossUnder(42, '1');
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');
        Assert.Equal(3, game.Cycle);
        Assert.Contains("under oath", game.World.Facts.OfType("echo").First().Detail);

        var plain = CrossUnder(42, ' ');
        Assert.DoesNotContain("under oath", plain.World.Facts.OfType("echo").First().Detail);
    }

    [Fact]
    public void TheTermsMenu_SpeaksPerAnswer_AfterResolution()
    {
        string OpeningLine(Resolution resolution)
        {
            var game = new Game(42);
            game.Player.Resolution = resolution;
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.ApplyKey('>');
            return game.Log.Recent(1).Single().Text;
        }

        Assert.Contains("keeper", OpeningLine(Resolution.Kept));
        Assert.Contains("no commission behind them now", OpeningLine(Resolution.Refused));
        Assert.Contains("Take up any you will bear, or none", OpeningLine(Resolution.None));
    }

    [Fact]
    public void TheJournal_ReplaysAnOathboundCrossing_Identically()
    {
        Game Play(out List<char> journal)
        {
            var game = new Game(42);
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            var keys = new List<char>();
            game.KeyApplied += keys.Add;
            game.ApplyKey('>');
            game.ApplyKey('1');
            game.ApplyKey('4');
            game.ApplyKey('>');
            game.ApplyKey('.');
            journal = keys;
            return game;
        }

        var live = Play(out var journal);
        Assert.Equal(new[] { '>', '1', '4', '>', '.' }, journal);

        // Same surgery, then the recorded keys: the crossing must land bit-identically.
        var replayed = new Game(42);
        replayed.Debug_ClearCamp();
        replayed.Debug_SetPlayerPos(replayed.World.GatePos);
        foreach (char key in journal) replayed.ApplyKey(key);

        var a = live.TakeSnapshot();
        var b = replayed.TakeSnapshot();
        Assert.Equal(a.Oaths, b.Oaths);
        Assert.Equal(a.Burden, b.Burden);
        Assert.Equal(a.WorldName, b.WorldName);
        Assert.Equal(a.Legend, b.Legend);
        Assert.Equal(a.Turn, b.Turn);
        Assert.Equal(a.MonstersAlive, b.MonstersAlive);
        Assert.Equal((a.X, a.Y), (b.X, b.Y));
    }

    [Fact]
    public void TheNewTerms_StandInTheMenu_AndTheOldBlood_WeighsDouble()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        var screen = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("5) - the old blood: every blow lands 1 deeper", screen);
        Assert.Contains("6) - the lean dark: essence comes up halved", screen);
        Assert.Contains("7) - the hushed name: the songs fall silent", screen);
        game.ApplyKey(' ');

        var sworn = CrossUnder(42, '5');
        var snap = sworn.TakeSnapshot();
        Assert.Equal("old_blood", snap.Oaths);
        Assert.Equal(2, snap.Burden);
    }

    [Fact]
    public void TheOldBlood_LandsEveryBlow_APointDeeper()
    {
        // Two identical worlds, one sworn: the old blood is not a generation
        // input, so the same goblin lands the same blow, one point deeper.
        var sworn = CrossUnder(42, '5');
        var plain = CrossUnder(42, ' ');

        foreach (var game in new[] { sworn, plain })
        {
            var (goblin, _) = AdjacentGoblin(game);
            foreach (var other in game.Monsters.Where(m => m.SiteId == "goblin-camp" && m != goblin))
                other.Hp = 0;
        }

        int max = plain.Player.MaxHp;
        for (int i = 0; i < 40 && plain.Player.Hp == max; i++)
        {
            sworn.ApplyKey('.');
            plain.ApplyKey('.');
        }
        Assert.True(plain.Player.Hp < max, "the goblin never landed a blow");
        Assert.Equal((max - plain.Player.Hp) + 1, max - sworn.Player.Hp);
    }

    [Fact]
    public void TheLeanDark_HalvesTheEssence_AndSparesTheCoin()
    {
        var game = CrossUnder(42, '6');
        var (goblin, key) = AdjacentGoblin(game);
        goblin.Hp = 1;
        int essenceBefore = game.Player.Essence;
        game.ApplyKey(key);

        // A goblin's 5 essence comes up as 2: halved, rounded against the bearer.
        Assert.Equal(essenceBefore + 2, game.Player.Essence);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("2 essence"));
    }

    [Fact]
    public void TheHushedName_SilencesTheSongs_AndEveryFavor()
    {
        Game CrossAt(int legend, char digit)
        {
            var game = new Game(42);
            game.Player.Legend = legend;
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.ApplyKey('>');
            if (digit != ' ') game.ApplyKey(digit);
            game.ApplyKey('>');
            return game;
        }

        // Legend 500 is standing 4: welcome, hearth-price, and menders' honor
        // would all answer. Under the hushed name, none of them do.
        var hushed = CrossAt(500, '7');
        Assert.Empty(hushed.World.Facts.OfType("echo"));
        Assert.Equal(0, hushed.Player.Rations);
        Assert.Equal(3, hushed.UnbindingsLeft);
        Assert.Contains(hushed.Log.Recent(20), e => e.Text.Contains("only a stranger off the road"));

        var plain = CrossAt(500, ' ');
        Assert.Single(plain.World.Facts.OfType("echo"));
        Assert.Equal(3, plain.Player.Rations);
        Assert.Equal(4, plain.UnbindingsLeft);
        Assert.Contains(plain.Log.Recent(20), e => e.Text.Contains("they already sing"));

        // The hearth-price discount is withheld too, whatever the base was.
        Assert.Equal(plain.RationPrice + 1, hushed.RationPrice);
    }

    [Fact]
    public void TheKnownFace_GreetsTheStoried_OnceAndNeverHushed()
    {
        // Standing 2 in world 1: the first villager spoken to knows the bearer.
        var game = new Game(42);
        game.Player.Legend = 100;
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_steadholder"));
        Assert.Contains(game.Log.Recent(10), e => e.Text.Contains("watched the road"));

        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_steadholder"));
        Assert.Equal(1, game.Log.Recent(50).Count(e => e.Text.Contains("watched the road")));
        game.ApplyKey(' ');

        // Under the hushed name the stead was never told, so no one knows.
        var hushed = new Game(42);
        hushed.Player.Legend = 100;
        hushed.Debug_ClearCamp();
        hushed.Debug_SetPlayerPos(hushed.World.GatePos);
        hushed.ApplyKey('>');
        hushed.ApplyKey('7');
        hushed.ApplyKey('>');
        NpcTests.BumpNpc(hushed, hushed.World.Npcs.First(n => n.Id == "npc_steadholder"));
        Assert.DoesNotContain(hushed.Log.Recent(10), e => e.Text.Contains("watched the road"));
    }

    [Fact]
    public void OldSongs_AnswerKnowsTheWalker_ByStanding()
    {
        string Answer(Game game)
        {
            NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_steadholder"));
            string answer = game.Topics.First(t => t.Label == "Old songs").Answer;
            game.ApplyKey(' ');
            return answer;
        }

        var game = CrossUnder(42, ' ');
        Assert.DoesNotContain("your height", Answer(game));

        game.Player.Legend = 30; // standing 1: the singer half-suspects
        Assert.Contains("about your height", Answer(game));

        game.Player.Legend = 250; // standing 3: no one is pretending
        Assert.Contains("looking at the door you came in by", Answer(game));
    }

    [Fact]
    public void TheHardSeason_IsSpoken_UnderTermsOnly()
    {
        bool Spoken(Game game, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                game.ApplyKey(i % 2 == 0 ? 'k' : 'j');
                if (game.Log.Recent(4).Any(e => e.Text.Contains("Lean, and long"))) return true;
            }
            return false;
        }

        var sworn = CrossUnder(42, '1');
        Assert.True(Spoken(sworn, 30), "the hard season was never spoken in an oath-bound world");
        Assert.False(Spoken(sworn, 30)); // world-scoped, once

        var plain = CrossUnder(42, ' ');
        Assert.False(Spoken(plain, 30));
    }

    [Fact]
    public void TheCrowdedDark_IsFeltInTheRaidsTalk()
    {
        var sworn = CrossUnder(42, '1');
        NpcTests.BumpNpc(sworn, sworn.World.Npcs.First(n => n.Id == "npc_steadholder"));
        Assert.Contains("more of them this year", sworn.Topics.First(t => t.Label == "The goblin raids").Answer);
        sworn.ApplyKey(' ');

        var plain = CrossUnder(42, ' ');
        NpcTests.BumpNpc(plain, plain.World.Npcs.First(n => n.Id == "npc_steadholder"));
        Assert.DoesNotContain("more of them this year", plain.Topics.First(t => t.Label == "The goblin raids").Answer);
    }

    /// <summary>Clears the camp and crosses under one oath's digit (' ' swears nothing).</summary>
    private static Game CrossUnder(ulong master, char digit)
    {
        var game = new Game(master);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        if (digit != ' ') game.ApplyKey(digit);
        game.ApplyKey('>');
        Assert.Equal(2, game.Cycle);
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
}
