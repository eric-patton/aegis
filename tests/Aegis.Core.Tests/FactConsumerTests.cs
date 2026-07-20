using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The fact graph's promised consumers (D-109): the three facts that stood
/// produced but unread get their readers. The confronted fact ends in the
/// making-right beat at the well (fed by both producers, the reckoning of
/// D-088 and the caught hand of D-107, gated on live shame back at zero);
/// the cellar secret matters in a raid's morning (gated on both facts, the
/// showing and the raid); and the lifted purse collides with trust when the
/// fence opens to a friend whose hand has been inside it unseen.
/// </summary>
public class FactConsumerTests
{
    [Fact]
    public void TheDebtMadeRight_IsMarkedAtTheWell_WhenEveryHandIsPaid()
    {
        var game = new Game(42);
        game.Player.Coin = 0;
        ShameTests.RobDoors(game, 3); // named a thief

        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        NpcTests.BumpNpc(game, villagers[0]); // the stead says its piece
        Assert.True(game.World.Facts.Exists("shame", "confronted"));
        game.ApplyKey(' ');

        // Every sill paid from the center, where all three doors are at arm's reach.
        game.Player.Coin = SteadShame.RepayCoin * 3;
        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, -2));
        for (int i = 0; i < 3; i++) game.Apply(Command.Grab);
        Assert.Equal(0, game.Shame);

        TalkUntil(game, villagers, "a nod is a document");
        Assert.True(game.World.Facts.Exists("shame", "made_right"));

        // Said once per world: the well does not repeat its documents.
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, villagers[1]);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("a nod is a document"));
    }

    [Fact]
    public void TheMadeRight_WaitsForTheLastSill()
    {
        var game = new Game(42);
        game.Player.Coin = 0;
        ShameTests.RobDoors(game, 3);
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager));
        game.ApplyKey(' ');

        // Two sills paid, one still standing: the debt is smaller, not gone.
        game.Player.Coin = SteadShame.RepayCoin * 2;
        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, -2));
        for (int i = 0; i < 2; i++) game.Apply(Command.Grab);
        Assert.Equal(1, game.Shame);

        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Kind == NpcKind.Villager));
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("a nod is a document"));
        Assert.False(game.World.Facts.Exists("shame", "made_right"));
    }

    [Fact]
    public void TheCaughtHand_Repaid_EarnsTheSameMark()
    {
        // The unified ladder's payoff (D-107): a caught lift's confronted fact
        // feeds the same making-right beat the reckoning does.
        var game = new Game(4); // pinned: the first lift is caught
        var mark = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        game.Debug_SetPlayerPos(mark.Pos.Plus(1, 0));
        game.Apply(Command.Lift);
        Assert.True(game.World.Facts.Exists("shame", "confronted"));
        Assert.Equal(1, game.Shame);

        game.Player.Coin = SteadShame.RepayCoin;
        game.Apply(Command.Lift); // restitution in the wronged hand
        Assert.Equal(0, game.Shame);

        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        TalkUntil(game, villagers, "a nod is a document");
        Assert.True(game.World.Facts.Exists("shame", "made_right"));
    }

    [Fact]
    public void TheDoorThatHeld_NeedsTheRaid_AndReadsItFromInsideTheCount()
    {
        var game = CrossTo(42, 2);
        game.Debug_ClearSite(SiteKind.Barrow);
        game.Debug_ClearCamp(); // regard 5: the stead's own, the showing opens

        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        for (int i = 0; i < 4 && !game.World.Facts.Exists("secret", "stead_cellar"); i++)
        {
            NpcTests.BumpNpc(game, villagers[i % villagers.Count]);
            game.ApplyKey(' ');
        }
        Assert.True(game.World.Facts.Exists("secret", "stead_cellar"));

        // The secret alone says nothing: no night has tested the door yet.
        for (int i = 0; i < 3; i++) ShameTests.StepStillNearAHouse(game);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("what the showing was for"));

        game.Debug_Raid();
        for (int i = 0; i < 6 && !game.Log.Entries.Any(e => e.Text.Contains("what the showing was for")); i++)
            ShameTests.StepStillNearAHouse(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("what the showing was for"));

        // Once per world: the morning is read a single time.
        ShameTests.StepStillNearAHouse(game);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("what the showing was for"));
    }

    [Fact]
    public void TheRaidAlone_SaysNothingOfTheCellar()
    {
        var game = new Game(42); // no showing: the door was never put in this knowing
        game.Debug_Raid();
        for (int i = 0; i < 3; i++) ShameTests.StepStillNearAHouse(game);

        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("what the showing was for"));
    }

    [Fact]
    public void TheTwoLedgers_AreCarriedTogether()
    {
        var game = new Game(1); // pinned: the first lift comes away clean
        var mark = game.World.Npcs.First(n => n.Kind == NpcKind.Villager);
        game.Debug_SetPlayerPos(mark.Pos.Plus(1, 0));
        game.Apply(Command.Lift);
        Assert.True(game.World.Facts.Exists("secret", "lifted_purse"));

        game.Debug_ClearCamp(); // the friend rung: the fence opens
        Assert.True(game.World.Facts.Exists("regard", "friend"));

        // The trust lands first (the hearthtale outranks), then its weight.
        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        TalkUntil(game, villagers, "line theirs is missing");
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("inside its own fence"));
    }

    [Fact]
    public void TheTwoLedgers_NeedTheLiftedPurse()
    {
        var game = new Game(1);
        game.Debug_ClearCamp(); // a friend with clean hands carries nothing hidden

        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        for (int i = 0; i < 4; i++)
        {
            NpcTests.BumpNpc(game, villagers[i % villagers.Count]);
            game.ApplyKey(' ');
        }

        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("line theirs is missing"));
    }

    [Fact]
    public void TheMendedPage_IsHowTheSteadRemembers()
    {
        // The made_right fact's consumer in its turn (D-113): on a later talk
        // than the well's nod, the stead is heard telling the paying-back as
        // a story it keeps on purpose.
        var game = MadeRightGame();
        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        TalkUntil(game, villagers, "see the stitching");

        // Without a risen chief, the valley's far book stays unread.
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("valley's memories"));

        // Once per world: the stead tells it, it does not recite it.
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, villagers[1]);
        game.ApplyKey(' ');
        Assert.Single(game.Log.Entries, e => e.Text.Contains("see the stitching"));
    }

    [Fact]
    public void TheTwoMemories_SetTheValleysBooksSideBySide()
    {
        // The made_right thread meeting the roster's memory (D-113): a bearer
        // who paid the stead's book shut while the dens' book passed, open,
        // to a risen heir, hears both read side by side, the heir named.
        var game = MadeRightGame();

        game.Debug_SetPlayerPos(game.World.CampSite.OverworldPos);
        game.Apply(Command.Enter);
        var chief = game.Monsters.Single(m => m.Chief);
        chief.Hp = 1;
        StrikeDown(game, chief);
        var heir = game.Monsters.Single(m => m.Alive && m.Chief);
        Assert.True(game.World.Facts.Exists("nemesis", "risen"));
        game.Apply(Command.Exit);

        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        TalkUntil(game, villagers, "valley's memories");
        Assert.Contains(game.Log.Entries,
            e => e.Text.Contains("come down that hill to settle one") && e.Text.Contains(heir.Epithet!));
    }

    /// <summary>A bearer named, confronted, and paid down to nothing: made_right stands.</summary>
    private static Game MadeRightGame()
    {
        var game = new Game(42);
        game.Player.Coin = 0;
        ShameTests.RobDoors(game, 3);
        var villagers = game.World.Npcs.Where(n => n.Kind == NpcKind.Villager).Take(2).ToList();
        NpcTests.BumpNpc(game, villagers[0]);
        game.ApplyKey(' ');

        game.Player.Coin = SteadShame.RepayCoin * 3;
        game.Debug_SetPlayerPos(game.World.ShrinePos.Plus(0, -2));
        for (int i = 0; i < 3; i++) game.Apply(Command.Grab);
        Assert.Equal(0, game.Shame);

        TalkUntil(game, villagers, "a nod is a document");
        Assert.True(game.World.Facts.Exists("shame", "made_right"));
        return game;
    }

    /// <summary>One killing blow: steps into the adjacent target's cell.</summary>
    private static void StrikeDown(Game game, Monster target)
    {
        if (target.Pos.Chebyshev(game.Player.Pos) != 1) target.Pos = OpenAt(game, game.Player.Pos);
        game.ApplyKey((Math.Sign(target.Pos.X - game.Player.Pos.X), Math.Sign(target.Pos.Y - game.Player.Pos.Y)) switch
        {
            (-1, -1) => 'y', (0, -1) => 'k', (1, -1) => 'u',
            (-1, 0) => 'h', (1, 0) => 'l',
            (-1, 1) => 'b', (0, 1) => 'j', _ => 'n',
        });
    }

    private static Pos OpenAt(Game game, Pos origin)
    {
        var map = game.CurrentMap;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var p = origin.Plus(dx, dy);
                if (p == origin || !map.Walkable(p)) continue;
                if (game.Monsters.Any(m => m.Alive && m.Pos == p)) continue;
                return p;
            }
        throw new InvalidOperationException("no open cell beside the bearer");
    }

    /// <summary>
    /// Bumps villagers in turn until the marker line lands. Talk beats take
    /// their turns by priority, one per conversation, so the beat under test
    /// may sit behind a hearthtale or a first meeting.
    /// </summary>
    private static void TalkUntil(Game game, IReadOnlyList<Npc> villagers, string marker)
    {
        for (int i = 0; i < 8 && !game.Log.Entries.Any(e => e.Text.Contains(marker)); i++)
        {
            NpcTests.BumpNpc(game, villagers[i % villagers.Count]);
            game.ApplyKey(' ');
        }
        Assert.Contains(game.Log.Entries, e => e.Text.Contains(marker));
    }

    private static Game CrossTo(ulong seed, int targetCycle)
    {
        var game = new Game(seed);
        while (game.Cycle < targetCycle)
        {
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.Apply(Command.Enter);
            game.Apply(Command.Enter);
        }
        return game;
    }
}
