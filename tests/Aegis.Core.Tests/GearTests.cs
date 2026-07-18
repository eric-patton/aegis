using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Gear v1 (D-041): the smith at every stead, five authored items, printed
/// requirements that penalize rather than block, wear as the auto-scaling coin
/// sink, and iron that is banked like attributes: death never takes it and
/// crossings carry it whole.
/// </summary>
public class GearTests
{
    [Fact]
    public void Smith_StandsAtEveryStead_AtEveryTier_Deterministically()
    {
        for (ulong seed = 1; seed <= 25; seed++)
        {
            foreach (int tier in (int[])[1, 3, 6])
            {
                var a = WorldGen.Generate(seed, tier);
                var b = WorldGen.Generate(seed, tier);

                var smith = a.Smith;
                Assert.Equal(NpcKind.Smith, smith.Kind);
                Assert.Equal("smith", smith.Role);
                Assert.True(a.Overworld.Walkable(smith.Pos), $"seed {seed} t{tier}: smith on unwalkable ground");
                Assert.DoesNotContain(a.Npcs, n => n != smith && n.Pos == smith.Pos);
                Assert.Equal((smith.Name, smith.Pos), (b.Smith.Name, b.Smith.Pos));

                // Close to home: the smith belongs to the stead, not the wilds.
                var settlement = a.Facts.OfType("settlement").First();
                var parts = settlement.Object!.Split(',');
                var center = new Pos(int.Parse(parts[0]), int.Parse(parts[1]));
                Assert.True(smith.Pos.Chebyshev(center) <= 3, $"seed {seed} t{tier}: smith far from the stead");
            }
        }
    }

    [Fact]
    public void Smith_SellsThePlainFour_RefusesTheBroke_AndDropsSoldStock()
    {
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.Smith);

        Assert.True(game.InTalkMenu);
        Assert.Equal(2, game.Topics.Count);
        Assert.Equal(4, game.Offers.Count(o => o.Good == TradeGood.Gear));
        Assert.DoesNotContain(game.Offers, o => o.Good == TradeGood.Repair);

        // The riveted shirt prints its requirement before any coin moves; the
        // bow (D-050) prints the first Grace asking in the catalog.
        Assert.Contains(game.Offers, o => o.Arg == "riveted_shirt" && o.Label.Contains("Vigor 7"));
        Assert.Contains(game.Offers, o => o.Arg == "hunting_bow" && o.Label.Contains("Grace 7") && o.Label.Contains("looses +2"));

        // Broke: the axe stays on the wall.
        game.Player.Coin = 5;
        game.ApplyKey(GearKey(game, "woodaxe"));
        Assert.Null(game.Player.Weapon);
        Assert.Equal(5, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("Iron keeps"));

        // Flush: the axe is bought and equipped into the empty slot. The stock
        // entry stays put, marked owned, so menu digits never shift mid-purchase.
        game.Player.Coin = 20;
        char axeKey = GearKey(game, "woodaxe");
        game.ApplyKey(axeKey);
        Assert.Equal("woodaxe", game.Player.Weapon?.Id);
        Assert.Equal(12, game.Player.Coin);
        Assert.True(game.InTalkMenu);
        Assert.Contains(game.Offers, o => o.Arg == "woodaxe" && o.Label.Contains("yours"));
        Assert.Equal(axeKey, GearKey(game, "woodaxe"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("Iron of your own"));

        // Buying the same piece twice is refused without coin moving.
        game.ApplyKey(axeKey);
        Assert.Equal(12, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("its like already"));

        // The jack too; both slots fill without a menu trip.
        game.ApplyKey(GearKey(game, "quilted_jack"));
        Assert.Equal("quilted_jack", game.Player.Armor?.Id);
        Assert.Equal(4, game.Player.Coin);
    }

    [Fact]
    public void WeaponBonus_AddsToTheSwing_AndSwingingWearsTheEdge()
    {
        var (bare, armed) = PairedCampFights(42, weaponId: "woodaxe");

        int bareDamage = LastStrikeDamage(bare);
        int armedDamage = LastStrikeDamage(armed);
        Assert.Equal(bareDamage + 2, armedDamage);
        Assert.Equal(1, armed.Player.Weapon!.Wear);
    }

    [Fact]
    public void WornGear_GivesHalfTheGood_AndSaysSoOnce()
    {
        var axe = GearCatalog.Create("woodaxe");
        var attrs = new AttributeSet();
        Assert.Equal(2, axe.EffectiveBonus(attrs));
        axe.Wear = axe.MaxWear;
        Assert.Equal(1, axe.EffectiveBonus(attrs));

        // Under-requirement halves; worn halves again.
        var maul = GearCatalog.Create("carvers_maul");
        Assert.Equal(2, maul.EffectiveBonus(attrs));
        maul.Wear = maul.MaxWear;
        Assert.Equal(1, maul.EffectiveBonus(attrs));

        // The last point of wear announces itself in the fight log.
        var (_, armed) = PairedCampFights(42, weaponId: "woodaxe", preWear: 39);
        Assert.True(armed.Player.Weapon!.Worn);
        Assert.Contains(armed.Log.Recent(6), e => e.Text.Contains("dull iron"));
    }

    [Fact]
    public void UnderRequirement_CostsAnExtraPointOfWind()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("carvers_maul");
        var (goblin, key) = AdjacentGoblin(game);
        game.ApplyKey(key);
        Assert.Equal(game.Player.MaxStamina - 4, game.Player.Stamina);

        var fair = new Game(42);
        fair.Player.Weapon = GearCatalog.Create("woodaxe");
        (goblin, key) = AdjacentGoblin(fair);
        fair.ApplyKey(key);
        Assert.Equal(fair.Player.MaxStamina - 3, fair.Player.Stamina);
    }

    [Fact]
    public void Armor_ThinsEveryHit_NeverBelowOne_AndTurningBlowsWearsIt()
    {
        // A goblin bite is 1-2; the quilted jack turns every bite down to 1.
        var game = new Game(42);
        game.Player.Armor = GearCatalog.Create("quilted_jack");
        var (goblin, key) = AdjacentGoblin(game);

        int hpBefore = game.Player.Hp;
        int wearBefore = 0;
        for (int i = 0; i < 60 && goblin.Alive && game.Player.Hp > 8; i++)
        {
            game.ApplyKey('.');
            var bites = game.Log.Recent(1).Where(e => e.Text.Contains("bites you for")).ToList();
            if (bites.Count > 0)
            {
                Assert.Contains("bites you for 1.", bites[0].Text);
                // Only a thinned blow wears the jack: a raw 1 passes through whole.
                Assert.True(game.Player.Armor.Wear >= wearBefore);
                wearBefore = game.Player.Armor.Wear;
            }
        }
        Assert.True(hpBefore > game.Player.Hp, "the goblin never landed a bite to thin");
    }

    [Fact]
    public void RepairPricing_ScalesWithWearAndValue_AndTheSmithRestores()
    {
        var axe = GearCatalog.Create("woodaxe");
        axe.Wear = axe.MaxWear;
        Assert.Equal(4, axe.RepairPrice); // half of 8 at full wear

        var maul = GearCatalog.Create("carvers_maul");
        maul.Wear = maul.MaxWear;
        Assert.Equal(13, maul.RepairPrice); // half of 26: richer iron taxes itself harder

        axe.Wear = 1;
        Assert.Equal(1, axe.RepairPrice); // never free while worn at all

        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        game.Player.Weapon.Wear = 20;
        game.Player.Armor = GearCatalog.Create("quilted_jack");
        game.Player.Armor.Wear = 40;
        int expected = game.Player.Weapon.RepairPrice + game.Player.Armor.RepairPrice;
        Assert.Equal(expected, game.RepairPrice);

        NpcTests.BumpNpc(game, game.World.Smith);
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Repair && o.Label.Contains($"{expected} coin"));

        game.Player.Coin = expected;
        game.ApplyKey(OfferKey(game, TradeGood.Repair));
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(0, game.Player.Weapon.Wear);
        Assert.Equal(0, game.Player.Armor.Wear);
        Assert.DoesNotContain(game.Offers, o => o.Good == TradeGood.Repair);
    }

    [Fact]
    public void DeepChests_HandOutTheirIron_OncePerCharacter()
    {
        // The barrow's chest (tier 2, world two of master 42) holds the grave-iron blade.
        var game = new Game(42);
        Cross(game);
        var barrow = game.World.BarrowSite!;
        game.Debug_SetPlayerPos(barrow.OverworldPos);
        game.Apply(Command.Enter);
        game.Debug_SetPlayerPos(barrow.ChestPos);
        game.Apply(Command.Grab);
        Assert.Equal("grave_iron", game.Player.Weapon?.Id);

        // The next world's barrow offers a twin; the bearer leaves it.
        game.Player.Hp = game.Player.MaxHp;
        Cross(game);
        var barrow2 = game.World.BarrowSite!;
        game.Debug_SetPlayerPos(barrow2.OverworldPos);
        game.Apply(Command.Enter);
        game.Debug_SetPlayerPos(barrow2.ChestPos);
        game.Apply(Command.Grab);
        Assert.Contains(game.Log.Recent(8), e => e.Text.Contains("twin of your own"));
        Assert.Single(game.Player.AllGear, g => g.Id == "grave_iron");

        // The quarry's toolcache holds the maul; with the blade in hand it packs.
        game.Debug_SetMode(MapMode.Overworld);
        var quarry = game.World.QuarrySite!;
        game.Debug_ClearSite(SiteKind.Quarry);
        game.Debug_SetPlayerPos(quarry.OverworldPos);
        game.Apply(Command.Enter);
        game.Debug_SetPlayerPos(quarry.ChestPos);
        game.Apply(Command.Grab);
        Assert.Contains(game.Player.Pack, g => g.Id == "carvers_maul");
        Assert.Equal("grave_iron", game.Player.Weapon?.Id);
    }

    [Fact]
    public void GearMenu_SwapsWeapons_AndDisplacedIronPacks()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        game.Player.Pack.Add(GearCatalog.Create("grave_iron"));

        game.ApplyKey('i');
        Assert.True(game.InGearMenu);

        // Item order: weapon, armor, pack. The blade is entry 2.
        game.ApplyKey('2');
        Assert.Equal("grave_iron", game.Player.Weapon?.Id);
        Assert.Contains(game.Player.Pack, g => g.Id == "woodaxe");

        // Selecting the held piece changes nothing.
        game.ApplyKey('1');
        Assert.Equal("grave_iron", game.Player.Weapon?.Id);
        Assert.True(game.InGearMenu);

        game.ApplyKey(' ');
        Assert.False(game.InGearMenu);

        // Empty-handed, the menu declines to open.
        var bare = new Game(42);
        bare.ApplyKey('i');
        Assert.False(bare.InGearMenu);
    }

    [Fact]
    public void Gear_SurvivesDeath_AndCrossesWholeWithItsWear()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        game.Player.Weapon.Wear = 7;
        game.Player.Armor = GearCatalog.Create("quilted_jack");
        game.Player.Pack.Add(GearCatalog.Create("carvers_maul"));
        game.Player.Coin = 30;

        // Death drops coin and essence; iron stays on the body that wakes.
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Equal(0, game.Player.Coin);
        Assert.Equal("woodaxe", game.Player.Weapon?.Id);
        Assert.Equal(7, game.Player.Weapon!.Wear);
        Assert.Equal(3, game.Player.AllGear.Count());

        Cross(game);
        Assert.Equal(2, game.Cycle);
        Assert.Equal("woodaxe", game.Player.Weapon?.Id);
        Assert.Equal(7, game.Player.Weapon!.Wear);
        Assert.Equal("quilted_jack", game.Player.Armor?.Id);
        Assert.Contains(game.Player.Pack, g => g.Id == "carvers_maul");
    }

    [Fact]
    public void GearSession_ReplaysIdenticallyFromJournal()
    {
        const ulong seed = 42;
        var live = new Game(seed);
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);
        live.Player.Coin = 20;

        var target = live.World.Smith.Pos;
        for (int guard = 0; guard < 400 && !live.InTalkMenu; guard++)
        {
            char? key = UnbinderTests.StepTo(live, target);
            if (key is null) break;
            live.ApplyKey(key.Value);
        }
        Assert.True(live.InTalkMenu, "bot never reached the smith");

        live.ApplyKey(GearKey(live, "woodaxe"));
        live.ApplyKey(GearKey(live, "quilted_jack"));
        live.ApplyKey(' ');
        live.ApplyKey('i');
        live.ApplyKey(' ');
        Assert.Equal("woodaxe", live.Player.Weapon?.Id);

        var replayed = new Game(seed);
        replayed.Player.Coin = 20;
        foreach (char key in journal.ToString()) replayed.ApplyKey(key);

        Assert.Equal(live.Player.Coin, replayed.Player.Coin);
        Assert.Equal(live.Player.Weapon?.Id, replayed.Player.Weapon?.Id);
        Assert.Equal(live.Player.Armor?.Id, replayed.Player.Armor?.Id);
        Assert.Equal(live.Turn, replayed.Turn);
        Assert.Equal(
            live.Log.Recent(15).Select(e => e.Text),
            replayed.Log.Recent(15).Select(e => e.Text));
    }

    [Fact]
    public void Snapshot_And_Sidebar_CarryTheIron()
    {
        var game = new Game(42);
        game.Player.Weapon = GearCatalog.Create("woodaxe");
        game.Player.Weapon.Wear = 5;
        game.Player.Pack.Add(GearCatalog.Create("carvers_maul"));

        var snap = game.TakeSnapshot();
        Assert.Equal("woodaxe", snap.WeaponId);
        Assert.Equal(5, snap.WeaponWear);
        Assert.Equal("", snap.ArmorId);
        Assert.Equal("carvers_maul", snap.PackGear);
        Assert.Equal(game.World.Smith.Pos.X, snap.SmithX);
        Assert.True(snap.RepairPrice > 0);

        var lines = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("Wpn woodsman's axe", lines);
        Assert.Contains("i gear", lines);

        game.ApplyKey('i');
        var menu = string.Join("\n", Presenter.Render(game).ToTextLines());
        Assert.Contains("Your gear", menu);
        Assert.Contains("woodsman's axe", menu);
        Assert.Contains("Might 8!", menu); // the maul prints what it asks
    }

    /// <summary>Clears the camp and steps through the waygate, surfacing first if needed.</summary>
    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
    }

    /// <summary>Two same-seed games, one bare-handed and one armed, walked into the same first strike.</summary>
    private static (Game Bare, Game Armed) PairedCampFights(ulong seed, string weaponId, int preWear = 0)
    {
        var bare = new Game(seed);
        var armed = new Game(seed);
        armed.Player.Weapon = GearCatalog.Create(weaponId);
        armed.Player.Weapon.Wear = preWear;

        var (_, key) = AdjacentGoblin(bare);
        bare.ApplyKey(key);
        (_, key) = AdjacentGoblin(armed);
        armed.ApplyKey(key);
        return (bare, armed);
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

    private static char GearKey(Game game, string id)
    {
        for (int i = 0; i < game.Offers.Count; i++)
            if (game.Offers[i].Good == TradeGood.Gear && game.Offers[i].Arg == id)
                return (char)('1' + game.Topics.Count + i);
        throw new InvalidOperationException($"no {id} on the smith's wall");
    }

    private static char OfferKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.Offers.Count; i++)
            if (game.Offers[i].Good == good)
                return (char)('1' + game.Topics.Count + i);
        throw new InvalidOperationException($"no {good} offer in this menu");
    }
}
