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
