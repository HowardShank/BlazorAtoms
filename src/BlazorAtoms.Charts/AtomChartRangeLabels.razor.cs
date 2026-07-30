namespace BlazorAtoms.Charts;

/// <summary>
/// Prints <c>Min</c> and <c>Max</c> at the ends of the arc, so the dial states its own scale. Goes in
/// <see cref="AtomGauge"/>'s <c>RangeLabels</c> slot.
/// </summary>
/// <remarks>
/// <para>Worth adding to any dial a stranger reads: a needle at two o'clock means nothing without the
/// numbers it is pointing between.</para>
/// <para>Rendered outside the gauge's rotated group at absolute angles, so the numbers stay upright at any
/// <c>SweepAngle</c>. On a full 360° dial the chart supplies one label rather than two, because the two ends
/// are the same point and a <c>Max</c> would print on top of the <c>Min</c> instead of opposite it.</para>
/// </remarks>
public partial class AtomChartRangeLabels : AtomChartElementBase
{
    private IReadOnlyList<ChartTextMark> Marks => Chart?.RangeLabels ?? [];
}
