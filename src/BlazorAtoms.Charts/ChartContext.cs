using System.Globalization;

namespace BlazorAtoms.Charts;

/// <summary>
/// A single piece of text at a computed position, with the anchor that centres it there.
/// </summary>
/// <remarks>
/// <para>Deliberately one type for four jobs — value-axis ticks, on-mark value readouts, donut slice
/// percentages and gauge range labels. All four are "text at an (x, y) with a text-anchor", and the
/// trigonometry that produced the coordinates stays in the chart that owns it. That is what lets the
/// element components be pure presentation: they place nothing and compute nothing.</para>
/// <para><paramref name="Share"/> carries the mark's own magnitude as a percentage where the chart has one
/// — currently only donut slices. It exists so an element can decide to <i>drop</i> a mark it was given
/// (<see cref="AtomChartSliceLabels.MinPercent"/>) without needing to know how the share was computed.
/// Zero everywhere else, which is the same as "no threshold applies".</para>
/// </remarks>
public readonly record struct ChartTextMark(
    string Text,
    double X,
    double Y,
    string Anchor,
    double Share = 0);

/// <summary>A straight line in view units — a gridline or a baseline.</summary>
public readonly record struct ChartLine(double X1, double Y1, double X2, double Y2);

/// <summary>
/// One row of a legend: the swatch colour, the label, the raw value and its share of the total as a
/// percentage.
/// </summary>
/// <remarks>
/// <paramref name="Color"/> is null for charts whose marks all take the inherited series colour, so a
/// legend can fall back to <c>--chart-series-color</c> rather than printing a transparent swatch.
/// <paramref name="Share"/> is 0 when the chart has no meaningful total (a line chart's values do not
/// sum to anything), which is why <see cref="AtomChartLegend.ShowPercent"/> defaults to false.
/// </remarks>
public readonly record struct ChartLegendEntry(string? Color, string? Label, double Value, double Share);

/// <summary>
/// The plot rectangle inside the chart's <c>viewBox</c>, in view units.
/// </summary>
/// <remarks>
/// Elements that draw inside the SVG need this to know what coordinate space they are in — and, for the
/// ones the chart renders inside a translated <c>&lt;g&gt;</c>, that their own coordinates are already
/// relative to <see cref="PadLeft"/>/<see cref="PadTop"/>. Every coordinate handed to an element in a
/// <see cref="ChartTextMark"/> or <see cref="ChartLine"/> is already in the space the element is placed
/// into, so this is for elements that want to size themselves against the plot rather than translate.
/// </remarks>
public readonly record struct ChartPlot(
    double PadLeft,
    double PadTop,
    double Width,
    double Height,
    double ViewWidth,
    double ViewHeight);

/// <summary>
/// Everything a chart element component needs, cascaded by the chart that renders it.
/// </summary>
/// <remarks>
/// <para><b>Rebuilt on every render, cascaded with <c>IsFixed="false"</c>.</b> Same shape as
/// <c>CardContext</c> in <c>BlazorAtoms.Cards</c>, and for the same reason: the elements only ever
/// <i>read</i> from it, so Blazor's own change detection re-renders them when the value changes and
/// there is no registration list, no <c>NotifyChildren</c> loop and no second render pass. That last
/// part is not a nicety — see the "Elements are slots, not registrations" section of
/// <c>DEVELOPMENT.md</c> for why a chart cannot learn about its children during its own render.</para>
/// <para><b>Geometry is precomputed.</b> The chart works out every coordinate with the helpers on
/// <c>AtomSeriesChartBase</c>/<c>AtomCartesianChartBase</c> and hands the results over ready to draw.
/// The elements own their class, their style and the shape of their markup; they own none of the maths.
/// A consequence worth stating: an element cannot change where it is drawn, only how it looks.</para>
/// <para><b>Unused lists stay empty.</b> One context per chart carries every element's data, so there
/// is a single <c>CascadingValue</c> rather than one per element kind. A donut leaves
/// <see cref="ValueTicks"/> empty; a line chart leaves <see cref="SliceLabels"/> empty. An element
/// handed nothing renders nothing, which is also what happens when one is used outside a chart
/// altogether and the cascade is null.</para>
/// </remarks>
public sealed class ChartContext
{
    /// <summary>False when the chart has no values to plot — what <see cref="AtomChartEmptyState"/>
    /// keys off.</summary>
    public bool HasData { get; init; }

    /// <summary>The plot rectangle in view units.</summary>
    public ChartPlot Plot { get; init; }

    /// <summary>The chart's formatter, so an element prints numbers the same way the tooltips do.</summary>
    public Func<double, string> Format { get; init; } = DefaultFormat;

    /// <summary>Which way a bar chart's bars grow. <see cref="ChartOrientation.Vertical"/> for
    /// everything else.</summary>
    public ChartOrientation Orientation { get; init; }

    /// <summary>
    /// True when the value axis is placed inside the SVG and should render as <c>&lt;text&gt;</c>;
    /// false when the chart placed it in an HTML row and it should render as spans.
    /// </summary>
    /// <remarks>
    /// The chart decides, because the choice is forced by which axis the labels align to — see
    /// "Two label mechanisms, chosen by axis" in <c>DEVELOPMENT.md</c>. A vertical axis has to align to
    /// fractions of the height, which CSS cannot express, so it lives in the SVG; a horizontal one
    /// aligns to fractions of the width, where HTML does it exactly and inherits the page font.
    /// </remarks>
    public bool ValueAxisInSvg { get; init; }

    /// <summary>Value-axis tick labels, positioned in the space the chart placed the axis slot into.</summary>
    public IReadOnlyList<ChartTextMark> ValueTicks { get; init; } = [];

    /// <summary>Per-mark value readouts, positioned beside their own mark.</summary>
    public IReadOnlyList<ChartTextMark> MarkLabels { get; init; } = [];

    /// <summary>Donut slice percentages, already filtered by the slice's minimum-share threshold.</summary>
    public IReadOnlyList<ChartTextMark> SliceLabels { get; init; } = [];

    /// <summary>Gauge range labels — <c>Min</c> and <c>Max</c> at the ends of the arc.</summary>
    public IReadOnlyList<ChartTextMark> RangeLabels { get; init; } = [];

    /// <summary>Category (X) labels, read positionally and null-padded, so index N is mark N.</summary>
    public IReadOnlyList<string?> CategoryLabels { get; init; } = [];

    /// <summary>
    /// True when the first and last mark sit exactly at the plot's edges, so their labels should anchor
    /// to those edges rather than centre in their own slot.
    /// </summary>
    /// <remarks>
    /// A line chart's points span <c>XAt(0, width) == 0</c> to <c>XAt(n-1, width) == width</c>, so the end
    /// labels centring in their slot would sit half a slot away from the points they name — hence edge
    /// anchoring there. A bar chart's bars are inset within their slot by <c>BarGap</c> regardless of
    /// position, so every label — first, last, or interior — centres the same way. The chart knows which
    /// geometry it has before it renders; the element does not.
    /// </remarks>
    public bool CategoryLabelsAlignEnds { get; init; }

    /// <summary>Gridline coordinates, at the interior tick positions.</summary>
    public IReadOnlyList<ChartLine> Gridlines { get; init; } = [];

    /// <summary>The zero line, or null on a chart that has none.</summary>
    public ChartLine? Baseline { get; init; }

    /// <summary>Legend rows, one per mark the legend should list.</summary>
    public IReadOnlyList<ChartLegendEntry> Legend { get; init; } = [];

    /// <summary>The gauge's formatted value, for <see cref="AtomChartReadout"/>. Null on the charts that
    /// have no single value to read out.</summary>
    public string? ReadoutText { get; init; }

    /// <summary>
    /// How far below centre a gauge readout should sit by default, as a fraction of the box.
    /// </summary>
    /// <remarks>
    /// Sweep-dependent, so it has to come from the chart: a partial dial has a gap at the bottom to
    /// move the readout into, and a full 360° one does not — where a centred readout would render on
    /// top of the needle's hub. <see cref="AtomChartReadout.Offset"/> overrides it.
    /// </remarks>
    public double ReadoutOffset { get; init; }

    /// <summary>Matches <c>AtomSeriesChartBase.Format</c>'s own fallback, so an element used without a
    /// formatter prints what the tooltips print.</summary>
    private static string DefaultFormat(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
