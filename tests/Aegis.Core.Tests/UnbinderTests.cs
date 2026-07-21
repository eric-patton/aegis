using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The Unbinder and attribute respec (D-034, D-016): cast into every world at every
/// tier, a lossless per-point essence refund, a per-world cap refreshed at crossings,
/// and the cross-world recognition thread.
/// </summary>
public class UnbinderTests
{
    [Fact]
    public void EveryWorld_CastsTheUnbinder_ReachableAndRumored()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            foreach (int tier in (int[])[1, 3])
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);

                var u = a.Unbinder;
                Assert.Equal(NpcKind.Unbinder, u.Kind);
                Assert.Equal("npc_unbinder", u.Id);
                Assert.Contains(u.Role, UnbinderGuises.Roles);
                Assert.True(a.Overworld.Walkable(u.Pos), $"seed {seed} tier {tier}: Unbinder on unwalkable tile");
                Assert.True(Reachable(a.Overworld, a.ShrinePos, u.Pos), $"seed {seed} tier {tier}: Unbinder unreachable");
                Assert.True(a.Facts.Exists("wanderer", u.Id), $"seed {seed} tier {tier}: no wanderer rumor fact");
                Assert.True(a.Facts.Exists("person", u.Id));

                Assert.Equal((u.Id, u.Name, u.Role, u.Pos),
                    (b.Unbinder.Id, b.Unbinder.Name, b.Unbinder.Role, b.Unbinder.Pos));
            }
        }
    }

    [Fact]
    public void Unbinder_IsNeverCastIntoAStoryRole()
    {
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var world = WorldGen.Generate(seed);
            foreach (var role in world.Facts.OfType("role"))
                Assert.NotEqual("npc_unbinder", role.Object);
        }
    }

    [Fact]
    public void Unbinding_RefundsTheMarginalCost_AndRoundTripsLossless()
    {
        var game = new Game(42);
        game.Player.Essence = 100;

        // Raise Vigor then Might at the shrine: costs 10 then 15.
        game.ApplyKey('r');
        game.ApplyKey('3');
        game.ApplyKey('1');
        game.ApplyKey('x');
        Assert.Equal(75, game.Player.Essence);

        OpenUnbindMenu(game);
        Assert.True(game.InUnbindMenu);

        // Loosen Vigor: the second raise cost 15, so 15 comes back.
        game.ApplyKey('3');
        Assert.Equal(5, game.Player.Attributes[Attr.Vigor]);
        Assert.Equal(90, game.Player.Essence);
        Assert.Equal(Game.UnbindingsPerWorld - 1, game.UnbindingsLeft);

        // Loosen Might: back to the start, to the essence.
        game.ApplyKey('1');
        Assert.Equal(5, game.Player.Attributes[Attr.Might]);
        Assert.Equal(100, game.Player.Essence);
        Assert.Equal(10, game.NextRaiseCost);

        game.ApplyKey(' ');
        Assert.False(game.InUnbindMenu);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("Nothing needs to be counted"));
    }

    [Fact]
    public void Unbinding_RefusesBaseline_AndIsCappedPerWorld()
    {
        var game = new Game(42);
        game.Player.Essence = 200;

        // Raise four attributes: costs 10, 15, 20, 25.
        game.ApplyKey('r');
        foreach (char key in "1234") game.ApplyKey(key);
        game.ApplyKey('x');
        Assert.Equal(130, game.Player.Essence);

        OpenUnbindMenu(game);

        // A baseline attribute refuses and burns no charge.
        game.ApplyKey('5');
        Assert.Equal(Game.UnbindingsPerWorld, game.UnbindingsLeft);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("born with"));

        // Three unbindings: refunds 25, 20, 15.
        foreach (char key in "123") game.ApplyKey(key);
        Assert.Equal(190, game.Player.Essence);
        Assert.Equal(0, game.UnbindingsLeft);

        // The fourth is refused even though Wits is still raised.
        game.ApplyKey('4');
        Assert.Equal(6, game.Player.Attributes[Attr.Wits]);
        Assert.Equal(190, game.Player.Essence);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("Not again in this world"));

        // Re-opening the service while spent refuses at the door.
        game.ApplyKey(' ');
        BumpUnbinder(game);
        game.ApplyKey((char)('1' + game.Topics.Count));
        Assert.False(game.InUnbindMenu);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("Not again in this world"));
    }

    [Fact]
    public void UnbindingVigor_ClampsHpAndStamina()
    {
        var game = new Game(42);
        game.Player.Essence = 100;

        game.ApplyKey('r');   // rest heals to full first
        game.ApplyKey('3');   // raise Vigor: max hp 22, and the raise heals 2
        game.ApplyKey('x');
        Assert.Equal(22, game.Player.Hp);

        OpenUnbindMenu(game);
        game.ApplyKey('3');
        Assert.Equal(20, game.Player.EffectiveMaxHp);
        Assert.True(game.Player.Hp <= 20);
        Assert.True(game.Player.Stamina <= game.Player.MaxStamina);
    }

    [Fact]
    public void Crossing_RefreshesTheUnbindings_AndRecastsTheGuise()
    {
        var game = new Game(42);
        game.Player.Essence = 200;
        game.ApplyKey('r');
        foreach (char key in "1234") game.ApplyKey(key);
        game.ApplyKey('x');

        var firstGuise = (game.World.Unbinder.Name, game.World.Unbinder.Role);

        OpenUnbindMenu(game);
        foreach (char key in "123") game.ApplyKey(key);
        Assert.Equal(0, game.UnbindingsLeft);
        game.ApplyKey(' ');

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.Equal(Game.UnbindingsPerWorld, game.UnbindingsLeft);

        // A fresh world casts a fresh guise (names could collide by chance on some
        // seed; position practically never does on this one).
        var u = game.World.Unbinder;
        Assert.NotNull(u);
        Assert.NotEqual(firstGuise, (u.Name, u.Role));
    }

    [Fact]
    public void Recognition_FiresOnALaterWorldsMeeting_OncePerCharacter()
    {
        var game = new Game(42);

        // World 1: meet the Unbinder. No recognition; the first-meeting cycle is recorded.
        BumpUnbinder(game);
        Assert.Equal(1, game.Player.FirstUnbinderCycle);
        Assert.DoesNotContain(game.Log.Recent(6), e => e.Text.Contains("These wanderers know you"));
        Assert.DoesNotContain(game.Topics, t => t.Label == "The one before");
        game.ApplyKey(' ');

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        // World 2: a different mender, and the thread pulls tight.
        BumpUnbinder(game);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("These wanderers know you"));
        Assert.True(game.World.Facts.Exists("noticed", "unbinder"));
        game.ApplyKey(' ');

        // The fact write unlocks the topic on the next conversation, and the
        // recognition beat itself never fires a second time (Character scope, Once).
        BumpUnbinder(game);
        Assert.Contains(game.Topics, t => t.Label == "The one before");
        game.ApplyKey(' ');
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("These wanderers know you")));
    }

    [Fact]
    public void VillagersPointTheWay_ToTheWanderer()
    {
        // Not the steadholder's topic since D-134 (the works bench took the
        // digit); every other door still points the way.
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager && n.Id != "npc_steadholder"));

        var topic = game.Topics.FirstOrDefault(t => t.Label == "The wanderer");
        Assert.NotEqual(default, topic);
        Assert.Contains(game.World.Unbinder.Name, topic.Answer);
        Assert.Contains(game.World.Unbinder.Role, topic.Answer);
    }

    [Fact]
    public void UnbindSession_ReplaysIdenticallyFromJournal()
    {
        const ulong seed = 42;
        var live = new Game(seed);
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);

        live.Player.Essence = 100; // granted, not journaled: grant on both sides
        live.ApplyKey('r');
        live.ApplyKey('3');
        live.ApplyKey('1');
        live.ApplyKey('x');

        // Walk to the Unbinder on journaled keys, then loosen Might and part ways.
        var target = live.World.Unbinder.Pos;
        for (int guard = 0; guard < 400 && !live.InTalkMenu; guard++)
        {
            char? key = StepTo(live, target);
            if (key is null) break;
            live.ApplyKey(key.Value);
        }
        Assert.True(live.InTalkMenu, "bot never reached the Unbinder");
        live.ApplyKey((char)('1' + live.Topics.Count));
        Assert.True(live.InUnbindMenu);
        live.ApplyKey('1');
        live.ApplyKey(' ');

        var replayed = new Game(seed);
        replayed.Player.Essence = 100;
        foreach (char key in journal.ToString()) replayed.ApplyKey(key);

        Assert.Equal(live.Player.Essence, replayed.Player.Essence);
        Assert.Equal(live.Player.Attributes[Attr.Might], replayed.Player.Attributes[Attr.Might]);
        Assert.Equal(live.Player.Attributes[Attr.Vigor], replayed.Player.Attributes[Attr.Vigor]);
        Assert.Equal(live.UnbindingsLeft, replayed.UnbindingsLeft);
        Assert.Equal(live.Player.Unbindings, replayed.Player.Unbindings);
        Assert.Equal(live.Turn, replayed.Turn);
        Assert.Equal(
            live.Log.Recent(15).Select(e => e.Text),
            replayed.Log.Recent(15).Select(e => e.Text));
    }

    /// <summary>Teleports beside the Unbinder and bumps into them.</summary>
    private static void BumpUnbinder(Game game) => NpcTests.BumpNpc(game, game.World.Unbinder);

    /// <summary>Bumps the Unbinder and picks the unbinding entry (last digit in the menu).</summary>
    private static void OpenUnbindMenu(Game game)
    {
        BumpUnbinder(game);
        game.ApplyKey((char)('1' + game.Topics.Count));
    }

    private static bool Reachable(GameMap map, Pos from, Pos to)
    {
        var seen = new HashSet<Pos> { from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (p == to) return true;
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var next = p.Plus(dx, dy);
                if (map.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return false;
    }

    /// <summary>One BFS step toward a target, treating the target tile itself as enterable (bump).</summary>
    internal static char? StepTo(Game game, Pos goal)
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
        _ => throw new InvalidOperationException("not adjacent"),
    };
}
