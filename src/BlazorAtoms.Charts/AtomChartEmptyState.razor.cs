using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// What the chart shows instead of marks when there is nothing to plot. Goes in a chart's
/// <c>EmptyState</c> slot.
/// </summary>
/// <remarks>
/// <para>Without it, a null or empty series draws a correctly sized, entirely blank box — which reads as
/// a component that failed rather than a query that returned nothing. The chart's own degenerate-input
/// handling is deliberate (an empty series draws no marks rather than throwing), and this is the piece
/// that makes that state legible.</para>
/// <para>Rendered over the plot area rather than in place of it, so the box does not change size when the
/// data arrives — no layout shift between the empty and loaded states.</para>
/// </remarks>
public partial class AtomChartEmptyState : AtomChartElementBase
{
    /// <summary>Message to show. Default <c>"No data"</c>. Ignored when
    /// <see cref="ChildContent"/> is supplied.</summary>
    [Parameter] public string? Text { get; set; } = "No data";

    /// <summary>Arbitrary content, replacing <see cref="Text"/> entirely — an icon, a retry button, a
    /// link to the filter that excluded everything.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
