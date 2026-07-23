using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// D-162's four end-appended activity skills: preparation, live-pressure
/// movement, quiet approach, and the criminal trade.
/// </summary>
public class ActivityBreadthTests
{
    [Fact]
    public void SkillAndCommandEnums_AreEndAppendedInStableOrder()
    {
        Assert.Equal(18, SkillSet.Count);
        Assert.Equal(
            new[] { SkillId.Alchemy, SkillId.Athletics, SkillId.Stealth, SkillId.Larceny },
            Enum.GetValues<SkillId>()[^4..]);
        Assert.Equal(Command.RushN, CommandMap.FromKey('K'));
        Assert.Equal(Command.RushS, CommandMap.FromKey('J'));
        Assert.Equal(Command.RushW, CommandMap.FromKey('H'));
        Assert.Equal(Command.RushE, CommandMap.FromKey('L'));
        Assert.Equal(Command.RushNW, CommandMap.FromKey('Y'));
        Assert.Equal(Command.RushNE, CommandMap.FromKey('U'));
        Assert.Equal(Command.RushSW, CommandMap.FromKey('B'));
        Assert.Equal(Command.RushSE, CommandMap.FromKey('N'));
    }

    [Fact]
    public void SelfBrew_FeedsAlchemyAndScalesYieldWithinTheRack()
    {
        var game = new Game(42);
        game.Player.Lessons.Add(LessonId.Stillcraft);
        game.Player.Herb = 3;
        for (int i = 0; i < SkillSet.UsesForLevel(2); i++)
            game.Player.Skills.AddUse(SkillId.Alchemy);

        game.Debug_SetPlayerPos(game.World.ShrinePos);
        game.ApplyKey('r');

        Assert.Equal(2, game.Player.Draughts);
        Assert.Equal(0, game.Player.Herb);
        Assert.Equal(21, game.Player.Skills.Uses(SkillId.Alchemy));
        Assert.Equal(1, game.Player.SelfBrews);
        Assert.Equal(1, game.TakeSnapshot().SelfBrews);
    }

    [Fact]
    public void SelfBrewKnacks_AddRackRoomOrAlternateHerbThrift()
    {
        var rack = new Game(42);
        rack.Player.Perks.Add(PerkId.RoomOnTheRack);
        Assert.Equal(3, rack.DraughtCap);

        var thrift = new Game(42);
        thrift.Player.Lessons.Add(LessonId.Stillcraft);
        thrift.Player.Perks.Add(PerkId.SecondSteeping);
        thrift.Player.Herb = 6;
        thrift.Debug_SetPlayerPos(thrift.World.ShrinePos);
        thrift.ApplyKey('r');
        thrift.ApplyKey(' ');
        thrift.Player.Draughts = 0;
        thrift.ApplyKey('r');

        Assert.Equal(1, thrift.Player.Herb);
        Assert.Equal(2, thrift.Player.SelfBrews);
    }

    [Fact]
    public void HerbwifesHands_DoNotFeedAlchemyOrUseTheSelfBrewThrift()
    {
        var game = new Game(42);
        game.Player.Herb = 3;
        game.Player.Perks.Add(PerkId.SecondSteeping);
        game.Player.SelfBrews = 1;
        var wife = game.World.Npcs.First(n => n.Id == "npc_herbwife");
        NpcTests.BumpNpc(game, wife);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        game.ApplyKey(TradeKey(game, TradeGood.Draught));

        Assert.Equal(0, game.Player.Herb);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Alchemy));
        Assert.Equal(1, game.Player.SelfBrews);
    }

    [Fact]
    public void Rush_CrossesTwoCellsInOneTurnAndFeedsOnlyUnderLivePressure()
    {
        var game = RushGame(42, out var start, out char key, distance: 2);
        int wind = game.Player.Stamina;
        int turn = game.Turn;

        game.ApplyKey(char.ToUpperInvariant(key));

        var delta = CommandMap.Delta(CommandMap.FromKey(key))!.Value;
        Assert.Equal(start.Plus(delta.dx * 2, delta.dy * 2), game.Player.Pos);
        Assert.Equal(turn + 1, game.Turn);
        Assert.Equal(wind - 3, game.Player.Stamina);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Athletics));
        Assert.Equal(1, game.RushesCompleted);

        var safe = RushGame(43, out _, out key, distance: 2);
        foreach (var foe in safe.Monsters) foe.Aware = false;
        safe.ApplyKey(char.ToUpperInvariant(key));
        Assert.Equal(0, safe.Player.Skills.Uses(SkillId.Athletics));
    }

    [Theory]
    [InlineData(-1, 0, 'H')]
    [InlineData(1, 0, 'L')]
    [InlineData(0, -1, 'K')]
    [InlineData(0, 1, 'J')]
    [InlineData(-1, -1, 'Y')]
    [InlineData(1, -1, 'U')]
    [InlineData(-1, 1, 'B')]
    [InlineData(1, 1, 'N')]
    public void Rush_WorksInEveryDirection(int dx, int dy, char key)
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        var active = game.Monsters.First(m => m.SiteId == game.CurrentSite!.Id);
        foreach (var foe in game.Monsters.Where(m => m.SiteId == game.CurrentSite!.Id && m != active))
            foe.Hp = 0;
        active.Aware = true;
        active.Dormant = false;
        var start = FindLine(game, distance: 2, avoid: active.Pos, dx, dy);
        game.Debug_SetPlayerPos(start);
        game.Player.Stamina = game.Player.MaxStamina;

        game.ApplyKey(key);

        Assert.Equal(start.Plus(dx * 2, dy * 2), game.Player.Pos);
        Assert.Equal(1, game.RushesCompleted);
    }

    [Fact]
    public void RushKnacks_ChooseLongerGroundOrCheaperWind()
    {
        var longStride = RushGame(42, out var start, out char key, distance: 3);
        longStride.Player.Perks.Add(PerkId.LongStride);
        var delta = CommandMap.Delta(CommandMap.FromKey(key))!.Value;
        longStride.ApplyKey(char.ToUpperInvariant(key));
        Assert.Equal(start.Plus(delta.dx * 3, delta.dy * 3), longStride.Player.Pos);

        var kept = RushGame(44, out _, out key, distance: 2);
        kept.Player.Perks.Add(PerkId.KeptBreath);
        int wind = kept.Player.Stamina;
        kept.ApplyKey(char.ToUpperInvariant(key));
        Assert.Equal(wind - 2, kept.Player.Stamina);
    }

    [Fact]
    public void RefusedRush_SpendsNothingAndMovesNoPartOfThePath()
    {
        var game = RushGame(42, out var start, out char key, distance: 2);
        var delta = CommandMap.Delta(CommandMap.FromKey(key))!.Value;
        var blocker = new Monster
        {
            Kind = MonsterKind.Goblin,
            Pos = start.Plus(delta.dx, delta.dy),
            SiteId = game.CurrentSite!.Id,
            Aware = true,
        };
        game.Monsters.Add(blocker);
        int wind = game.Player.Stamina;
        int turn = game.Turn;

        game.ApplyKey(char.ToUpperInvariant(key));

        Assert.Equal(start, game.Player.Pos);
        Assert.Equal(wind, game.Player.Stamina);
        Assert.Equal(turn, game.Turn);
        Assert.Equal(0, game.RushesCompleted);
    }

    [Fact]
    public void QuietStep_CostsTwoTurnsAndFeedsOnceWhenItCrossesNotice()
    {
        var game = QuietGame(42, ordinaryFrom: 9, ordinaryTo: 8, out var foe, out char key);
        int turn = game.Turn;
        game.ApplyKey('s');
        Assert.True(game.SoftTread);

        game.ApplyKey(key);

        Assert.Equal(turn + 2, game.Turn);
        Assert.False(foe.Aware);
        Assert.True(game.SoftTread);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Stealth));
        Assert.Equal(1, game.QuietBandsCrossed);

        game.ApplyKey(Opposite(key));
        game.ApplyKey(key);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Stealth));
    }

    [Fact]
    public void QuietStep_DiscoveryOnTheSettingTurnCancelsTheMove()
    {
        var game = QuietGame(42, ordinaryFrom: 6, ordinaryTo: 7, out var foe, out char key);
        var start = game.Player.Pos;
        int turn = game.Turn;
        game.ApplyKey('s');

        game.ApplyKey(key);

        Assert.Equal(start, game.Player.Pos);
        Assert.Equal(turn + 1, game.Turn);
        Assert.True(foe.Aware);
        Assert.False(game.SoftTread);
        Assert.Equal(1, game.SoftTreadDiscoveries);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Stealth));
    }

    [Fact]
    public void MetalNoiseAndQuietHarness_ComposeAtTheNoticeEdge()
    {
        var noisy = QuietGame(42, ordinaryFrom: 7, ordinaryTo: 8, out var noisyFoe, out char noisyKey);
        noisy.Player.Armor = GearCatalog.Create("riveted_shirt");
        noisy.ApplyKey('s');
        noisy.ApplyKey(noisyKey);
        Assert.True(noisyFoe.Aware);

        var harness = QuietGame(42, ordinaryFrom: 7, ordinaryTo: 8, out var quietFoe, out char quietKey);
        harness.Player.Armor = GearCatalog.Create("riveted_shirt");
        harness.Player.Perks.Add(PerkId.QuietHarness);
        harness.ApplyKey('s');
        harness.ApplyKey(quietKey);
        Assert.False(quietFoe.Aware);
    }

    [Fact]
    public void GraceSkillAndDeeperHush_EachReduceQuietNotice()
    {
        var graceful = QuietGame(42, ordinaryFrom: 6, ordinaryTo: 7, out var gracefulFoe, out char gracefulKey);
        graceful.Player.Attributes[Attr.Grace] = AttributeSet.Baseline + 2;
        graceful.ApplyKey('s');
        graceful.ApplyKey(gracefulKey);
        Assert.False(gracefulFoe.Aware);

        var practiced = QuietGame(42, ordinaryFrom: 6, ordinaryTo: 7, out var practicedFoe, out char practicedKey);
        for (int i = 0; i < SkillSet.UsesForLevel(2); i++)
            practiced.Player.Skills.AddUse(SkillId.Stealth);
        practiced.ApplyKey('s');
        practiced.ApplyKey(practicedKey);
        Assert.False(practicedFoe.Aware);

        var hushed = QuietGame(42, ordinaryFrom: 6, ordinaryTo: 7, out var hushedFoe, out char hushedKey);
        hushed.Player.Perks.Add(PerkId.DeeperHush);
        hushed.ApplyKey('s');
        hushed.ApplyKey(hushedKey);
        Assert.False(hushedFoe.Aware);
    }

    [Fact]
    public void CrimeKnacks_KeepTheirSkillsSeparateAndTheirCapsHonest()
    {
        Assert.Equal(0.60, Lifting.ChanceFor(0, 0.10), 3);
        Assert.Equal(0.45, Locks.ChanceFor(0, 0.10), 3);
        Assert.Equal(0.85, Lifting.ChanceFor(20, 0.10));
        Assert.Equal(0.85, Locks.ChanceFor(20, 0.10));

        var game = new Game(42);
        for (int i = 0; i < SkillSet.UsesForLevel(2); i++)
            game.Player.Skills.AddUse(SkillId.Larceny);
        game.Player.Perks.Add(PerkId.RoadPrice);
        Assert.Equal(Peddling.TrinketPrice + 3, game.FenceHeirloomPrice);
    }

    private static Game RushGame(ulong seed, out Pos start, out char key, int distance)
    {
        var game = new Game(seed);
        game.Debug_SetMode(MapMode.Site);
        var site = game.CurrentSite!;
        var active = game.Monsters.First(m => m.SiteId == site.Id);
        foreach (var foe in game.Monsters.Where(m => m.SiteId == site.Id && m != active))
            foe.Hp = 0;
        active.Aware = true;
        active.Dormant = false;

        (start, key) = FindLine(game, distance, avoid: active.Pos);
        game.Debug_SetPlayerPos(start);
        game.Player.Stamina = game.Player.MaxStamina;
        return game;
    }

    private static Game QuietGame(
        ulong seed,
        int ordinaryFrom,
        int ordinaryTo,
        out Monster foe,
        out char key)
    {
        var game = new Game(seed);
        game.Debug_SetMode(MapMode.Site);
        foreach (var existing in game.Monsters.Where(m => m.SiteId == game.CurrentSite!.Id))
            existing.Hp = 0;

        var (mark, from, move) = FindNoticeEdge(game.CurrentMap, ordinaryFrom, ordinaryTo);
        foe = new Monster
        {
            Kind = MonsterKind.Goblin,
            Pos = mark,
            SiteId = game.CurrentSite!.Id,
            Aware = false,
        };
        game.Monsters.Add(foe);
        game.Debug_SetPlayerPos(from);
        key = move;
        return game;
    }

    private static (Pos Start, char Key) FindLine(Game game, int distance, Pos avoid)
    {
        var map = game.CurrentMap;
        foreach (var (dx, dy) in Directions.All8)
            if (TryFindLine(game, distance, avoid, dx, dy, out var start))
                return (start, KeyFor(dx, dy));
        throw new InvalidOperationException("No clear rush line found.");
    }

    private static Pos FindLine(Game game, int distance, Pos avoid, int dx, int dy)
    {
        if (TryFindLine(game, distance, avoid, dx, dy, out var start))
            return start;
        throw new InvalidOperationException($"No clear rush line found for ({dx}, {dy}).");
    }

    private static bool TryFindLine(
        Game game,
        int distance,
        Pos avoid,
        int dx,
        int dy,
        out Pos start)
    {
        var map = game.CurrentMap;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var candidate = new Pos(x, y);
                bool open = Enumerable.Range(0, distance + 1)
                    .Select(i => candidate.Plus(dx * i, dy * i))
                    .All(p => map.InBounds(p)
                        && map[p] is Terrain.Floor or Terrain.Grass or Terrain.Forest or Terrain.Hills or Terrain.Heath
                        && p != avoid
                        && !game.NpcsHere.Any(n => n.Pos == p));
                if (open)
                {
                    start = candidate;
                    return true;
                }
            }
        start = default;
        return false;
    }

    private static (Pos Mark, Pos From, char Key) FindNoticeEdge(GameMap map, int fromDistance, int toDistance)
    {
        for (int my = 0; my < map.Height; my++)
            for (int mx = 0; mx < map.Width; mx++)
            {
                var mark = new Pos(mx, my);
                if (!map.Walkable(mark)) continue;
                for (int fy = 0; fy < map.Height; fy++)
                    for (int fx = 0; fx < map.Width; fx++)
                    {
                        var from = new Pos(fx, fy);
                        if (!map.Walkable(from) || from.Chebyshev(mark) != fromDistance
                            || !map.LineOfSight(mark, from)) continue;
                        foreach (var (dx, dy) in Directions.All8)
                        {
                            var to = from.Plus(dx, dy);
                            if (map.Walkable(to) && to.Chebyshev(mark) == toDistance
                                && map.LineOfSight(mark, to))
                                return (mark, from, KeyFor(dx, dy));
                        }
                    }
            }
        throw new InvalidOperationException("No notice edge found.");
    }

    private static char OfferKey(Game game, TradeGood good) =>
        (char)('1' + game.Topics.Count + game.Offers.ToList().FindIndex(o => o.Good == good));

    private static char TradeKey(Game game, TradeGood good) =>
        (char)('1' + game.TradeOffers.ToList().FindIndex(o => o.Good == good));

    private static char Opposite(char key) => key switch
    {
        'h' => 'l',
        'j' => 'k',
        'k' => 'j',
        'l' => 'h',
        'y' => 'n',
        'u' => 'b',
        'b' => 'u',
        'n' => 'y',
        _ => key,
    };

    private static char KeyFor(int dx, int dy) => (Math.Sign(dx), Math.Sign(dy)) switch
    {
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        (1, 1) => 'n',
        _ => '.',
    };
}
