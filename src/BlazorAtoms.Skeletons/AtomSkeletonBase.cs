using Microsoft.AspNetCore.Components;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Skeletons;

/// <summary>
/// Shared surface for every skeleton: the animation axis, the <c>--skeleton-*</c> theming tokens, and
/// the accessibility decision described below.
/// </summary>
/// <remarks>
/// <para><b>The family is one painted primitive plus three presets.</b>
/// <see cref="AtomSkeletonBlock"/> is the only component whose stylesheet paints anything;
/// <see cref="AtomSkeletonText"/>, <see cref="AtomSkeletonAvatar"/> and <see cref="AtomSkeletonCard"/>
/// render <c>AtomSkeletonBlock</c> children and contribute layout only. That keeps the shimmer
/// gradient, the pulse keyframe and the <c>prefers-reduced-motion</c> override in exactly one file
/// instead of four — the same call <c>BlazorAtoms.Buttons</c> makes by having
/// <c>AtomIconButton</c> render an <c>AtomButton</c>.</para>
/// <para>A consequence worth knowing: because scoped CSS stamps its scope id on elements written in
/// <i>that</i> component's <c>.razor</c> file, a preset cannot style the block it renders. Presets
/// therefore pass <b>parameters</b> (width, height, radius) rather than classes, and any element a
/// preset needs to style itself is a wrapper it writes in its own markup.</para>
/// <para><b>Accessibility.</b> A skeleton is decorative — it conveys nothing a reader can act on, and
/// the page above it is normally already announcing that it is loading. So the root is
/// <c>aria-hidden="true"</c> by default and contributes nothing to the accessibility tree. Set
/// <see cref="AriaLabel"/> and it becomes a polite live region instead
/// (<c>role="status" aria-live="polite"</c>) with that name. Opt-in rather than default, because six
/// skeletons on a page would otherwise announce six times and compete with the page's own message.</para>
/// </remarks>
public abstract class AtomSkeletonBase : AtomComponentBase
{
    // ---- animation ---------------------------------------------------------------------------

    /// <summary>Placeholder animation → <c>data-animation</c>. Default
    /// <see cref="SkeletonAnimation.Shimmer"/>.</summary>
    [Parameter] public SkeletonAnimation Animation { get; set; } = SkeletonAnimation.Shimmer;

    // ---- accessibility ------------------------------------------------------------------------

    /// <summary>Accessible name. When null (the default) the skeleton is <c>aria-hidden</c>; when set,
    /// the root becomes a polite live region announcing this text. See the remarks on this class.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>When false, hidden via CSS <c>display:none</c> (stays in the DOM, matching the rest of
    /// the repo). Default true.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    // ---- theming (→ --skeleton-* custom properties) --------------------------------------------

    /// <summary>Resting placeholder color (any CSS color) → <c>--skeleton-base-color</c>.</summary>
    [Parameter] public string? BaseColor { get; set; }

    /// <summary>Color of the sweeping highlight band → <c>--skeleton-highlight-color</c>. Read only by
    /// <see cref="SkeletonAnimation.Shimmer"/>; the other two animations never paint it.</summary>
    [Parameter] public string? HighlightColor { get; set; }

    /// <summary>One animation cycle as a CSS duration, e.g. <c>"1.4s"</c> →
    /// <c>--skeleton-duration</c>. A string rather than a number so <c>ms</c> works too.</summary>
    [Parameter] public string? Duration { get; set; }

    // ---- derived render state ------------------------------------------------------------------

    /// <summary>Null for <see cref="SkeletonAnimation.None"/>, so the static look is the CSS default
    /// and no attribute is emitted.</summary>
    protected string? AnimationAttr =>
        Animation == SkeletonAnimation.None ? null : Animation.ToString().ToLowerInvariant();

    /// <summary><c>"status"</c> only when named — see the accessibility remarks.</summary>
    protected string? RoleAttr => AriaLabel is null ? null : "status";

    /// <summary><c>"polite"</c> only when named.</summary>
    protected string? AriaLiveAttr => AriaLabel is null ? null : "polite";

    /// <summary><c>"true"</c> unless named. Mutually exclusive with <see cref="RoleAttr"/>: a live
    /// region that is also <c>aria-hidden</c> announces nothing.</summary>
    protected string? AriaHiddenAttr => AriaLabel is null ? "true" : null;

    /// <summary>Shared <c>--skeleton-*</c> block plus the visibility toggle. Derived components append
    /// their own declarations via <paramref name="extra"/> (last, so they win).</summary>
    protected string? BuildRootStyle(string? extra = null)
    {
        var vars = new StyleVars("skeleton")
            .Add("base-color", BaseColor)
            .Add("highlight-color", HighlightColor)
            .Add("duration", Duration)
            .ToString();

        var s = (Visible ? "" : "display:none;") + vars + extra;
        return string.IsNullOrEmpty(s) ? null : s;
    }
}
