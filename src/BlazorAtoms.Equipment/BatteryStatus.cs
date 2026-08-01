namespace BlazorAtoms.Equipment;

/// <summary>A badge drawn over an <see cref="AtomBattery"/>'s body, orthogonal to
/// <see cref="BatteryLevel"/> — the fill keeps showing charge, the badge adds a condition on top of
/// it (plugged in, faulty, unrecognized, etc.), the same way a real battery icon composes a bolt or an
/// exclamation mark over the fill glyph rather than replacing it.</summary>
public enum BatteryStatus
{
    /// <summary>No badge; the fill speaks for itself.</summary>
    None,
    Charging,
    Warning,
    Error,
    Slash,
    Unknown,
    Check,
}
