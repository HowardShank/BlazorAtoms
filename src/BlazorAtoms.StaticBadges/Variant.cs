namespace BlazorAtoms.StaticBadges;

/// <summary>Preset color scheme for the badge. Sets the default background/text/border tokens;
/// explicit <c>Background</c>/<c>TextColor</c>/<c>BorderColor</c> parameters still override it.</summary>
public enum Variant
{
    /// <summary>Neutral scheme-aware default.</summary>
    Default,
    Info,
    Success,
    Warning,
    Danger,
}
