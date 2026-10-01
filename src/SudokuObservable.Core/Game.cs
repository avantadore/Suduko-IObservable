namespace SudokuObservable.Core;

/// <summary>A session in which one 9×9 grid is filled in. Every game starts with all 81 cells empty.</summary>
public sealed class Game
{
    private static readonly int[] AllDigits = [1, 2, 3, 4, 5, 6, 7, 8, 9];

    private Game()
    {
    }

    public static Game New() => new();

    public GameState State => GameState.InProgress;

    /// <summary>All 81 cells, row by row.</summary>
    public IEnumerable<Cell> Cells =>
        from row in Enumerable.Range(1, 9)
        from column in Enumerable.Range(1, 9)
        select Cell(row, column);

    public Cell Cell(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(row, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(row, 9);
        ArgumentOutOfRangeException.ThrowIfLessThan(column, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(column, 9);

        return new Cell(row, column, Digit: null, Source: null, AllDigits);
    }
}
