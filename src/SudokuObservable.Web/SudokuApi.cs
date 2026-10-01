using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace SudokuObservable.Web;

/// <summary>Talks to the Sudoku API, resolved through Aspire service discovery.</summary>
public sealed class SudokuApi(HttpClient http)
{
    public async Task<Grid> NewGameAsync(CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsync("/games", content: null, cancellationToken);
        return await ReadGridAsync(response, cancellationToken);
    }

    /// <summary>Places a digit. A rejected move returns the API's reason instead of a grid.</summary>
    public async Task<MoveResult> MoveAsync(Guid id, int row, int column, int digit, CancellationToken cancellationToken = default)
    {
        var response = await http.PutAsJsonAsync($"/games/{id}/cells/{row}/{column}", new { digit }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            return new MoveResult.Rejected(problem?.Detail ?? "The move was rejected.");
        }

        return new MoveResult.Accepted(await ReadGridAsync(response, cancellationToken));
    }

    private static async Task<Grid> ReadGridAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Grid>(cancellationToken)
            ?? throw new HttpRequestException("The API returned no grid.");
    }
}

public sealed record Grid(Guid Id, string State, IReadOnlyList<GridCell> Cells);

public sealed record GridCell(int Row, int Column, int? Digit, string? Source, IReadOnlyList<int> Candidates);

public abstract record MoveResult
{
    private MoveResult()
    {
    }

    public sealed record Accepted(Grid Grid) : MoveResult;

    public sealed record Rejected(string Reason) : MoveResult;
}
