using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Legend standing (D-048, the boon rungs of D-011): Legend stays the only
/// state, standing is derived from it on a square curve, titles speak at the
/// crossing where Legend is minted, and the boons are hospitality (the welcome,
/// the hearth-price, the menders' honor), never combat power.
/// </summary>
public class LegendTests
{
    [Fact]
    public void Standing_Derives_OnTheSquareCurve()
    {
        Assert.Equal(0, LegendStanding.StandingFor(0));
        Assert.Equal(0, LegendStanding.StandingFor(24));
        Assert.Equal(1, LegendStanding.StandingFor(25));
        Assert.Equal(1, LegendStanding.StandingFor(99));
        Assert.Equal(2, LegendStanding.StandingFor(100));
        Assert.Equal(2, LegendStanding.StandingFor(224));
        Assert.Equal(3, LegendStanding.StandingFor(225));
        Assert.Equal(3, LegendStanding.StandingFor(399));
        Assert.Equal(4, LegendStanding.StandingFor(400));
        Assert.Equal(4, LegendStanding.StandingFor(624));
        Assert.Equal(5, LegendStanding.StandingFor(625));
        Assert.Equal(5, LegendStanding.StandingFor(100_000)); // the cap holds

        Assert.Equal("", LegendStanding.TitleOf(0));
        Assert.Equal("the songs' own", LegendStanding.TitleOf(LegendStanding.MaxStanding));
    }

    [Fact]
    public void TheFirstWorld_OwesNothing()
    {
        // Legend is only minted at crossings, so world 1 is always unstoried:
        // full price, three unbindings, no bread at the shrine (D-047's clean
        // first world, from the other side).
        var game = new Game(42);
        Assert.Equal(0, game.Standing);
        Assert.Equal(4, game.RationPrice);
        Assert.Equal(Game.UnbindingsPerWorld, game.UnbindingsLeft);
        Assert.Equal(0, game.Player.Rations);
    }

    [Fact]
    public void TheWelcome_SetsOutBread_AtArrival()
    {
        var game = new Game(42);
        game.Player.Coin = 30; // converts to 30 Legend: standing 1
        Cross(game);

        Assert.Equal(1, game.Standing);
        Assert.Equal(1, game.Player.Rations);
        Assert.Contains(game.Log.Recent(15), e => e.Text.Contains("bread has been set out against your coming"));
    }

    [Fact]
    public void TheWelcome_ScalesWithStanding_AndNeverPastTheCap()
    {
        // Standing 3 sets out three loaves; the welcome never grows past three
        // and never past what a person can carry.
        var storied = new Game(42);
        storied.Player.Legend = 500; // standing 4; the welcome still caps at 3
        Cross(storied);
        Assert.Equal(3, storied.Player.Rations);

        var laden = new Game(42);
        laden.Player.Legend = 500;
        laden.Player.Rations = Game.RationCap - 1;
        Cross(laden);
        Assert.Equal(Game.RationCap, laden.Player.Rations);
    }

    [Fact]
    public void TheHearthPrice_TakesACoinOff_ForTheStoried()
    {
        var game = new Game(42);
        Assert.Equal(4, game.RationPrice);
        game.Player.Legend = 25;
        Assert.Equal(4, game.RationPrice); // standing 1 is a name, not a discount
        game.Player.Legend = 100;
        Assert.Equal(3, game.RationPrice); // standing 2 eats a little cheaper
    }

    [Fact]
    public void TheHearthPrice_Composes_WithTheHungryRoad()
    {
        // The coin comes off before the oath doubles: (base - 1) * 2.
        var game = new Game(42);
        game.Player.Legend = 100;
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('2'); // the hungry road
        game.ApplyKey('>');

        bool blightStands = game.World.Facts.Exists("story", CreepingBlightTemplate.Id)
            && !game.World.Facts.Exists("story_complete", CreepingBlightTemplate.Id);
        Assert.Equal(blightStands ? 10 : 6, game.RationPrice);
    }

    [Fact]
    public void TheMendersHonor_AddsAFourthUnbinding()
    {
        var storied = new Game(42);
        storied.Player.Legend = 400; // standing 4
        Cross(storied);
        Assert.Equal(Game.UnbindingsPerWorld + 1, storied.UnbindingsLeft);

        var plain = new Game(42);
        Cross(plain);
        Assert.Equal(Game.UnbindingsPerWorld, plain.UnbindingsLeft);
    }

    [Fact]
    public void TheTitle_RisesAtTheCrossing_AndTheAegisSpeaksOnce()
    {
        var game = new Game(42);
        game.Player.Coin = 30;
        Cross(game);

        var firstRise = game.Log.Recent(20).Select(e => e.Text).ToList();
        Assert.Contains(firstRise, t => t.Contains("The weighing tips") && t.Contains("a name in one song"));
        Assert.Contains(firstRise, t => t.Contains("A third ledger"));

        // A second rise speaks the new title, but the aside is spent.
        game.Player.Coin = 100; // 30 + 100 = 130: standing 2
        Cross(game);
        var secondRise = game.Log.Recent(20).Select(e => e.Text).ToList();
        Assert.Contains(secondRise, t => t.Contains("The weighing tips") && t.Contains("a name at the hearths"));
        Assert.DoesNotContain(secondRise, t => t.Contains("A third ledger"));
    }

    [Fact]
    public void TheSheet_AndTheSnapshot_CarryTheTitle()
    {
        var game = new Game(42);
        var bare = game.TakeSnapshot();
        Assert.Equal(0, bare.Standing);
        Assert.Equal("", bare.Title);

        game.Player.Legend = 100;
        var snap = game.TakeSnapshot();
        Assert.Equal(2, snap.Standing);
        Assert.Equal("a name at the hearths", snap.Title);

        game.ApplyKey('c');
        var screen = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("Legend  100   a name at the hearths", screen);
    }

    [Fact]
    public void TheJournal_ReplaysAWelcomeCrossing_Identically()
    {
        Game Play(out List<char> journal)
        {
            var game = new Game(42);
            game.Player.Coin = 30;
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            var keys = new List<char>();
            game.KeyApplied += keys.Add;
            game.ApplyKey('>');
            game.ApplyKey('>');
            game.ApplyKey('.');
            journal = keys;
            return game;
        }

        var live = Play(out var journal);

        var replayed = new Game(42);
        replayed.Player.Coin = 30;
        replayed.Debug_ClearCamp();
        replayed.Debug_SetPlayerPos(replayed.World.GatePos);
        foreach (char key in journal) replayed.ApplyKey(key);

        var a = live.TakeSnapshot();
        var b = replayed.TakeSnapshot();
        Assert.Equal(a.Legend, b.Legend);
        Assert.Equal(a.Standing, b.Standing);
        Assert.Equal(a.Title, b.Title);
        Assert.Equal(a.Rations, b.Rations);
        Assert.Equal(a.RationPrice, b.RationPrice);
        Assert.Equal(a.Turn, b.Turn);
        Assert.Equal((a.X, a.Y), (b.X, b.Y));
    }

    /// <summary>Clears the camp and crosses plainly at the waygate.</summary>
    private static void Cross(Game game)
    {
        int cycle = game.Cycle;
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');
        Assert.Equal(cycle + 1, game.Cycle);
    }
}
