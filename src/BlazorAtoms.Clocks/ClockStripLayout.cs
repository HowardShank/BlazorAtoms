namespace BlazorAtoms.Clocks;

/// <summary>How an <see cref="AtomClockStrip"/> arranges its cells.</summary>
public enum ClockStripLayout
{
    /// <summary>A horizontal row that wraps to the next line as needed.</summary>
    Row,

    /// <summary>A responsive grid of equal-width columns.</summary>
    Grid,

    /// <summary>A single vertical column, one clock per line.</summary>
    Stacked,
}
