namespace SudokuObservable.Core;

/// <summary>A session in which one 9×9 grid is filled in. Every game starts with all 81 cells empty.</summary>
public sealed class Game
{
    private readonly Grid _grid = new();

    private Game()
    {
    }

    public static Game New() => new();

    public GameState State => _grid.State;

    /// <summary>
    /// Everything that happens to the grid: each move, followed by the steps of its cascade. A contradiction is a
    /// step too, so the stream never errors or completes. It is not necessarily the last step of its move: the
    /// eliminations of the placement that reached it still follow, but no further deductions do.
    /// </summary>
    public IObservable<Step> Steps => _grid.Steps;

    /// <summary>All 81 cells, row by row.</summary>
    public IEnumerable<Cell> Cells => _grid.Cells;

    public Cell Cell(int row, int column) => _grid.Cell(row, column);

    /// <summary>The player places <paramref name="digit"/> in a cell.</summary>
    public MoveOutcome Move(int row, int column, int digit) => _grid.Move(row, column, digit);
}
