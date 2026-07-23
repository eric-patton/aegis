using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>The tier-7 world-law library and its three opening laws (D-151/D-152).</summary>
public class WorldTwistTests
{
    [Fact]
    public void ShuffleBag_DealsAllThreeBeforeARepeat_WithoutBoundaryRepeats()
    {
        var expected = WorldTwistCatalog.All.Order().ToArray();
        for (ulong seed = 1; seed <= 40; seed++)
        {
            Assert.Equal(WorldTwist.None, WorldTwistCatalog.ForCycle(seed, 6));
            for (int first = 7; first <= 13; first += 3)
                Assert.Equal(expected, Enumerable.Range(first, 3)
                    .Select(tier => WorldTwistCatalog.ForCycle(seed, tier)).Order().ToArray());
            Assert.NotEqual(WorldTwistCatalog.ForCycle(seed, 9), WorldTwistCatalog.ForCycle(seed, 10));
            Assert.NotEqual(WorldTwistCatalog.ForCycle(seed, 12), WorldTwistCatalog.ForCycle(seed, 13));
        }
    }

    [Fact]
    public void Generation_AddsOnlyTheSelectedLawsParts_AndReportsThem()
    {
        var held = WorldGen.Generate(77, tier: 7, twist: WorldTwist.HeldRoad);
        Assert.Equal(WorldTwistCatalog.WaystonesPerWorld, held.Waystones.Count);
        Assert.All(held.Waystones, p => Assert.Equal(Terrain.Waystone, held.Road[p]));
        Assert.NotNull(held.RoadHolder);
        Assert.True(held.Facts.Exists("twist", "held_road"));

        var grave = WorldGen.Generate(77, tier: 7, twist: WorldTwist.GraveMarket);
        var tallies = grave.Npcs.Where(n => n.Kind == NpcKind.GraveTally).ToList();
        Assert.Equal(2, tallies.Count);
        Assert.Equal(["barrow", "fell-cairn"], tallies.Select(n => n.SiteId!).Order().ToArray());
        Assert.All(tallies, tally =>
        {
            var site = grave.Sites.Single(s => s.Id == tally.SiteId);
            Assert.Equal(1, Directions.Cardinal.Count(d => site.Map.Walkable(tally.Pos.Plus(d.dx, d.dy))));
            Assert.True(grave.Facts.Exists("person", tally.Id));
        });
        Assert.Empty(grave.Waystones);
        Assert.Null(grave.RoadHolder);

        var horned = WorldGen.Generate(77, tier: 7, twist: WorldTwist.HornedLaw);
        Assert.Empty(horned.Waystones);
        Assert.DoesNotContain(horned.Npcs, n => n.Kind == NpcKind.GraveTally);
        Assert.Equal("horned_law", WorldEval.Measure(horned).Twist);
    }

    [Fact]
    public void HeldRoad_WaystoneSheltersCamp_AndOfficialCountersTitheOnlyCompletedBusiness()
    {
        var game = GameAt(WorldTwist.HeldRoad);
        TakeRoad(game);
        game.Debug_SetPlayerPos(game.World.Waystones[0]);
        game.Debug_SetSky(RoadSky.Rain);
        game.Debug_HurtPlayer(10);
        game.Player.Rations = 1;
        int hp = game.Player.Hp;
        int turn = game.Turn;

        game.ApplyKey('m');

        Assert.Equal(hp + RoadLife.CampHealBase, game.Player.Hp);
        Assert.Equal(turn + RoadLife.CampTurns, game.Turn);

        game.Player.Coin = 20;
        Bump(game, game.World.Waykeeper);
        game.ApplyKey(OfferKey(game, TradeGood.Ration));
        Assert.Equal(20 - Peddling.RationPrice - WorldTwistCatalog.RoadTithe, game.Player.Coin);
        Assert.Equal(1, game.RoadTithes);
        Assert.Contains("held_road", game.TakeSnapshot().WorldTwist);
    }

    [Fact]
    public void GraveMarket_WaitsAndSettlesOneMouthWithoutRewardsOrDeed()
    {
        var game = GameAt(WorldTwist.GraveMarket);
        var barrow = game.World.BarrowSite!;
        Enter(game, barrow);
        var wights = game.Monsters.Where(m => m.Alive && m.SiteId == barrow.Id
            && m.Kind == MonsterKind.Wight).ToList();
        var before = wights.Select(m => (m.Pos, m.Hp)).ToArray();
        int hp = game.Player.Hp;

        game.ApplyKey('.');

        Assert.Equal(hp, game.Player.Hp);
        Assert.Equal(before, wights.Select(m => (m.Pos, m.Hp)).ToArray());
        Assert.All(wights, w => Assert.Null(w.Intent));

        int price = game.GraveBargainPrice(barrow);
        Assert.Equal(wights.Count * WorldTwistCatalog.GravePricePerWight(false), price);
        game.Player.Essence = price;
        Bump(game, game.NpcsHere.Single(n => n.Kind == NpcKind.GraveTally));
        game.ApplyKey(OfferKey(game, TradeGood.GraveBargain));

        Assert.True(barrow.Cleared);
        Assert.True(game.GraveTruceStands);
        Assert.Equal(0, game.Player.Essence);
        Assert.DoesNotContain(game.World.Facts.All, f => f.Type == "deed" && f.Subject == "barrow_stilled");
        Assert.Equal(0, game.Player.Coin);
    }

    [Fact]
    public void GraveMarket_BlowOrUnboughtGoodsClosesBothBooks()
    {
        var blow = GameAt(WorldTwist.GraveMarket);
        var barrow = blow.World.BarrowSite!;
        Enter(blow, barrow);
        var target = blow.Monsters.First(m => m.Alive && m.SiteId == barrow.Id);
        Bump(blow, target);
        Assert.False(blow.GraveTruceStands);
        Assert.All(blow.Monsters.Where(m => m.Alive && m.Kind == MonsterKind.Wight), w => Assert.False(w.Dormant));

        var theft = GameAt(WorldTwist.GraveMarket);
        var otherBarrow = theft.World.BarrowSite!;
        Enter(theft, otherBarrow);
        theft.Debug_SetPlayerPos(otherBarrow.ChestPos);
        theft.ApplyKey('g');
        Assert.False(theft.GraveTruceStands);
        Assert.True(otherBarrow.ChestLooted);
    }

    [Fact]
    public void HornedLaw_SeparatesProtectedHides_FencesThem_AndBooksThemAtTheGate()
    {
        var hunted = GameAt(WorldTwist.HornedLaw);
        TakeRoad(hunted);
        var trail = hunted.World.RoadWildsSite;
        Enter(hunted, trail);
        var hart = hunted.Monsters.First(m => m.Alive && m.SiteId == trail.Id && m.Kind == MonsterKind.Hart);
        hart.Hp = 1;
        Bump(hunted, hart);
        Assert.True(hunted.Player.ProtectedHide > 0);
        Assert.Equal(0, hunted.Player.Hide);
        Assert.True(hunted.World.Facts.Exists("secret", "protected_hart_taken"));

        var fenced = GameAt(WorldTwist.HornedLaw);
        fenced.Player.ProtectedHide = 2;
        fenced.Player.Coin = 0;
        Bump(fenced, fenced.World.Peddler);
        fenced.ApplyKey(OfferKey(fenced, TradeGood.Fence));
        Assert.Equal(2 * WorldTwistCatalog.ProtectedHideFencePrice, fenced.Player.Coin);
        Assert.Equal(0, fenced.Player.ProtectedHide);
        Assert.Equal(0, fenced.TownBook);

        var booked = GameAt(WorldTwist.HornedLaw);
        booked.Player.ProtectedHide = 2;
        TakeRoad(booked);
        booked.Debug_SetPlayerPos(booked.World.TownSite.OverworldPos);
        booked.Apply(Command.Enter);
        Assert.Equal(0, booked.Player.ProtectedHide);
        Assert.Equal(1, booked.TownBook);
    }

    [Fact]
    public void HornedLaw_WolfHidesEarnTheTownBonus_AndProtectedProvenanceEndsAtCrossing()
    {
        var game = GameAt(WorldTwist.HornedLaw);
        TakeRoad(game);
        game.Debug_SetPlayerPos(game.World.FellMouthPos);
        game.Apply(Command.Enter);
        var combe = game.World.FellWildsSite;
        Enter(game, combe);
        var wolf = game.Monsters.First(m => m.Alive && m.SiteId == combe.Id && m.Kind == MonsterKind.Wolf);
        wolf.Hp = 1;
        Bump(game, wolf);
        int hides = game.Player.Hide;
        Assert.True(hides > 0);
        Assert.Equal(hides, game.WolfBountyHides);

        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.FellHomePos);
        game.Apply(Command.Enter);
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.Apply(Command.Enter);
        Bump(game, game.NpcsHere.Single(n => n.Id == "npc_hidemonger"));
        game.Player.Coin = 0;
        game.ApplyKey(OfferKey(game, TradeGood.Hide));
        Assert.Equal(hides * (TownMarket.HidePrice + WorldTwistCatalog.WolfHideTownBonus), game.Player.Coin);

        var crossing = GameAt(WorldTwist.HornedLaw);
        crossing.Player.Hide = 1;
        crossing.Player.ProtectedHide = 2;
        Cross(crossing);
        Assert.Equal(3, crossing.Player.Hide);
        Assert.Equal(0, crossing.Player.ProtectedHide);
    }

    [Fact]
    public void CairnAndGillClears_DoNotFallThroughToTheSeveredDeed()
    {
        var game = new Game(42);
        game.Debug_ClearSite(SiteKind.Cairn);
        game.Debug_ClearSite(SiteKind.Gill);

        Assert.True(game.World.FellCairnSite.Cleared);
        Assert.True(game.World.FellGillSite.Cleared);
        Assert.False(game.World.Facts.Exists("deed", "severed_laid"));
    }

    [Fact]
    public void LeanDarkHalvesTheGraveMarketsAlreadyHalvedPrice()
    {
        Assert.Equal(4, WorldTwistCatalog.GravePricePerWight(false));
        Assert.Equal(2, WorldTwistCatalog.GravePricePerWight(true));
    }

    private static Game GameAt(WorldTwist twist)
    {
        ulong seed = Enumerable.Range(1, 500).Select(i => (ulong)i)
            .First(s => WorldTwistCatalog.ForCycle(s, WorldTwistCatalog.FirstTier) == twist);
        var game = new Game(seed);
        while (game.Cycle < WorldTwistCatalog.FirstTier) Cross(game);
        Assert.Equal(twist, game.World.Twist);
        return game;
    }

    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
    }

    private static void TakeRoad(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.Apply(Command.Enter);
        Assert.Equal(Area.Road, game.Area);
    }

    private static void Enter(Game game, Site site)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(site.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal(site.Id, game.CurrentSite!.Id);
    }

    private static void Bump(Game game, Npc npc)
    {
        var beside = Directions.All8.Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => game.CurrentMap.Walkable(p)
                && !game.NpcsHere.Any(n => n.Pos == p)
                && !game.LiveMonstersHere.Any(m => m.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
    }

    private static void Bump(Game game, Monster monster)
    {
        var beside = Directions.All8.Select(d => monster.Pos.Plus(d.dx, d.dy))
            .First(p => game.CurrentMap.Walkable(p)
                && !game.NpcsHere.Any(n => n.Pos == p)
                && !game.LiveMonstersHere.Any(m => m != monster && m.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(monster.Pos.X - beside.X, monster.Pos.Y - beside.Y));
    }

    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

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
