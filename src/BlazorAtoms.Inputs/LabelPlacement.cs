namespace BlazorAtoms.Inputs;

/// <summary>
/// Which side of the control its inline caption sits on, for the controls that have one
/// (<see cref="AtomCheckbox"/>, <see cref="AtomSwitch"/>). Independent of the separate
/// column-layout <c>Label</c>, which always renders in its own <c>LabelCol</c>.
/// </summary>
public enum LabelPlacement
{
    /// <summary>Caption before the control.</summary>
    Start,

    /// <summary>Caption after the control. Default — matches native checkbox/switch convention.</summary>
    End,
}
