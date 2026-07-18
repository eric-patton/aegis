using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>Names v2 (D-049): the weave, its seams, and the walked-list reroll.</summary>
public class NameGenTests
{
    [Fact]
    public void TheWeave_IsDeterministic_PerStream()
    {
        var a = new Rng(7);
        var b = new Rng(7);
        Assert.Equal(NameGen.World(ref a), NameGen.World(ref b));
        Assert.Equal(NameGen.Settlement(ref a), NameGen.Settlement(ref b));
        Assert.Equal(NameGen.Person(ref a), NameGen.Person(ref b));
    }

    [Fact]
    public void TheWeave_KeepsItsSeamsClean()
    {
        // World pools carry no doubled letters inside any syllable, so a doubled
        // letter in a woven world name could only come from a dirty seam.
        var rng = new Rng(99);
        for (int i = 0; i < 500; i++)
        {
            string name = NameGen.World(ref rng);
            Assert.True(char.IsUpper(name[0]) && name.Skip(1).All(char.IsLower), name);
            Assert.InRange(name.Length, 5, 14);
            for (int c = 1; c < name.Length; c++)
                Assert.True(name[c] != name[c - 1], $"dirty seam in {name}");
        }
    }

    [Fact]
    public void TheWorldWeave_RerollsAgainstTheTaken()
    {
        // 300 sequential draws from one stream, each added to the taken set:
        // collisions are statistically certain at this volume, so distinctness
        // proves the reroll works.
        var rng = new Rng(4242);
        var taken = new HashSet<string>();
        for (int i = 0; i < 300; i++)
        {
            string name = NameGen.World(ref rng, taken);
            Assert.DoesNotContain(name, taken);
            taken.Add(name);
        }
        Assert.Equal(300, taken.Count);
    }

    [Fact]
    public void TheWorlds_AndTheSteads_KeepSeparateTongues()
    {
        // Disjoint closer pools: no world can ever share a full name with a stead.
        var rng = new Rng(11);
        var worlds = new HashSet<string>();
        var steads = new HashSet<string>();
        for (int i = 0; i < 200; i++)
        {
            worlds.Add(NameGen.World(ref rng));
            steads.Add(NameGen.Settlement(ref rng));
        }
        Assert.Empty(worlds.Intersect(steads));
    }

    [Fact]
    public void TheCrossing_NeverRepeatsAVerse()
    {
        // Five crossings on one character: the long song must hold six distinct
        // world names, current world included.
        var game = new Game(42);
        var sung = new List<string> { game.World.Name };
        for (int i = 0; i < 5; i++)
        {
            game.Debug_ClearCamp();
            game.Debug_SetPlayerPos(game.World.GatePos);
            game.ApplyKey('>');
            game.ApplyKey('>');
            Assert.Equal(i + 2, game.Cycle);
            Assert.DoesNotContain(game.World.Name, sung);
            sung.Add(game.World.Name);
        }
        Assert.Equal(sung.Take(5), game.Player.WorldsWalked);
    }
}
