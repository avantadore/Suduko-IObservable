using System.Reactive.Linq;

namespace SudokuObservable.Core.Tests;

public class MoveTests
{
    [Fact]
    public void A_move_with_a_candidate_places_the_digit_as_a_move()
    {
        var game = Game.New();

        var outcome = game.Move(4, 7, 5);

        Assert.IsType<MoveOutcome.Accepted>(outcome);
        var cell = game.Cell(4, 7);
        Assert.Equal(5, cell.Digit);
        Assert.Equal(PlacementSource.Move, cell.Source);
        Assert.Equal([5], cell.Candidates);
    }

    [Fact]
    public void Every_peer_in_the_row_column_and_box_loses_the_digit_and_no_other_cell_does()
    {
        var game = Game.New();

        game.Move(5, 5, 7);

        (int Row, int Column)[] peersOf5x5 =
        [
            (5, 1), (5, 2), (5, 3), (5, 4), (5, 6), (5, 7), (5, 8), (5, 9), // row
            (1, 5), (2, 5), (3, 5), (4, 5), (6, 5), (7, 5), (8, 5), (9, 5), // column
            (4, 4), (4, 6), (6, 4), (6, 6),                                 // rest of the box
        ];
        var cellsWithout7 = game.Cells
            .Where(cell => cell.Digit is null && !cell.Candidates.Contains(7))
            .Select(cell => (cell.Row, cell.Column));
        Assert.Equal(peersOf5x5.Order(), cellsWithout7.Order());
        Assert.All(
            game.Cells.Where(cell => cell.Digit is null && !peersOf5x5.Contains((cell.Row, cell.Column))),
            cell => Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], cell.Candidates));
    }

    [Fact]
    public void A_move_with_a_digit_that_is_not_a_candidate_is_rejected_and_changes_nothing()
    {
        var game = Game.New();
        game.Move(1, 1, 5);
        var before = GridOf(game);

        var outcome = game.Move(1, 2, 5);

        var rejected = Assert.IsType<MoveOutcome.Rejected>(outcome);
        Assert.Contains("not a candidate", rejected.Reason);
        Assert.Equal(before, GridOf(game));
    }

    [Fact]
    public void A_different_digit_in_a_filled_cell_is_rejected_because_placements_are_final()
    {
        var game = Game.New();
        game.Move(1, 1, 5);
        var before = GridOf(game);

        var outcome = game.Move(1, 1, 6);

        var rejected = Assert.IsType<MoveOutcome.Rejected>(outcome);
        Assert.Contains("already holds 5", rejected.Reason);
        Assert.Equal(before, GridOf(game));
    }

    [Fact]
    public void The_same_digit_again_in_the_same_cell_is_unchanged()
    {
        var game = Game.New();
        game.Move(1, 1, 5);
        var before = GridOf(game);

        var outcome = game.Move(1, 1, 5);

        Assert.IsType<MoveOutcome.Unchanged>(outcome);
        Assert.Equal(before, GridOf(game));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void A_digit_outside_1_to_9_does_not_exist(int digit)
    {
        var game = Game.New();

        Assert.Throws<ArgumentOutOfRangeException>(() => game.Move(1, 1, digit));
    }

    [Fact]
    public void A_move_cannot_be_made_while_another_moves_cascade_is_running()
    {
        var game = Game.New();
        Exception? fromInnerMove = null;
        game.Steps.Take(1).Subscribe(_ => fromInnerMove = Record.Exception(() => game.Move(9, 9, 1)));

        game.Move(1, 1, 5);

        Assert.IsType<InvalidOperationException>(fromInnerMove);
        Assert.Null(game.Cell(9, 9).Digit);
    }

    private static List<string> GridOf(Game game) =>
        [.. game.Cells.Select(cell => $"{cell.Row},{cell.Column}: {cell.Digit} {cell.Source} [{string.Join("", cell.Candidates)}]")];
}
