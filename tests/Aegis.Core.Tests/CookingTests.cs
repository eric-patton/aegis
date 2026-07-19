using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Cooking (D-073), the first craft: raw meat off the hunt (D-070) becomes carried
/// rations at the woodward's fire (the D-071 bench), the Cooking skill fattening the
/// yield, all bounded by what a body can carry so a full larder wastes no meat.
/// </summary>
public class CookingTests
{
    [Fact]
    public void TheBench_CooksRawMeatIntoRations_AndGrowsCooking()
    {
        var game = new Game(42);
        game.Player.RawMeat = 3;
        game.Player.Rations = 0;
        int cookBefore = game.Player.Skills.Uses(SkillId.Cooking);
        OpenBench(game);

        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Cook
            && o.Label.Contains("3 raw into 3 ration"));
        game.ApplyKey(TradeKey(game, TradeGood.Cook));

        Assert.Equal(0, game.Player.RawMeat);   // the cuts went to the fire
        Assert.Equal(3, game.Player.Rations);   // three cuts, three rations at Cooking 0
        Assert.True(game.Player.Skills.Uses(SkillId.Cooking) > cookBefore, "the fire taught nothing");
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Cook && o.Label.Contains("none to cook"));
    }

    [Fact]
    public void CookingSkill_SqueezesMoreMealsFromTheSameMeat()
    {
        var game = new Game(42);
        while (game.Player.Skills.Level(SkillId.Cooking) < 2) game.Player.Skills.AddUse(SkillId.Cooking);
        Assert.Equal(1, game.Player.Skills.Bonus(SkillId.Cooking));
        game.Player.RawMeat = 2;
        game.Player.Rations = 0;
        OpenBench(game);
        game.ApplyKey(TradeKey(game, TradeGood.Cook));

        Assert.Equal(0, game.Player.RawMeat);
        Assert.Equal(3, game.Player.Rations); // 2 cuts + the Cooking bonus
    }

    [Fact]
    public void AFullLarder_CooksNothing_AndWastesNoMeat()
    {
        var game = new Game(42);
        game.Player.Rations = Game.RationCap;
        game.Player.RawMeat = 4;
        OpenBench(game);
        game.ApplyKey(TradeKey(game, TradeGood.Cook));

        Assert.Equal(Game.RationCap, game.Player.Rations); // no room, no meal
        Assert.Equal(4, game.Player.RawMeat);              // and the meat keeps, raw
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("larder is full"));
    }

    [Fact]
    public void Cooking_FillsToTheCap_AndKeepsTheRestRaw()
    {
        var game = new Game(42);
        game.Player.Rations = Game.RationCap - 2; // room for two
        game.Player.RawMeat = 5;
        OpenBench(game);
        game.ApplyKey(TradeKey(game, TradeGood.Cook));

        Assert.Equal(Game.RationCap, game.Player.Rations); // filled to the cap
        Assert.Equal(3, game.Player.RawMeat);              // only two cuts spent, three keep
    }

    [Fact]
    public void AnEmptyGameBag_CooksNothing()
    {
        var game = new Game(42);
        game.Player.RawMeat = 0;
        OpenBench(game);
        game.ApplyKey(TradeKey(game, TradeGood.Cook));
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("no raw meat"));
    }

    // ---- helpers ----

    private static void OpenBench(Game game)
    {
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.True(game.InTradeMenu);
    }

    private static char OfferKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.Offers.Count; i++)
            if (game.Offers[i].Good == good)
                return (char)('1' + game.Topics.Count + i);
        throw new InvalidOperationException($"no {good} offer");
    }

    private static char TradeKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.TradeOffers.Count; i++)
            if (game.TradeOffers[i].Good == good)
                return (char)('1' + i);
        throw new InvalidOperationException($"no {good} entry at the bench");
    }
}
