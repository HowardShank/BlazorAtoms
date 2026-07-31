namespace BlazorAtoms.Equipment;

/// <summary>Housing artwork for an <see cref="AtomFan"/>. Blade/spin mechanics are shared; only the
/// surrounding equipment differs.</summary>
public enum FanStyle
{
    /// <summary>Front view: grille cage over the blades, base stand below.</summary>
    Desk,

    /// <summary>Top-down view: bare motor hub and blades, no grille or base.</summary>
    Ceiling,
}
