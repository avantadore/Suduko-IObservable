namespace SudokuObservable.Core.Tests;

public class StepStreamTests
{
    [Fact]
    public void A_move_emits_the_move_first_then_its_eliminations_row_by_row()
    {
        var game = Game.New();
        var steps = Record(game);

        game.Move(1, 1, 5);

        Step[] expected =
        [
            new Step.Placement(1, 1, 5, PlacementSource.Move),
            new Step.Elimination(1, 2, 5), new Step.Elimination(1, 3, 5), new Step.Elimination(1, 4, 5),
            new Step.Elimination(1, 5, 5), new Step.Elimination(1, 6, 5), new Step.Elimination(1, 7, 5),
            new Step.Elimination(1, 8, 5), new Step.Elimination(1, 9, 5),
            new Step.Elimination(2, 1, 5), new Step.Elimination(2, 2, 5), new Step.Elimination(2, 3, 5),
            new Step.Elimination(3, 1, 5), new Step.Elimination(3, 2, 5), new Step.Elimination(3, 3, 5),
            new Step.Elimination(4, 1, 5), new Step.Elimination(5, 1, 5), new Step.Elimination(6, 1, 5),
            new Step.Elimination(7, 1, 5), new Step.Elimination(8, 1, 5), new Step.Elimination(9, 1, 5),
        ];
        Assert.Equal(expected, steps);
    }

    [Fact]
    public void A_move_emits_eliminations_only_for_peers_that_still_had_the_digit()
    {
        var game = Game.New();
        game.Move(1, 1, 5);
        var steps = Record(game);

        game.Move(2, 4, 5);

        // (2,1), (2,2) and (2,3) already lost 5 to (1,1), the box of (2,4) has lost row 1, and (1,4) lost it too.
        Step[] expected =
        [
            new Step.Placement(2, 4, 5, PlacementSource.Move),
            new Step.Elimination(2, 5, 5), new Step.Elimination(2, 6, 5), new Step.Elimination(2, 7, 5),
            new Step.Elimination(2, 8, 5), new Step.Elimination(2, 9, 5),
            new Step.Elimination(3, 4, 5), new Step.Elimination(3, 5, 5), new Step.Elimination(3, 6, 5),
            new Step.Elimination(4, 4, 5), new Step.Elimination(5, 4, 5), new Step.Elimination(6, 4, 5),
            new Step.Elimination(7, 4, 5), new Step.Elimination(8, 4, 5), new Step.Elimination(9, 4, 5),
        ];
        Assert.Equal(expected, steps);
    }

    [Fact]
    public void Rejected_and_unchanged_moves_emit_nothing()
    {
        var game = Game.New();
        game.Move(1, 1, 5);
        var steps = Record(game);

        game.Move(1, 1, 5);
        game.Move(1, 1, 6);
        game.Move(1, 2, 5);

        Assert.Empty(steps);
    }

    private static List<Step> Record(Game game)
    {
        var steps = new List<Step>();
        game.Steps.Subscribe(steps.Add);
        return steps;
    }
}
