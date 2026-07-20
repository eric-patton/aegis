using Aegis.Core;

namespace Aegis.Core.Tests;

public class CrossingTests
{
    [Fact]
    public void Waygate_IsPlacedWalkable_AndReachableFromShrine_ManySeeds()
    {
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var world = WorldGen.Generate(seed);
            Assert.Equal(Terrain.Waygate, world.Overworld[world.GatePos]);
            Assert.NotEqual(world.GatePos, world.CampPos);
            Assert.True(Reachable(world.Overworld, world.ShrinePos, world.GatePos),
                $"seed {seed}: waygate unreachable from shrine");
        }
    }

    [Fact]
    public void ShutGate_RefusesCrossing_UntilDeedIsDone()
    {
        var game = new Game(42);
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        Assert.Equal(1, game.Cycle);
        Assert.Equal(42UL, game.World.Seed);
    }

    [Fact]
    public void Crossing_CarriesCharacter_ConvertsCoin_ForfeitsRemnant()
    {
        var game = new Game(42);
        string firstWorld = game.World.Name;

        // Die with loot in the cave to leave a remnant behind.
        game.Player.Coin = 9;
        game.Player.Essence = 6;
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.NotNull(game.Remnant);

        // Earn a spendable character and finish the world's deed.
        game.Player.Coin = 12;
        game.Player.Essence = 30;
        game.Player.Attributes[Attr.Might] = 7;
        game.Debug_ClearCamp();

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);

        Assert.Equal(2, game.Cycle);
        Assert.Equal(2, game.World.Tier);
        Assert.NotEqual(42UL, game.World.Seed);

        // Character bucket carries; coin converts to Legend; the remnant is gone for good.
        Assert.Equal(7, game.Player.Attributes[Attr.Might]);
        Assert.Equal(30, game.Player.Essence);
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(12 + 5, game.Player.Legend); // 12 held + the stead's 5-coin friend's welcome (D-077), both converted
        Assert.Null(game.Remnant);

        // Fresh arrival: full health at the new shrine, wounds lifted, world state reset.
        Assert.Equal(MapMode.Overworld, game.Mode);
        Assert.Equal(game.World.ShrinePos, game.Player.Pos);
        Assert.Equal(0, game.Player.WoundedTurns);
        Assert.Equal(game.Player.MaxHp, game.Player.Hp);
        Assert.False(game.CampCleared);
        Assert.False(game.World.CampSite.ChestLooted);

        // Tier 2 generation input: one more goblin, each tougher, and the barrow band opens.
        var goblins = game.Monsters.Where(m => m.Kind == MonsterKind.Goblin).ToList();
        var wights = game.Monsters.Where(m => m.Kind == MonsterKind.Wight).ToList();
        Assert.Equal(4, goblins.Count);
        // Rank worn as hide (D-110): the chief and lieutenants over the base 10.
        Assert.All(goblins, m => Assert.Equal(
            10 + (m.Chief ? RaiderRoster.ChiefHide : m.Epithet is not null ? RaiderRoster.LieutenantHide : 0), m.Hp));
        Assert.Equal(2, wights.Count);
        Assert.All(wights, m => Assert.Equal(12, m.Hp));

        // The finished world is pressed into the new one as an echo (D-013).
        Assert.True(game.World.Facts.Exists("echo", "deed"));
        Assert.Contains(game.Log.Recent(10), e => e.Text.Contains(firstWorld));
    }

    [Fact]
    public void Crossing_IsDeterministic()
    {
        Game Run()
        {
            var game = new Game(1234);
            game.Player.Coin = 20;
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.Apply(Command.Enter);
            game.Apply(Command.Enter);
            foreach (char key in "jjkkllhh..")
                game.ApplyKey(key);
            return game;
        }

        var a = Run();
        var b = Run();

        Assert.Equal(a.World.Seed, b.World.Seed);
        Assert.Equal(a.World.Name, b.World.Name);
        Assert.Equal(a.World.Overworld.ContentHash(), b.World.Overworld.ContentHash());
        Assert.Equal(a.World.Camp.ContentHash(), b.World.Camp.ContentHash());
        Assert.Equal(a.Player.Pos, b.Player.Pos);
        Assert.Equal(a.Player.Hp, b.Player.Hp);
        Assert.Equal(a.Player.Legend, b.Player.Legend);
        Assert.Equal(a.Turn, b.Turn);
    }

    /// <summary>
    /// The whole loop, honestly: a greedy key-driven bot clears the camp, walks to the
    /// gate, and crosses, with every key recorded; the journal must then replay to the
    /// bit-identical post-crossing state. This is the save contract (D-028) spanning a
    /// world boundary.
    /// </summary>
    [Fact]
    public void KeyDrivenPlaythrough_Crosses_AndJournalReplaysIdentically()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            // The parity proof runs through the real wake (D-092): fate answers the asking.
            var game = new Game(seed, firstWake: true);
            var journal = new List<char>();
            game.KeyApplied += journal.Add;
            game.ApplyKey('0');

            if (!TryPlayToCrossing(game, maxKeys: 5000)) continue;

            Assert.Equal(2, game.Cycle);

            var replayed = SaveCodec.Replay(seed, new string(journal.ToArray()));

            Assert.Equal(2, replayed.Cycle);
            Assert.Equal(game.World.Seed, replayed.World.Seed);
            Assert.Equal(game.Player.Pos, replayed.Player.Pos);
            Assert.Equal(game.Player.Hp, replayed.Player.Hp);
            Assert.Equal(game.Player.Essence, replayed.Player.Essence);
            Assert.Equal(game.Player.Legend, replayed.Player.Legend);
            Assert.Equal(game.Player.Deaths, replayed.Player.Deaths);
            Assert.Equal(game.Turn, replayed.Turn);
            return; // one full key-driven crossing is the proof
        }

        Assert.Fail("No seed in 1..20 produced a bot-completable crossing within the key budget.");
    }

    /// <summary>Greedy bot: hunt goblins, then walk to the gate and cross. Deaths are survivable setbacks.</summary>
    private static bool TryPlayToCrossing(Game game, int maxKeys)
    {
        for (int i = 0; i < maxKeys && game.Running; i++)
        {
            if (game.Cycle >= 2) return true;
            char? key = NextBotKey(game);
            if (key is null) return false;
            game.ApplyKey(key.Value);
        }
        return game.Cycle >= 2;
    }

    private static char? NextBotKey(Game game)
    {
        if (game.InShrineMenu || game.InTalkMenu) return ' '; // rise / part ways without engaging

        if (game.Mode == MapMode.Site)
        {
            var target = game.Monsters.Where(m => m.Alive)
                .OrderBy(m => m.Pos.Manhattan(game.Player.Pos)).FirstOrDefault();
            if (target is null)
            {
                // Camp done: climb out from the exit ladder.
                if (game.World.Camp[game.Player.Pos] == Terrain.ExitLadder) return '<';
                return StepToward(game, game.World.Camp, FindLadder(game.World.Camp),
                    p => game.World.Camp.Walkable(p));
            }
            if (target.Pos.Chebyshev(game.Player.Pos) == 1)
                return KeyFor(target.Pos.X - game.Player.Pos.X, target.Pos.Y - game.Player.Pos.Y);
            return StepToward(game, game.World.Camp, target.Pos,
                p => game.World.Camp.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p && m != target));
        }

        var goal = game.CampCleared ? game.World.GatePos : game.World.CampPos;
        if (game.Player.Pos == goal) return '>';
        return StepToward(game, game.World.Overworld, goal,
            p => game.World.Overworld.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
    }

    private static Pos FindLadder(GameMap camp)
    {
        for (int y = 0; y < camp.Height; y++)
            for (int x = 0; x < camp.Width; x++)
                if (camp[new Pos(x, y)] == Terrain.ExitLadder) return new Pos(x, y);
        throw new InvalidOperationException("camp has no exit ladder");
    }

    /// <summary>BFS one step toward the goal, cardinal moves only; null if unreachable.</summary>
    private static char? StepToward(Game game, GameMap map, Pos goal, Func<Pos, bool> passable)
    {
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
                if ((passable(next) || next == goal) && map.InBounds(next) && !cameFrom.ContainsKey(next))
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
        _ => '.',
    };

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
}
