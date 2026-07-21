using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Skills v1 (D-042, the first slice of D-014/D-016): four use-grown skills
/// fed only by paid-for actions, levels derived from counted uses, a flat
/// bonus per two levels, and the sheet ('c') that shows both ledgers. Skills
/// are banked like attributes: death never takes them, crossings carry them.
/// </summary>
public class SkillsTests
{
    [Fact]
    public void Curve_AsksFourMoreUsesEachLevel_AndBonusIsHalfTheLevel()
    {
        Assert.Equal(8, SkillSet.UsesForLevel(1));
        Assert.Equal(20, SkillSet.UsesForLevel(2));
        Assert.Equal(36, SkillSet.UsesForLevel(3));
        Assert.Equal(56, SkillSet.UsesForLevel(4));
        Assert.Equal(80, SkillSet.UsesForLevel(5));

        var skills = new SkillSet();
        for (int i = 0; i < 7; i++) skills.AddUse(SkillId.Hafted);
        Assert.Equal(0, skills.Level(SkillId.Hafted));
        skills.AddUse(SkillId.Hafted);
        Assert.Equal(1, skills.Level(SkillId.Hafted));
        Assert.Equal(0, skills.Bonus(SkillId.Hafted));

        while (skills.Uses(SkillId.Hafted) < 20) skills.AddUse(SkillId.Hafted);
        Assert.Equal(2, skills.Level(SkillId.Hafted));
        Assert.Equal(1, skills.Bonus(SkillId.Hafted));

        while (skills.Uses(SkillId.Hafted) < 56) skills.AddUse(SkillId.Hafted);
        Assert.Equal(4, skills.Level(SkillId.Hafted));
        Assert.Equal(2, skills.Bonus(SkillId.Hafted));

        // Tracks are independent: the maul taught the arm nothing about edges.
        Assert.Equal(0, skills.Level(SkillId.Blades));
    }

    [Fact]
    public void Swings_TrainTheFamilySwung_AndFeebleFlailingTeachesNothing()
    {
        // Bare hands are Brawling.
        var bare = new Game(42);
        var (_, key) = AdjacentGoblin(bare);
        bare.ApplyKey(key);
        Assert.Equal(1, bare.Player.Skills.Uses(SkillId.Brawling));
        Assert.Equal(0, bare.Player.Skills.Uses(SkillId.Hafted));

        // The axe is Hafted; the grave-iron is Blades.
        var armed = new Game(42);
        armed.Player.Weapon = GearCatalog.Create("woodaxe");
        (_, key) = AdjacentGoblin(armed);
        armed.ApplyKey(key);
        Assert.Equal(1, armed.Player.Skills.Uses(SkillId.Hafted));
        Assert.Equal(0, armed.Player.Skills.Uses(SkillId.Brawling));
        Assert.Equal(SkillId.Blades, GearCatalog.Create("grave_iron").Family);

        // Winded, the swing is feeble, costs nothing, and so counts for nothing
        // (D-014: only paid-for uses feed growth).
        var winded = new Game(42);
        (_, key) = AdjacentGoblin(winded);
        winded.Player.Stamina = 0;
        winded.ApplyKey(key);
        Assert.Equal(0, winded.Player.Skills.Uses(SkillId.Brawling));
        Assert.Contains(winded.Log.Recent(3), e => e.Text.Contains("winded"));
    }

    [Fact]
    public void SkillBonus_AddsToTheFullSwing()
    {
        // Same seed, same axe, same goblin; only the arm differs.
        var green = new Game(42);
        green.Player.Weapon = GearCatalog.Create("woodaxe");
        var (_, key) = AdjacentGoblin(green);
        green.ApplyKey(key);

        var seasoned = new Game(42);
        seasoned.Player.Weapon = GearCatalog.Create("woodaxe");
        for (int i = 0; i < 20; i++) seasoned.Player.Skills.AddUse(SkillId.Hafted);
        (_, key) = AdjacentGoblin(seasoned);
        seasoned.ApplyKey(key);

        Assert.Equal(LastStrikeDamage(green) + 1, LastStrikeDamage(seasoned));
    }

    [Fact]
    public void LevelRise_SpeaksInTheLog_AndTheAegisWitnessesOnlyTheFirst()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        for (int i = 0; i < 7; i++) game.Player.Skills.AddUse(SkillId.Hafted);

        var (_, key) = AdjacentGoblin(game);
        game.ApplyKey(key);
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Hafted));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("Hafted rises to 1"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("ledger of its own"));

        // The next rise passes without the Aegis's aside.
        while (game.Player.Skills.Uses(SkillId.Hafted) < 19) game.Player.Skills.AddUse(SkillId.Hafted);
        game.Player.Stamina = game.Player.MaxStamina;
        (_, key) = AdjacentGoblin(game);
        game.ApplyKey(key);
        Assert.Equal(2, game.Player.Skills.Level(SkillId.Hafted));
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("Hafted rises to 2"));
        Assert.DoesNotContain(game.Log.Recent(3), e => e.Text.Contains("ledger of its own"));
    }

    [Fact]
    public void Warding_GrowsOnlyWhenWornIronTurnsABlow()
    {
        // No armor: bites land whole and teach nothing.
        var bare = new Game(42);
        var (goblin, _) = AdjacentGoblin(bare);
        for (int i = 0; i < 30 && goblin.Alive && bare.Player.Hp > 8; i++) bare.ApplyKey('.');
        Assert.True(bare.Player.Hp < bare.Player.MaxHp, "the goblin never landed a bite");
        Assert.Equal(0, bare.Player.Skills.Uses(SkillId.Warding));

        // The jack turns bites down to 1; each turned blow is a counted use.
        var jacked = new Game(42);
        jacked.Player.Armor = GearCatalog.Create("quilted_jack");
        (goblin, _) = AdjacentGoblin(jacked);
        for (int i = 0; i < 30 && goblin.Alive && jacked.Player.Hp > 8; i++) jacked.ApplyKey('.');
        Assert.True(jacked.Player.Skills.Uses(SkillId.Warding) > 0, "no bite was ever turned");
        Assert.Equal(jacked.Player.Skills.Uses(SkillId.Warding), jacked.Player.Armor!.Wear);
    }

    [Fact]
    public void WardingBonus_ThickensTheWornIron()
    {
        // Same seed, same jack, both bearers standing into the same blows; the
        // warded arm gives up less blood wherever the min-1 floor allows it.
        var plain = new Game(42);
        plain.Player.Armor = GearCatalog.Create("quilted_jack");
        AdjacentGoblin(plain);

        var warded = new Game(42);
        warded.Player.Armor = GearCatalog.Create("quilted_jack");
        for (int i = 0; i < 20; i++) warded.Player.Skills.AddUse(SkillId.Warding);
        AdjacentGoblin(warded);

        for (int i = 0; i < 40 && plain.Player.Hp > 8; i++)
        {
            plain.ApplyKey('.');
            warded.ApplyKey('.');
        }

        // Bites clamp to 1 for both; only the heavy telegraphed blows separate
        // them, and at least one lands inside 40 turns of standing still.
        Assert.True(warded.Player.Hp > plain.Player.Hp,
            $"warding never turned an extra point (plain {plain.Player.Hp}, warded {warded.Player.Hp})");
    }

    [Fact]
    public void Skills_SurviveDeath_AndCrossWhole()
    {
        var game = new Game(42);
        for (int i = 0; i < 20; i++) game.Player.Skills.AddUse(SkillId.Hafted);
        for (int i = 0; i < 9; i++) game.Player.Skills.AddUse(SkillId.Warding);
        game.Player.Coin = 30;

        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(20, game.Player.Skills.Uses(SkillId.Hafted));
        Assert.Equal(9, game.Player.Skills.Uses(SkillId.Warding));

        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.Equal(2, game.Player.Skills.Level(SkillId.Hafted));
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Warding));
    }

    [Fact]
    public void Sheet_OpensOnC_TakesNoTurn_ShowsBothLedgers_AndAnyKeyCloses()
    {
        var game = new Game(42);
        for (int i = 0; i < 12; i++) game.Player.Skills.AddUse(SkillId.Hafted);

        int turn = game.Turn;
        game.ApplyKey('c');
        Assert.True(game.InSheetMenu);
        Assert.Equal(turn, game.Turn);

        var sheet = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("The bearer", sheet);
        Assert.Contains("Might", sheet);
        Assert.Contains("Presence", sheet);
        Assert.Contains("Hafted", sheet);
        Assert.Contains("12/20", sheet); // progress toward the next line
        Assert.Contains("0/8", sheet);   // untrained tracks show their first ask

        var snap = game.TakeSnapshot();
        Assert.True(snap.InSheetMenu);
        Assert.Equal("blades:0:0,hafted:1:12,brawling:0:0,warding:0:0,ranged:0:0,hunting:0:0,cooking:0:0,survival:0:0,spellcraft:0:0,sleight:0:0,smithing:0:0,commerce:0:0", snap.Skills);

        game.ApplyKey(' ');
        Assert.False(game.InSheetMenu);
        Assert.Equal(turn, game.Turn);

        // The sidebar teaches the key.
        var lines = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("c you", lines);
    }

    [Fact]
    public void SkillsSession_ReplaysIdenticallyFromJournal()
    {
        const ulong seed = 42;
        var live = new Game(seed);
        live.Player.Weapon = GearCatalog.Create("woodaxe");
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);

        var (_, key) = AdjacentGoblin(live);
        for (int i = 0; i < 12; i++) live.ApplyKey(key);
        live.ApplyKey('c');
        live.ApplyKey(' ');
        Assert.True(live.Player.Skills.Uses(SkillId.Hafted) > 0);

        var replayed = new Game(seed);
        replayed.Player.Weapon = GearCatalog.Create("woodaxe");
        AdjacentGoblin(replayed);
        foreach (char k in journal.ToString()) replayed.ApplyKey(k);

        Assert.Equal(live.Player.Skills.Uses(SkillId.Hafted), replayed.Player.Skills.Uses(SkillId.Hafted));
        Assert.Equal(live.Player.Skills.Uses(SkillId.Warding), replayed.Player.Skills.Uses(SkillId.Warding));
        Assert.Equal(live.Player.Hp, replayed.Player.Hp);
        Assert.Equal(live.Turn, replayed.Turn);
        Assert.Equal(
            live.Log.Recent(15).Select(e => e.Text),
            replayed.Log.Recent(15).Select(e => e.Text));
    }

    /// <summary>Teleports into the camp beside a goblin; returns it and the key that strikes it.</summary>
    private static (Monster Goblin, char Key) AdjacentGoblin(Game game)
    {
        game.Debug_SetMode(MapMode.Site);
        var goblin = game.Monsters.First(m => m.Alive && m.SiteId == "goblin-camp");
        var map = game.World.Camp;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = goblin.Pos.Plus(dx, dy);
            if (map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p))
            {
                game.Debug_SetPlayerPos(p);
                return (goblin, KeyFor(goblin.Pos.X - p.X, goblin.Pos.Y - p.Y));
            }
        }
        throw new InvalidOperationException("goblin has no open neighbor");
    }

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
        _ => throw new InvalidOperationException("not adjacent"),
    };

    private static int LastStrikeDamage(Game game)
    {
        string text = game.Log.Recent(5).Last(e => e.Text.Contains("You strike the")).Text;
        int start = text.LastIndexOf("for ") + 4;
        int end = text.IndexOf('.', start);
        return int.Parse(text[start..end]);
    }
}
