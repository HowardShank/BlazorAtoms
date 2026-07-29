namespace BlazorAtoms.Badges;

/// <summary>Preset color scheme for the badge and the chip/tag/pill family. Sets the default
/// background/text/border tokens; explicit <c>Background</c>/<c>TextColor</c>/<c>BorderColor</c>
/// parameters still override it.</summary>
/// <remarks>
/// Prefixed <c>Badge*</c> so a page that <c>@using</c>s this package alongside another with its own
/// color scheme (e.g. <c>BlazorAtoms.Buttons</c>'s <c>ButtonVariant</c>) has no ambiguous
/// <c>Variant</c>. The parameter on each component is still called <c>Variant</c>.
/// </remarks>
public enum BadgeVariant
{
    /// <summary>Neutral scheme-aware default.</summary>
    Default,
    Info,
    Success,
    Warning,
    Danger,
}
