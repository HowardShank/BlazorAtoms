using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// Adds the one series of data — and the arithmetic every plotted chart needs to turn it into
/// coordinates. <see cref="AtomGauge"/> does not inherit this: it plots a single value, so
/// <see cref="Values"/> would be meaningless on it.
/// </summary>
/// <remarks>
/// <para><b>Degenerate input is normal input.</b> A chart's data usually arrives from a query, so empty,
/// single-point and dead-flat series are ordinary states rather than programming errors. Each is handled
/// rather than thrown on: an empty series draws no marks, a single point plots at the vertical middle,
/// and a flat series (every value equal, including all-zero) also plots at the middle — because
/// <c>(v - min) / (max - min)</c> is a division by zero there, which would otherwise reach the markup as
/// <c>NaN</c> coordinates and render nothing at all with no error.</para>
/// <para><b>Labels are advisory.</b> They are only ever read positionally and never sized against
/// <see cref="Values"/>, so a shorter, longer or absent <see cref="Labels"/> is not an error — a mark
/// without a label just falls back to its formatted value. Throwing on a length mismatch would turn a
/// cosmetic problem into an exception on a page that was otherwise fine.</para>
/// </remarks>
public abstract class AtomSeriesChartBase : AtomChartBase
{
    private double[]? _cache;
    private IEnumerable<double>? _cacheSource;

    /// <summary>The series to plot. Null or empty draws no marks.</summary>
    [Parameter] public IEnumerable<double>? Values { get; set; }

    /// <summary>Optional per-point labels, read positionally. Any length is accepted — see the class
    /// remarks.</summary>
    [Parameter] public IEnumerable<string>? Labels { get; set; }

    /// <summary>Formats a value for its <c>&lt;title&gt;</c> and any on-chart readout. Defaults to
    /// invariant round-trip formatting.</summary>
    [Parameter] public Func<double, string>? Formatter { get; set; }

    /// <summary>Lower bound of the plotted range. Null (the default) uses the data's own minimum.</summary>
    [Parameter] public double? Min { get; set; }

    /// <summary>Upper bound of the plotted range. Null (the default) uses the data's own maximum.</summary>
    [Parameter] public double? Max { get; set; }

    /// <summary>Materialised once per distinct <see cref="Values"/> instance: the geometry helpers below
    /// each need the count and random access, and re-enumerating a LINQ query per mark would re-run it.</summary>
    protected double[] Series
    {
        get
        {
            // The _cache null check is not redundant: on the first read of a chart with no Values,
            // _cacheSource and Values are both null, so ReferenceEquals is true and the fill below would
            // be skipped — leaving _cache null and throwing on the very next line. Rendering
            // <AtomSparkline /> with no data is an ordinary thing to do.
            if (_cache is null || !ReferenceEquals(_cacheSource, Values))
            {
                _cacheSource = Values;
                _cache = Values?.ToArray() ?? [];
            }
            return _cache;
        }
    }

    protected bool HasData => Series.Length > 0;

    /// <summary>
    /// Lower bound used when <see cref="Min"/> is not set. The data's own minimum for line-type charts;
    /// <see cref="AtomBarChart"/> overrides it to include zero.
    /// </summary>
    /// <remarks>
    /// A bar's length encodes its value, so the axis has to start at zero — scaled from the data minimum
    /// instead, the smallest bar is always zero-height and every other bar overstates its value. A line
    /// chart is the opposite case: its message is the shape of the change, which zero-basing can flatten
    /// into a straight line.
    /// </remarks>
    protected virtual double DefaultLo => Series.Min();

    /// <summary>
    /// Hook for widening the auto-derived range. The default returns it untouched;
    /// <see cref="AtomCartesianChartBase"/> rounds it outward to a "nice" step so labelled gridlines land
    /// on whole numbers. Only ever applied to bounds the component derived itself.
    /// </summary>
    protected virtual (double Lo, double Hi) AdjustRange(double lo, double hi) => (lo, hi);

    /// <summary>Plotted range. Collapses to a unit span when the data is flat, so callers can divide by
    /// it unconditionally.</summary>
    protected (double Lo, double Hi) Range
    {
        get
        {
            if (!HasData) return (0, 1);
            var lo = Min ?? DefaultLo;
            var hi = Max ?? Series.Max();
            if (hi <= lo) return (lo, lo + 1); // flat series — see the class remarks

            var (aLo, aHi) = AdjustRange(lo, hi);
            // An explicitly supplied bound is a caller's instruction, not a suggestion: rounding it
            // would silently move a range someone chose deliberately.
            if (Min is not null) aLo = Min.Value;
            if (Max is not null) aHi = Max.Value;
            return aHi <= aLo ? (aLo, aLo + 1) : (aLo, aHi);
        }
    }

    /// <summary>Where <paramref name="value"/> sits in <see cref="Range"/>, as 0..1. Clamped, so an
    /// explicit <see cref="Min"/>/<see cref="Max"/> narrower than the data cannot push a mark outside
    /// the plot area.</summary>
    protected double Fraction(double value)
    {
        var (lo, hi) = Range;
        return Math.Clamp((value - lo) / (hi - lo), 0, 1);
    }

    /// <summary>Evenly spaced x for index <paramref name="i"/> across <paramref name="width"/>. A single
    /// point sits at the middle rather than at x=0, where half the marker would be clipped.</summary>
    protected double XAt(int i, double width) =>
        Series.Length <= 1 ? width / 2 : width * i / (Series.Length - 1);

    /// <summary>Screen y for <paramref name="value"/> — inverted, since SVG y grows downward.</summary>
    protected double YAt(double value, double height) => height - Fraction(value) * height;

    /// <summary>The label at <paramref name="i"/>, or null. Never throws on a short
    /// <see cref="Labels"/>.</summary>
    protected string? LabelAt(int i)
    {
        if (Labels is null) return null;
        if (Labels is IReadOnlyList<string> list) return i < list.Count ? list[i] : null;
        return Labels.Skip(i).FirstOrDefault();
    }

    /// <summary>What a mark's <c>&lt;title&gt;</c> says: "label: value", or just the value.</summary>
    protected string TitleAt(int i)
    {
        var text = Format(Series[i]);
        var label = LabelAt(i);
        return string.IsNullOrEmpty(label) ? text : $"{label}: {text}";
    }

    protected string Format(double v) =>
        Formatter?.Invoke(v) ?? v.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Generated fallback name for the whole graphic. A screen reader given only the marks has
    /// nothing to work with, so state the shape of the data.</summary>
    protected string SeriesSummary(string kind) => HasData
        ? $"{kind} of {Series.Length} values from {Format(Series.Min())} to {Format(Series.Max())}"
        : $"empty {kind}";

    /// <summary>
    /// The series as an SVG path through <paramref name="width"/> × <paramref name="height"/>. Straight
    /// segments, or a Catmull-Rom spline converted to cubic Béziers when <paramref name="smooth"/> is set.
    /// </summary>
    /// <remarks>
    /// Catmull-Rom is the right curve here because it passes <i>through</i> every data point — a plain
    /// Bézier through the values as control points would smooth the line by pulling it away from the data
    /// it is meant to report. The end tangents duplicate the end points, so the curve neither overshoots
    /// the first and last values nor needs phantom data beyond them.
    /// </remarks>
    protected string LinePath(double width, double height, bool smooth)
    {
        if (!HasData) return "";

        var p = new (double X, double Y)[Series.Length];
        for (var i = 0; i < Series.Length; i++) p[i] = (XAt(i, width), YAt(Series[i], height));

        var sb = new System.Text.StringBuilder($"M {N(p[0].X)} {N(p[0].Y)}");
        if (p.Length == 1) return sb.ToString();

        if (!smooth)
        {
            for (var i = 1; i < p.Length; i++) sb.Append($" L {N(p[i].X)} {N(p[i].Y)}");
            return sb.ToString();
        }

        for (var i = 0; i < p.Length - 1; i++)
        {
            var p0 = p[Math.Max(i - 1, 0)];
            var p1 = p[i];
            var p2 = p[i + 1];
            var p3 = p[Math.Min(i + 2, p.Length - 1)];

            var c1X = p1.X + (p2.X - p0.X) / 6.0;
            var c1Y = p1.Y + (p2.Y - p0.Y) / 6.0;
            var c2X = p2.X - (p3.X - p1.X) / 6.0;
            var c2Y = p2.Y - (p3.Y - p1.Y) / 6.0;

            sb.Append($" C {N(c1X)} {N(c1Y)} {N(c2X)} {N(c2Y)} {N(p2.X)} {N(p2.Y)}");
        }
        return sb.ToString();
    }

    /// <summary>The same path closed down to the baseline, for a filled area. Empty when there is no
    /// data, so the markup carries no stray <c>Z</c>.</summary>
    protected string AreaPath(double width, double height, bool smooth)
    {
        if (!HasData) return "";
        var lastX = XAt(Series.Length - 1, width);
        var firstX = XAt(0, width);
        return $"{LinePath(width, height, smooth)} L {N(lastX)} {N(height)} L {N(firstX)} {N(height)} Z";
    }
}
