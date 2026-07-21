using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Foraging (D-074): herbs the wood grows for the picking, gathered by anyone (no
/// lesson, unlike the gleaning), the Survival skill fattening the take, and sold for
/// coin at the wood's-edge bench (D-071). Placed on their own worldgen stream, so
/// pinned worlds and the gleanings keep their layouts.
/// </summary>
public class ForagingTests
{
    [Fact]
    public void Herbs_GrowInEveryWorld_OnForest_Deterministic_ClearOfGleanings()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var a = WorldGen.Generate(seed, tier: 1);
            var b = WorldGen.Generate(seed, tier: 1);
            Assert.NotEmpty(a.Herbs);
            Assert.Equal(a.Herbs, b.Herbs);                              // deterministic
            Assert.All(a.Herbs, h => Assert.Equal(Terrain.Forest, a.Overworld[h]));
            Assert.All(a.Herbs, h => Assert.DoesNotContain(h, a.Gleanings)); // no tile is both
        }
    }

    [Fact]
    public void ForagingAHerb_TakesIt_GrowsSurvival_NoLessonNeeded()
    {
        var game = new Game(42);
        Assert.False(game.Player.HasLesson(LessonId.Gleaning)); // untaught, and it does not matter
        game.Player.Herb = 0;
        int survivalBefore = game.Player.Skills.Uses(SkillId.Survival);
        var spot = game.World.Herbs[0];
        int spots = game.World.Herbs.Count;

        StepOnto(game, spot);

        Assert.Equal(1, game.Player.Herb);                       // one sprig at Survival 0
        Assert.Equal(spots - 1, game.World.Herbs.Count);         // the spot is spent
        Assert.True(game.Player.Skills.Uses(SkillId.Survival) > survivalBefore, "foraging taught nothing");
    }

    [Fact]
    public void TheRiddenStride_StillStoopsForTheSpot()
    {
        // The latent D-100 seam D-138 closed: a ridden step crosses two cells,
        // and a spot on the first of them must still be picked, or a rider can
        // orbit a herb forever without ever taking it (found live: the pilot
        // burning a whole world's key budget circling one).
        var game = new Game(42);
        var map = game.World.Overworld;
        (Pos start, Pos mid, Pos far)? run = null;
        for (int x = 2; x < map.Width - 2 && run is null; x++)
            for (int y = 2; y < map.Height - 2 && run is null; y++)
                foreach (var (dx, dy) in Directions.All8)
                {
                    var a = new Pos(x, y);
                    var b = a.Plus(dx, dy);
                    var c = b.Plus(dx, dy);
                    if (!map.InBounds(c)) continue;
                    if (map[a] != Terrain.Grass || map[b] != Terrain.Grass || map[c] != Terrain.Grass) continue;
                    if (game.World.Npcs.Any(n => !n.OnRoad && (n.Pos == a || n.Pos == b || n.Pos == c))) continue;
                    if (game.World.Herbs.Contains(b) || game.World.Herbs.Contains(c)) continue;
                    if (game.World.Gleanings.Contains(b) || game.World.Gleanings.Contains(c)) continue;
                    run = (a, b, c);
                    break;
                }
        Assert.NotNull(run);
        var (start, mid, far) = run!.Value;

        game.Debug_SetPlayerPos(start);
        // The stride only doubles with the beast within two: stand it at the side.
        game.Debug_SetMount(new Mount { Kind = MountKind.Mule, Pos = start });
        game.World.Herbs.Add(mid);
        game.Player.Herb = 0;

        game.ApplyKey(KeyFor(mid.X - start.X, mid.Y - start.Y));

        Assert.Equal(far, game.Player.Pos);                 // the stride carried two cells
        Assert.True(game.Player.Herb > 0, "the stride skipped the spot under it");
        Assert.DoesNotContain(mid, game.World.Herbs);
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
        _ => '.',
    };

    [Fact]
    public void Survival_FattensTheForage()
    {
        var game = new Game(42);
        while (game.Player.Skills.Level(SkillId.Survival) < 2) game.Player.Skills.AddUse(SkillId.Survival);
        Assert.Equal(1, game.Player.Skills.Bonus(SkillId.Survival));
        game.Player.Herb = 0;

        StepOnto(game, game.World.Herbs[0]);

        Assert.Equal(2, game.Player.Herb); // 1 + the Survival bonus
    }

    [Fact]
    public void TheBench_BuysHerbs_ForCoin()
    {
        var game = new Game(42);
        game.Player.Herb = 3;
        game.Player.Coin = 0;
        OpenBench(game);

        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Herb && o.Label.Contains("3 at 4c, 12 coin"));
        game.ApplyKey(TradeKey(game, TradeGood.Herb));

        Assert.Equal(0, game.Player.Herb);
        Assert.Equal(12, game.Player.Coin);
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Herb && o.Label.Contains("satchel empty"));
    }

    // ---- D-081: the herbwife's stillroom, the second bench ----

    [Fact]
    public void TheStillroom_PaysTheApothecarysPrice()
    {
        // The same satchel is worth a coin more a sprig at the stillroom than at
        // the wood's edge: the herbwife is the simples' true buyer, the woodward
        // a middleman, and carrying them in is the arbitrage.
        var game = new Game(42);
        game.Player.Herb = 3;
        game.Player.Coin = 0;
        OpenStillroom(game);

        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Herb && o.Label.Contains("3 at 5c, 15 coin"));
        game.ApplyKey(TradeKey(game, TradeGood.Herb));

        Assert.Equal(0, game.Player.Herb);
        Assert.Equal(15, game.Player.Coin); // 5 a sprig, not the wood's-edge 4
        Assert.Contains(game.Log.Recent(3), e => e.Text.Contains("full worth"));
    }

    [Fact]
    public void TheStillroom_KeepsTheDressingOnItsBench()
    {
        // The wound-dressing lives at the stillroom's table now (D-081), off the
        // herbwife's talk menu, which her topics alone can fill to the cap.
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_herbwife"));
        Assert.DoesNotContain(game.Offers, o => o.Good == TradeGood.Mending);
        Assert.Contains(game.Offers, o => o.Good == TradeGood.Trade);

        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Mending && o.Label.Contains("you are whole"));
    }

    [Fact]
    public void TheWoodsEdge_StillPaysItsOwnPrice()
    {
        // The two benches quote their own prices side by side: no shared state
        // bleeds between them beyond the satchel itself.
        var game = new Game(42);
        game.Player.Herb = 2;
        OpenStillroom(game);
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Herb && o.Label.Contains("2 at 5c"));
        game.ApplyKey(' '); // leave the stillroom, satchel unsold

        OpenBench(game);
        Assert.Contains(game.TradeOffers, o => o.Good == TradeGood.Herb && o.Label.Contains("2 at 4c"));
    }

    // ---- helpers ----

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

    private static void OpenBench(Game game)
    {
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_woodward"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.True(game.InTradeMenu);
    }

    private static void OpenStillroom(Game game)
    {
        NpcTests.BumpNpc(game, game.World.Npcs.First(n => n.Id == "npc_herbwife"));
        game.ApplyKey(OfferKey(game, TradeGood.Trade));
        Assert.True(game.InTradeMenu);
    }

    private static char OfferKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.Offers.Count; i++)
            if (game.Offers[i].Good == good)
                return (char)('1' + game.Topics.Count + i);
        throw new InvalidOperationException($"no {good} offer");
    }

    private static char TradeKey(Game game, TradeGood good)
    {
        for (int i = 0; i < game.TradeOffers.Count; i++)
            if (game.TradeOffers[i].Good == good)
                return (char)('1' + i);
        throw new InvalidOperationException($"no {good} entry at the bench");
    }
}
