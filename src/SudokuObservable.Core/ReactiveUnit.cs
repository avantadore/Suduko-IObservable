using System.Reactive.Concurrency;

namespace SudokuObservable.Core;

/// <summary>
/// A row, column or box. It watches its nine cells lose candidates, and when a digit is left with only one
/// possible cell, an empty one, it deduces the digit there: a hidden single.
/// </summary>
internal sealed class ReactiveUnit
{
    private readonly IReadOnlyList<ReactiveCell> _cells;

    public ReactiveUnit(IReadOnlyList<ReactiveCell> cells)
    {
        _cells = cells;
        foreach (var cell in cells)
        {
            cell.LostCandidates.Subscribe(OnCandidateLost);
        }
    }

    private void OnCandidateLost(int digit)
    {
        // A filled cell keeps its digit as its candidate, so a unit that already holds the digit has a holder.
        if (_cells.Where(cell => cell.HasCandidate(digit)).Take(2).ToList() is [{ Digit: null } only])
        {
            Scheduler.CurrentThread.Schedule(() => only.PlaceDeduction(digit));
        }
    }
}
