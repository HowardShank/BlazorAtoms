using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Cards;

/// <summary>Hover-reveal card: a themed overlay (title/subtitle/background image/dot indicator)
/// plays a staggered entrance animation once on mount, then slides away on hover to reveal a
/// scrollable <see cref="AtomCardBase.BodyContent"/> panel behind it. Zero JS — pure CSS
/// <c>:hover</c> plus a fixed animation-delay stagger, no C# trigger state. Shared card params
/// live on <see cref="AtomCardBase"/>.</summary>
public partial class AtomCardReveal : AtomCardBase
{
    /// <summary>Which way the overlay slides away on hover; the body panel is revealed on the
    /// opposite side.</summary>
    [Parameter] public CardRevealDirection Direction { get; set; } = CardRevealDirection.Left;

    /// <summary>How much of the card the body panel takes up once revealed — the remaining
    /// <c>100% - RevealSize</c> stays as a visible sliver of the background image. Measured along
    /// whichever axis <see cref="Direction"/> selects: a width for
    /// <see cref="CardRevealDirection.Left"/>/<see cref="CardRevealDirection.Right"/>, a height for
    /// <see cref="CardRevealDirection.Up"/>/<see cref="CardRevealDirection.Down"/> — which is why
    /// this is "size" and not "width". Any CSS length; a percentage resolves against the card
    /// itself, not the viewport, so the layout holds at any <see cref="AtomCardBase.Width"/>.</summary>
    [Parameter] public string RevealSize { get; set; } = "70%";

    private string DirectionClass => $"atom-card-reveal-{Direction.ToString().ToLowerInvariant()}";

    private string RootStyle =>
        SharedStyleVars +
        $"--atom-card-reveal-body-size:{RevealSize};";
}
