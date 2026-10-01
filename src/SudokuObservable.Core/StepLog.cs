using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SudokuObservable.Core;

/// <summary>
/// Where the cells and units of one game record their steps, and whether the game has reached a contradiction.
/// A contradiction is recorded once, as a step: never as OnError, which would end every subscription (ADR 0001).
/// </summary>
internal sealed class StepLog
{
    private readonly Subject<Step> _steps = new();

    public IObservable<Step> Steps => _steps.AsObservable();

    public bool IsContradicted { get; private set; }

    public void Record(Step step) => _steps.OnNext(step);

    public void Contradict(string reason)
    {
        if (!IsContradicted)
        {
            IsContradicted = true;
            Record(new Step.Contradiction(reason));
        }
    }
}
