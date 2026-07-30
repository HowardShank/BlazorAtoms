using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// Rules across the plot at the interior tick positions. Goes in a chart's <c>Gridlines</c> slot on
/// <see cref="AtomLineChart"/> or <see cref="AtomBarChart"/>.
/// </summary>
/// <remarks>
/// <para><b>How many lines is the chart's decision, not this element's.</b> <c>GridlineCount</c> feeds the
/// tick step, which snaps the plotted range itself under <c>NiceScale</c> — so it changes where the marks
/// are, not merely how many rules are drawn, and it has to stay on the chart. This element decides only
/// how the rules look.</para>
/// <para>The lines sit at the same positions an <see cref="AtomChartValueAxis"/> labels, and deliberately
/// exclude both ends: the low end is the baseline's job, and a rule on the top edge reads as a border.
/// Gridlines and axis labels disagreeing by even one position looks like a rendering bug rather than a
/// rounding choice.</para>
/// </remarks>
public partial class AtomChartGridlines : AtomChartElementBase
{
    /// <summary>Dashes the rules. Default true — a dashed line reads as a reading aid where a solid one
    /// competes with the data.</summary>
    [Parameter] public bool Dashed { get; set; } = true;

    private IReadOnlyList<ChartLine> Rules => Chart?.Gridlines ?? [];

    private string? DashedAttr => Dashed ? "true" : null;
}
