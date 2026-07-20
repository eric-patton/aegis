using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The named of the dens (D-110): D-023's bounded Nemesis-style roster. The
/// world seed names a chief and two lieutenants, the stead's rumor carries
/// the chief's name from the first morning, and the memory beats ride the
/// replay: the scar remembered, the succession that hands the camp on, and
/// the boast kept by the hand that authored the bearer's death.
/// </summary>
public class NemesisTests
{
    [Fact]
    public void TheRoster_IsNamed_AndTheRumorCarriesTheChief()
    {
        var game = new Game(1);
        var named = game.Monsters.Where(m => m.Epithet is not null).ToList();

        Assert.Equal(RaiderRoster.Named, named.Count);
        Assert.All(named, m => Assert.Equal(MonsterKind.Goblin, m.Kind));
        Assert.Equal(named.Select(m => m.Epithet).Distinct().Count(), named.Count);

        // Rank worn as hide: the chief the tougher goblin, the lieutenants next.
        var chief = Assert.Single(named, m => m.Chief);
        Assert.Equal(8 + RaiderRoster.ChiefHide, chief.MaxHp);
        Assert.All(named.Where(m => !m.Chief), m => Assert.Equal(8 + RaiderRoster.LieutenantHide, m.MaxHp));

        // Perceivable before a blow is traded: the rumor and the graph both carry the name.
        Assert.Contains(game.Log.Entries, e => e.Text.Contains($"is called {chief.Epithet}"));
        Assert.True(game.World.Facts.Exists("nemesis", "chief"));
    }

    [Fact]
    public void TheChief_IsAnnounced_AtTheFirstDescentOnly()
    {
        var game = new Game(1);
        var chief = game.Monsters.Single(m => m.Chief);
        EnterCamp(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains($"{chief.Epithet}, if the stead has the name right"));

        game.Apply(Command.Exit);
        EnterCamp(game);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("if the stead has the name right"));
    }

    [Fact]
    public void TheScar_IsRemembered_AndSpokenAtTheNextMeeting()
    {
        var game = new Game(1);
        var marked = game.Monsters.First(m => m.Epithet is not null && !m.Chief);
        EnterCamp(game);
        marked.Hp -= 2;
        game.Apply(Command.Exit);

        Assert.True(marked.Scarred);
        Assert.True(game.World.Facts.Exists("nemesis", "scarred"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains($"{marked.Epithet} is still breathing around a wound"));

        // The grudge is spoken to the bearer's face at the next meeting, once.
        EnterCamp(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains($"{marked.Epithet} marks you and touches its scar"));
        game.Apply(Command.Exit);
        EnterCamp(game);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("touches its scar"));
    }

    [Fact]
    public void TheUnbloodied_KeepNoScar()
    {
        var game = new Game(1);
        EnterCamp(game);
        game.Apply(Command.Exit);

        Assert.DoesNotContain(game.Monsters, m => m.Scarred);
        Assert.False(game.World.Facts.Exists("nemesis", "scarred"));
    }

    [Fact]
    public void TheSuccession_HandsTheCampOn()
    {
        var game = new Game(1);
        EnterCamp(game);
        var chief = game.Monsters.Single(m => m.Chief);
        chief.Hp = 1;
        StrikeDown(game, chief);
        Assert.False(chief.Alive);

        var heir = game.Monsters.Single(m => m.Alive && m.Chief);
        Assert.NotNull(heir.Epithet);
        Assert.True(heir.Rose);
        Assert.Same(heir, game.CampChief);
        Assert.True(game.World.Facts.Exists("nemesis", "risen"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains($"{heir.Epithet} has just risen"));
    }

    [Fact]
    public void TheLastNamed_FallsToSilence()
    {
        var game = new Game(1);
        EnterCamp(game);
        foreach (var lieutenant in game.Monsters.Where(m => m.Epithet is not null && !m.Chief))
            lieutenant.Hp = 0;
        var chief = game.Monsters.Single(m => m.Alive && m.Chief);
        chief.Hp = 1;
        StrikeDown(game, chief);

        Assert.Null(game.CampChief);
        Assert.False(game.World.Facts.Exists("nemesis", "risen"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("no voice takes up the order"));
    }

    [Fact]
    public void TheSlaying_KeepsTheBoast()
    {
        var game = new Game(1);
        EnterCamp(game);
        var chief = game.Monsters.Single(m => m.Chief);
        // The chief alone at the bearer's throat: the unnamed stand down so
        // the boast has one possible owner.
        foreach (var other in game.Monsters.Where(m => m.Alive && m != chief && m.SiteId == "goblin-camp"))
            other.Hp = 0;
        chief.Pos = OpenAt(game, game.Player.Pos, 1);
        game.Player.Hp = 1;
        for (int i = 0; i < 20 && game.Player.Deaths == 0; i++) game.Apply(Command.Wait);

        Assert.Equal(1, game.Player.Deaths);
        Assert.True(chief.SlewBearer);
        Assert.True(game.World.Facts.Exists("nemesis", "slew_bearer"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains($"The last face over you is {chief.Epithet}'s"));

        // Meeting your own killer: the boast is spoken to your face.
        EnterCamp(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains($"{chief.Epithet} sees you first, and grins"));
    }

    [Fact]
    public void TheGrudge_ArmsTheHand_ByOnePoint()
    {
        Assert.Equal(4, RaiderRoster.Armed(grudge: true, roll: 3));
        Assert.Equal(3, RaiderRoster.Armed(grudge: false, roll: 3));

        var raider = new Monster { Kind = MonsterKind.Goblin, Pos = new Pos(0, 0), SiteId = "goblin-camp", Epithet = "Gnarg" };
        Assert.False(raider.Grudge);
        raider.Scarred = true;
        Assert.True(raider.Grudge);
    }

    [Fact]
    public void TheRaidsTopic_CarriesTheRisenVoice()
    {
        var game = new Game(1);
        EnterCamp(game);
        var chief = game.Monsters.Single(m => m.Chief);
        chief.Hp = 1;
        StrikeDown(game, chief);
        var heir = game.Monsters.Single(m => m.Alive && m.Chief);
        game.Apply(Command.Exit);

        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager));
        string answer = game.Topics.Single(t => t.Label == "The goblin raids").Answer;
        Assert.Contains("a new voice over them", answer);
        Assert.Contains(heir.Epithet!, answer);
    }

    [Fact]
    public void TheRaidsTopic_ReadsTheLeaderlessDens()
    {
        var game = new Game(1);
        foreach (var named in game.Monsters.Where(m => m.Epithet is not null))
            named.Hp = 0;
        Assert.False(game.CampCleared);

        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager));
        string answer = game.Topics.Single(t => t.Label == "The goblin raids").Answer;
        Assert.Contains("no voice leads them now", answer);
    }

    [Fact]
    public void TheBoast_ReachesTheWell_AndIsLaughedOffOnce()
    {
        var game = new Game(1);
        EnterCamp(game);
        var chief = game.Monsters.Single(m => m.Chief);
        foreach (var other in game.Monsters.Where(m => m.Alive && m != chief && m.SiteId == "goblin-camp"))
            other.Hp = 0;
        chief.Pos = OpenAt(game, game.Player.Pos, 1);
        game.Player.Hp = 1;
        for (int i = 0; i < 20 && game.Player.Deaths == 0; i++) game.Apply(Command.Wait);
        Assert.True(chief.SlewBearer);

        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        for (int i = 0; i < 5 && !game.Log.Entries.Any(e => e.Text.Contains("what a den's word is worth")); i++)
        {
            NpcTests.BumpNpc(game, villagers[i % villagers.Count]);
            game.ApplyKey(' ');
        }
        Assert.Contains(game.Log.Entries,
            e => e.Text.Contains("what a den's word is worth") && e.Text.Contains($"{chief.Epithet}'s"));

        // Once per world: the well does not repeat the joke.
        NpcTests.BumpNpc(game, villagers[1]);
        game.ApplyKey(' ');
        Assert.Single(game.Log.Entries, e => e.Text.Contains("what a den's word is worth"));
    }

    private static void EnterCamp(Game game)
    {
        game.Debug_SetPlayerPos(game.World.CampSite.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal(MapMode.Site, game.Mode);
    }

    /// <summary>One killing blow: steps into the adjacent target's cell.</summary>
    private static void StrikeDown(Game game, Monster target)
    {
        if (target.Pos.Chebyshev(game.Player.Pos) != 1) target.Pos = OpenAt(game, game.Player.Pos, 1);
        game.ApplyKey(DirKey(Math.Sign(target.Pos.X - game.Player.Pos.X), Math.Sign(target.Pos.Y - game.Player.Pos.Y)));
    }

    private static Pos OpenAt(Game game, Pos origin, int dist)
    {
        var map = game.CurrentMap;
        for (int dx = -dist; dx <= dist; dx++)
            for (int dy = -dist; dy <= dist; dy++)
            {
                var p = origin.Plus(dx, dy);
                if (p.Chebyshev(origin) != dist || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                if (p == game.Player.Pos) continue;
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
}
