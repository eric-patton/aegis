using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The loosed line (D-050): the hunting bow, the two-key loose, the shaft that
/// stops at the first body or the first stone, the Ranged skill fed only by
/// shafts that find a mark, and the knack the threshold opens.
/// </summary>
public class RangedTests
{
    [Fact]
    public void TheBow_IsTheSmithsFourthWare_AndTheFirstGraceIron()
    {
        // Fourth of five since D-056 hung the ash spear beside it.
        Assert.Equal("hunting_bow", GearCatalog.SmithStock[3]);

        var bow = GearCatalog.Create("hunting_bow");
        Assert.Equal(GearSlot.Ranged, bow.Slot);
        Assert.Equal(Attr.Grace, bow.ReqAttr);
        Assert.Equal(SkillId.Ranged, bow.Family);

        // Its own slot: taking up the bow displaces no axe.
        var game = new Game(42);
        game.Debug_GrantGear("woodaxe");
        game.Debug_GrantGear("hunting_bow");
        Assert.Equal("woodaxe", game.Player.Weapon?.Id);
        Assert.Equal("hunting_bow", game.Player.Bow?.Id);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("string the hunting bow"));
    }

    [Fact]
    public void TheLoose_IsTwoKeys_AimingIsFree_AndTheShaftCostsWindAndString()
    {
        var game = EnterCamp();
        game.Debug_GrantGear("hunting_bow");

        // Under open sky there is nothing to loose at.
        game.Debug_SetMode(MapMode.Overworld);
        game.ApplyKey('f');
        Assert.False(game.InAim);
        game.Debug_SetMode(MapMode.Site);

        var (mark, from, key) = FindLine(game, minLen: 2);
        game.Debug_SetPlayerPos(from);

        int turnBefore = game.Turn;
        game.ApplyKey('f');
        Assert.True(game.InAim);
        Assert.Equal(turnBefore, game.Turn);

        // A baseline bearer misses the bow's Grace asking: the draw taxes an
        // extra point of wind, exactly the D-015 penalty melee pays.
        int hpBefore = mark.Hp;
        int windBefore = game.Player.Stamina;
        game.ApplyKey(key);
        Assert.False(game.InAim);
        Assert.Equal(turnBefore + 1, game.Turn);
        Assert.True(mark.Hp < hpBefore, "the shaft found no body");
        Assert.Equal(windBefore - 4, game.Player.Stamina);
        Assert.Equal(1, game.Player.Bow!.Wear);
        Assert.Equal(1, game.Player.Skills.Uses(SkillId.Ranged));

        // Grown into the asking, the loose costs a swing's plain three.
        game.Player.Attributes[Attr.Grace] = 7;
        game.Player.Stamina = 10;
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.Equal(7, game.Player.Stamina);
    }

    [Fact]
    public void TheShaft_StopsAtTheFirstBody()
    {
        var game = EnterCamp();
        game.Debug_GrantGear("hunting_bow");

        var (mark, from, key) = FindLine(game, minLen: 3);
        game.Debug_SetPlayerPos(from);

        // Stand another tenant on the same line, nearer: it screens the mark.
        var screen = game.Monsters.First(m => m.Alive && m.SiteId == game.CurrentSite!.Id && m != mark);
        var d = CommandMap.Delta(CommandMap.FromKey(key))!.Value;
        screen.Pos = from.Plus(d.dx, d.dy);

        int markHp = mark.Hp, screenHp = screen.Hp;
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.True(screen.Hp < screenHp, "the nearer body was not struck");
        Assert.Equal(markHp, mark.Hp);
    }

    [Fact]
    public void TheShaft_SplintersOnStone_AndStoneTeachesNothing()
    {
        var game = EnterCamp();
        game.Debug_GrantGear("hunting_bow");

        // Find a floor cell with a wall directly beside it and loose into it.
        var map = game.CurrentSite!.Map;
        (Pos from, char key)? shot = null;
        for (int y = 1; y < map.Height - 1 && shot is null; y++)
            for (int x = 1; x < map.Width - 1 && shot is null; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                if (!map.Walkable(p.Plus(1, 0))) shot = (p, 'l');
                else if (!map.Walkable(p.Plus(-1, 0))) shot = (p, 'h');
            }
        Assert.NotNull(shot);

        game.Debug_SetPlayerPos(shot!.Value.from);
        game.ApplyKey('f');
        game.ApplyKey(shot.Value.key);

        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("splinters against stone"));
        Assert.Equal(1, game.Player.Bow!.Wear);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Ranged));
    }

    [Fact]
    public void TheDraw_RefusesWithoutWind_AndLoweringTheBowCostsNothing()
    {
        var game = EnterCamp();
        game.Debug_GrantGear("hunting_bow");
        var (_, from, key) = FindLine(game, minLen: 2);
        game.Debug_SetPlayerPos(from);

        // Winded: the draw refuses outright. At range, keeping the shaft is free.
        game.Player.Stamina = 2;
        int turnBefore = game.Turn;
        game.ApplyKey('f');
        Assert.False(game.InAim);
        Assert.Equal(turnBefore, game.Turn);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("not the wind to draw"));

        // Rested: arm the shot, then lower the bow. Nothing is spent.
        game.Player.Stamina = 10;
        game.ApplyKey('f');
        Assert.True(game.InAim);
        game.ApplyKey('g');
        Assert.False(game.InAim);
        Assert.Equal(turnBefore, game.Turn);
        Assert.Equal(10, game.Player.Stamina);
        Assert.Equal(0, game.Player.Bow!.Wear);
        _ = key;
    }

    [Fact]
    public void TheShaft_WakesTheSleeper_AcrossThePit()
    {
        var game = new Game(42);
        Cross(game);
        Cross(game);
        game.Debug_GrantGear("hunting_bow");
        game.Debug_SetPlayerPos(game.World.QuarrySite!.OverworldPos);
        game.Apply(Command.Enter);

        var (mark, from, key) = FindLine(game, minLen: 2, kind: MonsterKind.Graven);
        Assert.True(mark.Dormant);
        game.Debug_SetPlayerPos(from);
        game.ApplyKey('f');
        game.ApplyKey(key);

        Assert.False(mark.Dormant);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("hunting the line the shaft flew"));
    }

    [Fact]
    public void TheShaftAtTheKeeper_IsTheOldWay_AndClosesTheMoment()
    {
        var game = new Game(42);
        Cross(game);
        game.Player.Resolution = Resolution.Kept;
        game.Debug_GrantGear("hunting_bow");
        game.Debug_SetPlayerPos(game.World.HollowSite!.OverworldPos);
        game.Apply(Command.Enter);

        var (keeper, from, key) = FindLine(game, minLen: 2, kind: MonsterKind.Severed);
        game.Debug_SetPlayerPos(from);
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("The old way has a reach"));

        // The moment is closed: walking up afterward is a fight, not a choice.
        var beside = Beside(game, keeper.Pos);
        game.Debug_SetPlayerPos(beside);
        StepInto(game, keeper.Pos);
        Assert.False(game.InLayingMenu);
    }

    [Fact]
    public void TheThreshold_OpensItsQuestion_AndTheLightDraw_LightensTheDraw()
    {
        var game = EnterCamp();
        game.Debug_GrantGear("hunting_bow");
        game.Player.Attributes[Attr.Grace] = 7;
        while (game.Player.Skills.Level(SkillId.Ranged) < 2) game.Player.Skills.AddUse(SkillId.Ranged);
        Assert.Equal(SkillId.Ranged, game.PendingKnack?.Skill);

        game.ApplyKey('c');
        game.ApplyKey('2');
        Assert.True(game.Player.HasPerk(PerkId.LightDraw));
        game.ApplyKey(' ');

        var (_, from, key) = FindLine(game, minLen: 2);
        game.Debug_SetPlayerPos(from);
        int windBefore = game.Player.Stamina;
        game.ApplyKey('f');
        game.ApplyKey(key);
        Assert.Equal(windBefore - 2, game.Player.Stamina);
    }

    [Fact]
    public void ALoosedSession_ReplaysIdenticallyFromItsKeys()
    {
        var keys = new List<char>();
        var live = EnterCamp();
        live.KeyApplied += keys.Add;
        live.Debug_GrantGear("hunting_bow");
        var (_, from, key) = FindLine(live, minLen: 2);
        live.Debug_SetPlayerPos(from);
        foreach (char k in (char[])['f', key, '.', 'f', key]) live.ApplyKey(k);

        // Same seed, same debug setup, same keys: the same world results.
        var replayed = EnterCamp();
        replayed.Debug_GrantGear("hunting_bow");
        replayed.Debug_SetPlayerPos(from);
        foreach (char k in keys) replayed.ApplyKey(k);

        Assert.Equal(live.Turn, replayed.Turn);
        Assert.Equal(live.Player.Stamina, replayed.Player.Stamina);
        Assert.Equal(live.Player.Bow!.Wear, replayed.Player.Bow!.Wear);
        Assert.Equal(live.Player.Skills.Uses(SkillId.Ranged), replayed.Player.Skills.Uses(SkillId.Ranged));
        Assert.Equal(live.TakeSnapshot().MonstersAlive, replayed.TakeSnapshot().MonstersAlive);
    }

    // ---- helpers ----

    private static Game EnterCamp()
    {
        var game = new Game(42);
        game.Debug_SetPlayerPos(game.World.CampPos);
        game.Apply(Command.Enter);
        Assert.Equal(MapMode.Site, game.Mode);
        return game;
    }

    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
    }

    /// <summary>
    /// A monster with a clear straight walkable line of at least minLen cells:
    /// returns the mark, the cell to loose from, and the key that looses back
    /// along the line.
    /// </summary>
    private static (Monster Mark, Pos From, char Key) FindLine(Game game, int minLen, MonsterKind? kind = null)
    {
        var map = game.CurrentSite!.Map;
        string siteId = game.CurrentSite.Id;
        var marks = game.Monsters.Where(m => m.Alive && m.SiteId == siteId
            && (kind is null || m.Kind == kind));
        foreach (var mark in marks)
            foreach (var (dx, dy) in Directions.All8)
            {
                for (int len = 1; len <= Game.BowRange; len++)
                {
                    var p = mark.Pos.Plus(dx * len, dy * len);
                    if (!map.Walkable(p) || game.Monsters.Any(m => m.Alive && m.Pos == p)) break;
                    if (len >= minLen)
                        return (mark, p, KeyFor(-dx, -dy));
                }
            }
        Assert.Fail("no clear line to any mark");
        return default;
    }

    private static Pos Beside(Game game, Pos target)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = target.Plus(dx, dy);
            if (map.Walkable(p) && !game.Monsters.Any(m => m.Alive && m.Pos == p)) return p;
        }
        Assert.Fail($"no open cell beside {target}");
        return default;
    }

    private static void StepInto(Game game, Pos target)
    {
        int dx = Math.Sign(target.X - game.Player.Pos.X), dy = Math.Sign(target.Y - game.Player.Pos.Y);
        game.ApplyKey(KeyFor(dx, dy));
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (-1, 0) => 'h', (1, 0) => 'l', (0, -1) => 'k', (0, 1) => 'j',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', _ => 'n',
    };
}
