using System.Text;
using Aegis.Core;

namespace Aegis.Core.Tests;

public class SaveTests
{
    [Fact]
    public void Header_RoundTrips()
    {
        var (seed, keys) = SaveCodec.Parse(SaveCodec.EncodeHeader(123456789UL) + "\r\n");
        Assert.Equal(123456789UL, seed);
        Assert.Equal("", keys);
    }

    [Fact]
    public void Parse_RejectsGarbageAndWrongVersion()
    {
        Assert.Throws<FormatException>(() => SaveCodec.Parse("not a save"));
        Assert.Throws<FormatException>(() => SaveCodec.Parse("AEGIS-SAVE v999 seed:1"));
    }

    [Fact]
    public void Replay_ReproducesLiveGameExactly()
    {
        const ulong seed = 777;
        const string played = "llllkkkk..jjhh....llllbbbnnn";

        // Live game, recording its own journal through the event; the real wake
        // (D-092) asks first, and fate's one key answers on the record.
        var journal = new StringBuilder();
        var live = new Game(seed, firstWake: true);
        live.KeyApplied += k => journal.Append(k);
        live.ApplyKey('0');
        foreach (char key in played) live.ApplyKey(key);

        var replayed = SaveCodec.Replay(seed, journal.ToString());

        Assert.Equal(live.Turn, replayed.Turn);
        Assert.Equal(live.Player.Pos, replayed.Player.Pos);
        Assert.Equal(live.Player.Hp, replayed.Player.Hp);
        Assert.Equal(live.Player.Stamina, replayed.Player.Stamina);
        Assert.Equal(live.Player.Coin, replayed.Player.Coin);
        Assert.Equal(live.Player.Essence, replayed.Player.Essence);
        Assert.Equal(live.Log.Entries.Select(e => e.Text), replayed.Log.Entries.Select(e => e.Text));
    }

    [Fact]
    public void Replay_ReproducesDeathAndRemnant()
    {
        // Find a seed+script that reaches the cave and dies, by brute duel: use the
        // debug hooks on the live side is not replayable, so instead craft a real
        // scripted death: walk into the cave and wait among goblins until killed.
        const ulong seed = 42;
        var script = new StringBuilder("bbbbbbbbbbbbbjjjjjjjhhhhhhhkkk>");
        script.Append("kkkkkk");   // up the west corridor
        script.Append('l', 16);    // east through the first goblin, into the others' range
        script.Append('.', 80);    // stand still until something kills us

        var journal = new StringBuilder();
        var live = new Game(seed, firstWake: true);
        live.KeyApplied += k => journal.Append(k);
        live.ApplyKey('0');
        foreach (char key in script.ToString()) live.ApplyKey(key);

        Assert.True(live.Player.Deaths >= 1, "script did not produce a death; combat balance changed?");

        var replayed = SaveCodec.Replay(seed, journal.ToString());
        Assert.Equal(live.Player.Deaths, replayed.Player.Deaths);
        Assert.Equal(live.Player.WoundedTurns, replayed.Player.WoundedTurns);
        Assert.Equal(live.Remnant?.Pos, replayed.Remnant?.Pos);
        Assert.Equal(live.Remnant?.Coin, replayed.Remnant?.Coin);
        Assert.Equal(live.Remnant?.Essence, replayed.Remnant?.Essence);
        Assert.Equal(live.Player.Pos, replayed.Player.Pos);
    }

    [Fact]
    public void Replay_ReproducesShrineSpending()
    {
        const ulong seed = 55;
        var live = new Game(seed);
        var journal = new StringBuilder();
        live.KeyApplied += k => journal.Append(k);

        live.Player.Essence = 100; // grant directly; not journaled, so grant on both sides
        live.ApplyKey('r');        // rest at shrine (start pos IS the shrine)
        live.ApplyKey('3');        // raise Vigor
        live.ApplyKey('1');        // raise Might
        live.ApplyKey('x');        // close menu

        Assert.Equal(6, live.Player.Attributes[Attr.Vigor]);
        Assert.Equal(6, live.Player.Attributes[Attr.Might]);
        Assert.Equal(100 - 10 - 15, live.Player.Essence);
        Assert.False(live.InShrineMenu);

        var replayed = new Game(seed);
        replayed.Player.Essence = 100;
        foreach (char key in journal.ToString()) replayed.ApplyKey(key);

        Assert.Equal(live.Player.Attributes[Attr.Vigor], replayed.Player.Attributes[Attr.Vigor]);
        Assert.Equal(live.Player.Attributes[Attr.Might], replayed.Player.Attributes[Attr.Might]);
        Assert.Equal(live.Player.Essence, replayed.Player.Essence);
        Assert.Equal(live.Player.MaxHp, replayed.Player.MaxHp);
    }
}
