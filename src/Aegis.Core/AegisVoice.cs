namespace Aegis.Core;

/// <summary>
/// The Aegis speaks at deaths (D-019, Hades pattern). Death lines carry register,
/// never plot (arc sec 4): register one is terse and functional, register two
/// (after the ledger) personal and worried, the final register (after the
/// threshold) candid between equals. The motif rides all three, meaning shifting.
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

    private static readonly string[] DeathLinesRegisterTwo =
    [
        "Down again. I have you. I will always have you, and I no longer know if that is the best of me or the worst.",
        "Caught. ...Forgive the delay. I flinched, remembering the ones I did not catch.",
        "All is counted. I say it, and now we both hear the second meaning under it.",
        "Rise. The wound is mine a while. The rest of the weight was always mine.",
        "Not here. Not this deep. Not you.",
    ];

    private static readonly string[] DeathLinesRegisterFinal =
    [
        "Up you get. The count is yours; I am only the arithmetic.",
        "Caught you. I would say I always will, but you know that now. It is not a vow anymore, only a fact.",
        "That one was your own doing, and I say so as a friend.",
        "All is counted, and none of it against you. Rise.",
        "Again? Well. I have carried worse, and I am not telling you who.",
    ];

    public const string FirstDeathLine = "Be still. I have you. ... All is counted.";

    public const string ForfeitLine = "What you left behind is lost. The world keeps what it takes twice.";

    public const string ReclaimLine = "Reclaimed. Nothing is wasted that returns.";

    /// <summary>Spoken when the Death's Toll converts (D-098): the motif's cost made literal.</summary>
    public const string ScarLine = "I caught you. I did not catch all of you. All is counted, and this is what the counting costs.";

    /// <summary>Spoken when a scar's cure road is walked to its end (D-098 stage 2).</summary>
    public const string ScarMendedLine = "Parity. All is counted, and this once, the count gives back.";

    /// <summary>Spoken once, over the first shade ever called (D-099).</summary>
    public const string CallingLine = "Counted, but not among the living. Keep it where you can see it, bearer: what was never whole is nothing I can catch.";

    public static string DeathLine(int deathCount, int register = 1)
    {
        if (deathCount <= 1) return FirstDeathLine;
        var bank = register switch
        {
            >= 3 => DeathLinesRegisterFinal,
            2 => DeathLinesRegisterTwo,
            _ => DeathLinesRegisterOne,
        };
        return bank[(deathCount - 2) % bank.Length];
    }

    // Crossing lines (arc sec 5). The first crossing carries rung 1 of the reveal
    // ladder: there are other worlds, and this has happened before. Later crossings
    // reuse a holding line until their rungs are written.
    public const string FirstCrossingLine1 = "There are other worlds. This has happened before.";

    public const string FirstCrossingLine2 =
        "I have carried others. I do not remember how many. That frightens me, and I do not remember how to be frightened.";

    public const string LaterCrossingLine = "Deeper, then. Hold fast to me.";

    /// <summary>The Aegis's forge-name, recovered in cycle 2 (canon per D-043).</summary>
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

    // Post-resolution crossings (arc sec 9): the steady state's final register.
    // The two lines differ in fiction only; the crossing itself is unchanged.
    public const string KeptCrossingLine =
        "Deeper, then. The crossing is the keeping, keeper: every world we finish is wood on the fire.";

    public const string RefusedCrossingLine =
        "Deeper, then, on no one's errand but ours. Hold fast to me; I hold fast back.";

    public const string CoinConvertedLine = "Coin is of a world; it stays. The name it bought you, you keep.";

    public const string GateShutLine = "Not yet. This world's tally is unfinished.";
}
