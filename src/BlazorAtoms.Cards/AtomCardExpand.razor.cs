using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Cards;

/// <summary>Hover-expand card: rests at <see cref="AtomCardBase.Height"/> showing the background
/// image with title/subtitle and dot indicator, then grows to <see cref="ExpandedHeight"/> on hover
/// while the <see cref="AtomCardBase.BodyContent"/> panel slides up from the bottom edge. Zero JS —
/// pure CSS <c>:hover</c>, no C# trigger state. Shared card params live on
/// <see cref="AtomCardBase"/>.</summary>
public partial class AtomCardExpand : AtomCardBase
{
    /// <summary>Height the card grows to on hover. Any CSS length. Should exceed
    /// <see cref="AtomCardBase.Height"/> — the difference is the space the body panel expands
    /// into.</summary>
    [Parameter] public string ExpandedHeight { get; set; } = "90vmin";

    /// <summary>Height of the body panel once expanded, measured from the card's bottom edge. Any
    /// CSS length; a percentage resolves against the expanded card.</summary>
    [Parameter] public string BodyHeight { get; set; } = "75%";

    /// <summary>Background color of the body panel.</summary>
    [Parameter] public string BodyColor { get; set; } = "#fff";

    private string RootStyle =>
        SharedStyleVars +
        $"--atom-card-expand-expanded-height:{ExpandedHeight};" +
        $"--atom-card-expand-body-height:{BodyHeight};" +
        $"--atom-card-expand-body-color:{BodyColor};";
}
