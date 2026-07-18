using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// The autopilot that plays the ladder (D-062). Given a live <see cref="Game"/>, it
/// decides the one key to press next, reading only what a player could read: position,
/// the map, live monsters and their telegraphed intents, HP and stamina. The loop, world
/// by world: clear every tenanted site in reach (the camp that gates the arch, and the
/// barrow, hollow, quarry, hall, ringfort, and leaguer besides), then walk to the arch
/// and cross. It fights the way D-004 says the game can be fought: never stand on the
/// aimed cell, and strike the body that is spoken for. Some sites it cannot win (a ranged
/// foe that keeps its distance from a bare fist); the runner budgets each site and hands
/// the pilot a skip-set of the ones to leave standing, so it engages, learns the tell,
/// and moves on rather than spinning.
///
/// It is a pure function of game state (and the runner's skip-set, which is itself
/// derived deterministically), so a seeded run is perfectly reproducible: the same seed
/// walks the same road. That is what makes it a verification tool and not just a demo. It
/// drives through <see cref="Game.ApplyKey"/> alone, touching no debug hook, so every
/// crossing it makes is a real crossing.
/// </summary>
public static class JourneyPilot
{
    /// <summary>
    /// The next key to press, or null when the bot can find no move (report it, do not spin).
    /// <paramref name="skip"/> is the set of site ids the runner has given up clearing; the
    /// pilot treats them as done (engage no further, leave and move on).
    /// </summary>
    public static char? NextKey(Game g, IReadOnlySet<string> skip)
    {
        var p = g.Player;

        // Two menus the bot drives on purpose: the shrine's raising (spend essence on
        // Vigor and Might, the survivability the deep sites demand) and the arch's terms
        // (handled below). Any other menu that trapped it gets stepped back out of.
        if (g.InShrineMenu)
            return p.Essence >= g.NextRaiseCost ? RaiseDigit(p) : 'z';
        if (StuckInMenu(g)) return 'z';

        if (g.Mode == MapMode.Site)
        {
            var site = g.CurrentSite!;
            // Still work to do here: clear it. Otherwise climb back to daylight.
            if (!site.Cleared && !skip.Contains(site.Id))
                return FightOrApproach(g);
            var ladder = site.EntryPos;
            if (p.Pos == ladder) return '<';
            // Head for the ladder; if live foes box the route, bump through them.
            return NavKey(g, g.CurrentMap, p.Pos, ladder, LiveFoeCells(g))
                ?? NavKey(g, g.CurrentMap, p.Pos, ladder, Empty);
        }

        // Overworld.
        if (g.InCrossingMenu) return '>';   // already at the arch: cross plain, no terms.

        // Standing on the shrine with essence to spend or wounds to mend: rest and raise
        // before setting out (this also catches the shrine you wake on after a death).
        if (p.Pos == g.World.ShrinePos
            && (p.Essence >= g.NextRaiseCost || p.Hp < p.EffectiveMaxHp))
            return 'r';

        // Take the nearest site still worth entering before the arch.
        var target = NearestUnclearedSite(g, skip);
        if (target is not null)
        {
            if (p.Pos == target.OverworldPos) return '>';   // stand on the mouth, go down.
            return NavKey(g, g.World.Overworld, p.Pos, target.OverworldPos, OverworldBlocked(g));
        }

        // Every site is cleared or written off: the deed is done, the arch will answer.
        var gate = g.World.GatePos;
        if (p.Pos == gate) return '>';       // set a hand on the arch: the terms open.
        return NavKey(g, g.World.Overworld, p.Pos, gate, OverworldBlocked(g));
    }

    /// <summary>
    /// The nearest tenanted site the bot has neither cleared nor given up on: any site
    /// still holding a live body (dormant counts, it is only sleeping). The songhall and
    /// the threshold hold no foes, so they never qualify.
    /// </summary>
    private static Site? NearestUnclearedSite(Game g, IReadOnlySet<string> skip)
    {
        var here = g.Player.Pos;
        return g.World.Sites
            .Where(s => !s.Cleared && !skip.Contains(s.Id)
                        && g.Monsters.Any(m => m.Alive && m.SiteId == s.Id))
            .OrderBy(s => Chebyshev(here, s.OverworldPos))
            .ThenBy(s => s.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    // ---- combat: the read, the dodge, the answer ----

    private static char? FightOrApproach(Game g)
    {
        var p = g.Player;
        // Dormant foes are included on purpose: a graven man or a warder is woken by
        // being neared or struck, so the bot walks up to it and bumps it awake.
        var foes = g.LiveMonstersHere.ToList();
        if (foes.Count == 0) return null;

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

        // Close the distance, routing around the other foes when we can; if that boxes
        // us in, bump straight through (a step into a foe is a strike). Null only when
        // the nearest foe is truly unreachable (across water, say), which tells the
        // runner this site cannot be won on foot.
        var blocked = foes.Where(m => m != nearest).Select(m => m.Pos).ToHashSet();
        return NavKey(g, g.CurrentMap, p.Pos, nearest.Pos, blocked)
            ?? NavKey(g, g.CurrentMap, p.Pos, nearest.Pos, Empty);
    }

    private static readonly HashSet<Pos> Empty = new();

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
        g.InTalkMenu || g.InUnbindMenu || g.InThresholdMenu
        || g.InLayingMenu || g.InGearMenu || g.InSheetMenu; // the shrine and crossing menus are driven, not escaped.

    /// <summary>Which attribute to raise: keep Vigor and Might level, leaning to Vigor (staying alive) on a tie.</summary>
    private static char RaiseDigit(Player p)
    {
        Attr choice = p.Attributes[Attr.Vigor] <= p.Attributes[Attr.Might] ? Attr.Vigor : Attr.Might;
        return (char)('1' + (int)choice); // HandleShrineMenuKey maps '1'..'7' to Attr 0..6.
    }

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
