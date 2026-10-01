namespace SudokuObservable.Core;

/// <summary>
/// A row, column or box. It watches its nine cells lose candidates. When a digit is left with only one possible
/// cell, an empty one, it deduces the digit there: a hidden single. When a digit is left with no possible cell,
/// the grid is in contradiction.
/// </summary>
internal sealed class ReactiveUnit
{
    private readonly UnitKind _kind;
    private readonly int _number;
    private readonly IReadOnlyList<ReactiveCell> _cells;
    private readonly StepStream _steps;

    public ReactiveUnit(UnitKind kind, int number, IReadOnlyList<ReactiveCell> cells, StepStream steps)
    {
        _kind = kind;
        _number = number;
        _cells = cells;
        _steps = steps;
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
                _steps.Contradict(new Step.Contradiction.NoCellForDigit(_kind, _number, digit));
                break;
            case [{ Digit: null } only]:
                only.Deduce(digit);
                break;
        }
    }
}
