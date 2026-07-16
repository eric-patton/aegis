namespace Aegis.Core;

/// <summary>The seven attributes (D-015). All start at the humble baseline of 5.</summary>
public enum Attr { Might, Grace, Vigor, Wits, Mind, Will, Presence }

public sealed class AttributeSet
{
    public const int Baseline = 5;
    public const int Count = 7;

    private readonly int[] _values = [Baseline, Baseline, Baseline, Baseline, Baseline, Baseline, Baseline];

    public int this[Attr attr]
    {
        get => _values[(int)attr];
        internal set => _values[(int)attr] = value;
    }

    /// <summary>Total points bought above baseline; drives the global rising cost (D-014).</summary>
    public int TotalRaises => _values.Sum() - Baseline * Count;

    public static string NameOf(Attr attr) => attr switch
    {
        Attr.Might => "Might",
        Attr.Grace => "Grace",
        Attr.Vigor => "Vigor",
        Attr.Wits => "Wits",
        Attr.Mind => "Mind",
        Attr.Will => "Will",
        Attr.Presence => "Presence",
        _ => "?",
    };
}
