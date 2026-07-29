namespace BlazorAtoms.Cards;

/// <summary>
/// Opt-in hover/active treatment for <see cref="AtomCard"/>, driven entirely by CSS
/// <c>:hover</c>/<c>:active</c>/<c>:focus-visible</c> — no C# state, so it behaves identically in
/// every render mode. Emitted as <c>data-effect</c> (omitted for <see cref="None"/>).
/// </summary>
/// <remarks>
/// These are frame-level treatments only. Anything that <i>reveals</i> content on hover is a
/// different component: see <see cref="AtomCardReveal"/> and its siblings, whose whole purpose is the
/// reveal. A 3D tilt is <c>HoverEffect.Tilt</c> on <c>BlazorAtoms.Transitions</c>, which composes
/// around any card.
/// </remarks>
public enum CardEffect
{
    /// <summary>No hover treatment. Default.</summary>
    None,

    /// <summary>Card rises and its shadow deepens on hover.</summary>
    HoverLift,

    /// <summary>Accent-colored halo grows around the card on hover.</summary>
    HoverGlow,

    /// <summary>Border takes the accent color on hover (draws one even on borderless variants).</summary>
    HoverBorder,

    /// <summary>Card sinks slightly while pressed — pairs with a clickable card.</summary>
    PressSink,
}
