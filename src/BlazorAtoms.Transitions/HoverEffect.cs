namespace BlazorAtoms.Transitions;

/// <summary>Which hover-triggered effect <see cref="AtomHoverEffect"/> plays around its wrapped
/// <see cref="AtomHoverEffect.ChildContent"/>. Extensible family — every member shares the same
/// parameter surface (arbitrary child content, hover trigger), which is what lets them live on one
/// enum rather than becoming separate components.</summary>
public enum HoverEffect
{
    /// <summary>SVG sparkles pop in at scattered positions around the content, which itself
    /// scales up slightly and gains a colored glow.</summary>
    Sparkle,

    /// <summary>Content tilts in 3D toward the viewer, as though picked up at a corner. Purely
    /// decorative — nothing is revealed, so it works on any content (including a whole card).</summary>
    Tilt,
}
