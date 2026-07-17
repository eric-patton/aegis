using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The steady state (D-045, arc sec 9): the laying-down verb at the hollow, the
/// hermit's per-answer reaction, the Unbinder argument thread (a beat at a time,
/// never binged), the shared trade, hearth-signs in deep worlds, and the long
/// song compounding through the crossing pipe.
/// </summary>
public class SteadyStateTests
{
    [Fact]
    public void TheLayingMenu_OpensOnlyForTheResolved()
    {
        // Unresolved: a bump is an attack, as it has been since cycle 2.
        var before = EnterHollow(Crossed(1));
        var keeper = Keeper(before);
        int hp = keeper.Hp;
        BumpKeeper(before);
        Assert.False(before.InLayingMenu);
        Assert.True(keeper.Hp < hp);

        // Resolved: the same bump opens the choice, turn-free, no blood drawn.
        var after = EnterHollow(Resolved(Crossed(1), Resolution.Kept));
        keeper = Keeper(after);
        hp = keeper.Hp;
        int turn = after.Turn;
        BumpKeeper(after);
        Assert.True(after.InLayingMenu);
        Assert.Equal(hp, keeper.Hp);
        Assert.Equal(turn, after.Turn);
        Assert.Contains(after.Log.Entries, e => e.Text.Contains("Yours to weigh now"));
    }

    [Fact]
    public void LayingItDown_IsGentle_ClearsTheRing_AndPaysNothing()
    {
        var game = EnterHollow(Resolved(Crossed(1), Resolution.Kept));
        var keeper = Keeper(game);
        int essence = game.Player.Essence;

        BumpKeeper(game);
        game.ApplyKey('2');

        Assert.False(keeper.Alive);
        Assert.True(game.CurrentSite!.Cleared);
        Assert.Equal(essence, game.Player.Essence);
        Assert.Equal(1, game.Player.SeveredUnbound);
        Assert.True(game.World.Facts.Exists("deed", "severed_laid"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the way a knot comes undone"));
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("closed and carried"));
        // The fight path's lines stay the fight path's.
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("a debt being read out"));
    }

    [Fact]
    public void TheRefused_LayItDown_InTheirOwnRegister()
    {
        var game = EnterHollow(Resolved(Crossed(1), Resolution.Refused));
        BumpKeeper(game);
        game.ApplyKey('2');
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("closed and struck out"));
    }

    [Fact]
    public void TheOldWay_Strikes_AndTheMomentCloses()
    {
        var game = EnterHollow(Resolved(Crossed(1), Resolution.Kept));
        var keeper = Keeper(game);
        int hp = keeper.Hp;

        BumpKeeper(game);
        game.ApplyKey('1');
        Assert.False(game.InLayingMenu);
        Assert.True(keeper.Hp < hp);

        // Once the old way is chosen, the moment does not reopen this world.
        if (game.Player.Pos.Chebyshev(keeper.Pos) != 1)
            game.Debug_SetPlayerPos(AdjacentOpen(game, keeper.Pos));
        hp = keeper.Hp;
        BumpKeeper(game);
        Assert.False(game.InLayingMenu);
        Assert.True(keeper.Hp < hp);
    }

    [Fact]
    public void SteppingBack_KeepsTheMomentOpen()
    {
        var game = EnterHollow(Resolved(Crossed(1), Resolution.Kept));
        var keeper = Keeper(game);

        BumpKeeper(game);
        game.ApplyKey(' ');
        Assert.False(game.InLayingMenu);
        Assert.True(keeper.Alive);

        BumpKeeper(game);
        Assert.True(game.InLayingMenu);
    }

    [Fact]
    public void TheHermit_HearsTheAnswer_EachWay()
    {
        var kept = Resolved(Crossed(2), Resolution.Kept);
        TalkTo(kept, kept.World.SeveredNpc!.Pos);
        Assert.Contains(kept.Log.Entries, e => e.Text.Contains("turning that over for years"));

        // Once ever: the beat is a complete story, not a greeting.
        kept.ApplyKey('x');
        TalkTo(kept, kept.World.SeveredNpc!.Pos);
        Assert.Single(kept.Log.Entries, e => e.Text.Contains("turning that over for years"));

        var refused = Resolved(Crossed(2), Resolution.Refused);
        TalkTo(refused, refused.World.SeveredNpc!.Pos);
        Assert.Contains(refused.Log.Entries, e => e.Text.Contains("The third road"));
    }

    [Fact]
    public void TheArgument_AdvancesABeat_AtMostOncePerCycle()
    {
        var game = Resolved(Crossed(2), Resolution.Kept);

        // Same cycle as the answer: the resumption waits for a crossing.
        TalkTo(game, game.World.Unbinder.Pos);
        Assert.DoesNotContain(game.Log.Entries, e => e.Text.Contains("one to the shield"));
        game.ApplyKey('x');

        Cross(game);
        TalkTo(game, game.World.Unbinder.Pos);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("one to the shield"));
        Assert.Equal(1, game.Player.ArgumentStage);

        // A line at a time: beat two does not fire in the same world.
        game.ApplyKey('x');
        TalkTo(game, game.World.Unbinder.Pos);
        Assert.Equal(1, game.Player.ArgumentStage);
        game.ApplyKey('x');

        Cross(game);
        TalkTo(game, game.World.Unbinder.Pos);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("both sides won them"));
        Assert.Equal(2, game.Player.ArgumentStage);
        game.ApplyKey('x');

        Cross(game);
        TalkTo(game, game.World.Unbinder.Pos);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("neither of us is in any hurry"));
        Assert.Equal(3, game.Player.ArgumentStage);
    }

    [Fact]
    public void TheFirstLayingDown_IsSharedWithTheMender_BeforeTheArgument()
    {
        var game = EnterHollow(Resolved(Crossed(1), Resolution.Kept));
        BumpKeeper(game);
        game.ApplyKey('2');
        Cross(game);

        // The trade outranks the argument that visit; the argument keeps.
        TalkTo(game, game.World.Unbinder.Pos);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("the other half of my trade"));
        Assert.Equal(0, game.Player.ArgumentStage);

        game.ApplyKey('x');
        TalkTo(game, game.World.Unbinder.Pos);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("one to the shield"));
        Assert.Equal(1, game.Player.ArgumentStage);
    }

    [Fact]
    public void HearthSigns_ReadTheAnswer_OnlyInDeepWorlds()
    {
        var kept = Resolved(Crossed(3), Resolution.Kept);
        kept.Debug_SetPlayerPos(kept.World.ShrinePos);
        kept.Apply(Command.Rest);
        Assert.Contains(kept.Log.Entries, e => e.Text.Contains("like a held chord"));

        var refused = Resolved(Crossed(3), Resolution.Refused);
        StepOnto(refused, refused.World.GatePos);
        Assert.Contains(refused.Log.Entries, e => e.Text.Contains("answers no ringer"));

        // Tier 1 is not deep: the signs stay below the surface.
        var shallow = Resolved(new Game(42), Resolution.Kept);
        shallow.Debug_SetPlayerPos(shallow.World.ShrinePos);
        shallow.Apply(Command.Rest);
        Assert.DoesNotContain(shallow.Log.Entries, e => e.Text.Contains("like a held chord"));
    }

    [Fact]
    public void TheLongSong_Compounds_AndIsSungOnce()
    {
        var game = new Game(42);
        string firstWorld = game.World.Name;
        Cross(game);
        Assert.Null(game.World.Facts.All.FirstOrDefault(f => f.Type == "song"));
        Cross(game);

        var song = game.World.Facts.All.FirstOrDefault(f => f.Type == "song" && f.Subject == "the_descent");
        Assert.NotNull(song);
        Assert.Contains($"First {firstWorld}", song!.Detail);
        Assert.Contains("world of glass", song.Detail);
        Assert.Equal(2, game.Player.WorldsWalked.Count);

        Resolved(game, Resolution.Kept);
        for (int i = 0; i < 8 && !game.Log.Entries.Any(e => e.Text.Contains("Songs are ledgers")); i++)
            StepNearHouse(game);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("Songs are ledgers"));

        for (int i = 0; i < 4; i++) StepNearHouse(game);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("Songs are ledgers"));
    }

    // ---- helpers, in the house pattern.

    private static Game Crossed(int crossings)
    {
        var game = new Game(42);
        for (int i = 0; i < crossings; i++) Cross(game);
        Assert.Equal(1 + crossings, game.Cycle);
        return game;
    }

    private static Game Resolved(Game game, Resolution answer)
    {
        game.Player.Resolution = answer;
        game.Player.ResolutionCycle = game.Cycle;
        return game;
    }

    private static Game EnterHollow(Game game)
    {
        game.Debug_SetPlayerPos(game.World.HollowSite!.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal("hollow", game.CurrentSite!.Id);
        return game;
    }

    private static Monster Keeper(Game game) =>
        game.Monsters.First(m => m.Alive && m.Kind == MonsterKind.Severed);

    private static void BumpKeeper(Game game)
    {
        var keeper = Keeper(game);
        game.Debug_SetPlayerPos(AdjacentOpen(game, keeper.Pos));
        StepInto(game, keeper.Pos);
    }

    /// <summary>Bump-to-talk from a walkable overworld cell beside the target.</summary>
    private static void TalkTo(Game game, Pos npcPos)
    {
        game.Debug_SetMode(MapMode.Overworld);
        var map = game.World.Overworld;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = npcPos.Plus(dx, dy);
            if (dx != 0 && dy != 0) continue;
            if (!map.Walkable(p) || game.World.Npcs.Any(n => n.Pos == p)) continue;
            game.Debug_SetPlayerPos(p);
            StepInto(game, npcPos);
            Assert.True(game.InTalkMenu);
            return;
        }
        Assert.Fail($"no walkable cardinal cell beside {npcPos}");
    }

    private static void StepOnto(Game game, Pos target)
    {
        game.Debug_SetMode(MapMode.Overworld);
        var map = game.World.Overworld;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = target.Plus(dx, dy);
            if (dx != 0 && dy != 0) continue;
            if (!map.Walkable(p) || game.World.Npcs.Any(n => n.Pos == p)) continue;
            game.Debug_SetPlayerPos(p);
            StepInto(game, target);
            Assert.Equal(target, game.Player.Pos);
            return;
        }
        Assert.Fail($"no walkable cardinal cell beside {target}");
    }

    private static Pos AdjacentOpen(Game game, Pos target)
    {
        var map = game.CurrentSite!.Map;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = target.Plus(dx, dy);
            if (map.Walkable(p) && p != game.Player.Pos
                && !game.Monsters.Any(m => m.Alive && m.Pos == p)) return p;
        }
        Assert.Fail($"no open cell beside {target}");
        return default;
    }

    private static void StepInto(Game game, Pos target)
    {
        int dx = Math.Sign(target.X - game.Player.Pos.X), dy = Math.Sign(target.Y - game.Player.Pos.Y);
        char key = (dx, dy) switch
        {
            (-1, 0) => 'h', (1, 0) => 'l', (0, -1) => 'k', (0, 1) => 'j',
            (-1, -1) => 'y', (1, -1) => 'u', (-1, 1) => 'b', _ => 'n',
        };
        game.ApplyKey(key);
    }

    private static void StepNearHouse(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        var map = game.World.Overworld;
        for (int y = 1; y < map.Height - 1; y++)
            for (int x = 1; x < map.Width - 1; x++)
            {
                var p = new Pos(x, y);
                if (!map.Walkable(p) || game.World.Npcs.Any(n => n.Pos == p)) continue;
                if (!Directions.All8.Any(d => map.InBounds(p.Plus(d.dx, d.dy)) && map[p.Plus(d.dx, d.dy)] == Terrain.House)) continue;
                foreach (var (dx, dy, key) in ((int, int, char)[])[(-1, 0, 'l'), (1, 0, 'h'), (0, -1, 'j'), (0, 1, 'k')])
                {
                    var from = p.Plus(dx, dy);
                    if (!map.Walkable(from) || game.World.Npcs.Any(n => n.Pos == from)) continue;
                    game.Debug_SetPlayerPos(from);
                    game.ApplyKey(key);
                    if (game.Player.Pos == p) return;
                }
            }
        Assert.Fail("no walkable tile beside a house");
    }

    private static void Cross(Game game)
    {
        game.Debug_SetMode(MapMode.Overworld);
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
    }
}
