namespace Aegis.Core;

/// <summary>
/// The books (D-148): mundane script, the literacy lane's whole stock. Graven
/// script is not letters (the old words answer the warmth behind the eyes, not
/// schooling), so nothing here gates the stones; these are paper, ink, and the
/// patience to work through them. Each book is bought once, read in sittings at
/// the shrine's quiet, and pays a concrete keep when finished: no book in this
/// world is only words about words.
/// </summary>
public enum BookId { Herbal, Bestiary, Lay }

/// <summary>One book's catalog entry: the title, the coin, the Lore its lines ask, and the sittings it takes.</summary>
public sealed record BookDef(BookId Id, string Title, int Price, int LoreReq, int Sittings, string Blurb);

public static class BookCatalog
{
    public static readonly IReadOnlyList<BookDef> All =
    [
        // The herb lane's book: finishing it teaches the wort-cunning, and a
        // draught steeps from two sprigs wherever draughts are steeped.
        new(BookId.Herbal, "the hedge-wife's herbal", 8, 1, 5,
            "worts drawn true to the leaf, and the steepings written plain"),
        // The bestiary volume: a scholar's patient study of the old dead,
        // banked as reads the way watched wind-ups are (D-059's ledger).
        new(BookId.Bestiary, "the delver's bestiary", 9, 1, 5,
            "the buried kinds drawn from life, or from whatever the dead keep instead"),
        // The lay: the one text that asks a schooled eye (Lore 2). A story
        // read whole crosses as Legend once, at the crossing where Legend is
        // minted and nowhere else (D-048's one-home rule).
        new(BookId.Lay, "the lay of the kindled lands", 12, 2, 7,
            "the old verses of the worlds' first kindling, in a hand that knots"),
    ];

    public static BookDef Def(BookId id) => All.First(b => b.Id == id);

    /// <summary>Stable snake_case ids for snapshots, pilots, and offer args.</summary>
    public static string IdOf(BookId id) => id switch
    {
        BookId.Herbal => "herbal",
        BookId.Bestiary => "bestiary",
        BookId.Lay => "lay",
        _ => id.ToString().ToLowerInvariant(),
    };

    public static BookId FromId(string id) => All.First(b => IdOf(b.Id) == id).Id;
}

/// <summary>
/// The scrivener's board (D-148): the town's bookish anchor, and the school
/// for letters. A sitting is bought with coin and banked as Lore uses, the
/// D-141 pattern one street over: the sittings that carry a hand to Lore 1
/// are the act of learning its letters, and every sitting after is copy-work
/// that keeps the eye sharp. Cost-gated by construction: coin for the desk,
/// turns in the chair.
/// </summary>
public static class Scrivener
{
    public const int SittingCoin = 2;
    public const int SittingUses = 2;
}
