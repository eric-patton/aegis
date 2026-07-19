using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The stead's regard (D-076, the local-reputation foundation of the faction
/// pillar D-023): a per-world Fame earned only by deeds the folk can perceive,
/// surfaced the moment it lands, reset at every crossing, and set beside, never
/// merged with, the meta-Legend standing that carries between worlds.
/// </summary>
public class RegardTests
{
    [Fact]
    public void TheLadder_ClimbsOnAPlainStep()
    {
        Assert.Equal(1, SteadRegard.Threshold(1));
        Assert.Equal(3, SteadRegard.Threshold(2));
        Assert.Equal(5, SteadRegard.Threshold(3));

        Assert.Equal(0, SteadRegard.RungFor(0));
        Assert.Equal(1, SteadRegard.RungFor(1));
        Assert.Equal(1, SteadRegard.RungFor(2));
        Assert.Equal(2, SteadRegard.RungFor(3));
        Assert.Equal(2, SteadRegard.RungFor(4));
        Assert.Equal(3, SteadRegard.RungFor(5));
        Assert.Equal(3, SteadRegard.RungFor(100)); // the cap holds

        Assert.Equal("", SteadRegard.TitleOf(0));
        Assert.Equal("a known face here", SteadRegard.TitleOf(1));
        Assert.Equal("a friend to the stead", SteadRegard.TitleOf(3));
        Assert.Equal("the stead's own", SteadRegard.TitleOf(SteadRegard.Threshold(SteadRegard.MaxRung)));
    }

    [Fact]
    public void TheFirstWorld_HoldsNoRegardYet()
    {
        var game = new Game(42);
        Assert.Equal(0, game.Regard);
        Assert.False(game.Player.RegardLineHeard);
    }

    [Fact]
    public void ClearingTheCamp_RaisesRegard_Perceivably_AndTheAegisSpeaksOnce()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();

        Assert.Equal(3, game.Regard); // the raids ended: the stead's central grievance
        var log = game.Log.Recent(12).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("Word of the ended raids"));       // the change is perceived
        Assert.Contains(log, t => t.Contains("you are a friend to the stead now")); // the crossed rung is named
        Assert.Contains(log, t => t.Contains("A nearer weighing than mine"));    // the once-only Aegis aside
        Assert.True(game.Player.RegardLineHeard);
    }

    [Fact]
    public void TheSnapshot_CarriesRegard_AndTitle()
    {
        var game = new Game(42);
        var bare = game.TakeSnapshot();
        Assert.Equal(0, bare.Regard);
        Assert.Equal("", bare.RegardTitle);

        game.Debug_ClearCamp();
        var snap = game.TakeSnapshot();
        Assert.Equal(3, snap.Regard);
        Assert.Equal("a friend to the stead", snap.RegardTitle);
    }

    [Fact]
    public void Regard_ResetsAtEachCrossing()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        Assert.Equal(3, game.Regard);

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // The folk are this world's alone: the far gate leaves the regard behind
        // with them, and the next stead starts the bearer at a stranger again.
        Assert.Equal(0, game.Regard);
    }

    [Fact]
    public void ClearingTheBarrow_RaisesRegard_ButTheAegisAsideIsSpent()
    {
        var game = CrossTo(42, 2);
        Assert.NotNull(game.World.BarrowSite);
        Assert.Equal(0, game.Regard); // world 1's camp regard reset at the crossing

        game.Debug_ClearSite(SiteKind.Barrow);
        Assert.Equal(2, game.Regard);
        var log = game.Log.Recent(12).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("The lights on the mound above"));
        Assert.Contains(log, t => t.Contains("a known face here")); // rung 0 -> 1
        // The aside was spent clearing the camp back in world 1; it does not return.
        Assert.DoesNotContain(log, t => t.Contains("A nearer weighing than mine"));
    }

    [Fact]
    public void RemoteDeeds_TheSteadCannotPerceive_PassNoRegard()
    {
        // The perceivability rule (D-023): a quarry hushed leagues off is a real
        // deed, counted in Legend and written to the fact graph, but the stead
        // never feels it, so it moves no regard.
        bool tested = false;
        for (ulong seed = 1; seed <= 20 && !tested; seed++)
        {
            var game = CrossTo(seed, 3); // tier 3: the quarry's band
            if (game.World.QuarrySite is null) continue;

            Assert.Equal(0, game.Regard);
            game.Debug_ClearSite(SiteKind.Quarry);
            Assert.True(game.World.Facts.Exists("deed", "quarry_hushed")); // the deed happened
            Assert.Equal(0, game.Regard);                                  // the stead could not see it
            tested = true;
        }
        Assert.True(tested, "no tier-3 quarry found in seeds 1..20");
    }

    [Fact]
    public void TheRegard_ReachesAheadOfTheBearer_InGreetings()
    {
        var game = new Game(42);
        game.Debug_ClearCamp(); // regard 3 -> a friend to the stead

        string? plaintiff = game.World.Facts.Find("role", "plaintiff")?.Object;
        var npc = game.World.Npcs.First(n =>
            n.Kind == NpcKind.Villager && n.Id != plaintiff && !game.World.Facts.Exists("met", n.Id));
        NpcTests.BumpNpc(game, npc);

        // Even a villager the bearer has never met greets them as no stranger.
        Assert.Contains(game.Log.Recent(10), e => e.Text.Contains("No stranger to this stead"));
    }

    [Fact]
    public void AStranger_IsStillAStranger_WithNoRegard()
    {
        var game = new Game(42); // camp uncleared: regard 0
        string? plaintiff = game.World.Facts.Find("role", "plaintiff")?.Object;
        var npc = game.World.Npcs.First(n =>
            n.Kind == NpcKind.Villager && n.Id != plaintiff && !game.World.Facts.Exists("met", n.Id));
        NpcTests.BumpNpc(game, npc);

        Assert.Contains(game.Log.Recent(10), e => e.Text.Contains("A stranger, then"));
    }

    // ---- D-077: the friend's welcome, regard's first boon ----

    [Fact]
    public void TheFriendsWelcome_LandsWhenTheSteadFirstHoldsYouAFriend()
    {
        var game = new Game(42);
        Assert.Equal(0, game.Player.Coin);

        game.Debug_ClearCamp(); // regard 0 -> 3: crosses into the friend rung

        Assert.Equal(5, game.Player.Coin); // the stead's pooled purse
        Assert.Contains(game.Log.Recent(12), e => e.Text.Contains("gather what coin they can spare"));
    }

    [Fact]
    public void TheFriendsWelcome_ComesOncePerStead()
    {
        // Tier 2: the barrow raises regard to rung 1 (no welcome yet), and the camp
        // then crosses into the friend rung, so the gift lands exactly once even
        // though two deeds moved the regard.
        var game = CrossTo(42, 2);
        Assert.Equal(0, game.Player.Coin); // world 1's gift converted to Legend at the crossing

        game.Debug_ClearSite(SiteKind.Barrow); // +2 -> rung 1, no welcome
        Assert.Equal(0, game.Player.Coin);

        game.Debug_ClearCamp(); // +3 -> rung 3, crosses the friend rung: welcome once
        Assert.Equal(5, game.Player.Coin); // 5, not 10: it did not fire again on the barrow's rung-1 step
        Assert.Single(game.Log.Recent(15), e => e.Text.Contains("gather what coin they can spare")); // this world's only
    }

    [Fact]
    public void TheFriendsWelcome_DoesNotComeBelowTheFriendRung()
    {
        // Rung 1 (a known face) is not yet a friend, so no purse: at tier 2 the
        // barrow alone lifts regard to rung 1 and nothing is given.
        var game = CrossTo(42, 2);
        game.Debug_ClearSite(SiteKind.Barrow);
        Assert.Equal(1, SteadRegard.RungFor(game.Regard));
        Assert.Equal(0, game.Player.Coin);
        Assert.DoesNotContain(game.Log.Recent(12), e => e.Text.Contains("gather what coin they can spare"));
    }

    [Fact]
    public void TheFriendsWelcome_IsNotSilencedByTheHushedName()
    {
        // The hushed name silences the songs and every Legend favor (D-051), but the
        // stead's own thanks is earned by a deed they watched, not carried by a name,
        // so it stands where the arrival-welcome would not.
        var game = new Game(42);
        game.Debug_ClearCamp(); // world 1's own raids ended (its welcome fires; we cross past it)
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('7'); // swear the hushed name
        game.ApplyKey('>');
        Assert.Contains(OathId.HushedName, game.World.Oaths);

        int coinBefore = game.Player.Coin; // 0: world 1's gift converted to Legend at the crossing
        game.Debug_ClearCamp(); // this hushed world's raids ended: the friend's welcome still comes
        Assert.Equal(coinBefore + 5, game.Player.Coin);
        Assert.Contains(game.Log.Recent(12), e => e.Text.Contains("gather what coin they can spare"));
    }

    /// <summary>Crosses from a fresh seed to the given cycle, clearing each world's camp to open the gate.</summary>
    private static Game CrossTo(ulong seed, int targetCycle)
    {
        var game = new Game(seed);
        while (game.Cycle < targetCycle)
        {
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.Apply(Command.Enter);
            game.Apply(Command.Enter);
        }
        return game;
    }
}
