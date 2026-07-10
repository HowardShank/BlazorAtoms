namespace BlazorAtoms.Clocks;

/// <summary>Which face each cell of an <see cref="AtomClockStrip"/> shows.</summary>
public enum ClockFace
{
    /// <summary>A digital <see cref="AtomClock"/>.</summary>
    Digital,

    /// <summary>An <see cref="AtomAnalogClock"/> dial.</summary>
    Analog,
}
