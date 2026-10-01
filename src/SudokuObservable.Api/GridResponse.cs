using SudokuObservable.Core;

namespace SudokuObservable.Api;

public sealed record GridResponse(Guid Id, GameState State, IReadOnlyList<CellResponse> Cells)
{
    public static GridResponse From(Guid id, Game game) =>
        new(id, game.State, [.. game.Cells.Select(CellResponse.From)]);
}

public sealed record CellResponse(int Row, int Column, int? Digit, PlacementSource? Source, IReadOnlyList<int> Candidates)
{
    public static CellResponse From(Cell cell) =>
        new(cell.Row, cell.Column, cell.Digit, cell.Source, cell.Candidates);
}
