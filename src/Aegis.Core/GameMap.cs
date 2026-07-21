namespace Aegis.Core;

public readonly record struct Pos(int X, int Y)
{
    public int Manhattan(Pos other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    public int Chebyshev(Pos other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
    public Pos Plus(int dx, int dy) => new(X + dx, Y + dy);
}

public enum Terrain : byte
{
    Grass,
    Forest,
    Hills,
    Water,
    House,
    Shrine,
    CampEntrance,
    Wall,
    Floor,
    ExitLadder,
    Waygate,
    BarrowEntrance,
    HollowEntrance,
    ThresholdEntrance,
    Hearth,
    QuarryEntrance,
    HallEntrance,
    RingfortEntrance,
    SonghallEntrance,
    Plinth,
    LeaguerEntrance,
    WildsEntrance,
    HarrowEntrance,
    RoadMouth,
    TownGate,
    Heath,
    Scree,
    FellMouth,
    CairnEntrance,
    GillEntrance,
}

/// <summary>
/// Which overworld a thing stands on (D-146): the D-138 road promoted the
/// question past "the valley or not", and the fells promote it past a bool.
/// Sites, folk, beasts, and the bearer all carry one; the crossing of a mouth
/// is the only thing that changes it.
/// </summary>
public enum Area : byte
{
    Valley,
    Road,
    Fells,
}

public static class TerrainInfo
{
    public static bool Walkable(Terrain t) => t switch
    {
        Terrain.Water => false,
        Terrain.Wall => false,
        Terrain.House => false,
        Terrain.Scree => false, // the fells' shattered rock (D-146): feet find no purchase.
        _ => true,
    };

    /// <summary>
    /// What blinds a line of sight (D-057): stone and timber, never water. The
    /// leaguer's mere stops feet, not eyes. No earlier site holds water or a
    /// house, so every line drawn before this distinction existed is unchanged.
    /// </summary>
    public static bool Opaque(Terrain t) => t is Terrain.Wall or Terrain.House;
}

public sealed class GameMap
{
    private readonly Terrain[] _tiles;

    public int Width { get; }
    public int Height { get; }
    public string Id { get; }

    public GameMap(string id, int width, int height, Terrain fill)
    {
        Id = id;
        Width = width;
        Height = height;
        _tiles = new Terrain[width * height];
        Array.Fill(_tiles, fill);
    }

    public Terrain this[Pos p]
    {
        get => _tiles[p.Y * Width + p.X];
        set => _tiles[p.Y * Width + p.X] = value;
    }

    public bool InBounds(Pos p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

    public bool Walkable(Pos p) => InBounds(p) && TerrainInfo.Walkable(this[p]);

    /// <summary>
    /// Whether a straight line between two cells crosses no opaque tile
    /// (endpoints excluded). Bresenham, symmetric by construction of the octant
    /// walk; the graven men's throwing sight (D-040), blocked by quarry pillars.
    /// Since D-057 the test is opacity, not walkability: the mere is clear to
    /// the eye, and no older site held a tile where the two answers differ.
    /// </summary>
    public bool LineOfSight(Pos from, Pos to)
    {
        int x = from.X, y = from.Y;
        int dx = Math.Abs(to.X - x), dy = -Math.Abs(to.Y - y);
        int sx = Math.Sign(to.X - x), sy = Math.Sign(to.Y - y);
        int err = dx + dy;
        while (true)
        {
            if (x == to.X && y == to.Y) return true;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
            var step = new Pos(x, y);
            if ((x != to.X || y != to.Y) && (!InBounds(step) || TerrainInfo.Opaque(this[step]))) return false;
        }
    }

    /// <summary>Stable content hash for determinism tests.</summary>
    public ulong ContentHash()
    {
        ulong h = 14695981039346656037UL;
        foreach (var t in _tiles)
        {
            h ^= (byte)t;
            h *= 1099511628211UL;
        }
        return h;
    }
}
