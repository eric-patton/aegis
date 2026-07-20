using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The standing round and the light purse read (D-123): town life's second
/// verb and the loss ledger's consumer. Standing the room a round at the
/// skald's hearth costs coin, warms the lane, and writes a fact; once per
/// world, no rung and no ledger, because a coin-for-regard dial is exactly
/// what D-108 set carousing aside to avoid. The light_purse fact D-108
/// wrote gets its reader, gated on the live net like the luck's talk.
/// Seeds are pinned (probed once, stable): seed 15 loses its first three
/// games and wins the fourth.
/// </summary>
public class CarouseTests
{
    [Fact]
    public void TheRound_WarmsTheRoom_AndWritesTheFact()
    {
        var game = AtTheHearth(1);
        int turn = game.Turn;

        game.ApplyKey(RoundDigit(game));

        Assert.True(game.RoundStood);
        Assert.Equal(40 - Carousing.Price, game.Player.Coin);
        Assert.Equal(turn, game.Turn); // an evening's texture, not a clock
        Assert.True(game.InTalkMenu);  // the hearth stays warm
        Assert.True(game.World.Facts.Exists("game", "round_stood"));
        var log = game.Log.Recent(4).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("horns go down the benches"));
        Assert.Contains(log, t => t.Contains("remembered here longer than the ale"));
    }

    [Fact]
    public void TheSecondRound_IsCampaigning()
    {
        var game = AtTheHearth(1);
        game.ApplyKey(RoundDigit(game));
        int coin = game.Player.Coin;
        Assert.Contains(game.Offers, o => o.Label.Contains("drank your health tonight"));

        game.ApplyKey(RoundDigit(game));

        Assert.Equal(coin, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("two is campaigning"));
    }

    [Fact]
    public void ShortCoin_PoursNothing()
    {
        var game = AtTheHearth(1);
        game.Player.Coin = Carousing.Price - 1;

        game.ApplyKey(RoundDigit(game));

        Assert.False(game.RoundStood);
        Assert.Equal(Carousing.Price - 1, game.Player.Coin);
        Assert.False(game.World.Facts.Exists("game", "round_stood"));
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("pours on coin"));
    }

    [Fact]
    public void TheRound_OnALightPurse_GetsItsOwnToast()
    {
        var game = AtTheHearth(15);
        LoseGames(game, 3);
        Assert.True(game.World.Facts.Exists("game", "light_purse"));

        game.ApplyKey(RoundDigit(game));

        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("character or stubbornness"));
    }

    [Fact]
    public void TheRoundRemembered_OnTheLane()
    {
        var game = AtTheHearth(1);
        game.ApplyKey(RoundDigit(game));
        game.ApplyKey(' '); // leave the hearth

        BumpUntil(game, "remembers who poured");
    }

    [Fact]
    public void TheLightPurse_IsRead_WhileItStands()
    {
        var game = AtTheHearth(15);
        LoseGames(game, 3);
        Assert.Equal(-Knucklebones.TalkedAboutAt, game.BonesNet);
        game.ApplyKey(' ');

        BumpUntil(game, "feeding the skald's board");
    }

    [Fact]
    public void TheCoinWonBack_EndsTheReading_ButNotTheHistory()
    {
        var game = AtTheHearth(15);
        LoseGames(game, 4); // the fourth game wins: the net climbs off the line
        Assert.True(game.BonesNet > -Knucklebones.TalkedAboutAt);
        Assert.True(game.World.Facts.Exists("game", "light_purse")); // history keeps
        game.ApplyKey(' ');

        NpcTests.BumpNpc(game, FirstVillager(game));
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, FirstVillager(game));
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("feeding the skald's board"));
    }

    [Fact]
    public void TheCrossing_ForgetsTheEvening()
    {
        var game = AtTheHearth(1);
        game.ApplyKey(RoundDigit(game));
        Assert.True(game.RoundStood);
        game.ApplyKey(' ');

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.False(game.RoundStood); // the next hearth has met no one's generosity
        Assert.False(game.World.Facts.Exists("game", "round_stood"));
        NpcTests.BumpNpc(game, game.World.Skald);
        Assert.Contains(game.Offers, o => o.Label.Contains($"({Carousing.Price} coin)"));
    }

    /// <summary>Coin squared away and the skald bumped: the talk menu open at the hearth.</summary>
    private static Game AtTheHearth(ulong seed)
    {
        var game = new Game(seed);
        game.Player.Coin = 40;
        NpcTests.BumpNpc(game, game.World.Skald);
        Assert.True(game.InTalkMenu);
        return game;
    }

    /// <summary>Play n games standing on the first cast, whatever the bones say.</summary>
    private static void LoseGames(Game game, int n)
    {
        for (int i = 0; i < n; i++)
        {
            game.ApplyKey((char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Bones)));
            game.ApplyKey('1');
        }
    }

    /// <summary>Talk to the lane until it says the thing, because the first talk is always the first meeting's.</summary>
    private static void BumpUntil(Game game, string marker)
    {
        for (int i = 0; i < 8; i++)
        {
            NpcTests.BumpNpc(game, FirstVillager(game));
            if (game.Log.Recent(4).Any(e => e.Text.Contains(marker))) return;
            game.ApplyKey(' ');
        }
        Assert.Fail($"the lane never said: {marker}");
    }

    private static char RoundDigit(Game game) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Round));

    private static Npc FirstVillager(Game game) =>
        game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
}
