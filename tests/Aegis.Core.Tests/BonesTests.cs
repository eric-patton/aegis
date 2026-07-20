using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Knucklebones (D-108): town life's first activity, the wagered cast at the
/// skald's hearth. The stake goes down before the first cast, the one real
/// decision is the throw back, the skald plays its odds plainly (stands at
/// its line, sweeps up anything under), and the world keeps a net ledger the
/// stead talks about when it runs steep. Seeds are pinned (probed once,
/// stable): seed 1 loses to a standing skald, seed 6 wins after the skald
/// rethrows, seed 11 ties, seed 9 wins its first three games and loses the
/// fourth.
/// </summary>
public class BonesTests
{
    [Fact]
    public void TheBoard_AsksTheStake_First()
    {
        var game = new Game(1);
        game.Player.Coin = Knucklebones.Stake - 1;
        NpcTests.BumpNpc(game, game.World.Skald);

        game.ApplyKey(BonesDigit(game));

        Assert.False(game.InBonesMenu);
        Assert.True(game.InTalkMenu);
        Assert.Equal(Knucklebones.Stake - 1, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("do not roll on promises"));
    }

    [Fact]
    public void TheStake_GoesDown_AndTheBonesLieFaceUp()
    {
        var game = AtTheBoard(1);
        int turn = game.Turn;

        game.ApplyKey(BonesDigit(game));

        Assert.True(game.InBonesMenu);
        Assert.False(game.InTalkMenu);
        Assert.Equal(20 - Knucklebones.Stake, game.Player.Coin);
        Assert.Equal(3, game.BonesCast.Count);
        Assert.All(game.BonesCast, b => Assert.InRange(b, 1, 6));
        Assert.Equal(turn, game.Turn); // an evening's texture, not a clock
        Assert.True(game.TakeSnapshot().InBonesMenu);
    }

    [Fact]
    public void HighBoard_TakesThePot()
    {
        var game = AtTheBoard(6);
        game.ApplyKey(BonesDigit(game));

        game.ApplyKey('1'); // stand

        Assert.False(game.InBonesMenu);
        Assert.True(game.InTalkMenu); // the hearth stays warm
        Assert.Equal(20 + Knucklebones.Stake, game.Player.Coin);
        Assert.Equal(Knucklebones.Stake, game.BonesNet);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("High board"));
    }

    [Fact]
    public void LowBoard_FeedsTheSkald()
    {
        var game = AtTheBoard(1);
        game.ApplyKey(BonesDigit(game));

        game.ApplyKey('1');

        Assert.Equal(20 - Knucklebones.Stake, game.Player.Coin);
        Assert.Equal(-Knucklebones.Stake, game.BonesNet);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("Low board"));
    }

    [Fact]
    public void EvenBoards_ReturnTheStakes()
    {
        var game = AtTheBoard(11);
        game.ApplyKey(BonesDigit(game));

        game.ApplyKey('1');

        Assert.Equal(20, game.Player.Coin);
        Assert.Equal(0, game.BonesNet);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("Even boards"));
    }

    [Fact]
    public void TheThrowBack_IsOnce_AndTheSecondCastLies()
    {
        var game = AtTheBoard(1);
        game.ApplyKey(BonesDigit(game));
        var first = game.BonesCast.ToList();

        game.ApplyKey('2');

        Assert.True(game.InBonesMenu); // the rethrow does not resolve
        Assert.True(game.BonesRethrown);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("throw again"));

        game.ApplyKey('2'); // the throw is spent: this stands the board instead

        Assert.False(game.InBonesMenu);
        Assert.True(game.InTalkMenu);
        _ = first; // the first cast was swept; only the second was judged
    }

    [Fact]
    public void TheSkald_PlaysItsOddsPlainly()
    {
        // Seed 6: the skald's first cast falls under its line and is swept up.
        var game = AtTheBoard(6);
        game.ApplyKey(BonesDigit(game));
        game.ApplyKey('1');
        Assert.Contains(game.Log.Recent(5), e => e.Text.Contains("Not those"));

        // Seed 1: the first cast makes its line, and the skald stands on it.
        var stands = AtTheBoard(1);
        stands.ApplyKey(BonesDigit(stands));
        stands.ApplyKey('1');
        Assert.DoesNotContain(stands.Log.Recent(5), e => e.Text.Contains("Not those"));
    }

    [Fact]
    public void ALuckyHand_GetsTalkedAbout_WhileTheStreakStands()
    {
        // Seed 9 wins its first three games: net +9, the stead starts talking.
        var game = AtTheBoard(9);
        for (int i = 0; i < 3; i++)
        {
            game.ApplyKey(BonesDigit(game));
            game.ApplyKey('1');
        }
        Assert.Equal(3 * Knucklebones.Stake, game.BonesNet);
        Assert.True(game.World.Facts.Exists("game", "lucky_hand"));
        game.ApplyKey(' '); // leave the hearth

        // The first talk is the first meeting's; the luck is the second's.
        NpcTests.BumpNpc(game, FirstVillager(game));
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, FirstVillager(game));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("home in your shirt"));
    }

    [Fact]
    public void TheStreakGivenBack_EndsTheTalk_ButNotTheHistory()
    {
        // Seed 9's fourth game loses: the net falls below the line, the talk
        // stops, and the fact stays what it always was, history.
        var game = AtTheBoard(9);
        for (int i = 0; i < 4; i++)
        {
            game.ApplyKey(BonesDigit(game));
            game.ApplyKey('1');
        }
        Assert.Equal(2 * Knucklebones.Stake, game.BonesNet);
        Assert.True(game.World.Facts.Exists("game", "lucky_hand"));
        game.ApplyKey(' ');

        NpcTests.BumpNpc(game, FirstVillager(game));
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, FirstVillager(game));
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("home in your shirt"));
    }

    [Fact]
    public void TheCrossing_WipesTheBoard()
    {
        var game = AtTheBoard(6);
        game.ApplyKey(BonesDigit(game));
        game.ApplyKey('1');
        Assert.Equal(Knucklebones.Stake, game.BonesNet);
        game.ApplyKey(' ');

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.Equal(0, game.BonesNet); // the next hearth has seen no luck of any color
    }

    /// <summary>Coin squared away and the skald bumped: the talk menu open at the hearth.</summary>
    private static Game AtTheBoard(ulong seed)
    {
        var game = new Game(seed);
        game.Player.Coin = 20;
        NpcTests.BumpNpc(game, game.World.Skald);
        Assert.True(game.InTalkMenu);
        return game;
    }

    private static char BonesDigit(Game game) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Bones));

    private static Npc FirstVillager(Game game) =>
        game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
}
