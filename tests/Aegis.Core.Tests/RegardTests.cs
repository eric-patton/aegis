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

    // ---- D-080: the friend's price, the boon that keeps on giving ----

    [Fact]
    public void TheFriendsPrice_TakesACoinOffBread()
    {
        var game = new Game(42);
        int priceBefore = game.RationPrice;

        game.Debug_ClearCamp(); // regard 3: the stead holds the bearer a friend

        Assert.Equal(priceBefore - 1, game.RationPrice);
    }

    [Fact]
    public void TheFriendsPrice_IsNotGivenBelowTheFriendRung()
    {
        // A world without the blight story, so the barrow moves nothing but
        // regard (in a blighted world the barrow completes the story and the
        // base price itself eases, a different mechanism than the one on trial).
        bool tested = false;
        for (ulong seed = 1; seed <= 20 && !tested; seed++)
        {
            var game = CrossTo(seed, 2);
            if (game.World.BarrowSite is null) continue;
            if (game.World.Facts.Exists("story", CreepingBlightTemplate.Id)) continue;

            int priceBefore = game.RationPrice;
            game.Debug_ClearSite(SiteKind.Barrow); // rung 1: a known face, not yet a friend
            Assert.Equal(1, SteadRegard.RungFor(game.Regard));
            Assert.Equal(priceBefore, game.RationPrice);
            tested = true;
        }
        Assert.True(tested, "no unblighted tier-2 barrow found in seeds 1..20");
    }

    [Fact]
    public void TheFriendsPrice_IsNotSilencedByTheHushedName()
    {
        // The hearth-price (D-048) rides the bearer's name and the hushed name
        // silences it; the friend's price is bought by a deed the folk watched,
        // so it stands, the same line D-077's welcome drew.
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('7'); // swear the hushed name
        game.ApplyKey('>');
        Assert.Contains(OathId.HushedName, game.World.Oaths);

        int priceBefore = game.RationPrice;
        game.Debug_ClearCamp();
        Assert.Equal(priceBefore - 1, game.RationPrice);
    }

    [Fact]
    public void TheFriendsPrice_IsNamedInTheOffer_AndAloudOnce()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Player.Coin = 10;

        var steadholder = game.World.Npcs.First(n => n.Id == "npc_steadholder");
        NpcTests.BumpNpc(game, steadholder);
        var offer = game.Offers.First(o => o.Good == TradeGood.Ration);
        Assert.Contains("a friend's price", offer.Label);

        char buy = (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Ration));
        game.ApplyKey(buy);
        Assert.Equal(1, game.Player.Rations);
        game.ApplyKey(buy);
        Assert.Equal(2, game.Player.Rations);

        // The steadholder names the coin off exactly once per stead.
        Assert.Single(game.Log.Entries, e => e.Text.Contains("does not forget whose hand"));
    }

    // ---- D-085: the rungs written to the graph, and the rumor they open ----

    [Fact]
    public void TheRungs_AreWrittenToTheGraph_AndDieWithTheWorld()
    {
        var game = new Game(42);
        Assert.False(game.World.Facts.Exists("regard", "known"));

        game.Debug_ClearCamp(); // 0 -> 3: crosses two rungs in one stroke

        Assert.True(game.World.Facts.Exists("regard", "known"));  // every rung passed is written
        Assert.True(game.World.Facts.Exists("regard", "friend"));
        Assert.False(game.World.Facts.Exists("regard", "own"));

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        // World facts die with the world: the next stead has heard nothing.
        Assert.False(game.World.Facts.Exists("regard", "known"));
        Assert.False(game.World.Facts.Exists("regard", "friend"));
    }

    [Fact]
    public void TheHearthtale_IsToldToAFriend_OncePerWorld()
    {
        var game = new Game(42);
        game.Debug_ClearCamp(); // the friend rung: the regard fact stands

        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        NpcTests.BumpNpc(game, villagers[0]);
        Assert.Contains(game.Log.Recent(10), e => e.Text.Contains("inside its own fence"));
        Assert.True(game.World.Facts.Exists("rumor", "stead_hearthtale"));

        // Told once per world: a second villager does not tell it again.
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, villagers[1]);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("inside its own fence"));
    }

    [Fact]
    public void TheHearthtale_IsKeptFromStrangers()
    {
        var game = new Game(42); // camp uncleared: no regard, no fact, no telling
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        NpcTests.BumpNpc(game, villager);

        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("inside its own fence"));
        Assert.False(game.World.Facts.Exists("rumor", "stead_hearthtale"));
    }

    // ---- D-087: the stead's teaching, the own rung's boon ----

    [Fact]
    public void TheSteadsTeaching_OpensAtTheOwnRung_AndTheShowingsAreFree()
    {
        var game = CrossTo(42, 2);
        game.Debug_ClearSite(SiteKind.Barrow); // +2: a known face
        game.Debug_ClearCamp();                // +3 -> 5: friend and own crossed in one stroke

        Assert.Equal(SteadRegard.MaxRung, SteadRegard.RungFor(game.Regard));
        Assert.True(game.World.Facts.Exists("regard", "own")); // the rung is in the graph (D-085)
        Assert.Contains(game.Log.Recent(15), e => e.Text.Contains("yours for the asking now"));

        // The bench names the boon, and the showing takes no coin.
        game.Player.Coin = 0;
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Lesson
            && o.Label.Contains("freely, to the stead's own"));
        game.ApplyKey(TradeKey(game, TradeGood.Lesson));
        Assert.True(game.Player.HasLesson(LessonId.Gleaning));
        Assert.Equal(0, game.Player.Coin);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("Not from you"));
    }

    [Fact]
    public void TheTeaching_IsStillSold_BelowTheOwnRung()
    {
        var game = new Game(42);
        game.Debug_ClearCamp(); // regard 3: a friend, not yet the stead's own

        game.Player.Coin = 0;
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Lesson && o.Label.Contains("(10 coin)"));
        game.ApplyKey(TradeKey(game, TradeGood.Lesson));
        Assert.False(game.Player.HasLesson(LessonId.Gleaning));
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("Knowing has a price"));
    }

    [Fact]
    public void TheTeaching_IsWithheldFromTheUnwelcome_AndRestitutionReopensIt()
    {
        var game = CrossTo(42, 2);
        game.Player.Rations = 0; // room in the pack: a full pack turns the thieving hand away
        ShameTests.RobDoors(game, 2); // unwelcome here, before the deeds land
        game.Debug_ClearSite(SiteKind.Barrow);
        game.Debug_ClearCamp(); // regard 5: the own rung crossed under suspicion

        // The opening is withheld, and the withholding is narrated (D-023's rule).
        Assert.Contains(game.Log.Recent(15), e => e.Text.Contains("It is not said to you"));
        game.Player.Coin = 12;
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Lesson && o.Label.Contains("(10 coin)"));
        game.ApplyKey(' '); // step back from the bench

        // Restitution is the designed exit (D-086): the sills paid, the craft opens.
        RepayAllDoors(game);
        Assert.Equal(0, game.Shame);
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Lesson
            && o.Label.Contains("freely, to the stead's own"));
    }

    [Fact]
    public void TheTeaching_TakesStock_WhenThereIsNothingLeftToShow()
    {
        var game = CrossTo(42, 2);
        game.Debug_ClearSite(SiteKind.Barrow);
        game.Player.Coin = 30;
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        game.ApplyKey(TradeKey(game, TradeGood.Lesson)); // the gleaning, bought
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, game.World.Smith);
        game.ApplyKey(OfferKey(game, TradeGood.Lesson)); // the tended iron, bought
        Assert.True(game.Player.HasLesson(LessonId.TendedIron));

        game.Debug_ClearCamp(); // the own rung, with every showing already paid for
        Assert.Contains(game.Log.Recent(15), e => e.Text.Contains("little left they could show you"));
    }

    // ---- D-088: the facts answered: the tale carried, and the stead's keeping ----

    [Fact]
    public void TheTaleCarried_FollowsTheTelling_OnTheLane()
    {
        var game = new Game(42);
        game.Debug_ClearCamp(); // the friend rung
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager));
        Assert.True(game.World.Facts.Exists("rumor", "stead_hearthtale"));
        game.ApplyKey(' ');

        ShameTests.StepStillNearAHouse(game);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("tarred door-posts"));

        // Once per world: the lane does not repeat itself.
        ShameTests.StepStillNearAHouse(game);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("tarred door-posts"));
    }

    [Fact]
    public void TheSteadsKeeping_IsShownToTheSteadsOwn()
    {
        var game = CrossTo(42, 2);
        game.Debug_ClearSite(SiteKind.Barrow);
        game.Debug_ClearCamp(); // regard 5: the stead's own

        // Higher-priority talk beats (the hearthtale, the known face) take their
        // turns first; the showing follows within a few doors.
        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        for (int i = 0; i < 4 && !game.World.Facts.Exists("secret", "stead_cellar"); i++)
        {
            NpcTests.BumpNpc(game, villagers[i % villagers.Count]);
            game.ApplyKey(' ');
        }

        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the whole of the showing"));
        Assert.True(game.World.Facts.Exists("secret", "stead_cellar"));
    }

    [Fact]
    public void TheSteadsKeeping_IsNotShownBelowTheOwnRung()
    {
        var game = new Game(42);
        game.Debug_ClearCamp(); // a friend, not the stead's own

        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        NpcTests.BumpNpc(game, villagers[0]); // the hearthtale takes the first turn
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, villagers[1]);

        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("the whole of the showing"));
        Assert.False(game.World.Facts.Exists("secret", "stead_cellar"));
    }

    [Fact]
    public void TheSteadsKeeping_IsNotShownToWatchedHands()
    {
        var game = CrossTo(42, 2);
        game.Player.Rations = 0; // room in the pack: a full pack turns the thieving hand away
        ShameTests.RobDoors(game, 1); // watched: the door stays unmarked
        game.Debug_ClearSite(SiteKind.Barrow);
        game.Debug_ClearCamp(); // the own rung crossed under suspicion

        for (int i = 0; i < 3; i++)
        {
            NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager));
            game.ApplyKey(' ');
        }

        Assert.False(game.World.Facts.Exists("secret", "stead_cellar"));
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("the whole of the showing"));
    }

    /// <summary>Pays every robbed sill: for each unrepaid door, an angle beside it, and the grab that leaves the coin.</summary>
    private static void RepayAllDoors(Game game)
    {
        var map = game.World.Overworld;
        while (game.World.PilferedHouses.Any(h => !game.World.RepaidHouses.Contains(h)))
        {
            var door = game.World.PilferedHouses.First(h => !game.World.RepaidHouses.Contains(h));
            var spot = Directions.All8.Select(d => door.Plus(d.dx, d.dy))
                .First(p => map.InBounds(p) && map.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
            game.Debug_SetPlayerPos(spot);
            int before = game.Shame;
            game.Apply(Command.Grab);
            Assert.Equal(before - 1, game.Shame);
        }
    }

    /// <summary>The digit that selects a good in the open talk menu.</summary>
    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    /// <summary>The digit that selects an entry at an open vendor's trade bench (D-071).</summary>
    private static char TradeKey(Game game, TradeGood good) =>
        (char)('1' + game.TradeOffers.ToList().FindIndex(o => o.Good == good));

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
