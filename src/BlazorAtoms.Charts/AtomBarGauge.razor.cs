using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// A single value positioned within a range, on a straight track: colored bands (or one smooth
/// gradient, or many ticks) end to end, and a triangular pointer marking <see cref="Value"/>'s position
/// along it.
/// </summary>
/// <remarks>
/// Sibling of <see cref="AtomGauge"/> under the same <see cref="AtomGaugeBase"/> — same
/// <c>Value</c>/<c>Min</c>/<c>Max</c>/<c>SegmentCount</c> model, different shape. Unlike the dial, the
/// track always shows the whole range end to end; only the pointer moves. Geometry is linear (bar
/// length × <see cref="AtomGaugeBase.Fraction"/>), not the dial's trig, kept private here for the same
/// reason <see cref="AtomGauge"/> and <c>AtomDonut</c> each keep their own arc math private despite both
/// drawing circles.
/// </remarks>
public partial class AtomBarGauge : AtomGaugeBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private const double Margin = 10;
    private const double TrackLength = 180;
    private const double TrackThickness = 24;
    private const double PointerSize = 13;
    private const int GradientStopCount = 6;

    /// <summary>
    /// How far the colored fill (band/gradient/tick rects) sits inside the track's own rounded rect.
    /// Without this, a fill drawn at the track's exact bounds is a same-size, square-cornered rect
    /// sitting directly on top of it — it hides the track's own tint/border entirely and its sharp
    /// corners overhang the track's rounded ends. <see cref="ClipId"/> clips the fill to an inset
    /// rounded rect instead, so the track's stroke shows as a visible frame on every side.
    /// </summary>
    private const double TrackInset = 2;

    /// <summary>Which way the track runs. Default <see cref="ChartOrientation.Horizontal"/> — a bar
    /// gauge reads left-to-right by default, the opposite of <see cref="AtomBarChart"/>'s bars-rise
    /// default, since this is one continuous track rather than a set of rising columns.</summary>
    [Parameter] public ChartOrientation Orientation { get; set; } = ChartOrientation.Horizontal;

    /// <summary>How the track is colored. Default <see cref="BarGaugeStyle.Segmented"/>.</summary>
    [Parameter] public BarGaugeStyle BarStyle { get; set; } = BarGaugeStyle.Segmented;

    /// <summary>Draws the triangular pointer at <see cref="AtomGaugeBase.Value"/>'s position. Default true.</summary>
    [Parameter] public bool ShowPointer { get; set; } = true;

    /// <summary>Pointer color override → <c>--chart-needle-color</c>.</summary>
    [Parameter] public string? PointerColor { get; set; }

    /// <summary>Tick count for <see cref="BarGaugeStyle.Ticks"/>. Falls back to
    /// <see cref="AtomGaugeBase.SegmentCount"/>, then 10.</summary>
    [Parameter] public int? TickCount { get; set; }

    /// <summary>Track's unfilled color when no bands apply → <c>--chart-track-color</c>.</summary>
    [Parameter] public string? TrackColor { get; set; }

    /// <summary>The value printed over the track. Put an <see cref="AtomChartReadout"/> here.</summary>
    [Parameter] public RenderFragment? Readout { get; set; }

    /// <summary>Arbitrary content over the track. Put an <see cref="AtomChartCenter"/> here.</summary>
    [Parameter] public RenderFragment? Center { get; set; }

    /// <summary><c>Min</c> and <c>Max</c> at the track's ends. Put an
    /// <see cref="AtomChartRangeLabels"/> here.</summary>
    [Parameter] public RenderFragment? RangeLabels { get; set; }

    private bool IsVertical => Orientation == ChartOrientation.Vertical;

    private const double LabelReserve = 16;

    private double AlongTotal => Margin * 2 + TrackLength;

    /// <summary>
    /// The track's own cross-axis size — thickness, plus the pointer triangle, plus room for the
    /// Min/Max (or segment) labels when a <see cref="RangeLabels"/> slot is filled. Growing this
    /// reserve rather than hard-coding the labels' own offset keeps them inside the viewBox for any
    /// <see cref="TrackThickness"/>/<see cref="PointerSize"/> combination, instead of clipping by a
    /// constant amount regardless of either (the bug this replaces).
    /// </summary>
    private double CrossTotal => Margin * 2 + TrackThickness + PointerSize + (RangeLabels is not null ? LabelReserve : 0);

    private double ViewBoxWidth => IsVertical ? CrossTotal : AlongTotal;
    private double ViewBoxHeight => IsVertical ? AlongTotal : CrossTotal;

    private double TrackX => Margin;
    private double TrackY => IsVertical ? Margin : Margin + PointerSize;
    private double TrackW => IsVertical ? TrackThickness : TrackLength;
    private double TrackH => IsVertical ? TrackLength : TrackThickness;

    /// <summary>Coordinate along the track's own length axis (x for horizontal, y for vertical) where
    /// the pointer sits. Vertical reads bottom (min) to top (max), the same convention as
    /// <c>AtomBattery</c>'s vertical fill.</summary>
    private double PointerPos => IsVertical
        ? TrackY + TrackH - Fraction * TrackH
        : TrackX + Fraction * TrackLength;

    private int EffectiveTickCount => Math.Max(2, TickCount ?? SegmentCount ?? 10);

    private readonly string _gradientId = $"bg-{Guid.NewGuid():N}";
    private readonly string _clipId = $"bgc-{Guid.NewGuid():N}";

    protected override string DefaultAriaLabel =>
        $"bar gauge showing {Format(ClampedValue)} of {Format(Min)} to {Format(Max)}";

    private static string Fmt(double v) => v.ToString("F2", Inv);

    /// <summary>Bands as rects along the track, in the same length-fraction terms
    /// <see cref="AtomGauge"/>'s <c>BandArcs</c> uses.</summary>
    private IEnumerable<(double Offset, double Length, string Color, string Title)> BandRects
    {
        get
        {
            if (EffectiveBands is null) yield break;

            var span = Max - Min;
            if (span <= 0) yield break;

            var from = Min;
            foreach (var band in EffectiveBands)
            {
                var to = Math.Clamp(band.UpTo, Min, Max);
                if (to > from && !string.IsNullOrWhiteSpace(band.Color))
                {
                    var offset = (from - Min) / span * TrackLength;
                    var length = (to - from) / span * TrackLength;
                    yield return (offset, length, band.Color, $"{Format(from)} to {Format(to)}");
                }
                from = Math.Max(from, to);
            }
        }
    }

    /// <summary>Fixed-count gradient stops from <see cref="AtomGaugeBase.StartColor"/> to
    /// <see cref="AtomGaugeBase.EndColor"/> — independent of <see cref="AtomGaugeBase.SegmentCount"/>,
    /// so <see cref="BarGaugeStyle.Gradient"/> stays smooth regardless of how many discrete bands the
    /// other styles would draw.</summary>
    private IEnumerable<(double OffsetPercent, string Color)> GradientStops
    {
        get
        {
            var start = ResolvedStartColor;
            var end = ResolvedEndColor;
            for (var i = 0; i < GradientStopCount; i++)
            {
                var t = (double)i / (GradientStopCount - 1);
                yield return (t * 100, GaugeColorScale.Lerp(start, end, t));
            }
        }
    }

    /// <summary>Evenly spaced ticks across the track, colored by the same scale as
    /// <see cref="BarGaugeStyle.Gradient"/> rather than <see cref="EffectiveBands"/> — a tick ruler
    /// reads finer-grained than a handful of bands, so it draws its own scale at
    /// <see cref="EffectiveTickCount"/> resolution.</summary>
    private IEnumerable<(double Along, string Color, bool Active)> Ticks
    {
        get
        {
            var n = EffectiveTickCount;
            var start = ResolvedStartColor;
            var end = ResolvedEndColor;
            var activeIndex = (int)Math.Round(Fraction * (n - 1));

            for (var i = 0; i < n; i++)
            {
                var t = (double)i / (n - 1);
                var along = IsVertical ? TrackY + TrackH - t * TrackH : TrackX + t * TrackLength;
                yield return (along, GaugeColorScale.Lerp(start, end, t), i == activeIndex);
            }
        }
    }

    /// <summary>Pointer triangle path — tip touching the track's near edge, base pointing away from it.</summary>
    private string PointerPath
    {
        get
        {
            if (IsVertical)
            {
                var tipX = TrackX + TrackW;
                var baseX = tipX + PointerSize;
                return string.Create(Inv, $"M {Fmt(tipX)} {Fmt(PointerPos)} " +
                                           $"L {Fmt(baseX)} {Fmt(PointerPos - PointerSize / 2)} " +
                                           $"L {Fmt(baseX)} {Fmt(PointerPos + PointerSize / 2)} Z");
            }

            var tipY = TrackY;
            var baseY = tipY - PointerSize;
            return string.Create(Inv, $"M {Fmt(PointerPos)} {Fmt(tipY)} " +
                                       $"L {Fmt(PointerPos - PointerSize / 2)} {Fmt(baseY)} " +
                                       $"L {Fmt(PointerPos + PointerSize / 2)} {Fmt(baseY)} Z");
        }
    }

    private ChartTextMark[] BuildRangeLabels()
    {
        if (RangeLabels is null) return [];

        if (SegmentLabels is { Count: > 0 } labels)
        {
            var marks = new ChartTextMark[labels.Count];
            for (var i = 0; i < labels.Count; i++)
            {
                var frac = (i + 0.5) / labels.Count;
                marks[i] = IsVertical
                    ? new ChartTextMark(labels[i], TrackX + TrackW + PointerSize + 4, TrackY + TrackH - frac * TrackH, "start")
                    : new ChartTextMark(labels[i], TrackX + frac * TrackLength, TrackY + TrackH + 12, "middle");
            }
            return marks;
        }

        return IsVertical
            ?
            [
                new ChartTextMark(Format(Min), TrackX + TrackW + PointerSize + 4, TrackY + TrackH, "start"),
                new ChartTextMark(Format(Max), TrackX + TrackW + PointerSize + 4, TrackY, "start"),
            ]
            :
            [
                new ChartTextMark(Format(Min), TrackX, TrackY + TrackH + 12, "start"),
                new ChartTextMark(Format(Max), TrackX + TrackLength, TrackY + TrackH + 12, "end"),
            ];
    }

    private ChartContext ChartCtx => new()
    {
        HasData = Max > Min,
        Format = Format,
        Plot = new ChartPlot(0, 0, ViewBoxWidth, ViewBoxHeight, ViewBoxWidth, ViewBoxHeight),
        RangeLabels = BuildRangeLabels(),
        ReadoutText = Format(ClampedValue),
        ReadoutOffset = 0,
    };

    private string? RootStyle => BuildRootStyle(
        new StyleVars("chart")
            .Add("track-color", TrackColor)
            .Add("needle-color", PointerColor)
            .ToString());
}
