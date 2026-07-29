namespace BlazorAtoms.Buttons;

/// <summary>
/// Opt-in motion/decoration → <c>data-effect</c> (omitted for <see cref="None"/>, so the default costs
/// nothing). Every member except <see cref="ClickRipple"/> is pure CSS driven by
/// <c>:hover</c>/<c>:active</c>/<c>:focus-visible</c> with no C# state, so it behaves identically in
/// every render mode; all of them are suppressed under <c>prefers-reduced-motion: reduce</c>.
/// Adding an effect is one member here plus one CSS block in <c>AtomButton.razor.css</c>.
/// </summary>
public enum ButtonEffect
{
    /// <summary>Only the standard hover/active color transition. Default.</summary>
    None,

    /// <summary>Sits on a colored bottom edge and travels down into it when pressed, like a physical
    /// key.</summary>
    Press3d,

    /// <summary>Chiselled edges — light top-left, shadowed bottom-right — inverting when pressed.</summary>
    Bevel,

    /// <summary>Animated multi-stop gradient in the border only; the fill stays flat.</summary>
    GradientBorder,

    /// <summary>Full-surface hue rotation through the spectrum, running continuously.</summary>
    Rainbow,

    /// <summary>Carbonation: small bubbles rise across the face on hover.</summary>
    Fizzy,

    /// <summary>Storm: a sweeping highlight plus a flicker, as if lit from outside.</summary>
    Storm,

    /// <summary>Material-style ripple expanding from the pointer. The one member that needs C# — the
    /// click coordinates come from <c>MouseEventArgs.OffsetX/Y</c> (no JS) and a per-click key restarts
    /// the keyframe.</summary>
    ClickRipple,
}
