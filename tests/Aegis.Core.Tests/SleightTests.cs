using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Pickpocketing (D-107): the crime family's second verb and its first skill.
/// 'p' beside one of the stead's folk brushes their purse: clean lifts pay
/// coin and feed Sleight and write the stead's first secret fact; caught
/// lifts climb the same unified shame ladder pilfering climbs (D-086), with
/// restitution in the wronged hand as the designed exit. One try per pocket
/// per world. Seeds are pinned: on seed 1 the first lift comes away clean,
/// on seed 4 it is caught (deterministic, probed once, stable).
/// </summary>
public class SleightTests
{
    [Fact]
    public void TheOdds_RideTheSkill()
    {
        Assert.Equal(0.5, Lifting.ChanceFor(0));
        Assert.Equal(0.7, Lifting.ChanceFor(4), 3);
        Assert.Equal(0.85, Lifting.ChanceFor(20)); // no hand is ever safe
    }

    [Fact]
    public void TheCleanLift_TakesCoin_AndTeachesTheHand()
    {
        var game = new Game(1);
        var mark = FirstVillager(game);
        game.Debug_SetPlayerPos(mark.Pos.Plus(1, 0));
        int coin = game.Player.Coin;
        int turn = game.Turn;

        game.Apply(Command.Lift);

        Assert.Equal(coin + 2, game.Player.Coin);
        Assert.Equal(turn + 1, game.Turn); // a brush costs the turn it takes
        Assert.Contains(mark.Id, game.World.LiftedNpcs);
        Assert.Empty(game.World.CaughtLifts);
        Assert.Equal(0, game.Shame);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Sleight));
        Assert.True(game.World.Facts.Exists("secret", "lifted_purse"));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("No eye follows you away"));
        Assert.Contains("sleight:0:1", game.TakeSnapshot().Skills);
    }

    [Fact]
    public void TheCaughtLift_ClimbsTheSameLadder()
    {
        var game = new Game(4);
        var mark = FirstVillager(game);
        game.Debug_SetPlayerPos(mark.Pos.Plus(1, 0));
        int coin = game.Player.Coin;

        game.Apply(Command.Lift);

        Assert.Equal(coin, game.Player.Coin); // nothing taken, everything noticed
        Assert.Contains(mark.Id, game.World.CaughtLifts);
        Assert.Equal(1, game.Shame); // the unified ladder (D-086), not a second book
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Sleight)); // only work that worked counts
        Assert.True(game.World.Facts.Exists("shame", "confronted"));
        var log = game.Log.Recent(6).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("finds your wrist"));
        Assert.Contains(log, t => t.Contains("made right in that hand"));
        Assert.Contains(log, t => t.Contains("watched in this stead"));
    }

    [Fact]
    public void OnePocket_TellsAllItIsGoingTo()
    {
        var game = new Game(1);
        var mark = FirstVillager(game);
        game.Debug_SetPlayerPos(mark.Pos.Plus(1, 0));
        game.Apply(Command.Lift);
        int coin = game.Player.Coin;

        game.Apply(Command.Lift);

        Assert.Equal(coin, game.Player.Coin);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Sleight));
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("told you all it is going to"));
    }

    [Fact]
    public void TheRepay_WalksTheLadderDown_InTheSameHand()
    {
        var game = new Game(4);
        var mark = FirstVillager(game);
        game.Debug_SetPlayerPos(mark.Pos.Plus(1, 0));
        game.Apply(Command.Lift);
        Assert.Equal(1, game.Shame);
        game.Player.Coin = 10;

        game.Apply(Command.Lift); // the same key that did the wrong

        Assert.Equal(4, game.Player.Coin);
        Assert.Equal(0, game.Shame);
        Assert.Contains(mark.Id, game.World.RepaidLifts);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("count it, twice"));

        // A repaid pocket is closed both ways: no third act at this hip.
        game.Apply(Command.Lift);
        Assert.Equal(4, game.Player.Coin);
        Assert.Equal(0, game.Shame);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("told you all it is going to"));
    }

    [Fact]
    public void ShortCoin_LeavesTheWrongStanding()
    {
        var game = new Game(4);
        var mark = FirstVillager(game);
        game.Debug_SetPlayerPos(mark.Pos.Plus(1, 0));
        game.Apply(Command.Lift);
        game.Player.Coin = SteadShame.RepayCoin - 1;

        game.Apply(Command.Lift);

        Assert.Equal(SteadShame.RepayCoin - 1, game.Player.Coin);
        Assert.Equal(1, game.Shame);
        Assert.DoesNotContain(mark.Id, game.World.RepaidLifts);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("The wrong keeps"));
    }

    [Fact]
    public void TheLift_NeedsAMark_AndCostsNoTurnWithout()
    {
        var game = new Game(1);
        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, 3));
        int turn = game.Turn;

        game.Apply(Command.Lift);

        Assert.Equal(turn, game.Turn);
        Assert.Empty(game.World.LiftedNpcs);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("No one stands near enough"));
    }

    [Fact]
    public void TheDeepPlaces_KeepNoPockets()
    {
        var game = new Game(1);
        game.Debug_SetMode(MapMode.Site);

        game.Apply(Command.Lift);

        Assert.Empty(game.World.LiftedNpcs);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("No pockets down here"));
    }

    [Fact]
    public void TheCrossing_RegeneratesThePockets_AndDropsTheShame()
    {
        var game = new Game(4);
        var mark = FirstVillager(game);
        game.Debug_SetPlayerPos(mark.Pos.Plus(1, 0));
        game.Apply(Command.Lift);
        Assert.Equal(1, game.Shame);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.Empty(game.World.LiftedNpcs);
        Assert.Empty(game.World.CaughtLifts);
        Assert.Equal(0, game.Shame); // the folk of the next world have met no thief
    }

    private static Npc FirstVillager(Game game) =>
        game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
}
