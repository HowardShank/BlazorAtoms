namespace BlazorAtoms.Cards;

/// <summary>
/// Cascaded by <see cref="AtomCard"/> so its section children inherit the card's padding and divider
/// defaults instead of repeating them per section. A section's own explicitly-set parameter always
/// wins, because each section's value is nullable and the context is consulted only when it is null —
/// no "was it set?" detection needed.
/// </summary>
/// <remarks>
/// Carries only what a section genuinely needs from its parent. Variant/elevation/effect stay on the
/// card: they describe the card's own frame, and a section has no frame of its own to treat.
/// </remarks>
public sealed class CardContext
{
    /// <summary>The card's section padding in px, or null to leave the CSS default in place.</summary>
    public double? Padding { get; init; }

    /// <summary>Whether sections draw their divider rule by default.</summary>
    public bool Divider { get; init; } = true;
}
