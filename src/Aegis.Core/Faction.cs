namespace Aegis.Core;

/// <summary>
/// The ledgers kept on the bearer, keyed (D-078): D-023's per-faction dual
/// reputation begins here. Two factions so far, and one relationship between
/// them, the oldest one: the stead and the raiders that prey on it are standing
/// enemies, so a blow to one is a favor to the other (the camp emptied raises
/// the stead's regard and the raiders' wrath in the same stroke). A formal
/// relation matrix waits until a third faction gives it two edges to hold.
/// </summary>
public enum FactionId { Stead, Raiders }

/// <summary>
/// The home stead's regard for the bearer (D-076): the first rung of the faction
/// pillar (D-023), a local Fame earned only by deeds the stead can perceive and
/// reset at every crossing, because the folk are this world's and no other. It is
/// the deliberate opposite number to Legend's Standing: Standing is the songs of
/// all the worlds and carries between them; Regard is these folk, this valley,
/// this while. Rungs climb on a plain +2 step (1, 3, 5), so a world's two
/// perceivable deeds, the raids ended and the mound gone quiet, walk the whole
/// ladder. Nothing here is saved: like everything on the bearer it is rebuilt by
/// replay, and unlike the bearer it does not cross the waygate.
/// </summary>
public static class SteadRegard
{
    public const int MaxRung = 3;

    /// <summary>The rung at which the folk hold the bearer a friend (D-077): the welcome's threshold.</summary>
    public const int FriendRung = 2;

    /// <summary>The rung at which the stead holds the bearer its own (D-087): the teaching's threshold.</summary>
    public const int OwnRung = 3;

    /// <summary>Regard required for a rung: 1, 3, 5. A plain step, not a curve; the stead counts in deeds, not songs.</summary>
    public static int Threshold(int rung) => 2 * rung - 1;

    public static int RungFor(int regard)
    {
        int rung = 0;
        while (rung < MaxRung && regard >= Threshold(rung + 1)) rung++;
        return rung;
    }

    /// <summary>What the folk of the stead call the bearer, in their own plain words.</summary>
    public static string TitleOf(int regard) => RungFor(regard) switch
    {
        1 => "a known face here",
        2 => "a friend to the stead",
        3 => "the stead's own",
        _ => "",
    };
}

/// <summary>
/// The raiders' side of the ledger (D-078): the enemy faction's weighing of the
/// bearer, the Infamy-shaped half of D-023's dual reputation, kept by the folk
/// who have most cause to count. Wrath rises one notch for every raider slain,
/// rung by rung faster than the stead's regard (thresholds 1, 2, 4: hate
/// compounds where gratitude steps), and like the regard it is this world's
/// alone: the next world's dens have not met the bearer yet. At the dread rung
/// the ledger grows teeth the bearer can feel: raiders' blows come feared, and
/// land the weaker for it.
/// </summary>
public static class RaiderWrath
{
    public const int MaxRung = 3;

    /// <summary>The rung at which the raiders' blows start to falter (D-078): fear entering the work.</summary>
    public const int DreadRung = 2;

    /// <summary>Wrath required for a rung: 1, 2, 4. Hate compounds where gratitude steps.</summary>
    public static int Threshold(int rung) => rung <= 1 ? 1 : 2 * (rung - 1);

    public static int RungFor(int wrath)
    {
        int rung = 0;
        while (rung < MaxRung && wrath >= Threshold(rung + 1)) rung++;
        return rung;
    }

    /// <summary>What the dens call the bearer, in whatever tongue the dens keep.</summary>
    public static string TitleOf(int wrath) => RungFor(wrath) switch
    {
        1 => "a name the raiders curse",
        2 => "a dread on the raiders",
        3 => "the bane of the dens",
        _ => "",
    };

    /// <summary>
    /// A raider's damage roll under the dread (D-078): past the dread rung the
    /// blow is feared before it is thrown, and lands one point the weaker, never
    /// below one. Applied to the raw roll, before armor has its say, so the same
    /// draw leaves the dice either way and determinism holds.
    /// </summary>
    public static int Steadied(int wrath, int roll) =>
        RungFor(wrath) >= DreadRung ? Math.Max(1, roll - 1) : roll;
}

/// <summary>
/// The stead's suspicion of the bearer (D-086): the stead's own Infamy axis,
/// D-023's dual reputation completed on the home faction's side. It is earned
/// only one way so far, the first transgression verb: pilfering a house. The
/// stead keeps three doors, and in a place that small nothing taken stays
/// secret, so the ladder is the plainest in the game: one rung per door robbed
/// (thresholds 1, 2, 3). It runs beside the regard, never against it: a bearer
/// can be a friend to the stead and watched in it at once, and both titles say
/// so. Like every ledger it is this world's alone and rebuilt by replay. The
/// designed exit (D-023's rule) is restitution: coin left on the sill it was
/// taken from walks the ladder back down, door by door.
/// </summary>
public static class SteadShame
{
    public const int MaxRung = 3;

    /// <summary>The rung at which the folk stop extending a friend's terms (D-086): the purse and the price close.</summary>
    public const int UnwelcomeRung = 2;

    /// <summary>The rung at which the steadholder bars the larder (D-086): bread is not sold to a named thief.</summary>
    public const int BarredRung = 3;

    /// <summary>What one door's restitution costs: the loaf, and the trust, both paid for.</summary>
    public const int RepayCoin = 6;

    /// <summary>Shame required for a rung: 1, 2, 3. Three doors, three rungs; the stead counts its own.</summary>
    public static int Threshold(int rung) => rung;

    public static int RungFor(int shame)
    {
        int rung = 0;
        while (rung < MaxRung && shame >= Threshold(rung + 1)) rung++;
        return rung;
    }

    /// <summary>What the stead holds the bearer for it, in the words used at the well.</summary>
    public static string TitleOf(int shame) => RungFor(shame) switch
    {
        1 => "watched in this stead",
        2 => "unwelcome here",
        3 => "named a thief here",
        _ => "",
    };
}

/// <summary>
/// The raids themselves (D-079): the first coarse-tick faction event, D-023's
/// living-world half begun. While the camp stands, the raiders act: every tick
/// of turns they come down on the stead by night, each raid writing a fact,
/// narrated the moment it fires (the mandatory hook: no change the player
/// cannot perceive), and thinning the stead's stores so bread costs more for
/// the rest of the world. Clearing the camp is the designed exit condition
/// (D-023's no-eternal-stalemates rule); D-089 gave the tick its state
/// vectors, so what a raid takes rides the dens' boldness, the raids end on
/// their own dark exit when the lofts bare out, and a stead whose camp has
/// fallen recovers on the same tick.
/// </summary>
public static class SteadRaids
{
    /// <summary>Turns between faction moves: the coarse tick both vectors ride.</summary>
    public const int TickTurns = 160;
}

/// <summary>
/// The stead's stores (D-089): the home faction's first internal state axis,
/// the grain its season stands on. Raids drain it (an emboldened raid drains
/// double), bread's price rides it, and its floor is the raids' dark exit:
/// lofts bared to nothing leave nothing worth a night's ride. Once the camp
/// falls the stores recover a measure per tick until the lofts stand full,
/// each easing narrated as it lands, so ending the raids earns the stead its
/// season back rather than a frozen price. Per-world, replay-rebuilt.
/// </summary>
public static class SteadStores
{
    /// <summary>Full lofts: what a world's stead starts its season holding.</summary>
    public const int Max = 6;

    /// <summary>What a raid carries off; an emboldened raid takes double.</summary>
    public const int RaidTake = 1;
    public const int BoldRaidTake = 2;

    /// <summary>Bread's mark-up from thinned lofts: nothing while full, three coin at bare.</summary>
    public static int PriceBump(int stores) => (Max - stores + 1) / 2;
}

/// <summary>
/// The stead's levy (D-105): the home faction's first move of its own on the
/// tick. When the lofts run down to the last measure the stead calls a levy:
/// what grain is left is spoken for, the larder sells no bread while it
/// stands, and the steadholder takes the levy's answer instead: coin against
/// a measure carted in from whoever will sell to a hungry stead. Answering
/// is a deed the stead perceives, so it earns regard; the levy lifts when
/// the lofts climb clear again, by answers or by the season's own recovery.
/// </summary>
public static class SteadLevy
{
    /// <summary>Stores at or below this call the levy: the last measure is spoken for.</summary>
    public const int CalledAt = 1;

    /// <summary>Stores at or above this lift it: the larder opens again.</summary>
    public const int LiftedAt = 2;

    /// <summary>What one answered measure costs the bearer: grain bought dear and carted far.</summary>
    public const int AnswerCoin = 12;
}

/// <summary>
/// The stead's watch (D-105): the home faction's second move, posted the
/// morning after a raid comes greedy. While it stands the raiding nights are
/// met at the fold walls and turned away with nothing, but watchers must
/// eat, so the watch costs the lofts a measure a tick: protection now,
/// hunger later, and left standing long enough it can eat the stead bare
/// itself. It stands down when the dens' greed breaks (the cull), when the
/// camp falls, or when there is nothing left to feed it.
/// </summary>
public static class SteadWatch
{
    /// <summary>What a tick of standing watch eats from the lofts.</summary>
    public const int Upkeep = 1;
}

/// <summary>
/// The dens' boldness (D-089): the raider faction's internal state axis, and
/// deliberately a derived one: nights of unanswered plunder embolden the dens
/// and raiders slain cow them, so the axis is causal by construction and
/// rebuilt by replay for free. Below the raiding line the dens keep to their
/// dens (wrath's first faction-scale consequence: the fear that softens their
/// blows past the dread rung also holds the hills quiet); at the bold line
/// the raid comes greedy and carries off double.
/// </summary>
public static class RaiderBoldness
{
    /// <summary>Where a fresh world's dens start: bold enough to raid, not yet greedy.</summary>
    public const int Base = 3;

    /// <summary>Below this the dens hold to their dens: a cowed tick raids nothing.</summary>
    public const int RaidingAt = 2;

    /// <summary>At and past this the raid comes greedy: double grain carried off.</summary>
    public const int BoldAt = 4;

    /// <summary>Boldness as the causes stand: plunder emboldens, dead raiders cow.</summary>
    public static int Of(int raids, int wrath) => Math.Max(0, Base + raids - wrath);
}
