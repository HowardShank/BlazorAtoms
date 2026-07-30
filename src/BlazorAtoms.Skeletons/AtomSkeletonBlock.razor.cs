using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Skeletons;

/// <summary>
/// A single placeholder rectangle — the family's painted primitive. Use it directly for anything the
/// presets don't cover (a button, a thumbnail, a table cell); the other three components in this
/// library are presets that render it.
/// </summary>
/// <remarks>
/// This is the only skeleton whose stylesheet paints: the shimmer gradient, the pulse keyframe and the
/// <c>prefers-reduced-motion</c> override all live in <c>AtomSkeletonBlock.razor.css</c> and reach the
/// presets because the presets render this component rather than re-drawing it. See
/// <see cref="AtomSkeletonBase"/> for why that also means presets pass parameters, not classes.
/// </remarks>
public partial class AtomSkeletonBlock : AtomSkeletonBase
{
    /// <summary>Any CSS length or percentage → <c>--skeleton-width</c>. Default <c>100%</c> (CSS).</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Any CSS length → <c>--skeleton-height</c>. Default <c>1rem</c> (CSS).</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Corner radius, any CSS length or percentage → <c>--skeleton-radius</c>. Default
    /// <c>4px</c> (CSS). <c>"50%"</c> gives a circle, which is how
    /// <see cref="AtomSkeletonAvatar"/> draws one.</summary>
    [Parameter] public string? Radius { get; set; }

    private string? RootStyle => BuildRootStyle(
        new StyleVars("skeleton")
            .Add("width", Width)
            .Add("height", Height)
            .Add("radius", Radius)
            .ToString());
}
