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

    private static (int?, PlacementSource?) DigitAndSource(Cell cell) => (cell.Digit, cell.Source);
}
