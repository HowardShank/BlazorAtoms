namespace BlazorAtoms.Charts;

/// <summary>
/// Labels the value axis — every gridline plus both ends of the range. Goes in a chart's
/// <c>ValueAxis</c> slot on <see cref="AtomLineChart"/> or <see cref="AtomBarChart"/>.
/// </summary>
/// <remarks>
/// <para><b>Adding it widens the chart's gutter.</b> The chart reads the slot before it lays anything out
/// and reserves <c>ValueAxisWidth</c> for the labels — which is why that width is a parameter on the
/// chart and not on this element. See the "governing rule" section of <c>README.md</c>.</para>
/// <para><b>Two markup shapes, chosen by the chart.</b> A vertical value axis renders as SVG
/// <c>&lt;text&gt;</c> inside the plot's own coordinate system, because aligning to fractions of the
/// <i>height</i> is something CSS cannot express — percentage padding resolves against width even for
/// <c>padding-top</c>. A horizontal one (horizontal bars) renders as an HTML row, where the alignment is
/// exact and the text inherits the page's font. The chart sets
/// <see cref="ChartContext.ValueAxisInSvg"/>; this element does not choose.</para>
/// <para>The values themselves come from the chart's tick model, which <c>GridlineCount</c> and
/// <c>NiceScale</c> control. Those stay on the chart because they determine the plotted range, not just
/// its labels.</para>
/// </remarks>
public partial class AtomChartValueAxis : AtomChartElementBase
{
    private IReadOnlyList<ChartTextMark> Ticks => Chart?.ValueTicks ?? [];

    /// <summary>Defaults to the SVG shape when there is no chart to ask, since that is what both charts
    /// use in their default orientation. Standalone it renders an empty group.</summary>
    private bool InSvg => Chart?.ValueAxisInSvg ?? true;
}
