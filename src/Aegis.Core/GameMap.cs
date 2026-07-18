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
}

public static class TerrainInfo
{
    public static bool Walkable(Terrain t) => t switch
    {
        Terrain.Water => false,
        Terrain.Wall => false,
        Terrain.House => false,
        _ => true,
    };
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
    /// Whether a straight line between two cells crosses no unwalkable tile
    /// (endpoints excluded). Bresenham, symmetric by construction of the octant
    /// walk; the graven men's throwing sight (D-040), blocked by quarry pillars.
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
            if ((x != to.X || y != to.Y) && !Walkable(new Pos(x, y))) return false;
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
