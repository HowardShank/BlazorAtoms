namespace BlazorAtoms.Inputs;

/// <summary>
/// Opt-in motion for a field, driven entirely by CSS (<c>:focus-within</c> / <c>[data-state]</c>) —
/// no C# trigger state, so it behaves identically in every render mode. Emitted as
/// <c>data-effect</c> on the component root (omitted for <see cref="None"/>, so the default costs
/// nothing). Adding an effect is one enum member plus one CSS block.
/// </summary>
public enum InputEffect
{
    /// <summary>No motion beyond the field's own border/background transition. Default.</summary>
    None,

    /// <summary>Soft colored halo grows around the field on focus.</summary>
    FocusGlow,

    /// <summary>Field lifts a hair and casts a shadow on focus.</summary>
    FocusRaise,

    /// <summary>Accent rule wipes in under the field on focus.</summary>
    FocusUnderline,

    /// <summary>Field shakes once when it enters the validation-error state.</summary>
    ShakeOnError,
}
