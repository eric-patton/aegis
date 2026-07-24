namespace Aegis.Host;

public readonly record struct SquareCellLayout(
    int Columns,
    int Rows,
    int CellSize,
    int OriginX,
    int OriginY)
{
    public static SquareCellLayout Fit(
        int availableWidth,
        int availableHeight,
        int columns,
        int rows,
        int minimumCellSize = 1,
        int maximumCellSize = 64)
    {
        if (availableWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(availableWidth));
        if (availableHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(availableHeight));
        if (columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows));
        if (minimumCellSize <= 0 || maximumCellSize < minimumCellSize)
            throw new ArgumentOutOfRangeException(nameof(minimumCellSize));

        int fitted = Math.Min(availableWidth / columns, availableHeight / rows);
        int cellSize = Math.Clamp(fitted, minimumCellSize, maximumCellSize);
        int drawnWidth = columns * cellSize;
        int drawnHeight = rows * cellSize;
        return new SquareCellLayout(
            columns,
            rows,
            cellSize,
            (availableWidth - drawnWidth) / 2,
            (availableHeight - drawnHeight) / 2);
    }
}
