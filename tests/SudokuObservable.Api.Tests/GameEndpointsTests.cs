using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SudokuObservable.Api.Tests;

public class GameEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_a_game_returns_201_with_its_location_and_grid()
    {
        var response = await _client.PostAsync("/games", content: null, Cancellation);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var grid = await ReadJson(response);
        var id = grid.GetProperty("id").GetString();
        Assert.Equal($"/games/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("InProgress", grid.GetProperty("state").GetString());
        AssertGridShape(grid);
    }

    [Fact]
    public async Task A_created_game_can_be_read_back_by_its_id()
    {
        var id = await CreateGame();

        var response = await _client.GetAsync($"/games/{id}", Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grid = await ReadJson(response);
        Assert.Equal(id, grid.GetProperty("id").GetString());
        AssertGridShape(grid);
    }

    [Fact]
    public async Task Reading_an_unknown_game_returns_404()
    {
        var response = await _client.GetAsync($"/games/{Guid.NewGuid()}", Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reading_a_cells_candidates_in_a_new_game_returns_1_to_9()
    {
        var id = await CreateGame();

        var response = await _client.GetAsync($"/games/{id}/cells/4/7/candidates", Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], DigitsOf(await ReadJson(response)));
    }

    [Fact]
    public async Task Reading_a_filled_cells_candidates_returns_its_digit()
    {
        var id = await CreateGame();
        await Move(id, 4, 7, 3);

        var response = await _client.GetAsync($"/games/{id}/cells/4/7/candidates", Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([3], DigitsOf(await ReadJson(response)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 10)]
    public async Task Reading_candidates_outside_rows_and_columns_1_to_9_returns_400(int row, int column)
    {
        var id = await CreateGame();

        var response = await _client.GetAsync($"/games/{id}/cells/{row}/{column}/candidates", Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetails(response);
    }

    [Fact]
    public async Task Reading_candidates_of_an_unknown_game_returns_404()
    {
        var response = await _client.GetAsync($"/games/{Guid.NewGuid()}/cells/1/1/candidates", Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_move_returns_200_with_the_updated_grid()
    {
        var id = await CreateGame();

        var response = await Move(id, 2, 3, 8);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grid = await ReadJson(response);
        Assert.Equal(id, grid.GetProperty("id").GetString());
        AssertGridShape(grid);
        var cell = CellOf(grid, 2, 3);
        Assert.Equal(8, cell.GetProperty("digit").GetInt32());
        Assert.Equal("Move", cell.GetProperty("source").GetString());
    }

    [Fact]
    public async Task A_deduced_cell_reports_its_source_as_deduction()
    {
        var id = await CreateGame();
        for (var digit = 1; digit <= 7; digit++)
        {
            await Move(id, 1, digit, digit);
        }

        // Leaves (1,9) with a single candidate, which the game places itself.
        var response = await Move(id, 1, 8, 8);

        var grid = await ReadJson(response);
        Assert.Equal("Move", CellOf(grid, 1, 8).GetProperty("source").GetString());
        Assert.Equal("Deduction", CellOf(grid, 1, 9).GetProperty("source").GetString());
    }

    [Fact]
    public async Task Repeating_a_move_returns_200_with_the_unchanged_grid()
    {
        var id = await CreateGame();
        var first = await ReadJson(await Move(id, 2, 3, 8));

        var response = await Move(id, 2, 3, 8);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(first.GetRawText(), (await ReadJson(response)).GetRawText());
    }

    [Fact]
    public async Task A_rejected_move_returns_409_with_the_reason()
    {
        var id = await CreateGame();
        await Move(id, 2, 3, 8);

        var response = await Move(id, 2, 3, 9);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await AssertProblemDetails(response);
        Assert.Contains("already holds 8", problem.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(10, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 10, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 10)]
    public async Task A_move_outside_1_to_9_returns_400(int row, int column, int digit)
    {
        var id = await CreateGame();

        var response = await Move(id, row, column, digit);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetails(response);
    }

    [Fact]
    public async Task A_move_in_an_unknown_game_returns_404()
    {
        var response = await Move(Guid.NewGuid().ToString(), 1, 1, 1);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<string> CreateGame()
    {
        var response = await _client.PostAsync("/games", content: null, Cancellation);
        response.EnsureSuccessStatusCode();
        return (await ReadJson(response)).GetProperty("id").GetString()!;
    }

    private Task<HttpResponseMessage> Move(string id, int row, int column, int digit) =>
        _client.PutAsJsonAsync($"/games/{id}/cells/{row}/{column}", new { digit }, Cancellation);

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(Cancellation);
        return JsonDocument.Parse(json).RootElement;
    }

    private static async Task<JsonElement> AssertProblemDetails(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJson(response);
        Assert.Equal((int)response.StatusCode, problem.GetProperty("status").GetInt32());
        return problem;
    }

    private static IEnumerable<int> DigitsOf(JsonElement array) => array.EnumerateArray().Select(digit => digit.GetInt32());

    private static JsonElement CellOf(JsonElement grid, int row, int column) =>
        grid.GetProperty("cells").EnumerateArray()
            .Single(cell => cell.GetProperty("row").GetInt32() == row && cell.GetProperty("column").GetInt32() == column);

    /// <summary>The grid representation: a flat list of 81 cells, each carrying its own coordinates.</summary>
    private static void AssertGridShape(JsonElement grid)
    {
        Assert.Equal(JsonValueKind.String, grid.GetProperty("state").ValueKind);

        var cells = grid.GetProperty("cells").EnumerateArray().ToList();
        Assert.Equal(81, cells.Count);
        Assert.Equal(81, cells.Select(cell => (cell.GetProperty("row").GetInt32(), cell.GetProperty("column").GetInt32())).Distinct().Count());
        Assert.All(cells, cell =>
        {
            Assert.InRange(cell.GetProperty("row").GetInt32(), 1, 9);
            Assert.InRange(cell.GetProperty("column").GetInt32(), 1, 9);
            Assert.Contains(cell.GetProperty("digit").ValueKind, new[] { JsonValueKind.Null, JsonValueKind.Number });
            Assert.Contains(cell.GetProperty("source").ValueKind, new[] { JsonValueKind.Null, JsonValueKind.String });
            Assert.All(cell.GetProperty("candidates").EnumerateArray(), digit => Assert.Equal(JsonValueKind.Number, digit.ValueKind));
        });
    }
}
