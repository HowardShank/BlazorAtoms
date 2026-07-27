using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Cards;

/// <summary>Hover-flip card: the front face shows the background image with title/subtitle and dot
/// indicator; on hover the card rotates 180° around <see cref="FlipAxis"/> to show
/// <see cref="AtomCardBase.BodyContent"/> on the back. Zero JS — pure CSS <c>:hover</c> with
/// <c>preserve-3d</c>, no C# trigger state. Shared card params live on
/// <see cref="AtomCardBase"/>.</summary>
public partial class AtomCardFlip : AtomCardBase
{
    /// <summary>Axis the card rotates around on hover.</summary>
    [Parameter] public CardFlipAxis FlipAxis { get; set; } = CardFlipAxis.Y;

    /// <summary>CSS <c>perspective</c> applied to the card, controlling how pronounced the 3D
    /// foreshortening is during the flip. A smaller value exaggerates the effect. Any CSS length.</summary>
    [Parameter] public string Perspective { get; set; } = "1200px";

    /// <summary>Background color of the back face, behind
    /// <see cref="AtomCardBase.BodyContent"/>.</summary>
    [Parameter] public string BackColor { get; set; } = "#fff";

    private string AxisClass => $"atom-card-flip-axis-{FlipAxis.ToString().ToLowerInvariant()}";

    private string RootStyle =>
        SharedStyleVars +
        $"--atom-card-flip-perspective:{Perspective};" +
        $"--atom-card-flip-back-color:{BackColor};";
}
