using SudokuObservable.Core;

namespace SudokuObservable.Api.Tests;

public class GameStoreTests
{
    private readonly GameStore _store = new();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public void Using_an_unknown_game_reports_it_as_not_found()
    {
        var found = _store.TryUse(Guid.NewGuid(), game => game.State, out _);

        Assert.False(found);
    }

    [Fact]
    public void An_added_game_can_be_used_by_its_id_and_keeps_its_moves_between_uses()
    {
        var id = _store.Add(Game.New(), (id, _) => id);
        _store.TryUse(id, game => game.Move(4, 7, 3), out _);

        var found = _store.TryUse(id, game => game.Cell(4, 7).Digit, out var digit);

        Assert.True(found);
        Assert.Equal(3, digit);
    }

    [Fact]
    public async Task A_second_use_of_a_game_waits_until_the_first_has_finished()
    {
        var id = _store.Add(Game.New(), (id, _) => id);
        using var firstStarted = new ManualResetEventSlim();
        using var finishFirst = new ManualResetEventSlim();

        var first = Task.Run(() => _store.TryUse(id, _ =>
        {
            firstStarted.Set();
            return finishFirst.Wait(TimeSpan.FromSeconds(10));
        }, out _), Cancellation);
        firstStarted.Wait(Cancellation);
        var second = Task.Run(() => _store.TryUse(id, _ => true, out _), Cancellation);

        await Task.WhenAny(second, Task.Delay(TimeSpan.FromMilliseconds(200), Cancellation));
        var secondFinishedEarly = second.IsCompleted;
        finishFirst.Set();
        await Task.WhenAll(first, second);

        Assert.False(secondFinishedEarly);
    }
}
