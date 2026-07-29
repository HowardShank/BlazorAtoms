namespace BlazorAtoms.Buttons;

/// <summary>
/// Semantic color scheme. Sets the accent the <see cref="ButtonAppearance"/> paints with; an explicit
/// <c>Background</c>/<c>TextColor</c>/<c>BorderColor</c> still overrides it.
/// </summary>
/// <remarks>
/// Prefixed <c>Button*</c> rather than named plainly (as <c>BlazorAtoms.Badges.Variant</c> is), because
/// a page that <c>@using</c>s both packages would otherwise have an ambiguous <c>Variant</c>.
/// </remarks>
public enum ButtonVariant
{
    /// <summary>Neutral scheme — the default.</summary>
    Default,

    /// <summary>The page's main call to action.</summary>
    Primary,

    Info,
    Success,
    Warning,
    Danger,
}
