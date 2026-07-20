using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// The valley's two faiths (D-114): the harrow, the order's house at the old
/// holy ground, and the shrinekeeper who makes the stead's shrine an
/// institution. Worldgen plants the founding and the doctrine's two readings;
/// the war, the aggressor, and the schism accounts wait for the War of
/// Faiths template.
/// </summary>
public class HarrowTests
{
    [Fact]
    public void EveryWorld_HoldsTheHarrow_AndBothFaithsFolk()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var world = WorldGen.Generate(seed);
            var harrow = world.HarrowSite;
            Assert.Equal(Terrain.HarrowEntrance, world.Overworld[harrow.OverworldPos]);
            Assert.Empty(harrow.Spawns);
            Assert.True(world.Facts.Exists("site", "harrow"));
            Assert.True(world.Facts.Exists("founding", "harrow_shrine"));

            var keeper = world.Keeper;
            Assert.Equal("shrinekeeper", keeper.Role);
            Assert.Equal(1, keeper.Pos.Chebyshev(world.ShrinePos));

            var elder = world.HarrowElder;
            Assert.Equal(1, elder.Pos.Chebyshev(harrow.OverworldPos));
            var doorward = world.Npcs.Single(n => n.Id == "npc_harrow_doorward");
            Assert.Equal(1, doorward.Pos.Chebyshev(harrow.OverworldPos));
            Assert.NotEqual(elder.Pos, doorward.Pos);
        }
    }

    [Fact]
    public void TheHarrowsFolk_StandOnPlainGround_AtEveryTier()
    {
        // A doorward cast onto some other site's mouth stands between every
        // walker and that door (found live by the pilot: a deep world's
        // journey stalled on exactly this). The folk keep to plain ground.
        for (ulong seed = 1; seed <= 12; seed++)
            for (int tier = 1; tier <= 8; tier++)
            {
                var world = WorldGen.Generate(seed, tier);
                foreach (var npc in world.Npcs.Where(n => n.Kind == NpcKind.Harrower))
                    Assert.True(world.Overworld[npc.Pos] is Terrain.Grass or Terrain.Forest or Terrain.Hills,
                        $"seed {seed} tier {tier}: {npc.Id} stands on {world.Overworld[npc.Pos]}");
            }
    }

    [Fact]
    public void TheTwins_DealTheSameValley()
    {
        var a = WorldGen.Generate(97);
        var b = WorldGen.Generate(97);
        Assert.Equal(a.HarrowSite.OverworldPos, b.HarrowSite.OverworldPos);
        Assert.Equal(a.Keeper.Pos, b.Keeper.Pos);
        Assert.Equal(a.Keeper.Name, b.Keeper.Name);
        Assert.Equal(a.HarrowElder.Pos, b.HarrowElder.Pos);
        Assert.Equal(a.HarrowElder.Name, b.HarrowElder.Name);
    }

    [Fact]
    public void TheKeeper_SpeaksTheSteadsReading_AndTheRumor()
    {
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.Keeper);
        Assert.True(game.InTalkMenu);

        var keeping = game.Topics.Single(t => t.Label == "The keeping");
        Assert.Contains("gift", keeping.Answer);
        var reading = game.Topics.Single(t => t.Label == "The harrow");
        Assert.Contains("two readings", reading.Answer);
        // The rumor line (D-114): the custody question walking down the hill.
        var claim = game.Topics.Single(t => t.Label == "The harrow's claim");
        Assert.Contains("means to come down", claim.Answer);
    }

    [Fact]
    public void TheElder_SpeaksTheDebtReading_AndTheFounding()
    {
        var game = new Game(42);
        NpcTests.BumpNpc(game, game.World.HarrowElder);
        Assert.True(game.InTalkMenu);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("of the harrow"));

        var stone = game.Topics.Single(t => t.Label == "The daughter-stone");
        Assert.Contains("came off our ring", stone.Answer);
        var owed = game.Topics.Single(t => t.Label == "What is owed");
        Assert.Contains("owed", owed.Answer);

        // The doorward keeps the door's shorter answers.
        game.ApplyKey(' ');
        NpcTests.BumpNpc(game, game.World.Npcs.Single(n => n.Id == "npc_harrow_doorward"));
        Assert.Contains(game.Topics, t => t.Label == "The stead below");
        Assert.DoesNotContain(game.Topics, t => t.Label == "What is owed");
    }

    [Fact]
    public void TheHarrow_IsEnterable_AndTheStonesStateTheFounding()
    {
        var game = new Game(42);
        var harrow = game.World.HarrowSite;
        game.Debug_SetPlayerPos(harrow.OverworldPos);
        game.Apply(Command.Enter);
        Assert.Equal(MapMode.Site, game.Mode);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("under the harrow's roof"));

        // Step onto the mother-stone's tile: the description fires on movement.
        game.Debug_SetPlayerPos(WorldGen.HarrowStonePos.Plus(-1, 0));
        game.Apply(Command.MoveE);
        Assert.Equal(WorldGen.HarrowStonePos, game.Player.Pos);
        Assert.Contains(game.Log.Recent(4), e => e.Text.Contains("mother-stone"));

        game.Debug_SetPlayerPos(harrow.EntryPos);
        game.Apply(Command.Exit);
        Assert.Equal(MapMode.Overworld, game.Mode);
    }
}
