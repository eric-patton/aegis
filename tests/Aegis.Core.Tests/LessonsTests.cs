using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Lessons (D-052): the proficiency half of D-016, shown rather than trained.
/// The woodward and the smith sell their showings as teaching entries, the
/// herbwife teaches through her hands at the first bought mending, and what is
/// learned stays learned across deaths and worlds.
/// </summary>
public class LessonsTests
{
    [Fact]
    public void TheWoodward_SellsTheGleaning_AtTheBench_OnceAndForCoin()
    {
        var game = new Game(42);
        NpcTests.BumpNpc(game, Npc(game, "npc_woodward"));

        // The woodward's one talk digit is the bench, not the lesson itself (D-071):
        // the teaching now lives at the wood's edge, a keypress deeper.
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Trade);
        Assert.DoesNotContain(game.Offers, o => o.Good == TradeGood.Lesson);
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.True(game.InTradeMenu);
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Lesson
            && o.Label.Contains("Be shown the gleaning (10 coin)"));
        char learn = TradeKey(game, TradeGood.Lesson);

        // Broke: the showing waits, and no coin moves.
        game.Player.Coin = 5;
        game.ApplyKey(learn);
        Assert.False(game.Player.HasLesson(LessonId.Gleaning));
        Assert.Equal(5, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("Knowing has a price"));

        // Flush: the lesson is taken, the label flips, the Aegis marks the
        // fourth ledger exactly once.
        game.Player.Coin = 12;
        game.ApplyKey(learn);
        Assert.True(game.Player.HasLesson(LessonId.Gleaning));
        Assert.Equal(2, game.Player.Coin);
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Lesson && o.Label.Contains("yours already"));
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("smallest part of my own work"));

        // Shown once is shown: no second charge, no second lesson.
        game.ApplyKey(learn);
        Assert.Equal(2, game.Player.Coin);
        Assert.Single(game.Player.Lessons);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("Shown once is shown"));
        game.ApplyKey(' '); // step back from the bench
        Assert.False(game.InTradeMenu);

        // A second lesson from the smith is still a plain talk offer: taught, paid,
        // and the Aegis stays quiet, the fourth ledger being already marked.
        game.Player.Coin = 20;
        NpcTests.BumpNpc(game, game.World.Smith);
        game.ApplyKey(OfferKey(game, TradeGood.Lesson));
        Assert.True(game.Player.HasLesson(LessonId.TendedIron));
        Assert.Equal(5, game.Player.Coin);
        Assert.DoesNotContain(game.Log.Recent(3), e => e.Text.Contains("smallest part of my own work"));
    }

    [Fact]
    public void TheBench_WeighsHidesForCoin_TheFifthLedger_OncePerLot()
    {
        var game = new Game(42);
        game.Player.Hide = 5;
        game.Player.Coin = 0;
        NpcTests.BumpNpc(game, Npc(game, "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));

        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Hide
            && o.Label.Contains("5 at 3c, 15 coin"));
        char sell = TradeKey(game, TradeGood.Hide);

        // The lot is weighed at once: five hides become fifteen coin, and the Aegis
        // marks the fifth ledger, the first the bearer filled by their own hand, once.
        game.ApplyKey(sell);
        Assert.Equal(0, game.Player.Hide);
        Assert.Equal(15, game.Player.Coin);
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Hide && o.Label.Contains("none cured yet"));
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("fifth ledger"));

        // An empty bundle sells nothing, takes nothing, and does not sound the Aegis twice.
        game.ApplyKey(sell);
        Assert.Equal(0, game.Player.Hide);
        Assert.Equal(15, game.Player.Coin);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("Bring me hides"));

        // A fresh lot pays again, and across the whole night the reflection sounded once.
        game.Player.Hide = 2;
        game.ApplyKey(sell);
        Assert.Equal(21, game.Player.Coin);
        Assert.Single(game.Log.Entries, e => e.Text.Contains("fifth ledger"));
    }

    [Fact]
    public void TheGleanings_AreSet_ForTaughtEyesOnly()
    {
        var game = new Game(42);
        Assert.True(game.World.Gleanings.Count >= 2, "seed 42 should set at least two gleanings");
        Assert.All(game.World.Gleanings, g => Assert.Equal(Terrain.Forest, game.World.Overworld[g]));

        var spot = game.World.Gleanings[0];
        int spots = game.World.Gleanings.Count;

        // Untaught: the step gathers nothing and the spot stands.
        StepOnto(game, spot);
        Assert.Equal(0, game.Player.Rations);
        Assert.Equal(spots, game.World.Gleanings.Count);

        // Taught: the same step gathers a ration and consumes the spot.
        game.Player.Lessons.Add(LessonId.Gleaning);
        StepOnto(game, spot);
        Assert.Equal(1, game.Player.Rations);
        Assert.Equal(spots - 1, game.World.Gleanings.Count);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("right where the lesson said to look"));

        // At the cap the spot is marked and left standing, to come back for.
        var second = game.World.Gleanings[0];
        game.Player.Rations = Game.RationCap;
        StepOnto(game, second);
        Assert.Equal(Game.RationCap, game.Player.Rations);
        Assert.Contains(game.World.Gleanings, g => g == second);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("leave it standing"));
    }

    [Fact]
    public void TheGleanings_MarkTheMap_OnlyForTheTaught()
    {
        var game = new Game(42);
        var spot = game.World.Gleanings[0];

        // A 200x60 frame holds the whole overworld, so map cell (x, y) renders
        // at screen (x, 1 + y) with the camera pinned at the origin.
        char At(Pos p) => Presenter.Render(game, 200, 60).ToTextLines()[1 + p.Y][p.X];

        Assert.NotEqual('"', At(spot));
        game.Player.Lessons.Add(LessonId.Gleaning);
        Assert.Equal('"', At(spot));
    }

    [Fact]
    public void TheTendedIron_SettlesWearAtRest_NeverPastHalf()
    {
        var game = new Game(42);
        game.Debug_GrantGear("woodaxe");
        game.Debug_GrantGear("quilted_jack");
        game.Player.Weapon!.Wear = 30; // max 40: past half, tending reaches it
        game.Player.Armor!.Wear = 10;  // below half: the lesson has nothing to say
        game.Debug_SetPlayerPos(game.World.ShrinePos);

        // Untaught: rest restores the body and leaves the iron alone.
        game.ApplyKey('r');
        game.ApplyKey(' ');
        Assert.Equal(30, game.Player.Weapon!.Wear);

        game.Player.Lessons.Add(LessonId.TendedIron);
        game.ApplyKey('r');
        game.ApplyKey(' ');
        Assert.Equal(20, game.Player.Weapon!.Wear);
        Assert.Equal(10, game.Player.Armor!.Wear);
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("waits for the wheel"));

        // Nothing past half: a second rest has nothing to tend and says nothing.
        game.ApplyKey('r');
        game.ApplyKey(' ');
        Assert.Equal(20, game.Player.Weapon!.Wear);
        Assert.DoesNotContain(game.Log.Recent(3), e => e.Text.Contains("waits for the wheel"));
    }

    [Fact]
    public void TheHerbwife_TeachesTheDressing_WithTheFirstBoughtMend()
    {
        var game = new Game(42);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        NpcTests.BumpNpc(game, Npc(game, "npc_herbwife"));
        // The dressing moved onto the stillroom's bench (D-081).
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        char mend = (char)('1' + game.TradeOffers.ToList().FindIndex(o => o.Good == TradeGood.Mending));

        // A refused mend teaches nothing: she works before she shows.
        game.Player.Coin = 1;
        game.ApplyKey(mend);
        Assert.False(game.Player.HasLesson(LessonId.CleanDressing));

        game.Player.Coin = 30;
        game.ApplyKey(mend);
        Assert.True(game.Player.HasLesson(LessonId.CleanDressing));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("Hands teach hands"));
        Assert.Contains(game.Log.Recent(6), e => e.Text.Contains("smallest part of my own work"));
    }

    [Fact]
    public void TheCleanDressing_TendsTheWound_WithTheMeal()
    {
        var game = new Game(42);
        game.Player.Rations = 3;
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Equal(80, game.Player.WoundedTurns);

        // Untaught, whole of body: the ration keeps, wounded or not.
        game.ApplyKey('e');
        Assert.Equal(3, game.Player.Rations);
        Assert.Equal(80, game.Player.WoundedTurns);

        // Taught: the meal is also wound-craft. Sixteen turns per ration, and
        // the eaten turn itself passes one more.
        game.Player.Lessons.Add(LessonId.CleanDressing);
        game.ApplyKey('e');
        Assert.Equal(2, game.Player.Rations);
        Assert.Equal(63, game.Player.WoundedTurns);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("It eases"));

        // The last of the weight lifts, and the body's full strength returns.
        game.Player.WoundedTurns = 10;
        game.ApplyKey('e');
        Assert.Equal(0, game.Player.WoundedTurns);
        Assert.Equal(game.Player.MaxHp, game.Player.EffectiveMaxHp);
        Assert.Contains(game.Log.Recent(2), e => e.Text.Contains("whole again"));
    }

    [Fact]
    public void TheLessons_SurviveDeath_AndCrossWhole()
    {
        var game = new Game(42);
        game.Player.Lessons.Add(LessonId.Gleaning);
        game.Player.Lessons.Add(LessonId.TendedIron);

        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Equal(2, game.Player.Lessons.Count);

        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.Equal(2, game.Player.Lessons.Count);

        // The far world sets its own table: fresh gleanings, unclaimed.
        Assert.True(game.World.Gleanings.Count >= 1);
    }

    [Fact]
    public void TheMenus_KeepTheirNineDigits_TeachingEntriesIncluded()
    {
        var game = new Game(42);
        for (int i = 0; i < 2; i++)
        {
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.Apply(Command.Enter);
            game.Apply(Command.Enter);
        }
        Assert.Equal(3, game.World.Tier);

        foreach (string id in (string[])["npc_steadholder", "npc_herbwife", "npc_woodward", "npc_smith"])
        {
            NpcTests.BumpNpc(game, Npc(game, id));
            Assert.True(game.Topics.Count + game.Offers.Count <= 9,
                $"{id} holds {game.Topics.Count} topics + {game.Offers.Count} offers");
            // The bench keeps its own nine (D-071): the vendor menu behind the woodward's
            // trade digit must fit too, or the sell path has simply moved the wall.
            // The steadholder's works bench (D-134, grown by D-135) answers the same law.
            if (id == "npc_woodward" || id == "npc_steadholder")
            {
                game.ApplyKey(OfferKey(game, TradeGood.Trade));
                Assert.True(game.TradeOffers.Count <= 9,
                    $"the bench holds {game.TradeOffers.Count} entries");
            }
            game.ApplyKey(' ');
        }
    }

    [Fact]
    public void TheFullestWorld_StillFitsEveryBoardOnNineKeys()
    {
        // The topic-budget audit (D-139, the D-134 follow-on): a full world's
        // general villager list reaches eight topics by itself (stead, raids,
        // shrine, arch, mound, ring, wanderer, songs), and every villager
        // door is a named one carrying at least one offer digit besides, so
        // the boards sit exactly at the wall and the season's news (D-133)
        // had no key left to live on: it moved to the shrinekeeper's door.
        // This test builds that fullest world deliberately and walks every
        // talking door in it.
        var game = new Game(42);
        game.Debug_HoldTheDeck();
        game.Debug_ClearCamp();
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.World.Tier); // the mound and the ring both stand

        // A song carried over the pass, and season news on the books.
        game.World.Facts.Add("echo", "deed", game.World.SettlementName,
            "A verse about a walker came over the pass ahead of the walker.");
        game.Debug_DrawSteadEvent("drovers");

        // The woodward proves the state is truly fullest: eight topics and
        // the bench digit, the wall itself, with no key to spare.
        NpcTests.BumpNpc(game, Npc(game, "npc_woodward"));
        Assert.Equal(9, game.Topics.Count + game.Offers.Count);
        game.ApplyKey(' ');

        // The news found its reader at the door with room.
        NpcTests.BumpNpc(game, game.World.Keeper);
        Assert.Contains(game.Topics, t => t.Label == "The season's news");
        Assert.True(game.Topics.Count + game.Offers.Count <= 9);
        game.ApplyKey(' ');

        // And every other talking door fits beside what it carries.
        foreach (string id in (string[])["npc_steadholder", "npc_herbwife",
            "npc_smith", "npc_skald", "npc_peddler"])
        {
            NpcTests.BumpNpc(game, Npc(game, id));
            Assert.True(game.Topics.Count + game.Offers.Count <= 9,
                $"{id} holds {game.Topics.Count} topics + {game.Offers.Count} offers");
            game.ApplyKey(' ');
        }
    }

    [Fact]
    public void TeachingSession_ReplaysIdenticallyFromJournal()
    {
        const ulong seed = 42;
        var live = new Game(seed);
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);

        // Granted, not journaled: grant identically on both sides.
        live.Player.Coin = 15;

        var target = Npc(live, "npc_woodward").Pos;
        for (int guard = 0; guard < 400 && !live.InTalkMenu; guard++)
        {
            // The shuttered window may open on the way (D-117); leaving is journaled too.
            if (live.InScene) { live.ApplyKey('3'); continue; }
            char? key = UnbinderTests.StepTo(live, target);
            if (key is null) break;
            live.ApplyKey(key.Value);
        }
        Assert.True(live.InTalkMenu, "bot never reached the woodward");

        live.ApplyKey(OfferKey(live, TradeGood.Trade));   // open the bench (D-071)
        live.ApplyKey(TradeKey(live, TradeGood.Lesson));  // buy the gleaning there
        live.ApplyKey(' ');                               // step back
        Assert.True(live.Player.HasLesson(LessonId.Gleaning));

        var replayed = new Game(seed);
        replayed.Player.Coin = 15;
        foreach (char key in journal.ToString()) replayed.ApplyKey(key);

        Assert.Equal(live.Player.Lessons, replayed.Player.Lessons);
        Assert.Equal(live.Player.Coin, replayed.Player.Coin);
        Assert.Equal(live.Turn, replayed.Turn);
        Assert.Equal(
            live.Log.Recent(10).Select(e => e.Text),
            replayed.Log.Recent(10).Select(e => e.Text));
    }

    private static Npc Npc(Game game, string id) => game.World.Npcs.First(n => n.Id == id);

    /// <summary>The digit that selects a good in the open talk menu.</summary>
    private static char OfferKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.Offers.Count; i++)
            if (game.Offers[i].Good == good)
                return (char)('1' + game.Topics.Count + i);
        throw new InvalidOperationException($"no {good} offer in this menu");
    }

    /// <summary>The digit that selects an entry at an open vendor's trade bench (D-071).</summary>
    private static char TradeKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.TradeOffers.Count; i++)
            if (game.TradeOffers[i].Good == good)
                return (char)('1' + i);
        throw new InvalidOperationException($"no {good} entry at the bench");
    }

    /// <summary>Places the bearer beside a cell and walks the one legal step onto it.</summary>
    private static void StepOnto(Game game, Pos cell)
    {
        foreach (var (dx, dy, key) in (ReadOnlySpan<(int, int, char)>)
                 [(0, -1, 'j'), (0, 1, 'k'), (-1, 0, 'l'), (1, 0, 'h'),
                  (-1, -1, 'n'), (1, -1, 'b'), (-1, 1, 'u'), (1, 1, 'y')])
        {
            var from = cell.Plus(dx, dy);
            if (!game.World.Overworld.Walkable(from) || game.World.Npcs.Any(n => n.Pos == from)) continue;
            game.Debug_SetPlayerPos(from);
            game.ApplyKey(key);
            Assert.Equal(cell, game.Player.Pos);
            return;
        }
        throw new InvalidOperationException($"no open approach to {cell}");
    }
}
