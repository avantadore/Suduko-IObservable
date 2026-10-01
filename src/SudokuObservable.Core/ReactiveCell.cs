using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SudokuObservable.Core;

/// <summary>The live state of one cell. Its placements are observable, so its peers can subscribe and eliminate.</summary>
internal sealed class ReactiveCell(int row, int column, IObserver<Step> steps)
{
    private readonly SortedSet<int> _candidates = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    private readonly Subject<int> _placements = new();

    public int Row { get; } = row;

    public int Column { get; } = column;

    public int Box => (Row - 1) / 3 * 3 + (Column - 1) / 3;

    public int? Digit { get; private set; }

    public PlacementSource? Source { get; private set; }

    /// <summary>The digit placed in this cell, emitted once when it is placed.</summary>
    public IObservable<int> Placements => _placements.AsObservable();

    public bool IsPeerOf(ReactiveCell other) =>
        other != this && (other.Row == Row || other.Column == Column || other.Box == Box);

    public bool HasCandidate(int digit) => _candidates.Contains(digit);

    public void Place(int digit, PlacementSource source)
    {
        Digit = digit;
        Source = source;
        _candidates.IntersectWith([digit]);
        steps.OnNext(new Step.Placement(Row, Column, digit, source));
        _placements.OnNext(digit);
    }

    public void Eliminate(int digit)
    {
        if (_candidates.Remove(digit))
        {
            steps.OnNext(new Step.Elimination(Row, Column, digit));

            if (Digit is null && _candidates.Count == 1)
            {
                // A naked single. Queued on the trampoline rather than placed now, so the placement that
                // forced it finishes its eliminations first and the cascade unfolds breadth-first.
                Scheduler.CurrentThread.Schedule(PlaceNakedSingle);
            }
        }
    }

    private void PlaceNakedSingle()
    {
        if (Digit is null && _candidates.Count == 1)
        {
            Place(_candidates.Min, PlacementSource.Deduction);
        }
    }

    public Cell Snapshot() => new(Row, Column, Digit, Source, [.. _candidates]);
}
