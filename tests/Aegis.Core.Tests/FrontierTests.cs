using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The high fells (D-146, plan 2026-07 B4): the world's third overworld and
/// third named country, the frontier off the road's north shoulder. The tests
/// hold the naming, the track crossing both ways, the new ground, the pack
/// (its company rule, its pounce, and its yield down the hunt's own ladder),
/// the frontier's harsher weather, the high herbs, and the seams: areas never
/// bleeding, and the Aegis still catching a faller home to the valley.
/// </summary>
public class FrontierTests
{
    /// <summary>Walks the real mouths: valley to road, road up the track.</summary>
    internal static void ClimbFells(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        Assert.Equal(Area.Road, game.Area);
        game.Debug_SetPlayerPos(game.World.FellMouthPos);
        game.ApplyKey('>');
        Assert.Equal(Area.Fells, game.Area);
    }

    [Fact]
    public void TheWorld_GrowsItsThirdCountry_Deterministically()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var a = WorldGen.Generate(seed);
            var b = WorldGen.Generate(seed);

            Assert.Equal(3, a.Regions.Count);
            Assert.Equal(a.FellRegion.Name, b.FellRegion.Name);
            Assert.Equal(a.FellRegion.Name, a.Facts.Find("region", "fells")!.Object);

            // The third name keeps apart from every other named thing.
            Assert.NotEqual(a.FellRegion.Name, a.ValleyRegion.Name);
            Assert.NotEqual(a.FellRegion.Name, a.RoadRegion.Name);
            Assert.NotEqual(a.FellRegion.Name, a.Name);
            Assert.NotEqual(a.FellRegion.Name, a.SettlementName);
            Assert.NotEqual(a.FellRegion.Name, a.TownName);

            Assert.Equal(Terrain.FellMouth, a.Road[a.FellMouthPos]);
            Assert.Equal(Terrain.FellMouth, a.Fells[a.FellHomePos]);
            Assert.Equal(a.Fells.ContentHash(), b.Fells.ContentHash());

            var combe = a.FellWildsSite;
            Assert.Equal(Area.Fells, combe.Area);
            Assert.NotEmpty(combe.Spawns);
            Assert.All(combe.Spawns, s => Assert.Equal(MonsterKind.Wolf, s.Kind));
            Assert.Equal(Terrain.WildsEntrance, a.Fells[combe.OverworldPos]);
            // The valley's and the road's wilds accessors never read the combe.
            Assert.NotEqual("fell-wilds", a.RoadWildsSite.Id);

            Assert.NotEmpty(a.FellHerbs);
            Assert.All(a.FellHerbs, h => Assert.Equal(Terrain.Heath, a.Fells[h]));
            Assert.True(a.Facts.Exists("site", "fells"));
        }
    }

    [Fact]
    public void TheTrack_IsTakenByName_BothWays()
    {
        var game = new Game(42);
        ClimbFells(game);
        Assert.Equal(game.World.FellHomePos, game.Player.Pos);
        Assert.Equal("fells", game.CurrentMap.Id);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains($"onto the {game.World.FellRegion.Name}"));

        game.ApplyKey('>');
        Assert.Equal(Area.Road, game.Area);
        Assert.Equal(game.World.FellMouthPos, game.Player.Pos);
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains($"down off the {game.World.FellRegion.Name}"));
    }

    [Fact]
    public void TheCombe_IsEntered_AndClimbedOutOf_OnTheFells()
    {
        var game = new Game(42);
        ClimbFells(game);
        var combe = game.World.FellWildsSite;
        game.Debug_SetPlayerPos(combe.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
        Assert.Equal("fell-wilds", game.CurrentSite!.Id);

        game.Debug_SetPlayerPos(combe.EntryPos);
        game.ApplyKey('<');
        Assert.Equal(Area.Fells, game.Area); // climbing out comes back up on the fells
    }

    [Fact]
    public void TheWolf_IsGame_DownTheHuntsOwnLadder()
    {
        var game = new Game(42);
        ClimbFells(game);
        game.Debug_SetPlayerPos(game.World.FellWildsSite.OverworldPos);
        game.ApplyKey('>');

        var wolf = game.Monsters.First(m => m.Alive && m.Kind == MonsterKind.Wolf);
        wolf.Hp = 1;
        game.Debug_SetPlayerPos(wolf.Pos.Plus(-1, 0));
        int coin = game.Player.Coin, essence = game.Player.Essence;
        int hides = game.Player.Hide, meat = game.Player.RawMeat;
        int hunting = game.Player.Skills.Uses(SkillId.Hunting);

        game.ApplyKey('l'); // the bump ends it

        Assert.False(wolf.Alive);
        Assert.True(game.Player.Hide > hides, "the pelt was left on the ground");
        Assert.Equal(meat + 1, game.Player.RawMeat);
        Assert.Equal(coin, game.Player.Coin);       // no purse on a beast
        Assert.Equal(essence, game.Player.Essence); // and no essence in game
        Assert.True(game.Player.Skills.Uses(SkillId.Hunting) > hunting);
    }

    [Fact]
    public void ThePack_HoldsAlone_AndClosesInCompany()
    {
        var game = new Game(42);
        ClimbFells(game);
        game.Debug_SetPlayerPos(game.World.FellWildsSite.OverworldPos);
        game.ApplyKey('>');

        var wolves = game.Monsters.Where(m => m.Alive && m.Kind == MonsterKind.Wolf).ToList();
        Assert.True(wolves.Count >= 2);
        var near = wolves[0];
        // One wolf at the ring's edge, the rest of the pack held far off.
        var p = game.Player.Pos;
        near.Pos = p.Plus(3, 0);
        foreach (var far in wolves.Skip(1)) far.Pos = new Pos(1, 1);

        game.Apply(Command.Wait);
        Assert.Equal(p.Plus(3, 0), near.Pos); // alone at the edge, it holds and circles

        // A packmate near the bearer, and the held wolf commits.
        wolves[1].Pos = p.Plus(-2, 0);
        game.Apply(Command.Wait);
        Assert.True(near.Pos.Chebyshev(p) < 3, "the wolf held off with the pack in company");
    }

    [Fact]
    public void ThePounce_LandsOnTheKeptCell_AndMissesTheLeftOne()
    {
        var game = new Game(42);
        ClimbFells(game);
        game.Debug_SetPlayerPos(game.World.FellWildsSite.OverworldPos);
        game.ApplyKey('>');

        var wolves = game.Monsters.Where(m => m.Alive && m.Kind == MonsterKind.Wolf).ToList();
        foreach (var w in wolves) w.Pos = new Pos(1, 1); // parked out of the fight
        var pouncer = wolves[0];
        pouncer.Pos = game.Player.Pos.Plus(2, 0);
        pouncer.Intent = new Intent { Kind = IntentKind.Pounce, TargetCell = game.Player.Pos };

        int hp = game.Player.Hp;
        game.Apply(Command.Wait); // the kept cell takes the spring
        Assert.True(game.Player.Hp < hp, "the pounce landed nothing");
        Assert.True(game.Player.Reads.GetValueOrDefault(MonsterKind.Wolf) > 0); // the tell banked (D-059)

        // The same spring against feet that left the marked ground.
        pouncer.Pos = game.Player.Pos.Plus(2, 0);
        pouncer.Intent = new Intent { Kind = IntentKind.Pounce, TargetCell = game.Player.Pos };
        hp = game.Player.Hp;
        ForagingStep(game);
        Assert.Equal(hp, game.Player.Hp); // dodged by feet, D-004's sacred rule
    }

    [Fact]
    public void TheFellsCold_QuartersTheCamp_AndRefusesTheSupperless()
    {
        var game = new Game(42);
        ClimbFells(game);
        game.Debug_SetSky(RoadSky.Cold);
        var spot = OpenHeath(game);
        game.Debug_SetPlayerPos(spot);
        game.Debug_HurtPlayer(10);
        game.Player.Rations = 1;
        int maxHp = game.Player.EffectiveMaxHp;

        game.ApplyKey('m');
        // Base 6, halved for foul weather, halved again by the fells' cold: 1.
        Assert.Equal(maxHp - 9, game.Player.Hp);

        // And with nothing to burn, the cold camp is refused outright.
        game.Debug_HurtPlayer(5);
        game.Player.Rations = 0;
        game.Player.RawMeat = 0;
        int turn = game.Turn;
        game.ApplyKey('m');
        Assert.Equal(turn, game.Turn); // no night passed: the walker kept its feet
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("spring thaw"));
    }

    [Fact]
    public void TheHighHerbs_ArePicked_FromTheHeath()
    {
        var game = new Game(42);
        ClimbFells(game);
        game.Player.Herb = 0;
        var spot = game.World.FellHerbs[0];
        int spots = game.World.FellHerbs.Count;
        StepOnto(game, spot);
        Assert.True(game.Player.Herb > 0);
        Assert.Equal(spots - 1, game.World.FellHerbs.Count);
    }

    [Fact]
    public void TheFall_SendsThePackHomeWithItsKill()
    {
        var game = new Game(42);
        ClimbFells(game);
        var combe = game.World.FellWildsSite;
        game.Debug_SetPlayerPos(combe.OverworldPos);
        game.ApplyKey('>');

        // The pack at the door, the way a lost fight leaves it.
        var wolves = game.Monsters.Where(m => m.Alive && m.Kind == MonsterKind.Wolf).ToList();
        foreach (var w in wolves) w.Pos = game.Player.Pos;
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        // The kill is drawn off to the pack's own ground (D-146): the walk
        // back in is the walk it was the first time, never a door ambush.
        var dens = combe.Spawns.Select(s => s.Pos).ToHashSet();
        Assert.All(wolves, w => Assert.Contains(w.Pos, dens));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("draw off to their own ground"));
    }

    [Fact]
    public void TheFall_ComesHomeToTheValley()
    {
        var game = new Game(42);
        ClimbFells(game);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Equal(Area.Valley, game.Area); // the Aegis catches the bearer where it anchors
        Assert.Equal(game.World.ShrinePos, game.Player.Pos);
    }

    [Fact]
    public void TheWaykeeper_SpeaksTheFells_ByName()
    {
        var game = new Game(42);
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        var keeper = game.World.Waykeeper;
        var beside = Directions.All8.Select(d => keeper.Pos.Plus(d.dx, d.dy))
            .First(q => game.World.Road.Walkable(q) && !game.World.Npcs.Any(n => n.Pos == q));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(keeper.Pos.X - beside.X, keeper.Pos.Y - beside.Y));
        var fells = game.Topics.First(t => t.Label == "The fells");
        Assert.Contains(game.World.FellRegion.Name, fells.Answer);
        Assert.True(game.Topics.Count + game.Offers.Count <= 9); // the shared nine holds
    }

    /// <summary>A walkable heath cell with walkable ground around it, for a camp.</summary>
    private static Pos OpenHeath(Game game)
    {
        var fells = game.World.Fells;
        for (int x = 2; x < fells.Width - 2; x++)
            for (int y = 2; y < fells.Height - 2; y++)
            {
                var p = new Pos(x, y);
                if (fells[p] == Terrain.Heath && game.World.FellHerbs.All(h => h != p)
                    && p != game.World.FellWildsSite.OverworldPos)
                    return p;
            }
        throw new InvalidOperationException("no open heath");
    }

    /// <summary>One real step onto any walkable neighbour: enough to leave a marked cell.</summary>
    private static void ForagingStep(Game game)
    {
        foreach (var (dx, dy, key) in (ReadOnlySpan<(int, int, char)>)
                 [(1, 0, 'l'), (-1, 0, 'h'), (0, 1, 'j'), (0, -1, 'k')])
        {
            var to = game.Player.Pos.Plus(dx, dy);
            if (!game.CurrentMap.Walkable(to)) continue;
            if (game.Monsters.Any(m => m.Alive && m.SiteId == game.CurrentSite?.Id && m.Pos == to)) continue;
            game.ApplyKey(key);
            if (game.Player.Pos == to) return;
        }
        throw new InvalidOperationException("no open step");
    }

    private static char KeyFor(int dx, int dy) => (dx, dy) switch
    {
        (-1, 0) => 'h', (1, 0) => 'l', (0, -1) => 'k', (0, 1) => 'j',
        (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', _ => 'n',
    };

    private static void StepOnto(Game game, Pos cell)
    {
        foreach (var (dx, dy, key) in (ReadOnlySpan<(int, int, char)>)
                 [(0, -1, 'j'), (0, 1, 'k'), (-1, 0, 'l'), (1, 0, 'h'),
                  (-1, -1, 'n'), (1, -1, 'b'), (-1, 1, 'u'), (1, 1, 'y')])
        {
            var from = cell.Plus(dx, dy);
            if (!game.CurrentMap.Walkable(from)) continue;
            game.Debug_SetPlayerPos(from);
            game.ApplyKey(key);
            Assert.Equal(cell, game.Player.Pos);
            return;
        }
        throw new InvalidOperationException($"no open approach to {cell}");
    }
}
