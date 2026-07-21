using Aegis.Core;

namespace Aegis.Core.Tests;

public class NpcTests
{
    private static Pos SettlementCenter(Game game) => game.World.ShrinePos.Plus(0, -2);

    /// <summary>Puts the player beside an NPC and bumps into them; returns the NPC.</summary>
    private static Npc BumpFirstNpc(Game game) => BumpNpc(game, game.World.Npcs[0]);

    internal static Npc BumpNpc(Game game, Npc npc)
    {
        var beside = Directions.All8
            .Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => game.World.Overworld.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
        return npc;
    }

    [Fact]
    public void Npcs_AreCastDeterministically_BesideHouses_OffTheShrineRoad()
    {
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);

            Assert.True(a.Npcs.Count >= 1, $"seed {seed}: no NPCs cast");
            Assert.Equal(a.Npcs.Select(n => (n.Id, n.Name, n.Pos)), b.Npcs.Select(n => (n.Id, n.Name, n.Pos)));

            var settlement = a.ShrinePos.Plus(0, -2);
            foreach (var npc in a.Npcs)
            {
                // Everyone stands walkable on their OWN overworld (D-138): the
                // waykeeper's position means nothing on the valley's map.
                var ground = npc.OnRoad ? a.Road : a.Overworld;
                Assert.True(ground.Walkable(npc.Pos), $"seed {seed}: {npc.Id} on unwalkable tile");
                // The road rule is a settlement rule: the Unbinder camps far away
                // and may share the column without blocking anything.
                if (npc.Kind == NpcKind.Villager) Assert.NotEqual(settlement.X, npc.Pos.X);
                Assert.True(a.Facts.Exists("person", npc.Id));
            }
            Assert.Equal(a.Npcs.Count, a.Npcs.Select(n => (n.OnRoad, n.Pos)).Distinct().Count());
        }
    }

    [Fact]
    public void BumpToTalk_OpensMenu_WritesMetFact_AndFiresFirstVoices()
    {
        var game = new Game(42);
        int turnBefore = game.Turn;

        // The plaintiff's plea outranks the first-voices aside (priority tiers), so
        // assert the aside against a non-plaintiff villager.
        string plaintiffId = game.World.Facts.Find("role", "plaintiff")!.Object;
        var npc = BumpNpc(game, game.World.Npcs.First(n => n.Id != plaintiffId));

        Assert.True(game.InTalkMenu);
        Assert.Equal(npc.Name, game.TalkNpc!.Name);
        Assert.Equal(turnBefore + 1, game.Turn);
        Assert.True(game.World.Facts.Exists("met", npc.Id));
        Assert.Contains(game.Log.Recent(10), e => e.Text.Contains("So many voices"));
        Assert.NotEmpty(game.Topics);
    }

    [Fact]
    public void Topics_TrackWorldState()
    {
        var game = new Game(42);
        BumpFirstNpc(game);

        Assert.Contains(game.Topics, t => t.Label == "The goblin raids");
        Assert.DoesNotContain(game.Topics, t => t.Label == "The quiet nights");
        Assert.DoesNotContain(game.Topics, t => t.Label == "Old songs");

        // Asking lands the fact's detail in the log through the NPC's mouth.
        var raids = game.Topics.First(t => t.Label == "The goblin raids");
        int index = game.Topics.ToList().FindIndex(t => t.Label == "The goblin raids");
        game.ApplyKey((char)('1' + index));
        Assert.Contains(game.Log.Recent(5), e => e.Text.Contains("raid") && e.Text.Contains(game.TalkNpc!.Name));

        // Close, clear the camp, talk again: the world moved and the topics moved with it.
        game.ApplyKey(' ');
        Assert.False(game.InTalkMenu);
        game.Debug_ClearCamp();
        BumpFirstNpc(game);

        Assert.Contains(game.Topics, t => t.Label == "The quiet nights");
        Assert.DoesNotContain(game.Topics, t => t.Label == "The goblin raids");
        Assert.Contains(game.Topics, t => t.Label == "The black arch" && t.Answer.Contains("hums"));
    }

    [Fact]
    public void OldSongs_TopicAppearsAfterACrossing()
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        BumpFirstNpc(game);
        var songs = game.Topics.FirstOrDefault(t => t.Label == "Old songs");
        Assert.NotEqual(default, songs);
        Assert.Contains("goblin cave", songs.Answer);
    }

    [Fact]
    public void TalkSession_ReplaysIdenticallyFromJournal()
    {
        // The parity proof runs through the real wake (D-092): fate answers the asking.
        var game = new Game(42, firstWake: true);
        var journal = new List<char>();
        game.KeyApplied += journal.Add;
        game.ApplyKey('0');

        // Walk (journaled) rather than teleport: replays must not need debug hooks.
        var npc = game.World.Npcs[0];
        for (int guard = 0; guard < 200 && !game.InTalkMenu; guard++)
        {
            // The shuttered window may open on the way (D-117); leaving is journaled too.
            if (game.InScene) { game.ApplyKey('3'); continue; }
            char? key = StepTo(game, npc.Pos);
            if (key is null) break;
            game.ApplyKey(key.Value);
        }
        Assert.True(game.InTalkMenu, "bot never reached the NPC");

        game.ApplyKey('1');
        game.ApplyKey('2');
        game.ApplyKey(' ');

        var replayed = SaveCodec.Replay(42, new string(journal.ToArray()));

        Assert.Equal(game.Turn, replayed.Turn);
        Assert.Equal(game.InTalkMenu, replayed.InTalkMenu);
        Assert.Equal(game.World.Facts.All.Count, replayed.World.Facts.All.Count);
        Assert.Equal(
            game.Log.Recent(15).Select(e => e.Text),
            replayed.Log.Recent(15).Select(e => e.Text));
    }

    /// <summary>One BFS step toward a target tile, treating the target itself as enterable (bump).</summary>
    private static char? StepTo(Game game, Pos goal)
    {
        var map = game.World.Overworld;
        var from = game.Player.Pos;
        var cameFrom = new Dictionary<Pos, Pos> { [from] = from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (p == goal) break;
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var next = p.Plus(dx, dy);
                bool enterable = next == goal
                    || (map.Walkable(next) && !game.World.Npcs.Any(n => n.Pos == next));
                if (enterable && map.InBounds(next) && !cameFrom.ContainsKey(next))
                {
                    cameFrom[next] = p;
                    queue.Enqueue(next);
                }
            }
        }
        if (!cameFrom.ContainsKey(goal)) return null;
        var step = goal;
        while (cameFrom[step] != from) step = cameFrom[step];
        return KeyFor(step.X - from.X, step.Y - from.Y);
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        (1, 1) => 'n',
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
