namespace BlazorAtoms.Inputs;

/// <summary>
/// Outline shape of <see cref="AtomCheckbox"/>'s box. Emitted as <c>data-shape</c>; each member is
/// one <c>border-radius</c> rule.
/// </summary>
public enum CheckShape
{
    /// <summary>Hard corners.</summary>
    Square,

    /// <summary>Softly rounded corners. Default.</summary>
    Rounded,

    /// <summary>Fully round.</summary>
    Circle,
}
