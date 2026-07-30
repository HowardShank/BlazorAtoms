using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// Names an axis — "Revenue ($k)", "Month". Goes in a chart's <c>ValueAxisTitle</c> or
/// <c>CategoryAxisTitle</c> slot.
/// </summary>
/// <remarks>
/// <para><b>The same component for both axes.</b> It renders plain text; the chart's own wrapper is what
/// turns the value-axis one on its side, because only the chart knows which axis is which — and for
/// horizontal bars the value axis is the one along the bottom, so the two swap places. An element cannot
/// see which slot it was placed in.</para>
/// <para><b>HTML, never SVG.</b> A title rotated inside the <c>viewBox</c> would scale with the graphic
/// and cost <c>viewBox</c> space the chart would have to reserve. As an HTML cell in the plot's grid it
/// costs only the space it actually occupies, and vanishes to zero width when the slot is empty.</para>
/// </remarks>
public partial class AtomChartAxisTitle : AtomChartElementBase
{
    /// <summary>The axis name.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
