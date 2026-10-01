using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;

namespace SudokuObservable.Core;

/// <summary>A session in which one 9×9 grid is filled in. Every game starts with all 81 cells empty.</summary>
public sealed class Game
{
    private readonly Subject<Step> _steps = new();
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
    }

    public static Game New() => new();

    public GameState State => GameState.InProgress;

    /// <summary>Everything that happens to the grid: each move, followed by the steps of its cascade.</summary>
    public IObservable<Step> Steps => _steps.AsObservable();

    /// <summary>All 81 cells, row by row.</summary>
    public IEnumerable<Cell> Cells => _cells.Cast<ReactiveCell>().Select(cell => cell.Snapshot());

    public Cell Cell(int row, int column) => At(row, column).Snapshot();

    /// <summary>The player places <paramref name="digit"/> in a cell.</summary>
    public MoveOutcome Move(int row, int column, int digit)
    {
        ThrowIfOutside1To9(digit);
        var cell = At(row, column);

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

        // The move runs on Rx's current-thread trampoline, so every deduction its cascade forces is queued
        // and placed in order before Schedule returns.
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
