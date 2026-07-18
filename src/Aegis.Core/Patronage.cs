namespace Aegis.Core;

public enum PatronDeedId
{
    RaisedStone,
    EndowedHearth,
    TrueVerse,
}

/// <summary>One patron deed's catalog entry: the coin it asks now, the Legend it weighs later.</summary>
public sealed record PatronDeedDef(PatronDeedId Id, string Name, int Price, int Worth);

/// <summary>
/// The patron's deeds (D-054, paying D-025's patronage crossing): coin pledged
/// into the stead's songhall, spent now like any sink, and weighed at the next
/// crossing at half again its coin, because patronized coin sings louder than
/// counted coin. Each deed is pledged once ever; what it buys stands in the
/// songhall of every world the bearer's songs reach after, as text and fact,
/// never as power. The prices are D-025's rising ladder: each rung sits above
/// the means of the world where the last one came into reach.
/// </summary>
public static class PatronCatalog
{
    public static readonly IReadOnlyList<PatronDeedDef> All =
    [
        new(PatronDeedId.RaisedStone, "the raised stone", 20, 30),
        new(PatronDeedId.EndowedHearth, "the endowed hearth", 60, 90),
        new(PatronDeedId.TrueVerse, "the true verse", 120, 180),
    ];

    public static PatronDeedDef Def(PatronDeedId id) => All.First(d => d.Id == id);

    /// <summary>Stable snake_case ids for snapshots, facts, and offer args.</summary>
    public static string IdOf(PatronDeedId id) => id switch
    {
        PatronDeedId.RaisedStone => "raised_stone",
        PatronDeedId.EndowedHearth => "endowed_hearth",
        PatronDeedId.TrueVerse => "true_verse",
        _ => id.ToString().ToLowerInvariant(),
    };

    public static PatronDeedId FromId(string id) => All.First(d => IdOf(d.Id) == id).Id;
}
