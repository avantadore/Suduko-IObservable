using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SudokuObservable.Core;

/// <summary>
/// The live state of one cell. Its placements are observable, so its peers can subscribe and eliminate, and so
/// are the candidates it loses, so its units can look for hidden singles.
/// </summary>
internal sealed class ReactiveCell(int row, int column, StepStream steps)
{
    private readonly SortedSet<int> _candidates = [1, 2, 3, 4, 5, 6, 7, 8, 9];
    private readonly Subject<int> _placements = new();
    private readonly Subject<int> _lostCandidates = new();

    public int Row { get; } = row;

    public int Column { get; } = column;

    /// <summary>The box this cell belongs to, 0–8 row by row.</summary>
    public int BoxIndex => (Row - 1) / 3 * 3 + (Column - 1) / 3;

    public int? Digit { get; private set; }

    public PlacementSource? Source { get; private set; }

    /// <summary>The digit placed in this cell, emitted once when it is placed.</summary>
    public IObservable<int> Placements => _placements.AsObservable();

    /// <summary>Each candidate this cell loses, by elimination or because the cell was filled.</summary>
    public IObservable<int> LostCandidates => _lostCandidates.AsObservable();

    public bool IsPeerOf(ReactiveCell other) =>
        other != this && (other.Row == Row || other.Column == Column || other.BoxIndex == BoxIndex);

    public bool HasCandidate(int digit) => _candidates.Contains(digit);

    public void Place(int digit, PlacementSource source)
    {
        var lost = _candidates.Where(candidate => candidate != digit).ToList();
        Digit = digit;
        Source = source;
        _candidates.IntersectWith([digit]);
        steps.Publish(new Step.Placement(Row, Column, digit, source));
        _placements.OnNext(digit);
        lost.ForEach(_lostCandidates.OnNext);
    }

    public void Eliminate(int digit)
    {
        if (_candidates.Remove(digit))
        {
            steps.Publish(new Step.Elimination(Row, Column, digit));

            if (Digit is null && _candidates.Count == 1)
            {
                Deduce(_candidates.Min); // a naked single
            }
            else if (Digit is null && _candidates.Count == 0)
            {
                steps.Contradict(new Step.Contradiction.NoCandidateForCell(Row, Column));
            }

            _lostCandidates.OnNext(digit);
        }
    }

    /// <summary>
    /// Deduces <paramref name="digit"/> for this cell. The placement is queued on the trampoline rather than made
    /// now, so the placement that forced it finishes its eliminations first and the cascade unfolds breadth-first.
    /// </summary>
    public void Deduce(int digit) => Scheduler.CurrentThread.Schedule(() => PlaceDeduction(digit));

    // By the time a queued deduction runs, the cascade may have filled the cell, removed the digit or reached a
    // contradiction, after which nothing more is deduced.
    private void PlaceDeduction(int digit)
    {
        if (!steps.IsContradicted && Digit is null && HasCandidate(digit))
        {
            Place(digit, PlacementSource.Deduction);
        }
    }

    public Cell Snapshot() => new(Row, Column, Digit, Source, [.. _candidates]);
}
