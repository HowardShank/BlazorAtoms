using System.Globalization;
using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// A single value positioned within a range, on a dial: optional coloured bands, an optional needle, and
/// a readout in the middle.
/// </summary>
/// <remarks>
/// <para><b>Inherits <see cref="AtomChartBase"/>, not <see cref="AtomSeriesChartBase"/></b> — it plots one
/// number, so <c>Values</c>, <c>Labels</c> and the series geometry helpers would all be dead weight on
/// it. This is the same split that keeps <c>Min</c>/<c>Max</c> off <c>AtomProgressBase</c>.</para>
/// <para><b>Distinct from <c>AtomMeter</c></b> in <c>BlazorAtoms.Progress</c>, which is a themeable
/// re-implementation of the HTML <c>&lt;meter&gt;</c> element complete with its low/high/optimum
/// semantics and <c>role="meter"</c>. A gauge is a graphic: arbitrary bands, a needle, a sweep angle, and
/// <c>role="img"</c> because that is what a dial is. If you want the ARIA measurement semantics, use
/// <c>AtomMeter</c>; if you want a dial, use this.</para>
/// <para>Unlike the series charts, <see cref="Min"/> and <see cref="Max"/> are required in substance —
/// they default to 0 and 100 rather than being inferred, because a single value implies no range of its
/// own.</para>
/// </remarks>
public partial class AtomGauge : AtomChartBase
{
    /// <summary>The value to show. Clamped into <see cref="Min"/>..<see cref="Max"/>, so an out-of-range
    /// reading pins the needle at an end rather than swinging it off the dial.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>Bottom of the dial. Default 0.</summary>
    [Parameter] public double Min { get; set; }

    /// <summary>Top of the dial. Default 100.</summary>
    [Parameter] public double Max { get; set; } = 100;

    /// <summary>How much of a full circle the dial covers, in degrees, centred on 12 o'clock. Default
    /// 240; 180 gives a semicircle. Clamped to 30..360.</summary>
    [Parameter] public double SweepAngle { get; set; } = 240;

    /// <summary>Track thickness in view units (the viewBox is 100×100). Default 12.</summary>
    [Parameter] public double Thickness { get; set; } = 12;

    /// <summary>Coloured zones along the track, read in order — each begins where the previous ended.
    /// Null draws a plain track.</summary>
    [Parameter] public IEnumerable<GaugeBand>? Bands { get; set; }

    /// <summary>Draws a needle pointing at <see cref="Value"/>. Default true.</summary>
    [Parameter] public bool ShowNeedle { get; set; } = true;

    /// <summary>Fills the track up to <see cref="Value"/>. Default false — with a needle and bands it is
    /// usually redundant, but it is the clearer reading when both of those are off.</summary>
    [Parameter] public bool ShowValueArc { get; set; }

    /// <summary>The value printed in the middle. Put an <see cref="AtomChartReadout"/> here.</summary>
    [Parameter] public RenderFragment? Readout { get; set; }

    /// <summary>Arbitrary content in the middle. Put an <see cref="AtomChartCenter"/> here.</summary>
    /// <remarks>Separate from <see cref="Readout"/>, so the two can coexist — a label above the value, say.
    /// They were one parameter before, where supplying content silently suppressed the readout.</remarks>
    [Parameter] public RenderFragment? Center { get; set; }

    /// <summary><c>Min</c> and <c>Max</c> at the ends of the arc. Put an
    /// <see cref="AtomChartRangeLabels"/> here.</summary>
    [Parameter] public RenderFragment? RangeLabels { get; set; }

    /// <summary>Formats the readout and the accessible name. Defaults to invariant formatting.</summary>
    [Parameter] public Func<double, string>? Formatter { get; set; }

    /// <summary>Colour of the unfilled track → <c>--chart-track-color</c>.</summary>
    [Parameter] public string? TrackColor { get; set; }

    /// <summary>Colour of the needle → <c>--chart-needle-color</c>.</summary>
    [Parameter] public string? NeedleColor { get; set; }

    private double EffectiveSweep => Math.Clamp(SweepAngle, 30, 360);
    private double ResolvedThickness => Math.Clamp(Thickness, 0.5, 45);
    private double ArcRadius => 50 - ResolvedThickness / 2;

    /// <summary>The dial's own arc as a share of the full circle, in the 0–100 units
    /// <c>pathLength</c> establishes.</summary>
    private double TrackLength => EffectiveSweep / 360 * 100;

    internal double ClampedValue => Math.Clamp(Value, Min, Math.Max(Min, Max));

    /// <summary>Where the value sits in the range, 0..1. A zero-width range reads as empty rather than
    /// dividing by zero.</summary>
    private double Fraction
    {
        get
        {
            var span = Max - Min;
            return span <= 0 ? 0 : (ClampedValue - Min) / span;
        }
    }

    private double ValueLength => TrackLength * Fraction;

    /// <summary>Needle tip, in view units. Angles are measured from the rotated group's own frame, so 0
    /// here is already the dial's start.</summary>
    private double NeedleAngleRad => Fraction * EffectiveSweep * Math.PI / 180;

    private double NeedleLength => ArcRadius - ResolvedThickness / 2 - 2;

    private double NeedleX => 50 + Math.Cos(NeedleAngleRad) * NeedleLength;
    private double NeedleY => 50 + Math.Sin(NeedleAngleRad) * NeedleLength;

    protected override string DefaultAriaLabel =>
        $"gauge showing {Format(ClampedValue)} of {Format(Min)} to {Format(Max)}";

    private string Format(double v) =>
        Formatter?.Invoke(v) ?? v.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>
    /// Bands as arcs. Each runs from the previous band's edge to its own <see cref="GaugeBand.UpTo"/>,
    /// clamped into the dial's range; bands that fall entirely outside it, or that go backwards, are
    /// skipped rather than drawn inverted.
    /// </summary>
    private IEnumerable<(double Length, double Offset, string Color, string Title)> BandArcs
    {
        get
        {
            if (Bands is null) yield break;

            var span = Max - Min;
            if (span <= 0) yield break;

            var from = Min;
            foreach (var band in Bands)
            {
                var to = Math.Clamp(band.UpTo, Min, Max);
                if (to > from && !string.IsNullOrWhiteSpace(band.Color))
                {
                    var offset = (from - Min) / span * TrackLength;
                    var length = (to - from) / span * TrackLength;
                    yield return (length, offset, band.Color, $"{Format(from)} to {Format(to)}");
                }
                // Advance regardless, so a clamped or out-of-order band cannot make the next one overlap.
                from = Math.Max(from, to);
            }
        }
    }

    /// <summary>
    /// Min and Max at the arc's ends. Placed just inside the track radius and nudged away from the arc so
    /// the glyphs clear the stroke.
    /// </summary>
    /// <remarks>
    /// Absolute angles, since the element that draws these is rendered outside the rotated group: the
    /// group's own rotation plus the position along the sweep.
    /// </remarks>
    private ChartTextMark[] BuildRangeLabels()
    {
        if (RangeLabels is null) return [];

        var radius = ArcRadius - ResolvedThickness / 2 - 4;

        // On a closed dial the two ends are the same point, so a Max label would print on top of the Min
        // one rather than opposite it.
        var ends = EffectiveSweep >= 360
            ? [(Fraction: 0d, Value: Min)]
            : new[] { (Fraction: 0d, Value: Min), (Fraction: 1d, Value: Max) };

        var marks = new ChartTextMark[ends.Length];

        for (var i = 0; i < ends.Length; i++)
        {
            var radians = (-90 - EffectiveSweep / 2 + ends[i].Fraction * EffectiveSweep) * Math.PI / 180;

            marks[i] = new ChartTextMark(
                Format(ends[i].Value),
                50 + Math.Cos(radians) * radius,
                50 + Math.Sin(radians) * radius,
                "middle");
        }

        return marks;
    }

    /// <summary>
    /// The default the readout element uses unless it is given an <c>Offset</c> of its own: centred on a
    /// full circle, pushed down into the dial's gap otherwise.
    /// </summary>
    /// <remarks>
    /// Computed here rather than in the element because it depends on <see cref="SweepAngle"/>, which is the
    /// chart's. The threshold is 340 rather than 360 so a nearly-closed dial, whose remaining gap is too
    /// narrow to hold a number, also stays centred.
    /// </remarks>
    private double DefaultReadoutOffset => EffectiveSweep >= 340 ? 0 : 0.16;

    /// <summary>
    /// What the element components see.
    /// </summary>
    /// <remarks>
    /// <c>HasData</c> is whether the dial has a range to position a value within. A gauge always has a
    /// <see cref="Value"/> — it defaults to 0 — so "are there values" is not the useful question; a
    /// <see cref="Min"/> equal to or above <see cref="Max"/> is the degenerate state, and the one an
    /// empty-state element should cover.
    /// </remarks>
    private ChartContext ChartCtx => new()
    {
        HasData = Max > Min,
        Format = Format,
        Plot = new ChartPlot(0, 0, 100, 100, 100, 100),
        RangeLabels = BuildRangeLabels(),
        ReadoutText = Format(ClampedValue),
        ReadoutOffset = DefaultReadoutOffset,
    };

    private string? RootStyle => BuildRootStyle(
        new StyleVars("chart")
            .Add("track-color", TrackColor)
            .Add("needle-color", NeedleColor)
            // --chart-readout-offset is emitted by AtomChartReadout on its own root instead, so that its
            // Offset parameter can override the sweep-aware default without the chart having to read it.
            .ToString());
}
