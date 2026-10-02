namespace SudokuObservable.Core.Tests;

public class MoveHistoryTests
{
    [Fact]
    public void A_new_game_has_no_moves_and_is_at_position_0()
    {
        var game = Game.New();

        Assert.Empty(game.Moves);
        Assert.Equal(0, game.Position);
    }

    [Fact]
    public void Accepted_moves_are_recorded_in_order_with_how_many_deductions_their_cascade_made()
    {
        var game = Game.New();
        for (var digit = 1; digit <= 7; digit++)
        {
            game.Move(1, digit, digit);
        }

        // Leaves (1,9) with only 9, which the game deduces.
        game.Move(1, 8, 8);

        RecordedMove[] expected =
        [
            new(1, 1, 1, 0), new(1, 2, 2, 0), new(1, 3, 3, 0), new(1, 4, 4, 0),
            new(1, 5, 5, 0), new(1, 6, 6, 0), new(1, 7, 7, 0), new(1, 8, 8, 1),
        ];
        Assert.Equal(expected, game.Moves);
        Assert.Equal(8, game.Position);
    }

    [Fact]
    public void Rejected_and_unchanged_moves_are_not_recorded()
    {
        var game = Game.New();
        game.Move(1, 1, 5);

        game.Move(1, 1, 5); // unchanged
        game.Move(1, 1, 6); // already filled
        game.Move(1, 2, 5); // not a candidate

        Assert.Equal([new RecordedMove(1, 1, 5, 0)], game.Moves);
        Assert.Equal(1, game.Position);
    }

    [Fact]
    public void A_move_into_contradiction_is_recorded_with_the_deductions_placed_before_its_cascade_stopped()
    {
        var game = Game.New();
        // Rows 1 and 9 hold 1–7 in columns 1–7, so (1,8), (1,9), (9,8) and (9,9) are left with 8 and 9.
        for (var column = 1; column <= 7; column++)
        {
            game.Move(1, column, column);
            game.Move(9, column, column % 7 + 1);
        }

        // 9 in column 9 deduces 8 at (1,9), which leaves (9,9) with no candidates before anything else is deduced.
        game.Move(5, 9, 9);

        Assert.Equal(GameState.Contradicted, game.State);
        Assert.Equal(new RecordedMove(5, 9, 9, 1), game.Moves[^1]);
        Assert.Equal(15, game.Position);
    }
}
