namespace BlazorAtoms.Charts;

/// <summary>
/// Where a chart puts its <see cref="AtomChartLegend"/>.
/// </summary>
/// <remarks>
/// A chart parameter rather than a legend one, because it decides which layout area the legend is
/// rendered into and the chart has to know that before it renders — the same constraint that keeps
/// <c>ValueAxisWidth</c> on the chart. See the "governing rule" section of <c>README.md</c>.
/// </remarks>
public enum ChartLegendPlacement
{
    /// <summary>Full width beneath the plot. The default everywhere except <see cref="AtomDonut"/>.</summary>
    Below,

    /// <summary>Beside the plot, at the inline end — where a donut's key belongs, since a ring leaves
    /// the space free and a square plot beside a list reads as one unit.</summary>
    End,
}
