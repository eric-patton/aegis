namespace Aegis.Core;

/// <summary>
/// The hearth game (D-108): town life's first activity, a wagered cast of
/// knucklebones at the skald's hearth. Three bones, one throw back if you
/// dare it, high board takes the pot. The one real decision is informed by
/// numbers on the table (stand on a cast you can see, or sweep it up and
/// take what the second throw gives), and the house plays its odds plainly
/// and predictably, so the game can be read and played, not merely drawn.
/// Coin's first pure sink-or-swell that is not a shop; the stead talks when
/// the winnings run steep either way.
/// </summary>
public static class Knucklebones
{
    /// <summary>What a throw costs to sit down to: matched by the skald, winner takes both.</summary>
    public const int Stake = 3;

    /// <summary>The skald stands on this total or better; anything under is swept up and thrown again.</summary>
    public const int SkaldStandsAt = 11;

    /// <summary>Net winnings (either sign) at which a stead this small starts talking.</summary>
    public const int TalkedAboutAt = 9;
}
