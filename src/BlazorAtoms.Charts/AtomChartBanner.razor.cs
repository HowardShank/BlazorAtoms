using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// A dark pill straddling the shape's own bottom edge — the "SCORE"-style banner several
/// reference gauges overlap directly on the ring, rather than a caption sitting below the whole
/// component. Goes in a gauge-family member's <c>Banner</c> slot.
/// </summary>
/// <remarks>
/// Distinct from <see cref="AtomChartCaption"/>: a caption is placed by <c>AtomChartFrame</c> in the
/// chart's own flex flow, beneath everything. A banner is positioned by the gauge itself, inside the
/// same relatively-positioned wrapper <see cref="AtomChartReadout"/>/<see cref="AtomChartCenter"/> use,
/// so it can sit half-on/half-off the shape's own edge rather than clear of it.
/// </remarks>
public partial class AtomChartBanner : AtomChartElementBase
{
    /// <summary>The banner text or markup.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
