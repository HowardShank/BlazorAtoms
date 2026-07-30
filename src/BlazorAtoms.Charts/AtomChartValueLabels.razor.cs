namespace BlazorAtoms.Charts;

/// <summary>
/// Prints each value beside its own mark. Goes in a chart's <c>ValueLabels</c> slot on
/// <see cref="AtomLineChart"/> or <see cref="AtomBarChart"/>.
/// </summary>
/// <remarks>
/// <para><b>Overlap is possible and not detected.</b> On a dense series the readouts collide, and nothing
/// here can measure rendered text to avoid it — that is precisely what SVG cannot do without JavaScript.
/// The element exists so the decision is yours to make per chart rather than being taken by a default.</para>
/// <para>SVG text rather than HTML, unlike the category labels: a readout has to sit at its mark's own
/// coordinates, which only the plot's coordinate system can express.</para>
/// </remarks>
public partial class AtomChartValueLabels : AtomChartElementBase
{
    private IReadOnlyList<ChartTextMark> Marks => Chart?.MarkLabels ?? [];
}
