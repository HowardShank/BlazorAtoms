namespace BlazorAtoms.Cards;

/// <summary>Where <see cref="AtomCard"/>'s <c>Media</c> slot sits relative to the card's sections.
/// The two inline values turn the card into a horizontal media object.</summary>
public enum CardMediaPosition
{
    /// <summary>Above the sections, spanning the card's full width. Default.</summary>
    Top,

    /// <summary>Below the sections, spanning the full width.</summary>
    Bottom,

    /// <summary>Before the sections on the inline axis (left in LTR), full card height.</summary>
    Start,

    /// <summary>After the sections on the inline axis (right in LTR), full card height.</summary>
    End,
}
