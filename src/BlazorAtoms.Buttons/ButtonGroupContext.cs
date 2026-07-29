namespace BlazorAtoms.Buttons;

/// <summary>
/// Cascaded by <see cref="AtomButtonGroup"/> so its children inherit the group's styling axes instead
/// of repeating them per button. A child's own explicitly-set parameter always wins — see
/// <see cref="ButtonFamilyBase"/> for how "explicitly set" is detected.
/// </summary>
/// <remarks>
/// Deliberately carries only the four shape/color axes, not behavior: <c>Disabled</c>, <c>Loading</c>,
/// and <c>OnClick</c> stay per-button, because a group-wide disable is rarely what's wanted and would
/// be surprising to override.
/// </remarks>
public sealed class ButtonGroupContext
{
    public ButtonVariant Variant { get; init; }
    public ButtonAppearance Appearance { get; init; }
    public ButtonSize Size { get; init; }
    public ButtonShape Shape { get; init; }
}
