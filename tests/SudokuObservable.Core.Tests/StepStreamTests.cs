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
    public void A_cascade_emits_each_deduction_after_the_eliminations_of_the_placement_that_forced_it()
    {
        var game = Game.New();
        for (var digit = 1; digit <= 7; digit++)
        {
            game.Move(1, digit, digit);
        }

        var steps = Record(game);

        game.Move(1, 8, 8);

        Step[] expected =
        [
            new Step.Placement(1, 8, 8, PlacementSource.Move),
            new Step.Elimination(1, 9, 8), // leaves (1,9) with only 9
            new Step.Elimination(2, 7, 8), new Step.Elimination(2, 8, 8), new Step.Elimination(2, 9, 8),
            new Step.Elimination(3, 7, 8), new Step.Elimination(3, 8, 8), new Step.Elimination(3, 9, 8),
            new Step.Elimination(4, 8, 8), new Step.Elimination(5, 8, 8), new Step.Elimination(6, 8, 8),
            new Step.Elimination(7, 8, 8), new Step.Elimination(8, 8, 8), new Step.Elimination(9, 8, 8),
            new Step.Placement(1, 9, 9, PlacementSource.Deduction),
            new Step.Elimination(2, 7, 9), new Step.Elimination(2, 8, 9), new Step.Elimination(2, 9, 9),
            new Step.Elimination(3, 7, 9), new Step.Elimination(3, 8, 9), new Step.Elimination(3, 9, 9),
            new Step.Elimination(4, 9, 9), new Step.Elimination(5, 9, 9), new Step.Elimination(6, 9, 9),
            new Step.Elimination(7, 9, 9), new Step.Elimination(8, 9, 9), new Step.Elimination(9, 9, 9),
        ];
        Assert.Equal(expected, steps);
    }

    [Fact]
    public void Deductions_forced_by_the_same_elimination_are_placed_in_the_order_they_were_forced()
    {
        var game = Game.New();
        for (var digit = 1; digit <= 6; digit++)
        {
            game.Move(1, digit, digit);
        }

        game.Move(7, 8, 9); // (1,8) is left with 7 and 8
        game.Move(4, 9, 9); // (1,9) is left with 7 and 8, so 9 is a hidden single at (1,7)
        Assert.Equal(PlacementSource.Deduction, game.Cell(1, 7).Source);
        var steps = Record(game);

        // Eliminating 7 from (1,9) forces two deductions: (1,9) is a naked single for 8, and (1,8) is the
        // only cell left for 7 in row 1. The cell's own naked single is queued before its units' hidden singles.
        game.Move(9, 9, 7);

        Step[] placements =
        [
            new Step.Placement(9, 9, 7, PlacementSource.Move),
            new Step.Placement(1, 9, 8, PlacementSource.Deduction),
            new Step.Placement(1, 8, 7, PlacementSource.Deduction),
        ];
        Assert.Equal(placements, steps.OfType<Step.Placement>());
        Assert.True(steps.IndexOf(new Step.Elimination(1, 9, 7)) < steps.IndexOf(placements[1]));
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
