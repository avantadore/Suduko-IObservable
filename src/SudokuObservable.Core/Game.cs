using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;

namespace SudokuObservable.Core;

/// <summary>A session in which one 9×9 grid is filled in. Every game starts with all 81 cells empty.</summary>
public sealed class Game
{
    private readonly StepStream _steps = new();
    private readonly ReactiveCell[,] _cells = new ReactiveCell[9, 9];

    private Game()
    {
        for (var row = 1; row <= 9; row++)
        {
            for (var column = 1; column <= 9; column++)
            {
                _cells[row - 1, column - 1] = new ReactiveCell(row, column, _steps);
            }
        }

        // Every cell subscribes to its peers' placements. Cells subscribe row by row, so each placement
        // reaches its peers, and its eliminations happen, in row-by-row order.
        var cells = _cells.Cast<ReactiveCell>().ToList();
        foreach (var cell in cells)
        {
            foreach (var peer in cells.Where(cell.IsPeerOf))
            {
                peer.Placements.Subscribe(cell.Eliminate);
            }
        }

        // The rows, then the columns, then the boxes watch their cells for hidden singles and contradictions. The
        // units are kept alive by their subscriptions to the cells.
        var units = cells.GroupBy(cell => (Kind: UnitKind.Row, Number: cell.Row))
            .Concat(cells.GroupBy(cell => (Kind: UnitKind.Column, Number: cell.Column)))
            .Concat(cells.GroupBy(cell => (Kind: UnitKind.Box, Number: cell.BoxIndex + 1)));
        foreach (var unit in units)
        {
            _ = new ReactiveUnit(unit.Key.Kind, unit.Key.Number, [.. unit], _steps);
        }
    }

    public static Game New() => new();

    public GameState State =>
        _steps.IsContradicted ? GameState.Contradicted
        : _cells.Cast<ReactiveCell>().All(cell => cell.Digit is not null) ? GameState.Solved
        : GameState.InProgress;

    /// <summary>
    /// Everything that happens to the grid: each move, followed by the steps of its cascade. A contradiction is a
    /// step too, so the stream never errors or completes. It is not necessarily the last step of its move: the
    /// eliminations of the placement that reached it still follow, but no further deductions do.
    /// </summary>
    public IObservable<Step> Steps => _steps.Steps;

    /// <summary>All 81 cells, row by row.</summary>
    public IEnumerable<Cell> Cells => _cells.Cast<ReactiveCell>().Select(cell => cell.Snapshot());

    public Cell Cell(int row, int column) => At(row, column).Snapshot();

    /// <summary>The player places <paramref name="digit"/> in a cell.</summary>
    public MoveOutcome Move(int row, int column, int digit)
    {
        ThrowIfOutside1To9(digit);
        var cell = At(row, column);

        // Inside a running trampoline (a cascade, e.g. a Steps subscriber reacting to a step), the move
        // would only be queued: it would return before the digit is placed, and the cascade could
        // invalidate it first.
        if (!CurrentThreadScheduler.IsScheduleRequired)
        {
            throw new InvalidOperationException("A move cannot be made while another move's cascade is running.");
        }

        if (_steps.IsContradicted)
        {
            return new MoveOutcome.Rejected(
                "The game is in contradiction, so no more moves can be made. Start a new game.");
        }

        if (cell.Digit == digit)
        {
            return new MoveOutcome.Unchanged();
        }

        if (cell.Digit is { } placed)
        {
            return new MoveOutcome.Rejected($"Cell ({row}, {column}) already holds {placed}, and placements are final.");
        }

        if (!cell.HasCandidate(digit))
        {
            return new MoveOutcome.Rejected($"{digit} is not a candidate for cell ({row}, {column}).");
        }

        // The move starts Rx's current-thread trampoline, so every deduction its cascade forces is queued
        // and placed in order before Schedule returns. (Move refuses to run inside a trampoline, see above.)
        Scheduler.CurrentThread.Schedule(() => cell.Place(digit, PlacementSource.Move));
        return new MoveOutcome.Accepted();
    }

    private ReactiveCell At(int row, int column)
    {
        ThrowIfOutside1To9(row);
        ThrowIfOutside1To9(column);
        return _cells[row - 1, column - 1];
    }

    private static void ThrowIfOutside1To9(int value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 9, name);
    }
}
