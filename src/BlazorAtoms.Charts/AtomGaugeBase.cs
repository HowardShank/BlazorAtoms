using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Charts;

/// <summary>
/// What the gauge-family components share: a single <see cref="Value"/> positioned within
/// <see cref="Min"/>..<see cref="Max"/>, formatted, and optionally sliced into red→green
/// <see cref="GaugeBand"/>s. Geometry (arc trig, bar layout, dot spacing) stays private to each
/// concrete component — the same split <see cref="AtomGauge"/> and <see cref="AtomDonut"/> already
/// have despite both drawing arcs, so a dial, a bar and a dot scale can each lay themselves out
/// however their shape needs without a shared geometry method forcing a lowest common denominator.
/// </summary>
public abstract class AtomGaugeBase : AtomChartBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The value to show. Clamped into <see cref="Min"/>..<see cref="Max"/>, so an
    /// out-of-range reading pins at an end rather than running off the shape.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Bottom of the range. Default 0.</summary>
    [Parameter] public double Min { get; set; }

    /// <summary>Top of the range. Default 100.</summary>
    [Parameter] public double Max { get; set; } = 100;

    /// <summary>Formats the readout and the accessible name. Defaults to invariant formatting.</summary>
    [Parameter] public Func<double, string>? Formatter { get; set; }

    /// <summary>Divides <see cref="Min"/>..<see cref="Max"/> into this many equal red→green bands,
    /// unless <see cref="Bands"/> is set explicitly (which always wins). Null (the default) still draws
    /// bands — 4 of them — because every reference "score gauge" is colored out of the box; pass an
    /// explicit empty <see cref="Bands"/> list for the old plain-track look.</summary>
    [Parameter] public int? SegmentCount { get; set; }

    /// <summary>First segment's color when auto-generating from <see cref="SegmentCount"/>. Default a
    /// red. No effect when <see cref="Bands"/> is set explicitly.</summary>
    [Parameter] public string? StartColor { get; set; }

    /// <summary>Last segment's color when auto-generating from <see cref="SegmentCount"/>. Default a
    /// green. No effect when <see cref="Bands"/> is set explicitly.</summary>
    [Parameter] public string? EndColor { get; set; }

    /// <summary>Coloured zones along the range, read in order — each begins where the previous ended.
    /// Set this to hand-build exact bands; leave it null and set <see cref="SegmentCount"/> instead to
    /// have <see cref="EndColor"/>-to-<see cref="StartColor"/> generate them.</summary>
    [Parameter] public IEnumerable<GaugeBand>? Bands { get; set; }

    /// <summary>Drop-shadow/gloss treatment. Default <see cref="GaugeElevation.Floating"/> — the
    /// reference "high quality" look ships by default, with this param as the way back to flat.</summary>
    [Parameter] public GaugeElevation Elevation { get; set; } = GaugeElevation.Floating;

    /// <summary>Labels shown in the <c>RangeLabels</c> slot, one per evenly-spaced segment instead of
    /// just <see cref="Min"/>/<see cref="Max"/> — e.g. <c>["Poor", "Fair", "Good", "Excellent"]</c>.
    /// Null (the default) keeps the two-endpoint labeling every gauge shape starts with.</summary>
    [Parameter] public IReadOnlyList<string>? SegmentLabels { get; set; }

    /// <summary>A dark pill straddling the shape's own bottom edge. Put an <see cref="AtomChartBanner"/>
    /// here — distinct from <c>Caption</c>, which the chart places below everything instead of
    /// overlapping the shape itself.</summary>
    [Parameter] public RenderFragment? Banner { get; set; }

    /// <summary>The value actually drawn: <see cref="Value"/> pinned into <see cref="Min"/>..
    /// <see cref="Max"/>.</summary>
    protected double ClampedValue => Math.Clamp(Value, Min, Math.Max(Min, Max));

    /// <summary>Where the value sits in the range, 0..1. A zero-width range reads as empty rather than
    /// dividing by zero.</summary>
    protected double Fraction
    {
        get
        {
            var span = Max - Min;
            return span <= 0 ? 0 : (ClampedValue - Min) / span;
        }
    }

    /// <summary>Default segment count when <see cref="SegmentCount"/> is unset — matches the
    /// Poor/Fair/Good/Excellent four-band look every reference gauge ships with by default.</summary>
    private const int DefaultSegmentCount = 4;

    /// <summary><see cref="Bands"/> if the caller set it explicitly — an empty list included, which is
    /// how a caller opts out of bands entirely — else <see cref="SegmentCount"/> (or
    /// <see cref="DefaultSegmentCount"/>) equal red→green bands. Never null: a gauge with untouched
    /// defaults is still colored, not a plain track.</summary>
    protected IEnumerable<GaugeBand> EffectiveBands => Bands ?? GaugeColorScale.Bands(
        SegmentCount is int n and > 0 ? n : DefaultSegmentCount,
        Min, Max, StartColor ?? "#e5484d", EndColor ?? "#30a46c");

    protected string Format(double v) =>
        Formatter?.Invoke(v) ?? v.ToString("0.###", Inv);

    /// <summary><c>data-elevation</c> value for the root element.</summary>
    protected string ElevationAttr => Elevation switch
    {
        GaugeElevation.Flat => "flat",
        GaugeElevation.Raised => "raised",
        _ => "floating",
    };
}
