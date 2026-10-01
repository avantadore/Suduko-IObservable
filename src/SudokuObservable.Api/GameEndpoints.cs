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
        var grid = store.Add(Game.New(), GridResponse.From);
        return TypedResults.Created($"/games/{grid.Id}", grid);
    }

    private static Results<Ok<GridResponse>, NotFound> GetGame(Guid id, GameStore store) =>
        store.TryUse(id, game => GridResponse.From(id, game), out var grid)
            ? TypedResults.Ok(grid)
            : TypedResults.NotFound();

    private static Results<Ok<GridResponse>, ProblemHttpResult, ValidationProblem, NotFound> MakeMove(
        Guid id, int row, int column, MoveRequest move, GameStore store)
    {
        var errors = ValidateOneToNine(("Row", row), ("Column", column), ("Digit", move.Digit));
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (!store.TryUse(id, game => (Outcome: game.Move(row, column, move.Digit), Grid: GridResponse.From(id, game)), out var played))
        {
            return TypedResults.NotFound();
        }

        return played.Outcome switch
        {
            MoveOutcome.Rejected rejected => TypedResults.Problem(
                title: "Move rejected", detail: rejected.Reason, statusCode: StatusCodes.Status409Conflict),
            _ => TypedResults.Ok(played.Grid),
        };
    }

    private static Results<Ok<IReadOnlyList<int>>, ValidationProblem, NotFound> GetCandidates(
        Guid id, int row, int column, GameStore store)
    {
        var errors = ValidateOneToNine(("Row", row), ("Column", column));
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        return store.TryUse(id, game => game.Cell(row, column).Candidates, out var candidates)
            ? TypedResults.Ok(candidates)
            : TypedResults.NotFound();
    }

    /// <summary>Rows, columns and digits are all 1–9.</summary>
    private static Dictionary<string, string[]> ValidateOneToNine(params (string Name, int Value)[] values) =>
        values
            .Where(value => value.Value is < 1 or > 9)
            .ToDictionary(value => value.Name, value => new[] { $"{value.Name} must be between 1 and 9." });
}

public sealed record MoveRequest(int Digit);
