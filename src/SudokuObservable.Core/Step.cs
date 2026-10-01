namespace SudokuObservable.Core;

/// <summary>One thing that happened to a grid. A move is followed by the steps of its cascade, in order.</summary>
public abstract record Step
{
    private Step()
    {
    }

    /// <summary>A digit was put in a cell, by the player (a move) or by the game (a deduction).</summary>
    public sealed record Placement(int Row, int Column, int Digit, PlacementSource Source) : Step;

    /// <summary>A candidate was removed from a cell because one of its peers holds that digit.</summary>
    public sealed record Elimination(int Row, int Column, int Digit) : Step;
}
