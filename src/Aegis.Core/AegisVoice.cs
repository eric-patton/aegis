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

    public const string CoinConvertedLine = "Coin is of a world; it stays. The name it bought you, you keep.";

    public const string GateShutLine = "Not yet. This world's tally is unfinished.";
}
