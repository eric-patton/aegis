using Aegis.Core;

namespace Aegis.Core.Tests;

public class DeathTests
{
    private static Game GameWithLoot(ulong seed = 7)
    {
        var game = new Game(seed);
        game.Player.Coin = 25;
        game.Player.Essence = 10;
        return game;
    }

    [Fact]
    public void Death_DropsRemnant_AndRespawnsAtShrineWounded()
    {
        var game = GameWithLoot();
        game.Debug_SetMode(MapMode.Site);
        var deathSpot = game.World.CampEntryPos;
        game.Debug_SetPlayerPos(deathSpot);

        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        Assert.NotNull(game.Remnant);
        Assert.Equal(25, game.Remnant!.Coin);
        Assert.Equal(10, game.Remnant.Essence);
        Assert.Equal("goblin-camp", game.Remnant.MapId);
        Assert.Equal(deathSpot, game.Remnant.Pos);

        Assert.Equal(0, game.Player.Coin);
        Assert.Equal(0, game.Player.Essence);
        Assert.Equal(MapMode.Overworld, game.Mode);
        Assert.Equal(game.World.ShrinePos, game.Player.Pos);
        Assert.True(game.Player.WoundedTurns > 0);
        Assert.Equal(game.Player.EffectiveMaxHp, game.Player.Hp);
        Assert.True(game.Player.EffectiveMaxHp < game.Player.MaxHp);
        Assert.Equal(1, game.Player.Deaths);
    }

    [Fact]
    public void Grab_OnRemnant_ReclaimsEverything()
    {
        var game = GameWithLoot();
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        // Walk back in and grab: simulate directly by teleporting to the remnant.
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.Remnant!.Pos);
        game.Apply(Command.Grab);

        Assert.Null(game.Remnant);
        Assert.Equal(25, game.Player.Coin);
        Assert.Equal(10, game.Player.Essence);
    }

    [Fact]
    public void SecondDeath_ForfeitsOldRemnant()
    {
        var game = GameWithLoot();
        game.Debug_SetMode(MapMode.Site);
        game.Debug_SetPlayerPos(game.World.CampEntryPos);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        var firstRemnant = game.Remnant;
        Assert.NotNull(firstRemnant);

        // Die again carrying a little new coin: the old remnant must be gone.
        game.Player.Coin = 3;
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();

        Assert.NotNull(game.Remnant);
        Assert.NotSame(firstRemnant, game.Remnant);
        Assert.Equal(3, game.Remnant!.Coin);
        Assert.Equal(2, game.Player.Deaths);
        Assert.Contains(game.Log.Entries, e => e.Text.Contains("takes twice"));
    }

    [Fact]
    public void DeathWithEmptyPockets_LeavesNoRemnant()
    {
        var game = new Game(7);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.Null(game.Remnant);
    }

    [Fact]
    public void Wounded_ExpiresAfterItsTurns()
    {
        var game = new Game(7);
        game.Debug_HurtPlayer(999);
        game.Debug_ForceDeathCheck();
        Assert.True(game.Player.WoundedTurns > 0);
        int woundedMax = game.Player.EffectiveMaxHp;

        for (int i = 0; i < 100 && game.Player.WoundedTurns > 0; i++)
            game.Apply(Command.Wait);

        Assert.Equal(0, game.Player.WoundedTurns);
        Assert.Equal(game.Player.MaxHp, game.Player.EffectiveMaxHp);
        Assert.True(game.Player.EffectiveMaxHp > woundedMax);
    }
}
