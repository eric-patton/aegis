using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The wolf-winter (D-149, B4's frontier-news deferral): every world's hard
/// winter reaches the tops one tick behind the valley, sits there three, and
/// its word walks to the town on the drovers' clock (D-143) two ticks later,
/// pricing hides while it stands; the lifting's word takes the same road home.
/// The tests hold the whole calendar choreography, the winter's teeth on the
/// pack, and the counter's scarcity coin engaging and releasing.
/// </summary>
public class FellWinterTests
{
    private static void Wait(Game game, int turns)
    {
        for (int i = 0; i < turns; i++) game.ApplyKey('.');
    }

    /// <summary>Waits whole coarse ticks; the calendar acts only on the tick.</summary>
    private static void WaitTicks(Game game, int ticks) => Wait(game, SteadRaids.TickTurns * ticks);

    [Fact]
    public void TheWolfWinter_FollowsTheValleys_AndItsWordWalksBothWays()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_HoldTheDeck();

        // Walk the calendar to the valley's hard winter (due tick 3-5).
        int guard = 0;
        while (!game.World.Facts.Exists("event", "hard_winter") && guard++ < 8) WaitTicks(game, 1);
        Assert.True(game.World.Facts.Exists("event", "hard_winter"));

        // The valley's landing puts the tops' turn on the calendar, announced.
        Assert.Contains(game.Upcoming, f => f.Key == "wolf_winter");
        Assert.False(game.FellWinterStands);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("whitening already"));

        // One tick behind the valley, the wolf-winter sits on the tops.
        WaitTicks(game, 1);
        Assert.True(game.FellWinterStands);
        Assert.True(game.World.Facts.Exists("event", "wolf_winter"));
        Assert.Contains(game.Upcoming, f => f.Key == "wolf_word");
        Assert.False(game.WolfWordStands); // news is freight, not telepathy

        // The waykeeper prices the season at the climb's own door.
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        var keeper = game.World.Npcs.First(n => n.Kind == NpcKind.Waykeeper);
        var beside = Directions.All8
            .Select(d => keeper.Pos.Plus(d.dx, d.dy))
            .First(p => game.World.Road.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(keeper.Pos.X - beside.X, keeper.Pos.Y - beside.Y));
        Assert.True(game.InTalkMenu);
        var fells = game.Topics.First(t => t.Label == "The fells");
        Assert.Contains("wolf-winter", fells.Answer);
        game.ApplyKey(' ');

        // The climb narrates the season it climbs into.
        game.Debug_SetPlayerPos(game.World.FellMouthPos);
        game.ApplyKey('>');
        Assert.Equal(Area.Fells, game.Area);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("up into the wolf-winter"));
        game.Debug_SetPlayerPos(game.World.FellHomePos);
        game.ApplyKey('>');
        Assert.Equal(Area.Road, game.Area);

        // Two ticks on, the word lands in town just as the tops go quiet:
        // news trails the season, and scarcity's chalk pays a coin over.
        WaitTicks(game, 2);
        Assert.True(game.WolfWordStands);
        Assert.False(game.FellWinterStands);
        Assert.True(game.World.Facts.Exists("news", "wolf_winter"));
        game.Player.Hide = 2;
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        NewsTests.BumpTowner(game, "npc_hidemonger");
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Hide && o.Label.Contains("wolf-winter"));
        int coin = game.Player.Coin;
        game.ApplyKey(NewsTests.OfferKey(game, TradeGood.Hide));
        Assert.Equal(coin + 2 * (TownMarket.HidePrice + FellWinter.HideBonus), game.Player.Coin);
        game.ApplyKey(' ');

        // The lifting's word takes the same road: two ticks later the
        // ordinary chalk is back on the board.
        WaitTicks(game, 2);
        Assert.False(game.WolfWordStands);
        game.Player.Hide = 2;
        NewsTests.BumpTowner(game, "npc_hidemonger");
        coin = game.Player.Coin;
        game.ApplyKey(NewsTests.OfferKey(game, TradeGood.Hide));
        Assert.Equal(coin + 2 * TownMarket.HidePrice, game.Player.Coin);
    }

    [Fact]
    public void TheHungryPack_BitesExactlyOneDeeper()
    {
        // Twin games, identical keys, identical dice: the only difference is
        // the pinned season, so the damage gap IS the fang, hit for hit.
        var (baseTotal, baseHits) = BittenBy(winter: false);
        var (coldTotal, coldHits) = BittenBy(winter: true);
        Assert.True(baseHits > 0);
        Assert.Equal(baseHits, coldHits);
        Assert.Equal(baseTotal + baseHits * FellWinter.Fang, coldTotal);
    }

    private static (int Total, int Hits) BittenBy(bool winter)
    {
        var game = new Game(42);
        if (winter) game.Debug_SetFellWinter(99);
        FrontierTests.ClimbFells(game);
        var combe = game.World.FellWildsSite;
        game.Debug_SetPlayerPos(combe.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);

        // One wolf kept, pinned at the throat; the rest of the pack put down
        // so the sample is the plain bite, no pack bonus, no pounce.
        var wolves = game.Monsters.Where(m => m.Kind == MonsterKind.Wolf && m.SiteId == combe.Id).ToList();
        foreach (var w in wolves.Skip(1)) w.Hp = 0;
        wolves[0].Pos = new Pos(combe.EntryPos.X + 1, combe.EntryPos.Y);

        int total = 0, hits = 0;
        for (int i = 0; i < 30; i++)
        {
            game.Player.Hp = 30;
            game.ApplyKey('.');
            int taken = 30 - game.Player.Hp;
            if (taken > 0) { total += taken; hits++; }
        }
        return (total, hits);
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
        _ => 'n',
    };
}
