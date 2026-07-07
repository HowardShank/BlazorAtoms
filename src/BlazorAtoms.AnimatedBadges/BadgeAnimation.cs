namespace BlazorAtoms.AnimatedBadges;

/// <summary>Motion applied to the badge. Pair with <see cref="AnimationTrigger"/> to control when
/// it plays. All animation is disabled under <c>prefers-reduced-motion: reduce</c>.</summary>
public enum BadgeAnimation
{
    /// <summary>No motion.</summary>
    None,
    /// <summary>Scale + fade in — the "pop in" entrance.</summary>
    Pop,
    /// <summary>Vertical bounce.</summary>
    Bounce,
    /// <summary>Continuous 360° rotation.</summary>
    Spin,
    /// <summary>Scale pulse in place.</summary>
    Pulse,
    /// <summary>Expanding ring behind the badge (classic "new notification" ping).</summary>
    Ping,
}
