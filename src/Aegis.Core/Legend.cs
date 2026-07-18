namespace Aegis.Core;

/// <summary>
/// The Legend meta-layer's first rungs (D-011, D-048). Legend itself stays the
/// only state: minted at crossings, never spent. Standing is derived from it on
/// a square curve, so every rung costs more Legend than the last: diminishing
/// returns by construction, at the research's bounding exponent. Boons read
/// standing where they apply; nothing here is saved, reset, or death-handled.
/// </summary>
public static class LegendStanding
{
    public const int MaxStanding = 5;

    /// <summary>Legend required for a standing: 25, 100, 225, 400, 625.</summary>
    public static int Threshold(int standing) => 25 * standing * standing;

    public static int StandingFor(int legend)
    {
        int standing = 0;
        while (standing < MaxStanding && legend >= Threshold(standing + 1)) standing++;
        return standing;
    }

    /// <summary>What the songs call the bearer, in the songs' own words.</summary>
    public static string TitleOf(int standing) => standing switch
    {
        1 => "a name in one song",
        2 => "a name at the hearths",
        3 => "a storied bearer",
        4 => "a walking song",
        5 => "the songs' own",
        _ => "",
    };
}
