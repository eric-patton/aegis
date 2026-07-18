using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// The autopilot that plays the ladder (D-062). Given a live <see cref="Game"/>, it
/// decides the one key to press next, reading only what a player could read: position,
/// the map, live monsters and their telegraphed intents, HP and stamina. The whole loop
/// is: walk to the camp, clear it by dodging the wind-up and answering it, climb back to
/// daylight, walk to the arch, and cross. It fights the way D-004 says the game can be
/// fought: never stand on the aimed cell, and strike the body that is spoken for.
///
/// It is a pure function of game state, so a seeded run is perfectly reproducible: the
/// same seed walks the same road. That is what makes it a verification tool and not just
/// a demo. It drives through <see cref="Game.ApplyKey"/> alone, touching no debug hook,
/// so every crossing it makes is a real crossing.
/// </summary>
public static class JourneyPilot
{
    /// <summary>The next key to press, or null when the bot can find no move (report it, do not spin).</summary>
    public static char? NextKey(Game g)
    {
        var p = g.Player;

        // The only menu the bot ever opens on purpose is the arch's terms. Anything
        // else that trapped it (a bumped villager, say) gets stepped back out of.
        if (StuckInMenu(g)) return 'z';

        if (!g.CampCleared)
        {
            if (g.Mode == MapMode.Site)
                return FightOrApproach(g);

            // Overworld, camp still loud: make for the cave mouth and go down.
            var camp = g.World.CampSite.OverworldPos;
            if (p.Pos == camp) return '>';
            return NavKey(g, g.World.Overworld, p.Pos, camp, OverworldBlocked(g));
        }

        // Camp silenced. If we are still underground, climb out by the ladder.
        if (g.Mode == MapMode.Site)
        {
            var ladder = g.CurrentSite!.EntryPos;
            if (p.Pos == ladder) return '<';
            return NavKey(g, g.CurrentMap, p.Pos, ladder, LiveFoeCells(g));
        }

        // Daylight, deed done: the arch will answer now.
        if (g.InCrossingMenu) return '>';   // take up no terms; cross plain.
        var gate = g.World.GatePos;
        if (p.Pos == gate) return '>';       // set a hand on the arch: the terms open.
        return NavKey(g, g.World.Overworld, p.Pos, gate, OverworldBlocked(g));
    }

    // ---- combat: the read, the dodge, the answer ----

    private static char? FightOrApproach(Game g)
    {
        var p = g.Player;
        var foes = g.LiveMonstersHere.Where(m => !m.Dormant).ToList();
        if (foes.Count == 0) return null; // camp not cleared yet only dormant foes remain: not a tier-1 case.

        // Am I standing on a cell a wind-up is aimed at? Then the whole of the play
        // is to not be here when it lands.
        bool aimed = foes.Any(m => m.Intent is { } it && it.TargetCell == p.Pos);
        if (aimed)
        {
            if (ChooseDodge(g, foes) is { } dodge) return dodge;
            // Nowhere safe to step: better to answer a blow than take it standing.
        }

        var nearest = foes.OrderBy(m => Chebyshev(p.Pos, m.Pos)).First();
        if (Chebyshev(p.Pos, nearest.Pos) == 1)
            return KeyFor(Math.Sign(nearest.Pos.X - p.Pos.X), Math.Sign(nearest.Pos.Y - p.Pos.Y)); // bump = strike

        // Close the distance, stepping around the other foes rather than into them.
        var blocked = foes.Where(m => m != nearest).Select(m => m.Pos).ToHashSet();
        return NavKey(g, g.CurrentMap, p.Pos, nearest.Pos, blocked);
    }

    /// <summary>
    /// Step off the aimed cell to the best open neighbour: one still beside a foe when
    /// it can, so the next turn answers the blow, but never into another wind-up and
    /// never into a swarm.
    /// </summary>
    private static char? ChooseDodge(Game g, List<Monster> foes)
    {
        var map = g.CurrentMap;
        var cur = g.Player.Pos;
        var targeted = foes.Where(m => m.Intent is not null).Select(m => m.Intent!.TargetCell).ToHashSet();
        var occupied = foes.Select(m => m.Pos).ToHashSet();

        Pos? best = null;
        int bestScore = int.MaxValue;
        foreach (var (dx, dy) in Directions.All8)
        {
            var n = cur.Plus(dx, dy);
            if (!map.Walkable(n) || occupied.Contains(n) || targeted.Contains(n)) continue;
            int adj = foes.Count(m => Chebyshev(n, m.Pos) == 1);
            // Prefer staying engaged with exactly one foe; break ties toward fewer foes near.
            int score = Math.Abs(adj - 1) * 10 + adj;
            if (score < bestScore) { bestScore = score; best = n; }
        }
        return best is { } b ? KeyFor(b.X - cur.X, b.Y - cur.Y) : null;
    }

    // ---- navigation: breadth-first, one step at a time ----

    /// <summary>
    /// The key for the first step of a shortest walkable route from <paramref name="from"/>
    /// to <paramref name="goal"/>. The goal cell itself may be occupied (a foe to bump, an
    /// arch to touch): it is allowed as the terminal even when not walkable. Null if no
    /// route exists.
    /// </summary>
    private static char? NavKey(Game g, GameMap map, Pos from, Pos goal, HashSet<Pos> blocked)
    {
        if (from == goal) return null;
        var came = new Dictionary<Pos, Pos> { [from] = from };
        var q = new Queue<Pos>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var (dx, dy) in Directions.All8)
            {
                var next = cur.Plus(dx, dy);
                if (came.ContainsKey(next)) continue;
                bool isGoal = next == goal;
                if (!isGoal && (!map.Walkable(next) || blocked.Contains(next))) continue;
                came[next] = cur;
                if (isGoal)
                {
                    var step = next;
                    while (came[step] != from) step = came[step];
                    return KeyFor(step.X - from.X, step.Y - from.Y);
                }
                q.Enqueue(next);
            }
        }
        return null;
    }

    private static HashSet<Pos> OverworldBlocked(Game g) =>
        g.World.Npcs.Select(n => n.Pos).ToHashSet(); // never bump a person into a menu.

    private static HashSet<Pos> LiveFoeCells(Game g) =>
        g.LiveMonstersHere.Select(m => m.Pos).ToHashSet();

    private static bool StuckInMenu(Game g) =>
        g.InShrineMenu || g.InTalkMenu || g.InUnbindMenu || g.InThresholdMenu
        || g.InLayingMenu || g.InGearMenu || g.InSheetMenu; // the crossing menu is driven, not escaped.

    private static int Chebyshev(Pos a, Pos b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

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
        _ => '.', // no movement wanted: let the turn pass.
    };
}
