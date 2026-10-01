namespace SudokuObservable.Core.Tests;

public class CascadeTests
{
    // The example puzzle and its solution from https://en.wikipedia.org/wiki/Sudoku. It needs only naked and
    // hidden singles, but most of its cells are forced only by earlier deductions.
    private static readonly string[] Givens =
    [
        "53..7....",
        "6..195...",
        ".98....6.",
        "8...6...3",
        "4..8.3..1",
        "7...2...6",
        ".6....28.",
        "...419..5",
        "....8..79",
    ];

    private static readonly string[] Solution =
    [
        "534678912",
        "672195348",
        "198342567",
        "859761423",
        "426853791",
        "713924856",
        "961537284",
        "287419635",
        "345286179",
    ];

    [Fact]
    public void Deductions_force_further_deductions_until_the_givens_of_a_puzzle_solve_it()
    {
        var game = Game.New();

        foreach (var (row, column, digit) in Cells(Givens))
        {
            // A given may already have been deduced from earlier givens, which leaves the move unchanged.
            Assert.IsNotType<MoveOutcome.Rejected>(game.Move(row, column, digit));
        }

        foreach (var (row, column, digit) in Cells(Solution))
        {
            var cell = game.Cell(row, column);
            Assert.Equal(digit, cell.Digit);
            if (Givens[row - 1][column - 1] == '.')
            {
                Assert.Equal(PlacementSource.Deduction, cell.Source);
            }
        }
    }

    private static IEnumerable<(int Row, int Column, int Digit)> Cells(string[] rows) =>
        from row in Enumerable.Range(1, 9)
        from column in Enumerable.Range(1, 9)
        let character = rows[row - 1][column - 1]
        where character != '.'
        select (row, column, character - '0');
}
