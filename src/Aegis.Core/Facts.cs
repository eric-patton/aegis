namespace Aegis.Core;

/// <summary>
/// One ID-referenced fact in the world graph (D-018 layer 1). The graph is the
/// single source of truth: quests, rumors, prices, and mythology all trace here.
/// The slice carries only a handful of worldgen facts plus runtime deed facts.
/// </summary>
public sealed record Fact(int Id, string Type, string Subject, string Object, string Detail);

public sealed class FactGraph
{
    private readonly List<Fact> _facts = [];
    private int _nextId = 1;

    public IReadOnlyList<Fact> All => _facts;

    public Fact Add(string type, string subject, string obj, string detail = "")
    {
        var fact = new Fact(_nextId++, type, subject, obj, detail);
        _facts.Add(fact);
        return fact;
    }

    public IEnumerable<Fact> OfType(string type) => _facts.Where(f => f.Type == type);

    public Fact? Find(string type, string subject)
        => _facts.FirstOrDefault(f => f.Type == type && f.Subject == subject);

    public bool Exists(string type, string subject) => Find(type, subject) is not null;
}
