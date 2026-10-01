using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SudokuObservable.Core;

namespace SudokuObservable.Api;

/// <summary>
/// Holds games in memory for the lifetime of the process. A game is not thread-safe,
/// so the store hands one out only while holding that game's lock.
/// </summary>
public sealed class GameStore
{
    private readonly ConcurrentDictionary<Guid, Entry> _games = new();

    /// <summary>Stores a game under a new id, running <paramref name="use"/> on it before any other request can reach it.</summary>
    public T Add<T>(Game game, Func<Guid, Game, T> use)
    {
        var id = Guid.NewGuid();
        var result = use(id, game);
        _games[id] = new Entry(game);
        return result;
    }

    /// <summary>Runs <paramref name="use"/> on the game while holding its lock, or returns false if there is no such game.</summary>
    public bool TryUse<T>(Guid id, Func<Game, T> use, [MaybeNullWhen(false)] out T result)
    {
        if (_games.GetValueOrDefault(id) is not { } entry)
        {
            result = default;
            return false;
        }

        lock (entry.Lock)
        {
            result = use(entry.Game);
            return true;
        }
    }

    private sealed record Entry(Game Game)
    {
        public Lock Lock { get; } = new();
    }
}
