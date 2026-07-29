namespace BlazorAtoms.Buttons;

/// <summary>
/// Layout axis of an <see cref="AtomButtonGroup"/>. Decides which inner edges are collapsed into a
/// shared seam.
/// </summary>
public enum ButtonGroupOrientation
{
    /// <summary>Buttons in a row; inner left/right radii are flattened. Default.</summary>
    Horizontal,

    /// <summary>Buttons stacked; inner top/bottom radii are flattened.</summary>
    Vertical,
}
