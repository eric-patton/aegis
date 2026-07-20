using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The War of Faiths at slice scale (D-116): D-115's two institutions quarreling
/// over the founding both misremember. The template's contract features: a whole
/// cast filled by office (only the straddler and the aggressor side are drawn),
/// two schism accounts planted against each other, a present-tense wrong, evidence
/// at the mother-stone's socket that complicates both books, and a climax at the
/// shrine that branches on whether the truth was in hand when the claim was said.
/// D-118 staged the truth-in-hand climax as a dialogue scene: the socket's telling
/// is the bearer's to say whole, wield for the stead behind a visible check, or keep.
/// </summary>
public class FaithsTests
{
    /// <summary>Master seed whose cycle-2 world selects the War of Faiths (44 tells it direct at tier 2 as well).</summary>
    private const ulong FaithsMaster = 44;

    [Fact]
    public void Casting_FillsTheOffices_AndDrawsOnlyTheStraddlerAndTheWrong()
    {
        var world = WorldGen.Generate(FaithsMaster, tier: 2);
        Assert.Equal("war-of-faiths", world.Facts.OfType("story").Single().Subject);
        Assert.Equal(11, world.StoryStorylets.Count);

        // The champions and the silent keeper hold offices, never lots.
        Assert.Equal(world.Keeper.Id, world.Facts.Find("role", "stead_champion")!.Object);
        Assert.Equal(world.HarrowElder.Id, world.Facts.Find("role", "harrow_champion")!.Object);
        var straddler = world.Npcs.Single(n => n.Id == world.Facts.Find("role", "straddler")!.Object);
        Assert.Equal(NpcKind.Villager, straddler.Kind);

        // Both schism accounts stand, told against each other, and one side has
        // done this season's wrong.
        Assert.Contains("jealousy", world.Facts.Find("history", "schism_stead")!.Detail);
        Assert.Contains("theft dressed in thanks", world.Facts.Find("history", "schism_harrow")!.Detail);
        var aggressor = world.Facts.OfType("aggressor").Single();
        Assert.Contains(aggressor.Subject, (string[])["stead", "harrow"]);
    }

    [Fact]
    public void TheAggressor_IsDrawn_BothSidesOccur()
    {
        var seen = new HashSet<string>();
        for (ulong seed = 1; seed <= 60 && seen.Count < 2; seed++)
        {
            var world = WorldGen.Generate(seed, tier: 2);
            if (world.Facts.OfType("story").Single().Subject != "war-of-faiths") continue;
            seen.Add(world.Facts.OfType("aggressor").Single().Subject);
        }
        Assert.Equal(2, seen.Count);
    }

    [Fact]
    public void TheQuarrel_FromTheStraddler_WritesThePromise()
    {
        var game = CrossedFaithsGame();
        TalkUntil(game, Straddler(game), () => game.World.Facts.Exists("promise", "see_it_settled"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("owes neither book anything"));
    }

    [Fact]
    public void TheAccounts_ComeFromTheChampions_AndAreHeardSeparately()
    {
        var game = CrossedFaithsGame();

        TalkUntil(game, game.World.Keeper, () => game.World.Facts.Exists("heard", "stead_account"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("handed with the broom"));
        Assert.False(game.World.Facts.Exists("heard", "harrow_account"));

        TalkUntil(game, game.World.HarrowElder, () => game.World.Facts.Exists("heard", "harrow_account"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("bring it to me"));
    }

    [Fact]
    public void Evidence_AtTheSocket_ComplicatesBothBooks()
    {
        var game = CrossedFaithsGame();
        ReadTheSocket(game);
        Assert.True(game.World.Facts.Exists("evidence", "founding_truth"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("burying winter"));

        // The complicit authority: the doorward has known the whole office.
        TalkUntil(game, game.World.Npcs.Single(n => n.Id == "npc_harrow_doorward"),
            () => game.Log.Entries.Any(e => e.Text.Contains("You read the socket, then")));
    }

    [Fact]
    public void Climax_WithTheTruth_OpensTheScene_WithTheWieldingChecked()
    {
        var game = HeardBothGame();
        ReadTheSocket(game);
        game.Debug_SetPlayerPos(game.World.ShrinePos);
        game.Apply(Command.Rest);

        // The three-way ending (D-118): say it whole, wield it, or keep it,
        // with the wielding's odds on the table before anything is committed.
        Assert.True(game.InScene);
        Assert.Equal("The claim at dawn", game.SceneTitle);
        Assert.Equal(3, game.SceneChoices.Count);
        Assert.Equal("", game.SceneChoices[0].Tag);
        Assert.Contains("Presence", game.SceneChoices[1].Tag);
        Assert.Contains("in 100", game.SceneChoices[1].Tag);
        Assert.Equal("", game.SceneChoices[2].Tag);
        Assert.False(game.World.Facts.Exists("story_complete", "war-of-faiths"));
    }

    [Fact]
    public void SaidWhole_SharesTheGrief()
    {
        var game = HeardBothGame();
        ReadTheSocket(game);

        SayTheClaim(game, '1');
        Assert.True(game.World.Facts.Exists("story_complete", "war-of-faiths"));
        Assert.True(game.World.Facts.Exists("coda", "one_grief_shared"));
        Assert.False(game.World.Facts.Exists("coda", "claim_shelved"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("rite said at both stones"));
    }

    [Fact]
    public void TheWielding_RollsTheShownOdds_BreaksTheClaimOrIsSeenThrough()
    {
        int broken = 0, seen = 0;
        for (int burn = 0; burn < 10 && (broken == 0 || seen == 0); burn++)
        {
            var game = HeardBothGame();
            TalkUntil(game, Straddler(game), () => game.World.Facts.Exists("promise", "see_it_settled"));
            ReadTheSocket(game);
            for (int i = 0; i < burn; i++) game.Debug_BurnCombatRoll();

            SayTheClaim(game, '2');
            Assert.True(game.World.Facts.Exists("story_complete", "war-of-faiths"));
            if (game.World.Facts.Exists("coda", "claim_broken"))
            {
                broken++;
                Assert.False(game.World.Facts.Exists("coda", "one_grief_shared"));
                Assert.False(game.World.Facts.Exists("coda", "claim_shelved"));
                Assert.Contains(game.Log.Entries, e => e.Text.Contains("crooked course"));

                // The settling still pays: the straddler saw what you did.
                int before = game.Player.Essence;
                TalkUntil(game, Straddler(game),
                    () => game.Log.Entries.Any(e => e.Text.Contains("beaten fire")));
                Assert.Equal(before + 3, game.Player.Essence);
            }
            else
            {
                seen++;
                Assert.True(game.World.Facts.Exists("coda", "claim_shelved"));
                Assert.Contains(game.Log.Entries, e => e.Text.Contains("spent it"));
            }
        }

        // At 40 in 100 both endings must occur across the burned starts.
        Assert.True(broken >= 1, "the wielding never carried");
        Assert.True(seen >= 1, "the wielding was never seen through");
    }

    [Fact]
    public void TheKeeping_ShelvesTheClaim_AndTheGraphRemembersTheSilence()
    {
        var game = HeardBothGame();
        ReadTheSocket(game);

        SayTheClaim(game, '3');
        Assert.True(game.World.Facts.Exists("story_complete", "war-of-faiths"));
        Assert.True(game.World.Facts.Exists("coda", "claim_shelved"));
        Assert.True(game.World.Facts.Exists("withheld", "founding_truth"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("unsaid things"));
    }

    [Fact]
    public void Climax_WithoutTheTruth_ShelvesTheClaim_WithNothingToChoose()
    {
        var game = HeardBothGame();

        RestAtShrine(game);
        Assert.False(game.InScene);
        Assert.True(game.World.Facts.Exists("story_complete", "war-of-faiths"));
        Assert.True(game.World.Facts.Exists("coda", "claim_shelved"));
        Assert.False(game.World.Facts.Exists("coda", "one_grief_shared"));
        Assert.False(game.World.Facts.Exists("withheld", "founding_truth"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("folded away"));
    }

    [Fact]
    public void TheStoryEndsOnce_LateEvidence_DoesNotResayTheClaim()
    {
        var game = HeardBothGame();
        RestAtShrine(game);
        Assert.True(game.World.Facts.Exists("coda", "claim_shelved"));

        ReadTheSocket(game);
        Assert.True(game.World.Facts.Exists("evidence", "founding_truth"));
        RestAtShrine(game);
        Assert.False(game.InScene);
        Assert.False(game.World.Facts.Exists("coda", "one_grief_shared"));
    }

    [Fact]
    public void TheSettling_PaysTheSameCoin_TruthOrShelved_AndNeedsTheAsking()
    {
        // Truth path, asked: the straddler's two houses are one house.
        var truth = HeardBothGame();
        TalkUntil(truth, Straddler(truth), () => truth.World.Facts.Exists("promise", "see_it_settled"));
        ReadTheSocket(truth);
        SayTheClaim(truth, '1');
        int before = truth.Player.Essence;
        TalkUntil(truth, Straddler(truth),
            () => truth.Log.Entries.Any(e => e.Text.Contains("one prayer")));
        Assert.Equal(before + 3, truth.Player.Essence);

        // Shelved path, asked: same coin, colder telling.
        var cold = HeardBothGame();
        TalkUntil(cold, Straddler(cold), () => cold.World.Facts.Exists("promise", "see_it_settled"));
        RestAtShrine(cold);
        before = cold.Player.Essence;
        TalkUntil(cold, Straddler(cold),
            () => cold.Log.Entries.Any(e => e.Text.Contains("lived my whole life under shelved")));
        Assert.Equal(before + 3, cold.Player.Essence);

        // Never asked: the morning was stood, but not for anyone. No settling.
        var unasked = HeardBothGame();
        RestAtShrine(unasked);
        before = unasked.Player.Essence;
        for (int i = 0; i < 6; i++)
        {
            NpcTests.BumpNpc(unasked, Straddler(unasked));
            unasked.ApplyKey(' ');
        }
        Assert.Equal(before, unasked.Player.Essence);
    }

    /// <summary>Crosses the faiths master into its cycle-2 world, which tells the War of Faiths.</summary>
    private static Game CrossedFaithsGame()
    {
        var game = new Game(FaithsMaster);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.Equal("war-of-faiths", game.World.Facts.OfType("story").Single().Subject);
        return game;
    }

    /// <summary>A crossed faiths game that has heard both schism accounts.</summary>
    private static Game HeardBothGame()
    {
        var game = CrossedFaithsGame();
        TalkUntil(game, game.World.Keeper, () => game.World.Facts.Exists("heard", "stead_account"));
        TalkUntil(game, game.World.HarrowElder, () => game.World.Facts.Exists("heard", "harrow_account"));
        return game;
    }

    /// <summary>Walks into the harrow and steps onto the mother-stone, where the socket reads.</summary>
    private static void ReadTheSocket(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.HarrowSite.OverworldPos);
        game.Apply(Command.Enter);
        game.Debug_SetPlayerPos(WorldGen.HarrowStonePos.Plus(-1, 0));
        game.Apply(Command.MoveE);
        game.Debug_SetMode(MapMode.Overworld);
    }

    /// <summary>Rests at the shrine (the Rest hook's only stage) and closes the menu.</summary>
    private static void RestAtShrine(Game game)
    {
        game.Debug_SetPlayerPos(game.World.ShrinePos);
        game.Apply(Command.Rest);
        game.ApplyKey(' ');
    }

    /// <summary>
    /// Rests at the shrine into the claim-at-dawn scene (D-118), gives the
    /// answer, closes the terminal node, and rises from the shrine menu.
    /// </summary>
    private static void SayTheClaim(Game game, char answer)
    {
        game.Debug_SetPlayerPos(game.World.ShrinePos);
        game.Apply(Command.Rest);
        Assert.True(game.InScene, "the claim-at-dawn scene never opened");
        game.ApplyKey(answer);
        Assert.True(game.InScene);
        game.ApplyKey(' ');
        Assert.False(game.InScene);
        game.ApplyKey(' ');
    }

    private static Npc Straddler(Game game)
    {
        string id = game.World.Facts.Find("role", "straddler")!.Object;
        return game.World.Npcs.First(n => n.Id == id);
    }

    /// <summary>Bumps the NPC (talking through the priority queue) until the condition holds.</summary>
    private static void TalkUntil(Game game, Npc npc, Func<bool> done)
    {
        for (int i = 0; i < 8 && !done(); i++)
        {
            NpcTests.BumpNpc(game, npc);
            game.ApplyKey(' ');
        }
        Assert.True(done(), "the expected talk beat never fired");
    }
}
