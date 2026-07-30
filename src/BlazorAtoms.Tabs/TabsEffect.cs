namespace BlazorAtoms.Tabs;

/// <summary>
/// Opt-in motion, driven entirely by CSS — no C# trigger state, so it behaves identically in every
/// render mode. Emitted as <c>data-effect</c> on the root (omitted for <see cref="None"/>, so the
/// default costs nothing). All members are <c>prefers-reduced-motion</c> guarded.
/// </summary>
public enum TabsEffect
{
    /// <summary>No motion beyond the tabs' own color transition. Default.</summary>
    None,

    /// <summary>The active panel fades in when the selection changes.</summary>
    FadePanel,

    /// <summary>The active panel slides in from the leading edge.</summary>
    SlidePanel,

    /// <summary>Hovered tabs lift slightly.</summary>
    HoverRaise,

    /// <summary>The active tab casts a halo in the accent color.</summary>
    ActiveGlow,

    /// <summary>The indicator rule wipes out from the middle of the active tab.</summary>
    GrowIndicator,
}
