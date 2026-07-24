using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The dialogue-tree scene layer (D-117): a storylet can open a modal scene whose
/// digits are journaled keys, whose checked choices show their odds before the
/// player commits, and whose prose lands in the log as the one full transcript.
/// The first content is the shuttered window, grievance-voiced grown into a scene.
/// </summary>
public class ScenesTests
{
    /// <summary>Settlement center sits two tiles north of the shrine, ringed by houses.</summary>
    private static Game WindowOpen(ulong seed)
    {
        var game = new Game(seed);
        for (int i = 0; i < 40 && !game.InScene; i++) StepNearHouse(game);
        Assert.True(game.InScene, $"the shuttered window never opened at seed {seed}");
        return game;
    }

    private static void StepNearHouse(Game game)
    {
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

    private static bool LogContains(Game game, string fragment)
        => game.Log.Entries.Any(e => e.Text.Contains(fragment));

    [Fact]
    public void TheWindow_OpensAsAScene_WithTheCheckShown()
    {
        var game = WindowOpen(42);

        Assert.Equal("The shuttered window", game.SceneTitle);
        Assert.Equal(3, game.SceneChoices.Count);
        // The visible check (D-021 cashed): the odds stand on the choice row
        // before anything is committed, read off the bearer's own sheet.
        Assert.Contains("Presence", game.SceneChoices[0].Tag);
        Assert.Contains("in 100", game.SceneChoices[0].Tag);
        Assert.Equal("", game.SceneChoices[1].Tag);
        Assert.Equal("", game.SceneChoices[2].Tag);

        // The log stays the one full transcript, and the meeting is written.
        Assert.True(LogContains(game, "A shutter opens a finger's width"));
        Assert.True(game.World.Facts.Exists("met", "worried_villager"));
    }

    [Fact]
    public void ThePressing_RollsTheShownOdds_AndBranchesOnThem()
    {
        int carried = 0, failed = 0;
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = WindowOpen(seed);
            game.ApplyKey('1');

            if (LogContains(game, "it carries."))
            {
                carried++;
                Assert.True(LogContains(game, "The shutter swings wide"));
                Assert.True(game.World.Facts.Exists("counsel", "camp_ways"));
            }
            else
            {
                failed++;
                Assert.True(LogContains(game, "it fails."));
                Assert.True(LogContains(game, "a bolt slides home"));
                Assert.False(game.World.Facts.Exists("counsel", "camp_ways"));
            }

            // Both endings are terminal: any key lets the moment go.
            Assert.True(game.InScene);
            Assert.Empty(game.SceneChoices);
            game.ApplyKey(' ');
            Assert.False(game.InScene);
        }

        // At 40 in 100 both branches must occur across twenty steads.
        Assert.True(carried >= 1, "the pressing never carried");
        Assert.True(failed >= 1, "the pressing never failed");
    }

    [Fact]
    public void TheWordGiven_WritesThePromise()
    {
        var game = WindowOpen(42);
        game.ApplyKey('2');

        Assert.True(LogContains(game, "Words are thin blankets"));
        Assert.True(game.World.Facts.Exists("promise", "quiet_nights"));
        game.ApplyKey(' ');
        Assert.False(game.InScene);
    }

    [Fact]
    public void LeavingIsAnAnswerToo_AndTheBeatStaysSpent()
    {
        var game = WindowOpen(42);
        game.ApplyKey('3');

        Assert.False(game.InScene);
        Assert.False(game.World.Facts.Exists("promise", "quiet_nights"));
        Assert.False(game.World.Facts.Exists("counsel", "camp_ways"));

        // Once is once: walking the lane again does not reopen the window.
        for (int i = 0; i < 6; i++) StepNearHouse(game);
        Assert.False(game.InScene);
        Assert.Equal(1, game.Log.Entries.Count(e => e.Text.Contains("A shutter opens a finger's width")));
    }

    [Fact]
    public void TheMoment_WaitsToBeAnswered()
    {
        var game = WindowOpen(42);
        var pos = game.Player.Pos;
        int turn = game.Turn;

        // While choices stand, a scene is a moment, not a menu: movement, rest,
        // and stray keys are the scene waiting, and the world holds still.
        foreach (char key in "krz9 ")
            game.ApplyKey(key);

        Assert.True(game.InScene);
        Assert.Equal(pos, game.Player.Pos);
        Assert.Equal(turn, game.Turn);
    }

    [Fact]
    public void TheOdds_StepWithTheAsking_AndNeverLeaveTheTable()
    {
        var game = new Game(42);
        double plain = SceneCheck.OfAttr(Attr.Presence).ChanceFor(game);
        double pressed = SceneCheck.OfAttr(Attr.Presence, difficulty: 1).ChanceFor(game);
        Assert.Equal(0.5, plain, 3);
        Assert.Equal(0.4, pressed, 3);

        // The floor and the ceiling keep every check a real one.
        Assert.Equal(0.05, SceneCheck.OfAttr(Attr.Presence, difficulty: 10).ChanceFor(game), 3);
        Assert.Equal(0.95, SceneCheck.OfSkill(SkillId.Sleight, difficulty: -10).ChanceFor(game), 3);
    }

    [Fact]
    public void ASceneSession_ReplaysIdenticallyFromJournal()
    {
        // The parity proof runs through the real wake (D-092): fate answers the
        // asking, then the window opens on journaled steps alone and is answered.
        var game = new Game(42, firstWake: true);
        var journal = new List<char>();
        game.KeyApplied += journal.Add;
        game.ApplyKey('0');
        game.ApplyKey('.');

        // Walk (journaled) rather than teleport: replays must not need debug
        // hooks. The settlement's houses ring the ground just north of the shrine.
        for (int i = 0; i < 30 && !game.InScene; i++)
            game.ApplyKey(i % 2 == 0 ? 'k' : 'j');
        Assert.True(game.InScene, "the shuttered window never opened");
        game.ApplyKey('1');
        game.ApplyKey(' ');
        Assert.False(game.InScene);

        var replayed = SaveCodec.Replay(42, new string(journal.ToArray()));

        Assert.Equal(game.Turn, replayed.Turn);
        Assert.Equal(game.InScene, replayed.InScene);
        Assert.Equal(game.World.Facts.All.Count, replayed.World.Facts.All.Count);
        Assert.Equal(
            game.Log.Recent(15).Select(e => e.Text),
            replayed.Log.Recent(15).Select(e => e.Text));
    }
}
