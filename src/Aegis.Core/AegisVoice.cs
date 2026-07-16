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
}
