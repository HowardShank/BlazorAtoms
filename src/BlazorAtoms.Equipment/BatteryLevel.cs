namespace BlazorAtoms.Equipment;

/// <summary>Charge level shown by an <see cref="AtomBattery"/>, as a fixed set of fill steps rather
/// than a continuous percentage — matches the discrete look of the source icon set.</summary>
public enum BatteryLevel
{
    Empty,
    Quarter,
    Half,
    ThreeQuarter,
    Full,
}
