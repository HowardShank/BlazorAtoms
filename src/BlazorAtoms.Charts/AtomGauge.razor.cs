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
public partial class AtomGauge : AtomGaugeBase
{
    /// <summary>How much of a full circle the dial covers, in degrees, centred on 12 o'clock. Default
    /// 240; 180 gives a semicircle. Clamped to 30..360.</summary>
    [Parameter] public double SweepAngle { get; set; } = 240;

    /// <summary>Track thickness in view units (the viewBox is 100×100). Default 12.</summary>
    [Parameter] public double Thickness { get; set; } = 12;

    /// <summary>Draws a needle pointing at <see cref="Value"/>. Default true.</summary>
    [Parameter] public bool ShowNeedle { get; set; } = true;

    /// <summary>How the arc is colored. Default <see cref="GaugeArcStyle.Segmented"/>.</summary>
    [Parameter] public GaugeArcStyle ArcStyle { get; set; } = GaugeArcStyle.Segmented;

    /// <summary>Tick count for <see cref="GaugeArcStyle.Ticks"/>. Falls back to
    /// <see cref="AtomGaugeBase.SegmentCount"/>, then 20.</summary>
    [Parameter] public int? TickCount { get; set; }

    /// <summary>How the needle is drawn. Default <see cref="GaugeNeedleStyle.Line"/>.</summary>
    [Parameter] public GaugeNeedleStyle NeedleStyle { get; set; } = GaugeNeedleStyle.Line;

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

    /// <summary>Colour of the unfilled track → <c>--chart-track-color</c>.</summary>
    [Parameter] public string? TrackColor { get; set; }

    /// <summary>Colour of the needle → <c>--chart-needle-color</c>.</summary>
    [Parameter] public string? NeedleColor { get; set; }

    /// <summary>Colour of the dial's face plate → <c>--gauge-face-color</c>. Default a light neutral
    /// (dark-mode aware), not a tint of <see cref="TrackColor"/> or <see cref="NeedleColor"/> — the
    /// reference look is a deliberately plain plate.</summary>
    [Parameter] public string? FaceColor { get; set; }

    /// <summary>Colour of the bezel ring → <c>--chart-bezel-color</c>.</summary>
    [Parameter] public string? BezelColor { get; set; }

    /// <summary>Bezel stroke width in view units. Default 1 (a hairline). Several reference dials use
    /// a much heavier framed bezel — turn this up to match.</summary>
    [Parameter] public double BezelWidth { get; set; } = 1;

    /// <summary>Draws a flat rounded platform under the dial — a physically drawn shape, not
    /// <see cref="GaugeElevation"/>'s drop-shadow, for the reference dials that sit on a literal
    /// pedestal rather than merely floating above one. Default false.</summary>
    [Parameter] public bool ShowPedestal { get; set; }

    /// <summary>Value where a bolder "danger zone" arc begins, drawn on top of whatever <see cref="ArcStyle"/>
    /// already draws — a tachometer redline. Null (default) draws no redline.</summary>
    [Parameter] public double? RedlineFrom { get; set; }

    /// <summary>Colour of the redline arc. Null (default) uses a fixed saturated red, independent of
    /// <see cref="AtomGaugeBase.EndColor"/> — a redline reads as a fixed danger colour regardless of
    /// where the rest of the scale's hue sweep ends.</summary>
    [Parameter] public string? RedlineColor { get; set; }

    /// <summary>Draws a major/minor tick ruler with numbers around the arc — a car speedometer's white-face
    /// graduations. Independent of <see cref="ArcStyle"/>/<see cref="AtomGaugeBase.SegmentLabels"/>; the
    /// numbers still need a <c>RangeLabels</c> slot filled to show, same as <c>SegmentLabels</c> does.
    /// Default false.</summary>
    [Parameter] public bool ShowTickRuler { get; set; }

    /// <summary>Number of major (numbered) ticks across the range, including both ends. Default 6 — e.g.
    /// 0/20/40/60/80/100 for the default 0–100 range.</summary>
    [Parameter] public int MajorTickCount { get; set; } = 6;

    /// <summary>Small unlabeled ticks between each pair of major ticks. Default 4.</summary>
    [Parameter] public int MinorTicksPerMajor { get; set; } = 4;

    private double EffectiveSweep => Math.Clamp(SweepAngle, 30, 360);
    private double ResolvedThickness => Math.Clamp(Thickness, 0.5, 45);

    /// <summary>Space reserved outside the band for the bezel ring: the gap between them, half the
    /// bezel's own stroke width (a stroke extends both directions from its radius), and a small
    /// safety margin so the bezel's outer edge never lands exactly on the viewBox boundary.</summary>
    private double BezelReserve => 1 + BezelWidth / 2 + 0.5;

    /// <summary>
    /// Pulled in from the raw 50-unit half-extent by <see cref="BezelReserve"/> — without this, the
    /// band's own outer edge sits exactly at the viewBox boundary (radius 50) with zero room left for
    /// anything drawn further out, and the bezel ring gets clipped by the SVG's own edge on all four
    /// sides. This shrinks the whole dial by a few units so the bezel — whatever <see cref="BezelWidth"/>
    /// the caller sets — always fits inside the 100×100 viewBox.
    /// </summary>
    private double ArcRadius => 50 - ResolvedThickness / 2 - BezelReserve;

    /// <summary>The dial's own arc as a share of the full circle, in the 0–100 units
    /// <c>pathLength</c> establishes.</summary>
    private double TrackLength => EffectiveSweep / 360 * 100;

    private double ValueLength => TrackLength * Fraction;

    /// <summary>
    /// Degrees the needle-group rotates from the dial's own start (0) to <see cref="Value"/>'s position,
    /// applied as a <c>rotate()</c> on the wrapping <c>&lt;g&gt;</c> in the markup — not baked into any
    /// shape's own coordinates below. That split is what makes the needle's motion between values
    /// CSS-animatable (a damped/overshoot ease): every needle style draws itself at a fixed reference
    /// angle (pointing along +X, i.e. <see cref="NeedleDirX"/>/<see cref="NeedleDirY"/> below are
    /// constants), so its own path never changes with <see cref="Value"/> — only the wrapping group's
    /// <c>transform</c> does, which a CSS <c>transition: transform</c> can smoothly interpolate. Baking
    /// the angle into each shape's own trig, as this used to, leaves nothing for CSS to transition: the
    /// path's <c>d</c> attribute would have to jump, not ease, between values.
    /// </summary>
    private double NeedleRotationDeg => Fraction * EffectiveSweep;

    private double NeedleLength => ArcRadius - ResolvedThickness / 2 - 2;

    private double NeedleX => 50 + NeedleLength;
    private const double NeedleY = 50;

    // ---- GaugeNeedleStyle.Tapered geometry ---------------------------------------------------------
    // A triangle from a wide base near the hub to a point at the tip reads as a dart rather than a
    // uniform-width stroke, which is the one thing a CSS override on the plain <line> could never fake.

    private const double TaperedBaseRadius = 5;
    private const double TaperedHalfWidth = 2.4;
    private const double TailLength = 9;
    private const double TailBallRadius = 1.8;
    private const double HubOuterRadius = 5.5;
    private const double HubInnerRadius = 2.5;

    // Fixed at angle 0 (pointing along +X) — see NeedleRotationDeg above for why these are constants
    // rather than a function of Value.
    private const double NeedleDirX = 1;
    private const double NeedleDirY = 0;
    private const double NeedlePerpX = 0;
    private const double NeedlePerpY = 1;

    /// <summary>Short counterweight tail opposite the tip — a real needle pivots, it doesn't just point.</summary>
    private double TailX => 50 - NeedleDirX * TailLength;
    private double TailY => 50 - NeedleDirY * TailLength;

    private string TaperedNeedlePath
    {
        get
        {
            var baseCx = 50 + NeedleDirX * TaperedBaseRadius;
            var baseCy = 50 + NeedleDirY * TaperedBaseRadius;
            var leftX = baseCx + NeedlePerpX * TaperedHalfWidth;
            var leftY = baseCy + NeedlePerpY * TaperedHalfWidth;
            var rightX = baseCx - NeedlePerpX * TaperedHalfWidth;
            var rightY = baseCy - NeedlePerpY * TaperedHalfWidth;

            return $"M {N(leftX)} {N(leftY)} L {N(NeedleX)} {N(NeedleY)} L {N(rightX)} {N(rightY)} Z";
        }
    }

    // ---- GaugeNeedleStyle.Triangle geometry --------------------------------------------------------
    // Short and bold rather than reaching to the tip radius: the reference this copies pivots what
    // reads as a stubby arrowhead directly off the face plate, with no separate hub layer to draw.

    private const double TriangleLength = 22;
    private const double TriangleHalfWidth = 4.5;

    private double TriangleTipX => 50 + NeedleDirX * TriangleLength;
    private double TriangleTipY => 50 + NeedleDirY * TriangleLength;

    private string TriangleNeedlePath
    {
        get
        {
            var leftX = 50 + NeedlePerpX * TriangleHalfWidth;
            var leftY = 50 + NeedlePerpY * TriangleHalfWidth;
            var rightX = 50 - NeedlePerpX * TriangleHalfWidth;
            var rightY = 50 - NeedlePerpY * TriangleHalfWidth;

            return $"M {N(leftX)} {N(leftY)} L {N(TriangleTipX)} {N(TriangleTipY)} L {N(rightX)} {N(rightY)} Z";
        }
    }

    // ---- GaugeNeedleStyle.RimTab geometry ----------------------------------------------------------
    // Not a centre-pivot needle: a small tab that straddles the band's own radius, base just outside
    // it, tip poking into its inner edge — reads as a slider caught on the rim rather than a needle
    // swinging from the middle.

    private const double RimTabHalfWidth = 3.2;
    private const double RimTabOuterMargin = 4;
    private const double RimTabInnerMargin = 1;

    private string RimTabPath
    {
        get
        {
            var outerR = ArcRadius + ResolvedThickness / 2 + RimTabOuterMargin;
            var innerR = ArcRadius - ResolvedThickness / 2 - RimTabInnerMargin;

            var baseCx = 50 + NeedleDirX * outerR;
            var baseCy = 50 + NeedleDirY * outerR;
            var leftX = baseCx + NeedlePerpX * RimTabHalfWidth;
            var leftY = baseCy + NeedlePerpY * RimTabHalfWidth;
            var rightX = baseCx - NeedlePerpX * RimTabHalfWidth;
            var rightY = baseCy - NeedlePerpY * RimTabHalfWidth;

            var tipX = 50 + NeedleDirX * innerR;
            var tipY = 50 + NeedleDirY * innerR;

            return $"M {N(leftX)} {N(leftY)} L {N(tipX)} {N(tipY)} L {N(rightX)} {N(rightY)} Z";
        }
    }

    /// <summary>Radius of the dial's face plate, just inside the ring.</summary>
    private double FaceRadius => Math.Max(0, ArcRadius - ResolvedThickness / 2 - 1);

    /// <summary>Radius of the bezel ring, just outside the band.</summary>
    private double BezelRadius => ArcRadius + ResolvedThickness / 2 + 1;

    // ---- ShowPedestal geometry ---------------------------------------------------------------------
    // A literal drawn platform, not a shadow — drawn first so the dial's own layers cover most of it
    // and only the flat base peeks out beneath, the same way the reference dials sit on one.

    private const double PedestalHeight = 9;
    private double PedestalWidth => Math.Min(70, BezelRadius * 1.5);
    private double PedestalX => 50 - PedestalWidth / 2;
    private double PedestalY => 100 - PedestalHeight - 2;

    protected override string DefaultAriaLabel =>
        $"gauge showing {Format(ClampedValue)} of {Format(Min)} to {Format(Max)}";

    /// <summary>
    /// Bands as arcs. Each runs from the previous band's edge to its own <see cref="GaugeBand.UpTo"/>,
    /// clamped into the dial's range; bands that fall entirely outside it, or that go backwards, are
    /// skipped rather than drawn inverted.
    /// </summary>
    private IEnumerable<(double Length, double Offset, string Color, string Title)> BandArcs
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
                    yield return (length, offset, band.Color, $"{Format(from)} to {Format(to)}");
                }
                // Advance regardless, so a clamped or out-of-order band cannot make the next one overlap.
                from = Math.Max(from, to);
            }
        }
    }

    private const int GradientSliceCount = 60;

    /// <summary>
    /// <see cref="GaugeArcStyle.Gradient"/>'s arc as many equal-width slices across the whole track (not
    /// clamped to <see cref="AtomGaugeBase.EffectiveBands"/>), each colored by <see cref="GaugeColorScale"/>
    /// at its own position — independent of <see cref="AtomGaugeBase.SegmentCount"/>, so the sweep stays
    /// smooth no matter how many discrete bands <see cref="GaugeArcStyle.Segmented"/> would draw.
    /// </summary>
    private IEnumerable<(double Length, double Offset, string Color)> GradientArcs
    {
        get
        {
            var start = StartColor ?? "#e5484d";
            var end = EndColor ?? "#30a46c";
            var sliceLength = TrackLength / GradientSliceCount;

            for (var i = 0; i < GradientSliceCount; i++)
            {
                var t = (double)i / (GradientSliceCount - 1);
                yield return (sliceLength, sliceLength * i, GaugeColorScale.Lerp(start, end, t));
            }
        }
    }

    private int EffectiveTickCount => Math.Max(2, TickCount ?? SegmentCount ?? 20);

    /// <summary>
    /// <see cref="GaugeArcStyle.Ticks"/>'s radial marks, evenly spaced across the sweep and colored by the
    /// same scale <see cref="GradientArcs"/> uses. Angles are measured in the rotated group's own frame —
    /// 0 is the dial's start — since these are drawn inside that group.
    /// </summary>
    private IEnumerable<(double X1, double Y1, double X2, double Y2, string Color, bool Active)> ArcTicks
    {
        get
        {
            var n = EffectiveTickCount;
            var start = StartColor ?? "#e5484d";
            var end = EndColor ?? "#30a46c";
            var activeIndex = (int)Math.Round(Fraction * (n - 1));
            var innerR = ArcRadius - ResolvedThickness / 2;
            var outerR = ArcRadius + ResolvedThickness / 2;

            for (var i = 0; i < n; i++)
            {
                var t = (double)i / (n - 1);
                var angle = t * EffectiveSweep * Math.PI / 180;
                var cos = Math.Cos(angle);
                var sin = Math.Sin(angle);

                yield return (
                    50 + cos * innerR, 50 + sin * innerR,
                    50 + cos * outerR, 50 + sin * outerR,
                    GaugeColorScale.Lerp(start, end, t), i == activeIndex);
            }
        }
    }

    private const double RedlineThickness = 3;
    private const double RedlineGap = 3;

    /// <summary>Radius the redline sits at — inside the band's own inner edge, a slim ring rather than a
    /// thicker overlay on the scale itself.</summary>
    private double RedlineRadius => ArcRadius - ResolvedThickness / 2 - RedlineGap;

    /// <summary>
    /// <see cref="RedlineFrom"/> as a length/offset in <see cref="BandArcs"/>'s own terms — from the
    /// clamped start to <see cref="AtomGaugeBase.Max"/>. Null when <see cref="RedlineFrom"/> is unset or
    /// clamps to nothing (at/past Max).
    /// </summary>
    private (double Length, double Offset)? RedlineArc
    {
        get
        {
            if (RedlineFrom is not double from) return null;

            var span = Max - Min;
            if (span <= 0) return null;

            var clampedFrom = Math.Clamp(from, Min, Max);
            if (clampedFrom >= Max) return null;

            var offset = (clampedFrom - Min) / span * TrackLength;
            var length = (Max - clampedFrom) / span * TrackLength;
            return (length, offset);
        }
    }

    private const double TickRulerMajorLength = 6;
    private const double TickRulerMinorLength = 3;
    private const double TickRulerGap = 2;

    /// <summary>Radius the tick ruler's outer edge sits at — just inside the band's own inner edge, so the
    /// ruler reads as part of the face plate rather than overlapping the coloured arc.</summary>
    private double TickRulerOuterR => ArcRadius - ResolvedThickness / 2 - TickRulerGap;

    /// <summary>
    /// <see cref="ShowTickRuler"/>'s radial marks — a major tick at every <see cref="MajorTickCount"/>th
    /// position with <see cref="MinorTicksPerMajor"/> short unlabeled ticks between each pair. Same
    /// rotated-group frame as <see cref="ArcTicks"/>; the numbers themselves are a separate
    /// <see cref="ChartTextMark"/> list built by <see cref="BuildRangeLabels"/>, since text has to render
    /// outside the rotated group.
    /// </summary>
    private IEnumerable<(double X1, double Y1, double X2, double Y2, bool Major)> TickRulerMarks
    {
        get
        {
            if (!ShowTickRuler) yield break;

            var majors = Math.Max(2, MajorTickCount);
            var minorsPerMajor = Math.Max(0, MinorTicksPerMajor);
            var totalIntervals = (majors - 1) * (minorsPerMajor + 1);
            var outerR = TickRulerOuterR;

            for (var i = 0; i <= totalIntervals; i++)
            {
                var isMajor = i % (minorsPerMajor + 1) == 0;
                var t = (double)i / totalIntervals;
                var angle = t * EffectiveSweep * Math.PI / 180;
                var innerR = outerR - (isMajor ? TickRulerMajorLength : TickRulerMinorLength);
                var cos = Math.Cos(angle);
                var sin = Math.Sin(angle);

                yield return (50 + cos * innerR, 50 + sin * innerR, 50 + cos * outerR, 50 + sin * outerR, isMajor);
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

        // The tick ruler's numbers sit just inside its own tick marks rather than the plain radius the
        // Min/Max/SegmentLabels branches use, so they don't collide with the major ticks' own length.
        var radius = ShowTickRuler
            ? TickRulerOuterR - TickRulerMajorLength - 6
            : ArcRadius - ResolvedThickness / 2 - 4;

        (double Fraction, string Text)[] points;

        if (SegmentLabels is { Count: > 0 } labels)
        {
            // One label per segment midpoint, not per endpoint — e.g. 4 labels sit centred over 4
            // equal-width bands rather than only naming the two ends of the whole dial.
            points = new (double Fraction, string Text)[labels.Count];
            for (var i = 0; i < labels.Count; i++)
                points[i] = ((i + 0.5) / labels.Count, labels[i]);
        }
        else if (ShowTickRuler)
        {
            // One number per major tick — SegmentLabels still takes precedence when both are set, since
            // explicit content beats an auto-generated numeric scale.
            var majors = Math.Max(2, MajorTickCount);
            points = new (double Fraction, string Text)[majors];
            for (var i = 0; i < majors; i++)
            {
                var t = (double)i / (majors - 1);
                points[i] = (t, Format(Min + t * (Max - Min)));
            }
        }
        else
        {
            // On a closed dial the two ends are the same point, so a Max label would print on top of the
            // Min one rather than opposite it.
            points = EffectiveSweep >= 360
                ? [(0d, Format(Min))]
                : [(0d, Format(Min)), (1d, Format(Max))];
        }

        var marks = new ChartTextMark[points.Length];

        for (var i = 0; i < points.Length; i++)
        {
            var radians = (-90 - EffectiveSweep / 2 + points[i].Fraction * EffectiveSweep) * Math.PI / 180;

            marks[i] = new ChartTextMark(
                points[i].Text,
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
            .Add("face-color", FaceColor)
            .Add("bezel-color", BezelColor)
            // --chart-readout-offset is emitted by AtomChartReadout on its own root instead, so that its
            // Offset parameter can override the sweep-aware default without the chart having to read it.
            .ToString());
}
