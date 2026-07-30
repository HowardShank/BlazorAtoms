namespace BlazorAtoms.Equipment;

/// <summary>Housing layout for an <see cref="AtomStoplight"/> — a stack of three lamps read
/// top-to-bottom (<see cref="Vertical"/>) or left-to-right (<see cref="Horizontal"/>), always in
/// Red/Yellow/Green order.</summary>
public enum StoplightOrientation
{
    Vertical,
    Horizontal,
}
