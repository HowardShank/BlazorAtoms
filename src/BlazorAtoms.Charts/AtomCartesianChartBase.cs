using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// Chrome for the two charts that have axes to hang it on: <see cref="AtomLineChart"/> and
/// <see cref="AtomBarChart"/>.
/// </summary>
/// <remarks>
/// <para>Not on <see cref="AtomSeriesChartBase"/>, because a gridline means nothing on a donut and
/// nothing on a sparkline either — the sparkline's whole premise is a bare trend line with no chrome, so
/// a gridline slot it ignored would be a parameter that lies.</para>
/// <para>Every piece of that chrome is an opt-in element in its own slot rather than a boolean. What stays
/// here is only what the chart must know before it renders: the tick model
/// (<see cref="GridlineCount"/>, <see cref="NiceScale"/>), which decides the plotted range itself, and
/// <see cref="ValueAxisWidth"/>, which is the gutter the plot is inset by.</para>
/// </remarks>
public abstract class AtomCartesianChartBase : AtomSeriesChartBase
{
    /// <summary>The zero line. Put an <see cref="AtomChartBaseline"/> here.</summary>
    [Parameter] public RenderFragment? Baseline { get; set; }

    /// <summary>Rules across the plot. Put an <see cref="AtomChartGridlines"/> here.</summary>
    [Parameter] public RenderFragment? Gridlines { get; set; }

    /// <summary>Per-mark value readouts. Put an <see cref="AtomChartValueLabels"/> here.</summary>
    [Parameter] public RenderFragment? ValueLabels { get; set; }

    /// <summary>The labelled value axis. Put an <see cref="AtomChartValueAxis"/> here.</summary>
    /// <remarks>Filling this slot is what reserves <see cref="ValueAxisWidth"/> for the labels, so the
    /// chart reads it before it computes any geometry.</remarks>
    [Parameter] public RenderFragment? ValueAxis { get; set; }

    /// <summary>The row of category labels beside the marks. Put an
    /// <see cref="AtomChartCategoryAxis"/> here.</summary>
    /// <remarks><see cref="AtomSeriesChartBase.Labels"/> supplies the text either way — it also names each
    /// mark's tooltip. This slot is what puts them on the axis.</remarks>
    [Parameter] public RenderFragment? CategoryAxis { get; set; }

    /// <summary>Name for the value axis. Put an <see cref="AtomChartAxisTitle"/> here.</summary>
    [Parameter] public RenderFragment? ValueAxisTitle { get; set; }

    /// <summary>Name for the category axis. Put an <see cref="AtomChartAxisTitle"/> here.</summary>
    [Parameter] public RenderFragment? CategoryAxisTitle { get; set; }

    /// <summary>How many gridlines to aim for, which is also how many intervals the value axis is
    /// divided into. Clamped to 0..20 — beyond that they read as a solid block rather than as guides.
    /// Default 4.</summary>
    /// <remarks>On the chart rather than on <see cref="AtomChartGridlines"/> because it feeds
    /// <see cref="NiceScale"/>'s step, which snaps the plotted range: it moves the marks, not just the
    /// rules. It also has to agree with the axis labels, and one parameter cannot be owned by two
    /// elements.</remarks>
    [Parameter] public int GridlineCount { get; set; } = 4;

    /// <summary>Colour of baseline and gridlines → <c>--chart-axis-color</c>.</summary>
    [Parameter] public string? AxisColor { get; set; }

    /// <summary>
    /// Width of the value-axis gutter in view units, when <see cref="ValueAxis"/> is filled. Default 30.
    /// </summary>
    /// <remarks>
    /// Here and not on <see cref="AtomChartValueAxis"/>, because this <i>is</i> the plot's left padding:
    /// the chart has to reserve it before it lays out a single mark, and a parent cannot read a child
    /// component's parameters during its own render pass. Widen it for long labels — a formatter emitting
    /// currency or thousands separators will overflow 30.
    /// </remarks>
    [Parameter] public double? ValueAxisWidth { get; set; }

    /// <summary>Gutter actually reserved: zero unless the value axis slot is filled.</summary>
    protected double EffectiveValueAxisWidth =>
        ValueAxis is null ? 0 : Math.Max(0, ValueAxisWidth ?? DefaultValueAxisWidth);

    /// <summary>Wide enough for four digits at the 9px label size, with a little air before the plot.</summary>
    protected const double DefaultValueAxisWidth = 30;

    /// <summary>
    /// Rounds the auto-derived range outward to a 1/2/5 × 10ⁿ step, so ticks land on whole numbers.
    /// Default true.
    /// </summary>
    /// <remarks>
    /// This is what makes <see cref="AtomChartValueAxis"/> worth having. Evenly dividing the raw data range
    /// gives ticks like 5.6 / 11.2 / 16.8 — technically correct and unreadable. The cost is that the axis
    /// extends slightly past the data (a 28 maximum plots to 30), which is how charts normally behave but
    /// is worth knowing when comparing pixel output. An explicit <c>Min</c>/<c>Max</c> is never rounded.
    /// </remarks>
    [Parameter] public bool NiceScale { get; set; } = true;

    protected int EffectiveGridlineCount => Math.Clamp(GridlineCount, 0, 20);

    /// <summary>How many intervals the value axis is divided into: one more than the gridline count,
    /// since <c>n</c> lines cut the span into <c>n + 1</c> pieces.</summary>
    private int TickIntervals => Math.Max(1, EffectiveGridlineCount + 1);

    protected override (double Lo, double Hi) AdjustRange(double lo, double hi)
    {
        if (!NiceScale) return (lo, hi);

        var step = NiceStep(hi - lo, TickIntervals);
        if (step <= 0) return (lo, hi);

        return (SnapDown(lo, step), SnapUp(hi, step));
    }

    /// <summary>
    /// <c>floor</c> / <c>ceil</c> onto a step multiple, but treating a value already within floating-point
    /// dust of one as exactly on it.
    /// </summary>
    /// <remarks>
    /// Naive rounding is wrong here in a way that only shows on already-tidy data: <c>0.07 / 0.01</c> is
    /// <c>7.000000000000001</c>, so <c>Math.Ceiling</c> returns 8 and the axis gains a whole extra step
    /// above the data. The mirror case (<c>0.3 / 0.1</c> = <c>2.9999999999999996</c>) loses a step below it.
    /// </remarks>
    private static double SnapDown(double v, double step)
    {
        var q = v / step;
        var nearest = Math.Round(q);
        return (Math.Abs(q - nearest) < 1e-9 ? nearest : Math.Floor(q)) * step;
    }

    private static double SnapUp(double v, double step)
    {
        var q = v / step;
        var nearest = Math.Round(q);
        return (Math.Abs(q - nearest) < 1e-9 ? nearest : Math.Ceiling(q)) * step;
    }

    /// <summary>
    /// The 1/2/5 × 10ⁿ step nearest to <paramref name="span"/> / <paramref name="intervals"/>. Those three
    /// mantissas are the ones people read fluently — 2.5 or 3 are arithmetically fine and cognitively
    /// worse.
    /// </summary>
    private static double NiceStep(double span, int intervals)
    {
        if (span <= 0 || intervals <= 0 || double.IsNaN(span) || double.IsInfinity(span)) return 0;

        var rough = span / intervals;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        if (magnitude <= 0 || double.IsInfinity(magnitude)) return 0;

        var normalised = rough / magnitude;
        var nice = normalised <= 1 ? 1 : normalised <= 2 ? 2 : normalised <= 5 ? 5 : 10;
        return nice * magnitude;
    }

    /// <summary>
    /// Spacing between ticks: a nice value under <see cref="NiceScale"/>, or an even division of the raw
    /// range otherwise.
    /// </summary>
    /// <remarks>
    /// Recomputed from the <i>already-snapped</i> <see cref="Range"/> and agrees with the step used to snap
    /// it, because snapping only ever moves a bound onto a multiple of that same step.
    /// </remarks>
    private double TickStep
    {
        get
        {
            var (lo, hi) = Range;
            var span = hi - lo;
            if (span <= 0) return 0;
            if (!NiceScale) return span / TickIntervals;

            var step = NiceStep(span, TickIntervals);
            if (step <= 0) return span / TickIntervals;

            // Only use the nice step if it actually divides the span. It won't when a bound was supplied
            // explicitly, because those are never snapped — and stepping past an explicit Max would print
            // tick labels above the top of the caller's own axis.
            var intervals = span / step;
            return Math.Abs(intervals - Math.Round(intervals)) < 1e-9 ? step : span / TickIntervals;
        }
    }

    /// <summary>
    /// How many intervals the axis actually has — the span divided by the step, which is <b>not</b>
    /// necessarily <see cref="GridlineCount"/> + 1.
    /// </summary>
    /// <remarks>
    /// This is the part that makes <see cref="GridlineCount"/> a target rather than a promise while
    /// <see cref="NiceScale"/> is on, and it is not optional: fixing the count instead would put ticks
    /// between step multiples. A −13..46 series snaps to −20..60, and dividing that span into exactly 5
    /// gives a tick every 16 — labels reading −20, −4, 12, 28, which is worse than the raw data was. With
    /// <c>NiceScale="false"</c> the count is honoured exactly and the values are whatever they are.
    /// </remarks>
    protected int ActualIntervals
    {
        get
        {
            var step = TickStep;
            if (step <= 0) return TickIntervals;
            var (lo, hi) = Range;
            return Math.Max(1, (int)Math.Round((hi - lo) / step));
        }
    }

    /// <summary>Tick values low to high, inclusive of both ends. Empty when there is no data, so an empty
    /// chart shows no axis.</summary>
    protected IEnumerable<double> TickValues
    {
        get
        {
            if (!HasData) yield break;
            var (lo, _) = Range;
            var step = TickStep;
            var count = ActualIntervals;
            for (var i = 0; i <= count; i++) yield return lo + step * i;
        }
    }

    /// <summary>Where a tick sits along the value axis as 0..1, measured from the low end.</summary>
    protected double TickFraction(int index) => (double)index / ActualIntervals;

    /// <summary>
    /// Gridline offsets across <paramref name="height"/>, at every tick position except the low end — that
    /// one is the baseline's job, and drawing both would double the line under the same value.
    /// </summary>
    /// <remarks>
    /// Driven by <see cref="ActualIntervals"/> rather than <see cref="GridlineCount"/> so that every line
    /// sits under a tick label. Gridlines and axis labels disagreeing by even one position is the kind of
    /// thing that looks like a rendering bug rather than a rounding choice — which is exactly what the top
    /// tick having no line looked like before this included it.
    /// <para>Empty when the <see cref="Gridlines"/> slot is not filled, so a chart with no gridline element
    /// does not compute coordinates nothing will draw.</para>
    /// </remarks>
    protected IEnumerable<double> GridlineOffsets(double height)
    {
        if (Gridlines is null || EffectiveGridlineCount == 0) yield break;

        var count = ActualIntervals;
        for (var i = 1; i <= count; i++) yield return height - height * i / count;
    }
}
