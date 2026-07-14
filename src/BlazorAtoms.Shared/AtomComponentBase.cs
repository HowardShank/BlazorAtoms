using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Shared;

/// <summary>
/// Base class for all public BlazorAtoms components that own a styleable visual root — adds
/// <see cref="CssClass"/>/<see cref="Style"/> on top of <see cref="AtomComponentCore"/>'s shared
/// essentials. Compiled into every library via <c>build/Shared.props</c> — it is not a separate package.
/// </summary>
public abstract class AtomComponentBase : AtomComponentCore
{
    /// <summary>Extra CSS class(es) appended after the component's own root class. Layer onto the
    /// defaults; use this rather than the attribute splat to add a class.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Extra inline style appended after the component's own root style (custom properties).
    /// Later declarations win, so this overrides the component defaults. Use this rather than the
    /// attribute splat to add a style.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Root class-attribute value = the component's own class plus the caller's <see cref="CssClass"/>.</summary>
    protected string ClassAttr(string baseClass) =>
        string.IsNullOrEmpty(CssClass) ? baseClass : $"{baseClass} {CssClass}";

    /// <summary>Root style-attribute value = the component's own <paramref name="rootStyle"/> plus the
    /// caller's <see cref="Style"/>. Returns null (so no empty <c>style</c> attribute renders) when both are empty.</summary>
    protected string? StyleAttr(string? rootStyle)
    {
        if (string.IsNullOrEmpty(Style)) return string.IsNullOrEmpty(rootStyle) ? null : rootStyle;
        if (string.IsNullOrEmpty(rootStyle)) return Style;
        // StyleVars tokens already end in ';'; raw styles (e.g. "width:100%") may not — add one separator.
        return rootStyle.EndsWith(';') ? rootStyle + Style : rootStyle + ";" + Style;
    }
}
