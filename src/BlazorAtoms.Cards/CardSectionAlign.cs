namespace BlazorAtoms.Cards;

/// <summary>How <see cref="AtomCardFooter"/> distributes its children along the inline axis. Maps onto
/// <c>justify-content</c>.</summary>
public enum CardSectionAlign
{
    /// <summary>Packed at the start (left in LTR). Default.</summary>
    Start,

    /// <summary>Centered.</summary>
    Center,

    /// <summary>Packed at the end (right in LTR) — the usual place for footer actions.</summary>
    End,

    /// <summary>Spread apart, first at the start and last at the end.</summary>
    Between,
}
