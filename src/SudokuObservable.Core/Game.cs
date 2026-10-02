using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SudokuObservable.Core;

/// <summary>A session in which one 9×9 grid is filled in. Every game starts with all 81 cells empty.</summary>
public sealed class Game
{
    private readonly List<RecordedMove> _moves = [];
    private readonly Subject<Step> _steps = new();
    private readonly SerialDisposable _forwarding = new();
    private Grid _grid;

    private Game() => Use(new Grid());

    public static Game New() => new();

    public GameState State => _grid.State;

    /// <summary>The moves this game has accepted, in order.</summary>
    public IReadOnlyList<RecordedMove> Moves => _moves.AsReadOnly();

    /// <summary>How many moves from the start of <see cref="Moves"/> apply to the grid. 0 is the empty grid.</summary>
    public int Position { get; private set; }

    /// <summary>
    /// Everything that happens to the grid: each move, followed by the steps of its cascade. A contradiction is a
    /// step too, so the stream never errors or completes. It is not necessarily the last step of its move: the
    /// eliminations of the placement that reached it still follow, but no further deductions do. A replay emits
    /// nothing: subscribers stay subscribed and receive the steps of the moves made after it.
    /// </summary>
    public IObservable<Step> Steps => _steps.AsObservable();

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
            _moves.RemoveRange(Position, _moves.Count - Position);
            _moves.Add(new RecordedMove(row, column, digit, deductions));
            Position = _moves.Count;
        }

        return outcome;
    }

    /// <summary>Rebuilds the grid from the first <paramref name="position"/> moves, which becomes the game's position.</summary>
    public void ReplayTo(int position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(position, _moves.Count);

        var grid = new Grid();
        foreach (var move in _moves.Take(position))
        {
            // Propagation is deterministic (ADR 0002), so a recorded move is accepted again. If it ever isn't, the
            // grid would no longer match the history, so fail before the replayed grid is used.
            if (grid.Move(move.Row, move.Column, move.Digit) is not MoveOutcome.Accepted)
            {
                throw new InvalidOperationException($"Replaying {move} was not accepted, so the grid would not match the move history.");
            }
        }

        Use(grid);
        Position = position;
    }

    /// <summary>
    /// Makes a fully built grid current, so subscribers to <see cref="Steps"/> see only what happens to it from now on
    /// (ADR 0002).
    /// </summary>
    [MemberNotNull(nameof(_grid))]
    private void Use(Grid grid)
    {
        _grid = grid;
        _forwarding.Disposable = grid.Steps.Subscribe(_steps.OnNext);
    }
}
