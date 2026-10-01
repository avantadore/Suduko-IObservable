using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SudokuObservable.Core;

/// <summary>
/// Where the cells and units of one game publish their steps, and whether the game has reached a contradiction.
/// It only passes steps on to subscribers and keeps none of them. A contradiction is published once, as a step:
/// never as OnError, which would end every subscription (ADR 0001).
/// </summary>
internal sealed class StepStream
{
    private readonly Subject<Step> _steps = new();

    public IObservable<Step> Steps => _steps.AsObservable();

    public bool IsContradicted { get; private set; }

    public void Publish(Step step) => _steps.OnNext(step);

    public void Contradict(Step.Contradiction contradiction)
    {
        if (!IsContradicted)
        {
            IsContradicted = true;
            Publish(contradiction);
        }
    }
}
