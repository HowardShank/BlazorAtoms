using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Cards;

/// <summary>Hover page-curl card: the front sheet's <see cref="Corner"/> peels back on hover to
/// uncover the <see cref="AtomCardBase.BodyContent"/> panel underneath, with a shaded triangular
/// fold where the sheet lifts. Zero JS — pure CSS <c>:hover</c>, no C# trigger state.
/// <para>This is a CSS corner <em>fold</em>, not a photorealistic curl: CSS cannot warp a plane, so
/// a true curl would need an SVG displacement filter or WebGL. The fold is the honest ceiling for a
/// zero-JS implementation.</para>
/// Shared card params live on <see cref="AtomCardBase"/>.</summary>
public partial class AtomCardCurl : AtomCardBase
{
    /// <summary>Which corner peels back on hover.</summary>
    [Parameter] public CardCurlCorner Corner { get; set; } = CardCurlCorner.BottomRight;

    /// <summary>How far the corner peels back on hover, measured along each edge from the corner.
    /// Any CSS length; a percentage resolves against the card.</summary>
    [Parameter] public string CurlSize { get; set; } = "60%";

    /// <summary>Size of the fold at rest, before hover — a small dog-ear hinting the card is
    /// peelable. Set to <c>"0px"</c> for no hint at all. Any CSS length.</summary>
    [Parameter] public string RestingCurlSize { get; set; } = "2.5rem";

    /// <summary>Color of the lifted underside of the sheet.</summary>
    [Parameter] public string FoldColor { get; set; } = "#e8e8e8";

    /// <summary>Background color of the body panel revealed under the sheet.</summary>
    [Parameter] public string BodyColor { get; set; } = "#fff";

    private string CornerClass =>
        "atom-card-curl-" + Corner switch
        {
            CardCurlCorner.BottomLeft => "bottomleft",
            CardCurlCorner.TopRight => "topright",
            CardCurlCorner.TopLeft => "topleft",
            _ => "bottomright",
        };

    private string RootStyle =>
        SharedStyleVars +
        $"--atom-card-curl-size:{CurlSize};" +
        $"--atom-card-curl-resting-size:{RestingCurlSize};" +
        $"--atom-card-curl-fold-color:{FoldColor};" +
        $"--atom-card-curl-body-color:{BodyColor};";
}
