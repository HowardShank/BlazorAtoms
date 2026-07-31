namespace BlazorAtoms.Equipment;

/// <summary>Which way an <see cref="AtomFan"/>'s blades turn — the same forward/reverse switch a
/// real ceiling fan has, kept separate from <see cref="FanSpeed"/> since it is set by its own
/// hardware, not cycled by the same control.</summary>
public enum FanDirection
{
    Forward,
    Reverse,
}
