namespace BlazorAtoms.Progress;

/// <summary>Semantic color scheme for the filled portion of a progress indicator. Sets the accent
/// the fill paints with; an explicit <c>FillColor</c> still overrides it.</summary>
/// <remarks>
/// Prefixed <c>Progress*</c> per the repo convention that a cross-package enum name carries its
/// package's noun — <c>BadgeVariant</c>, <c>ButtonVariant</c>, <c>InputVariant</c> and this one would
/// otherwise all be a bare <c>Variant</c>, leaving no unambiguous name for a page that
/// <c>@using</c>s more than one. The parameter on each component is still called <c>Variant</c>.
/// <para>Members deliberately mirror <c>ButtonVariant</c> so the two families theme together.</para>
/// </remarks>
public enum ProgressVariant
{
    /// <summary>Neutral scheme — the default.</summary>
    Default,

    /// <summary>The page's main accent.</summary>
    Primary,

    Info,
    Success,
    Warning,
    Danger,
}
