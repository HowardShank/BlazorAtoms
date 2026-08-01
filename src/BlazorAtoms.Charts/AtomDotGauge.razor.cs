using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// A single value positioned within a range, as a row of dots growing from a small dim start color to
/// a large full-hue end color, with the dot nearest <see cref="AtomGaugeBase.Value"/> marked active.
/// </summary>
/// <remarks>
/// Sibling of <see cref="AtomGauge"/>/<see cref="AtomBarGauge"/> under <see cref="AtomGaugeBase"/>.
/// Active/inactive styling copies <c>AtomStoplight</c>'s technique directly: each dot's
/// <c>--dot-hue</c> custom property carries its own scale color, and a <c>data-active</c> attribute
/// flips <c>color</c> from a dim <c>color-mix()</c> tint to the full hue — one property flip drives
/// both the fill and its glow, exactly as the stoplight lamps do.
/// </remarks>
public partial class AtomDotGauge : AtomGaugeBase
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private const double Margin = 10;
    private const double MinRadius = 4;
    private const double DotGap = 8;
    private const double PointerSize = 10;

    /// <summary>Which way the row runs. Default <see cref="ChartOrientation.Horizontal"/>.</summary>
    [Parameter] public ChartOrientation Orientation { get; set; } = ChartOrientation.Horizontal;

    /// <summary>Number of dots. Falls back to <see cref="AtomGaugeBase.SegmentCount"/>, then 5.</summary>
    [Parameter] public int? DotCount { get; set; }

    /// <summary>Diameter of the largest (last) dot, in view units. Default 20.</summary>
    [Parameter] public double DotSize { get; set; } = 20;

    /// <summary>Draws a small pointer beneath/beside the dot nearest <see cref="AtomGaugeBase.Value"/>.
    /// Default true.</summary>
    [Parameter] public bool ShowPointer { get; set; } = true;

    /// <summary>Pointer color override → <c>--chart-needle-color</c>.</summary>
    [Parameter] public string? PointerColor { get; set; }

    /// <summary>The value printed beside the row. Put an <see cref="AtomChartReadout"/> here.</summary>
    [Parameter] public RenderFragment? Readout { get; set; }

    /// <summary>Arbitrary content beside the row. Put an <see cref="AtomChartCenter"/> here.</summary>
    [Parameter] public RenderFragment? Center { get; set; }

    /// <summary><c>Min</c> and <c>Max</c> at the row's ends. Put an
    /// <see cref="AtomChartRangeLabels"/> here.</summary>
    [Parameter] public RenderFragment? RangeLabels { get; set; }

    private bool IsVertical => Orientation == ChartOrientation.Vertical;
    private int EffectiveDotCount => Math.Max(2, DotCount ?? SegmentCount ?? 5);
    private double MaxRadius => Math.Max(MinRadius, DotSize / 2);
    private double CenterSpacing => MaxRadius * 2 + DotGap;

    private const double LabelReserve = 16;

    private double AlongTotal => Margin * 2 + MaxRadius * 2 + CenterSpacing * (EffectiveDotCount - 1);
    private double PointerReserve => ShowPointer ? PointerSize + 4 : 0;

    /// <summary>
    /// The row's own cross-axis size — dot diameter, plus the pointer triangle when shown, plus room
    /// for the Min/Max (or segment) labels when a <see cref="RangeLabels"/> slot is filled. Growing this
    /// reserve rather than a fixed offset keeps <see cref="LabelCrossPos"/> inside the viewBox for any
    /// <see cref="DotSize"/>/pointer combination, instead of clipping by a constant amount regardless of
    /// either (the bug this replaces).
    /// </summary>
    private double CrossTotal => Margin * 2 + MaxRadius * 2 + PointerReserve + (RangeLabels is not null ? LabelReserve : 0);

    private double ViewBoxWidth => IsVertical ? CrossTotal : AlongTotal;
    private double ViewBoxHeight => IsVertical ? AlongTotal : CrossTotal;

    private double CrossCenter => Margin + MaxRadius;

    /// <summary>Where a range/segment label sits across the row — always 4 units clear of
    /// <see cref="CrossTotal"/>'s own far edge, however big the dots or pointer are.</summary>
    private double LabelCrossPos => CrossCenter + MaxRadius + PointerReserve + 12;

    private double AlongRaw(int i) => Margin + MaxRadius + i * CenterSpacing;

    /// <summary>Position along the row's own length axis. Vertical reads bottom (index 0, min) to top
    /// (last index, max) — the same convention <c>AtomBattery</c>'s vertical fill and
    /// <c>AtomBarGauge</c>'s vertical track use.</summary>
    private double Along(int i) => IsVertical ? AlongTotal - AlongRaw(i) : AlongRaw(i);

    private double DotCx(int i) => IsVertical ? CrossCenter : Along(i);
    private double DotCy(int i) => IsVertical ? Along(i) : CrossCenter;

    private double Radius(int i) =>
        EffectiveDotCount <= 1 ? MaxRadius : MinRadius + (MaxRadius - MinRadius) * i / (EffectiveDotCount - 1);

    private int ActiveIndex => Math.Clamp((int)Math.Round(Fraction * (EffectiveDotCount - 1)), 0, EffectiveDotCount - 1);

    private IEnumerable<(int Index, double Cx, double Cy, double R, string Color, bool Active)> Dots
    {
        get
        {
            var start = StartColor ?? "#e5484d";
            var end = EndColor ?? "#30a46c";
            var n = EffectiveDotCount;
            var active = ActiveIndex;

            for (var i = 0; i < n; i++)
            {
                var t = n == 1 ? 1.0 : (double)i / (n - 1);
                yield return (i, DotCx(i), DotCy(i), Radius(i), GaugeColorScale.Lerp(start, end, t), i == active);
            }
        }
    }

    protected override string DefaultAriaLabel =>
        $"dot gauge showing {Format(ClampedValue)} of {Format(Min)} to {Format(Max)}";

    private static string Fmt(double v) => v.ToString("F2", Inv);

    /// <summary>Small pointer triangle, apex touching the active dot's edge.</summary>
    private string PointerPath
    {
        get
        {
            var along = Along(ActiveIndex);
            var r = Radius(ActiveIndex);

            if (IsVertical)
            {
                var apexX = CrossCenter + r + 2;
                var baseX = apexX + PointerSize;
                return string.Create(Inv, $"M {Fmt(apexX)} {Fmt(along)} " +
                                           $"L {Fmt(baseX)} {Fmt(along - PointerSize / 2)} " +
                                           $"L {Fmt(baseX)} {Fmt(along + PointerSize / 2)} Z");
            }

            var apexY = CrossCenter + r + 2;
            var baseY = apexY + PointerSize;
            return string.Create(Inv, $"M {Fmt(along)} {Fmt(apexY)} " +
                                       $"L {Fmt(along - PointerSize / 2)} {Fmt(baseY)} " +
                                       $"L {Fmt(along + PointerSize / 2)} {Fmt(baseY)} Z");
        }
    }

    private ChartTextMark[] BuildRangeLabels()
    {
        if (RangeLabels is null) return [];

        if (SegmentLabels is { Count: > 0 } labels)
        {
            var firstRaw = AlongRaw(0);
            var lastRaw = AlongRaw(EffectiveDotCount - 1);
            var marks = new ChartTextMark[labels.Count];

            for (var i = 0; i < labels.Count; i++)
            {
                var frac = (i + 0.5) / labels.Count;
                var raw = firstRaw + frac * (lastRaw - firstRaw);
                var along = IsVertical ? AlongTotal - raw : raw;

                marks[i] = IsVertical
                    ? new ChartTextMark(labels[i], LabelCrossPos, along, "start")
                    : new ChartTextMark(labels[i], along, LabelCrossPos, "middle");
            }

            return marks;
        }

        var firstAlong = Along(0);
        var lastAlong = Along(EffectiveDotCount - 1);

        return IsVertical
            ?
            [
                new ChartTextMark(Format(Min), LabelCrossPos, firstAlong, "start"),
                new ChartTextMark(Format(Max), LabelCrossPos, lastAlong, "start"),
            ]
            :
            [
                new ChartTextMark(Format(Min), firstAlong, LabelCrossPos, "middle"),
                new ChartTextMark(Format(Max), lastAlong, LabelCrossPos, "middle"),
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
        new StyleVars("chart").Add("needle-color", PointerColor).ToString());
}
