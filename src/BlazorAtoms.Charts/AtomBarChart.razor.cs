using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// A bar chart, vertical or horizontal, for comparing discrete values.
/// </summary>
/// <remarks>
/// <para><b>Zero-based by default.</b> <see cref="DefaultLo"/> includes zero, because a bar encodes its
/// value as a length: scaled from the data's minimum instead, the smallest bar would always be
/// zero-height and every other bar would overstate itself. Setting <see cref="AtomSeriesChartBase.Min"/>
/// explicitly overrides that, which is the caller's decision to make.</para>
/// <para><b>Negatives work.</b> Bars are drawn from the zero line rather than from the bottom edge, so a
/// series straddling zero renders below it as well as above. A value of exactly zero draws a
/// zero-height rect — invisible, and therefore with an unreachable tooltip, which is the honest
/// rendering of nothing.</para>
/// <para><b>The chrome is opt-in.</b> Baseline, gridlines, axes, readouts, heading, legend and caption are
/// each a child component placed in the matching slot. Nothing but the bars is drawn by default.</para>
/// </remarks>
public partial class AtomBarChart : AtomCartesianChartBase
{
    /// <summary>Which way the bars grow → <c>data-orientation</c>. Default
    /// <see cref="ChartOrientation.Vertical"/>.</summary>
    [Parameter] public ChartOrientation Orientation { get; set; } = ChartOrientation.Vertical;

    /// <summary>Space between bars as a fraction of each slot, 0..0.9. Default <c>0.25</c>.</summary>
    [Parameter] public double BarGap { get; set; } = 0.25;

    /// <summary>Corner radius in view units. Default 0 (square corners).</summary>
    [Parameter] public double? Radius { get; set; }

    // Bars use the inherited --chart-series-color like every other series, so there is no bar-specific
    // colour parameter.
    private const double ViewWidth = 320;
    private const double ViewHeight = 160;

    /// <summary>Widened for the value-axis gutter, but only for vertical bars — a horizontal chart's value
    /// axis runs along the bottom and is labelled in HTML instead.</summary>
    private double PadLeft => HasSvgValueAxis ? EffectiveValueAxisWidth : 6;

    private double PadRight => 6;

    /// <summary>True when the value axis is both present and vertical, which is the only case that costs
    /// the plot any horizontal room.</summary>
    private bool HasSvgValueAxis => ValueAxis is not null && IsVertical;

    /// <summary>
    /// Zero across the bar axis in horizontal mode, so the bar track spans the full height.
    /// </summary>
    /// <remarks>
    /// The label track has to line up with the bar track. For vertical bars the labels are a row, and a
    /// percentage padding matching the SVG's own works, because percentage padding resolves against the
    /// containing block's <i>width</i> — which is the SVG's width.
    /// <para>For horizontal bars the labels are a column, and the padding needed is a share of the
    /// <i>height</i> — which percentage padding cannot express, since it resolves against width there too.
    /// So rather than padding the label column to match the SVG, the SVG stops padding that axis: both
    /// tracks span the full height and two sets of equal flex items line up exactly. Nothing is clipped,
    /// because <see cref="BarGap"/> already insets each bar inside its own slot.</para>
    /// </remarks>
    private double PadY => IsVertical ? 6 : 0;

    /// <summary>
    /// Extra room above the plot for the topmost tick label, which is centred on the plot's top edge — its
    /// ascender needs space or the viewBox clips it. Vertical bars only; a horizontal chart's ticks are the
    /// HTML row underneath.
    /// </summary>
    private double PadTop => HasSvgValueAxis ? 12 : PadY;

    private double PlotWidth => ViewWidth - PadLeft - PadRight;
    private double PlotHeight => ViewHeight - PadTop - PadY;
    private static string ViewBox => $"0 0 {N(ViewWidth)} {N(ViewHeight)}";

    private bool IsVertical => Orientation == ChartOrientation.Vertical;
    private string OrientationAttr => Orientation.ToString().ToLowerInvariant();

    /// <summary>Bars measure length from zero — see the class remarks.</summary>
    protected override double DefaultLo => Math.Min(0, Series.Min());

    protected override string DefaultAriaLabel => SeriesSummary("bar chart");

    /// <summary>Where the zero line falls along the value axis, clamped into the plotted range so an
    /// explicit Min/Max that excludes zero still produces a bar base inside the plot.</summary>
    private double ZeroOffset
    {
        get
        {
            var (lo, hi) = Range;
            var zero = Math.Clamp(0, lo, hi);
            var span = IsVertical ? PlotHeight : PlotWidth;
            var f = (zero - lo) / (hi - lo);
            return IsVertical ? span - f * span : f * span;
        }
    }

    private (double X, double Y, double W, double H) BarAt(int i)
    {
        var count = Series.Length;
        var gap = Math.Clamp(BarGap, 0, 0.9);

        if (IsVertical)
        {
            var slot = PlotWidth / count;
            var w = Math.Max(slot * (1 - gap), 0.5);
            var x = PadLeft + i * slot + (slot - w) / 2;
            var valueY = PadTop + PlotHeight - Fraction(Series[i]) * PlotHeight;
            var zeroY = PadTop + ZeroOffset;
            return (x, Math.Min(valueY, zeroY), w, Math.Abs(valueY - zeroY));
        }
        else
        {
            var slot = PlotHeight / count;
            var h = Math.Max(slot * (1 - gap), 0.5);
            var y = PadTop + i * slot + (slot - h) / 2;
            var valueX = PadLeft + Fraction(Series[i]) * PlotWidth;
            var zeroX = PadLeft + ZeroOffset;
            return (Math.Min(valueX, zeroX), y, Math.Abs(valueX - zeroX), h);
        }
    }

    /// <summary>
    /// Everything the element components draw. Unlike the line chart's, these coordinates are absolute:
    /// this chart has no translated group, so the slots sit directly in the SVG's own space.
    /// </summary>
    private ChartContext ChartCtx => new()
    {
        HasData = HasData,
        Format = Format,
        Orientation = Orientation,
        ValueAxisInSvg = IsVertical,
        // Left at the default false: every bar is inset within its own slot by BarGap, so the first and
        // last are exactly as centred as the ones between them — unlike a line chart's end points.
        Plot = new ChartPlot(PadLeft, PadTop, PlotWidth, PlotHeight, ViewWidth, ViewHeight),
        ValueTicks = BuildValueTicks(),
        MarkLabels = BuildMarkLabels(),
        CategoryLabels = BuildCategoryLabels(),
        Gridlines = BuildGridlines(),
        Baseline = Baseline is null ? null : BaselineLine,
        Legend = BuildLegend(),
    };

    /// <summary>The zero line, drawn along the value axis' origin rather than along the box edge.</summary>
    private ChartLine BaselineLine => IsVertical
        ? new ChartLine(PadLeft, PadTop + ZeroOffset, PadLeft + PlotWidth, PadTop + ZeroOffset)
        : new ChartLine(PadLeft + ZeroOffset, PadTop, PadLeft + ZeroOffset, PadTop + PlotHeight);

    /// <summary>Gridlines run across the value axis, so they are horizontal for vertical bars and
    /// vertical for horizontal ones — a gridline parallel to the bars would tell you nothing.</summary>
    private ChartLine[] BuildGridlines() =>
        GridlineOffsets(IsVertical ? PlotHeight : PlotWidth)
            .Select(o => IsVertical
                ? new ChartLine(PadLeft, PadTop + o, PadLeft + PlotWidth, PadTop + o)
                : new ChartLine(PadLeft + o, PadTop, PadLeft + o, PadTop + PlotHeight))
            .ToArray();

    /// <summary>
    /// Value-axis tick labels.
    /// </summary>
    /// <remarks>
    /// For vertical bars these are SVG text in the left gutter — exact alignment by construction, since
    /// they share the gridlines' coordinate system. For horizontal bars the axis becomes an HTML row and
    /// only <see cref="ChartTextMark.Text"/> is read: the row aligns by <c>space-between</c> and
    /// percentage padding, so there are no coordinates to supply. Those are left at zero rather than
    /// computed and thrown away.
    /// </remarks>
    private ChartTextMark[] BuildValueTicks()
    {
        if (ValueAxis is null || !HasData) return [];

        var ticks = TickValues.ToArray();
        var marks = new ChartTextMark[ticks.Length];

        for (var i = 0; i < ticks.Length; i++)
        {
            var text = Format(ticks[i]);

            marks[i] = IsVertical
                ? new ChartTextMark(
                    text,
                    PadLeft - 4,
                    PadTop + PlotHeight - TickFraction(i) * PlotHeight + 3,
                    "end")
                : new ChartTextMark(text, 0, 0, "middle");
        }

        return marks;
    }

    private ChartTextMark[] BuildMarkLabels()
    {
        if (ValueLabels is null || !HasData) return [];

        var marks = new ChartTextMark[Series.Length];

        for (var i = 0; i < Series.Length; i++)
        {
            var bar = BarAt(i);

            marks[i] = new ChartTextMark(
                Format(Series[i]),
                IsVertical ? bar.X + bar.W / 2 : bar.X + bar.W + 3,
                IsVertical ? bar.Y - 3 : bar.Y + bar.H / 2 + 3,
                IsVertical ? "middle" : "start");
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

    /// <summary>One row per bar. Share stays zero — a bar chart's values do not sum to a whole.</summary>
    private ChartLegendEntry[] BuildLegend()
    {
        if (Legend is null || !HasData) return [];

        var rows = new ChartLegendEntry[Series.Length];
        for (var i = 0; i < Series.Length; i++) rows[i] = new ChartLegendEntry(null, LabelAt(i), Series[i], 0);
        return rows;
    }

    private string? RootStyle => BuildRootStyle(
        // Radius is not a token: it goes straight onto each rect's rx attribute, and emitting a
        // --chart-radius nothing reads would be dead surface.
        new StyleVars("chart")
            .Add("axis-color", AxisColor)
            // The HTML label and tick tracks inset by the same amount as the SVG's own padding —
            // percentages of the viewBox width, so they scale with the box exactly as the SVG does. Custom
            // properties rather than classes, because they cross a CSS-scope boundary into the element
            // components.
            .Add("pad-left", $"{N(PadLeft / ViewWidth * 100)}%")
            .Add("pad-right", $"{N(PadRight / ViewWidth * 100)}%")
            .ToString());
}
