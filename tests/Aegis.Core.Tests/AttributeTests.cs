using Aegis.Core;

namespace Aegis.Core.Tests;

public class AttributeTests
{
    [Fact]
    public void Rest_OnlyWorksAtTheShrine()
    {
        var game = new Game(9);
        game.Apply(Command.MoveE); // step off the shrine
        game.Apply(Command.Rest);
        Assert.False(game.InShrineMenu);

        game.Apply(Command.MoveW); // back onto it
        game.Apply(Command.Rest);
        Assert.True(game.InShrineMenu);
    }

    [Fact]
    public void Rest_HealsToEffectiveMax()
    {
        var game = new Game(9);
        game.Debug_HurtPlayer(7);
        game.Apply(Command.Rest);
        Assert.Equal(game.Player.EffectiveMaxHp, game.Player.Hp);
    }

    [Fact]
    public void Raising_CostsEscalate_AndVigorGrowsHp()
    {
        var game = new Game(9);
        game.Player.Essence = 25;
        game.Apply(Command.Rest);

        Assert.Equal(10, game.NextRaiseCost);
        game.ApplyKey('3'); // Vigor
        Assert.Equal(6, game.Player.Attributes[Attr.Vigor]);
        Assert.Equal(22, game.Player.MaxHp);
        Assert.Equal(11, game.Player.MaxStamina);
        Assert.Equal(15, game.Player.Essence);
        Assert.Equal(15, game.NextRaiseCost);

        game.ApplyKey('1'); // Might, exactly affordable
        Assert.Equal(6, game.Player.Attributes[Attr.Might]);
        Assert.Equal(0, game.Player.Essence);
    }

    [Fact]
    public void Raising_RefusedWhenUnaffordable()
    {
        var game = new Game(9);
        game.Player.Essence = 3;
        game.Apply(Command.Rest);
        game.ApplyKey('5'); // Mind
        Assert.Equal(5, game.Player.Attributes[Attr.Mind]);
        Assert.Equal(3, game.Player.Essence);
        Assert.True(game.InShrineMenu); // refusal does not close the menu
    }

    [Fact]
    public void AnyOtherKey_ClosesTheMenu_AndMovementResumes()
    {
        var game = new Game(9);
        game.Apply(Command.Rest);
        Assert.True(game.InShrineMenu);

        game.ApplyKey('l'); // in menu: closes it, does NOT move
        Assert.False(game.InShrineMenu);
        Assert.Equal(game.World.ShrinePos, game.Player.Pos);

        game.ApplyKey('l'); // now moves
        Assert.NotEqual(game.World.ShrinePos, game.Player.Pos);
    }

    [Fact]
    public void MightRaisesMeleeBonus()
    {
        var player = new Player();
        Assert.Equal(0, player.MeleeBonus);
        player.Attributes[Attr.Might] = 7;
        Assert.Equal(1, player.MeleeBonus);
        player.Attributes[Attr.Might] = 9;
        Assert.Equal(2, player.MeleeBonus);
    }
}
