using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// A footnote beneath the chart — a source, a caveat, a unit. Goes in a chart's <c>Caption</c> slot.
/// </summary>
/// <remarks>
/// Deliberately a plain content wrapper with no parameters of its own beyond the shared
/// <c>CssClass</c>/<c>Style</c>: everything a caption does is decided by what you put in it. It exists as
/// a component rather than as raw markup in the slot so that its typography is consistent across charts
/// and restyleable in one place.
/// </remarks>
public partial class AtomChartCaption : AtomChartElementBase
{
    /// <summary>The caption text or markup.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
