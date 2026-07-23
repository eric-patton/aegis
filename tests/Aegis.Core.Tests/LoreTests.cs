using Aegis.Core;

namespace Aegis.Core.Tests;

/// <summary>
/// Letters and Lore (D-148, plan 2026-07 step 13): the 14th skill, whose level
/// 1 IS literacy. The scrivener's desk sells the sittings that teach letters
/// (the D-141 school pattern), the shelf sells its books, and the shrine's
/// quiet is where pages are worked through, a sitting a key, each feeding Lore.
/// Graven script is exempt by doctrine: the stones answer something older than
/// schooling, so nothing shipped regresses. The tests hold the gate, the desk's
/// arithmetic, each book's concrete keep, the lettered starts, and the lay's
/// once-ever Legend at the crossing.
/// </summary>
public class LoreTests
{
    private static char OfferKey(Game game, TradeGood good, string arg = "")
        => (char)('1' + game.Topics.Count + game.Offers.ToList()
            .FindIndex(o => o.Good == good && (arg.Length == 0 || o.Arg == arg)));

    private static char TradeKey(Game game, TradeGood good, string arg = "")
        => (char)('1' + game.TradeOffers.ToList()
            .FindIndex(o => o.Good == good && (arg.Length == 0 || o.Arg == arg)));

    /// <summary>Walks the real road and gate: the mouth, then the arch.</summary>
    private static void EnterTown(Game game)
    {
        game.Debug_SetPlayerPos(game.World.RoadMouthPos);
        game.ApplyKey('>');
        Assert.True(game.OnRoad);
        game.Debug_SetPlayerPos(game.World.TownSite.OverworldPos);
        game.ApplyKey('>');
        Assert.Equal(MapMode.Site, game.Mode);
    }

    /// <summary>Bumps a towner inside the town through the real key surface.</summary>
    private static Npc BumpTowner(Game game, string id)
    {
        var npc = game.World.Npcs.First(n => n.Id == id);
        var town = game.CurrentSite!.Map;
        var beside = Directions.All8
            .Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => town.Walkable(p) && !game.World.Npcs.Any(n => n.SiteId == "town" && n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(npc.Pos.X - beside.X, npc.Pos.Y - beside.Y));
        return npc;
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
        _ => 'n',
    };

    [Fact]
    public void TheScrivener_KeepsTheMarketLeanTo_EverySeed()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var world = WorldGen.Generate(seed);
            var town = world.TownSite;
            var scrivener = world.Npcs.Single(n => n.Id == "npc_scrivener");
            Assert.Equal("scrivener", scrivener.Role);
            Assert.Equal("town", scrivener.SiteId);
            Assert.True(town.Map.Walkable(scrivener.Pos), $"seed {seed}: the scrivener stands in a wall");

            // The desk is reachable from the gate: someone beside it can be
            // walked to, so the bump lands the talk in every dealt town.
            var seen = new HashSet<Pos> { town.EntryPos };
            var queue = new Queue<Pos>();
            queue.Enqueue(town.EntryPos);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var (dx, dy) in Directions.All8)
                {
                    var next = p.Plus(dx, dy);
                    if (town.Map.Walkable(next) && seen.Add(next)) queue.Enqueue(next);
                }
            }
            Assert.Contains(seen, p => p.Chebyshev(scrivener.Pos) == 1);
        }
    }

    [Fact]
    public void TheLetters_AreBought_SittingBySitting()
    {
        var game = new Game(42);
        game.Player.Coin = Scrivener.SittingCoin * 4;
        EnterTown(game);
        BumpTowner(game, "npc_scrivener");
        Assert.True(game.InTalkMenu);

        // Unlettered: the shelf refuses the sale whole, and keeps no coin.
        game.ApplyKey(OfferKey(game, TradeGood.Shelf));
        game.ApplyKey(TradeKey(game, TradeGood.Book, "herbal"));
        Assert.Empty(game.Player.Books);
        Assert.Equal(Scrivener.SittingCoin * 4, game.Player.Coin);
        game.ApplyKey(' ');
        BumpTowner(game, "npc_scrivener");

        // Four sittings at two uses each carry the hand to Lore 1: lettered.
        for (int i = 0; i < 3; i++)
        {
            game.ApplyKey(OfferKey(game, TradeGood.Script));
            Assert.Equal(0, game.Player.Skills.Level(SkillId.Lore));
        }
        game.ApplyKey(OfferKey(game, TradeGood.Script));
        Assert.Equal(1, game.Player.Skills.Level(SkillId.Lore));
        Assert.Equal(0, game.Player.Coin);

        // Short coin: the chair refused, nothing banked.
        int uses = game.Player.Skills.Uses(SkillId.Lore);
        game.ApplyKey(OfferKey(game, TradeGood.Script));
        Assert.Equal(uses, game.Player.Skills.Uses(SkillId.Lore));
    }

    [Fact]
    public void TheHerbal_IsBought_ReadAtTheShrine_AndSteepsCheaper()
    {
        var game = new Game(42);
        game.Debug_BankLore(SkillSet.UsesForLevel(1));
        var herbal = BookCatalog.Def(BookId.Herbal);
        game.Player.Coin = herbal.Price;
        EnterTown(game);
        BumpTowner(game, "npc_scrivener");
        game.ApplyKey(OfferKey(game, TradeGood.Shelf));
        game.ApplyKey(TradeKey(game, TradeGood.Book, "herbal"));
        Assert.Contains(BookId.Herbal, game.Player.Books);
        Assert.Equal(0, game.Player.Coin);

        // Pages want the shrine's quiet: nothing reads inside the town.
        game.ApplyKey(' '); // the talk closed first, so the key reaches the verb
        int sittings = game.Player.BookSittings.GetValueOrDefault(BookId.Herbal);
        game.ApplyKey('v');
        Assert.Equal(sittings, game.Player.BookSittings.GetValueOrDefault(BookId.Herbal));

        // Home to the shrine by the real doors: the arch, the road, the mouth.
        // Each sitting there costs the turn and feeds Lore, and the last one
        // pays the wort-cunning: a draught from two sprigs.
        game.Debug_SetPlayerPos(game.CurrentSite!.EntryPos);
        game.ApplyKey('<');
        game.Debug_SetPlayerPos(game.World.RoadHomePos);
        game.ApplyKey('>');
        Assert.False(game.OnRoad);
        game.Debug_SetPlayerPos(game.World.ShrinePos);
        int usesBefore = game.Player.Skills.Uses(SkillId.Lore);
        Assert.Equal(3, game.DraughtNeed);
        for (int i = 0; i < herbal.Sittings; i++)
        {
            Assert.False(game.Player.HasRead(BookId.Herbal));
            int turn = game.Turn;
            game.ApplyKey('v');
            Assert.True(game.Turn > turn);
        }
        Assert.True(game.Player.HasRead(BookId.Herbal));
        Assert.True(game.Player.HasLesson(LessonId.WortCunning));
        Assert.Equal(usesBefore + herbal.Sittings, game.Player.Skills.Uses(SkillId.Lore));
        Assert.Equal(2, game.DraughtNeed);

        // A finished shelf feeds nothing: the reread is refused without a turn.
        int after = game.Turn;
        game.ApplyKey('v');
        Assert.Equal(after, game.Turn);
        Assert.Equal(usesBefore + herbal.Sittings, game.Player.Skills.Uses(SkillId.Lore));
    }

    [Fact]
    public void TheBestiary_ReadsTheOldDead_Keen()
    {
        var game = new Game(42);
        game.Debug_BankLore(SkillSet.UsesForLevel(1));
        game.Debug_GiveBook(BookId.Bestiary);
        game.Debug_SetPlayerPos(game.World.ShrinePos);
        Assert.Equal(ReadTier.Blur, game.Player.ReadOf(MonsterKind.Wight, game.Cycle));
        for (int i = 0; i < BookCatalog.Def(BookId.Bestiary).Sittings; i++) game.ApplyKey('v');
        Assert.True(game.Player.HasRead(BookId.Bestiary));
        Assert.Equal(ReadTier.Keen, game.Player.ReadOf(MonsterKind.Wight, game.Cycle));
    }

    [Fact]
    public void TheLay_AsksASchooledEye_AndCrossesAsLegend_Once()
    {
        // The control walks the same crossing without the lay: the diff is the proof.
        var control = new Game(42);
        control.Debug_ClearCamp();
        control.Player.Coin = 0;
        control.Debug_SetPlayerPos(control.World.GatePos);
        control.Apply(Command.Enter);
        control.Apply(Command.Enter);

        var game = new Game(42);
        game.Debug_GiveBook(BookId.Lay);
        game.Debug_BankLore(SkillSet.UsesForLevel(1));
        game.Debug_SetPlayerPos(game.World.ShrinePos);

        // Lore 1 opens the cover and the hand knots: no sitting, no turn.
        int turn = game.Turn;
        game.ApplyKey('v');
        Assert.Equal(turn, game.Turn);
        Assert.False(game.Player.BookSittings.ContainsKey(BookId.Lay));

        // A schooled eye (Lore 2) works it through, and the skald knows the reader.
        game.Debug_BankLore(SkillSet.UsesForLevel(2) - SkillSet.UsesForLevel(1));
        for (int i = 0; i < BookCatalog.Def(BookId.Lay).Sittings; i++) game.ApplyKey('v');
        Assert.True(game.Player.HasRead(BookId.Lay));
        Assert.True(game.World.Facts.Exists("book", "lay"));
        BumpSkald(game);
        Assert.Contains(game.Topics, t => t.Label == "The old lay");
        game.ApplyKey(' ');

        // The crossing weighs the story in, exactly two Legend over the control.
        game.Debug_ClearCamp();
        game.Player.Coin = 0;
        game.Debug_SetPlayerPos(game.World.GatePos);
        game.Apply(Command.Enter);
        game.Apply(Command.Enter);
        Assert.Equal(2, game.Cycle);
        Assert.True(game.Player.LayHonored);
        Assert.Equal(control.Player.Legend + 2, game.Player.Legend);
    }

    [Fact]
    public void TheUnlettered_MeetTheMarks_AndTheLetteredPastsDoNot()
    {
        // An unlettered bearer owns the page and cannot open it.
        var game = new Game(42);
        game.Debug_GiveBook(BookId.Herbal);
        game.Debug_SetPlayerPos(game.World.ShrinePos);
        int turn = game.Turn;
        game.ApplyKey('v');
        Assert.Equal(turn, game.Turn);
        Assert.Equal(0, game.Player.Skills.Uses(SkillId.Lore));
        Assert.False(game.Player.HasRead(BookId.Herbal));

        // The lettered pasts (D-148): the scribe's-ward and the hedge-healer
        // wake at Lore 1; the rest start below the marks.
        // Keys: folk, past, shaping done, thing, no burden, no vow, no face, name sealed.
        var ward = new Game(42, firstWake: true);
        foreach (char k in "150400..") ward.ApplyKey(k);
        Assert.False(ward.InCreation);
        Assert.Equal(PastId.ScribesWard, ward.Player.Past);
        Assert.Equal(1, ward.Player.Skills.Level(SkillId.Lore));

        var healer = new Game(42, firstWake: true);
        foreach (char k in "130400..") healer.ApplyKey(k);
        Assert.Equal(PastId.HedgeHealer, healer.Player.Past);
        Assert.Equal(1, healer.Player.Skills.Level(SkillId.Lore));

        var soldier = new Game(42, firstWake: true);
        foreach (char k in "110400..") soldier.ApplyKey(k);
        Assert.Equal(PastId.Soldier, soldier.Player.Past);
        Assert.Equal(0, soldier.Player.Skills.Level(SkillId.Lore));
    }

    /// <summary>Bumps the skald at the songhall door through the real key surface.</summary>
    private static void BumpSkald(Game game)
    {
        var skald = game.World.Skald;
        var beside = Directions.All8
            .Select(d => skald.Pos.Plus(d.dx, d.dy))
            .First(p => game.World.Overworld.Walkable(p) && !game.World.Npcs.Any(n => n.Pos == p));
        game.Debug_SetPlayerPos(beside);
        game.ApplyKey(KeyFor(skald.Pos.X - beside.X, skald.Pos.Y - beside.Y));
        Assert.True(game.InTalkMenu);
    }
}
