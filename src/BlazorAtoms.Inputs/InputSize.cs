namespace BlazorAtoms.Inputs;

/// <summary>
/// Density preset shared by every <see cref="AtomInputBase{TValue}"/>-derived field. Emitted as
/// <c>data-size</c> on the component root; each member sets the <c>--field-pad-*</c> /
/// <c>--field-control-size</c> hooks, so an explicit <c>FontSize</c>/<c>Width</c> still wins.
/// </summary>
public enum InputSize
{
    Small,
    Medium,
    Large,
}
