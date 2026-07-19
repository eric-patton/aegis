using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The huntsman's debt and the bond's ledger (D-097, stage 2): the woodward
/// cast off a talk once the stead has bled, loyalty beats banking from blood,
/// care, and firesides, the full death weight (grave fact, beloved fact,
/// stead grief, the memorial thread), and the arc's paid ending.
/// </summary>
public class GuestArcTests
{
    private static Pos OpenAt(Game game, Pos origin, int dist)
    {
        var map = game.CurrentMap;
        for (int dx = -dist; dx <= dist; dx++)
            for (int dy = -dist; dy <= dist; dy++)
            {
                var p = origin.Plus(dx, dy);
                if (p.Chebyshev(origin) != dist || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                if (game.World.Npcs.Any(n => n.Pos == p)) continue;
                if (p == game.Player.Pos || p == game.Guest?.Pos) continue;
                return p;
            }
        throw new InvalidOperationException($"no open cell at distance {dist}");
    }

    private static char DirKey(int dx, int dy) => (dx, dy) switch
    {
        (-1, -1) => 'y', (0, -1) => 'k', (1, -1) => 'u',
        (-1, 0) => 'h', (1, 0) => 'l',
        (-1, 1) => 'b', (0, 1) => 'j', _ => 'n',
    };

    /// <summary>Bump-talks the given NPC by standing beside and stepping in.</summary>
    private static void Talk(Game game, Npc npc)
    {
        var map = game.CurrentMap;
        foreach (var (dx, dy) in Directions.All8)
        {
            var beside = npc.Pos.Plus(dx, dy);
            if (!map.Walkable(beside)) continue;
            game.Debug_SetPlayerPos(beside);
            game.ApplyKey(DirKey(Math.Sign(npc.Pos.X - beside.X), Math.Sign(npc.Pos.Y - beside.Y)));
            return;
        }
        throw new InvalidOperationException("npc unreachable");
    }

    /// <summary>Draws raider blood so the woodward's grievance has a live edge (wrath 1).</summary>
    private static void BloodyTheBearer(Game game)
    {
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        var weak = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, game.Player.Pos, 1), SiteId = "goblin-camp", Hp = 1 };
        game.Monsters.Add(weak);
        game.ApplyKey(DirKey(Math.Sign(weak.Pos.X - game.Player.Pos.X), Math.Sign(weak.Pos.Y - game.Player.Pos.Y)));
        Assert.False(weak.Alive);
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_SetPlayerPos(game.World.ShrinePos);
    }

    [Fact]
    public void TheDebt_CastsTheWoodward_AndPaysOff_AtTheBrokenCamp()
    {
        var game = new Game(42);
        var woodward = game.World.Npcs.First(n => n.Id == "npc_woodward");

        // Before the stead has bled, the woodward keeps the bench.
        Talk(game, woodward);
        Assert.Null(game.Guest);
        game.ApplyKey(' '); // close the talk menu

        BloodyTheBearer(game);
        Talk(game, woodward);
        Assert.NotNull(game.Guest);
        Assert.Equal(woodward.Name, game.Guest!.Name);
        Assert.True(game.Guest.Fighter); // the woodward's hands know the work
        Assert.DoesNotContain(game.World.Npcs, n => n.Id == "npc_woodward");
        Assert.False(game.InTalkMenu); // the talk closed; the road starts here

        int regard = game.Regard;
        game.Debug_ClearCamp();
        Assert.Null(game.Guest); // the debt is paid; they walk their own roads now
        Assert.Contains(game.World.Npcs, n => n.Id == "npc_woodward"); // home to the bench
        Assert.Contains(game.World.Facts.All, f => f.Type == "portfolio" && f.Subject == "npc_woodward");
        Assert.True(game.Regard > regard); // a stead believes its own
    }

    [Fact]
    public void TheBeats_Bank_FromBloodAndCareAndFiresides()
    {
        var game = new Game(42);
        var guest = new Guest { Id = "guest_test", Name = "Oswin", Role = GuestRole.Huntsman, Pos = game.World.ShrinePos.Plus(1, 0), MaxHp = 16, Hp = 16 };
        if (!game.CurrentMap.Walkable(guest.Pos)) guest.Pos = OpenAt(game, game.Player.Pos, 1);
        game.Debug_SetGuest(guest);

        // Fireside words: a rest banks a beat, mends them whole, and speaks.
        guest.Hp = 9;
        game.ApplyKey('r');
        Assert.Equal(1, guest.Beats);
        Assert.Equal(guest.MaxHp, guest.Hp);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("at the fire"));
        game.ApplyKey(' '); // close the shrine menu

        // Care spent: a tending banks a beat.
        guest.Hp = 5;
        game.Player.Herb = 1;
        game.ApplyKey('o');
        Assert.Equal(2, guest.Beats);

        // Shared blood and the arc's own deed: a raider felled in reach banks both.
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        guest.Pos = OpenAt(game, game.Player.Pos, 1);
        var weak = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, game.Player.Pos, 1), SiteId = "goblin-camp", Hp = 1 };
        game.Monsters.Add(weak);
        game.ApplyKey(DirKey(Math.Sign(weak.Pos.X - game.Player.Pos.X), Math.Sign(weak.Pos.Y - game.Player.Pos.Y)));
        Assert.False(weak.Alive);
        Assert.Equal(4, guest.Beats);
    }

    [Fact]
    public void TheFall_WritesTheWeight_AndTheSteadRemembers()
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        var guest = new Guest
        {
            Id = "guest_npc_woodward", Name = "Oswin", Role = GuestRole.Huntsman,
            NpcId = "npc_woodward", Pos = OpenAt(game, game.Player.Pos, 2), MaxHp = 16, Hp = 2,
        };
        guest.Beats = 3;
        game.Debug_SetGuest(guest);
        var brute = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, guest.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        brute.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = guest.Pos };
        game.Monsters.Add(brute);

        game.ApplyKey('.');
        Assert.False(guest.Alive);
        Assert.Contains(game.World.Facts.All, f => f.Type == "guest-fell" && f.Subject == "npc_woodward");
        Assert.Contains(game.World.Facts.All, f => f.Type == "guest-beloved");
        Assert.Equal(1, game.Shame); // a life spent in your keeping costs standing

        // The memorial thread: the stead passes the name hand to hand, once.
        // Higher-priority one-shot talk beats (the world-story's plaintiff)
        // may claim the first talks; they drain, and the memorial keeps.
        game.Debug_SetMode(MapMode.Overworld);
        var mourner = game.World.Npcs.First(n => n.Kind == NpcKind.Villager && n.Id != "npc_woodward");
        bool remembered = false;
        for (int i = 0; i < 4 && !remembered; i++)
        {
            Talk(game, mourner);
            remembered = game.Log.Recent(8).Any(e => e.Text.Contains("stays warm"));
            game.ApplyKey(' ');
        }
        Assert.True(remembered);
    }

    [Fact]
    public void TheFall_UnlovedOrUnknown_LeavesNoBelovedFact()
    {
        var game = new Game(42);
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        var guest = new Guest
        {
            Id = "guest_npc_woodward", Name = "Oswin", Role = GuestRole.Huntsman,
            NpcId = "npc_woodward", Pos = OpenAt(game, game.Player.Pos, 2), MaxHp = 16, Hp = 2,
        };
        game.Debug_SetGuest(guest); // no beats banked: the road was too short
        var brute = new Monster { Kind = MonsterKind.Goblin, Pos = OpenAt(game, guest.Pos, 1), SiteId = "goblin-camp", Hp = 60 };
        brute.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = guest.Pos };
        game.Monsters.Add(brute);

        game.ApplyKey('.');
        Assert.False(guest.Alive);
        Assert.Contains(game.World.Facts.All, f => f.Type == "guest-fell");
        Assert.DoesNotContain(game.World.Facts.All, f => f.Type == "guest-beloved");
    }
}
