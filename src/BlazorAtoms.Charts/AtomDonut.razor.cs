using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Charts;

/// <summary>
/// Parts of a whole, as a ring. Each value becomes a slice sized by its share of the total.
/// </summary>
/// <remarks>
/// <para><b>Not the same thing as <c>AtomProgressRing</c>,</b> which shows one fraction of a task and
/// belongs to <c>BlazorAtoms.Progress</c>. A donut has many slices, no notion of "complete", and a
/// palette; the two would share almost no parameters, which is why they are separate components in
/// separate packages rather than one with a mode switch.</para>
/// <para><b><see cref="Min"/> and <see cref="Max"/> are ignored</b>, along with
/// <see cref="AtomSeriesChartBase.Formatter"/>'s role in axis scaling: a slice's size is its share of the
/// total, so there is no range to rescale. They are inherited from
/// <see cref="AtomSeriesChartBase"/> because the data itself is; this is the one place in the library
/// where an inherited parameter does nothing, and it is called out here rather than pretended away.
/// Reach for <see cref="AtomGauge"/> if you want a value positioned within a range.</para>
/// <para><b>Negative and zero values.</b> Negatives are meaningless as a share of a whole, so they are
/// dropped rather than drawn as a reversed arc; a total of zero draws only the track. Both are ordinary
/// query results, not caller errors.</para>
/// </remarks>
public partial class AtomDonut : AtomSeriesChartBase
{
    /// <summary>Ring thickness in view units (the viewBox is 100×100, so this reads as a percentage of
    /// the diameter). Default 18, clamped so the ring cannot swallow its own hole.</summary>
    [Parameter] public double Thickness { get; set; } = 18;

    /// <summary>Where the first slice begins, in degrees clockwise from 12 o'clock. Default 0.</summary>
    [Parameter] public double StartAngle { get; set; }

    /// <summary>Gap between slices, in the same 0–100 units as slice length. Default 0.5; set 0 for a
    /// continuous ring.</summary>
    [Parameter] public double PadAngle { get; set; } = 0.5;

    /// <summary>Slice colours, cycled when there are more slices than colours. Falls back to the CSS
    /// palette. Lives here rather than on a base because it is the only chart with several marks to
    /// colour.</summary>
    [Parameter] public IEnumerable<string>? Palette { get; set; }

    /// <summary>Content for the hole. Put an <see cref="AtomChartCenter"/> here.</summary>
    [Parameter] public RenderFragment? Center { get; set; }

    /// <summary>Percentages printed on the ring. Put an <see cref="AtomChartSliceLabels"/> here.</summary>
    [Parameter] public RenderFragment? SliceLabels { get; set; }

    /// <summary>Colour of the unfilled ring behind the slices → <c>--chart-track-color</c>.</summary>
    [Parameter] public string? TrackColor { get; set; }

    private static readonly string[] FallbackPalette =
    [
        "#4e79a7", "#f28e2b", "#e15759", "#76b7b2",
        "#59a14f", "#edc948", "#b07aa1", "#ff9da7",
    ];

    /// <summary>Positive values only — see the class remarks on negatives.</summary>
    private double[] Shares => Series.Where(v => v > 0).ToArray();

    private double Total => Shares.Sum();

    private bool HasTotal => Total > 0;

    /// <summary>Clamped so the stroke cannot exceed the radius, which would invert the hole.</summary>
    private double ResolvedThickness => Math.Clamp(Thickness, 0.5, 45);

    /// <summary>Radius of the arc's centre line: half the box minus half the stroke, so the ring's outer
    /// edge lands exactly on the viewBox edge whatever the thickness.</summary>
    private double ArcRadius => 50 - ResolvedThickness / 2;

    protected override string DefaultAriaLabel =>
        HasTotal
            ? $"donut chart of {Shares.Length} values totalling {Format(Total)}"
            : "empty donut chart";

    private string[] EffectivePalette
    {
        get
        {
            var p = Palette?.Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
            return p is { Length: > 0 } ? p : FallbackPalette;
        }
    }

    /// <summary>
    /// Each slice's arc length and start offset in 0–100 units, plus its colour and tooltip. Offsets
    /// accumulate, so slices sit end to end around the ring.
    /// </summary>
    internal readonly record struct Slice(
        double Length,
        double Offset,
        string Color,
        string Title,
        double Value,
        double Share,
        string? Label)
    {
        /// <summary>Where the slice's midpoint sits around the ring, 0..1 — the anchor for an on-ring label.</summary>
        public double MidFraction => (Offset + Share / 2) / 100;

        public string PercentText =>
            Math.Round(Share, 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
    }

    private IEnumerable<Slice> Slices
    {
        get
        {
            var palette = EffectivePalette;
            var pad = Math.Clamp(PadAngle, 0, 10);
            var offset = 0d;
            var index = 0;

            for (var i = 0; i < Series.Length; i++)
            {
                if (Series[i] <= 0) continue; // dropped, but the label indices still line up below

                var share = Series[i] / Total * 100;
                // Never pad a slice out of existence: a hairline arc still carries its tooltip.
                var length = Math.Max(share - pad, 0.1);
                var label = LabelAt(i);
                var percent = Math.Round(share, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                var title = string.IsNullOrEmpty(label)
                    ? $"{Format(Series[i])} ({percent}%)"
                    : $"{label}: {Format(Series[i])} ({percent}%)";

                yield return new Slice(length, offset, palette[index % palette.Length], title,
                    Series[i], share, label);

                offset += share;
                index++;
            }
        }
    }

    /// <summary>
    /// Percentage labels for the ring, positioned at absolute angles so they stay upright.
    /// </summary>
    /// <remarks>
    /// The angles are absolute — the slice group's own rotation plus the slice's position around the ring —
    /// because the element that draws these is rendered <b>outside</b> that group. Inside it, the rotation
    /// would apply to the text as well and the labels would sit at a tilt that changed with
    /// <see cref="StartAngle"/>.
    /// <para>Every slice gets a mark, with its share attached. The minimum-share threshold is
    /// <see cref="AtomChartSliceLabels.MinPercent"/>'s to apply: dropping a label changes no geometry, so it
    /// belongs to the element.</para>
    /// </remarks>
    private ChartTextMark[] BuildSliceLabels()
    {
        if (SliceLabels is null || !HasTotal) return [];

        var marks = new List<ChartTextMark>();

        foreach (var slice in Slices)
        {
            var degrees = StartAngle - 90 + slice.MidFraction * 360;
            var radians = degrees * Math.PI / 180;

            marks.Add(new ChartTextMark(
                slice.PercentText,
                50 + Math.Cos(radians) * ArcRadius,
                // +2.5 centres a 6px glyph on the arc; SVG has no reliable single-line vertical centring.
                50 + Math.Sin(radians) * ArcRadius + 2.5,
                "middle",
                slice.Share));
        }

        return [.. marks];
    }

    /// <summary>Beside the ring rather than beneath it: a ring leaves that space free, and a square plot
    /// next to a list of keys reads as one unit.</summary>
    protected override ChartLegendPlacement DefaultLegendPlacement => ChartLegendPlacement.End;

    /// <summary>
    /// What the element components see.
    /// </summary>
    /// <remarks>
    /// <c>HasData</c> is <see cref="HasTotal"/>, not the base's "any values at all". A donut whose values
    /// are all zero or negative has data and still cannot draw a ring, so that is the state an empty-state
    /// element should cover — see the class remarks on negatives.
    /// </remarks>
    private ChartContext ChartCtx => new()
    {
        HasData = HasTotal,
        Format = Format,
        Plot = new ChartPlot(0, 0, 100, 100, 100, 100),
        SliceLabels = BuildSliceLabels(),
        Legend = HasTotal
            ? Slices.Select(s => new ChartLegendEntry(s.Color, s.Label, s.Value, s.Share)).ToArray()
            : [],
    };

    private string? RootStyle => BuildRootStyle(
        new StyleVars("chart")
            .Add("track-color", TrackColor)
            .ToString());
}
