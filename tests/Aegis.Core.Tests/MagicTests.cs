using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The remnant craft (D-091): magic v1. Words wait on graven stones set deep
/// in the old fabric, one per fighting site; reading one takes it into the
/// bearer for good (knowledge, like lessons: crossing and death never touch
/// it). Saying a word spends Focus (Will's pool), Mind drives its weight, and
/// the levin is the caster's own wind-up, held one turn on marked ground the
/// way the heave is. Spellcraft counts only workings that did work.
/// </summary>
public class MagicTests
{
    [Fact]
    public void TheStone_StandsInTheDeep_AndGivesItsWordOnce()
    {
        var game = new Game(42);
        var camp = game.World.CampSite;
        Assert.NotNull(camp.StonePos);

        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(camp.StonePos!.Value);
        game.ApplyKey('g');

        Assert.True(game.Player.HasSpell(SpellId.Spark)); // the camp's leaning gives the spark first
        Assert.True(camp.StoneRead);
        Assert.Equal(game.Player.MaxFocus, game.Player.Focus); // the pool unveils full
        Assert.True(game.Player.SpellLineHeard);
        var log = game.Log.Recent(8).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("the spark is yours"));
        Assert.Contains(log, t => t.Contains("a focus, waiting to be spent"));

        // A read stone is only company; the word is not given twice.
        game.ApplyKey('g');
        Assert.Single(game.Player.Spells);
    }

    [Fact]
    public void TheStone_GivesTheFirstWordOfItsLeaning_TheBearerLacks()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Spark);
        var camp = game.World.CampSite;
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(camp.StonePos!.Value);

        game.ApplyKey('g');
        // The camp leans spark, then ward: with the spark carried, the ward is given.
        Assert.True(game.Player.HasSpell(SpellId.Ward));
        Assert.Equal(2, game.Player.Spells.Count);
    }

    [Fact]
    public void TheSpark_FliesItsLine_AndTeachesOnlyWhereItBurns()
    {
        var (game, goblin, dir) = ArrangeShot(42, SpellId.Spark);
        int hpBefore = goblin.Hp;
        int turnBefore = game.Turn;

        game.ApplyKey('z');
        Assert.True(game.InCastMenu);
        Assert.Equal(turnBefore, game.Turn); // the choosing costs nothing
        game.ApplyKey('1');
        Assert.True(game.InCastLine);
        game.ApplyKey(dir);

        Assert.True(goblin.Hp < hpBefore);
        Assert.Equal(game.Player.MaxFocus - 1, game.Player.Focus);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Spellcraft));
        Assert.Equal(turnBefore + 1, game.Turn); // the saying costs the turn
    }

    [Fact]
    public void TheLevin_IsHeld_ThenFallsOnTheMarkedGround()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Levin);
        game.Debug_SetMode(MapMode.Site);
        var entry = game.World.CampEntryPos;
        game.Debug_SetPlayerPos(entry);
        char dir = OpenLineFrom(game, entry);

        game.ApplyKey('z');
        game.ApplyKey('1');
        game.ApplyKey(dir);
        Assert.NotNull(game.Player.LevinTarget);
        Assert.Equal(game.Player.MaxFocus - 2, game.Player.Focus); // spent at the raising

        // The next act says it, on the mark, hit or miss (here: empty stone).
        game.ApplyKey('.');
        Assert.Null(game.Player.LevinTarget);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("The levin comes down"));
    }

    [Fact]
    public void TheWard_ThickensTheAir_AndRunsOutWithTheTurns()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Ward);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);

        game.ApplyKey('z');
        game.ApplyKey('1');
        // The cast turn already ticks once: six held breaths, five still to run.
        Assert.Equal(Game.WardHeldTurns - 1, game.Player.WardTurns);

        for (int i = 0; i < Game.WardHeldTurns - 1; i++) game.ApplyKey('.');
        Assert.Equal(0, game.Player.WardTurns);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("goes out of the air"));
    }

    [Fact]
    public void TheVeilsight_NamesTheFloor_AndSharpensTheRead()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Veilsight);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        Assert.Equal(ReadTier.Blur, game.Player.ReadOf(MonsterKind.Goblin, game.Cycle));

        game.ApplyKey('z');
        game.ApplyKey('1');

        Assert.True(game.Player.ReadOf(MonsterKind.Goblin, game.Cycle) >= ReadTier.Read);
        Assert.True(game.World.CampSite.Unveiled);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Spellcraft));
        var log = game.Log.Recent(6).Select(e => e.Text).ToList();
        Assert.Contains(log, t => t.Contains("The floor gives up its living"));
        Assert.Contains(log, t => t.Contains("know their blows before they are thrown"));
    }

    [Fact]
    public void TheFocus_GathersOnTheRoad_AndFillsAtTheRest()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Spark);
        game.Player.Focus = 0;

        int start = game.Turn;
        int gathered = 0;
        for (int i = 0; i < 2 * Game.FocusRegenTurns; i++)
        {
            game.ApplyKey('.');
            if (game.Turn % Game.FocusRegenTurns == 0) gathered++;
        }
        Assert.Equal(start + 2 * Game.FocusRegenTurns, game.Turn);
        Assert.Equal(gathered, game.Player.Focus); // a point per tick crossed, nothing more

        game.Debug_SetPlayerPos(game.World.ShrinePos);
        game.ApplyKey('r');
        Assert.Equal(game.Player.MaxFocus, game.Player.Focus);
        game.ApplyKey(' ');
    }

    [Fact]
    public void TheWords_CrossTheWaygateWhole_AndTheStonesStandAgain()
    {
        var game = new Game(42);
        game.Debug_LearnSpell(SpellId.Spark);
        game.Player.Focus = 1;
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);

        Assert.True(game.Player.HasSpell(SpellId.Spark)); // knowledge crosses whole
        Assert.Equal(game.Player.MaxFocus, game.Player.Focus); // the pool arrives at brim
        Assert.False(game.World.CampSite.StoneRead); // the new world's stones stand unread
        Assert.NotNull(game.World.CampSite.StonePos);
        Assert.NotNull(game.World.BarrowSite!.StonePos); // tier 2's barrow carries one too
    }

    [Fact]
    public void TheOpenSky_RefusesTheWords_AndTheEmptyHeadIsToldWhereTheyWait()
    {
        var game = new Game(42);
        int turn = game.Turn;
        game.ApplyKey('z');
        Assert.False(game.InCastMenu);
        Assert.Equal(turn, game.Turn);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("wait graven in the deep places"));

        game.Debug_LearnSpell(SpellId.Spark);
        game.ApplyKey('z');
        Assert.True(game.InCastMenu);
        game.ApplyKey('1');
        Assert.False(game.InCastLine); // the overworld answers no word
        Assert.Equal(game.Player.MaxFocus, game.Player.Focus);
        Assert.Equal(turn, game.Turn);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("under this open sky"));
    }

    /// <summary>Puts the bearer on a walkable cell beside a camp goblin, word in head, and hands back the line to it.</summary>
    private static (Game Game, Monster Goblin, char Dir) ArrangeShot(ulong seed, SpellId spell)
    {
        var game = new Game(seed);
        game.Debug_LearnSpell(spell);
        game.Debug_SetMode(MapMode.Site);
        var camp = game.World.Camp;
        foreach (var goblin in game.Monsters.Where(m => m.Alive && m.SiteId == "goblin-camp"))
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var p = goblin.Pos.Plus(dx, dy);
                if (camp.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p))
                {
                    game.Debug_SetPlayerPos(p);
                    return (game, goblin, DirKey(-dx, -dy));
                }
            }
        throw new InvalidOperationException("No goblin with an open flank; pick another seed.");
    }

    /// <summary>First cardinal line from a cell with at least one open step, as its key.</summary>
    private static char OpenLineFrom(Game game, Pos from)
    {
        foreach (var (dx, dy) in Directions.Cardinal)
            if (game.CurrentMap.Walkable(from.Plus(dx, dy)))
                return DirKey(dx, dy);
        throw new InvalidOperationException("No open line; pick another seed.");
    }

    private static char DirKey(int dx, int dy) => (dx, dy) switch
    {
        (-1, 0) => 'h',
        (1, 0) => 'l',
        (0, -1) => 'k',
        (0, 1) => 'j',
        (-1, -1) => 'y',
        (1, -1) => 'u',
        (-1, 1) => 'b',
        _ => 'n',
    };
}
