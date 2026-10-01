using System.Collections.Concurrent;
using SudokuObservable.Core;

namespace SudokuObservable.Api;

/// <summary>Holds games in memory for the lifetime of the process.</summary>
public sealed class GameStore
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();

    public Guid Add(Game game)
    {
        var id = Guid.NewGuid();
        _games[id] = game;
        return id;
    }

    public Game? Find(Guid id) => _games.GetValueOrDefault(id);
}
