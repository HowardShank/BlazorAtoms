using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Progress;

/// <summary>
/// A circular determinate progress indicator: an SVG arc drawn over a full-circle track, with an
/// optional centered readout or arbitrary <see cref="CenterContent"/>. A null
/// <see cref="AtomProgressValueBase.Value"/> switches it to a spinning indeterminate arc. Pure
/// SVG + CSS — no JS in any render mode.
/// </summary>
/// <remarks>
/// <para><b>The arc math has no pi in it.</b> Setting <c>pathLength="100"</c> on the circle re-bases
/// its own length onto a 0-100 scale, so <c>stroke-dasharray="100"</c> plus a
/// <c>stroke-dashoffset</c> of <c>100 - percent</c> draws exactly that percentage of the
/// circumference — no matter the radius. The alternative (computing <c>2πr</c> in C#) would also
/// have to be recomputed whenever the radius changed.</para>
/// <para><b>Stroke width resolves in C# here, not CSS</b> — unlike the bar, where
/// <c>--progress-thickness</c> alone is enough. The radius depends on the stroke width (the ring must
/// sit inside the box, or the stroke is clipped at the edges), and putting <c>r</c> in CSS as an SVG2
/// geometry property is not portable yet. So <see cref="AtomProgressBase.Thickness"/> falls back to a
/// per-<c>Size</c> constant in C#; the token is still emitted so effect CSS can read it.</para>
/// </remarks>
public partial class AtomProgressRing : AtomProgressValueBase
{
    /// <summary>Outer width/height of the ring in px. Default 96.</summary>
    [Parameter] public double Diameter { get; set; } = 96;

    /// <summary>End treatment of the arc → SVG <c>stroke-linecap</c>. Default
    /// <see cref="ProgressRingCap.Butt"/>.</summary>
    [Parameter] public ProgressRingCap Cap { get; set; } = ProgressRingCap.Butt;

    /// <summary>Angle in degrees where the arc starts, measured clockwise from 3 o'clock. Default
    /// <c>-90</c> (12 o'clock).</summary>
    [Parameter] public double StartAngle { get; set; } = -90;

    /// <summary>Content for the middle of the ring. Takes precedence over the
    /// <see cref="AtomProgressBase.ShowValue"/> readout, so a caller can put an icon or a
    /// "3 of 7" there instead.</summary>
    [Parameter] public RenderFragment? CenterContent { get; set; }

    /// <inheritdoc />
    protected override string DefaultAriaLabel => "Progress";

    /// <summary>Stroke width in px: the explicit <see cref="AtomProgressBase.Thickness"/>, else a
    /// per-<c>Size</c> default. Clamped so it can never exceed the radius and swallow the hole.</summary>
    private double ResolvedThickness
    {
        get
        {
            var t = Thickness ?? Size switch
            {
                ProgressSize.Small => 6,
                ProgressSize.Large => 12,
                _ => 8,
            };
            return Math.Clamp(t, 0, Diameter / 2);
        }
    }

    private double CenterUnits => Diameter / 2;

    private string ViewBox => $"0 0 {Invariant(Diameter)} {Invariant(Diameter)}";

    private string Center => Invariant(CenterUnits);

    /// <summary>Radius measured to the stroke's centerline, so the stroke's outer edge lands exactly
    /// on the box edge rather than half-outside it.</summary>
    private string RadiusValue => Invariant(Math.Max(0, CenterUnits - ResolvedThickness / 2));

    private string StrokeWidth => Invariant(ResolvedThickness);

    private string CapAttr => Cap == ProgressRingCap.Round ? "round" : "butt";

    /// <summary>Full 100 (an empty arc) while indeterminate — the CSS supplies a fixed dash there and
    /// spins it, so an inline offset would fight the keyframe.</summary>
    private string DashOffset => Invariant(IsIndeterminate ? 100 : Math.Round(100 - Percent, 4));

    private string ArcTransform => $"rotate({Invariant(StartAngle)} {Center} {Center})";

    /// <summary>Re-exports the <i>resolved</i> (defaulted and clamped) stroke width so the effect rules
    /// can read it — the base only emits <c>--progress-thickness</c> when the caller set it explicitly.
    /// No <c>--progress-diameter</c> token: nothing reads it, since the geometry is already on the
    /// SVG's own attributes.</summary>
    private string? RootStyle => BuildRootStyle(
        $"--progress-thickness:{Invariant(ResolvedThickness)}px;");
}
