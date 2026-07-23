using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// V1-08 acceptance for companion parity, the grain road, bounded memory,
/// beast warmth, scar closure, tiered Toll, and the final two launch oaths.
/// </summary>
public class CompanionConsequencesTests
{
    private static readonly (int dx, int dy, char key)[] Steps =
    [
        (-1, -1, 'y'), (0, -1, 'k'), (1, -1, 'u'), (-1, 0, 'h'),
        (1, 0, 'l'), (-1, 1, 'b'), (0, 1, 'j'), (1, 1, 'n'),
    ];

    private static Game EmptyCamp()
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        foreach (var monster in game.Monsters) monster.Hp = 0;
        return game;
    }

    private static (Pos A, Pos B, Pos C, char Key) OpenLine(Game game)
    {
        var map = game.CurrentMap;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
                foreach (var (dx, dy, key) in Steps)
                {
                    var a = new Pos(x, y);
                    var b = a.Plus(dx, dy);
                    var c = b.Plus(dx, dy);
                    if (map.Walkable(a) && map.Walkable(b) && map.Walkable(c))
                        return (a, b, c, key);
                }
        throw new InvalidOperationException("no open three-cell line");
    }

    private static (Pos A, Pos B, Pos C, Pos D, char Key) OpenFour(Game game)
    {
        var map = game.CurrentMap;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
                foreach (var (dx, dy, key) in Steps)
                {
                    var a = new Pos(x, y);
                    var b = a.Plus(dx, dy);
                    var c = b.Plus(dx, dy);
                    var d = c.Plus(dx, dy);
                    if (map.Walkable(a) && map.Walkable(b)
                        && map.Walkable(c) && map.Walkable(d))
                        return (a, b, c, d, key);
                }
        throw new InvalidOperationException("no open four-cell line");
    }

    private static Game CrossUnder(char oathKey)
    {
        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey(oathKey);
        game.ApplyKey('>');
        return game;
    }

    [Fact]
    public void PhysicalIntent_ChoosesTheNearerVisibleFellow_ButMagicKeepsTheBearer()
    {
        var game = EmptyCamp();
        var line = OpenLine(game);
        game.Debug_SetPlayerPos(line.A);
        var guest = new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = line.B, MaxHp = 16, Hp = 16,
        };
        game.Debug_SetGuest(guest);
        var foe = new Monster { Kind = MonsterKind.Wight, Pos = line.C, SiteId = "goblin-camp", Hp = 20 };
        game.Monsters.Add(foe);

        foe.Intent = new Intent { Kind = IntentKind.BarrowBlade, TargetCell = game.Player.Pos };
        game.Debug_RetargetPhysicalIntent(foe);
        Assert.Equal(guest.Pos, foe.Intent.TargetCell);
        Assert.Equal(1, game.PhysicalTargetsOnFellows);

        foe.Intent = new Intent { Kind = IntentKind.BindingWord, TargetCell = game.Player.Pos };
        game.Debug_RetargetPhysicalIntent(foe);
        Assert.Equal(game.Player.Pos, foe.Intent.TargetCell);
    }

    [Fact]
    public void PhysicalTargeting_KeepsBearerPriorityOnATie_AndHonorsBlockedSight()
    {
        var game = EmptyCamp();
        var line = OpenFour(game);
        game.Debug_SetPlayerPos(line.A);
        var guest = new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = line.B, MaxHp = 16, Hp = 16,
        };
        game.Debug_SetGuest(guest);
        var foe = new Monster { Kind = MonsterKind.Wight, Pos = line.D, SiteId = "goblin-camp", Hp = 20 };
        game.Monsters.Add(foe);

        game.CurrentMap[line.C] = Terrain.Wall;
        foe.Intent = new Intent { Kind = IntentKind.BarrowBlade, TargetCell = game.Player.Pos };
        game.Debug_RetargetPhysicalIntent(foe);
        Assert.Equal(game.Player.Pos, foe.Intent.TargetCell);

        game.CurrentMap[line.C] = Terrain.Floor;
        game.Debug_SetPlayerPos(line.B);
        guest.Pos = Enumerable.Range(-2, 5)
            .SelectMany(dy => Enumerable.Range(-2, 5)
                .Select(dx => foe.Pos.Plus(dx, dy)))
            .First(p => p != game.Player.Pos && p.Chebyshev(foe.Pos) == 2
                && game.CurrentMap.Walkable(p));
        foe.Intent = new Intent { Kind = IntentKind.BarrowBlade, TargetCell = game.Player.Pos };
        game.Debug_RetargetPhysicalIntent(foe);
        Assert.Equal(game.Player.Pos, foe.Intent.TargetCell);
    }

    [Fact]
    public void MarkedFootprint_HitsEveryBody_AndHeldGroundAcceptsTheCost()
    {
        var game = EmptyCamp();
        var line = OpenLine(game);
        game.Debug_SetPlayerPos(line.A);
        var guest = new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = line.B, MaxHp = 30, Hp = 30, Holding = true,
        };
        game.Debug_SetGuest(guest);
        var foe = new Monster { Kind = MonsterKind.Wight, Pos = line.C, SiteId = "goblin-camp", Hp = 20 };
        foe.Intent = new Intent
        {
            Kind = IntentKind.BarrowBlade,
            TargetCell = game.Player.Pos,
            Footprint = [game.Player.Pos, guest.Pos],
        };
        game.Monsters.Add(foe);

        int bearerHp = game.Player.Hp;
        game.ApplyKey('.');

        Assert.True(game.Player.Hp < bearerHp);
        Assert.True(guest.Hp < guest.MaxHp);
        Assert.Equal(1, game.HeldFellowImpacts);
    }

    [Fact]
    public void ChargeAndLoftFootprints_StrikeFellowsByOccupiedCell()
    {
        var charge = EmptyCamp();
        var line = OpenFour(charge);
        charge.Debug_SetPlayerPos(line.C);
        var guest = new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = line.B, MaxHp = 40, Hp = 40, Holding = true,
        };
        charge.Debug_SetGuest(guest);
        var boar = new Monster { Kind = MonsterKind.Boar, Pos = line.A, SiteId = "goblin-camp", Hp = 30 };
        boar.Intent = new Intent { Kind = IntentKind.BoarCharge, TargetCell = line.D };
        charge.Monsters.Add(boar);
        int bearerHp = charge.Player.Hp;
        charge.ApplyKey('.');
        Assert.True(guest.Hp < guest.MaxHp);
        Assert.True(charge.Player.Hp < bearerHp);

        var loft = EmptyCamp();
        var loftLine = OpenLine(loft);
        loft.Debug_SetPlayerPos(loftLine.A);
        var loftGuest = new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = loftLine.B, MaxHp = 40, Hp = 40, Holding = true,
        };
        loft.Debug_SetGuest(loftGuest);
        var warder = new Monster { Kind = MonsterKind.Warder, Pos = loftLine.C, SiteId = "goblin-camp", Hp = 30 };
        warder.Intent = new Intent { Kind = IntentKind.LoftedStone, TargetCell = loftGuest.Pos };
        loft.Monsters.Add(warder);
        loft.ApplyKey('.');
        Assert.True(loftGuest.Hp < loftGuest.MaxHp);
    }

    [Fact]
    public void FollowingFellow_StepsOffAVisibleMark_InStableSafetyOrder()
    {
        var game = EmptyCamp();
        var line = OpenLine(game);
        game.Debug_SetPlayerPos(line.A);
        var guest = new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = line.B, MaxHp = 16, Hp = 16,
        };
        game.Debug_SetGuest(guest);
        var foe = new Monster { Kind = MonsterKind.Wight, Pos = line.C, SiteId = "goblin-camp", Hp = 20 };
        foe.Intent = new Intent { Kind = IntentKind.BarrowBlade, TargetCell = guest.Pos, TurnsUntilResolve = 2 };
        game.Monsters.Add(foe);

        var marked = guest.Pos;
        game.ApplyKey('.');

        Assert.NotEqual(marked, guest.Pos);
        Assert.Equal(1, game.FellowEvasions);
    }

    [Fact]
    public void FollowingFellow_GainsNoEscapeWhenEveryAdjacentCellIsIllegal()
    {
        var game = EmptyCamp();
        var line = OpenLine(game);
        game.Debug_SetPlayerPos(line.A);
        var guest = new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = line.B, MaxHp = 30, Hp = 30,
        };
        game.Debug_SetGuest(guest);
        foreach (var (dx, dy, _) in Steps)
        {
            var cell = guest.Pos.Plus(dx, dy);
            if (cell != game.Player.Pos && game.CurrentMap.InBounds(cell))
                game.CurrentMap[cell] = Terrain.Wall;
        }
        var foe = new Monster { Kind = MonsterKind.Wight, Pos = line.C, SiteId = "goblin-camp", Hp = 20 };
        foe.Intent = new Intent { Kind = IntentKind.BarrowBlade, TargetCell = guest.Pos, TurnsUntilResolve = 2 };
        game.Monsters.Add(foe);

        var before = guest.Pos;
        game.ApplyKey('.');

        Assert.Equal(before, guest.Pos);
        Assert.Equal(0, game.FellowEvasions);
    }

    [Fact]
    public void MortalGuest_BlocksALooseForFree_ButOnlyBeforeTheFirstFoe()
    {
        var game = EmptyCamp();
        var line = OpenLine(game);
        game.Debug_SetPlayerPos(line.A);
        game.Debug_GrantGear("hunting_bow");
        var guest = new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = line.B, MaxHp = 16, Hp = 16,
        };
        game.Debug_SetGuest(guest);
        game.Monsters.Add(new Monster
        {
            Kind = MonsterKind.Goblin, Pos = line.C, SiteId = "goblin-camp", Hp = 20,
        });

        int turn = game.Turn;
        int stamina = game.Player.Stamina;
        int wear = game.Player.Bow!.Wear;
        int looses = game.Player.Looses;
        int skill = game.Player.Skills.Uses(SkillId.Ranged);
        game.ApplyKey('f');
        game.ApplyKey(line.Key);

        Assert.Equal(turn, game.Turn);
        Assert.Equal(stamina, game.Player.Stamina);
        Assert.Equal(wear, game.Player.Bow.Wear);
        Assert.Equal(looses, game.Player.Looses);
        Assert.Equal(skill, game.Player.Skills.Uses(SkillId.Ranged));
        Assert.Equal(1, game.GuestShotRefusals);
    }

    [Fact]
    public void AShadeDoesNotBlockAShaft_AndAGuestBehindTheFirstFoeDoesNotRefuseIt()
    {
        var shadeGame = EmptyCamp();
        var shadeLine = OpenLine(shadeGame);
        shadeGame.Debug_SetPlayerPos(shadeLine.A);
        shadeGame.Debug_GrantGear("hunting_bow");
        shadeGame.Debug_LearnSpell(SpellId.Calling);
        shadeGame.ApplyKey('z');
        shadeGame.ApplyKey((char)('1' + shadeGame.Player.Spells.IndexOf(SpellId.Calling)));
        shadeGame.Shade!.Pos = shadeLine.B;
        shadeGame.Monsters.Add(new Monster
        {
            Kind = MonsterKind.Goblin, Pos = shadeLine.C, SiteId = "goblin-camp", Hp = 20,
        });
        int shadeTurn = shadeGame.Turn;
        shadeGame.ApplyKey('f');
        shadeGame.ApplyKey(shadeLine.Key);
        Assert.True(shadeGame.Turn > shadeTurn);
        Assert.Equal(0, shadeGame.GuestShotRefusals);

        var behind = EmptyCamp();
        var line = OpenFour(behind);
        behind.Debug_SetPlayerPos(line.A);
        behind.Debug_GrantGear("hunting_bow");
        behind.Debug_SetGuest(new Guest
        {
            Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman,
            Pos = line.C, MaxHp = 16, Hp = 16,
        });
        behind.Monsters.Add(new Monster
        {
            Kind = MonsterKind.Goblin, Pos = line.B, SiteId = "goblin-camp", Hp = 20,
        });
        int turn = behind.Turn;
        behind.ApplyKey('f');
        behind.ApplyKey(line.Key);
        Assert.True(behind.Turn > turn);
        Assert.Equal(0, behind.GuestShotRefusals);
    }

    [Fact]
    public void GrainRoad_CompletesAtTheGuild_AndItsNextTickCartIsCappedAndCounted()
    {
        var game = new Game(42);
        game.World.Facts.Add("guild", "guild_sworn", game.World.TownName, "The carriers' bond stands.");
        game.Debug_SetStores(1);
        game.Debug_CallLevy();
        game.Debug_HoldTheDeck();
        game.Debug_StartGuest("npc_herbwife", GuestRole.Crofter);

        Assert.Equal(GuestRole.Crofter, game.Guest!.Role);
        game.Debug_CompleteGrainRoad();

        Assert.Null(game.Guest);
        Assert.True(game.GrainRoadCompleted);
        Assert.True(game.GrainCartScheduled);
        Assert.True(game.Player.GuestSuccessMemoryArmed);

        for (int i = 0; i < SteadRaids.TickTurns; i++) game.ApplyKey('.');

        Assert.True(game.GrainCartDelivered);
        Assert.Equal(1, game.GrainDeliveries);
        Assert.InRange(game.GrainStoresRestored, 0, 2);
        Assert.Equal(1, game.Regard);
    }

    [Fact]
    public void GrainRoad_RequiresEveryOfferGate_AndCastsOnlyAnEligibleVillager()
    {
        static Npc Crofter(Game game) =>
            game.World.Npcs.First(n => n.Kind == NpcKind.Villager && n.Id != "npc_woodward");

        var noLevy = new Game(42);
        noLevy.World.Facts.Add("guild", "guild_sworn", noLevy.World.TownName, "The carriers' bond stands.");
        NpcTests.BumpNpc(noLevy, Crofter(noLevy));
        Assert.Null(noLevy.Guest);

        var noGuild = new Game(42);
        noGuild.Debug_SetStores(1);
        noGuild.Debug_CallLevy();
        NpcTests.BumpNpc(noGuild, Crofter(noGuild));
        Assert.Null(noGuild.Guest);

        var eligible = new Game(42);
        eligible.World.Facts.Add("guild", "guild_sworn", eligible.World.TownName, "The carriers' bond stands.");
        eligible.Debug_SetStores(1);
        eligible.Debug_CallLevy();
        var cast = Crofter(eligible);
        for (int i = 0; i < 8 && eligible.Guest is null; i++)
        {
            NpcTests.BumpNpc(eligible, cast);
            eligible.ApplyKey(' ');
        }
        Assert.Equal(GuestRole.Crofter, eligible.Guest!.Role);
        Assert.Equal(cast.Id, eligible.Guest.NpcId);
        Assert.Equal(1, eligible.GuestArcOffers);

        var closed = new Game(42);
        closed.World.Facts.Add("guild", "guild_sworn", closed.World.TownName, "The carriers' bond stands.");
        closed.Debug_SetStores(1);
        closed.Debug_CallLevy();
        closed.World.Facts.Add("arc", "grain_road_started", "old", "The road was already offered.");
        NpcTests.BumpNpc(closed, Crofter(closed));
        Assert.Null(closed.Guest);
    }

    [Fact]
    public void FallenCrofter_GetsEstablishedGuestConsequences_ButNoCart()
    {
        var game = EmptyCamp();
        game.Debug_StartGuest("npc_herbwife", GuestRole.Crofter);
        var guest = game.Guest!;
        guest.Hp = 1;
        guest.Holding = true;
        var line = OpenLine(game);
        game.Debug_SetPlayerPos(line.A);
        guest.Pos = line.B;
        var foe = new Monster { Kind = MonsterKind.Wight, Pos = line.C, SiteId = "goblin-camp", Hp = 20 };
        foe.Intent = new Intent { Kind = IntentKind.BarrowBlade, TargetCell = guest.Pos };
        game.Monsters.Add(foe);

        game.ApplyKey('.');

        Assert.False(guest.Alive);
        Assert.Equal(1, game.GuestDeaths);
        Assert.True(game.World.Facts.Exists("guest-fell", "npc_herbwife"));
        Assert.False(game.GrainCartScheduled);
        Assert.False(game.GrainRoadCompleted);
    }

    [Fact]
    public void UnfinishedMortalRoad_EndsInAFarewellAtTheArch()
    {
        var game = new Game(42);
        game.Debug_StartGuest("npc_herbwife", GuestRole.Crofter);
        game.Debug_ClearCamp();
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');

        Assert.Null(game.Guest);
        Assert.Equal(1, game.GuestFarewells);
        Assert.False(game.World.Facts.Exists("portfolio", "grain_road"));
    }

    [Fact]
    public void CompletedMortalRoad_IsRememberedOnce_AtALaterShrine()
    {
        var game = new Game(42);
        game.Debug_StartGuest("npc_woodward", GuestRole.Huntsman);
        game.Debug_ClearCamp();
        Assert.True(game.Player.GuestSuccessMemoryArmed);

        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');
        game.ApplyKey('r');

        Assert.False(game.Player.GuestSuccessMemoryArmed);
        Assert.True(game.Player.GuestSuccessMemoryConsumed);
    }

    [Fact]
    public void BelovedLossMemory_ArmsOnlyForTheApprovedLoss_AndStaysWithTheAegis()
    {
        var game = EmptyCamp();
        game.Debug_StartGuest("npc_woodward", GuestRole.Huntsman);
        var guest = game.Guest!;
        guest.Beats = 3;
        guest.Hp = 1;
        guest.Holding = true;
        var line = OpenLine(game);
        game.Debug_SetPlayerPos(line.A);
        guest.Pos = line.B;
        var foe = new Monster { Kind = MonsterKind.Wight, Pos = line.C, SiteId = "goblin-camp", Hp = 20 };
        foe.Intent = new Intent { Kind = IntentKind.BarrowBlade, TargetCell = guest.Pos };
        game.Monsters.Add(foe);
        game.ApplyKey('.');

        Assert.True(game.Player.GuestLossMemoryArmed);
        Assert.True(game.World.Facts.Exists("guest-beloved", "npc_woodward"));

        game.Debug_ClearCamp();
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');
        Assert.False(game.World.Facts.Exists("guest-beloved", "npc_woodward"));
        game.ApplyKey('r');
        Assert.True(game.Player.GuestLossMemoryConsumed);
        Assert.False(game.Player.GuestLossMemoryArmed);
    }

    [Fact]
    public void RaidsTopic_ReadsTheLiveWatchAndLevyStates()
    {
        var game = new Game(42);
        game.Debug_SetStores(1);
        game.Debug_CallLevy();
        game.World.Facts.Add("event", "watch_posted", game.World.SettlementName,
            "The watch once stood and has now stood down.");
        var villager = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        NpcTests.BumpNpc(game, villager);

        var raids = game.Topics.Single(t => t.Label == "The goblin raids").Answer;
        Assert.Contains("watch has stood down", raids, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("levy still stands", raids, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExposedBeastWarmth_AddsOneAfterWeather_AndRecognitionDoesNotRepeat()
    {
        var plain = new Game(42);
        var warmed = new Game(42);
        Pos camp = Enumerable.Range(0, warmed.World.Overworld.Height)
            .SelectMany(y => Enumerable.Range(0, warmed.World.Overworld.Width).Select(x => new Pos(x, y)))
            .First(p => warmed.World.Overworld[p] == Terrain.Grass
                && Directions.All8.Any(d => warmed.World.Overworld.Walkable(p.Plus(d.dx, d.dy))));
        warmed.Debug_SetPlayerPos(camp);
        plain.Debug_SetPlayerPos(camp);
        warmed.Player.Hp = plain.Player.Hp = 1;
        warmed.Player.Rations = plain.Player.Rations = 1;
        var beastPos = Directions.All8.Select(d => camp.Plus(d.dx, d.dy))
            .First(p => warmed.CurrentMap.Walkable(p));
        warmed.Debug_SetMount(new Mount { Kind = MountKind.Mule, Pos = beastPos, Area = Area.Valley });

        plain.ApplyKey('m');
        warmed.ApplyKey('m');

        Assert.Equal(plain.Player.Hp + 1, warmed.Player.Hp);
        Assert.True(warmed.Player.MuleRecognized);
        Assert.Equal(1, warmed.BeastWarmCamps);
    }

    [Theory]
    [InlineData(WeatherFamily.Calm)]
    [InlineData(WeatherFamily.Wind)]
    [InlineData(WeatherFamily.Wet)]
    [InlineData(WeatherFamily.Cold)]
    public void ExposedBeastWarmth_IsExactlyOneAcrossEveryLowlandWeather(WeatherFamily weather)
    {
        static Pos Camp(Game game) => Enumerable.Range(0, game.World.Overworld.Height)
            .SelectMany(y => Enumerable.Range(0, game.World.Overworld.Width).Select(x => new Pos(x, y)))
            .First(p => game.World.Overworld[p] == Terrain.Grass
                && Directions.All8.Any(d => game.World.Overworld.Walkable(p.Plus(d.dx, d.dy))));

        var plain = new Game(7);
        var warm = new Game(7);
        var camp = Camp(warm);
        plain.Debug_SetPlayerPos(camp);
        warm.Debug_SetPlayerPos(camp);
        plain.Debug_SetWeather(ClimateBand.Lowlands, weather);
        warm.Debug_SetWeather(ClimateBand.Lowlands, weather);
        plain.Player.Hp = warm.Player.Hp = 1;
        plain.Player.Rations = warm.Player.Rations = 1;
        var beside = Directions.All8.Select(d => camp.Plus(d.dx, d.dy))
            .First(p => warm.CurrentMap.Walkable(p));
        warm.Debug_SetMount(new Mount { Kind = MountKind.Courser, Pos = beside, Area = Area.Valley });

        plain.ApplyKey('m');
        warm.ApplyKey('m');

        Assert.Equal(plain.Player.Hp + 1, warm.Player.Hp);
        Assert.True(warm.Player.CourserRecognized);
    }

    [Fact]
    public void WaystoneShelterGetsNoBeastBonus_AndStableBeastsDoNotStack()
    {
        ulong seed = Enumerable.Range(1, 500).Select(i => (ulong)i)
            .First(s => WorldTwistCatalog.ForCycle(s, WorldTwistCatalog.FirstTier)
                == WorldTwist.HeldRoad);
        static Game AtHeldRoad(ulong seed)
        {
            var game = new Game(seed);
            while (game.Cycle < WorldTwistCatalog.FirstTier)
            {
                game.Debug_SetMode(MapMode.Overworld);
                game.Debug_ClearCamp();
                game.Debug_SetPlayerPos(game.World.GatePos);
                game.ApplyKey('>');
                game.ApplyKey('>');
            }
            return game;
        }
        var sheltered = AtHeldRoad(seed);
        var plain = AtHeldRoad(seed);
        sheltered.Debug_SetPlayerPos(sheltered.World.RoadMouthPos);
        plain.Debug_SetPlayerPos(plain.World.RoadMouthPos);
        sheltered.ApplyKey('>');
        plain.ApplyKey('>');
        var waystone = sheltered.World.Waystones[0];
        sheltered.Debug_SetPlayerPos(waystone);
        plain.Debug_SetPlayerPos(waystone);
        sheltered.Player.Hp = plain.Player.Hp = 1;
        sheltered.Player.Rations = plain.Player.Rations = 1;
        var beside = Directions.All8.Select(d => waystone.Plus(d.dx, d.dy))
            .First(p => sheltered.CurrentMap.Walkable(p));
        sheltered.Debug_SetMount(new Mount { Kind = MountKind.Mule, Pos = beside, Area = Area.Road });
        sheltered.Stable.Add(new Mount { Kind = MountKind.Courser, Pos = beside, Area = Area.Road });
        sheltered.Stable.Add(new Mount { Kind = MountKind.FellPony, Pos = beside, Area = Area.Road });

        plain.ApplyKey('m');
        sheltered.ApplyKey('m');

        Assert.Equal(plain.Player.Hp, sheltered.Player.Hp);
        Assert.Equal(0, sheltered.BeastWarmCamps);
        Assert.True(sheltered.Player.MuleRecognized);
        Assert.False(sheltered.Player.CourserRecognized);
        Assert.False(sheltered.Player.FellPonyRecognized);
    }

    [Fact]
    public void ScarAndMendFacts_UseStableIds_AndTheBraceBecomesPermanent()
    {
        var game = new Game(42);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        Assert.Contains(game.World.Facts.All,
            f => f.Type == "scar" && f.Subject == DeathsToll.IdOf(ScarId.TakenEye));

        game.Player.Scars.Remove(ScarId.TakenEye);
        game.Player.Scars.Add(ScarId.CrushedHand);
        game.Player.Coin = DeathsToll.BraceCoin;
        NpcTests.BumpNpc(game, game.World.Smith);
        char brace = (char)('1' + game.Topics.Count
            + game.Offers.ToList().FindIndex(o => o.Good == TradeGood.Brace));
        game.ApplyKey(brace);

        Assert.True(game.Player.FittedBrace);
        Assert.Contains(game.World.Facts.All,
            f => f.Type == "scar-mended" && f.Subject == "crushed_hand");
    }

    [Fact]
    public void EveryMendFact_HasOneAftercareConsumerOnItsApprovedSurface()
    {
        static void AssertAftercare(string scarId, string marker, Func<Game, Npc> npc)
        {
            var game = new Game(42);
            game.World.Facts.Add("scar-mended", scarId, game.Player.Name, "The mend stands.");
            for (int i = 0; i < 8 && !game.Log.Entries.Any(e => e.Text.Contains(marker)); i++)
            {
                NpcTests.BumpNpc(game, npc(game));
                game.ApplyKey(' ');
            }
            Assert.Single(game.Log.Entries, e => e.Text.Contains(marker));
            for (int i = 0; i < 3; i++)
            {
                NpcTests.BumpNpc(game, npc(game));
                game.ApplyKey(' ');
            }
            Assert.Single(game.Log.Entries, e => e.Text.Contains(marker));
        }

        AssertAftercare("taken_eye", "doorway light",
            g => g.World.Npcs.First(n => n.Id == "npc_herbwife"));
        AssertAftercare("crushed_hand", "each joint", g => g.World.Smith);
        AssertAftercare("haunted_look", "space behind", g => g.World.Skald);
    }

    [Fact]
    public void FittedBrace_LightensOnlyWieldedParries_AndAScarSuppressesIt()
    {
        static int ParrySpend(bool scarred)
        {
            var game = EmptyCamp();
            var line = OpenLine(game);
            game.Debug_SetPlayerPos(line.A);
            game.Debug_GrantGear("woodaxe");
            game.Player.FittedBrace = true;
            if (scarred) game.Player.Scars.Add(ScarId.CrushedHand);
            var foe = new Monster { Kind = MonsterKind.Goblin, Pos = line.B, SiteId = "goblin-camp", Hp = 30 };
            foe.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = game.Player.Pos };
            game.Monsters.Add(foe);
            int before = game.Player.Stamina;
            game.ApplyKey('a');
            return before - game.Player.Stamina;
        }

        Assert.Equal(1, ParrySpend(scarred: false));
        Assert.Equal(2, ParrySpend(scarred: true));

        var unarmed = EmptyCamp();
        var line = OpenLine(unarmed);
        unarmed.Debug_SetPlayerPos(line.A);
        unarmed.Player.FittedBrace = true;
        var foe = new Monster { Kind = MonsterKind.Goblin, Pos = line.B, SiteId = "goblin-camp", Hp = 30 };
        foe.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = unarmed.Player.Pos };
        unarmed.Monsters.Add(foe);
        int before = unarmed.Player.Stamina;
        unarmed.ApplyKey('a');
        Assert.Equal(GuardBreak.ParryCost, before - unarmed.Player.Stamina);
    }

    [Fact]
    public void TollFill_ScalesAfterTierFour_BeforeWillAndAtTheCap()
    {
        Assert.Equal(100, DeathsToll.FillFor(heavy: false, will: 5, tier: 4));
        Assert.Equal(110, DeathsToll.FillFor(heavy: false, will: 5, tier: 5));
        Assert.Equal(120, DeathsToll.FillFor(heavy: false, will: 5, tier: 6));
        Assert.Equal(130, DeathsToll.FillFor(heavy: false, will: 5, tier: 7));
        Assert.Equal(140, DeathsToll.FillFor(heavy: false, will: 5, tier: 8));
        Assert.Equal(140, DeathsToll.FillFor(heavy: false, will: 5, tier: 20));
        Assert.Equal(120, DeathsToll.FillFor(heavy: false, will: 7, tier: 8));
        Assert.Equal(200, DeathsToll.FillFor(heavy: true, will: 5, tier: 8));
        Assert.Equal(40, DeathsToll.FillFor(heavy: false, will: 20, tier: 8));
    }

    [Fact]
    public void SheetAndSnapshot_ShowBurdenScarsBraceAndExactTierContribution()
    {
        var game = new Game(42);
        game.Player.FittedBrace = true;
        game.Player.Scars.Add(ScarId.TakenEye);
        game.ApplyKey('c');
        var text = string.Join('\n', Presenter.Render(game, 120, 40).ToTextLines());
        var snapshot = game.TakeSnapshot();

        Assert.Contains("Burden", text);
        Assert.Contains("taken eye", text);
        Assert.Contains("fitted brace", text);
        Assert.True(snapshot.FittedBrace);
        Assert.True(snapshot.BraceActive);
        Assert.Equal(DeathsToll.TierContribution(game.World.Tier), snapshot.TollTierContribution);
    }

    [Fact]
    public void FinalOaths_AreEndAppended_AndAlterOnlyTheirNamedMagnitudes()
    {
        Assert.Equal(OathId.ClosedDoor, OathCatalog.All[^2].Id);
        Assert.Equal(OathId.LongCount, OathCatalog.All[^1].Id);
        Assert.Equal(9, OathCatalog.All.Count);

        var closed = CrossUnder('8');
        closed.Debug_ClearCamp();
        Assert.Equal(3, closed.Regard);
        Assert.Equal(1, closed.RegardRung);
        Assert.True(closed.TakeSnapshot().ClosedDoorStands);

        var longCount = CrossUnder('9');
        longCount.Player.Toll = 10;
        longCount.ApplyKey('.');
        longCount.ApplyKey('.');
        Assert.Equal(9, longCount.Player.Toll);
        Assert.True(longCount.TakeSnapshot().LongCountStands);
    }

    [Fact]
    public void FinalOaths_HavePinnedWeightsBurdenLegendAndWorldScope()
    {
        Assert.Equal(1, OathCatalog.Def(OathId.ClosedDoor).Weight);
        Assert.Equal(1, OathCatalog.Def(OathId.LongCount).Weight);
        Assert.Equal([2, 4, 6],
            new[] { 1, 2, 3 }.Select(r => SteadRegard.Threshold(r, closedDoor: true)).ToArray());

        var game = new Game(42);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('8');
        game.ApplyKey('9');
        game.ApplyKey('>');
        Assert.Equal(2, game.World.Burden);
        int legendBeforeHonor = game.Player.Legend;

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.ApplyKey('>');
        game.ApplyKey('>');
        Assert.True(game.Player.Legend >= legendBeforeHonor + 20);
        Assert.False(game.ClosedDoorStands);
        Assert.DoesNotContain(OathId.LongCount, game.World.Oaths);
    }
}
