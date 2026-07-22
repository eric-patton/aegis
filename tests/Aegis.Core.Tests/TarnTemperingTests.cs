using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>The first tarn-iron recipe (D-154): learned from a book, worked at the town forge.</summary>
public class TarnTemperingTests
{
    [Fact]
    public void TheRedBook_AsksLoreTwo_AndTeachesTheBloomTemper()
    {
        var game = new Game(42);
        var book = BookCatalog.Def(BookId.Smithing);
        game.Debug_GiveBook(BookId.Smithing);
        game.Debug_BankLore(SkillSet.UsesForLevel(1));
        game.Debug_SetPlayerPos(game.World.ShrinePos);

        int turn = game.Turn;
        game.ApplyKey('v');
        Assert.Equal(turn, game.Turn);
        Assert.False(game.Player.HasRead(BookId.Smithing));

        game.Debug_BankLore(SkillSet.UsesForLevel(2) - SkillSet.UsesForLevel(1));
        for (int i = 0; i < book.Sittings; i++) game.ApplyKey('v');

        Assert.True(game.Player.HasRead(BookId.Smithing));
        Assert.True(game.Player.HasLesson(LessonId.BloomTemper));
        Assert.Contains("smithing", game.TakeSnapshot().BooksRead);
        Assert.Contains("bloom_temper", game.TakeSnapshot().Lessons);
    }

    [Fact]
    public void TheLongHearth_TempersChosenIronOnce_ForOneBloomAndSmithing()
    {
        var game = new Game(42);
        var axe = GearCatalog.Create("woodaxe");
        axe.Wear = 7;
        game.Player.Weapon = axe;
        game.Player.Armor = GearCatalog.Create("riveted_shirt");
        game.Player.Bow = GearCatalog.Create("hunting_bow");
        game.Player.Pack.Add(GearCatalog.Create("quilted_jack"));
        game.Player.IronBloom = 2;
        EnterTown(game);
        Bump(game, "npc_townsmith");

        // The recipe is visible but refuses before the red book is known.
        game.ApplyKey(OfferKey(game, TradeGood.TarnTemper));
        Assert.True(game.InTalkMenu);
        Assert.Equal(2, game.Player.IronBloom);

        game.Player.Lessons.Add(LessonId.BloomTemper);
        game.ApplyKey(OfferKey(game, TradeGood.TarnTemper));
        Assert.True(game.InTradeMenu);
        Assert.Contains(game.TradeOffers, o => o.Arg == "woodaxe");
        Assert.Contains(game.TradeOffers, o => o.Arg == "riveted_shirt");
        Assert.DoesNotContain(game.TradeOffers, o => o.Arg == "hunting_bow");
        Assert.DoesNotContain(game.TradeOffers, o => o.Arg == "quilted_jack");
        Assert.True(game.TradeOffers.Count <= 9);

        int oldMax = axe.MaxWear;
        int oldBonus = axe.Bonus;
        int smithing = game.Player.Skills.Uses(SkillId.Smithing);
        game.ApplyKey(TradeKey(game, TradeGood.TarnTemper, "woodaxe"));

        Assert.True(axe.TarnTempered);
        Assert.Equal(oldMax + FellIron.TemperWear, axe.MaxWear);
        Assert.Equal(7, axe.Wear);
        Assert.Equal(oldBonus, axe.Bonus);
        Assert.Equal(1, game.Player.IronBloom);
        Assert.Equal(smithing + 1, game.Player.Skills.Uses(SkillId.Smithing));
        Assert.True(game.World.Facts.Exists("craft", "tarn_temper_woodaxe"));
        Assert.Contains($"woodaxe:{axe.MaxWear}", game.TakeSnapshot().TarnTemperedGear);

        // The same piece cannot take a second bloom or feed the craft twice.
        game.ApplyKey(TradeKey(game, TradeGood.TarnTemper, "woodaxe"));
        Assert.Equal(1, game.Player.IronBloom);
        Assert.Equal(oldMax + FellIron.TemperWear, axe.MaxWear);
        Assert.Equal(smithing + 1, game.Player.Skills.Uses(SkillId.Smithing));
    }

    [Fact]
    public void ATemperedPiece_CrossesWithItsLongerService()
    {
        var game = new Game(42);
        var iron = GearCatalog.Create("grave_iron");
        iron.TarnTempered = true;
        iron.MaxWear += FellIron.TemperWear;
        game.Player.Weapon = iron;

        game.Debug_ClearCamp();
        game.Player.Coin = 0;
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        Assert.Equal(2, game.Cycle);
        Assert.Same(iron, game.Player.Weapon);
        Assert.True(game.Player.Weapon!.TarnTempered);
        Assert.Equal(55, game.Player.Weapon.MaxWear);
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
        var beside = Directions.All8.Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => game.CurrentMap.Walkable(p) && !game.NpcsHere.Any(n => n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
    }

    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    private static char TradeKey(Game game, TradeGood good, string arg) =>
        (char)('1' + game.TradeOffers.ToList().FindIndex(o => o.Good == good && o.Arg == arg));

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k', (0, 1) => 'j', (-1, 0) => 'h', (1, 0) => 'l',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
