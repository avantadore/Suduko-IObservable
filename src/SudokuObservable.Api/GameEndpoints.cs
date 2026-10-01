using Microsoft.AspNetCore.Http.HttpResults;
using SudokuObservable.Core;

namespace SudokuObservable.Api;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/games").WithTags("Games");

        games.MapPost("/", CreateGame);
        games.MapGet("/{id:guid}", GetGame);
        games.MapPut("/{id:guid}/cells/{row:int}/{column:int}", MakeMove);
        games.MapGet("/{id:guid}/cells/{row:int}/{column:int}/candidates", GetCandidates);

        return app;
    }

    private static Created<GridResponse> CreateGame(GameStore store)
    {
        var game = Game.New();
        var id = store.Add(game);
        return TypedResults.Created($"/games/{id}", GridResponse.From(id, game));
    }

    private static Results<Ok<GridResponse>, NotFound> GetGame(Guid id, GameStore store)
    {
        if (store.Find(id) is not { } game)
        {
            return TypedResults.NotFound();
        }

        // A game is not thread-safe, so every request that touches one holds its lock.
        lock (game)
        {
            return TypedResults.Ok(GridResponse.From(id, game));
        }
    }

    private static Results<Ok<GridResponse>, ProblemHttpResult, ValidationProblem, NotFound> MakeMove(
        Guid id, int row, int column, MoveRequest move, GameStore store)
    {
        var errors = ValidateDigits((nameof(row), row), (nameof(column), column), ("digit", move.Digit));
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (store.Find(id) is not { } game)
        {
            return TypedResults.NotFound();
        }

        lock (game)
        {
            return game.Move(row, column, move.Digit) switch
            {
                MoveOutcome.Rejected rejected => TypedResults.Problem(
                    title: "Move rejected", detail: rejected.Reason, statusCode: StatusCodes.Status409Conflict),
                _ => TypedResults.Ok(GridResponse.From(id, game)),
            };
        }
    }

    private static Results<Ok<IReadOnlyList<int>>, ValidationProblem, NotFound> GetCandidates(
        Guid id, int row, int column, GameStore store)
    {
        var errors = ValidateDigits((nameof(row), row), (nameof(column), column));
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (store.Find(id) is not { } game)
        {
            return TypedResults.NotFound();
        }

        lock (game)
        {
            return TypedResults.Ok(game.Cell(row, column).Candidates);
        }
    }

    /// <summary>Rows, columns and digits are all 1–9.</summary>
    private static Dictionary<string, string[]> ValidateDigits(params (string Name, int Value)[] values) =>
        values
            .Where(value => value.Value is < 1 or > 9)
            .ToDictionary(value => value.Name, value => new[] { $"{value.Name} must be between 1 and 9." });
}

public sealed record MoveRequest(int Digit);
