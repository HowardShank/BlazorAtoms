namespace BlazorAtoms.Buttons;

/// <summary>
/// Density preset → <c>data-size</c>. Sets the <c>--btn-pad-*</c> / <c>--btn-font-size</c> /
/// <c>--btn-height</c> hooks, so an explicit <c>Height</c>/<c>FontSize</c> still wins.
/// </summary>
public enum ButtonSize
{
    Small,
    Medium,
    Large,
}
