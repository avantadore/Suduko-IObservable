namespace SudokuObservable.Core.Tests;

public class ReplayTests
{
    [Fact]
    public void Replaying_to_the_last_move_gives_the_same_grid_as_the_live_game()
    {
        var game = Game.New();
        foreach (var (row, column, digit) in Puzzle.Givens.Take(20))
        {
            game.Move(row, column, digit);
        }

        var live = GridOf(game);
        var state = game.State;
        var moves = game.Moves.ToList();

        game.ReplayTo(game.Moves.Count);

        Assert.Equal(live, GridOf(game));
        Assert.Equal(state, game.State);
        Assert.Equal(moves, game.Moves);
        Assert.Equal(moves.Count, game.Position);
    }

    [Fact]
    public void Replaying_back_keeps_the_later_moves_and_replaying_forward_again_restores_them()
    {
        var game = Game.New();
        game.Move(1, 1, 5);
        var afterFirst = GridOf(game);
        game.Move(2, 4, 5);
        game.Move(5, 5, 7);
        var afterThird = GridOf(game);
        var moves = game.Moves.ToList();

        game.ReplayTo(1);

        Assert.Equal(1, game.Position);
        Assert.Equal(afterFirst, GridOf(game));
        Assert.Equal(moves, game.Moves);

        game.ReplayTo(0);

        Assert.Equal(0, game.Position);
        Assert.All(game.Cells, cell => Assert.Null(cell.Digit));
        Assert.Equal(moves, game.Moves);

        game.ReplayTo(3);

        Assert.Equal(3, game.Position);
        Assert.Equal(afterThird, GridOf(game));
    }

    [Fact]
    public void A_new_move_at_an_earlier_position_discards_the_later_moves()
    {
        var game = ThreeMoves();
        game.ReplayTo(1);

        var outcome = game.Move(9, 9, 1);

        Assert.IsType<MoveOutcome.Accepted>(outcome);
        Assert.Equal([new RecordedMove(1, 1, 5, 0), new RecordedMove(9, 9, 1, 0)], game.Moves);
        Assert.Equal(2, game.Position);
        Assert.Null(game.Cell(2, 4).Digit);
    }

    [Fact]
    public void Placing_exactly_the_next_recorded_move_is_a_new_move_that_discards_the_ones_after_it()
    {
        var game = ThreeMoves();
        game.ReplayTo(1);

        game.Move(2, 4, 5);

        Assert.Equal([new RecordedMove(1, 1, 5, 0), new RecordedMove(2, 4, 5, 0)], game.Moves);
        Assert.Equal(2, game.Position);
    }

    [Fact]
    public void An_unchanged_or_rejected_move_at_an_earlier_position_keeps_the_later_moves()
    {
        var game = ThreeMoves();
        var moves = game.Moves.ToList();
        game.ReplayTo(1);

        var unchanged = game.Move(1, 1, 5);
        var rejected = game.Move(1, 2, 5);

        Assert.IsType<MoveOutcome.Unchanged>(unchanged);
        Assert.IsType<MoveOutcome.Rejected>(rejected);
        Assert.Equal(moves, game.Moves);
        Assert.Equal(1, game.Position);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Replaying_outside_0_to_the_number_of_moves_throws(int position)
    {
        var game = ThreeMoves();

        Assert.Throws<ArgumentOutOfRangeException>(() => game.ReplayTo(position));
        Assert.Equal(3, game.Position);
    }

    [Fact]
    public void Two_fresh_games_given_the_same_moves_emit_the_same_steps()
    {
        var first = Game.New();
        var second = Game.New();
        var firstSteps = Record(first);
        var secondSteps = Record(second);

        foreach (var game in new[] { first, second })
        {
            foreach (var (row, column, digit) in Puzzle.Givens)
            {
                game.Move(row, column, digit);
            }
        }

        Assert.NotEmpty(firstSteps);
        Assert.Equal(firstSteps, secondSteps);
    }

    [Fact]
    public void A_steps_subscriber_from_before_a_replay_receives_later_moves_and_none_of_the_replayed_steps()
    {
        var game = ThreeMoves();
        var steps = Record(game);

        game.ReplayTo(0);
        game.ReplayTo(2);

        Assert.Empty(steps);

        game.Move(9, 9, 1);

        Assert.Equal(new Step.Placement(9, 9, 1, PlacementSource.Move), steps[0]);
        Assert.DoesNotContain(steps, step => step is Step.Placement { Digit: not 1 });
    }

    private static List<Step> Record(Game game)
    {
        var steps = new List<Step>();
        game.Steps.Subscribe(steps.Add);
        return steps;
    }

    private static Game ThreeMoves()
    {
        var game = Game.New();
        game.Move(1, 1, 5);
        game.Move(2, 4, 5);
        game.Move(5, 5, 7);
        return game;
    }

    private static List<string> GridOf(Game game) =>
        [.. game.Cells.Select(cell => $"{cell.Row},{cell.Column}: {cell.Digit} {cell.Source} [{string.Join("", cell.Candidates)}]")];
}
