using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The songhall and the patron's deeds (D-054): the Hall of Legends as a place,
/// and D-025's patronage crossing. Every stead keeps the hall; the skald reads
/// the third ledger back in numbers; pledged coin is spent now, weighed at the
/// crossing at half again its count, and its traces stand in every later hall
/// as text and fact, never as power.
/// </summary>
public class SonghallTests
{
    [Fact]
    public void TheSonghall_StandsInEveryWorld_BesideTheStead()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);

            var hall = a.SonghallSite;
            Assert.Equal(Terrain.SonghallEntrance, a.Overworld[hall.OverworldPos]);
            Assert.True(a.Facts.Exists("site", "songhall"), $"seed {seed}: no songhall fact");

            // The stead's hall, not a den: it stands at the settlement's edge.
            var settlement = a.ShrinePos.Plus(0, -2);
            Assert.True(hall.OverworldPos.Chebyshev(settlement) <= 9,
                $"seed {seed}: songhall {hall.OverworldPos} too far from the stead");

            // The keeper at the door, on ground a bearer can stand on.
            var skald = a.Skald;
            Assert.True(a.Overworld.Walkable(skald.Pos), $"seed {seed}: skald on unwalkable tile");
            Assert.True(skald.Pos.Chebyshev(hall.OverworldPos) <= 1, $"seed {seed}: skald far from the door");
            Assert.True(a.Facts.Exists("person", "npc_skald"));

            // Same seed, same hall, same keeper: the draw is its own stream.
            Assert.Equal(hall.OverworldPos, b.SonghallSite.OverworldPos);
            Assert.Equal((skald.Name, skald.Pos), (b.Skald.Name, b.Skald.Pos));

            // The authored room: door, plinth, long hearth, and the verse-wall's floor.
            Assert.Equal(Terrain.ExitLadder, hall.Map[hall.EntryPos]);
            Assert.Equal(Terrain.Plinth, hall.Map[FindTile(hall.Map, Terrain.Plinth)]);
            Assert.Equal(Terrain.Hearth, hall.Map[FindTile(hall.Map, Terrain.Hearth)]);
            Assert.Equal(Terrain.Floor, hall.Map[WorldGen.SonghallVersePos]);
            Assert.Empty(hall.Spawns);
        }
    }

    [Fact]
    public void TheHall_ReadsBare_BeforeAnySong()
    {
        var game = new Game(42);
        EnterHall(game);

        StepOnto(game, FindTile(game.CurrentSite!.Map, Terrain.Plinth));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("stands bare by the door"));

        StepOnto(game, FindTile(game.CurrentSite!.Map, Terrain.Hearth));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("burns low"));

        StepOnto(game, WorldGen.SonghallVersePos);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("Nothing in them names a walker"));
    }

    [Fact]
    public void TheSkald_ReadsTheThirdLedger_InNumbers()
    {
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.Skald);
        Assert.Contains(game.Topics, t => t.Label == "The hall");
        Assert.Contains(game.Topics, t => t.Label == "Your name" && t.Answer.Contains("No song carries you yet"));
        game.ApplyKey(' ');

        game.Player.Legend = 130;
        NpcTests.BumpNpc(game, game.World.Skald);
        Assert.Contains(game.Topics, t => t.Label == "Your name"
            && t.Answer.Contains("weigh what you have carried at 130")
            && t.Answer.Contains("a name at the hearths")
            && t.Answer.Contains("tips at 225"));
        game.ApplyKey(' ');
    }

    [Fact]
    public void ThePledge_TakesCoinNow_OnceAndOnlyWithCoin()
    {
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.Skald);
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Pledge && o.Label.Contains("Pledge the raised stone (20 coin)"));
        char stone = OfferKeyFor(game, "raised_stone");

        // Broke: the pledge waits, and no coin moves.
        game.Player.Coin = 10;
        game.ApplyKey(stone);
        Assert.Empty(game.Player.PledgedDeeds);
        Assert.Equal(10, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("The hall keeps; it does not lend"));

        // Flush: coin into the chest, the label flips, the Aegis marks the
        // spending exactly once, the pledge is written to the world's facts.
        game.Player.Coin = 30;
        game.ApplyKey(stone);
        Assert.Equal([PatronDeedId.RaisedStone], game.Player.PledgedDeeds);
        Assert.Equal(10, game.Player.Coin);
        Assert.True(game.World.Facts.Exists("pledge", "raised_stone"));
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Pledge && o.Label.Contains("The raised stone (pledged)"));
        Assert.Contains(game.Log.Recent(5), e => e.Text.Contains("Coin into song"));

        // Pledged is pledged: no second charge, no second entry.
        game.ApplyKey(stone);
        Assert.Equal(10, game.Player.Coin);
        Assert.Single(game.Player.PledgedDeeds);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("the chest holds it"));

        // A second deed: taken, paid, and the Aegis stays quiet this time.
        game.Player.Coin = 60;
        game.ApplyKey(OfferKeyFor(game, "endowed_hearth"));
        Assert.Equal(2, game.Player.PledgedDeeds.Count);
        Assert.Equal(0, game.Player.Coin);
        Assert.DoesNotContain(game.Log.Recent(3), e => e.Text.Contains("Coin into song"));
        game.ApplyKey(' ');

        // In the pledge's own world the hall shows the waiting, not the deed.
        EnterHall(game);
        StepOnto(game, FindTile(game.CurrentSite!.Map, Terrain.Plinth));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("your pledge is made"));
        StepOnto(game, FindTile(game.CurrentSite!.Map, Terrain.Hearth));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("waiting for the songs to carry it"));
    }

    [Fact]
    public void TheCrossing_WeighsThePledge_AndTheStone_Stands()
    {
        var game = new Game(42);
        Pledge(game, "raised_stone", 20);
        game.Player.Coin = 0;

        Cross(game);
        // The pledge at half again its coin (30), plus the stead's friend's-welcome purse (5, D-077).
        Assert.Equal(30 + 5, game.Player.Legend);
        Assert.Empty(game.Player.PledgedDeeds);
        Assert.Equal([PatronDeedId.RaisedStone], game.Player.PatronDeeds);
        Assert.Contains(game.Log.Recent(14), e => e.Text.Contains("at half again its coin"));

        // The trace fact travels, and the next hall shows the stone standing.
        Assert.True(game.World.Facts.Exists("patronage", "raised_stone"));
        EnterHall(game);
        StepOnto(game, FindTile(game.CurrentSite!.Map, Terrain.Plinth));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("the name cut into it is yours"));
        StepOnto(game, WorldGen.SonghallVersePos);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("Among them, new-cut"));
    }

    [Fact]
    public void TheHearth_FeedsTheRoad_OneLoaf()
    {
        var game = new Game(42);
        Pledge(game, "endowed_hearth", 60);
        game.Player.Coin = 0;

        Cross(game);
        // Standing 1: one loaf for the songs, one from the hall. Legend is the pledge
        // weighed at half again (90) plus the stead's friend's-welcome purse (5, D-077).
        Assert.Equal(90 + 5, game.Player.Legend);
        Assert.Equal(2, game.Player.Rations);
        Assert.Contains(game.Log.Recent(14), e => e.Text.Contains("bread has been set out"));
    }

    [Fact]
    public void TheHushedName_HushesTheTraces_NeverTheWeighing()
    {
        var game = new Game(42);
        Pledge(game, "raised_stone", 20);
        game.Player.Coin = 0;

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey(HushedKey());
        game.ApplyKey('>');

        // The deed is weighed: the true ledger does not hush. The traces do. Nor does
        // the hushed name silence the stead's friend's-welcome purse (5, D-077), which
        // rides in on top of the pledge weighed at half again (30).
        Assert.Equal(30 + 5, game.Player.Legend);
        Assert.Equal([PatronDeedId.RaisedStone], game.Player.PatronDeeds);
        Assert.False(game.World.Facts.Exists("patronage", "raised_stone"));
        Assert.Equal(0, game.Player.Rations);

        EnterHall(game);
        StepOnto(game, FindTile(game.CurrentSite!.Map, Terrain.Plinth));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("stands bare by the door"));
    }

    [Fact]
    public void TheLadder_Rises_AndTheFullHall_Reads()
    {
        // The catalog is D-025's rising ladder, each rung above the last.
        Assert.Equal([20, 60, 120], PatronCatalog.All.Select(d => d.Price));
        Assert.Equal([30, 90, 180], PatronCatalog.All.Select(d => d.Worth));

        var game = new Game(42);
        game.Player.Coin = 200;
        NpcTests.BumpNpc(game, game.World.Skald);
        game.ApplyKey(OfferKeyFor(game, "raised_stone"));
        game.ApplyKey(OfferKeyFor(game, "endowed_hearth"));
        game.ApplyKey(OfferKeyFor(game, "true_verse"));
        game.ApplyKey(' ');
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(3, game.Player.PledgedDeeds.Count);

        Cross(game);
        Assert.Equal(300 + 5, game.Player.Legend); // three pledges weighed at half again (300) + the friend's welcome (5, D-077)

        EnterHall(game);
        StepOnto(game, FindTile(game.CurrentSite!.Map, Terrain.Hearth));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("burns high and fed"));
        StepOnto(game, WorldGen.SonghallVersePos);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("your own verse, the account as you gave it"));
    }

    [Fact]
    public void TheStead_SpeaksOfTheStone_ItNeverOrdered()
    {
        var game = new Game(42);
        Pledge(game, "raised_stone", 20);
        Cross(game);

        bool heard = false;
        foreach (var near in HouseNeighbors(game))
        {
            // The shuttered window may have opened on the last step (D-117); leave it.
            if (game.InScene) game.ApplyKey('3');
            var from = Directions.All8.Select(d => near.Plus(d.dx, d.dy))
                .FirstOrDefault(p => game.World.Overworld.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
            if (from == default) continue;
            game.Debug_SetPlayerPos(from);
            game.ApplyKey(KeyFor(near.X - from.X, near.Y - from.Y));
            if (game.Log.Recent(4).Any(e => e.Text.Contains("the stone at the songhall door"))) { heard = true; break; }
        }
        Assert.True(heard, "the-stone-at-the-door never fired walking the stead");
    }

    [Fact]
    public void ASession_ReplaysIdentically_PledgeAndAll()
    {
        var a = Play(new Game(7));
        var b = Play(new Game(7));
        // Arrays compare by reference under record equality, so the log window
        // is stripped and checked by value on its own.
        Assert.Equal(a.TakeSnapshot().RecentMessages, b.TakeSnapshot().RecentMessages);
        Assert.Equal(a.TakeSnapshot() with { RecentMessages = null! }, b.TakeSnapshot() with { RecentMessages = null! });

        static Game Play(Game game)
        {
            game.Player.Coin = 25;
            NpcTests.BumpNpc(game, game.World.Skald);
            game.ApplyKey(OfferKeyFor(game, "raised_stone"));
            game.ApplyKey(' ');
            Cross(game);
            EnterHall(game);
            StepOnto(game, FindTile(game.CurrentSite!.Map, Terrain.Plinth));
            return game;
        }
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        _ => 'n',
    };

    private static Pos FindTile(GameMap map, Terrain t)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map[new Pos(x, y)] == t) return new Pos(x, y);
        throw new InvalidOperationException($"no {t} tile in {map.Id}");
    }

    private static void EnterHall(Game game)
    {
        game.Debug_SetPlayerPos(game.World.SonghallSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(SiteKind.Songhall, game.CurrentSite?.Kind);
    }

    private static void StepOnto(Game game, Pos p)
    {
        game.Debug_SetPlayerPos(p.Plus(-1, 0));
        game.ApplyKey('l');
        Assert.Equal(p, game.Player.Pos);
    }

    private static char OfferKeyFor(Game game, string arg)
    {
        for (int i = 0; i < game.Offers.Count; i++)
            if (game.Offers[i].Arg == arg) return (char)('1' + game.Topics.Count + i);
        throw new InvalidOperationException($"no offer for {arg}");
    }

    private static void Pledge(Game game, string deed, int coin)
    {
        game.Player.Coin = coin;
        NpcTests.BumpNpc(game, game.World.Skald);
        game.ApplyKey(OfferKeyFor(game, deed));
        game.ApplyKey(' ');
        Assert.Single(game.Player.PledgedDeeds);
    }

    private static void Cross(Game game)
    {
        int cycle = game.Cycle;
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');
        Assert.Equal(cycle + 1, game.Cycle);
    }

    private static char HushedKey()
    {
        for (int i = 0; i < OathCatalog.All.Count; i++)
            if (OathCatalog.All[i].Id == OathId.HushedName) return (char)('1' + i);
        throw new InvalidOperationException("no hushed name oath");
    }

    private static IEnumerable<Pos> HouseNeighbors(Game game)
    {
        var map = game.World.Overworld;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var h = new Pos(x, y);
                if (map[h] != Terrain.House) continue;
                foreach (var (dx, dy) in Directions.All8)
                {
                    var p = h.Plus(dx, dy);
                    if (map.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p)) yield return p;
                }
            }
    }
}
