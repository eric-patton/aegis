using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Word that travels (D-143, plan 2026-07 B3): news moves between the named
/// countries on the drovers' clock, through the calendar (D-132), never
/// instantly. Eastbound: an unwelcome name at the stead rides to the town two
/// ticks later and kills the haggle coin while the home shame stands, with
/// restitution before the due tick as the designed exit. Westbound: a name at
/// the town's barred rung is carried to the valley's doors as talk alone (the
/// stead's book moves only for what it sees). The tests hold both roads, both
/// exits, the tooth, and the tooth's release.
/// </summary>
public class NewsTests
{
    internal static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    internal static void EnterTown(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
    }

    internal static Npc BumpTowner(Game game, string id)
    {
        var npc = game.World.Npcs.First(n => n.Id == id);
        var town = game.CurrentSite!.Map;
        var beside = Directions.All8
            .Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => town.Walkable(p) && !game.World.Npcs.Any(n => n.SiteId == "town" && n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
        return npc;
    }

    /// <summary>A quiet world for choreographed ticks: no raids, no season deals, just the calendar under test.</summary>
    private static Game QuietGame(ulong seed = 42)
    {
        var game = new Game(seed);
        game.Debug_ClearCamp();
        game.Debug_HoldTheDeck();
        return game;
    }

    [Fact]
    public void TheUnwelcomeName_PutsWordOnTheRoad()
    {
        var game = QuietGame();
        game.Debug_RaiseShame(2);
        Assert.Contains(game.Upcoming, f => f.Key == "word_east");
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("drovers east will carry your name"));
        Assert.False(game.WordEast); // scheduled is not landed: news takes the road at freight speed
    }

    [Fact]
    public void TheWord_WalksEast_AndKillsTheChalkTrust_UntilHomeStandsEven()
    {
        var game = QuietGame();
        for (int i = 0; i < 8; i++) game.Player.Skills.AddUse(SkillId.Commerce);
        game.Debug_RaiseShame(2);
        Wait(game, SteadRaids.TickTurns * 2);
        Assert.True(game.WordEast);
        Assert.True(game.World.Facts.Exists("news", "word_east"));

        // The tooth: a road-spoken name is paid chalk alone while the home
        // shame stands beside the word.
        int level = game.Player.Skills.Level(SkillId.Commerce);
        Assert.True(level > 0);
        game.Player.Hide = 2;
        EnterTown(game);
        int coin = game.Player.Coin;
        BumpTowner(game, "npc_hidemonger");
        game.ApplyKey(OfferKey(game, TradeGood.Hide));
        Assert.Equal(coin + 2 * TownMarket.HidePrice, game.Player.Coin);
        game.ApplyKey(' ');

        // The release: the road carries the mending too. Home squared, the
        // word stays written but its teeth are done.
        game.Debug_LowerShame(2);
        Assert.True(game.WordEast);
        game.Player.Hide = 2;
        coin = game.Player.Coin;
        int haggle = game.Player.Skills.Level(SkillId.Commerce);
        BumpTowner(game, "npc_hidemonger");
        game.ApplyKey(OfferKey(game, TradeGood.Hide));
        Assert.Equal(coin + 2 * TownMarket.HidePrice + haggle, game.Player.Coin);
    }

    [Fact]
    public void TheBookMadeEven_BeatsTheDrovers()
    {
        var game = QuietGame();
        game.Debug_RaiseShame(2);
        game.Debug_LowerShame(1); // one door made right: back under the unwelcome rung
        Wait(game, SteadRaids.TickTurns);

        Assert.False(game.WordEast);
        Assert.DoesNotContain(game.Upcoming, f => f.Key == "word_east");
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the word died at home"));
    }

    [Fact]
    public void TheWardenNames_TheRoadsWord()
    {
        var game = QuietGame();
        game.Debug_RaiseShame(2);
        Wait(game, SteadRaids.TickTurns * 2);
        EnterTown(game);
        BumpTowner(game, "npc_mootwarden");
        var moot = game.Topics.First(t => t.Label == "The moot").Answer;
        Assert.Contains(game.World.ValleyRegion.Name, moot);
        Assert.Contains("another country's grievance", moot);
    }

    [Fact]
    public void TheBarredName_IsFreight_AndTheDoorsHearIt()
    {
        var game = QuietGame();
        game.Debug_RaiseTownBook(TownLaw.BarredRung);
        Assert.Contains(game.Upcoming, f => f.Key == "word_west");
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("carts west will carry yours"));

        Wait(game, SteadRaids.TickTurns * 2);
        Assert.True(game.WordWest);
        Assert.True(game.World.Facts.Exists("news", "word_west"));

        // Talk only, and said plainly at the doors: the stead's own book
        // never moves for hearsay (D-142's ledgers stay separate).
        Assert.Equal(0, game.Shame);
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        NpcTests.BumpNpc(game, villager);
        Assert.Contains("But it is being said", game.Topics.First(t => t.Label == "The stead").Answer);
    }

    [Fact]
    public void ThePleas_BeatTheCarts()
    {
        var game = QuietGame();
        game.Player.Coin = 2 * TownLaw.FineCoin;
        game.Debug_RaiseTownBook(TownLaw.BarredRung);
        EnterTown(game);
        BumpTowner(game, "npc_mootwarden");
        game.ApplyKey(OfferKey(game, TradeGood.Plea)); // one mark answered: back under the rung
        game.ApplyKey(' ');
        Wait(game, SteadRaids.TickTurns);

        Assert.False(game.WordWest);
        Assert.DoesNotContain(game.Upcoming, f => f.Key == "word_west");
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("nothing to say of you"));
    }

    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.Apply(Command.Wait);
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k', (0, 1) => 'j', (-1, 0) => 'h', (1, 0) => 'l',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
