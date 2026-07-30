using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// A line chart: the sparkline's data with chrome — every piece of it an opt-in element in its own slot.
/// </summary>
/// <remarks>
/// <para>Nothing but the line is drawn by default. A baseline, gridlines, a labelled value axis, a row of
/// category labels, per-point readouts, a heading, a legend and a caption are each a child component you
/// place in the matching slot, with its own <c>CssClass</c>/<c>Style</c> and its own stylesheet.</para>
/// <para>Category labels render as HTML beneath the SVG rather than as <c>&lt;text&gt;</c> inside it. Text
/// in a scaled <c>viewBox</c> scales with the graphic, which means it ignores the reader's font-size
/// preference and cannot wrap; HTML labels inherit the page's typography and reflow. The trade-off is that
/// they are not part of the exported SVG, which is the right way round for a component whose output is a
/// live page.</para>
/// </remarks>
public partial class AtomLineChart : AtomCartesianChartBase
{
    /// <summary>Draws a marker at each data point. Default true.</summary>
    [Parameter] public bool ShowPoints { get; set; } = true;

    /// <summary>Fills the area under the line. Default false.</summary>
    [Parameter] public bool ShowArea { get; set; }

    /// <summary>Curves the line through the points instead of joining them with straight segments.
    /// Default false.</summary>
    [Parameter] public bool Smooth { get; set; }

    /// <summary>Line thickness in view units → <c>--chart-stroke-width</c>. Default <c>2</c> (CSS).</summary>
    [Parameter] public double? StrokeWidth { get; set; }

    /// <summary>Colour of the filled area → <c>--chart-area-color</c>.</summary>
    [Parameter] public string? AreaColor { get; set; }

    /// <summary>Opacity of the filled area, 0..1 → <c>--chart-area-opacity</c>. Default
    /// <c>0.18</c> (CSS).</summary>
    [Parameter] public double? AreaOpacity { get; set; }

    private const double ViewWidth = 320;
    private const double ViewHeight = 160;
    private const double BasePad = 6;

    /// <summary>Widened on the left when the value-axis slot is filled, to make room for its labels.</summary>
    /// <remarks>Read from the slot rather than from a child component, because this has to be known before
    /// the first mark is placed and a parent cannot see its children during its own render pass.</remarks>
    private double PadLeft => ValueAxis is null ? BasePad : EffectiveValueAxisWidth;

    /// <summary>
    /// Also widened when the axis is shown: the topmost tick label is centred on the plot's top edge, so
    /// its ascender needs room above it or the viewBox clips the glyph.
    /// </summary>
    /// <remarks>
    /// 6 units of padding left roughly 2 for a 9-unit font's ascender — inside the box arithmetically, and
    /// clipped in practice as soon as a font's ascent runs past 0.8em. 12 clears any reasonable face.
    /// </remarks>
    private double PadTop => ValueAxis is null ? BasePad : 12;

    private double PlotWidth => ViewWidth - PadLeft - BasePad;
    private double PlotHeight => ViewHeight - PadTop - PadBottom;

    private double PadBottom => BasePad;
    private double PointRadius => (StrokeWidth ?? 2) + 1.5;

    private static string ViewBox => $"0 0 {N(ViewWidth)} {N(ViewHeight)}";

    protected override string DefaultAriaLabel => SeriesSummary("line chart");

    /// <summary>
    /// Everything the element components draw, computed here and handed over ready to place.
    /// </summary>
    /// <remarks>
    /// All the SVG coordinates are relative to the translated <c>&lt;g&gt;</c> the chart renders those
    /// slots inside, so an element never has to know about <see cref="PadLeft"/>/<see cref="PadTop"/>.
    /// </remarks>
    private ChartContext ChartCtx => new()
    {
        HasData = HasData,
        Format = Format,
        ValueAxisInSvg = true,
        // XAt spans 0..PlotWidth exactly, so the first and last points sit on the plot's own edges.
        CategoryLabelsAlignEnds = true,
        Plot = new ChartPlot(PadLeft, PadTop, PlotWidth, PlotHeight, ViewWidth, ViewHeight),
        ValueTicks = BuildValueTicks(),
        MarkLabels = BuildMarkLabels(),
        CategoryLabels = BuildCategoryLabels(),
        Gridlines = BuildGridlines(),
        Baseline = Baseline is null ? null : new ChartLine(0, PlotHeight, PlotWidth, PlotHeight),
        Legend = BuildLegend(),
    };

    /// <summary>
    /// Tick labels for the left gutter.
    /// </summary>
    /// <remarks>
    /// In the SVG rather than HTML, unlike the category labels below. A vertical axis has to align to
    /// fractions of the <i>height</i>, and CSS percentage padding resolves against width even for
    /// top/bottom — the trap that put the horizontal bar labels 13px out. Inside the SVG the alignment is
    /// exact by construction, since the labels share the coordinate system of the gridlines they name.
    /// </remarks>
    private ChartTextMark[] BuildValueTicks()
    {
        if (ValueAxis is null || !HasData) return [];

        var ticks = TickValues.ToArray();
        var marks = new ChartTextMark[ticks.Length];

        for (var i = 0; i < ticks.Length; i++)
        {
            marks[i] = new ChartTextMark(
                Format(ticks[i]),
                -4,
                // +3 nudges the baseline of the text to the middle of the glyph, which SVG has no way to
                // ask for on a single line ('central' is inconsistent across engines for text elements).
                PlotHeight - TickFraction(i) * PlotHeight + 3,
                "end");
        }

        return marks;
    }

    private ChartTextMark[] BuildMarkLabels()
    {
        if (ValueLabels is null || !HasData) return [];

        var marks = new ChartTextMark[Series.Length];

        for (var i = 0; i < Series.Length; i++)
        {
            marks[i] = new ChartTextMark(
                Format(Series[i]),
                XAt(i, PlotWidth),
                YAt(Series[i], PlotHeight) - PointRadius - 3,
                "middle");
        }

        return marks;
    }

    private string?[] BuildCategoryLabels()
    {
        if (CategoryAxis is null || !HasData) return [];

        var labels = new string?[Series.Length];
        for (var i = 0; i < Series.Length; i++) labels[i] = LabelAt(i);
        return labels;
    }

    private ChartLine[] BuildGridlines() =>
        GridlineOffsets(PlotHeight).Select(y => new ChartLine(0, y, PlotWidth, y)).ToArray();

    /// <summary>
    /// One row per point rather than one per series: there is only one series, and it has no name to print.
    /// </summary>
    /// <remarks>
    /// Share is left at zero because a line chart's values do not sum to anything — which is also what
    /// makes <see cref="AtomChartLegend.ShowPercent"/> resolve to false here without being told.
    /// </remarks>
    private ChartLegendEntry[] BuildLegend()
    {
        if (Legend is null || !HasData) return [];

        var rows = new ChartLegendEntry[Series.Length];
        for (var i = 0; i < Series.Length; i++) rows[i] = new ChartLegendEntry(null, LabelAt(i), Series[i], 0);
        return rows;
    }

    private string? RootStyle => BuildRootStyle(
        new StyleVars("chart")
            .Add("stroke-width", StrokeWidth)
            .Add("axis-color", AxisColor)
            .Add("area-color", AreaColor)
            .Add("area-opacity", AreaOpacity is null ? null : N(AreaOpacity.Value))
            // The HTML category-label row has to inset by the same amount as the SVG's own padding.
            // Percentages of the viewBox width, so they scale with the box exactly as the SVG does — and
            // custom properties rather than a class, because they have to cross a CSS-scope boundary into
            // AtomChartCategoryAxis.
            .Add("pad-left", $"{N(PadLeft / ViewWidth * 100)}%")
            .Add("pad-right", $"{N(BasePad / ViewWidth * 100)}%")
            .ToString());
}
