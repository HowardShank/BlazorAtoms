namespace BlazorAtoms.Charts;

/// <summary>
/// The chart's zero line. Goes in a chart's <c>Baseline</c> slot on <see cref="AtomLineChart"/> or
/// <see cref="AtomBarChart"/>.
/// </summary>
/// <remarks>
/// Drawn along the value axis' origin rather than along the box edge, so on a series that straddles zero it
/// sits where zero actually is and the bars below it hang from it. Which line that is remains the chart's
/// calculation — a bar chart clamps zero into the plotted range so an explicit <c>Min</c> above zero still
/// produces a bar base inside the plot.
/// </remarks>
public partial class AtomChartBaseline : AtomChartElementBase
{
    /// <summary>A one-or-zero-item list, so the shared <see cref="AtomChartElementBase.Lines"/> builder
    /// can render it without a special case for "there is no baseline".</summary>
    private IReadOnlyList<ChartLine> Rule =>
        Chart?.Baseline is { } line ? [line] : [];
}
