using System.Net.Http.Json;

namespace SudokuObservable.Web;

/// <summary>Talks to the Sudoku API, resolved through Aspire service discovery.</summary>
public sealed class SudokuApi(HttpClient http)
{
    public async Task<Grid> NewGameAsync(CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsync("/games", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Grid>(cancellationToken))!;
    }
}

public sealed record Grid(Guid Id, string State, IReadOnlyList<GridCell> Cells);

public sealed record GridCell(int Row, int Column, int? Digit, string? Source, IReadOnlyList<int> Candidates);
