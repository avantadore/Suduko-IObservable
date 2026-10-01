namespace SudokuObservable.Core;

/// <summary>
/// A row, column or box. It watches its nine cells lose candidates. When a digit is left with only one possible
/// cell, an empty one, it deduces the digit there: a hidden single. When a digit is left with no possible cell,
/// the grid is in contradiction.
/// </summary>
internal sealed class ReactiveUnit
{
    private readonly string _name;
    private readonly IReadOnlyList<ReactiveCell> _cells;
    private readonly StepLog _log;

    public ReactiveUnit(string name, IReadOnlyList<ReactiveCell> cells, StepLog log)
    {
        _name = name;
        _cells = cells;
        _log = log;
        foreach (var cell in cells)
        {
            cell.LostCandidates.Subscribe(OnCandidateLost);
        }
    }

    private void OnCandidateLost(int digit)
    {
        // A filled cell keeps its digit as its candidate, so a unit that already holds the digit has a holder.
        switch (_cells.Where(cell => cell.HasCandidate(digit)).Take(2).ToList())
        {
            case []:
                _log.Contradict($"{digit} has no possible cell left in {_name}.");
                break;
            case [{ Digit: null } only]:
                only.Deduce(digit);
                break;
        }
    }
}
