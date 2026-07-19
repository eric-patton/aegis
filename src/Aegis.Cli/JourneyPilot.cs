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
/// D-057) with the loosed line instead of a chase that never closes. When a death leaves a
/// remnant behind, it walks back over the ground it fell on, the foes there down, and takes
/// its coin and Essence back rather than letting the next crossing forfeit them (D-065).
/// Every site it holds it strips before climbing out (D-066): it opens the chest for its
/// coin and the deep iron the smith never stocks (the barrow's blade, the hall's mail, the
/// ringfort's warbow, and their like), and it puts that iron on, wearing the best piece it
/// owns in each slot rather than leaving a stronger one dead in the pack. And as the
/// training settles it answers the sheet's own questions (D-067): at each skill's
/// threshold it takes the knack that pays for reading the fight the way it fights, the
/// deeper cut and the bitten wind-up and the shaft that finds a body mid-move, so what it
/// learns sharpens the very play that earned the lesson. A site it still cannot bring to
/// ground the runner budgets and writes off, handing the pilot a skip-set of the ones to
/// leave standing, so it engages, learns the tell, and moves on rather than spinning.
/// And it walks the arc's own ladder now (D-068): it seeks out the mender and the hermit
/// for the words that turn the reveal, rests for the vision, goes down the last stair to
/// answer the keeping, and then, face to face with a ward-dropped keeper, lays one down
/// gently and mends the next, so D-060's rarest grace is driven live by real keys instead
/// of stood in for by a hand-set flag. And at the arch itself it sets its own terms now
/// (D-069): rather than crossing plain it swears the three oaths its own way of playing
/// makes nearly free (the hungry road, the spent edge, the hushed name), lighting each with
/// its digit and then crossing, so it carries a real, honored burden the whole ladder down
/// for the Legend it buys, while refusing the four that would cost this bot blood or growth.
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
        // The pack is a menu we drive on purpose too (D-066): wear the best piece
        // owned, one digit at a time, then close. Chest loot lands here unworn
        // whenever the slot it wants was already filled at the forge.
        if (g.InGearMenu) return GearEquipDigit(g) ?? 'z';
        // The sheet is a menu we drive on purpose too (D-067): while a threshold
        // question stands open its digits answer it, so we take the preferred knack
        // and let the next question (if any) be put, then close. A standing question
        // always resolves to a real answer, never a close, so opening it can't loop.
        if (g.InSheetMenu) return KnackDigit(g) ?? 'z';
        // The keeping and the laying are menus we drive on purpose too (D-068). At the
        // Hearth the keeping question is answered for good (and the mercy road opens
        // behind it); face to face with a ward-dropped keeper the laying menu is where
        // the bearer lays it down, and later mends the one it is finally trusted to mend.
        if (g.InThresholdMenu) return ThresholdAnswer;
        if (g.InLayingMenu) return LayingDigit(g);
        if (StuckInMenu(g)) return 'z';

        // Wear the best iron in hand before anything else (D-066). A stronger piece
        // taken from a deep chest sits useless in the pack until it is put on, and
        // putting it on is a free glance down, no turn spent, so re-arm at once. It
        // only ever fires on cleared ground: the loot that fills the pack is taken
        // from a site already emptied of its foes, so this never pre-empts a dodge.
        if (BestPackUpgrade(g) is not null) return 'i';

        // Settle a threshold question the moment the training clicks (D-067). Like the
        // pack, the sheet costs no turn, so answering is free and the knack's edge rides
        // the very next blow; and because it is turn-free it is safe even mid-fight, no
        // turn passing means no stone falls while the bearer reads its own ledger.
        if (g.PendingKnack is not null) return 'c';

        if (g.Mode == MapMode.Site)
        {
            var site = g.CurrentSite!;
            // The last stair holds no foes (D-068): walk down to the Hearth to put the
            // keeping question (the step onto it opens the menu, answered above), then
            // climb back out once the answer is taken. It is never cleared and never in
            // the site loop, so the resolve-goal below is the only thing that comes here.
            if (site.Kind == SiteKind.Threshold)
                return ThresholdSiteMove(g, site);
            // Still work to do here: clear it. The wilds is hunted, not fought (D-070):
            // game flees, so the generic close-and-bump never catches it; the hunt loosens
            // a shaft at a hart on a clear line, or herds it into a corner. Otherwise climb
            // back to daylight.
            if (!site.Cleared && !skip.Contains(site.Id))
                return site.Kind == SiteKind.Wilds ? HuntMove(g) : FightOrApproach(g);
            // Held, foes down: open the site's own chest before leaving (D-066). It
            // holds coin and, in the deep sites, a piece of iron better than any the
            // smith draws, and it costs only the walk back over ground already won.
            if (site.Cleared && LootHere(g) is { } take) return take;
            // Take back a remnant a death left here before climbing out (D-065). Only
            // when the site is cleared, never one we gave up on: chasing coin through
            // the foes that beat us only courts the death that would forfeit it.
            if (site.Cleared && ReclaimHere(g) is { } grab) return grab;
            var ladder = site.EntryPos;
            if (p.Pos == ladder) return '<';
            // Head for the ladder; if live foes box the route, bump through them.
            return NavKey(g, g.CurrentMap, p.Pos, ladder, LiveFoeCells(g))
                ?? NavKey(g, g.CurrentMap, p.Pos, ladder, Empty);
        }

        // Overworld.
        // At the arch the bearer sets its own terms now (D-069): it swears the oaths its
        // own way of living already absorbs, lighting each with its digit, then crosses. The
        // toggle only ever adds a term here (it never presses a digit for one already lit),
        // so the set climbs to the sworn one and the cross fires exactly once, no oscillation.
        if (g.InCrossingMenu) return CrossingKey(g);

        // The vision is a rung of the ladder taken by resting (D-068): once the guilt has
        // been spoken at a crossing, the next rest at the shrine pulls the bearer under
        // into the forging-memory. So rest even with nothing to raise or mend when that
        // rung still waits, and seek the shrine out for it below.
        bool needVision = NeedVision(p);

        // Standing on the shrine with essence to spend or wounds to mend: rest and raise
        // before setting out (this also catches the shrine you wake on after a death).
        if (p.Pos == g.World.ShrinePos
            && (needVision || p.Essence >= g.NextRaiseCost || p.Hp < p.EffectiveMaxHp))
            return 'r';

        // Walk back to the shrine to spend essence on attributes whenever a raise is in
        // hand (or to rest for the vision). Arming thinned the deaths that used to wake us
        // on the shrine for free, so the raising has to be sought out now, or the bearer
        // crosses under-grown and the deep sites (the leaguer above all) ask more than it
        // can give. Returning between sites means it meets each in better skin than the last.
        if (needVision || p.Essence >= g.NextRaiseCost)
        {
            var toShrine = NavKey(g, g.World.Overworld, p.Pos, g.World.ShrinePos, OverworldBlocked(g));
            if (toShrine is not null) return toShrine;
        }

        // ---- the reveal ladder, walked by real feet (D-068) ----
        // Seek out the mender and the hermit when a rung waits on a word with them (the
        // vision named to the mender for tier 1; the two witnesses, the one at peace and
        // the one at cost, borne before tier 2). A bump opens the talk and the rung fires
        // on it, so the menu closes itself next tick. The target's own cell is the goal,
        // so the walk reaches it though every other person on the road stays blocked.
        if (ArcTalkTarget(g) is { } npc)
        {
            var toNpc = NavKey(g, g.World.Overworld, p.Pos, npc.Pos, OverworldBlocked(g));
            if (toNpc is not null) return toNpc;
        }

        // Once the commission is heard and the last stair stands in this world (tier 5+),
        // go down and answer the keeping before grinding the world out, so the keeper the
        // next hollow holds meets a resolved bearer and can be laid down, not only killed.
        if (p.CommissionHeard && p.Resolution == Resolution.None && g.World.ThresholdSite is { } stair)
        {
            if (p.Pos == stair.OverworldPos) return '>';
            var toStair = NavKey(g, g.World.Overworld, p.Pos, stair.OverworldPos, OverworldBlocked(g));
            if (toStair is not null) return toStair;
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

        // Every site is cleared or written off. Before the arch forfeits it, fetch a
        // remnant left somewhere safe to walk back into: out on the overworld, or down a
        // site already cleared of its foes (D-065). One left in a site we gave up on is
        // left to the dark; the coin is not worth re-entering ground that killed us.
        if (ReclaimDetour(g, skip) is { } reclaim) return reclaim;

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

    /// <summary>
    /// The hunt (D-070): the wilds hold fleeing game, not foes, so it is worked with the
    /// bow, not the fist. With a bow and a hart on a clear line within range, loose (the
    /// aim resolves to the hart next tick, the world frozen between the two keys). Else
    /// close on the nearest hart to bring it into range or push it into a corner where no
    /// step gains it distance and a bump ends it. Bowless, the chase still resolves: a hart
    /// is either cornered or driven out a run, so the glade always empties in the end.
    /// </summary>
    private static char? HuntMove(Game g)
    {
        var p = g.Player;
        var harts = g.LiveMonstersHere.Where(m => m.Kind == MonsterKind.Hart).ToList();
        if (harts.Count == 0) return '<';   // all taken or fled: climb out.

        if (p.Bow is not null)
        {
            var (damaging, _, _) = ScanRays(g);   // a hart on a clear line reads as a mark to loose at.
            if (damaging is not null && p.Stamina >= LooseCost(p)) return 'f';
        }

        var nearest = harts.OrderBy(m => Chebyshev(p.Pos, m.Pos)).First();
        if (Chebyshev(p.Pos, nearest.Pos) == 1)
            return KeyFor(Math.Sign(nearest.Pos.X - p.Pos.X), Math.Sign(nearest.Pos.Y - p.Pos.Y)); // cornered: a bump takes it.
        return NavKey(g, g.CurrentMap, p.Pos, nearest.Pos, LiveFoeCells(g))
            ?? NavKey(g, g.CurrentMap, p.Pos, nearest.Pos, Empty);
    }

    private static char? FightOrApproach(Game g)
    {
        var p = g.Player;
        // Dormant foes are included on purpose: a graven man or a warder is woken by
        // being neared or struck, so the bot walks up to it and bumps it awake.
        var foes = g.LiveMonstersHere.ToList();
        if (foes.Count == 0) return null;

        // Standing on our own remnant mid-fight: take it now (D-065). A death here would
        // forfeit it, so the sure coin in hand beats one more swing, unless a stone is due
        // on this very cell next turn, which the dodge below must answer first.
        if (g.Remnant is { } rem && g.CurrentMap.Id == rem.MapId && p.Pos == rem.Pos
            && !foes.Any(m => m.Intent is { } it && it.TargetCell == p.Pos && it.TurnsUntilResolve <= 1))
            return 'g';

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

    // ---- the reclaim (D-065): what a death drops, a life gets one chance to take back ----

    /// <summary>
    /// Take back a remnant lying on the map underfoot: walk onto it and grab it. A death
    /// drops the fallen's coin and Essence where it fell, and the next death or the crossing
    /// forfeits it for good (D-008), so a site held with its foes down is the safe ground to
    /// walk back over. Null when no remnant of worth lies on this map.
    /// </summary>
    private static char? ReclaimHere(Game g)
    {
        if (g.Remnant is not { } rem || rem.Coin + rem.Essence <= 0) return null;
        if (g.CurrentMap.Id != rem.MapId) return null;
        var p = g.Player;
        if (p.Pos == rem.Pos) return 'g';
        return NavKey(g, g.CurrentMap, p.Pos, rem.Pos, LiveFoeCells(g))
            ?? NavKey(g, g.CurrentMap, p.Pos, rem.Pos, Empty);
    }

    /// <summary>
    /// The move that fetches a remnant from off the current map, when it is safe to do so:
    /// out on the overworld, walked straight to; or down a site already cleared of foes,
    /// entered at its mouth so <see cref="ReclaimHere"/> takes it from the entry. A remnant
    /// in a site we gave up on, or one still tenanted, is left alone: the runner routes the
    /// bot into a live site to clear it anyway, and re-entering a site that beat us for coin
    /// only risks the death that forfeits the coin. Null when nothing is safely retrievable.
    /// </summary>
    private static char? ReclaimDetour(Game g, IReadOnlySet<string> skip)
    {
        if (g.Remnant is not { } rem || rem.Coin + rem.Essence <= 0) return null;
        var p = g.Player;

        if (g.World.Overworld.Id == rem.MapId)
        {
            if (p.Pos == rem.Pos) return 'g';
            return NavKey(g, g.World.Overworld, p.Pos, rem.Pos, OverworldBlocked(g));
        }

        var site = g.World.Sites.FirstOrDefault(s => s.Map.Id == rem.MapId);
        if (site is null || skip.Contains(site.Id) || !site.Cleared) return null;
        if (p.Pos == site.OverworldPos) return '>';
        return NavKey(g, g.World.Overworld, p.Pos, site.OverworldPos, OverworldBlocked(g));
    }

    // ---- the chest (D-066): the site's own prize, and wearing what it holds ----

    /// <summary>
    /// Walk onto the current site's chest and open it: coin, and in the deep sites a
    /// signature piece of iron the smith never stocks (D-041). Called only on a cleared
    /// site, so the ground to it is safe and the foes are down. Null when the chest is
    /// already open or there is none (the songhall and threshold carry none to take).
    /// </summary>
    private static char? LootHere(Game g)
    {
        if (g.CurrentSite is not { } site || site.ChestLooted) return null;
        var p = g.Player;
        if (p.Pos == site.ChestPos) return 'g';
        return NavKey(g, g.CurrentMap, p.Pos, site.ChestPos, LiveFoeCells(g))
            ?? NavKey(g, g.CurrentMap, p.Pos, site.ChestPos, Empty);
    }

    /// <summary>
    /// The piece sitting in the pack that ought to be worn: for each slot, the best-owned
    /// good is compared to what is worn there, and if the better one is in the pack it is
    /// handed back to be equipped. Ranked by the good it gives here and now (the effective
    /// bonus, which already halves an under-met or worn piece), then by raw bonus so a
    /// higher ceiling wins a tie and is worn early against the day the arm grows into it,
    /// then by stauncher wear and id for a stable order. Null when every slot already wears
    /// its best, which is also the signal to stop opening the pack. Cannot oscillate: the
    /// ranking shifts only toward the higher-req piece as attributes rise, and equipping is
    /// free, so it converges in one swap per slot and holds.
    /// </summary>
    private static GearItem? BestPackUpgrade(Game g)
    {
        var p = g.Player;
        foreach (var slot in Slots)
        {
            var best = p.AllGear
                .Where(it => it.Slot == slot)
                .OrderByDescending(it => it.EffectiveBonus(p.Attributes))
                .ThenByDescending(it => it.Bonus)
                .ThenByDescending(it => it.MaxWear)
                .ThenBy(it => it.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (best is not null && !ReferenceEquals(best, WornIn(p, slot)))
                return best;
        }
        return null;
    }

    /// <summary>The digit that wears <see cref="BestPackUpgrade"/> out of the open pack: its place in the gear list, which lists worn pieces first then the pack in order.</summary>
    private static char? GearEquipDigit(Game g)
    {
        if (BestPackUpgrade(g) is not { } item) return null;
        int idx = g.Player.AllGear.ToList().IndexOf(item);
        return idx >= 0 ? (char)('1' + idx) : null;
    }

    private static GearItem? WornIn(Player p, GearSlot slot) => slot switch
    {
        GearSlot.Weapon => p.Weapon,
        GearSlot.Ranged => p.Bow,
        _ => p.Armor,
    };

    // ---- the sheet's questions (D-067): the knacks that pay for reading the fight ----

    /// <summary>
    /// The knack to take from each threshold question (D-046), one rule for all ten:
    /// take the side that pays for reading the fight and pressing the attack, because
    /// that is the fight this pilot plays (it steps off the telegraph, strikes the body
    /// that is spoken for, finishes the weakest, and holds a line for the wind-up that
    /// drops a board). So at level 2 it takes the deeper blow over the spared wind or
    /// strap, and at level 4 the read moment (the extra bite into a blow already read)
    /// over the even hand that pays a little on every exchange (D-055). Two departures,
    /// both forced by how this bot lives: brawling takes the deep breath (+2 wind for
    /// good) over the harder bare fist, because it arms at the forge and never fights
    /// bare past the first camp, so the wind aids every blow and loose while the knuckle
    /// would die the moment it buys an axe; and the ranged pair (the hunter's eye, the
    /// picked moment) is the archer's own, sharpening the shafts this pilot leans on
    /// hardest against a leaguer's warders, the picked moment above all (it deepens a
    /// shot into a body mid-move, which is exactly the warder mid-whirl it looses at).
    /// </summary>
    private static readonly HashSet<PerkId> Preferred =
    [
        PerkId.DrawnCut,       // blades 2:   the cut goes 1 deeper.
        PerkId.FollowThrough,  // hafted 2:   a kill hands its wind back to fuel the next.
        PerkId.DeepBreath,     // brawling 2: +2 wind for good (the armed bearer's choice).
        PerkId.BracedShoulder, // warding 2:  turn the telegraphed blow 2 further.
        PerkId.HuntersEye,     // ranged 2:   the shaft strikes 1 deeper.
        PerkId.AnsweredCut,    // blades 4:   bite a wind-up already read, 2 deeper.
        PerkId.CheckedSwing,   // hafted 4:   land on the raised blow and break it.
        PerkId.CaughtArm,      // brawling 4: (moot, bare-handed) the read-moment side.
        PerkId.ShieldWall,     // warding 4:  hold the line when a site swarms.
        PerkId.PickedMoment,   // ranged 4:   +2 into a body mid-move, which is when we loose.
    ];

    /// <summary>
    /// The digit that answers the open threshold question with the preferred knack: its
    /// place among the two offered, one-indexed. Null only when no question stands, which
    /// is the signal to close the sheet. Every question lists one of its two sides in
    /// <see cref="Preferred"/>, so a standing question always yields a real answer and never
    /// a close, which is what keeps the opener from reopening what it just shut.
    /// </summary>
    private static char? KnackDigit(Game g)
    {
        if (g.PendingKnack is not { } q) return null;
        for (int i = 0; i < q.Options.Length; i++)
            if (Preferred.Contains(q.Options[i].Id))
                return (char)('1' + i);
        return '1'; // unreached: answer rather than close, so the opener cannot loop.
    }

    // ---- the arc: the reveal ladder, the keeping, the mending (D-068) ----

    /// <summary>Refuse the keeping (arc sec 8): the answer the full-playthrough test walks, and the mending reaches it and the keeping alike at one price (D-060).</summary>
    private const char ThresholdAnswer = '2';

    /// <summary>
    /// The vision rung is taken by resting once the guilt has been spoken at a crossing
    /// (D-068): the shrine pulls the bearer under into the forging-memory. True until the
    /// vision is seen, which is what makes the bot rest and seek the shrine out even with
    /// nothing to raise or mend.
    /// </summary>
    private static bool NeedVision(Player p) => p.CrossingGuiltHeard && !p.VisionSeen;

    /// <summary>
    /// The one on the road the arc wants a word with next, or null when no talking rung
    /// waits. Each gate is exactly the storylet's own (D-038, D-039), so a single bump
    /// turns the rung and clears the goal, and the bot never walks up to a face with
    /// nothing to say. Tier 2 is checked first, but it can only be ready once tier 1 has
    /// already fired, so the order only settles which of tier 1 and the hermit leads when
    /// both wait.
    /// </summary>
    private static Npc? ArcTalkTarget(Game g)
    {
        var p = g.Player;
        // The mender's second reveal: the two witnesses borne, and the first tier behind us.
        if (p.UnbinderRevealTier >= 1 && p.SeveredPeaceHeard && p.SeveredCostSeen && p.UnbinderRevealTier < 2)
            return g.World.Unbinder;
        // The mender's first reveal: the vision named to their face.
        if (p.VisionSeen && p.UnbinderRevealTier < 1)
            return g.World.Unbinder;
        // The one at peace: the hermit at the fire, once the ledger has been heard and one
        // keeps a fire in this world (tier-3-and-up worlds hold a hermit; below, the rung waits).
        if (p.LedgerHeard && !p.SeveredPeaceHeard && g.World.SeveredNpc is { } hermit)
            return hermit;
        return null;
    }

    /// <summary>
    /// The digit that answers the laying menu (D-060): mend the keeper the moment the
    /// bearer is trusted to (resolved, one already laid down, none yet mended), else lay it
    /// down gently. Laying clears the ring as a kill would but spends no blood, so post-
    /// resolution every keeper is met this way: the first laid down to earn the mending,
    /// the second mended, and any after laid down again rather than fought.
    /// </summary>
    private static char LayingDigit(Game g) => g.CanRestoreSevered ? '3' : '2';

    /// <summary>
    /// Down the last stair (D-068): make for the Hearth to put the keeping question (the
    /// step onto it opens the menu, answered above), then climb back to daylight once the
    /// answer is taken. No foes stand here, so navigation is plain and the fallbacks only
    /// ever end at the exit ladder.
    /// </summary>
    private static char? ThresholdSiteMove(Game g, Site site)
    {
        var p = g.Player;
        if (p.Resolution == Resolution.None && HearthPos(g.CurrentMap) is { } hearth)
        {
            if (p.Pos == hearth) return '.'; // on the stone: the menu is open, answered above.
            return NavKey(g, g.CurrentMap, p.Pos, hearth, Empty) ?? '<';
        }
        if (p.Pos == site.EntryPos) return '<';
        return NavKey(g, g.CurrentMap, p.Pos, site.EntryPos, Empty) ?? '<';
    }

    /// <summary>The Hearth cell of the last stair: the single such tile on its authored map, found by a scan (what a player sees on the floor).</summary>
    private static Pos? HearthPos(GameMap map)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map[new Pos(x, y)] == Terrain.Hearth) return new Pos(x, y);
        return null;
    }

    // ---- the terms of the crossing (D-069): the harder walking, freely taken ----

    /// <summary>
    /// The oaths the bearer swears at every arch (D-011, D-047): the three its own way of
    /// living renders nearly free, so the burden buys Legend and a louder echo (10 per weight,
    /// honored at the sworn world's far gate) without spiking the death toll the arc climb
    /// keeps low. It takes the hungry road (it heals by resting at the shrine, never by bought
    /// bread), the spent edge (it re-arms at every forge and loots deep iron from each site, so
    /// a blade that wears twice as fast is one it was replacing anyway), and the hushed name (it
    /// never leans on songs or standing for power, and the arc's rungs turn on the bearer's own
    /// carried flags, not on a song crossing the arch, so a silent world climbs the same ladder).
    /// It refuses the other four, each of which would cost this bot real blood or real growth:
    /// the crowded dark and the old blood put more death in every den (the old blood the heaviest
    /// oath there is, a whole weight of 2), the slow mending lets a wound its step-off fight still
    /// takes linger and compound, and the lean dark would halve the essence its growth, and so its
    /// survival at depth, is fed by. The result is a real, honored burden carried the whole ladder
    /// down while the mending still lands and the deaths stay where D-068 left them.
    /// </summary>
    private static readonly OathId[] SwornOaths =
        { OathId.HungryRoad, OathId.SpentEdge, OathId.HushedName };

    /// <summary>
    /// The next key at the open arch (D-069): light each sworn oath not yet lit (its digit is
    /// its one-indexed place in the catalog, the very index the terms menu toggles by), then
    /// cross. Because a digit only ever adds a term here, the chosen set climbs monotonically
    /// to <see cref="SwornOaths"/> and the '>' fires exactly once, so the menu cannot loop.
    /// </summary>
    private static char CrossingKey(Game g)
    {
        foreach (var oath in SwornOaths)
            if (!g.ChosenOaths.Contains(oath))
                return (char)('1' + OathIndex(oath));
        return '>';
    }

    /// <summary>The one-indexed digit's index for an oath: its place in the catalog, which is what <see cref="Game"/>'s terms menu keys off (key - '1').</summary>
    private static int OathIndex(OathId oath)
    {
        for (int i = 0; i < OathCatalog.All.Count; i++)
            if (OathCatalog.All[i].Id == oath) return i;
        return 0; // unreached: every sworn oath is in the catalog.
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
        // A reveal talk is opened for its rung then closed here next tick; the respec is
        // never wanted, and the bench (D-071) the bot has no errand at. The shrine, smith,
        // aim, pack, sheet, keeping, and laying menus are all driven above, not escaped.
        g.InTalkMenu || g.InUnbindMenu || g.InTradeMenu;

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
