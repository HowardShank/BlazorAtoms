using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Progress;

/// <summary>
/// Shared surface for every progress indicator in this library: the label/readout params, the three
/// styling axes (<see cref="Variant"/>/<see cref="Size"/>/<see cref="Effect"/>) emitted as
/// <c>data-*</c>, and the <c>--progress-*</c> custom properties each component's CSS reads.
/// </summary>
/// <remarks>
/// <para>Deliberately split from <see cref="AtomProgressValueBase"/>: <see cref="AtomProgressSteps"/>
/// has no continuous value, so <c>Value</c>/<c>Min</c>/<c>Max</c> would be dead parameters on it —
/// the same rule that keeps the hover-reveal cards separate components instead of one effect enum.
/// Everything genuinely common to all four lives here.</para>
/// <para><b>Theming priority</b>, lowest to highest: the CSS defaults block in each component's
/// <c>.razor.css</c> → <c>[data-variant]</c>/<c>[data-size]</c> rules → the <c>--progress-*</c>
/// parameters below (inline, so they beat both) → the caller's <c>Style</c> (appended last).</para>
/// <para>Every component here is JS-free. <see cref="AtomScrollProgressBar"/> is the library's one
/// JS-using component and shares none of this base — it has no <c>Value</c> at all, being driven by
/// scroll position.</para>
/// </remarks>
public abstract class AtomProgressBase : AtomComponentBase
{
    // ---- structure ---------------------------------------------------------------------------

    /// <summary>Caption shown above (or beside) the indicator. Omitted entirely when null.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>When true, renders the formatted value readout. Default false.</summary>
    [Parameter] public bool ShowValue { get; set; }

    /// <summary>Accessible name for the indicator. Falls back to <see cref="Label"/>, then to a
    /// per-component default, so the control is never unnamed.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    // ---- styling axes -----------------------------------------------------------------------

    /// <summary>Color scheme → <c>data-variant</c>. Default <see cref="ProgressVariant.Primary"/>.</summary>
    [Parameter] public ProgressVariant Variant { get; set; } = ProgressVariant.Primary;

    /// <summary>Density preset → <c>data-size</c>. Default <see cref="ProgressSize.Medium"/>.</summary>
    [Parameter] public ProgressSize Size { get; set; } = ProgressSize.Medium;

    /// <summary>Opt-in CSS motion/texture → <c>data-effect</c>. Default
    /// <see cref="ProgressEffect.None"/> (no attribute emitted).</summary>
    [Parameter] public ProgressEffect Effect { get; set; } = ProgressEffect.None;

    // ---- theming (→ --progress-* custom properties) ------------------------------------------

    /// <summary>Track thickness in px → <c>--progress-thickness</c>. Overrides the
    /// <see cref="Size"/> default. On the ring this is the stroke width; on the steps it is the
    /// connector thickness.</summary>
    [Parameter] public double? Thickness { get; set; }

    /// <summary>Unfilled track color (any CSS color) → <c>--progress-track-color</c>.</summary>
    [Parameter] public string? TrackColor { get; set; }

    /// <summary>Filled portion color → <c>--progress-fill-color</c>. Overrides
    /// <see cref="Variant"/>'s accent.</summary>
    [Parameter] public string? FillColor { get; set; }

    /// <summary>Label + readout color → <c>--progress-text-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Font size in px for the label and readout → <c>--progress-font-size</c>.</summary>
    [Parameter] public double? FontSize { get; set; }

    /// <summary>Duration in seconds for both the value transition and any <see cref="Effect"/>
    /// keyframe → <c>--progress-duration</c>. Default is per-component CSS.</summary>
    [Parameter] public double? Duration { get; set; }

    // ---- derived render state ----------------------------------------------------------------

    /// <summary>Accessible name fallback when neither <see cref="AriaLabel"/> nor
    /// <see cref="Label"/> is set.</summary>
    protected abstract string DefaultAriaLabel { get; }

    /// <summary>The name actually rendered — never null, so the control always has one.</summary>
    protected string EffectiveAriaLabel => AriaLabel ?? Label ?? DefaultAriaLabel;

    protected string VariantAttr => Kebab(Variant.ToString());

    protected string SizeAttr => Kebab(Size.ToString());

    /// <summary>Null for <see cref="ProgressEffect.None"/> so the default emits no attribute.</summary>
    protected string? EffectAttr => Effect == ProgressEffect.None ? null : Kebab(Effect.ToString());

    /// <summary>Shared <c>--progress-*</c> block plus the visibility toggle. Derived components pass
    /// their own extra declarations in <paramref name="extra"/> (appended last, so they win).</summary>
    protected string? BuildRootStyle(string? extra = null)
    {
        var vars = new StyleVars("progress")
            .Add("thickness", Thickness)
            .Add("track-color", TrackColor)
            .Add("fill-color", FillColor)
            .Add("text-color", TextColor)
            .Add("font-size", FontSize)
            .Add("duration", Duration is null ? null : Invariant(Duration.Value) + "s")
            .ToString();

        var s = (Visible ? "" : "display:none;") + vars + extra;
        return string.IsNullOrEmpty(s) ? null : s;
    }

    /// <summary>Invariant-culture number formatting. Required for CSS: a locale that writes
    /// <c>0,5</c> would produce an invalid declaration.</summary>
    protected static string Invariant(double v) =>
        v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>PascalCase enum name → kebab-case attribute value (<c>StripesAnimated</c> →
    /// <c>stripes-animated</c>), so multi-word members read as normal CSS attribute selectors.</summary>
    internal static string Kebab(string pascal)
    {
        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        for (var i = 0; i < pascal.Length; i++)
        {
            var c = pascal[i];
            if (char.IsUpper(c) && i > 0) sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
