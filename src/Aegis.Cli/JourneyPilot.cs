using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// The autopilot that plays the ladder (D-062). Given a live <see cref="Game"/>, it
/// decides the one key to press next, reading only what a player could read: position,
/// the map, live monsters and their telegraphed intents, HP and stamina. The loop, world
/// by world: clear every tenanted site in reach (the camp that gates the arch, and the
/// barrow, hollow, quarry, hall, ringfort, and leaguer besides), then walk to the arch
/// and cross. Along the way it arms itself (D-064): with coin from the dark it buys what
/// the smith stocks and a slot of its own is still bare, armor first for the staying
/// power the deep sites ask, then an edge, then the hunting bow. It fights the way D-004
/// says the game can be fought: never stand on the aimed cell, strike the body that is
/// spoken for, and answer a foe that stands off (the sling-warder on the leaguer's works,
/// D-057) with the loosed line instead of a chase that never closes. A site it still
/// cannot bring to ground the runner budgets and writes off, handing the pilot a skip-set
/// of the ones to leave standing, so it engages, learns the tell, and moves on rather
/// than spinning.
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
        // The smith's trade is a talk menu we drive on purpose: buy what a bare slot and
        // the purse both allow, then leave (D-064). The aim is the bow's second key
        // (D-050), sent along whatever line bears a target.
        if (g.InTalkMenu && g.TalkNpc?.Kind == NpcKind.Smith)
            return SmithBuyDigit(g) ?? 'z';
        if (g.InAim) return AimDirection(g) ?? 'z';
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

        // Walk back to the shrine to spend essence on attributes whenever a raise is in
        // hand. Arming thinned the deaths that used to wake us on the shrine for free, so
        // the raising has to be sought out now, or the bearer crosses under-grown and the
        // deep sites (the leaguer above all) ask more than it can give. Returning between
        // sites means it meets each in better skin than the last.
        if (p.Essence >= g.NextRaiseCost)
        {
            var toShrine = NavKey(g, g.World.Overworld, p.Pos, g.World.ShrinePos, OverworldBlocked(g));
            if (toShrine is not null) return toShrine;
        }

        // Arm at the smith before taking the next site, whenever a slot of ours is bare
        // and the coin is in hand (D-064). Buying fills the empty slot on the spot, and
        // the iron rides on the bearer through death and every crossing, so a world or
        // two of the dark's coin leaves us shod for the deep tiers.
        if (SmithBestBuy(g) is not null)
        {
            var toSmith = NavKey(g, g.World.Overworld, p.Pos, g.World.Smith.Pos, OverworldBlocked(g));
            if (toSmith is not null) return toSmith;
        }

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

        // A sling-warder keeps its distance and cannot be run down on foot (D-057). If we
        // carry a bow, answer it in kind rather than chasing a retreat that never ends;
        // without one, fall through and corner it against the water as best we can.
        if (p.Bow is not null
            && foes.Any(m => m.Kind == MonsterKind.Warder && Chebyshev(p.Pos, m.Pos) > 1)
            && BowMove(g, foes) is { } shot)
            return shot;

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

    // ---- the loosed line (D-050): the bow answers what the fist cannot reach ----

    private const int LooseBase = 3; // a swing's price in wind; the unmet Grace taxes it one more.

    private static int LooseCost(Player p) =>
        LooseBase + (p.Bow!.MeetsReq(p.Attributes) ? 0 : 1);

    /// <summary>
    /// The bow half of the fight against a leaguer's warders. Reads the eight lines out of
    /// the bearer's cell as a shaft would fly them and: looses at a body a shaft would bite,
    /// or once to wake a dormant board (the sighting that sounds the horn, D-057); else, a
    /// stone due on our own cell next turn is stepped out from under; else, if the ground is
    /// safe enough and a warder stands on a line, holds for the board to drop; else relocates
    /// to a better firing cell. Null when no warder can be reached or shot at all, so the
    /// caller falls back to the chase (and, failing that, the runner writes the site off).
    /// </summary>
    private static char? BowMove(Game g, List<Monster> foes)
    {
        var p = g.Player;
        if (p.Bow is null) return null;
        int cost = LooseCost(p);

        var (damaging, rousing, hasLine) = ScanRays(g);

        // Loose the moment a shaft would bite, wind allowing; a leaguer is cleared a
        // warder at a time, and at the eye's strength a body falls in a shot or two.
        if (damaging is not null && p.Stamina >= cost) return 'f'; // a shaft that bites
        if (rousing is not null && p.Stamina >= cost) return 'f';  // a shaft that wakes them

        // No shot this breath. A lofted stone falls on the ground it was marked to, two
        // turns after the whirl (D-057), so a stone due to land on us next turn is
        // stepped out from under first of all.
        bool landsNext = foes.Any(m => m.Intent is { } it
            && it.TargetCell == p.Pos && it.TurnsUntilResolve <= 1);
        if (landsNext) return ChooseDodge(g, foes) ?? LineUpStep(g, foes);

        // Hold a shooting line to catch the next wind-up (the board drops the whole time
        // a warder whirls, and a line within range always winds up in time), gathering
        // wind while we wait. But only while few enough slings bear on this ground to
        // outlast: under a heavier fan, relocate toward a thinner arc of the ring so the
        // watch falls a cluster at a time instead of all at once.
        int fan = foes.Count(m => m.Kind == MonsterKind.Warder && Chebyshev(p.Pos, m.Pos) <= Game.LoftRange);
        if (hasLine && fan <= SafeFan && p.Stamina >= cost) return '.';
        return LineUpStep(g, foes);
    }

    /// <summary>The line to loose along once the shaft is set: the same choice that set it, the world being frozen between the two keys.</summary>
    private static char? AimDirection(Game g)
    {
        var (damaging, rousing, _) = ScanRays(g);
        return damaging ?? rousing; // null lowers the bow and keeps the shaft (a safe no-op).
    }

    /// <summary>
    /// What each of the eight lines out of the bearer's cell would meet, read as the shaft
    /// reads them: the first body within range with only open ground before it. Returns
    /// the line to a body a shaft would wound (the weakest, to finish what is started), the
    /// line to a dormant board a shaft would wake, and whether any warder stands on a clear
    /// line at all (a shot worth holding for).
    /// </summary>
    private static (char? Damaging, char? Rousing, bool HasLine) ScanRays(Game g)
    {
        var p = g.Player;
        var map = g.CurrentMap;
        char? damaging = null;
        int weakest = int.MaxValue;
        char? rousing = null;
        bool hasLine = false;

        foreach (var (dx, dy) in Directions.All8)
        {
            var pos = p.Pos;
            for (int step = 0; step < Game.BowRange; step++)
            {
                pos = pos.Plus(dx, dy);
                if (!map.Walkable(pos)) break; // the shaft splinters on stone: no target this way.
                var foe = g.Monsters.FirstOrDefault(
                    m => m.Alive && m.SiteId == g.CurrentSite!.Id && m.Pos == pos);
                if (foe is null) continue; // open ground: the shaft flies on.

                // The first body on the line is what a shaft strikes. A carl's or a
                // warder's board turns it (no wound) unless its bearer is mid-cast or
                // blown open; a dormant board is woken by the sighting alone (D-057).
                bool board = foe.Kind is MonsterKind.Carl or MonsterKind.Warder
                             && foe.Intent is null && foe.ExposedTurns == 0;
                if (foe.Kind == MonsterKind.Warder) hasLine = true;
                if (!board)
                {
                    if (foe.Hp < weakest) { weakest = foe.Hp; damaging = KeyFor(dx, dy); }
                }
                else if (foe.Dormant)
                {
                    rousing ??= KeyFor(dx, dy);
                }
                break; // the shaft stops at the first body, wound or turned.
            }
        }
        return (damaging, rousing, hasLine);
    }

    /// <summary>
    /// Moves toward a place to shoot the nearest warder from. Greedy stepping is no use
    /// around the mere, where only a shot straight along a bank clears the water; so this
    /// picks a firing cell (a clear line to the target at a standoff, under the fewest
    /// other slings) and paths to it with the same breadth-first walk the rest of the
    /// pilot uses. Once standing on it, it holds for the wind-up that drops the board.
    /// Null only when no warder can be reached or shot at all, handing the fight back.
    /// </summary>
    private static char? LineUpStep(Game g, List<Monster> foes)
    {
        var p = g.Player;
        var map = g.CurrentMap;
        var warders = foes.Where(m => m.Kind == MonsterKind.Warder).ToList();
        if (warders.Count == 0)
        {
            var nearest = foes.OrderBy(m => Chebyshev(p.Pos, m.Pos)).First().Pos;
            return NavKey(g, map, p.Pos, nearest, Empty);
        }

        var target = warders
            .OrderBy(m => Chebyshev(p.Pos, m.Pos)).ThenBy(m => m.Pos.X).ThenBy(m => m.Pos.Y).First();

        // The cell to loose from: a clear bank-line to the target, under the fewest slings,
        // and nearest to hand on a tie. Building it outward from the target over open
        // ground guarantees the shaft's own line back is clear (it walked the same cells).
        Pos? bestCell = null;
        int bestScore = int.MaxValue;
        foreach (var cell in FiringCells(map, target.Pos))
        {
            int exposure = warders.Count(m => Chebyshev(cell, m.Pos) <= Game.LoftRange);
            int score = exposure * 1000 + Chebyshev(p.Pos, cell);
            if (score < bestScore) { bestScore = score; bestCell = cell; }
        }

        if (bestCell is { } fc)
        {
            if (p.Pos == fc) return '.'; // in position: hold for the board to drop.
            var blocked = foes.Select(m => m.Pos).ToHashSet();
            return NavKey(g, map, p.Pos, fc, blocked) ?? NavKey(g, map, p.Pos, fc, Empty);
        }

        // Nowhere bears a clear shot: close on the target to rouse it or force a corner.
        return NavKey(g, map, p.Pos, target.Pos, Empty);
    }

    /// <summary>
    /// The cells a shaft could strike <paramref name="target"/> from: stepped outward along
    /// each of the eight lines over open ground (a wall or the mere ends that line), kept a
    /// standoff or more away so the warder lofts instead of backing off. Each has a clear
    /// line to the target by construction, being the very ground the shaft would cross.
    /// </summary>
    private static IEnumerable<Pos> FiringCells(GameMap map, Pos target)
    {
        foreach (var (dx, dy) in Directions.All8)
        {
            var pos = target;
            for (int step = 1; step <= Game.BowRange; step++)
            {
                pos = pos.Plus(dx, dy);
                if (!map.Walkable(pos)) break; // beyond here the shaft would splinter.
                if (step >= StandoffMin) yield return pos;
            }
        }
    }

    private const int StandoffMin = 3; // inside this a warder retreats rather than casts.
    private const int SafeFan = 2;     // slings we will stand and trade with; more, and we move on.

    // ---- the smith's trade (D-064): coin from the dark into iron of our own ----

    private static readonly GearSlot[] Slots = { GearSlot.Armor, GearSlot.Weapon, GearSlot.Ranged };

    /// <summary>
    /// The stock piece worth buying now: a bare slot of ours, a price the purse can meet,
    /// and the strongest good for that slot, taking armor before an edge and an edge before
    /// the bow. Null when nothing is both wanted and affordable, which is also the signal
    /// to stop detouring to the forge.
    /// </summary>
    private static string? SmithBestBuy(Game g)
    {
        var p = g.Player;
        foreach (var slot in Slots)
        {
            if (!SlotEmpty(p, slot)) continue;
            string? pick = null;
            int bestScore = int.MinValue;
            foreach (var id in GearCatalog.SmithStock)
            {
                var item = GearCatalog.Create(id);
                if (item.Slot != slot || p.OwnsGear(id) || p.Coin < item.Value) continue;
                int score = item.EffectiveBonus(p.Attributes) * 1000 - item.Value; // best good, cheaper on a tie
                if (score > bestScore) { bestScore = score; pick = id; }
            }
            if (pick is not null) return pick;
        }
        return null;
    }

    private static bool SlotEmpty(Player p, GearSlot slot) => slot switch
    {
        GearSlot.Weapon => p.Weapon is null,
        GearSlot.Ranged => p.Bow is null,
        _ => p.Armor is null,
    };

    /// <summary>
    /// The digit that buys <see cref="SmithBestBuy"/> out of the open trade menu. The
    /// smith's offers never shift under a buyer's fingers (D-041), so the digit is simply
    /// the topic count plus the offer's place in the list.
    /// </summary>
    private static char? SmithBuyDigit(Game g)
    {
        var id = SmithBestBuy(g);
        if (id is null) return null;
        for (int i = 0; i < g.Offers.Count; i++)
            if (g.Offers[i].Good == TradeGood.Gear && g.Offers[i].Arg == id)
                return (char)('1' + g.Topics.Count + i);
        return null;
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

    /// <summary>
    /// Which attribute to raise. Bare-handed, keep Vigor and Might level (staying alive,
    /// then hitting harder). Once a bow is on the shoulder the eye earns its keep, so
    /// Grace joins the rotation: it is what makes a shaft bite (D-050) and what slips a
    /// lofted stone (D-057), and the leaguer asks for both. Wits is left alone throughout,
    /// so the read goes on dulling across the crossings for the report to show (D-061).
    /// </summary>
    private static char RaiseDigit(Player p)
    {
        Attr choice = p.Bow is not null
            ? Lowest(p, Attr.Vigor, Attr.Might, Attr.Grace)
            : Lowest(p, Attr.Vigor, Attr.Might);
        return (char)('1' + (int)choice); // HandleShrineMenuKey maps '1'..'7' to Attr 0..6.
    }

    /// <summary>The lowest of the listed attributes, ties kept in listed order (Vigor first, then Might, then Grace).</summary>
    private static Attr Lowest(Player p, params Attr[] attrs)
    {
        Attr best = attrs[0];
        foreach (var a in attrs)
            if (p.Attributes[a] < p.Attributes[best]) best = a;
        return best;
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
