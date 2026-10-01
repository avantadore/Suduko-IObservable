namespace SudokuObservable.Core.Tests;

public class NakedSingleTests
{
    [Fact]
    public void A_cell_left_with_one_candidate_is_filled_as_a_deduction_and_its_peers_eliminate_the_digit()
    {
        var game = Game.New();
        // Row 1 holds 1–7, so (1,8) and (1,9) are left with 8 and 9.
        for (var digit = 1; digit <= 7; digit++)
        {
            game.Move(1, digit, digit);
        }

        game.Move(1, 8, 8);

        var deduced = game.Cell(1, 9);
        Assert.Equal(9, deduced.Digit);
        Assert.Equal(PlacementSource.Deduction, deduced.Source);
        Assert.Equal([9], deduced.Candidates);
        Assert.DoesNotContain(9, game.Cell(5, 9).Candidates);
        Assert.DoesNotContain(9, game.Cell(2, 7).Candidates);
    }

    [Fact]
    public void A_deduction_can_force_further_deductions_within_the_same_move()
    {
        var game = Game.New();
        // Row 1 holds 1–6, leaving 7, 8 and 9 for (1,7), (1,8) and (1,9).
        for (var digit = 1; digit <= 6; digit++)
        {
            game.Move(1, digit, digit);
        }

        game.Move(7, 8, 9); // (1,8) is left with 7 and 8
        game.Move(4, 9, 9); // (1,9) is left with 7 and 8, so 9 is a hidden single at (1,7)

        // 7 in column 9 leaves (1,9) with 8, which in turn leaves (1,8) with 7.
        game.Move(9, 9, 7);

        Assert.Equal((8, PlacementSource.Deduction), DigitAndSource(game.Cell(1, 9)));
        Assert.Equal((7, PlacementSource.Deduction), DigitAndSource(game.Cell(1, 8)));
    }

    private static (int?, PlacementSource?) DigitAndSource(Cell cell) => (cell.Digit, cell.Source);
}
