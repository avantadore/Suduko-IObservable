namespace SudokuObservable.Core.Tests;

public class GameStateTests
{
    [Fact]
    public void A_cell_left_with_no_candidates_puts_the_game_in_contradiction_after_applying_the_move()
    {
        var game = Game.New();
        // Rows 1 and 9 hold 1–7 in columns 1–7, so (1,8), (1,9), (9,8) and (9,9) are left with 8 and 9.
        for (var column = 1; column <= 7; column++)
        {
            game.Move(1, column, column);
            game.Move(9, column, column % 7 + 1);
        }

        var steps = Record(game, out var ended);

        // 9 in column 9 leaves (1,9) and (9,9) both with only 8. (1,9) is deduced 8 first, which leaves (9,9)
        // with no candidates at all.
        var outcome = game.Move(5, 9, 9);

        Assert.IsType<MoveOutcome.Accepted>(outcome);
        Assert.Equal(9, game.Cell(5, 9).Digit);
        Assert.Equal(GameState.Contradicted, game.State);
        Assert.Equal(new Step.Contradiction.NoCandidateForCell(9, 9), Assert.Single(steps.OfType<Step.Contradiction>()));
        Assert.Empty(game.Cell(9, 9).Candidates);
        Assert.False(ended());
    }

    [Fact]
    public void A_digit_left_with_no_cell_in_a_unit_puts_the_game_in_contradiction_after_applying_the_move()
    {
        var game = Game.New();
        // Row 1 holds 1–7, so 9 can only go in (1,8) or (1,9), both in the top-right box.
        for (var digit = 1; digit <= 7; digit++)
        {
            game.Move(1, digit, digit);
        }

        var steps = Record(game, out var ended);

        var outcome = game.Move(2, 7, 9); // a legal move, but now 9 has no place in row 1

        Assert.IsType<MoveOutcome.Accepted>(outcome);
        Assert.Equal(9, game.Cell(2, 7).Digit);
        Assert.Equal(GameState.Contradicted, game.State);
        Assert.Equal(
            new Step.Contradiction.NoCellForDigit(UnitKind.Row, 1, 9),
            Assert.Single(steps.OfType<Step.Contradiction>()));
        Assert.False(ended());
    }

    [Fact]
    public void A_digit_left_with_no_cell_in_a_box_names_the_box_by_its_number_row_by_row()
    {
        var game = Game.New();
        // Box 4 (rows 4–6, columns 1–3) holds 1, 3, 7 and 9, and 8 in (1,2) rules out its column 2, so 8 can
        // only go in (4,1) or (6,1).
        game.Move(5, 3, 1);
        game.Move(5, 1, 7);
        game.Move(6, 3, 9);
        game.Move(4, 3, 3);
        game.Move(1, 2, 8);

        var steps = Record(game, out _);

        game.Move(9, 1, 8); // column 1 still has room for 8, but box 4 has none

        Assert.Equal(GameState.Contradicted, game.State);
        Assert.Equal(
            new Step.Contradiction.NoCellForDigit(UnitKind.Box, 4, 8),
            Assert.Single(steps.OfType<Step.Contradiction>()));
    }

    [Fact]
    public void Every_move_in_a_contradicted_game_is_rejected_and_the_grid_stays_readable()
    {
        var game = Game.New();
        for (var digit = 1; digit <= 7; digit++)
        {
            game.Move(1, digit, digit);
        }

        game.Move(2, 7, 9);
        var steps = Record(game, out _);

        var elsewhere = game.Move(5, 5, 5);
        var repeated = game.Move(2, 7, 9);

        Assert.Contains("contradiction", Assert.IsType<MoveOutcome.Rejected>(elsewhere).Reason);
        Assert.IsType<MoveOutcome.Rejected>(repeated);
        Assert.Empty(steps);
        Assert.Null(game.Cell(5, 5).Digit);
        Assert.Equal(81, game.Cells.Count());
    }

    [Fact]
    public void A_game_is_in_progress_while_cells_are_empty_and_solved_once_all_81_are_filled()
    {
        var game = Game.New();
        var givens = Puzzle.Givens.ToList();

        game.Move(givens[0].Row, givens[0].Column, givens[0].Digit);
        Assert.Equal(GameState.InProgress, game.State);

        foreach (var (row, column, digit) in givens.Skip(1))
        {
            game.Move(row, column, digit);
        }

        Assert.Equal(GameState.Solved, game.State);
        Assert.All(game.Cells, cell => Assert.NotNull(cell.Digit));
    }

    private static List<Step> Record(Game game, out Func<bool> ended)
    {
        var steps = new List<Step>();
        var hasEnded = false;
        game.Steps.Subscribe(steps.Add, _ => hasEnded = true, () => hasEnded = true);
        ended = () => hasEnded;
        return steps;
    }
}
