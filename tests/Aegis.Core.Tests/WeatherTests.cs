using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>The shared calendar, three climate bands, and seasonal events (D-158).</summary>
public class WeatherTests
{
    [Fact]
    public void TheCalendar_OpensInAutumn_UsesTheSeededWinter_AndLoops()
    {
        var game = QuietGame(42);
        Assert.Equal(WorldSeason.Autumn, game.Season);
        Assert.Equal(0, game.SeasonPosition);
        Assert.InRange(game.WinterDueTick, 3, 5);

        WaitTicks(game, game.WinterDueTick);
        Assert.Equal(WorldSeason.Winter, game.Season);
        Assert.Equal(0, game.SeasonPosition);
        WaitTicks(game, 3);
        Assert.Equal(WorldSeason.Spring, game.Season);
        WaitTicks(game, 3);
        Assert.Equal(WorldSeason.Summer, game.Season);
        WaitTicks(game, 3);
        Assert.Equal(WorldSeason.Autumn, game.Season);
        WaitTicks(game, 3);
        Assert.Equal(WorldSeason.Winter, game.Season);
        Assert.Contains(game.World.Facts.All, f => f.Type == "season" && f.Subject.StartsWith("winter_"));
    }

    [Fact]
    public void EveryHand_IsDeterministic_Independent_AndKeepsItsSignature()
    {
        bool repeatSeen = false;
        for (ulong seed = 1; seed <= 80; seed++)
            for (int seasonIndex = 0; seasonIndex < 8; seasonIndex++)
                foreach (var band in Enum.GetValues<ClimateBand>())
                {
                    var a = WeatherCalendar.Hand(seed, band, seasonIndex);
                    var b = WeatherCalendar.Hand(seed, band, seasonIndex);
                    Assert.Equal(a, b);
                    Assert.Contains(WeatherCalendar.Signature(WeatherCalendar.SeasonForIndex(seasonIndex)), a);
                    repeatSeen |= a[0] == a[1] || a[1] == a[2];
                }

        Assert.True(repeatSeen, "weighted open slots permit a front to persist");
        Assert.NotEqual(
            WeatherCalendar.Hand(42, ClimateBand.Lowlands, 3),
            WeatherCalendar.Hand(42, ClimateBand.Road, 3));
        Assert.NotEqual(
            WeatherCalendar.Hand(42, ClimateBand.Road, 3),
            WeatherCalendar.Hand(42, ClimateBand.Fells, 3));
    }

    [Fact]
    public void TheForecast_IsExactlyTheNextTick_AcrossEveryTransition()
    {
        var game = QuietGame(7);
        for (int tick = 0; tick < 18; tick++)
        {
            var expected = Enum.GetValues<ClimateBand>()
                .ToDictionary(b => b, game.ForecastAt);
            WaitTicks(game, 1);
            foreach (var band in Enum.GetValues<ClimateBand>())
                Assert.Equal(expected[band], game.WeatherAt(band));
        }
    }

    [Fact]
    public void BroadWeatherEvaluation_CoversEveryFamily_AndShowsRegionalBias()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (ulong seed = 1; seed <= 200; seed++)
            foreach (var kv in WorldEval.WeatherCoverage(seed))
                counts[kv.Key] = counts.GetValueOrDefault(kv.Key) + kv.Value;

        foreach (var band in Enum.GetValues<ClimateBand>())
            foreach (var family in Enum.GetValues<WeatherFamily>())
                Assert.True(counts[$"{band.ToString().ToLowerInvariant()}:{family.ToString().ToLowerInvariant()}"] > 0);

        int Count(ClimateBand b, WeatherFamily f) =>
            counts[$"{b.ToString().ToLowerInvariant()}:{f.ToString().ToLowerInvariant()}"];
        Assert.True(Count(ClimateBand.Lowlands, WeatherFamily.Calm) + Count(ClimateBand.Lowlands, WeatherFamily.Wet)
            > Count(ClimateBand.Lowlands, WeatherFamily.Wind) + Count(ClimateBand.Lowlands, WeatherFamily.Cold));
        Assert.True(Count(ClimateBand.Road, WeatherFamily.Wet) + Count(ClimateBand.Road, WeatherFamily.Wind)
            > Count(ClimateBand.Road, WeatherFamily.Calm) + Count(ClimateBand.Road, WeatherFamily.Cold));
        Assert.True(Count(ClimateBand.Fells, WeatherFamily.Wind) + Count(ClimateBand.Fells, WeatherFamily.Cold)
            > Count(ClimateBand.Fells, WeatherFamily.Calm) + Count(ClimateBand.Fells, WeatherFamily.Wet));
    }

    [Fact]
    public void Wind_AllowsTheWildStep_ButHalvesAnExposedCamp()
    {
        var game = new Game(42);
        TakeRoad(game);
        game.Debug_SetWeather(ClimateBand.Road, WeatherFamily.Wind);
        game.Player.Stamina = 1;
        StepOntoPlain(game, game.World.Road);
        Assert.Equal(2, game.Player.Stamina);

        game.Debug_HurtPlayer(10);
        game.Player.Rations = 1;
        int max = game.Player.EffectiveMaxHp;
        game.ApplyKey('m');
        Assert.Equal(max - 7, game.Player.Hp);
    }

    [Fact]
    public void Lowlands_KeepTheStep_ButWeatherStillTouchesTheOpenCamp()
    {
        var game = new Game(42);
        game.Debug_SetWeather(ClimateBand.Lowlands, WeatherFamily.Wet);
        game.Debug_SetPlayerPos(OpenPlain(game, game.World.Overworld));
        game.Player.Stamina = 1;
        StepOntoPlain(game, game.World.Overworld);
        Assert.Equal(2, game.Player.Stamina);

        game.Debug_HurtPlayer(10);
        game.Player.Rations = 1;
        int max = game.Player.EffectiveMaxHp;
        game.ApplyKey('m');
        Assert.Equal(max - 7, game.Player.Hp);

        game.Debug_SetWeather(ClimateBand.Lowlands, WeatherFamily.Cold);
        game.Player.Rations = 0;
        game.Player.RawMeat = 0;
        int turn = game.Turn;
        game.ApplyKey('m');
        Assert.Equal(turn + RoadLife.CampTurns, game.Turn);
    }

    [Fact]
    public void TheSeasonDeck_UsesTheApprovedGates()
    {
        var game = new Game(42);
        game.Debug_SetStores(3);

        game.Debug_SetSeason(WorldSeason.Autumn);
        Assert.True(game.Debug_SteadEventEligible("drovers"));
        Assert.True(game.Debug_SteadEventEligible("season_bargain"));
        Assert.False(game.Debug_SteadEventEligible("fords_washout"));

        game.Debug_SetSeason(WorldSeason.Spring);
        game.Debug_SetWeather(ClimateBand.Lowlands, WeatherFamily.Cold);
        Assert.True(game.Debug_SteadEventEligible("far_fields"));
        Assert.True(game.Debug_SteadEventEligible("fords_washout"));
        Assert.True(game.Debug_SteadEventEligible("late_frost"));
        Assert.False(game.Debug_SteadEventEligible("wedding"));

        game.Debug_SetSeason(WorldSeason.Summer);
        game.Debug_SetWeather(ClimateBand.Lowlands, WeatherFamily.Calm);
        Assert.True(game.Debug_SteadEventEligible("far_fields"));
        Assert.True(game.Debug_SteadEventEligible("haying_days"));
        Assert.True(game.Debug_SteadEventEligible("wedding"));
        Assert.False(game.Debug_SteadEventEligible("drovers"));
    }

    [Fact]
    public void HayingAndLateFrost_MoveStores_Levy_AndGranary()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Debug_SetStores(2);
        game.Debug_DrawSteadEvent("late_frost");
        Assert.Equal(1, game.Stores);
        Assert.True(game.LevyStands);
        game.Debug_DrawSteadEvent("haying_days");
        Assert.Equal(2, game.Stores);
        Assert.False(game.LevyStands);

        var held = new Game(42);
        held.Player.Coin = SteadFacilities.GranaryCoin;
        Fund(held, "granary");
        held.Debug_SetStores(2);
        held.Debug_DrawSteadEvent("late_frost");
        Assert.Equal(2, held.Stores);
        Assert.True(held.World.Facts.Exists("event", "late_frost_stood"));
    }

    [Fact]
    public void TheSeasonBargain_HonorsPrice_Shame_AndNoRegard()
    {
        var stranger = new Game(42);
        stranger.Debug_SetStores(3);
        stranger.Debug_DrawSteadEvent("season_bargain");
        stranger.Player.Coin = 6;
        BuyBargain(stranger);
        Assert.Equal(4, stranger.Stores);
        Assert.Equal(0, stranger.Player.Coin);
        Assert.Equal(0, stranger.Regard);
        Assert.True(stranger.World.Facts.Exists("event", "season_bargain_bought"));

        var friend = new Game(42);
        friend.Debug_ClearCamp();
        friend.Debug_SetStores(3);
        friend.Debug_DrawSteadEvent("season_bargain");
        int regard = friend.Regard;
        friend.Player.Coin = 4;
        BuyBargain(friend);
        Assert.Equal(0, friend.Player.Coin);
        Assert.Equal(regard, friend.Regard);

        var refused = new Game(42);
        refused.Debug_SetStores(3);
        refused.Debug_RaiseShame(2);
        refused.Debug_DrawSteadEvent("season_bargain");
        refused.Player.Coin = 99;
        BuyBargain(refused);
        Assert.Equal(3, refused.Stores);
        Assert.Equal(99, refused.Player.Coin);
        Assert.False(refused.SeasonBargainOpen);
        Assert.True(refused.World.Facts.Exists("event", "season_bargain_refused"));
    }

    [Fact]
    public void TheSeasonBargain_ExpiresAtTheNextTick()
    {
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Debug_SetStores(3);
        game.Debug_DrawSteadEvent("season_bargain");
        Assert.True(game.SeasonBargainOpen);
        WaitTicks(game, 1);
        Assert.False(game.SeasonBargainOpen);
        Assert.True(game.World.Facts.Exists("event", "season_bargain_expired"));
    }

    [Fact]
    public void SnapshotSidebarReadersAndHelp_AgreeOnWeather()
    {
        var game = new Game(42);
        game.Debug_SetWeather(ClimateBand.Lowlands, WeatherFamily.Wet);
        var snap = game.TakeSnapshot();
        Assert.Equal("autumn", snap.Season);
        Assert.Equal("wet", snap.LowlandWeather);
        Assert.NotEmpty(snap.RoadForecast);
        Assert.NotEmpty(snap.FellForecast);

        var lines = Presenter.Render(game, 120, 40).ToTextLines();
        Assert.Contains(lines, l => l.Contains("Autumn: rain > rain"));

        game.ApplyKey('?');
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("Calm leaves travel"));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("sidebar's > mark"));

        game.Debug_DrawSteadEvent("drovers");
        NpcTests.BumpNpc(game, game.World.Keeper);
        Assert.Contains(game.Topics, t => t.Label == "The season's news" && t.Answer.Contains("next"));
    }

    [Fact]
    public void RoadAndFellsReaders_NameCurrentAndNextConditions()
    {
        var game = new Game(42);
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("next turn", StringComparison.OrdinalIgnoreCase));

        BumpCurrent(game, game.World.Waykeeper);
        Assert.Contains(game.Topics, t => t.Label == "The road" && t.Answer.Contains("Next turn"));
        Assert.Contains(game.Topics, t => t.Label == "The fells" && t.Answer.Contains("next"));
        game.ApplyKey('z');

        game.Debug_SetPlayerPos(game.World.FellMouthPos);
        game.ApplyKey('>');
        Assert.Equal(Area.Fells, game.Area);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("next turn", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WeatherDoesNotChangeOrdinaryPrices_AndCrossingResetsTheCalendar()
    {
        var game = new Game(42);
        int price = game.RationPrice;
        foreach (var family in Enum.GetValues<WeatherFamily>())
        {
            game.Debug_SetWeather(ClimateBand.Lowlands, family);
            Assert.Equal(price, game.RationPrice);
        }

        game.Debug_SetSeason(WorldSeason.Summer, 2);
        game.Debug_SetStores(3);
        game.Debug_DrawSteadEvent("season_bargain");
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(WorldSeason.Autumn, game.Season);
        Assert.Equal(0, game.SeasonPosition);
        Assert.False(game.SeasonBargainOpen);
    }

    [Fact]
    public void WeatherState_RebuildsExactlyFromTheJournal_InSaveV96()
    {
        const ulong seed = 1234;
        string keys = "0" + new string('.', SteadRaids.TickTurns * 7);
        var live = new Game(seed, firstWake: true);
        foreach (char key in keys) live.ApplyKey(key);
        var replay = SaveCodec.Replay(seed, keys);
        var a = live.TakeSnapshot();
        var b = replay.TakeSnapshot();

        Assert.Equal(96, SaveCodec.Version);
        Assert.Equal(a.Season, b.Season);
        Assert.Equal(a.SeasonPosition, b.SeasonPosition);
        Assert.Equal(a.LowlandWeather, b.LowlandWeather);
        Assert.Equal(a.RoadForecast, b.RoadForecast);
        Assert.Equal(a.FellWeather, b.FellWeather);
        Assert.Equal(a.Stores, b.Stores);
    }

    private static Game QuietGame(ulong seed)
    {
        var game = new Game(seed);
        game.Debug_HoldTheDeck();
        game.Debug_ClearCamp();
        return game;
    }

    private static void BuyBargain(Game game)
    {
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_steadholder"));
        int offer = game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Ration);
        game.ApplyKey((char)('1' + game.Topics.Count + offer));
    }

    private static void Fund(Game game, string work)
    {
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_steadholder"));
        int bench = game.Offers.ToList().FindIndex(o => o.Label.Contains("stead's works"));
        game.ApplyKey((char)('1' + game.Topics.Count + bench));
        int digit = game.TradeOffers.ToList().FindIndex(o => o.Arg == work);
        game.ApplyKey((char)('1' + digit));
        game.ApplyKey('z');
    }

    private static void TakeRoad(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
    }

    private static void BumpCurrent(Game game, Npc npc)
    {
        var beside = Directions.All8.Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => game.CurrentMap.Walkable(p) && !game.NpcsHere.Any(n => n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
    }

    private static Pos OpenPlain(Game game, GameMap map)
    {
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (map[p] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills or Terrain.Heath)) continue;
                if (game.World.Npcs.Any(n => n.Area == game.Area && n.Pos == p)) continue;
                if (game.HerbsHere.Contains(p)) continue;
                return p;
            }
        throw new InvalidOperationException("no open plain ground");
    }

    private static void StepOntoPlain(Game game, GameMap map)
    {
        if (map[game.Player.Pos] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills or Terrain.Heath))
            game.Debug_SetPlayerPos(OpenPlain(game, map));
        foreach (var (dx, dy) in Directions.All8)
        {
            var q = game.Player.Pos.Plus(dx, dy);
            if (!map.InBounds(q) || !map.Walkable(q)) continue;
            if (map[q] is not (Terrain.Grass or Terrain.Forest or Terrain.Hills or Terrain.Heath)) continue;
            if (game.NpcsHere.Any(n => n.Pos == q) || game.HerbsHere.Contains(q)) continue;
            game.ApplyKey(KeyFor(dx, dy));
            return;
        }
        throw new InvalidOperationException("no adjacent plain ground");
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k', (0, 1) => 'j', (-1, 0) => 'h', (1, 0) => 'l',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', (1, 1) => 'n',
        _ => '.',
    };

    private static void WaitTicks(Game game, int ticks)
    {
        for (int i = 0; i < ticks * SteadRaids.TickTurns; i++) game.Apply(Command.Wait);
    }
}
