using System.Net;
using System.Text;
using System.Text.Json;

namespace SudokuObservable.Web.Tests;

public class SudokuApiTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_grids_state_and_sources_are_read_into_enums()
    {
        var api = ApiReturning(GridJson(state: "Solved", source: "\"Deduction\""));

        var grid = await api.NewGameAsync(Cancellation);

        Assert.Equal(GameState.Solved, grid.State);
        Assert.Equal(PlacementSource.Deduction, grid.Cells[0].Source);
        Assert.Null(grid.Cells[1].Source);
    }

    [Fact]
    public async Task A_grids_position_and_moves_are_read_including_moves_after_the_position()
    {
        var api = ApiReturning(GridJson(state: "InProgress", source: "\"Move\""));

        var grid = await api.NewGameAsync(Cancellation);

        Assert.Equal(1, grid.Position);
        Assert.Equal([new GridMove(1, 1, 5, 0), new GridMove(4, 7, 2, 3)], grid.Moves);
    }

    [Theory]
    [InlineData("Won", "\"Move\"")]
    [InlineData("1", "\"Move\"")]
    [InlineData("Solved", "\"Guess\"")]
    [InlineData("Solved", "0")]
    public async Task A_state_or_source_the_web_does_not_know_fails_loudly(string state, string source)
    {
        var api = ApiReturning(GridJson(state, source));

        await Assert.ThrowsAsync<JsonException>(() => api.NewGameAsync(Cancellation));
    }

    /// <summary>
    /// A grid of two cells: one filled from <paramref name="source"/> (raw JSON), one empty. It is at position 1 of
    /// two moves, so the second move is kept but not applied.
    /// </summary>
    private static string GridJson(string state, string source) =>
        $$"""
        {
          "id": "{{Guid.NewGuid()}}",
          "state": "{{state}}",
          "cells": [
            { "row": 1, "column": 1, "digit": 5, "source": {{source}}, "candidates": [5] },
            { "row": 1, "column": 2, "digit": null, "source": null, "candidates": [1, 2] }
          ],
          "position": 1,
          "moves": [
            { "row": 1, "column": 1, "digit": 5, "deductions": 0 },
            { "row": 4, "column": 7, "digit": 2, "deductions": 3 }
          ]
        }
        """;

    private static SudokuApi ApiReturning(string json) =>
        new(new HttpClient(new StubHandler(json)) { BaseAddress = new Uri("http://api") });

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }
}
