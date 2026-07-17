namespace Aegis.Core;

/// <summary>
/// The Aegis speaks at deaths (D-019, Hades pattern). Slice carries only the
/// cycle-1 register: terse, functional, the motif planted from the first fall.
/// Line banks swap registers as the arc advances (design/story/aegis-arc.md sec 4).
/// </summary>
public static class AegisVoice
{
    private static readonly string[] DeathLinesRegisterOne =
    [
        "All is counted.",
        "Again, then. I have you.",
        "Rise. The wound is mine to carry a while.",
        "Not here. Not yet.",
        "I caught you. I will always catch you.",
    ];

    public const string FirstDeathLine = "Be still. I have you. ... All is counted.";

    public const string ForfeitLine = "What you left behind is lost. The world keeps what it takes twice.";

    public const string ReclaimLine = "Reclaimed. Nothing is wasted that returns.";

    public static string DeathLine(int deathCount)
        => deathCount <= 1 ? FirstDeathLine : DeathLinesRegisterOne[(deathCount - 2) % DeathLinesRegisterOne.Length];

    // Crossing lines (arc sec 5). The first crossing carries rung 1 of the reveal
    // ladder: there are other worlds, and this has happened before. Later crossings
    // reuse a holding line until their rungs are written.
    public const string FirstCrossingLine1 = "There are other worlds. This has happened before.";

    public const string FirstCrossingLine2 =
        "I have carried others. I do not remember how many. That frightens me, and I do not remember how to be frightened.";

    public const string LaterCrossingLine = "Deeper, then. Hold fast to me.";

    /// <summary>The Aegis's forge-name, recovered in cycle 2 (working placeholder, arc sec 11).</summary>
    public const string ForgeName = "Skeld";

    // Crossing rung "the failures" (arc sec 6, cycle 2): the stranger-kind named
    // for what they are, the two ways they are made, and the first admission of
    // guilt. Gated on the post-fight truth, spoken once ever.
    public static readonly string[] CrossingGuiltLines =
    [
        "The one at the fire. You have earned the whole of it, so hear it now, between worlds, where nothing else is listening.",
        "The stranger-kind are bearers. Mine, some of them. Some cut themselves loose. Some were dropped, when their ward broke, and went on falling without it.",
        "A ward that fails does not die of it. Its bearer does worse than die. Now you know what walks the deep worlds wearing faces like yours.",
    ];

    // Crossing rung "the forging" (arc sec 6, cycle 3): the ledger. The tithe
    // named, and the bearer's own grind recontextualized in one line. Gated on
    // the shrine vision, spoken once ever.
    public static readonly string[] CrossingLedgerLines =
    [
        "I said all is counted. Bearer, hear the part I could not remember: counted, and tithed.",
        "A share of every deed's weight has gone out of us at every crossing, down the chain, the whole time. Your essence, and mine. I do not yet remember to what.",
        "I did not lie to you. I did not remember. I am not certain which is worse.",
    ];

    // Crossing rung "the argument" (arc sec 6, cycle 4): the commission in full.
    // The vision's cut-off sentence is finished, the tithe's destination named,
    // and then the one thing no one built it to say. Gated on the Unbinder's
    // second reveal, spoken once ever.
    public static readonly string[] CrossingCommissionLines =
    [
        "The rest came back to me between that world and this one. The sentence I could not finish at the shrine ends like this: a soul fit to keep the Hearth.",
        "The Hearth is where worlds are kindled. Its keeping has stood empty since my makers' age ended, and an unkept fire still burns, only wrongly. You have walked the proof: every world deeper is a world crueler. The tithe I confessed has been feeding that fire all along, keeperless, like wood thrown into the dark.",
        "That is the commission. Find and temper a soul fit to keep it. That is what I am for, and what you are for, if the count says so.",
        "And here is the part no one forged into me. You may refuse. I will carry you either way.",
    ];

    public const string CoinConvertedLine = "Coin is of a world; it stays. The name it bought you, you keep.";

    public const string GateShutLine = "Not yet. This world's tally is unfinished.";
}
