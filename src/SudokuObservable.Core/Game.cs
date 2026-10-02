namespace SudokuObservable.Core;

/// <summary>A session in which one 9×9 grid is filled in. Every game starts with all 81 cells empty.</summary>
public sealed class Game
{
    private readonly Grid _grid = new();
    private readonly List<RecordedMove> _moves = [];

    private Game()
    {
    }

    public static Game New() => new();

    public GameState State => _grid.State;

    /// <summary>The moves this game has accepted, in order.</summary>
    public IReadOnlyList<RecordedMove> Moves => _moves.AsReadOnly();

    /// <summary>How many moves from the start of <see cref="Moves"/> apply to the grid. 0 is the empty grid.</summary>
    public int Position => _moves.Count;

    /// <summary>
    /// Everything that happens to the grid: each move, followed by the steps of its cascade. A contradiction is a
    /// step too, so the stream never errors or completes. It is not necessarily the last step of its move: the
    /// eliminations of the placement that reached it still follow, but no further deductions do.
    /// </summary>
    public IObservable<Step> Steps => _grid.Steps;

    /// <summary>All 81 cells, row by row.</summary>
    public IEnumerable<Cell> Cells => _grid.Cells;

    public Cell Cell(int row, int column) => _grid.Cell(row, column);

    /// <summary>The player places <paramref name="digit"/> in a cell. Only an accepted move enters the move history.</summary>
    public MoveOutcome Move(int row, int column, int digit)
    {
        // The cascade runs to completion inside Move, so a subscription for its duration sees exactly its steps.
        var deductions = 0;
        MoveOutcome outcome;
        using (_grid.Steps.Subscribe(step => deductions += step is Step.Placement { Source: PlacementSource.Deduction } ? 1 : 0))
        {
            outcome = _grid.Move(row, column, digit);
        }

        if (outcome is MoveOutcome.Accepted)
        {
            _moves.Add(new RecordedMove(row, column, digit, deductions));
        }

        return outcome;
    }
}
