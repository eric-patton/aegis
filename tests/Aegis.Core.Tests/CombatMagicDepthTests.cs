using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>Focused acceptance for V1-07 combat and magic depth (D-163).</summary>
public class CombatMagicDepthTests
{
    [Fact]
    public void Catalogs_EndAppendTheNewKindsWordsAndKnacks_InThresholdOrder()
    {
        Assert.Equal(12, (int)MonsterKind.RuneTongue);
        Assert.Equal(5, (int)SpellId.Severing);
        Assert.Equal(6, (int)SpellId.Mending);
        Assert.Equal(7, SpellCatalog.All.Count);
        Assert.Equal(new[] { SpellId.Severing, SpellId.Mending }, SpellCatalog.All.Skip(5).Select(s => s.Id));
        Assert.Equal(
            new[] { SpellId.Spark, SpellId.Severing, SpellId.Mending },
            SpellCatalog.CreationPool);
        var preferenceTails = Enum.GetValues<SiteKind>()
            .Select(kind => SpellCatalog.StonePreference(kind).Skip(5).ToArray())
            .ToList();
        Assert.All(preferenceTails,
            tail => Assert.Equal(new[] { SpellId.Severing, SpellId.Mending }, tail.Order()));
        Assert.Contains(preferenceTails,
            tail => tail.SequenceEqual(new[] { SpellId.Severing, SpellId.Mending }));
        Assert.Contains(preferenceTails,
            tail => tail.SequenceEqual(new[] { SpellId.Mending, SpellId.Severing }));

        var levelSix = PerkCatalog.Choices.Where(c => c.Level == 6).ToList();
        Assert.Equal(
            new[] { SkillId.Blades, SkillId.Hafted, SkillId.Brawling, SkillId.Warding, SkillId.Ranged },
            levelSix.Select(c => c.Skill));
        Assert.All(levelSix, choice => Assert.Equal(2, choice.Options.Length));
        Assert.All(PerkCatalog.Choices.TakeWhile(c => c.Level < 6), choice => Assert.True(choice.Level < 6));

        var spellcraft = PerkCatalog.Choices.Where(c => c.Skill == SkillId.Spellcraft).ToList();
        Assert.Equal(new[] { 2, 4 }, spellcraft.Select(c => c.Level));
        Assert.Equal(
            new[] { PerkId.FullWord, PerkId.SpareSyllable, PerkId.AnsweringWord, PerkId.DeepWell },
            spellcraft.SelectMany(c => c.Options).Select(o => o.Id));
    }

    [Fact]
    public void PlayerFlank_RequiresExactOpposition_AndAddsBladeBloodAndTwoPressure()
    {
        var flanked = Arena(41, MonsterKind.RuneTongue, out var target, out var opposite, out _);
        EquipBlade(flanked);
        flanked.Debug_SetGuest(Fellow(opposite));

        var broad = Arena(41, MonsterKind.RuneTongue, out var broadTarget, out _, out var side);
        EquipBlade(broad);
        broad.Debug_SetGuest(Fellow(side));

        int flankHp = target.Hp;
        int broadHp = broadTarget.Hp;
        flanked.ApplyKey(KeyToward(flanked.Player.Pos, target.Pos));
        broad.ApplyKey(KeyToward(broad.Player.Pos, broadTarget.Pos));

        Assert.Equal((broadHp - broadTarget.Hp) + 1, flankHp - target.Hp);
        Assert.Equal(3, target.PostureDmg);
        Assert.Equal(1, broadTarget.PostureDmg);
        Assert.Equal(1, flanked.PlayerFlanks);
        Assert.Equal(0, broad.PlayerFlanks);
    }

    [Fact]
    public void EnemyFlank_RequiresExactOpposition_AndAddsOneCommittedPressure()
    {
        var flanked = EnemyFlankArena(52, opposite: true, out var striker);
        var broad = EnemyFlankArena(52, opposite: false, out var broadStriker);

        striker.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = flanked.Player.Pos };
        broadStriker.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = broad.Player.Pos };
        flanked.ApplyKey('.');
        broad.ApplyKey('.');

        Assert.Equal(broad.Player.PostureDmg + 1, flanked.Player.PostureDmg);
        Assert.Equal(1, flanked.EnemyFlanks);
        Assert.Equal(0, broad.EnemyFlanks);
    }

    [Fact]
    public void LevelSixBladeAndRangedEdges_PayOnlyTheirNamedOpenings()
    {
        var edge = Arena(61, MonsterKind.RuneTongue, out var edgeTarget, out _, out _);
        EquipBlade(edge);
        edge.Player.Stance = Stance.Pressing;
        edge.Player.Perks.Add(PerkId.ForwardEdge);

        var plain = Arena(61, MonsterKind.RuneTongue, out var plainTarget, out _, out _);
        EquipBlade(plain);
        plain.Player.Stance = Stance.Pressing;

        edge.ApplyKey(KeyToward(edge.Player.Pos, edgeTarget.Pos));
        plain.ApplyKey(KeyToward(plain.Player.Pos, plainTarget.Pos));
        Assert.Equal((999 - plainTarget.Hp) + 1, 999 - edgeTarget.Hp);

        var drawn = Arena(73, MonsterKind.RuneTongue, out var drawnTarget, out _, out _);
        EquipBow(drawn);
        drawn.Player.Stance = Stance.Pressing;
        drawn.Player.Perks.Add(PerkId.ForwardDraw);

        var ordinary = Arena(73, MonsterKind.RuneTongue, out var ordinaryTarget, out _, out _);
        EquipBow(ordinary);
        ordinary.Player.Stance = Stance.Pressing;

        LooseAt(drawn, drawnTarget);
        LooseAt(ordinary, ordinaryTarget);
        Assert.Equal((999 - ordinaryTarget.Hp) + 1, 999 - drawnTarget.Hp);

        var waiting = Arena(74, MonsterKind.RuneTongue, out var waitingTarget, out _, out _);
        EquipBow(waiting);
        waiting.Player.Perks.Add(PerkId.WaitingString);
        waitingTarget.Intent = new Intent
        {
            Kind = IntentKind.FallingWord,
            TargetCell = waiting.Player.Pos,
            TurnsUntilResolve = 3,
            HitPointsAtCommit = waitingTarget.Hp,
        };
        LooseAt(waiting, waitingTarget);
        Assert.Equal(1, waitingTarget.PostureDmg);
    }

    [Fact]
    public void LevelSixParryEdges_ChangeReturnCostAndRecovery()
    {
        var blade = Arena(81, MonsterKind.RuneTongue, out var bladeTarget, out _, out _);
        EquipBlade(blade);
        blade.Player.Perks.Add(PerkId.ReturningEdge);
        blade.Player.Perks.Add(PerkId.EasyGuard);
        blade.Player.PostureDmg = 3;
        bladeTarget.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = blade.Player.Pos };
        int bladeWind = blade.Player.Stamina;
        blade.ApplyKey('a');

        Assert.Equal(bladeWind - GuardBreak.ParryCost, blade.Player.Stamina);
        Assert.Equal(5, bladeTarget.PostureDmg);
        Assert.Equal(2, blade.Player.PostureDmg);

        var bare = Arena(82, MonsterKind.RuneTongue, out var bareTarget, out _, out _);
        bare.Player.Weapon = null;
        bare.Player.Perks.Add(PerkId.CaughtWrist);
        bareTarget.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = bare.Player.Pos };
        int bareWind = bare.Player.Stamina;
        bare.ApplyKey('a');
        Assert.Equal(bareWind - 1, bare.Player.Stamina);
    }

    [Fact]
    public void LevelSixHaftAndHands_ChangeHeaveRootAndShove()
    {
        var whole = Arena(91, MonsterKind.RuneTongue, out var wholeTarget, out _, out _);
        EquipAxe(whole);
        whole.Player.Stance = Stance.Pressing;
        whole.Player.Perks.Add(PerkId.WholeWeight);
        HeaveAt(whole, wholeTarget);
        Assert.Equal(4, wholeTarget.PostureDmg);

        var rooted = HeavyBlowAtBearer(92, rooted: true);
        var unrooted = HeavyBlowAtBearer(92, rooted: false);
        Assert.Equal(0, rooted.Player.PostureDmg);
        Assert.Equal(1, unrooted.Player.PostureDmg);

        var hands = Arena(93, MonsterKind.RuneTongue, out var shoved, out _, out _);
        hands.Player.Weapon = null;
        hands.Player.Stance = Stance.Pressing;
        hands.Player.Perks.Add(PerkId.CrowdingHands);
        Pos start = shoved.Pos;
        int dx = Math.Sign(shoved.Pos.X - hands.Player.Pos.X);
        int dy = Math.Sign(shoved.Pos.Y - hands.Player.Pos.Y);
        hands.ApplyKey(KeyToward(hands.Player.Pos, shoved.Pos));
        Assert.Equal(start.Plus(dx * 2, dy * 2), shoved.Pos);
    }

    [Fact]
    public void DeepSet_TurnsOneMoreBloodWhileGuardedAndArmored()
    {
        var deep = HeavyBloodAtBearer(101, deepSet: true);
        var plain = HeavyBloodAtBearer(101, deepSet: false);
        Assert.Equal(plain.Player.Hp + 1, deep.Player.Hp);
    }

    [Fact]
    public void RuneTongue_IsAbsentBelowTierFive_AndExactlyOneLegalSpawnAfterward()
    {
        for (ulong seed = 1; seed <= 12; seed++)
        {
            for (int tier = 1; tier < 5; tier++)
                Assert.DoesNotContain(WorldGen.Generate(seed, tier).Sites.SelectMany(s => s.Spawns),
                    spawn => spawn.Kind == MonsterKind.RuneTongue);

            for (int tier = 5; tier <= 8; tier++)
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);
                var placed = a.Sites.SelectMany(site => site.Spawns
                    .Where(spawn => spawn.Kind == MonsterKind.RuneTongue)
                    .Select(spawn => (Site: site, Spawn: spawn))).Single();
                var twin = b.Sites.SelectMany(site => site.Spawns
                    .Where(spawn => spawn.Kind == MonsterKind.RuneTongue)
                    .Select(spawn => (Site: site, Spawn: spawn))).Single();

                Assert.Equal(placed.Site.Id, twin.Site.Id);
                Assert.Equal(placed.Spawn, twin.Spawn);
                Assert.True(placed.Site.Map.Walkable(placed.Spawn.Pos));
                Assert.NotEqual(placed.Site.EntryPos, placed.Spawn.Pos);
                Assert.DoesNotContain(placed.Site.Spawns,
                    spawn => spawn.Kind != MonsterKind.RuneTongue && spawn.Pos == placed.Spawn.Pos);
            }
        }
    }

    [Fact]
    public void GoblinCry_AlertsTheUnaware_WithoutMovingAuthoredDormancy()
    {
        var game = Arena(108, MonsterKind.Goblin, out var crier, out var opposite, out var side);
        crier.Intent = new Intent { Kind = IntentKind.RallyCry, TargetCell = crier.Pos };
        var ordinary = new Monster
        {
            Kind = MonsterKind.Goblin,
            Pos = opposite,
            SiteId = game.CurrentSite!.Id,
            Hp = 20,
            MaxHp = 20,
            Aware = false,
        };
        var dormant = new Monster
        {
            Kind = MonsterKind.Goblin,
            Pos = side,
            SiteId = game.CurrentSite.Id,
            Hp = 20,
            MaxHp = 20,
            Aware = false,
            Dormant = true,
        };
        game.Monsters.Add(ordinary);
        game.Monsters.Add(dormant);
        Pos dormantStart = dormant.Pos;

        game.ApplyKey('.');

        Assert.True(ordinary.Aware);
        Assert.True(dormant.Aware);
        Assert.True(dormant.Dormant);
        Assert.Equal(dormantStart, dormant.Pos);
    }

    [Fact]
    public void BoardlessWarder_ClosesOnce_AndNeverRaisesAnotherLoft()
    {
        var game = Arena(109, MonsterKind.Warder, out var warder, out _, out _);
        Pos stand = FindWalkableAtDistance(game, warder.Pos, minimum: 4);
        game.Debug_SetPlayerPos(stand);
        warder.BoardBroken = true;
        int before = warder.Pos.Chebyshev(game.Player.Pos);

        game.ApplyKey('.');
        int after = warder.Pos.Chebyshev(game.Player.Pos);
        Assert.True(after < before);
        Assert.Null(warder.Intent);
        Assert.Equal(1, game.BoardlessWarderClosures);

        for (int i = 0; i < 8 && warder.Alive; i++) game.ApplyKey('.');
        Assert.NotEqual(IntentKind.LoftedStone, warder.Intent?.Kind);
        Assert.Equal(1, game.BoardlessWarderClosures);
    }

    [Fact]
    public void SeveredSweep_MarksThreeCells_DodgesByFeet_AndCanBeParried()
    {
        var dodge = Arena(110, MonsterKind.Severed, out var sweeper, out _, out _);
        Pos[] footprint = FacingArc(sweeper.Pos, dodge.Player.Pos);
        Assert.Equal(3, footprint.Length);
        sweeper.Intent = new Intent
        {
            Kind = IntentKind.SeveredSweep,
            TargetCell = dodge.Player.Pos,
            Footprint = footprint,
        };
        Pos escape = Directions.All8.Select(d => dodge.Player.Pos.Plus(d.dx, d.dy))
            .First(p => dodge.CurrentMap.Walkable(p)
                && !footprint.Contains(p)
                && !dodge.Monsters.Any(m => m.Alive && m.Pos == p));
        int hp = dodge.Player.Hp;
        dodge.ApplyKey(KeyToward(dodge.Player.Pos, escape));
        Assert.Equal(hp, dodge.Player.Hp);
        Assert.Equal(1, dodge.SweepsDodged);

        var parry = Arena(110, MonsterKind.Severed, out var parried, out _, out _);
        EquipBlade(parry);
        parried.Intent = new Intent
        {
            Kind = IntentKind.SeveredSweep,
            TargetCell = parry.Player.Pos,
            Footprint = FacingArc(parried.Pos, parry.Player.Pos),
        };
        int parryHp = parry.Player.Hp;
        parry.ApplyKey('a');
        Assert.Equal(parryHp, parry.Player.Hp);
        Assert.Equal(GuardBreak.ParryPressure, parried.PostureDmg);
        Assert.Equal(1, parry.SweepsLanded);
    }

    [Fact]
    public void SweepReadTiers_KeepTheFootprintFair_ThenNameWeightAndParry()
    {
        var game = Arena(107, MonsterKind.Severed, out var sweeper, out _, out _);
        sweeper.Intent = new Intent
        {
            Kind = IntentKind.SeveredSweep,
            TargetCell = game.Player.Pos,
            Footprint = FacingArc(sweeper.Pos, game.Player.Pos),
        };

        var blur = Presenter.Render(game);
        Assert.Equal(3, CountMarkedCells(blur));
        Assert.DoesNotContain("severed sweep marked", string.Join("\n", blur.ToTextLines()));

        game.Player.WitnessTell(MonsterKind.Severed);
        string read = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("severed sweep marked", read);
        Assert.Contains("falls this turn", read);
        Assert.DoesNotContain("heavy; parry legal", read);

        game.Player.WitnessTell(MonsterKind.Severed);
        game.Player.WitnessTell(MonsterKind.Severed);
        string keen = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("heavy; parry legal", keen);
    }

    [Fact]
    public void FallingWord_UsesItsFiveCellCross_BypassesIron_AndAppliesWillResistance()
    {
        var bare = FallingArena(111, will: 5, armored: false);
        var iron = FallingArena(111, will: 5, armored: true);
        var resisted = FallingArena(111, will: 9, armored: true);
        int bareHp = bare.Player.Hp;
        int ironHp = iron.Player.Hp;
        int resistedHp = resisted.Player.Hp;

        bare.ApplyKey('.');
        iron.ApplyKey('.');
        resisted.ApplyKey('.');

        Assert.Equal(bareHp - bare.Player.Hp, ironHp - iron.Player.Hp);
        Assert.Equal(4, (ironHp - iron.Player.Hp) - (resistedHp - resisted.Player.Hp));
        Assert.Equal(0, iron.Player.Armor!.Wear);
        Assert.Equal(0, bare.Player.WillResistance);
        Assert.Equal(4, resisted.Player.WillResistance);
        Assert.Equal(1, resisted.HostileWorkingsResisted);
    }

    [Fact]
    public void HostileWords_InterruptOnBloodAndPosture_ThenShowRecovery()
    {
        var blood = Arena(121, MonsterKind.RuneTongue, out var bloodCaster, out _, out _);
        bloodCaster.Intent = FallingIntent(bloodCaster, blood.Player.Pos, turns: 2);
        bloodCaster.Hp--;
        blood.ApplyKey('.');
        Assert.Null(bloodCaster.Intent);
        Assert.Equal(1, bloodCaster.RecoveryTurns);
        Assert.Equal(1, blood.HostileWorkingsInterrupted);
        blood.ApplyKey('.');
        Assert.Equal(0, bloodCaster.RecoveryTurns);

        var posture = Arena(122, MonsterKind.RuneTongue, out var postureCaster, out _, out _);
        postureCaster.PostureDmg = postureCaster.MaxPosture - 1;
        postureCaster.Intent = FallingIntent(postureCaster, posture.Player.Pos, turns: 2);
        EquipBow(posture);
        posture.Player.Perks.Add(PerkId.WaitingString);
        LooseAt(posture, postureCaster);
        Assert.Null(postureCaster.Intent);
        Assert.Equal(1, postureCaster.RecoveryTurns);
        Assert.Equal(1, posture.HostileWorkingsInterrupted);
    }

    [Fact]
    public void BindingWord_FollowsTheBearer_RespectsResourceFloors_AndBreaksOnLostSight()
    {
        var landed = Arena(131, MonsterKind.RuneTongue, out var caster, out _, out _);
        landed.Player.Attributes[Attr.Will] = 9;
        landed.Player.Stamina = 0;
        landed.Player.Focus = 0;
        caster.Intent = new Intent
        {
            Kind = IntentKind.BindingWord,
            TargetCell = landed.Player.Pos,
            TurnsUntilResolve = 1,
            HitPointsAtCommit = caster.Hp,
        };
        var ordinary = Arena(131, MonsterKind.RuneTongue, out var ordinaryCaster, out _, out _);
        ordinary.Player.Attributes[Attr.Will] = 9;
        ordinary.Player.Stamina = 0;
        ordinary.Player.Focus = 0;
        ordinaryCaster.ExposedTurns = 2;
        ordinary.ApplyKey('.');
        landed.ApplyKey('.');
        Assert.Equal(ordinary.Player.Stamina - 1, landed.Player.Stamina);
        Assert.Equal(0, landed.Player.Focus);
        Assert.Equal(1, landed.HostileWorkingsLanded);

        var broken = Arena(132, MonsterKind.RuneTongue, out var hiddenCaster, out _, out _);
        Pos hidden = FindCellWithoutSight(broken, hiddenCaster.Pos);
        broken.Debug_SetPlayerPos(hidden);
        hiddenCaster.Intent = new Intent
        {
            Kind = IntentKind.BindingWord,
            TargetCell = hidden,
            TurnsUntilResolve = 1,
            HitPointsAtCommit = hiddenCaster.Hp,
        };
        broken.ApplyKey('.');
        Assert.Equal(1, broken.HostileWorkingsInterrupted);
        Assert.Equal(0, broken.HostileWorkingsLanded);
    }

    [Fact]
    public void Severing_CancelsOnlyMagicalIntent_AddsPressureGrowthAndComposedRefunds()
    {
        var game = Arena(141, MonsterKind.RuneTongue, out var caster, out _, out _);
        game.Debug_LearnSpell(SpellId.Severing);
        game.Player.Perks.Add(PerkId.SpareSyllable);
        game.Player.Perks.Add(PerkId.AnsweringWord);
        game.Player.Skills.AddUse(SkillId.Spellcraft);
        game.Player.Focus = game.Player.MaxFocus;
        caster.Intent = FallingIntent(caster, game.Player.Pos, turns: 2);

        SayLine(game, SpellId.Severing, KeyToward(game.Player.Pos, caster.Pos));

        Assert.Null(caster.Intent);
        Assert.Equal(2, caster.PostureDmg);
        Assert.Equal(2, game.Player.Skills.Uses(SkillId.Spellcraft));
        Assert.Equal(game.Player.MaxFocus, game.Player.Focus);
        Assert.Equal(1, game.WorkingCasts(SpellId.Severing));
        Assert.Equal(1, game.WorkingEffects(SpellId.Severing));
        Assert.True(game.Player.SeveringLineHeard);

        var physical = Arena(142, MonsterKind.RuneTongue, out var fighter, out _, out _);
        physical.Debug_LearnSpell(SpellId.Severing);
        fighter.Intent = new Intent
        {
            Kind = IntentKind.CrushingBlow,
            TargetCell = physical.Player.Pos,
            TurnsUntilResolve = 3,
        };
        int focus = physical.Player.Focus = physical.Player.MaxFocus;
        SayLine(physical, SpellId.Severing, KeyToward(physical.Player.Pos, fighter.Pos));
        Assert.NotNull(fighter.Intent);
        Assert.Equal(focus - 2, physical.Player.Focus);
        Assert.Equal(0, physical.Player.Skills.Uses(SkillId.Spellcraft));
    }

    [Fact]
    public void Mending_RefusesFull_ThenHealsWithoutTreatingTheWound()
    {
        var game = Arena(151, MonsterKind.RuneTongue, out var bystander, out _, out _);
        bystander.Hp = 0;
        game.Debug_LearnSpell(SpellId.Mending);
        game.Player.Focus = game.Player.MaxFocus;
        int fullFocus = game.Player.Focus;
        int turn = game.Turn;
        Say(game, SpellId.Mending);
        Assert.Equal(turn, game.Turn);
        Assert.Equal(fullFocus, game.Player.Focus);

        game.Player.Hp -= 8;
        game.Player.WoundedTurns = 20;
        game.Player.Perks.Add(PerkId.FullWord);
        int wound = game.Player.WoundedTurns;
        int before = game.Player.Hp;
        Say(game, SpellId.Mending);
        Assert.True(game.Player.MendingHeld);
        game.ApplyKey('.');

        Assert.False(game.Player.MendingHeld);
        Assert.Equal(Math.Min(game.Player.EffectiveMaxHp, before + 6), game.Player.Hp);
        Assert.Equal(wound - 2, game.Player.WoundedTurns);
        Assert.Equal(1, game.WorkingEffects(SpellId.Mending));
    }

    [Fact]
    public void FullWordAndDeepWell_DeepenWardAndFocus_AndNewKnowledgePersists()
    {
        var game = new Game(161);
        game.Player.Perks.Add(PerkId.FullWord);
        game.Player.Perks.Add(PerkId.DeepWell);
        game.Debug_LearnSpell(SpellId.Ward);
        game.Debug_LearnSpell(SpellId.Severing);
        game.Debug_LearnSpell(SpellId.Mending);
        Assert.Equal(4, game.Player.MaxFocus);

        game.Debug_SetMode(MapMode.Site);
        game.Player.Focus = game.Player.MaxFocus;
        Say(game, SpellId.Ward);
        Assert.Equal(Game.WardHeldTurns, game.Player.WardTurns);

        game.Player.Hp = 0;
        game.Debug_ForceDeathCheck();
        Assert.True(game.Player.HasSpell(SpellId.Severing));
        Assert.True(game.Player.HasSpell(SpellId.Mending));
        Assert.True(game.Player.HasPerk(PerkId.FullWord));

        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.True(game.Player.HasSpell(SpellId.Severing));
        Assert.True(game.Player.HasSpell(SpellId.Mending));
        Assert.True(game.Player.HasPerk(PerkId.DeepWell));
    }

    [Fact]
    public void SnapshotAndSheet_ExposeResistanceHostileStateAndWorkingDiagnostics()
    {
        var game = Arena(171, MonsterKind.RuneTongue, out var caster, out _, out _);
        game.Player.Attributes[Attr.Will] = 8;
        caster.Intent = FallingIntent(caster, game.Player.Pos, turns: 2);
        game.Debug_LearnSpell(SpellId.Mending);
        game.Player.Hp--;
        game.Player.Focus = game.Player.MaxFocus;
        Say(game, SpellId.Mending);

        var snap = game.TakeSnapshot();
        Assert.Equal(3, snap.WillResistance);
        Assert.True(snap.MendingLoaded);
        Assert.Contains("runetongue:fallingword:1", snap.HostileMagic);
        Assert.Contains("mending:1", snap.WorkingCasts);

        game.ApplyKey('c');
        string screen = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("Will resist 3", screen);
    }

    [Fact]
    public void NewCombatAndMagicState_ReplaysExactlyFromTheJournal()
    {
        const ulong seed = 181;
        var live = Arena(seed, MonsterKind.RuneTongue, out var caster, out _, out _);
        live.Debug_LearnSpell(SpellId.Severing);
        live.Player.Focus = live.Player.MaxFocus;
        caster.Intent = FallingIntent(caster, live.Player.Pos, turns: 2);
        var journal = new StringBuilder();
        live.KeyApplied += key => journal.Append(key);
        SayLine(live, SpellId.Severing, KeyToward(live.Player.Pos, caster.Pos));

        var replay = Arena(seed, MonsterKind.RuneTongue, out var replayCaster, out _, out _);
        replay.Debug_LearnSpell(SpellId.Severing);
        replay.Player.Focus = replay.Player.MaxFocus;
        replayCaster.Intent = FallingIntent(replayCaster, replay.Player.Pos, turns: 2);
        foreach (char key in journal.ToString()) replay.ApplyKey(key);

        var a = live.TakeSnapshot();
        var b = replay.TakeSnapshot();
        Assert.Equal(a.WorkingCasts, b.WorkingCasts);
        Assert.Equal(a.WorkingEffects, b.WorkingEffects);
        Assert.Equal(a.HostileWorkingsInterrupted, b.HostileWorkingsInterrupted);
        Assert.Equal(a.GuardWorn, b.GuardWorn);
        Assert.Equal(a.Focus, b.Focus);
        Assert.Equal(a.Turn, b.Turn);
    }

    private static Game Arena(ulong seed, MonsterKind kind, out Monster target, out Pos opposite, out Pos side)
    {
        var game = new Game(seed);
        game.Debug_SetMode(MapMode.Site);
        foreach (var monster in game.Monsters) monster.Hp = 0;
        var map = game.CurrentMap;
        foreach (int y in Enumerable.Range(2, map.Height - 4))
            foreach (int x in Enumerable.Range(2, map.Width - 4))
                foreach (var (dx, dy) in Directions.All8)
                {
                    var center = new Pos(x, y);
                    var bearer = center.Plus(dx, dy);
                    var far = center.Plus(-dx, -dy);
                    var other = Directions.All8.Select(d => center.Plus(d.dx, d.dy))
                        .FirstOrDefault(p => p != bearer && p != far && map.Walkable(p));
                    var beyond = center.Plus(-dx * 2, -dy * 2);
                    if (!map.Walkable(center) || !map.Walkable(bearer) || !map.Walkable(far)
                        || !map.Walkable(beyond) || other == default)
                        continue;
                    target = new Monster
                    {
                        Kind = kind,
                        Pos = center,
                        SiteId = game.CurrentSite!.Id,
                        Hp = 999,
                        MaxHp = 999,
                        Aware = true,
                    };
                    game.Monsters.Add(target);
                    game.Debug_SetPlayerPos(bearer);
                    opposite = far;
                    side = other;
                    return game;
                }
        throw new InvalidOperationException("No open arena line.");
    }

    private static Game EnemyFlankArena(ulong seed, bool opposite, out Monster striker)
    {
        var game = Arena(seed, MonsterKind.RuneTongue, out striker, out _, out var side);
        int dx = Math.Sign(game.Player.Pos.X - striker.Pos.X);
        int dy = Math.Sign(game.Player.Pos.Y - striker.Pos.Y);
        Pos far = game.Player.Pos.Plus(dx, dy);
        var second = new Monster
        {
            Kind = MonsterKind.Goblin,
            Pos = opposite ? far : side,
            SiteId = game.CurrentSite!.Id,
            Hp = 99,
            MaxHp = 99,
            Aware = true,
            ExposedTurns = 99,
        };
        game.Monsters.Add(second);
        return game;
    }

    private static Game HeavyBlowAtBearer(ulong seed, bool rooted)
    {
        var game = Arena(seed, MonsterKind.RuneTongue, out var attacker, out _, out _);
        EquipAxe(game);
        game.Player.Stance = Stance.Guarded;
        if (rooted) game.Player.Perks.Add(PerkId.RootedHaft);
        attacker.Intent = new Intent
        {
            Kind = IntentKind.SeveredSweep,
            TargetCell = game.Player.Pos,
            Footprint = [game.Player.Pos],
        };
        game.ApplyKey('.');
        return game;
    }

    private static Game HeavyBloodAtBearer(ulong seed, bool deepSet)
    {
        var game = Arena(seed, MonsterKind.RuneTongue, out var attacker, out _, out _);
        game.Player.Armor = GearCatalog.Create("quilted_jack");
        game.Player.Stance = Stance.Guarded;
        if (deepSet) game.Player.Perks.Add(PerkId.DeepSet);
        attacker.Intent = new Intent { Kind = IntentKind.SunderingCut, TargetCell = game.Player.Pos };
        game.ApplyKey('.');
        return game;
    }

    private static Game FallingArena(ulong seed, int will, bool armored)
    {
        var game = Arena(seed, MonsterKind.RuneTongue, out var caster, out _, out _);
        game.Player.Attributes[Attr.Will] = will;
        game.Player.Hp = 100;
        if (armored) game.Player.Armor = GearCatalog.Create("quilted_jack");
        caster.Intent = FallingIntent(caster, game.Player.Pos, turns: 1);
        return game;
    }

    private static Intent FallingIntent(Monster caster, Pos center, int turns) => new()
    {
        Kind = IntentKind.FallingWord,
        TargetCell = center,
        TurnsUntilResolve = turns,
        Footprint =
        [
            center,
            center.Plus(0, -1),
            center.Plus(0, 1),
            center.Plus(-1, 0),
            center.Plus(1, 0),
        ],
        HitPointsAtCommit = caster.Hp,
    };

    private static Guest Fellow(Pos pos) => new()
    {
        Id = "test-fellow",
        Name = "the test fellow",
        Role = GuestRole.Crofter,
        Pos = pos,
        MaxHp = 20,
        Hp = 20,
    };

    private static void EquipBlade(Game game)
    {
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("grave_iron");
    }

    private static void EquipAxe(Game game)
    {
        game.Player.Attributes[Attr.Might] = 7;
        game.Player.Weapon = GearCatalog.Create("woodaxe");
    }

    private static void EquipBow(Game game)
    {
        game.Player.Attributes[Attr.Grace] = 7;
        game.Player.Bow = GearCatalog.Create("hunting_bow");
    }

    private static void LooseAt(Game game, Monster target)
    {
        game.ApplyKey('f');
        game.ApplyKey(KeyToward(game.Player.Pos, target.Pos));
    }

    private static void HeaveAt(Game game, Monster target)
    {
        game.ApplyKey('w');
        game.ApplyKey(KeyToward(game.Player.Pos, target.Pos));
        game.ApplyKey('.');
    }

    private static void Say(Game game, SpellId spell)
    {
        game.ApplyKey('z');
        game.ApplyKey((char)('1' + game.Player.Spells.IndexOf(spell)));
    }

    private static void SayLine(Game game, SpellId spell, char direction)
    {
        Say(game, spell);
        game.ApplyKey(direction);
    }

    private static Pos FindCellWithoutSight(Game game, Pos from)
    {
        var map = game.CurrentMap;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (map.Walkable(p) && !map.LineOfSight(from, p)
                    && !game.Monsters.Any(m => m.Alive && m.Pos == p))
                    return p;
            }
        throw new InvalidOperationException("No cell outside line of sight.");
    }

    private static Pos FindWalkableAtDistance(Game game, Pos from, int minimum)
    {
        var map = game.CurrentMap;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                int distance = p.Chebyshev(from);
                if (distance >= minimum && distance <= 8 && map.Walkable(p)
                    && map.LineOfSight(from, p)
                    && !game.Monsters.Any(m => m.Alive && m.Pos == p))
                    return p;
            }
        throw new InvalidOperationException("No sufficiently distant walkable cell.");
    }

    private static Pos[] FacingArc(Pos origin, Pos target)
    {
        (int dx, int dy)[] ring =
            [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)];
        var facing = (Math.Sign(target.X - origin.X), Math.Sign(target.Y - origin.Y));
        int center = Array.IndexOf(ring, facing);
        return
        [
            origin.Plus(ring[(center + 7) % 8].dx, ring[(center + 7) % 8].dy),
            origin.Plus(ring[center].dx, ring[center].dy),
            origin.Plus(ring[(center + 1) % 8].dx, ring[(center + 1) % 8].dy),
        ];
    }

    private static int CountMarkedCells(Frame frame)
    {
        int count = 0;
        for (int y = 0; y < frame.Height; y++)
            for (int x = 0; x < frame.Width; x++)
                if (frame[x, y].Bg == Hue.DarkRed) count++;
        return count;
    }

    private static char KeyToward(Pos from, Pos to) =>
        KeyFor(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        (1, 1) => 'n',
        _ => throw new InvalidOperationException("Not an eight-way direction."),
    };
}
