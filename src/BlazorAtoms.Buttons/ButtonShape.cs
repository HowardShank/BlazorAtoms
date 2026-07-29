namespace BlazorAtoms.Buttons;

/// <summary>
/// Corner treatment → <c>data-shape</c>. Each member is one <c>--btn-radius</c> value; an explicit
/// <c>Radius</c> parameter overrides it.
/// </summary>
public enum ButtonShape
{
    /// <summary>Softly rounded corners. Default.</summary>
    Rounded,

    /// <summary>Hard corners.</summary>
    Square,

    /// <summary>Fully rounded ends.</summary>
    Pill,

    /// <summary>Round. Also squares the box so width tracks height — intended for
    /// <see cref="AtomIconButton"/>, which defaults to it.</summary>
    Circle,
}
