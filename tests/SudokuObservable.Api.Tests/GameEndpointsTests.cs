using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SudokuObservable.Api.Tests;

public class GameEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly int[] AllDigits = [1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Creating_a_game_returns_201_with_its_location_and_grid()
    {
        var response = await _client.PostAsync("/games", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var grid = await ReadJson(response);
        var id = grid.GetProperty("id").GetString();
        Assert.Equal($"/games/{id}", response.Headers.Location?.OriginalString);
        AssertEmptyGrid(grid);
    }

    [Fact]
    public async Task A_created_game_can_be_read_back_by_its_id()
    {
        var id = await CreateGame();

        var response = await _client.GetAsync($"/games/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grid = await ReadJson(response);
        Assert.Equal(id, grid.GetProperty("id").GetString());
        AssertEmptyGrid(grid);
    }

    [Fact]
    public async Task Reading_an_unknown_game_returns_404()
    {
        var response = await _client.GetAsync($"/games/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reading_a_cells_candidates_returns_its_digits()
    {
        var id = await CreateGame();

        var response = await _client.GetAsync($"/games/{id}/cells/4/7/candidates", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var candidates = await ReadJson(response);
        Assert.Equal(AllDigits, candidates.EnumerateArray().Select(digit => digit.GetInt32()));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 10)]
    public async Task Reading_candidates_outside_rows_and_columns_1_to_9_returns_400(int row, int column)
    {
        var id = await CreateGame();

        var response = await _client.GetAsync($"/games/{id}/cells/{row}/{column}/candidates", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reading_candidates_of_an_unknown_game_returns_404()
    {
        var response = await _client.GetAsync($"/games/{Guid.NewGuid()}/cells/1/1/candidates", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<string> CreateGame()
    {
        var response = await _client.PostAsync("/games", content: null, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await ReadJson(response)).GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement;
    }

    private static void AssertEmptyGrid(JsonElement grid)
    {
        Assert.Equal("InProgress", grid.GetProperty("state").GetString());

        var cells = grid.GetProperty("cells").EnumerateArray().ToList();
        Assert.Equal(81, cells.Count);
        Assert.Equal(81, cells.Select(cell => (cell.GetProperty("row").GetInt32(), cell.GetProperty("column").GetInt32())).Distinct().Count());
        Assert.All(cells, cell =>
        {
            Assert.InRange(cell.GetProperty("row").GetInt32(), 1, 9);
            Assert.InRange(cell.GetProperty("column").GetInt32(), 1, 9);
            Assert.Equal(JsonValueKind.Null, cell.GetProperty("digit").ValueKind);
            Assert.Equal(JsonValueKind.Null, cell.GetProperty("source").ValueKind);
            Assert.Equal(AllDigits, cell.GetProperty("candidates").EnumerateArray().Select(digit => digit.GetInt32()));
        });
    }
}
