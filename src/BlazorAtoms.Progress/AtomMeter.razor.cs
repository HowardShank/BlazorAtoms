using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Progress;

/// <summary>
/// A scalar gauge for a measurement that simply *is* what it is — disk used, fuel remaining, a score,
/// a password strength — as opposed to a task advancing toward completion. Renders
/// <c>role="meter"</c> and, when <see cref="Low"/>/<see cref="High"/>/<see cref="Optimum"/> are
/// supplied, classifies the value into a <c>data-level</c> so the fill can recolor itself. Pure CSS —
/// no JS in any render mode.
/// </summary>
/// <remarks>
/// <para><b>Why not the native <c>&lt;meter&gt;</c> element:</b> its bar is drawn by the UA with
/// vendor pseudo-elements that differ per browser (<c>::-webkit-meter-optimum-value</c> vs Firefox's
/// own set) and cannot be themed consistently, let alone carry this library's
/// <c>--progress-*</c> surface or effect keyframes. The <i>semantics</i> are kept — the ARIA role and
/// the value/low/high/optimum model are the native ones, and
/// <see cref="Level"/> reimplements the HTML spec's own three-way classification.</para>
/// <para>A null <see cref="AtomProgressValueBase.Value"/> is a degenerate case here rather than a
/// meaningful state: a meter has nothing to sweep. It renders an empty track and omits
/// <c>aria-valuenow</c>.</para>
/// </remarks>
public partial class AtomMeter : AtomProgressValueBase
{
    /// <summary>Upper bound of the "low" span of the scale. Null (default) means unset.</summary>
    [Parameter] public double? Low { get; set; }

    /// <summary>Lower bound of the "high" span of the scale. Null (default) means unset.</summary>
    [Parameter] public double? High { get; set; }

    /// <summary>Where on the scale the ideal value sits. Its position relative to
    /// <see cref="Low"/>/<see cref="High"/> is what decides which span counts as good — see
    /// <see cref="Level"/>. Null (default) means unset, and no level is emitted.</summary>
    [Parameter] public double? Optimum { get; set; }

    /// <summary>Number of tick divisions drawn over the track (e.g. 5 for a five-segment strength
    /// meter). Null or 1 (default) draws none.</summary>
    [Parameter] public int? Segments { get; set; }

    /// <summary>When true, renders a scale ruler under the track showing <c>Min</c>, <c>Max</c> and
    /// any <see cref="Low"/>/<see cref="High"/> marks at their real positions. Default false.</summary>
    [Parameter] public bool ShowScale { get; set; }

    /// <summary>Overall meter width. Any CSS length. Null (default) fills the parent.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Track corner radius in px → <c>--progress-radius</c>. <c>0</c> squares the track off.
    /// Declared here rather than on <see cref="AtomProgressBase"/> because only this component and
    /// <see cref="AtomProgressBar"/> have a rectangular track to round.</summary>
    [Parameter] public double? Radius { get; set; }

    /// <inheritdoc />
    protected override string DefaultAriaLabel => "Meter";

    /// <summary>
    /// The value's quality band, following the HTML <c>&lt;meter&gt;</c> spec's rules verbatim:
    /// <list type="bullet">
    /// <item><description><see cref="Optimum"/> below <see cref="Low"/> — small is good: at/below
    /// <c>Low</c> is optimum, up to <c>High</c> is suboptimum, above that is
    /// sub-suboptimum.</description></item>
    /// <item><description><see cref="Optimum"/> above <see cref="High"/> — large is good: the mirror
    /// image.</description></item>
    /// <item><description><see cref="Optimum"/> between them — the middle is good: inside
    /// <c>Low</c>..<c>High</c> is optimum, outside is suboptimum, and sub-suboptimum never
    /// occurs.</description></item>
    /// </list>
    /// Null when the value is indeterminate or when <see cref="Optimum"/> was not supplied — with no
    /// stated ideal there is nothing to judge the value against.
    /// </summary>
    private MeterLevel? Level
    {
        get
        {
            if (ClampedValue is not { } v || Optimum is not { } opt) return null;

            // Unset bounds collapse to the ends of the scale, which is also how the native element
            // treats them.
            var low = Low ?? Min;
            var high = High ?? Max;

            if (opt < low)
                return v <= low ? MeterLevel.Optimum
                    : v <= high ? MeterLevel.Suboptimum
                    : MeterLevel.SubSuboptimum;

            if (opt > high)
                return v >= high ? MeterLevel.Optimum
                    : v >= low ? MeterLevel.Suboptimum
                    : MeterLevel.SubSuboptimum;

            return v >= low && v <= high ? MeterLevel.Optimum : MeterLevel.Suboptimum;
        }
    }

    private string? LevelAttr => Level is { } l ? Kebab(l.ToString()) : null;

    /// <summary>Tick overlay as one repeating gradient: a hairline every <c>100/Segments</c> percent.
    /// One node regardless of segment count.</summary>
    private string? TicksStyle
    {
        get
        {
            if (Segments is not { } n || n <= 1) return null;

            var step = Invariant(Math.Round(100d / n, 6));
            return $"background-image:repeating-linear-gradient(90deg," +
                   $"transparent 0,transparent calc({step}% - var(--progress-tick-width))," +
                   $"var(--progress-tick-color) calc({step}% - var(--progress-tick-width))," +
                   $"var(--progress-tick-color) {step}%);";
        }
    }

    /// <summary>Left offset for a scale mark, as a percentage of the span.</summary>
    private string MarkStyle(double at)
    {
        var span = Max - Min;
        var pct = span <= 0 ? 0 : (Math.Clamp(at, Min, Max) - Min) / span * 100;
        return $"inset-inline-start:{Invariant(Math.Round(pct, 4))}%;";
    }

    /// <summary>Scale-ruler text. Uses <see cref="AtomProgressValueBase.Formatter"/> when supplied so
    /// the ruler speaks the same units as the readout ("40 GB", not "40%"); otherwise the raw number,
    /// since a percentage of itself would be meaningless on a ruler.</summary>
    private string ScaleText(double at) =>
        Formatter is not null ? Formatter(at) : Invariant(Math.Round(at, 2));

    private string? RootStyle => BuildRootStyle(
        (Radius is null ? null : $"--progress-radius:{Invariant(Radius.Value)}px;") +
        (Width is null ? null : $"width:{Width};"));
}
