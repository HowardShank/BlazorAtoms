using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Cards;

/// <summary>Split-shutter card: the face is cut in two along <see cref="SplitAxis"/>, and on hover
/// the halves swing open to expose a single <see cref="AtomCardBase.BodyContent"/> panel underneath.
/// Zero JS — pure CSS <c>:hover</c> with <c>preserve-3d</c>, no C# trigger state.
/// <para>The halves are front faces with <c>backface-visibility: hidden</c>, so they vanish as they
/// pass 90° instead of presenting a back. That keeps the revealed content one element: text is never
/// split at the seam and never duplicated in the DOM.</para>
/// Shared card params live on <see cref="AtomCardBase"/>.</summary>
public partial class AtomCardSplit : AtomCardBase
{
    /// <summary>Where the seam runs, and therefore which axis the halves rotate around.</summary>
    [Parameter] public CardSplitAxis SplitAxis { get; set; } = CardSplitAxis.Vertical;

    /// <summary>CSS <c>perspective</c> applied to the card, controlling how pronounced the 3D
    /// foreshortening is as the halves swing. A smaller value exaggerates it. Any CSS length.</summary>
    [Parameter] public string Perspective { get; set; } = "1000px";

    /// <summary>How long a half takes to swing fully open.</summary>
    [Parameter] public string OpenDuration { get; set; } = ".6s";

    /// <summary>Renders a circle straddling the seam — whole while the card is closed, halved as the
    /// shutters part. Opt-in; off by default.</summary>
    [Parameter] public bool ShowSeamCircle { get; set; }

    /// <summary>Color of the seam circle. Only used when <see cref="ShowSeamCircle"/> is set.</summary>
    [Parameter] public string SeamCircleColor { get; set; } = "#fff";

    /// <summary>Diameter of the seam circle. Only used when <see cref="ShowSeamCircle"/> is set. Any
    /// CSS length.</summary>
    [Parameter] public string SeamCircleSize { get; set; } = "100px";

    /// <summary>Background color of the body panel revealed under the shutters.</summary>
    [Parameter] public string BodyColor { get; set; } = "#fff";

    private string AxisClass => $"atom-card-split-{SplitAxis.ToString().ToLowerInvariant()}";

    private string RootStyle =>
        SharedStyleVars +
        $"--atom-card-split-perspective:{Perspective};" +
        $"--atom-card-split-duration:{OpenDuration};" +
        $"--atom-card-split-circle-color:{SeamCircleColor};" +
        $"--atom-card-split-circle-size:{SeamCircleSize};" +
        $"--atom-card-split-body-color:{BodyColor};";
}
