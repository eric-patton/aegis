using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The asking (D-092): character creation as the first wake's own scene. Folk,
/// past, shapings, the precious thing, and the name are answered one journaled
/// key at a time before the first step; the fate door ('0') rolls the whole
/// bearer from the world's stream. Identity is knowledge: death and the
/// waygate never touch it.
/// </summary>
public class CreationTests
{
    private static Game Wake(ulong seed = 42) => new(seed, firstWake: true);

    /// <summary>Answers the asking start to finish with explicit keys.</summary>
    private static void Answer(Game g, char folk, char past, string shapings, char thing, string name = "")
    {
        g.ApplyKey(folk);
        g.ApplyKey(past);
        foreach (char k in shapings) g.ApplyKey(k);
        g.ApplyKey(thing);
        foreach (char k in name) g.ApplyKey(k);
        g.ApplyKey('.');
    }

    [Fact]
    public void TheAsking_HoldsTheWorldStill_UntilItIsAnswered()
    {
        var game = Wake();
        Assert.True(game.InCreation);
        Assert.Equal(0, game.Turn);

        // Movement keys are swallowed by the open question; no turn passes.
        var posBefore = game.Player.Pos;
        game.ApplyKey('l');
        game.ApplyKey('h');
        Assert.Equal(posBefore, game.Player.Pos);
        Assert.Equal(0, game.Turn);
        Assert.True(game.InCreation);

        // The test harness's instant wake never asks.
        Assert.False(new Game(42).InCreation);
    }

    [Fact]
    public void TheFolk_Tilts_AndTheEmberCarriesADeeperWell()
    {
        var game = Wake();
        Answer(game, '2', '6', "0", '4'); // emberwrought wayfarer, unshapen, purse
        Assert.False(game.InCreation);
        Assert.Equal(FolkId.Emberwrought, game.Player.Folk);
        Assert.Equal(6, game.Player.Attributes[Attr.Mind]);
        Assert.Equal(4, game.Player.Attributes[Attr.Vigor]);
        Assert.Equal(4, game.Player.MaxFocus); // 3 at baseline Will, +1 for the kindled spark
    }

    [Fact]
    public void TheCairnborn_ReadAStranger_OnSight()
    {
        var game = Wake();
        Answer(game, '3', '6', "0", '4');
        // Baseline Wits and no witnessed wind-ups: the keeper's blood alone names the kind.
        Assert.Equal(ReadTier.Read, game.Player.ReadOf(MonsterKind.Goblin, game.Cycle));
    }

    [Fact]
    public void ThePast_BanksItsCraft_AndItsSmallExtra()
    {
        var game = Wake();
        Answer(game, '1', '1', "0", '4'); // steadfolk soldier
        Assert.Equal(PastId.Soldier, game.Player.Past);
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Blades));
        Assert.Equal("quilted_jack", game.Player.Armor?.Id);
        Assert.True(game.World.Facts.Exists("past", "soldier"));
        Assert.Equal(35, game.Player.Coin); // 10 from home, 25 in the purse
    }

    [Fact]
    public void TheOathbreaker_IsTwiceSkilled_AndOnceStained()
    {
        var game = Wake();
        Answer(game, '2', '7', "0", '4');
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Blades));
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Hunting));
        Assert.Equal(1, game.Shame);
    }

    [Fact]
    public void TheShaping_PaysForEveryRise_AndSteadfolkGetAThird()
    {
        var game = Wake();
        // Steadfolk: three shapings. Might rises twice to the ceiling of 7,
        // and the third shaping must go elsewhere (the ceiling holds, D-005).
        Answer(game, '1', '1', "17" + "14" + "32", '4');
        Assert.Equal(7, game.Player.Attributes[Attr.Might]);
        Assert.Equal(6, game.Player.Attributes[Attr.Vigor]);
        Assert.Equal(4, game.Player.Attributes[Attr.Presence]);
        Assert.Equal(4, game.Player.Attributes[Attr.Wits]);
        Assert.Equal(4, game.Player.Attributes[Attr.Grace]);
    }

    [Fact]
    public void TheWord_ComesCarried_AndTheFocusWithIt()
    {
        var game = Wake();
        Answer(game, '1', '5', "0", '1'); // scribe's-ward with the known word
        Assert.True(game.Player.HasSpell(SpellId.Spark));
        Assert.Equal(game.Player.MaxFocus, game.Player.Focus);
        Assert.True(game.Player.SpellLineHeard);
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Spellcraft));
    }

    [Fact]
    public void TheKeepsake_IsCarried_AndWritten()
    {
        var game = Wake();
        Answer(game, '4', '2', "0", '5');
        Assert.True(game.Player.Keepsake);
        Assert.True(game.World.Facts.Exists("keepsake", "unassuming-thing"));
    }

    [Fact]
    public void TheName_TakesShape_ErasesBack_AndSeals()
    {
        var game = Wake();
        game.ApplyKey('5'); // wrightkin
        game.ApplyKey('4'); // smith's-hand
        game.ApplyKey('0'); // unshapen
        game.ApplyKey('2'); // fine arms
        foreach (char k in "gorma") game.ApplyKey(k);
        game.ApplyKey('-');
        game.ApplyKey('.');
        Assert.False(game.InCreation);
        Assert.Equal("Gorm", game.Player.Name);
    }

    [Fact]
    public void TheEmptyName_TakesTheFolksOwn()
    {
        var game = Wake();
        Answer(game, '1', '1', "0", '4');
        Assert.True(game.Player.Name.Length > 0);
    }

    [Fact]
    public void TheFateDoor_RollsTheWholeBearer_TheSameWayEveryTime()
    {
        var one = Wake();
        one.ApplyKey('0');
        var two = Wake();
        two.ApplyKey('0');

        Assert.False(one.InCreation);
        Assert.NotNull(one.Player.Folk);
        Assert.NotNull(one.Player.Past);
        Assert.True(one.Player.Name.Length > 0);
        Assert.Equal(one.Player.Folk, two.Player.Folk);
        Assert.Equal(one.Player.Past, two.Player.Past);
        Assert.Equal(one.Player.Name, two.Player.Name);
        Assert.Equal(one.Player.Coin, two.Player.Coin);
        foreach (var attr in Enum.GetValues<Attr>())
            Assert.Equal(one.Player.Attributes[attr], two.Player.Attributes[attr]);
    }

    [Fact]
    public void TheIdentity_CrossesTheWaygateWhole_AndIsNeverAskedAgain()
    {
        var game = Wake();
        Answer(game, '3', '2', "0", '2', "Kerak");
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.False(game.InCreation); // the asking is once, ever
        Assert.Equal(FolkId.Cairnborn, game.Player.Folk);
        Assert.Equal(PastId.Poacher, game.Player.Past);
        Assert.Equal("Kerak", game.Player.Name);
        Assert.Equal("grave_iron", game.Player.Weapon?.Id); // keepsakes are keepsakes
    }
}
