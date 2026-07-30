using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Skeletons;

/// <summary>
/// A card-shaped placeholder: an optional media band, then an optional avatar beside a block of text
/// lines. The preset that matches what <c>BlazorAtoms.Cards</c>' <c>AtomCard</c> renders, so one can
/// stand in for the other while data loads.
/// </summary>
/// <remarks>
/// <para>Composes <see cref="AtomSkeletonBlock"/>, <see cref="AtomSkeletonAvatar"/> and
/// <see cref="AtomSkeletonText"/> rather than drawing its own shapes, so the animation and colors have
/// exactly one definition. The cost is visible in the markup: the four inherited axes are forwarded by
/// hand to each child, because a cascade would be a heavier mechanism than four attributes deserve and
/// nothing here needs to be notified of anything.</para>
/// <para>One consequence of composition is worth knowing: each child paints <b>its own</b> shimmer
/// gradient, so the highlight sweeps across every shape simultaneously rather than travelling across
/// the card as one band. That is how skeleton libraries generally behave, and a single card-wide sweep
/// would need the children to stop painting themselves — which would put the gradient back in four
/// places.</para>
/// </remarks>
public partial class AtomSkeletonCard : AtomSkeletonBase
{
    /// <summary>Whether to draw the image band above the body. Default true.</summary>
    [Parameter] public bool ShowMedia { get; set; } = true;

    /// <summary>Height of the media band, any CSS length. Default <c>120px</c>.</summary>
    [Parameter] public string MediaHeight { get; set; } = "120px";

    /// <summary>Whether to draw the avatar beside the text. Default true.</summary>
    [Parameter] public bool ShowAvatar { get; set; } = true;

    /// <summary>Avatar diameter, any CSS length. Default <c>40px</c>.</summary>
    [Parameter] public string AvatarSize { get; set; } = "40px";

    /// <summary>How many text lines to draw. Default 3. Forwarded to
    /// <see cref="AtomSkeletonText.Lines"/>, so 0 draws none.</summary>
    [Parameter] public int Lines { get; set; } = 3;

    /// <summary>Space between the text lines. Left null to use
    /// <see cref="AtomSkeletonText"/>'s own default.</summary>
    [Parameter] public string? LineGap { get; set; }

    /// <summary>Space between the card's own parts (media / body) → <c>--skeleton-gap</c>. Default
    /// <c>0.75rem</c> (CSS).</summary>
    [Parameter] public string? Gap { get; set; }

    /// <summary>Inner padding → <c>--skeleton-padding</c>. Default <c>0</c> (CSS) — a bare placeholder
    /// sits flush, and a caller wrapping it in a real card supplies that card's padding.</summary>
    [Parameter] public string? Padding { get; set; }

    /// <summary>Overall width → <c>--skeleton-width</c>. Default <c>100%</c> (CSS).</summary>
    [Parameter] public string? Width { get; set; }

    private string? RootStyle => BuildRootStyle(
        new StyleVars("skeleton")
            .Add("width", Width)
            .Add("gap", Gap)
            .Add("padding", Padding)
            .ToString());
}
