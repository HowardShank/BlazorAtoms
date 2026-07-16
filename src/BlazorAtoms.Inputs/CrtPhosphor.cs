namespace BlazorAtoms.Inputs;

/// <summary>
/// Phosphor color for <see cref="AtomCrtInput"/>. Modeled on real P-series CRT phosphors (P1
/// green, P3 amber, P4 white/blue, plus a red danger variant). Sets the text/glow/caret color;
/// the dark background stays the same across all of them.
/// </summary>
public enum CrtPhosphor
{
    /// <summary>Classic P1 terminal green (Apple II / VT100).</summary>
    Green,

    /// <summary>P3 amber monitor.</summary>
    Amber,

    /// <summary>Cool white/blue-tinted phosphor.</summary>
    Blue,

    /// <summary>Alarm/danger variant.</summary>
    Red,

    /// <summary>Neutral white phosphor (late-era VGA-style).</summary>
    White,
}
