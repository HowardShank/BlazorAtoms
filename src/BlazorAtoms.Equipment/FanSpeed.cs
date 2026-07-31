namespace BlazorAtoms.Equipment;

/// <summary>How fast an <see cref="AtomFan"/> spins. Ordered low to high — click/keyboard cycling
/// walks this list and wraps from <see cref="High"/> back to <see cref="Off"/>. Adding a member later
/// (e.g. a Turbo above High) is non-breaking.</summary>
public enum FanSpeed
{
    Off,
    Low,
    Medium,
    High,
}
