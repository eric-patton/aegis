using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The Usurped Throne at slice scale (D-112): the dens' seat as the taken throne.
/// The template's contract features: an accepted history that blames the taking
/// outward (which is what keeps the raids righteous), evidence in the camp that
/// complicates rather than inverts, endings that branch on whether the truth came
/// down the hill, and restoration beats that ride D-110's live succession.
/// </summary>
public class ThroneTests
{
    /// <summary>Master seed whose cycle-2 world selects the Usurped Throne.</summary>
    private const ulong ThroneMaster = 40;

    [Fact]
    public void Casting_NamesTheOldChief_AndTheClaimantLieutenant()
    {
        // Tier-2 seed 7 tells the throne directly (probe-pinned, deterministic).
        var world = WorldGen.Generate(7, tier: 2);
        Assert.Equal("usurped-throne", world.Facts.OfType("story").Single().Subject);
        Assert.Equal(8, world.StoryStorylets.Count);

        var camp = world.Sites.First(s => s.Kind == SiteKind.GoblinCamp);
        var named = camp.Spawns.Where(s => s.Epithet is not null).ToList();
        string chief = named.First(s => s.Chief).Epithet!;
        var lieutenants = named.Where(s => !s.Chief).Select(s => s.Epithet!).ToList();

        // The claimant is a standing lieutenant; the teller is a stead voice.
        string claimant = world.Facts.Find("role", "claimant")!.Object;
        Assert.Contains(claimant, lieutenants);
        Assert.Contains(world.Npcs, n => n.Id == world.Facts.Find("role", "teller")!.Object);

        // The accepted history names the sitting chief and blames the stead.
        string history = world.Facts.Find("history", "seat_taken")!.Detail;
        Assert.Contains(chief, history);
        Assert.Contains("stead arrow", history);
    }

    [Fact]
    public void TheStoryTold_VoicesTheHistory_AndOnlyFromTheTeller()
    {
        var game = CrossedThroneGame();
        var teller = Teller(game);

        var bystander = game.World.Npcs.First(n => n.Kind == NpcKind.Villager && n.Id != teller.Id);
        NpcTests.BumpNpc(game, bystander);
        Assert.False(game.World.Facts.Exists("heard", "seat_story"));
        game.ApplyKey(' ');

        TalkUntil(game, teller, () => game.World.Facts.Exists("heard", "seat_story"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Blood serves strangely up there"));
    }

    [Fact]
    public void Evidence_DeepInTheCamp_WritesTheTruth()
    {
        var game = EvidencedThroneGame();
        Assert.True(game.World.Facts.Exists("evidence", "seat_truth"), "walking the dens never surfaced the cairn");
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("no arrow did this"));
    }

    [Fact]
    public void Ending_WithTheTruth_CarriesTheLedgerDown()
    {
        var game = EvidencedThroneGame();
        game.Debug_ClearCamp();

        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("true telling of the seat"));
        Assert.True(game.World.Facts.Exists("story_complete", "usurped-throne"));
        Assert.True(game.World.Facts.Exists("coda", "seat_truth_carried"));
        Assert.False(game.World.Facts.Exists("coda", "seat_lie_stands"));
    }

    [Fact]
    public void Ending_WithoutTheTruth_LetsTheLieStand()
    {
        var game = CrossedThroneGame();
        game.Debug_ClearCamp();

        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("goes cold under the cairn-stones"));
        Assert.True(game.World.Facts.Exists("story_complete", "usurped-throne"));
        Assert.True(game.World.Facts.Exists("coda", "seat_lie_stands"));
        Assert.False(game.World.Facts.Exists("coda", "seat_truth_carried"));
    }

    [Fact]
    public void TheStoryEndsOnce_LateEvidence_DoesNotRewriteTheFall()
    {
        // Found live under the pilot (D-112): the camp fell before the cairn was
        // read, the lie ending closed the story, and the truth ending then rode
        // the next deed's hook. A story ends once.
        var game = CrossedThroneGame();
        TalkUntil(game, Teller(game), () => game.World.Facts.Exists("heard", "seat_story"));
        game.Debug_ClearCamp();
        Assert.True(game.World.Facts.Exists("coda", "seat_lie_stands"));

        EvidenceWalk(game);
        Assert.True(game.World.Facts.Exists("evidence", "seat_truth"));
        game.Debug_ClearSite(SiteKind.Barrow);

        Assert.False(game.World.Facts.Exists("coda", "seat_truth_carried"));
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("true telling of the seat"));

        // The late truth still reaches the teller through the settling.
        int before = game.Player.Essence;
        TalkUntil(game, Teller(game),
            () => game.Log.Entries.Any(e => e.Text.Contains("given us back the ledger")));
        Assert.Equal(before + 3, game.Player.Essence);
    }

    [Fact]
    public void TheSettling_PaysTheSameCoin_TruthOrQuiet()
    {
        // Quiet path: story heard, camp cleared, truth never found.
        var quiet = CrossedThroneGame();
        TalkUntil(quiet, Teller(quiet), () => quiet.World.Facts.Exists("heard", "seat_story"));
        quiet.Debug_ClearCamp();
        int before = quiet.Player.Essence;
        TalkUntil(quiet, Teller(quiet),
            () => quiet.Log.Entries.Any(e => e.Text.Contains("Let the hill keep its stories")));
        Assert.Equal(before + 3, quiet.Player.Essence);

        // Truth path: same coin, different telling.
        var truth = EvidencedThroneGame();
        TalkUntil(truth, Teller(truth), () => truth.World.Facts.Exists("heard", "seat_story"));
        truth.Debug_ClearCamp();
        before = truth.Player.Essence;
        TalkUntil(truth, Teller(truth),
            () => truth.Log.Entries.Any(e => e.Text.Contains("given us back the ledger")));
        Assert.Equal(before + 3, truth.Player.Essence);
    }

    [Fact]
    public void ColdPath_StoryNeverHeard_SettlesNothing()
    {
        var game = CrossedThroneGame();
        game.Debug_ClearCamp();

        int before = game.Player.Essence;
        for (int i = 0; i < 6; i++)
        {
            NpcTests.BumpNpc(game, Teller(game));
            game.ApplyKey(' ');
        }
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("Let the hill keep its stories"));
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("given us back the ledger"));
        Assert.Equal(before, game.Player.Essence);
    }

    [Fact]
    public void TheClaimantRising_IsSpokenAsRestoration()
    {
        var game = CrossedThroneGame();
        string claimant = game.World.Facts.Find("role", "claimant")!.Object;

        // Fell every named raider except the claimant, then the chief last, so
        // the succession has exactly one heir to crown: the old blood.
        EnterCamp(game);
        foreach (var m in game.Monsters.Where(m => m.Epithet is not null && !m.Chief && m.Epithet != claimant))
            m.Hp = 0;
        var chief = game.Monsters.Single(m => m.Alive && m.Chief);
        chief.Hp = 1;
        StrikeDown(game, chief);
        Assert.Equal(claimant, game.Monsters.Single(m => m.Alive && m.Chief).Epithet);
        game.Apply(Command.Exit);

        TalkUntil(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager),
            () => game.Log.Entries.Any(e => e.Text.Contains("The old blood back on the seat")));
    }

    [Fact]
    public void TheClaimantPassedOver_IsSpokenAsAStoryThatWillKeep()
    {
        var game = CrossedThroneGame();
        string claimant = game.World.Facts.Find("role", "claimant")!.Object;

        // Fell the claimant first, then the chief: the other lieutenant rises.
        EnterCamp(game);
        game.Monsters.Single(m => m.Epithet == claimant).Hp = 0;
        var chief = game.Monsters.Single(m => m.Alive && m.Chief);
        chief.Hp = 1;
        StrikeDown(game, chief);
        var heir = game.Monsters.Single(m => m.Alive && m.Chief);
        Assert.NotEqual(claimant, heir.Epithet);
        game.Apply(Command.Exit);

        TalkUntil(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager),
            () => game.Log.Entries.Any(e => e.Text.Contains("stood aside or was stood aside")));
        Assert.Contains(game.Log.Entries,
            e => e.Text.Contains("stood aside or was stood aside") && e.Text.Contains(heir.Epithet!));
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

    /// <summary>Crosses the throne master into its cycle-2 world, which tells the throne.</summary>
    private static Game CrossedThroneGame()
    {
        var game = new Game(ThroneMaster);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.Equal("usurped-throne", game.World.Facts.OfType("story").Single().Subject);
        return game;
    }

    /// <summary>A crossed throne game that has walked the dens deep enough to read the cairn.</summary>
    private static Game EvidencedThroneGame()
    {
        var game = CrossedThroneGame();
        EvidenceWalk(game);
        return game;
    }

    /// <summary>Empties the dens and steps the deep floor until the cairn beat fires.</summary>
    private static void EvidenceWalk(Game game)
    {
        EnterCamp(game);
        foreach (var m in game.Monsters.Where(m => m.Alive && m.SiteId == "goblin-camp"))
            m.Hp = 0;

        // The camp is a cave, not a passage: step onto deep floor tiles (the
        // teleport itself skips EnterTile, so arrive by a real step) until the
        // cairn beat fires.
        var entry = game.World.CampSite.EntryPos;
        var map = game.CurrentMap;
        for (int y = 0; y < map.Height && !game.World.Facts.Exists("evidence", "seat_truth"); y++)
            for (int x = 0; x < map.Width && !game.World.Facts.Exists("evidence", "seat_truth"); x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || p.Manhattan(entry) < 10) continue;
                foreach (var (dx, dy) in Directions.Cardinal)
                {
                    var q = p.Plus(dx, dy);
                    if (!map.Walkable(q)) continue;
                    game.Debug_SetPlayerPos(q);
                    game.ApplyKey(KeyFor(p.X - q.X, p.Y - q.Y));
                    break;
                }
            }
        Assert.True(game.World.Facts.Exists("evidence", "seat_truth"), "no deep step surfaced the cairn");
        game.Debug_SetMode(MapMode.Overworld);
    }

    private static Npc Teller(Game game)
    {
        string id = game.World.Facts.Find("role", "teller")!.Object;
        return game.World.Npcs.First(n => n.Id == id);
    }

    private static void EnterCamp(Game game)
    {
        game.Debug_SetPlayerPos(game.World.CampSite.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal(MapMode.Site, game.Mode);
    }

    /// <summary>One killing blow: steps into the adjacent target's cell.</summary>
    private static void StrikeDown(Game game, Monster target)
    {
        if (target.Pos.Chebyshev(game.Player.Pos) != 1) target.Pos = OpenAt(game, game.Player.Pos, 1);
        game.ApplyKey(KeyFor(Math.Sign(target.Pos.X - game.Player.Pos.X), Math.Sign(target.Pos.Y - game.Player.Pos.Y)));
    }

    private static Pos OpenAt(Game game, Pos origin, int dist)
    {
        var map = game.CurrentMap;
        for (int dx = -dist; dx <= dist; dx++)
            for (int dy = -dist; dy <= dist; dy++)
            {
                var p = origin.Plus(dx, dy);
                if (p.Chebyshev(origin) != dist || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                if (p == game.Player.Pos) continue;
                return p;
            }
        throw new InvalidOperationException($"no open cell at distance {dist}");
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (-1, -1) => 'y', (0, -1) => 'k', (1, -1) => 'u',
        (-1, 0) => 'h', (1, 0) => 'l',
        (-1, 1) => 'b', (0, 1) => 'j', (0, 0) => '.', _ => 'n',
    };
}
