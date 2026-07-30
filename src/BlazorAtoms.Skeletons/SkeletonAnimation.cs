namespace BlazorAtoms.Skeletons;

/// <summary>
/// How a skeleton placeholder animates while it waits. Emitted as <c>data-animation</c>.
/// </summary>
/// <remarks>
/// All three are always-on: a skeleton has no trigger and no state, it simply animates for as long as
/// it is rendered. Every member is suppressed under <c>prefers-reduced-motion: reduce</c>, which holds
/// the static base color — so <see cref="None"/> is a design choice, not the accessibility fallback.
/// </remarks>
public enum SkeletonAnimation
{
    /// <summary>A highlight band sweeps across the shape. The default.</summary>
    Shimmer,

    /// <summary>The whole shape fades in and out. Cheaper than <see cref="Shimmer"/> — animates only
    /// <c>opacity</c>, so it stays on the compositor.</summary>
    Pulse,

    /// <summary>No animation; a flat block of the base color. Emits no <c>data-animation</c>.</summary>
    None,
}
