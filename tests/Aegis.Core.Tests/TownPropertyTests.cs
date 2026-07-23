using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The town's private room and law-day (D-161): a keyed holding, its useful
/// furniture, a fitted workshop, the stable shelf, and formal nonlethal law.
/// </summary>
public class TownPropertyTests
{
    [Fact]
    public void TownStitch_HoldsTheLoftRingAndMarshal_ReachableAcrossSeeds()
    {
        Terrain[] fixtures =
        [
            Terrain.LoftDoor, Terrain.LoftBed, Terrain.LoftDesk,
            Terrain.LoftStrongbox, Terrain.LoftWorkshop, Terrain.LawDayRing,
        ];

        for (ulong seed = 1; seed <= 100; seed++)
        {
            var world = WorldGen.Generate(seed, 8);
            var reached = Reachable(world.TownSite.Map, world.TownSite.EntryPos);
            foreach (var fixture in fixtures)
                Assert.Contains(Cells(world.TownSite.Map, fixture), reached.Contains);

            var marshal = world.Npcs.Single(n => n.Id == "npc_listsmarshal");
            Assert.Equal("lists marshal", marshal.Role);
            Assert.Contains(marshal.Pos, reached);
        }
    }

    [Fact]
    public void Shelf_KeepsSixStableTitles_AndOwnershipDoesNotShiftDigits()
    {
        var game = new Game(42);
        EnterTown(game);
        Bump(game, "npc_scrivener");
        Assert.DoesNotContain(game.Offers, o => o.Good == TradeGood.Book);
        game.ApplyKey(OfferKey(game, TradeGood.Shelf));

        string[] ids = ["herbal", "bestiary", "lay", "smithing", "town_law", "folk_tales"];
        Assert.Equal(ids, game.TradeOffers.Select(o => o.Arg));
        Assert.All(game.TradeOffers, o => Assert.Equal(TradeGood.Book, o.Good));

        game.Debug_BankLore(SkillSet.UsesForLevel(1));
        game.Player.Coin = 100;
        int townLawDigit = game.TradeOffers.ToList().FindIndex(o => o.Arg == "town_law");
        game.ApplyKey((char)('1' + townLawDigit));

        Assert.Contains(BookId.TownLaw, game.Player.Books);
        Assert.Equal(ids, game.TradeOffers.Select(o => o.Arg));
        Assert.Contains("yours", game.TradeOffers[townLawDigit].Label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LastTwoBooks_KeepTheirExactPricesGatesAndFiveDeskSittings()
    {
        var law = BookCatalog.Def(BookId.TownLaw);
        var tales = BookCatalog.Def(BookId.FolkTales);
        Assert.Equal((11, 1, 5), (law.Price, law.LoreReq, law.Sittings));
        Assert.Equal((10, 1, 5), (tales.Price, tales.LoreReq, tales.Sittings));

        var game = OwnedLoftGame();
        game.Debug_GiveBook(BookId.TownLaw);
        game.Debug_GiveBook(BookId.FolkTales);
        game.Debug_BankLore(SkillSet.UsesForLevel(1));
        game.Debug_SetPlayerPos(Cell(game.CurrentMap, Terrain.LoftDesk));
        for (int i = 0; i < law.Sittings + tales.Sittings; i++) game.ApplyKey('v');

        Assert.True(game.Player.HasRead(BookId.TownLaw));
        Assert.True(game.Player.HasRead(BookId.FolkTales));
        Assert.Contains("town_law", game.TakeSnapshot().BooksRead);
        Assert.Contains("folk_tales", game.TakeSnapshot().BooksRead);
    }

    [Fact]
    public void Loft_RequiresPrimerBondEvenBookAndCoin_ThenItsKeyOutlastsLaterMarks()
    {
        var game = new Game(42);
        game.Player.Coin = 500;
        EnterTown(game);
        Bump(game, "npc_guildmaster");

        game.ApplyKey(OfferKey(game, TradeGood.Loft));
        Assert.False(game.GuildLoftOwned);

        game.Player.BooksRead.Add(BookId.TownLaw);
        game.ApplyKey(OfferKey(game, TradeGood.Loft));
        Assert.False(game.GuildLoftOwned);

        for (int i = 0; i < SkillSet.UsesForLevel(1); i++) game.Player.Skills.AddUse(SkillId.Commerce);
        game.ApplyKey(OfferKey(game, TradeGood.Bond));
        Assert.True(game.GuildSworn);
        game.Debug_RaiseTownBook(1);
        game.ApplyKey(OfferKey(game, TradeGood.Loft));
        Assert.False(game.GuildLoftOwned);

        game.ApplyKey(' ');
        Bump(game, "npc_mootwarden");
        game.ApplyKey(OfferKey(game, TradeGood.Plea));
        Assert.Equal(0, game.TownBook);
        game.ApplyKey(' ');
        Bump(game, "npc_guildmaster");
        int before = game.Player.Coin;
        game.ApplyKey(OfferKey(game, TradeGood.Loft));
        Assert.True(game.GuildLoftOwned);
        Assert.Equal(before - TownProperty.LoftCoin, game.Player.Coin);
        game.ApplyKey(' ');

        game.Debug_RaiseTownBook(TownLaw.BarredRung);
        var door = Cell(game.CurrentMap, Terrain.LoftDoor);
        var beside = AdjacentOpen(game, door);
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(door.X - beside.X, door.Y - beside.Y));
        Assert.Equal(door, game.Player.Pos);
    }

    [Fact]
    public void LoftFurniture_RestsReadsAndMovesTheWholePurse()
    {
        var game = OwnedLoftGame();
        game.Player.Coin = 37;
        game.Debug_SetPlayerPos(Cell(game.CurrentMap, Terrain.LoftStrongbox));
        int turn = game.Turn;
        game.ApplyKey('g');
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(37, game.BoxedCoin);
        Assert.Equal(turn + 1, game.Turn);
        Assert.True(game.World.Facts.Exists("property-use", "boxed_coin_in"));

        game.ApplyKey('g');
        Assert.Equal(37, game.Player.Coin);
        Assert.Equal(0, game.BoxedCoin);
        Assert.True(game.World.Facts.Exists("property-use", "boxed_coin_out"));

        game.Player.Hp = 1;
        game.Player.Stamina = 0;
        game.Player.Focus = 0;
        game.Debug_SetPlayerPos(Cell(game.CurrentMap, Terrain.LoftBed));
        game.ApplyKey('r');
        Assert.Equal(game.Player.EffectiveMaxHp, game.Player.Hp);
        Assert.Equal(game.Player.MaxStamina, game.Player.Stamina);
        Assert.Equal(game.Player.MaxFocus, game.Player.Focus);
        Assert.False(game.InShrineMenu);
        Assert.True(game.World.Facts.Exists("property-use", "loft_bed"));

        game.Debug_GiveBook(BookId.TownLaw);
        game.Debug_BankLore(SkillSet.UsesForLevel(1));
        game.Debug_SetPlayerPos(Cell(game.CurrentMap, Terrain.LoftDesk));
        game.ApplyKey('v');
        Assert.Equal(1, game.Player.BookSittings.GetValueOrDefault(BookId.TownLaw));
        Assert.True(game.World.Facts.Exists("property-use", "loft_desk"));
    }

    [Fact]
    public void Strongbox_IsOutsideDeathAndRaid_AndJoinsTheCrossingWeighing()
    {
        var game = OwnedLoftGame();
        game.Player.Coin = 31;
        game.Debug_SetPlayerPos(Cell(game.CurrentMap, Terrain.LoftStrongbox));
        game.ApplyKey('g');

        game.Player.Hp = 0;
        game.Debug_ForceDeathCheck();
        Assert.Equal(31, game.BoxedCoin);
        Assert.Null(game.Remnant);
        game.Debug_Raid();
        Assert.Equal(31, game.BoxedCoin);

        int legend = game.Player.Legend;
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');
        Assert.True(game.Player.Legend >= legend + 31);
        Assert.Equal(0, game.BoxedCoin);
        Assert.False(game.GuildLoftOwned);
    }

    [Fact]
    public void FittedWorkshop_UsesTheSharedRepairArithmeticWithoutAnotherCharge()
    {
        var game = OwnedLoftGame();
        game.Player.Coin = TownProperty.WorkshopCoin;
        for (int i = 0; i < SkillSet.UsesForLevel(2); i++) game.Player.Skills.AddUse(SkillId.Smithing);
        Bump(game, "npc_townsmith");
        game.ApplyKey(OfferKey(game, TradeGood.Workshop));
        Assert.True(game.WorkshopFitted);
        Assert.Equal(0, game.Player.Coin);
        game.ApplyKey(' ');

        var axe = GearCatalog.Create("woodaxe");
        var shirt = GearCatalog.Create("riveted_shirt");
        axe.Wear = 9;
        shirt.Wear = 15;
        game.Player.Weapon = axe;
        game.Player.Armor = shirt;
        int uses = game.Player.Skills.Uses(SkillId.Smithing);
        game.Debug_SetPlayerPos(Cell(game.CurrentMap, Terrain.LoftWorkshop));
        game.ApplyKey('g');

        int rate = SteadFacilities.BenchBase + SteadFacilities.BenchPerLevel * 2;
        Assert.Equal(9, axe.Wear);
        Assert.Equal(15 - rate, shirt.Wear);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(uses + 1, game.Player.Skills.Uses(SkillId.Smithing));
        Assert.True(game.World.Facts.Exists("property-use", "workshop_sitting"));

        shirt.Wear = 0;
        axe.Wear = 0;
        game.ApplyKey('g');
        Assert.Equal(uses + 1, game.Player.Skills.Uses(SkillId.Smithing));
    }

    [Fact]
    public void HeldRoad_TithesEachOfficialPropertyAndLawDayPurchaseExactlyOnce()
    {
        var game = GameAt(WorldTwist.HeldRoad);
        game.Player.BooksRead.Add(BookId.TownLaw);
        game.World.Facts.Add("guild", "guild_sworn", game.World.TownName, "test bond");
        game.Player.Coin = TownProperty.LoftCoin + WorldTwistCatalog.RoadTithe;
        EnterTown(game);
        Bump(game, "npc_guildmaster");
        game.ApplyKey(OfferKey(game, TradeGood.Loft));
        Assert.True(game.GuildLoftOwned);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(1, game.RoadTithes);

        game.ApplyKey(' ');
        for (int i = 0; i < SkillSet.UsesForLevel(2); i++) game.Player.Skills.AddUse(SkillId.Smithing);
        game.Player.Coin = TownProperty.WorkshopCoin + WorldTwistCatalog.RoadTithe;
        Bump(game, "npc_townsmith");
        game.ApplyKey(OfferKey(game, TradeGood.Workshop));
        Assert.True(game.WorkshopFitted);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(2, game.RoadTithes);

        game.ApplyKey(' ');
        game.Player.Coin = LawDayLists.EntryCoin + WorldTwistCatalog.RoadTithe;
        Bump(game, "npc_listsmarshal");
        game.ApplyKey(OfferKey(game, TradeGood.Lists));
        Assert.True(game.ListsEntered);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(3, game.RoadTithes);
    }

    [Fact]
    public void Lists_TakeOneEntryAndThreeYieldsPayOneChampionPurse()
    {
        var game = new Game(42);
        game.Player.Coin = LawDayLists.EntryCoin;
        EnterTown(game);
        Bump(game, "npc_listsmarshal");
        game.ApplyKey(OfferKey(game, TradeGood.Lists));

        Assert.True(game.ListsEntered);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(FormalBoutKind.Lists, game.FormalBout);
        Assert.Single(game.LiveMonstersHere, m => m.FormalName is not null);

        int deaths = game.Player.Deaths;
        int essence = game.Player.Essence;
        for (int bout = 1; bout <= LawDayLists.Bouts; bout++)
        {
            game.Player.Hp = 1;
            game.Player.Stamina = 0;
            game.Player.Focus = 0;
            game.Debug_ResolveFormalBout(playerYielded: false);
            Assert.Equal(game.Player.EffectiveMaxHp, game.Player.Hp);
            Assert.Equal(game.Player.MaxStamina, game.Player.Stamina);
            Assert.Equal(game.Player.MaxFocus, game.Player.Focus);
            Assert.Equal(bout, game.ListsWins);
        }

        Assert.Null(game.FormalBout);
        Assert.True(game.ListsChampion);
        Assert.Equal(LawDayLists.ChampionPurse, game.Player.Coin);
        Assert.Equal(deaths, game.Player.Deaths);
        Assert.Equal(essence, game.Player.Essence);

        Bump(game, "npc_listsmarshal");
        game.ApplyKey(OfferKey(game, TradeGood.Lists));
        Assert.Null(game.FormalBout);
        Assert.Equal(LawDayLists.ChampionPurse, game.Player.Coin);
    }

    [Fact]
    public void Lists_RefuseMarkedWoundedAndShortCoinEntriesWhole()
    {
        var marked = new Game(42);
        marked.Player.Coin = 100;
        marked.Debug_RaiseTownBook(1);
        EnterTown(marked);
        Bump(marked, "npc_listsmarshal");
        marked.ApplyKey(OfferKey(marked, TradeGood.Lists));
        Assert.False(marked.ListsEntered);
        Assert.Equal(100, marked.Player.Coin);

        var wounded = new Game(43);
        wounded.Player.Coin = 100;
        EnterTown(wounded);
        Bump(wounded, "npc_listsmarshal");
        wounded.Player.WoundedTurns = 3;
        wounded.ApplyKey(OfferKey(wounded, TradeGood.Lists));
        Assert.False(wounded.ListsEntered);
        Assert.Equal(100, wounded.Player.Coin);

        var shortCoin = new Game(44);
        shortCoin.Player.Coin = LawDayLists.EntryCoin - 1;
        EnterTown(shortCoin);
        Bump(shortCoin, "npc_listsmarshal");
        shortCoin.ApplyKey(OfferKey(shortCoin, TradeGood.Lists));
        Assert.False(shortCoin.ListsEntered);
        Assert.Equal(LawDayLists.EntryCoin - 1, shortCoin.Player.Coin);
    }

    [Fact]
    public void ACombatYield_FeedsTheWeaponSkillButNoOrdinaryKillLedger()
    {
        var game = new Game(42);
        game.Player.Coin = LawDayLists.EntryCoin;
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        EnterTown(game);
        Bump(game, "npc_listsmarshal");
        game.ApplyKey(OfferKey(game, TradeGood.Lists));

        int essence = game.Player.Essence;
        int deaths = game.Player.Deaths;
        int hafted = game.Player.Skills.Uses(SkillId.Hafted);
        int killFacts = game.World.Facts.OfType("kill").Count();
        int reads = game.Player.Reads.Count;
        for (int attempt = 0; attempt < 50 && game.ListsWins == 0; attempt++)
        {
            var opponent = game.LiveMonstersHere.Single(m => m.FormalName is not null);
            opponent.Hp = 1;
            var beside = Directions.All8.Select(d => opponent.Pos.Plus(d.dx, d.dy))
                .First(p => game.CurrentMap[p] == Terrain.LawDayRing && p != opponent.Pos);
            game.Debug_SetPlayerPos(beside);
            game.Player.Hp = game.Player.EffectiveMaxHp;
            game.ApplyKey(KeyFor(opponent.Pos.X - beside.X, opponent.Pos.Y - beside.Y));
        }

        Assert.Equal(1, game.ListsWins);
        Assert.Equal(essence, game.Player.Essence);
        Assert.Equal(deaths, game.Player.Deaths);
        Assert.True(game.Player.Skills.Uses(SkillId.Hafted) > hafted);
        Assert.Equal(killFacts, game.World.Facts.OfType("kill").Count());
        Assert.Equal(reads, game.Player.Reads.Count);
    }

    [Fact]
    public void FormalLoss_IsNonlethalAndClosesTheEntryWithoutRewards()
    {
        var game = new Game(99);
        game.Player.Coin = LawDayLists.EntryCoin;
        EnterTown(game);
        Bump(game, "npc_listsmarshal");
        game.ApplyKey(OfferKey(game, TradeGood.Lists));
        var opponent = game.LiveMonstersHere.Single(m => m.FormalName is not null);
        var returnPos = game.TakeSnapshot();
        int deaths = game.Player.Deaths;
        int essence = game.Player.Essence;
        int wrath = game.Wrath;
        int grudge = game.Grudge;

        game.Debug_ResolveFormalBout(playerYielded: true);

        Assert.Null(game.FormalBout);
        Assert.False(opponent.Alive);
        Assert.Equal(0, game.ListsWins);
        Assert.Equal(deaths, game.Player.Deaths);
        Assert.Equal(essence, game.Player.Essence);
        Assert.Equal(wrath, game.Wrath);
        Assert.Equal(grudge, game.Grudge);
        Assert.Null(game.Remnant);
        Assert.Equal("", game.TakeSnapshot().FormalBout);
        Assert.NotNull(returnPos);
    }

    [Fact]
    public void JudicialChallenge_IsOncePerWorldAndNeverFeedsPersuasion()
    {
        var win = JudicialGame(42, marks: 2);
        int persuasion = win.Player.Skills.Uses(SkillId.Persuasion);
        win.Debug_ResolveFormalBout(playerYielded: false);
        Assert.Equal(1, win.TownBook);
        Assert.Equal(persuasion, win.Player.Skills.Uses(SkillId.Persuasion));
        Assert.True(win.JudicialChallengeUsed);

        Bump(win, "npc_mootwarden");
        win.ApplyKey(OfferKey(win, TradeGood.Judicial));
        Assert.Null(win.FormalBout);
        Assert.Equal(1, win.TownBook);

        var loss = JudicialGame(77, marks: 2);
        loss.Debug_ResolveFormalBout(playerYielded: true);
        Assert.Equal(2, loss.TownBook);
        Assert.True(loss.JudicialChallengeUsed);
        Assert.Equal(0, loss.Player.Skills.Uses(SkillId.Persuasion));
    }

    [Fact]
    public void FolkBook_OpensThreeFactKeyedCharacterStorylets()
    {
        var game = new Game(42);
        game.Player.BooksRead.Add(BookId.FolkTales);
        game.World.Facts.Add("event", "hard_winter", game.World.SettlementName, "test fact");
        game.Debug_FireStorylet(StoryletTrigger.NearHouse);
        Assert.True(game.World.Facts.Exists("book-tale", "winter-line"));

        game.World.Facts.Add("deed", "camp_cleared", game.World.SettlementName, "test fact");
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager && !n.OnRoad);
        BumpOverworld(game, villager);
        Assert.True(game.World.Facts.Exists("book-tale", "quiet-den"));

        game.ApplyKey(' ');
        game.World.Facts.Add("resource-state", "black_tarn_worked", game.World.FellRegion.Name, "test fact");
        for (int i = 0; i < 500 && !game.World.Facts.Exists("book-tale", "black-water"); i++)
            game.Debug_FireStorylet(StoryletTrigger.AmbientTurn);
        Assert.True(game.World.Facts.Exists("book-tale", "black-water"));
    }

    private static Game JudicialGame(ulong seed, int marks)
    {
        var game = new Game(seed);
        game.Player.BooksRead.Add(BookId.TownLaw);
        game.Debug_RaiseTownBook(marks);
        EnterTown(game);
        Bump(game, "npc_mootwarden");
        game.ApplyKey(OfferKey(game, TradeGood.Judicial));
        Assert.Equal(FormalBoutKind.Judicial, game.FormalBout);
        return game;
    }

    private static Game OwnedLoftGame()
    {
        var game = new Game(42);
        game.World.Facts.Add("property", "guild_loft", game.World.TownName, "test holding");
        EnterTown(game);
        return game;
    }

    private static Game GameAt(WorldTwist twist)
    {
        ulong seed = Enumerable.Range(1, 500).Select(i => (ulong)i)
            .First(s => WorldTwistCatalog.ForCycle(s, WorldTwistCatalog.FirstTier) == twist);
        var game = new Game(seed);
        while (game.Cycle < WorldTwistCatalog.FirstTier)
        {
            game.Debug_SetMode(MapMode.Overworld);
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.ApplyKey('>');
            game.ApplyKey('>');
        }
        return game;
    }

    private static void EnterTown(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
    }

    private static void Bump(Game game, string id)
    {
        var npc = game.NpcsHere.Single(n => n.Id == id);
        var beside = AdjacentOpen(game, npc.Pos);
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
    }

    private static void BumpOverworld(Game game, Npc npc)
    {
        game.Debug_SetMode(MapMode.Overworld);
        var beside = Directions.All8.Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => game.CurrentMap.Walkable(p) && !game.World.Npcs.Any(n => !n.OnRoad && n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
    }

    private static Pos AdjacentOpen(Game game, Pos target) => Directions.All8
        .Select(d => target.Plus(d.dx, d.dy))
        .First(p => game.CurrentMap.Walkable(p) && !game.NpcsHere.Any(n => n.Pos == p));

    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    private static Pos Cell(GameMap map, Terrain terrain) => Cells(map, terrain).Single();

    private static IEnumerable<Pos> Cells(GameMap map, Terrain terrain)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var p = new Pos(x, y);
                if (map[p] == terrain) yield return p;
            }
    }

    private static HashSet<Pos> Reachable(GameMap map, Pos start)
    {
        var seen = new HashSet<Pos> { start };
        var queue = new Queue<Pos>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var d in Directions.All8)
            {
                var next = p.Plus(d.dx, d.dy);
                if (map.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return seen;
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k', (0, 1) => 'j', (-1, 0) => 'h', (1, 0) => 'l',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
