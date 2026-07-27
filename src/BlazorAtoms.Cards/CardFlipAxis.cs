namespace BlazorAtoms.Cards;

/// <summary>Which axis <see cref="AtomCardFlip"/> rotates around on hover.</summary>
public enum CardFlipAxis
{
    /// <summary>Rotates around the vertical axis — the card turns left-to-right, like a page.</summary>
    Y,

    /// <summary>Rotates around the horizontal axis — the card turns top-to-bottom, like a calendar.</summary>
    X,
}
