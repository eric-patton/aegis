using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The town's depth, first cut (D-141, plan 2026-07 step 10): the forge-smith's
/// school (iron filed for coin, Smithing fed away from home, the drawn temper
/// taught only to proven hands) and the carriers' guild (the bond sworn once
/// per world on a proven name, the mark worth a coin on every town lot). The
/// tests hold the school's arithmetic, its refusals, the lesson's gate and
/// keep, and the bond's whole ledger.
/// </summary>
public class TownDepthTests
{
    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    /// <summary>Walks the real road and gate: the mouth, then the arch.</summary>
    private static void EnterTown(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        Assert.True(game.OnRoad);
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
    }

    /// <summary>Bumps a towner inside the town through the real key surface.</summary>
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

    [Fact]
    public void TheForge_FilesIron_AndFeedsTheCraft()
    {
        var game = new Game(42);
        var weapon = GearCatalog.Create("woodaxe");
        game.Player.Weapon = weapon;
        weapon.Wear = 10;
        game.Player.Coin = TownForge.WorkCoin;
        EnterTown(game);
        BumpTowner(game, "npc_townsmith");
        Assert.True(game.InTalkMenu);

        // One sitting: the green hand's base off the worst piece, the coin
        // into the smith's box, and the craft fed exactly one honest use.
        game.ApplyKey(OfferKey(game, TradeGood.Forge));
        Assert.Equal(10 - SteadFacilities.BenchBase, weapon.Wear);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Smithing));
    }

    [Fact]
    public void TheForge_TakesNothing_ForTrueIron_OrShortCoin()
    {
        var game = new Game(42);
        game.Player.Coin = 20;
        EnterTown(game);
        BumpTowner(game, "npc_townsmith");

        // True iron: the smith hands it back, keeps nothing, teaches nothing.
        game.ApplyKey(OfferKey(game, TradeGood.Forge));
        Assert.Equal(20, game.Player.Coin);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Smithing));

        // Short coin: the sitting refused whole; the forge runs no slates.
        var weapon = GearCatalog.Create("woodaxe");
        game.Player.Weapon = weapon;
        weapon.Wear = 6;
        game.Player.Coin = TownForge.WorkCoin - 1;
        game.ApplyKey(OfferKey(game, TradeGood.Forge));
        Assert.Equal(6, weapon.Wear);
        Assert.Equal(TownForge.WorkCoin - 1, game.Player.Coin);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Smithing));
    }

    [Fact]
    public void TheDrawnTemper_IsShown_OnlyToProvenHands()
    {
        var game = new Game(42);
        EnterTown(game);
        BumpTowner(game, "npc_townsmith");

        // A green hand sees the sitting alone: the showing is not on the board.
        Assert.DoesNotContain(game.Offers, o => o.Good == TradeGood.Lesson);
        game.ApplyKey(' ');

        // The iron answers the hands (Smithing 1): the showing appears, and
        // the coin buys a keep that follows the bearer to any bench anywhere.
        for (int i = 0; i < 8; i++) game.Player.Skills.AddUse(SkillId.Smithing);
        game.Player.Coin = LessonCatalog.Def(LessonId.DrawnTemper).Price;
        BumpTowner(game, "npc_townsmith");
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Lesson);
        game.ApplyKey(OfferKey(game, TradeGood.Lesson));
        Assert.True(game.Player.HasLesson(LessonId.DrawnTemper));
        Assert.Equal(0, game.Player.Coin);

        // The keep at work: one sitting now files base + level + temper deep.
        var weapon = GearCatalog.Create("woodaxe");
        game.Player.Weapon = weapon;
        weapon.Wear = 20;
        game.Player.Coin = TownForge.WorkCoin;
        game.ApplyKey(OfferKey(game, TradeGood.Forge));
        int rate = SteadFacilities.BenchBase + SteadFacilities.BenchPerLevel + SteadFacilities.TemperBonus;
        Assert.Equal(20 - rate, weapon.Wear);
    }

    [Fact]
    public void TheBond_IsSworn_OnAProvenNameAlone()
    {
        var game = new Game(42);
        game.Player.Coin = CarriersGuild.BondCoin;
        EnterTown(game);
        BumpTowner(game, "npc_guildmaster");
        Assert.True(game.InTalkMenu);
        Assert.Contains(game.Topics, t => t.Label == "The carriers");

        // An unproven name: refused whole, coin unmoved, no book opened.
        game.ApplyKey(OfferKey(game, TradeGood.Bond));
        Assert.False(game.GuildSworn);
        Assert.Equal(CarriersGuild.BondCoin, game.Player.Coin);

        // The market's chalk against the name (Commerce 1): the bond takes.
        for (int i = 0; i < 8; i++) game.Player.Skills.AddUse(SkillId.Commerce);
        game.ApplyKey(OfferKey(game, TradeGood.Bond));
        Assert.True(game.GuildSworn);
        Assert.Equal(0, game.Player.Coin);
        Assert.True(game.World.Facts.Exists("guild", "guild_sworn"));

        // Sworn is sworn: the book takes no name twice and no coin twice.
        game.Player.Coin = CarriersGuild.BondCoin;
        game.ApplyKey(OfferKey(game, TradeGood.Bond));
        Assert.Equal(CarriersGuild.BondCoin, game.Player.Coin);
    }

    [Fact]
    public void TheGuildsMark_RidesEveryTownLot()
    {
        var game = new Game(42);
        for (int i = 0; i < 8; i++) game.Player.Skills.AddUse(SkillId.Commerce);
        game.Player.Coin = CarriersGuild.BondCoin;
        game.Player.Hide = 2;
        EnterTown(game);
        BumpTowner(game, "npc_guildmaster");
        game.ApplyKey(OfferKey(game, TradeGood.Bond));
        Assert.True(game.GuildSworn);
        game.ApplyKey(' ');

        // The lot pays the chalked price, the tongue's level, and the mark.
        BumpTowner(game, "npc_hidemonger");
        game.ApplyKey(OfferKey(game, TradeGood.Hide));
        Assert.Equal(2 * TownMarket.HidePrice + 1 + CarriersGuild.LotBonus, game.Player.Coin);

        // The mark is the town's ledger, per-world like every ledger: the
        // valley benches never read it, because TownHaggle is town-gated.
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Commerce) - 8);
    }

    [Fact]
    public void TheSchool_AndTheHall_KeepTheSharedNine()
    {
        var game = new Game(42);
        // The fullest board the school ever deals: proven hands, so the
        // showing's digit is on the list beside the sitting's.
        for (int i = 0; i < 8; i++) game.Player.Skills.AddUse(SkillId.Smithing);
        EnterTown(game);
        BumpTowner(game, "npc_townsmith");
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
        game.ApplyKey(' ');
        BumpTowner(game, "npc_guildmaster");
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k', (0, 1) => 'j', (-1, 0) => 'h', (1, 0) => 'l',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
