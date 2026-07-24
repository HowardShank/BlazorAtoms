namespace BlazorAtoms.Transitions;

/// <summary>Which hover-triggered effect <see cref="AtomHoverEffect"/> plays around its wrapped
/// <see cref="AtomHoverEffect.ChildContent"/>. Extensible family — <see cref="Sparkle"/> is the
/// first member.</summary>
public enum HoverEffect
{
    /// <summary>SVG sparkles pop in at scattered positions around the content, which itself
    /// scales up slightly and gains a colored glow.</summary>
    Sparkle,
}
