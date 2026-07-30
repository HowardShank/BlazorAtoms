using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// Prints each slice's percentage on the ring. Goes in <see cref="AtomDonut"/>'s <c>SliceLabels</c> slot.
/// </summary>
/// <remarks>
/// Rendered outside the donut's rotated slice group, at absolute angles the chart computes, so the numbers
/// stay upright whatever <c>StartAngle</c> is. Text inside that group would inherit its rotation and sit at
/// a tilt that changed with the parameter.
/// </remarks>
public partial class AtomChartSliceLabels : AtomChartElementBase
{
    /// <summary>
    /// Slices below this percentage get no label. Default 5.
    /// </summary>
    /// <remarks>
    /// A threshold rather than clever collision detection, because measuring rendered text is exactly what
    /// SVG cannot do without JavaScript. Thin slices would otherwise overlap their neighbours or spill off
    /// the ring; a dropped slice keeps its tooltip and its legend row, so no information is lost.
    /// </remarks>
    [Parameter] public double MinPercent { get; set; } = 5;

    /// <summary>
    /// The chart hands over every slice's label with its share attached, and the threshold is applied here.
    /// </summary>
    /// <remarks>
    /// Filtered by the element rather than by the chart so that <see cref="MinPercent"/> can live here:
    /// dropping a mark changes nothing about the plot's geometry, which is the line between what belongs to
    /// an element and what has to stay on the chart.
    /// </remarks>
    private IReadOnlyList<ChartTextMark> Marks =>
        Chart?.SliceLabels.Where(m => m.Share >= MinPercent).ToArray() ?? [];
}
