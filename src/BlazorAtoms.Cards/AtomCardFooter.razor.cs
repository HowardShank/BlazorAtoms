using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Cards;

/// <summary>
/// The bottom section of an <see cref="AtomCard"/> — typically actions or metadata. Lays its children
/// out in a row and distributes them per <see cref="Align"/>. Works standalone as well as nested.
/// </summary>
public partial class AtomCardFooter : AtomCardSectionBase
{
    /// <summary>How children are distributed along the row → <c>data-align</c>. Default
    /// <see cref="CardSectionAlign.Start"/>.</summary>
    [Parameter] public CardSectionAlign Align { get; set; } = CardSectionAlign.Start;

    /// <summary>Whether to draw the hairline rule above the footer. Null (default) inherits the
    /// enclosing card's setting, then true. Declared here and on <see cref="AtomCardHeader"/> rather
    /// than on the shared base, because <see cref="AtomCardBody"/> has no rule of its own.</summary>
    [Parameter] public bool? Divider { get; set; }

    private string AlignAttr => AtomCard.Kebab(Align.ToString());
}
