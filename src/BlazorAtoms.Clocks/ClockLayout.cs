namespace BlazorAtoms.Clocks;

/// <summary>How <see cref="AtomClockPair"/> arranges its two clocks.</summary>
public enum ClockLayout
{
    /// <summary>Horizontal row, the two clocks separated by a divider.</summary>
    SideBySide,
    /// <summary>Vertical stack, one clock above the other.</summary>
    Stacked,
}
