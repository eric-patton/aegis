namespace Aegis.Core;

/// <summary>The one calendar every country in a world shares (D-158).</summary>
public enum WorldSeason { Autumn, Winter, Spring, Summer }

/// <summary>The three independent local expressions of the shared season (D-158).</summary>
public enum ClimateBand { Lowlands, Road, Fells, Fens }

/// <summary>The four mechanical weather families shared by every climate band (D-158).</summary>
public enum WeatherFamily { Calm, Wet, Wind, Cold }

/// <summary>One exact place on the shared calendar.</summary>
public readonly record struct WeatherMoment(WorldSeason Season, int SeasonIndex, int Position);

/// <summary>
/// Deterministic weather hands and calendar arithmetic (D-158). Each band and
/// season index receives its own derived stream, so changing one country's
/// weights cannot move another country's weather.
/// </summary>
public static class WeatherCalendar
{
    public const int TicksPerSeason = 3;
    public const int CardsPerHand = 3;

    /// <summary>
    /// Reads an absolute coarse tick into the calendar. The opening autumn may
    /// begin before its regular three-card cadence because the established hard
    /// winter arrives on seed-drawn tick 3-5. Its first card holds through that
    /// lead, then the final three ticks use the full hand before winter lands.
    /// </summary>
    public static WeatherMoment AtTick(int coarseTick, int winterTick)
    {
        if (coarseTick < winterTick)
        {
            int lead = Math.Max(0, winterTick - TicksPerSeason);
            return new WeatherMoment(WorldSeason.Autumn, 0,
                Math.Clamp(coarseTick - lead, 0, CardsPerHand - 1));
        }

        int sinceWinter = coarseTick - winterTick;
        int seasonIndex = 1 + sinceWinter / TicksPerSeason;
        return new WeatherMoment(SeasonForIndex(seasonIndex), seasonIndex,
            sinceWinter % TicksPerSeason);
    }

    public static WorldSeason SeasonForIndex(int seasonIndex) => (seasonIndex % 4) switch
    {
        0 => WorldSeason.Autumn,
        1 => WorldSeason.Winter,
        2 => WorldSeason.Spring,
        _ => WorldSeason.Summer,
    };

    public static WeatherFamily Signature(WorldSeason season) => season switch
    {
        WorldSeason.Spring => WeatherFamily.Wet,
        WorldSeason.Summer => WeatherFamily.Calm,
        WorldSeason.Autumn => WeatherFamily.Wind,
        _ => WeatherFamily.Cold,
    };

    /// <summary>Builds one three-card hand from this band's own named stream.</summary>
    public static WeatherFamily[] Hand(ulong worldSeed, ClimateBand band, int seasonIndex)
    {
        string stream = band switch
        {
            ClimateBand.Lowlands => "weather_lowlands",
            ClimateBand.Road => "weather_road",
            ClimateBand.Fells => "weather_fells",
            _ => "weather_fens",
        };
        var rng = new Rng(SeedTree.Derive(worldSeed, stream, seasonIndex));
        var hand = new WeatherFamily[CardsPerHand];
        int signatureSlot = rng.Next(CardsPerHand);
        for (int i = 0; i < hand.Length; i++)
            hand[i] = i == signatureSlot
                ? Signature(SeasonForIndex(seasonIndex))
                : WeightedDraw(ref rng, band, SeasonForIndex(seasonIndex));
        return hand;
    }

    public static WeatherFamily At(ulong worldSeed, ClimateBand band, WeatherMoment moment) =>
        Hand(worldSeed, band, moment.SeasonIndex)[moment.Position];

    private static WeatherFamily WeightedDraw(ref Rng rng, ClimateBand band, WorldSeason season)
    {
        Span<int> weights = stackalloc int[4];
        switch (band)
        {
            case ClimateBand.Lowlands:
                weights[0] = 6; weights[1] = 5; weights[2] = 2; weights[3] = 2;
                break;
            case ClimateBand.Road:
                weights[0] = 3; weights[1] = 5; weights[2] = 5; weights[3] = 3;
                break;
            case ClimateBand.Fells:
                weights[0] = 2; weights[1] = 3; weights[2] = 5; weights[3] = 6;
                break;
            default:
                weights[0] = 2; weights[1] = 6; weights[2] = 6; weights[3] = 2;
                break;
        }

        // The season leans the two open draws without ever making a family
        // impossible. The signature slot remains the season's guarantee.
        weights[(int)Signature(season)] += 2;
        int roll = rng.Next(weights[0] + weights[1] + weights[2] + weights[3]);
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0) return (WeatherFamily)i;
        }
        return WeatherFamily.Calm;
    }

    /// <summary>The local name for one shared family.</summary>
    public static string Name(ClimateBand band, WeatherFamily family) => (band, family) switch
    {
        (ClimateBand.Lowlands, WeatherFamily.Calm) => "fair",
        (ClimateBand.Lowlands, WeatherFamily.Wet) => "rain",
        (ClimateBand.Lowlands, WeatherFamily.Wind) => "hard wind",
        (ClimateBand.Lowlands, WeatherFamily.Cold) => "frost",
        (ClimateBand.Road, WeatherFamily.Calm) => "clear",
        (ClimateBand.Road, WeatherFamily.Wet) => "rain",
        (ClimateBand.Road, WeatherFamily.Wind) => "crosswind",
        (ClimateBand.Road, WeatherFamily.Cold) => "cold",
        (ClimateBand.Fells, WeatherFamily.Calm) => "clear",
        (ClimateBand.Fells, WeatherFamily.Wet) => "wet mist",
        (ClimateBand.Fells, WeatherFamily.Wind) => "gale",
        (ClimateBand.Fens, WeatherFamily.Calm) => "still",
        (ClimateBand.Fens, WeatherFamily.Wet) => "driving rain",
        (ClimateBand.Fens, WeatherFamily.Wind) => "salt wind",
        (ClimateBand.Fens, WeatherFamily.Cold) => "fen frost",
        _ => "killing cold",
    };

    public static bool HalvesExposedCamp(WeatherFamily family) => family != WeatherFamily.Calm;
    public static bool SuppressesWildStep(WeatherFamily family) => family is WeatherFamily.Wet or WeatherFamily.Cold;
}
