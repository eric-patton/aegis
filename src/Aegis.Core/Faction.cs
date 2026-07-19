namespace Aegis.Core;

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
