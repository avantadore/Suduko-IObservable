namespace SudokuObservable.Web.Tests;

public class HistoryDisplayTests
{
    [Fact]
    public void A_new_game_has_only_the_empty_grid_row_and_it_is_current()
    {
        var row = Assert.Single(HistoryDisplay.Rows(position: 0, moves: []));

        Assert.Equal(new HistoryRow(0, "Empty grid", null, null, HistoryRowState.Current), row);
    }

    [Fact]
    public void Each_move_is_numbered_with_its_cell_and_digit_and_a_suffix_only_when_it_made_deductions()
    {
        GridMove[] moves = [new(1, 1, 5, 0), new(2, 3, 8, 1), new(4, 7, 2, 5)];

        var rows = HistoryDisplay.Rows(position: 3, moves);

        HistoryRow[] expected =
        [
            new(0, "Empty grid", null, null, HistoryRowState.Applied),
            new(1, "1. r1c1 = 5", null, null, HistoryRowState.Applied),
            new(2, "2. r2c3 = 8", "(+1)", "1 deduction", HistoryRowState.Applied),
            new(3, "3. r4c7 = 2", "(+5)", "5 deductions", HistoryRowState.Current),
        ];
        Assert.Equal(expected, rows);
    }

    // The classes are the ones MoveHistory.razor.css styles.
    [Theory]
    [InlineData(HistoryRowState.Applied, "applied")]
    [InlineData(HistoryRowState.Current, "current")]
    public void A_history_row_state_is_shown_with_its_class(HistoryRowState state, string cssClass) =>
        Assert.Equal(cssClass, state.CssClass());
}
