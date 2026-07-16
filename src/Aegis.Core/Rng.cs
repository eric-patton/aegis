namespace Aegis.Core;

/// <summary>
/// SplitMix64 stream. Cheap, statistically solid for game use, and trivially
/// serializable (the state is one ulong). Every subsystem gets its own stream
/// derived via <see cref="SeedTree"/>; streams are never shared (D-011 / vision sec 11).
/// </summary>
public struct Rng
{
    private ulong _state;

    public Rng(ulong seed) => _state = seed;

    public ulong State => _state;

    public ulong Next()
    {
        ulong z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Uniform int in [0, maxExclusive). Modulo bias is negligible at game ranges.</summary>
    public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(Next() % (ulong)maxExclusive);

    /// <summary>Uniform int in [min, maxExclusive).</summary>
    public int Range(int min, int maxExclusive) => min + Next(maxExclusive - min);

    public double NextDouble() => (Next() >> 11) * (1.0 / (1UL << 53));

    public bool Chance(double p) => NextDouble() < p;

    public T Pick<T>(IReadOnlyList<T> items) => items[Next(items.Count)];
}

/// <summary>
/// Hierarchical seed derivation: master seed hashed with stable string identifiers
/// (subsystem, region, site). FNV-1a over the child name, folded with the parent,
/// finished with a murmur-style avalanche.
/// </summary>
public static class SeedTree
{
    public static ulong Derive(ulong parent, string child)
    {
        ulong h = 14695981039346656037UL ^ parent;
        foreach (char c in child)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        h ^= h >> 33;
        h *= 0xFF51AFD7ED558CCDUL;
        h ^= h >> 33;
        return h;
    }

    public static ulong Derive(ulong parent, string child, int index)
    {
        ulong h = Derive(parent, child);
        h ^= (ulong)(uint)index * 0x9E3779B97F4A7C15UL;
        h ^= h >> 33;
        h *= 0xC4CEB9FE1A85EC53UL;
        h ^= h >> 33;
        return h;
    }

    public static ulong Derive(ulong parent, string child, int x, int y)
        => Derive(parent, child, x * 73856093 ^ y * 19349663 ^ (x << 16));
}
