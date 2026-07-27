namespace BlazorAtoms.Cards;

/// <summary>Where <see cref="AtomCardSplit"/>'s seam runs, and therefore which axis its two halves
/// rotate around when they open.</summary>
public enum CardSplitAxis
{
    /// <summary>Seam runs top-to-bottom down the middle; the left and right halves swing open around
    /// the Y axis, like shutters.</summary>
    Vertical,

    /// <summary>Seam runs left-to-right across the middle; the top and bottom halves swing open
    /// around the X axis, like a hatch.</summary>
    Horizontal,
}
