using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The town's law (D-142, plan 2026-07 step 10's second half): the moot's
/// book surfaced as machinery. The light hand works inside the wall and a
/// caught one goes into the warden's book, not the stead's shame; a standing
/// mark kills the haggle coin, two shut the counters (never the moot); the
/// plea answers marks for fines and seeds Persuasion, the 13th skill. The
/// tests hold the lift's two outcomes, both teeth, the plea's arithmetic,
/// the pleader's shaving, and the ledgers' separation.
/// </summary>
public class TownLawTests
{
    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    private static void EnterTown(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
    }

    private static Npc BumpTowner(Game game, string id)
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

    /// <summary>Stands the bearer beside a towner and tries the pocket through the real command.</summary>
    private static Npc Lift(Game game, string id)
    {
        var npc = game.World.Npcs.First(n => n.Id == id);
        var town = game.CurrentSite!.Map;
        var beside = Directions.All8
            .Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => town.Walkable(p) && !game.World.Npcs.Any(n => n.SiteId == "town" && n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.Apply(Command.Lift);
        return npc;
    }

    [Fact]
    public void TheWall_HasPockets_AndABook()
    {
        // The dice fall per seed (the SleightTests discipline): walk seeds
        // until both outcomes have shown themselves, and hold each one's
        // whole ledger. The clean lift pays the town's own secret; the
        // caught one goes into the WARDEN'S book, and the stead's shame,
        // which has no eyes out east, never moves for either.
        bool sawClean = false, sawCaught = false;
        for (ulong seed = 1; seed <= 40 && !(sawClean && sawCaught); seed++)
        {
            var game = new Game(seed);
            EnterTown(game);
            int coin = game.Player.Coin;
            Lift(game, "npc_provisioner");

            Assert.Equal(0, game.Shame);
            if (game.TownBook == 0)
            {
                sawClean = true;
                Assert.True(game.Player.Coin > coin);
                Assert.Equal(1, game.Player.Skills.Uses(SkillId.Sleight));
                Assert.True(game.World.Facts.Exists("secret", "lifted_purse_town"));
                Assert.False(game.World.Facts.Exists("secret", "lifted_purse"));
            }
            else
            {
                sawCaught = true;
                Assert.Equal(1, game.TownBook);
                Assert.Equal(coin, game.Player.Coin);
                Assert.Equal(0, game.Player.Skills.Uses(SkillId.Sleight));
                Assert.True(game.World.Facts.Exists("law", "booked"));
                Assert.False(game.World.Facts.Exists("shame", "confronted"));
            }
        }
        Assert.True(sawClean && sawCaught, "forty seeds showed only one of the lift's two outcomes");
    }

    [Fact]
    public void TheRepaidHand_DoesNotEraseTheBook()
    {
        // Find a caught hand, then make it right in the hand: the wronged
        // towner is even, and the mark at the moot-stone stands untouched.
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var game = new Game(seed);
            EnterTown(game);
            var mark = Lift(game, "npc_provisioner");
            if (game.TownBook == 0) continue;

            game.Player.Coin = SteadShame.RepayCoin;
            game.Apply(Command.Lift); // making right outranks more wrong (D-086)
            Assert.Contains(mark.Id, game.World.RepaidLifts);
            Assert.Equal(0, game.Player.Coin);
            Assert.Equal(1, game.TownBook);
            Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("waits for the moot-stone"));
            return;
        }
        Assert.Fail("forty seeds never showed a caught hand");
    }

    [Fact]
    public void TheBookedHand_LosesTheHaggle()
    {
        var game = new Game(42);
        for (int i = 0; i < 8; i++) game.Player.Skills.AddUse(SkillId.Commerce);
        game.Player.Hide = 2;
        EnterTown(game);
        game.Debug_RaiseTownBook(1);

        // The lot still sells (one mark distrusts, it does not bar), but the
        // chalked price is all of it: no counter trusts a booked hand's scales.
        int coin = game.Player.Coin;
        BumpTowner(game, "npc_hidemonger");
        game.ApplyKey(OfferKey(game, TradeGood.Hide));
        Assert.Equal(coin + 2 * TownMarket.HidePrice, game.Player.Coin);
    }

    [Fact]
    public void TheBarredRung_ShutsTheCounters_ButNeverTheMoot()
    {
        var game = new Game(42);
        var weapon = GearCatalog.Create("woodaxe");
        game.Player.Weapon = weapon;
        weapon.Wear = 10;
        game.Player.Hide = 2;
        game.Player.Coin = 20;
        EnterTown(game);
        game.Debug_RaiseTownBook(TownLaw.BarredRung);

        // The stall keeps its hands flat: the lot stays in the pack.
        BumpTowner(game, "npc_hidemonger");
        game.ApplyKey(OfferKey(game, TradeGood.Hide));
        Assert.Equal(2, game.Player.Hide);
        game.ApplyKey(' ');

        // The forge refuses the sitting whole: no coin, no filing, no feed.
        BumpTowner(game, "npc_townsmith");
        game.ApplyKey(OfferKey(game, TradeGood.Forge));
        Assert.Equal(10, weapon.Wear);
        Assert.Equal(20, game.Player.Coin);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Smithing));
        game.ApplyKey(' ');

        // The moot itself always hears: the plea stands, the fine moves the
        // book, and the craft is seeded by the answering.
        BumpTowner(game, "npc_mootwarden");
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Plea);
        game.ApplyKey(OfferKey(game, TradeGood.Plea));
        Assert.Equal(TownLaw.BarredRung - 1, game.TownBook);
        Assert.Equal(20 - TownLaw.FineCoin, game.Player.Coin);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Persuasion));
    }

    [Fact]
    public void ThePlea_AnswersMarkByMark_AndGoesHomeEven()
    {
        var game = new Game(42);
        game.Player.Coin = 2 * TownLaw.FineCoin;
        EnterTown(game);
        game.Debug_RaiseTownBook(2);

        BumpTowner(game, "npc_mootwarden");
        game.ApplyKey(OfferKey(game, TradeGood.Plea));
        Assert.Equal(1, game.TownBook);
        game.ApplyKey(OfferKey(game, TradeGood.Plea));
        Assert.Equal(0, game.TownBook);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(2, game.Player.Skills.Uses(SkillId.Persuasion));

        // The book even, the digit goes home: the warden keeps no counter,
        // which is rather the point of him (D-140's line, still true).
        Assert.DoesNotContain(game.Offers, o => o.Good == TradeGood.Plea);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("stands even"));
    }

    [Fact]
    public void ThePracticedPleader_ShavesTheFine()
    {
        var game = new Game(42);
        for (int i = 0; i < 8; i++) game.Player.Skills.AddUse(SkillId.Persuasion);
        game.Player.Coin = TownLaw.FineCoin - 1; // exactly the shaved fine
        EnterTown(game);
        game.Debug_RaiseTownBook(1);

        BumpTowner(game, "npc_mootwarden");
        game.ApplyKey(OfferKey(game, TradeGood.Plea));
        Assert.Equal(0, game.TownBook);
        Assert.Equal(0, game.Player.Coin);
    }

    [Fact]
    public void TheMootTopic_ReadsTheBook()
    {
        var game = new Game(42);
        EnterTown(game);
        game.Debug_RaiseTownBook(1);
        BumpTowner(game, "npc_mootwarden");
        Assert.Contains(game.Topics, t => t.Label == "The moot" && t.Answer.Contains("1 mark against it"));
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k', (0, 1) => 'j', (-1, 0) => 'h', (1, 0) => 'l',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
