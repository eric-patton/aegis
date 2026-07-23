using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>The Salt Fen and generator-version contract (D-165).</summary>
public class FenTests
{
    internal static void EnterFens(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        game.Debug_SetPlayerPos(game.World.FenMouthPos);
        game.ApplyKey('>');
        Assert.Equal(Area.Fens, game.Area);
    }

    [Fact]
    public void GeneratorOne_AddsOneConnectedFen_WithTheApprovedSiteMix()
    {
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var a = WorldGen.Generate(seed, generatorVersion: 1);
            var b = WorldGen.Generate(seed, generatorVersion: 1);

            Assert.Equal(1, a.GeneratorVersion);
            Assert.Equal(4, a.Regions.Count);
            Assert.Equal(a.FenRegion.Name, b.FenRegion.Name);
            Assert.Equal(a.Fens.ContentHash(), b.Fens.ContentHash());
            Assert.Equal(Terrain.FenMouth, a.Road[a.FenMouthPos]);
            Assert.Equal(Terrain.FenMouth, a.Fens[a.FenHomePos]);
            Assert.Equal(3, a.SaltworksSite.SaltPans.Count);
            Assert.Equal(
                new[] { SiteKind.FenHamlet, SiteKind.Saltworks, SiteKind.FenWilds, SiteKind.FenWatch, SiteKind.FenVault },
                a.Sites.Where(s => s.Area == Area.Fens).Select(s => s.Kind).ToArray());

            var reached = Reachable(a.Fens, a.FenHomePos);
            Assert.All(a.Sites.Where(s => s.Area == Area.Fens), s => Assert.Contains(s.OverworldPos, reached));
            Assert.All(a.SaltworksSite.SaltPans, p => Assert.Equal(Terrain.SaltPan, a.SaltworksSite.Map[p]));
            Assert.Contains(a.FenWildsSite.Spawns, s => s.Kind == MonsterKind.FenAdder);

            var measure = WorldEval.Measure(a);
            Assert.Equal(1, measure.GeneratorVersion);
            Assert.Equal(a.FenRegion.Name, measure.FenRegion);
            Assert.Equal(a.Fens.ContentHash().ToString("x16"), measure.FenHash);
            Assert.Equal(3, measure.Sites.Single(s => s.Kind == "saltworks").SaltPans);
        }
    }

    [Fact]
    public void GeneratorAndSaveHeaders_PinAndRejectVersions()
    {
        Assert.Equal(new[] { 1 }, WorldGen.SupportedGeneratorVersions);
        Assert.Throws<NotSupportedException>(() => WorldGen.Generate(42, generatorVersion: 2));

        string encoded = SaveCodec.EncodeHeader(42, 1) + "\n0";
        var (seed, generator, keys) = SaveCodec.Parse(encoded);
        Assert.Equal(42UL, seed);
        Assert.Equal(1, generator);
        Assert.Equal("0", keys);
        Assert.Throws<FormatException>(() => SaveCodec.Parse("AEGIS-SAVE v99 gen:2 seed:42\n0"));
    }

    [Fact]
    public void CausewayCrossing_WorksBothWays_AndDoesNotStride()
    {
        var game = new Game(42);
        EnterFens(game);
        Assert.Equal(game.World.FenHomePos, game.Player.Pos);
        Assert.Equal("fens", game.CurrentMap.Id);

        var start = game.Player.Pos;
        game.ApplyKey('L');
        Assert.True(game.Player.Pos.Chebyshev(start) <= 1);

        game.Debug_SetPlayerPos(game.World.FenHomePos);
        game.ApplyKey('>');
        Assert.Equal(Area.Road, game.Area);
        Assert.Equal(game.World.FenMouthPos, game.Player.Pos);
    }

    [Fact]
    public void SaltPan_RefusesWetAndColdWithoutCost_ThenWorksInSixTurns()
    {
        var game = new Game(42);
        EnterFens(game);
        var saltwork = game.World.SaltworksSite;
        game.Debug_SetPlayerPos(saltwork.OverworldPos);
        game.ApplyKey('>');
        var pan = saltwork.SaltPans[0];
        game.Debug_SetPlayerPos(pan);
        int turn = game.Turn;
        int stamina = game.Player.Stamina;

        game.Debug_SetWeather(ClimateBand.Fens, WeatherFamily.Wet);
        game.ApplyKey('g');
        Assert.Equal(turn, game.Turn);
        Assert.Equal(stamina, game.Player.Stamina);
        Assert.Equal(Terrain.SaltPan, saltwork.Map[pan]);
        Assert.Equal(1, game.FenPanRefusals);

        game.Debug_SetWeather(ClimateBand.Fens, WeatherFamily.Wind);
        game.ApplyKey('g');
        Assert.Equal(turn + FenLife.PanTurns, game.Turn);
        Assert.Equal(stamina, game.Player.Stamina);
        Assert.Equal(Terrain.ExhaustedPan, saltwork.Map[pan]);
        Assert.Equal(1, game.Player.Salt);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Survival));
    }

    [Fact]
    public void FenAdder_CrossesOneWaterCell_ButEndsOnGround()
    {
        var game = new Game(42);
        EnterFens(game);
        var wilds = game.World.FenWildsSite;
        game.Debug_SetPlayerPos(wilds.OverworldPos);
        game.ApplyKey('>');
        var adder = game.Monsters.First(m => m.Kind == MonsterKind.FenAdder && m.SiteId == wilds.Id);
        foreach (var other in game.Monsters.Where(m => m.SiteId == wilds.Id && m != adder)) other.Hp = 0;

        adder.Aware = true;
        adder.Pos = new Pos(2, 5);
        game.Debug_SetPlayerPos(new Pos(6, 5));
        wilds.Map[new Pos(3, 5)] = Terrain.Water;
        wilds.Map[new Pos(4, 5)] = Terrain.Floor;
        wilds.Map[new Pos(5, 5)] = Terrain.Floor;

        game.ApplyKey('.');

        Assert.Equal(new Pos(4, 5), adder.Pos);
        Assert.NotEqual(Terrain.Water, wilds.Map[adder.Pos]);
    }

    [Fact]
    public void FenAdder_TelegraphsATwoCellStraightCoil()
    {
        bool seen = false;
        for (ulong seed = 1; seed <= 20 && !seen; seed++)
        {
            var game = new Game(seed);
            EnterFens(game);
            var wilds = game.World.FenWildsSite;
            game.Debug_SetPlayerPos(wilds.OverworldPos);
            game.ApplyKey('>');
            var adder = game.Monsters.First(m => m.Kind == MonsterKind.FenAdder && m.SiteId == wilds.Id);
            foreach (var other in game.Monsters.Where(m => m.SiteId == wilds.Id && m != adder)) other.Hp = 0;
            adder.Aware = true;
            adder.Pos = new Pos(4, 4);
            game.Debug_SetPlayerPos(new Pos(6, 4));
            wilds.Map[adder.Pos] = Terrain.Floor;
            wilds.Map[new Pos(5, 4)] = Terrain.Floor;
            wilds.Map[game.Player.Pos] = Terrain.Floor;

            game.ApplyKey('.');

            if (adder.Intent?.Kind != IntentKind.CoilStrike) continue;
            Assert.Equal(new[] { new Pos(5, 4), new Pos(6, 4) }, adder.Intent.Footprint);
            seen = true;
        }
        Assert.True(seen, "no deterministic seed in the audit sample armed the coil strike");
    }

    [Fact]
    public void RegionalAccount_HasTwoEqualConclusions_AndOneDelayedCappedRestock()
    {
        foreach (TradeGood conclusion in new[] { TradeGood.CompactMeasure, TradeGood.CompactRoad })
        {
            var game = new Game(conclusion == TradeGood.CompactMeasure ? 42UL : 43UL);
            EnterFens(game);
            var saltwork = game.World.SaltworksSite;
            game.Debug_SetWeather(ClimateBand.Fens, WeatherFamily.Calm);
            game.Debug_SetPlayerPos(saltwork.OverworldPos);
            game.ApplyKey('>');
            foreach (var pan in saltwork.SaltPans.ToList())
            {
                game.Debug_SetPlayerPos(pan);
                game.ApplyKey('g');
            }
            game.Debug_SetPlayerPos(saltwork.EntryPos);
            game.ApplyKey('<');

            game.Debug_ClearSite(SiteKind.FenWilds);
            game.Debug_ClearSite(SiteKind.FenWatch);
            game.Debug_ClearSite(SiteKind.FenVault);
            var hamlet = game.World.FenHamletSite;
            game.Debug_SetPlayerPos(hamlet.OverworldPos);
            game.ApplyKey('>');
            Bump(game, game.NpcsHere.Single(n => n.Id == "npc_compact_keeper"));

            int salt = game.Player.Salt;
            game.World.PeddlerSalt = 0;
            game.ApplyKey(OfferKey(game, conclusion));
            Assert.Equal(salt + 1, game.Player.Salt);
            Assert.Equal(1, game.FenArcConclusions);
            Assert.True(game.FenRestockScheduled);
            Assert.False(game.FenRestocked);

            while (!game.FenRestocked) game.ApplyKey('.');
            Assert.Equal(1, game.FenRestocks);
            Assert.InRange(game.World.PeddlerSalt, 0, Peddling.SaltStock(game.World.Tier));
            for (int i = 0; i < SteadRaids.TickTurns; i++) game.ApplyKey('.');
            Assert.Equal(1, game.FenRestocks);
        }
    }

    [Fact]
    public void OnePointZeroAppendOnlyKinds_AndReaderCatalog_AreAuditable()
    {
        Assert.Equal((int)Area.Fells + 1, (int)Area.Fens);
        Assert.Equal((int)MonsterKind.RuneTongue + 1, (int)MonsterKind.FenAdder);
        Assert.Equal((int)IntentKind.BindingWord + 1, (int)IntentKind.CoilStrike);
        Assert.Equal((int)NpcKind.GraveTally + 1, (int)NpcKind.Fenfolk);
        Assert.Equal((int)Terrain.LawDayRing + 1, (int)Terrain.FenMouth);

        Assert.Contains(StoryletCatalog.All, s =>
            s.Id == "the-lane-knows-the-step"
            && s.Trigger == StoryletTrigger.NearHouse
            && s.Requires.Contains(new FactPattern("shame", "housebroken")));
        Assert.Contains(StoryletCatalog.All, s =>
            s.Id == "the-villager-sees-the-shade" && s.Trigger == StoryletTrigger.Talk);
        Assert.Contains(StoryletCatalog.All, s =>
            s.Id == "the-warder-answers-the-shade" && s.Trigger == StoryletTrigger.Talk);
    }

    [Fact]
    public void HousebreakerReader_RequiresKnowledge_FiresOnce_AndChangesNoNumber()
    {
        var ignorant = new Game(42);
        ignorant.Debug_FireStorylet(StoryletTrigger.NearHouse);
        Assert.DoesNotContain(ignorant.Log.Entries, e => e.Text.Contains("learned your step"));

        var game = new Game(42);
        game.World.Facts.Add("shame", "housebroken", game.World.SettlementName,
            "The bearer was seen leaving a house they had entered.");
        var before = game.TakeSnapshot();
        game.Debug_FireStorylet(StoryletTrigger.NearHouse);
        game.Debug_FireStorylet(StoryletTrigger.NearHouse);
        var after = game.TakeSnapshot();

        Assert.Single(game.Log.Entries, e => e.Text.Contains("learned your step"));
        Assert.Equal(before.Coin, after.Coin);
        Assert.Equal(before.Hp, after.Hp);
        Assert.Equal(before.Essence, after.Essence);
        Assert.Equal(before.Regard, after.Regard);
        Assert.Equal(before.Shame, after.Shame);
    }

    [Fact]
    public void ShadeReaders_RequireThePresentShade_AndRemainNonnumeric()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Calling);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        game.ApplyKey('z');
        game.ApplyKey((char)('1' + game.Player.Spells.IndexOf(SpellId.Calling)));
        Assert.NotNull(game.Shade);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        game.ApplyKey('<');

        var before = game.TakeSnapshot();
        Bump(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager && n.Area == Area.Valley));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("one word early"));
        game.ApplyKey('z');

        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Bump(game, game.NpcsHere.Single(n => n.Id == "npc_mootwarden"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("second cup"));
        var after = game.TakeSnapshot();

        Assert.Equal(before.Coin, after.Coin);
        Assert.Equal(before.Essence, after.Essence);
        Assert.Equal(before.Regard, after.Regard);
        Assert.Equal(before.Shame, after.Shame);
    }

    private static char OfferKey(Game game, TradeGood good)
    {
        int index = game.Offers.ToList().FindIndex(o => o.Good == good);
        Assert.True(index >= 0, $"offer {good} was absent");
        return (char)('1' + game.Topics.Count + index);
    }

    private static void Bump(Game game, Npc npc)
    {
        foreach (var (dx, dy, key) in new[]
        {
            (-1, 0, 'l'), (1, 0, 'h'), (0, -1, 'j'), (0, 1, 'k'),
            (-1, -1, 'n'), (1, -1, 'b'), (-1, 1, 'u'), (1, 1, 'y'),
        })
        {
            var from = npc.Pos.Plus(dx, dy);
            if (!game.CurrentMap.Walkable(from)) continue;
            game.Debug_SetPlayerPos(from);
            game.ApplyKey(key);
            Assert.True(game.InTalkMenu);
            return;
        }
        throw new Xunit.Sdk.XunitException("No walkable cell beside fen NPC.");
    }

    private static HashSet<Pos> Reachable(GameMap map, Pos start)
    {
        var reached = new HashSet<Pos> { start };
        var queue = new Queue<Pos>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var here = queue.Dequeue();
            foreach (var (dx, dy) in Directions.All8)
            {
                var next = here.Plus(dx, dy);
                if (map.Walkable(next) && reached.Add(next)) queue.Enqueue(next);
            }
        }
        return reached;
    }
}
