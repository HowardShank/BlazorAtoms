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

    /// <summary>First segment's color when auto-generating from <see cref="SegmentCount"/>. Hex
    /// (<c>#e5484d</c>/<c>e5484d</c>, 3 or 6 digit) or a standard CSS named color (<c>purple</c>) — the
    /// same shapes <see cref="GaugeColorScale.ResolveHex"/> accepts. Default a red. Anything else (empty,
    /// unrecognized, or a partial string mid-typed into a live-bound input) falls back to the default
    /// rather than erroring. No effect when <see cref="Bands"/> is set explicitly.</summary>
    [Parameter] public string? StartColor { get; set; }

    /// <summary>Last segment's color when auto-generating from <see cref="SegmentCount"/>. Same accepted
    /// shapes as <see cref="StartColor"/>. Default a green. No effect when <see cref="Bands"/> is set
    /// explicitly.</summary>
    [Parameter] public string? EndColor { get; set; }

    /// <summary>Swaps which end <see cref="StartColor"/>/<see cref="EndColor"/> apply to — the scale
    /// sweeps <see cref="EndColor"/>-to-<see cref="StartColor"/> (green→red by default) instead of the
    /// other way round. No effect when <see cref="Bands"/> is set explicitly: an explicit list is already
    /// in whatever order the caller wrote it in.</summary>
    [Parameter] public bool ReverseColors { get; set; }

    /// <summary>The colour the scale actually starts from (<see cref="Min"/>'s end), after
    /// <see cref="ReverseColors"/>. Every auto-generated scale (<see cref="EffectiveBands"/>, and each
    /// concrete gauge's own Gradient/Ticks arc) reads colour through this pair rather than
    /// <see cref="StartColor"/>/<see cref="EndColor"/> directly, so the switch only has to live in one
    /// place. Accepts a named CSS color (e.g. "purple") the same as hex, resolved via
    /// <see cref="GaugeColorScale.ResolveHex"/>, and falls back the same way an unset value would when
    /// the parameter holds neither — a live-bound text input passes every partial keystroke through as
    /// the caller types (e.g. "R" or "Re" on the way to a real color), and those must not crash or flash
    /// black.</summary>
    protected string ResolvedStartColor => ReverseColors ? ValidColorOr(EndColor, "#30a46c") : ValidColorOr(StartColor, "#e5484d");

    /// <summary>The colour the scale actually ends at (<see cref="Max"/>'s end), after
    /// <see cref="ReverseColors"/>.</summary>
    protected string ResolvedEndColor => ReverseColors ? ValidColorOr(StartColor, "#e5484d") : ValidColorOr(EndColor, "#30a46c");

    private static string ValidColorOr(string? color, string fallback) =>
        GaugeColorScale.ResolveHex(color) ?? fallback;

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
        Min, Max, ResolvedStartColor, ResolvedEndColor);

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
