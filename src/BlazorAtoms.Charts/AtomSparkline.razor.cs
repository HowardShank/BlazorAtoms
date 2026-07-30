using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// A bare trend line — no axes, no labels, no chrome. The chart you put inline in a table cell or beside
/// a number, where the shape of the series is the whole message.
/// </summary>
/// <remarks>
/// Inherits <see cref="AtomSeriesChartBase"/> rather than <see cref="AtomCartesianChartBase"/> on
/// purpose: baselines and gridlines contradict what a sparkline is for, and inheriting parameters it
/// then ignored would be worse than not offering them. <see cref="AtomLineChart"/> is the same data with
/// the chrome.
/// </remarks>
public partial class AtomSparkline : AtomSeriesChartBase
{
    /// <summary>Fills the area under the line. Default false.</summary>
    [Parameter] public bool Fill { get; set; }

    /// <summary>Marks the most recent value with a dot — the "you are here" of a sparkline. Default
    /// true.</summary>
    [Parameter] public bool ShowLastPoint { get; set; } = true;

    /// <summary>Curves the line through the points instead of joining them with straight segments.
    /// Default false.</summary>
    [Parameter] public bool Smooth { get; set; }

    /// <summary>Line thickness in view units → <c>--chart-stroke-width</c>. Default <c>2</c> (CSS).</summary>
    [Parameter] public double? StrokeWidth { get; set; }

    /// <summary>Colour of the filled area → <c>--chart-area-color</c>. Defaults to the series colour at
    /// low opacity (CSS).</summary>
    [Parameter] public string? AreaColor { get; set; }

    /// <summary>Opacity of the filled area, 0..1 → <c>--chart-area-opacity</c>. Default <c>0.18</c> (CSS).
    /// Separate from <see cref="AreaColor"/> so the default can tint <c>currentColor</c> without needing
    /// <c>color-mix()</c>.</summary>
    [Parameter] public double? AreaOpacity { get; set; }

    // The view box is fixed and the CSS box scales it uniformly, so a point marker stays a circle rather
    // than an ellipse. Padding keeps the marker and the stroke's own width from being clipped at the edges.
    //
    // 7.5:1 because the CSS locks the box to this ratio: a sparkline is a wide, short strip, and the
    // 300x80 (3.75:1) it started as made a full-width one ~247px tall — a panel, not a sparkline.
    private const double ViewWidth = 300;
    private const double ViewHeight = 40;
    private const double Pad = 4;

    private static double PlotWidth => ViewWidth - Pad * 2;
    private static double PlotHeight => ViewHeight - Pad * 2;
    private double PointRadius => (StrokeWidth ?? 2) + 1;

    private static string ViewBox => $"0 0 {N(ViewWidth)} {N(ViewHeight)}";

    protected override string DefaultAriaLabel => SeriesSummary("sparkline");

    /// <summary>
    /// What the element components see. A sparkline draws no chrome, so every geometry list stays empty —
    /// only <see cref="AtomChartEmptyState"/> has anything to key off here.
    /// </summary>
    /// <remarks>
    /// The chrome elements still work: a heading, a caption and a legend are page furniture rather than
    /// marks on the plot, so none of them contradicts what a sparkline is. Gridlines and axes do, which is
    /// why those slots live one level down on <see cref="AtomCartesianChartBase"/> and a sparkline never
    /// offers them.
    /// </remarks>
    private ChartContext ChartCtx => new()
    {
        HasData = HasData,
        Format = Format,
        Plot = new ChartPlot(Pad, Pad, PlotWidth, PlotHeight, ViewWidth, ViewHeight),
    };

    private string? RootStyle => BuildRootStyle(
        new StyleVars("chart")
            .Add("stroke-width", StrokeWidth)
            .Add("area-color", AreaColor)
            // Formatted as a bare number: StyleVars' double overload appends "px", which would make an
            // opacity declaration invalid and silently drop it.
            .Add("area-opacity", AreaOpacity is null ? null : N(AreaOpacity.Value))
            .ToString());
}
