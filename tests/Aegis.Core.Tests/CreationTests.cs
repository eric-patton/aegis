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

    /// <summary>
    /// Answers the asking start to finish with explicit keys. The extras string
    /// covers D-093's stages between thing and name: default declines the burden,
    /// walks unsworn, and carries no face.
    /// </summary>
    private static void Answer(Game g, char folk, char past, string shapings, char thing, string name = "",
        string extras = "00.")
    {
        g.ApplyKey(folk);
        g.ApplyKey(past);
        foreach (char k in shapings) g.ApplyKey(k);
        g.ApplyKey(thing);
        foreach (char k in extras) g.ApplyKey(k);
        foreach (char k in name) g.ApplyKey(k);
        g.ApplyKey('.');
    }

    /// <summary>Crosses the game's current world through the waygate, clearing the camp first.</summary>
    private static void Cross(Game g)
    {
        g.Debug_ClearCamp();
        g.Debug_SetPlayerPos(g.World.GatePos);
        g.Apply(Command.Enter);
        g.Apply(Command.Enter);
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
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Larceny));
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
        game.ApplyKey('0'); // no burden
        game.ApplyKey('0'); // no vow
        game.ApplyKey('.'); // no face
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
        Cross(game);
        Assert.Equal(2, game.Cycle);
        Assert.False(game.InCreation); // the asking is once, ever
        Assert.Equal(FolkId.Cairnborn, game.Player.Folk);
        Assert.Equal(PastId.Poacher, game.Player.Past);
        Assert.Equal("Kerak", game.Player.Name);
        Assert.Equal("grave_iron", game.Player.Weapon?.Id); // keepsakes are keepsakes
    }

    // ---- Stage 2 (D-093): burdens, vows, the face, and the keepsake's thread. ----

    [Fact]
    public void TheBurden_BuysASecondThing_AndNothingIsTakenTwice()
    {
        var game = Wake();
        game.ApplyKey('1'); // steadfolk
        game.ApplyKey('1'); // soldier
        game.ApplyKey('0'); // unshapen
        game.ApplyKey('4'); // the purse
        game.ApplyKey('1'); // the old wound
        Assert.True(game.PickingSecondThing);
        game.ApplyKey('4'); // the purse again: refused
        Assert.True(game.InCreation);
        Assert.Single(game.Player.Things, ThingId.Purse);
        game.ApplyKey('2'); // fine arms instead
        game.ApplyKey('0'); // no vow
        game.ApplyKey('.'); // no face
        game.ApplyKey('.'); // fated name

        Assert.False(game.InCreation);
        Assert.Equal(BurdenId.OldWound, game.Player.Burden);
        Assert.Equal([ThingId.Purse, ThingId.FineArms], game.Player.Things);
        Assert.Equal(18, game.Player.MaxHp); // the wound keeps two of the brim
    }

    [Fact]
    public void TheHuntedPast_WakesEveryWorldsWrath()
    {
        var game = Wake();
        Answer(game, '1', '1', "0", '4', extras: "22" + "0.");
        Assert.Equal(BurdenId.HuntedPast, game.Player.Burden);
        Assert.Equal(1, game.Wrath); // this world's dens already know the smell

        Cross(game);
        Assert.Equal(2, game.Cycle);
        Assert.Equal(1, game.Wrath); // and so do the next world's
    }

    [Fact]
    public void TheVowOfTheRoad_HearsItsFirstAnswer_AtTheFirstCrossing()
    {
        var game = Wake();
        Answer(game, '1', '1', "0", '4', extras: "0" + "3" + ".");
        Assert.Equal(VowId.Return, game.Player.Vow);

        Cross(game);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("the road clearing its throat"));
    }

    [Fact]
    public void TheVowOfFinding_NeverDangles_WithoutAFace()
    {
        var game = Wake();
        Answer(game, '1', '1', "0", '4', extras: "0" + "2" + ".");
        Assert.Equal(VowId.Finding, game.Player.Vow);
        Assert.True(game.Player.RememberedFace.Length > 0); // drawn from the stream
    }

    [Fact]
    public void TheKeepsake_IsNamed_ThenSung_ByTheKeeperOfSongs()
    {
        var game = Wake();
        Answer(game, '1', '1', "0", '5'); // the unassuming thing
        Cross(game);
        Assert.Equal(2, game.Cycle);

        TalkToSkald(game);
        Assert.True(game.Player.KeepsakeKnown);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("touch-piece"));

        game.ApplyKey(' '); // part ways
        game.ApplyKey('.');
        game.ApplyKey('.');
        game.ApplyKey('.'); // the cooldown's turns pass
        int legendBefore = game.Player.Legend;
        TalkToSkald(game);
        Assert.True(game.Player.KeepsakeSung);
        Assert.Equal(legendBefore + 3, game.Player.Legend);
    }

    [Fact]
    public void TheThingUnpicked_WaitsDownTheChain_AndIsFound()
    {
        var game = Wake();
        Answer(game, '1', '1', "0", '4'); // the purse; the keepsake goes unchosen
        Assert.False(game.Player.Keepsake);

        // The arrival draw may favor other beats in any one world; the thing
        // keeps waiting, and within a few crossings it is always found.
        for (int i = 0; i < 4 && !game.Player.Keepsake; i++) Cross(game);
        Assert.True(game.Player.Keepsake);
    }

    /// <summary>Walks the bearer beside the skald and bumps them to talk.</summary>
    private static void TalkToSkald(Game game)
    {
        var skald = game.World.Skald;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = skald.Pos.Plus(dx, dy);
            if (game.World.Overworld.Walkable(p))
            {
                game.Debug_SetPlayerPos(p);
                game.ApplyKey(DirKey(-dx, -dy));
                Assert.True(game.InTalkMenu, "the bump did not open the skald's talk");
                return;
            }
        }
        throw new InvalidOperationException("no walkable cell beside the skald");
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
