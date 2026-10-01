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
        games.MapGet("/{id:guid}/cells/{row:int}/{column:int}/candidates", GetCandidates);

        return app;
    }

    private static Created<GridResponse> CreateGame(GameStore store)
    {
        var game = Game.New();
        var id = store.Add(game);
        return TypedResults.Created($"/games/{id}", GridResponse.From(id, game));
    }

    private static Results<Ok<GridResponse>, NotFound> GetGame(Guid id, GameStore store) =>
        store.Find(id) is { } game
            ? TypedResults.Ok(GridResponse.From(id, game))
            : TypedResults.NotFound();

    private static Results<Ok<IReadOnlyList<int>>, ValidationProblem, NotFound> GetCandidates(
        Guid id, int row, int column, GameStore store)
    {
        var errors = ValidateCoordinates(row, column);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        return store.Find(id) is { } game
            ? TypedResults.Ok(game.Cell(row, column).Candidates)
            : TypedResults.NotFound();
    }

    private static Dictionary<string, string[]> ValidateCoordinates(int row, int column)
    {
        var errors = new Dictionary<string, string[]>();
        if (row is < 1 or > 9)
        {
            errors[nameof(row)] = ["Row must be between 1 and 9."];
        }

        if (column is < 1 or > 9)
        {
            errors[nameof(column)] = ["Column must be between 1 and 9."];
        }

        return errors;
    }
}
