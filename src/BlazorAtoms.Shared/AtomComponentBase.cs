using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Shared;

/// <summary>
/// Base class for all public BlazorAtoms components and the home for truly shared component
/// behavior — members declared here flow to every component without editing each library.
/// Compiled into every library via <c>build/Shared.props</c> — it is not a separate package.
/// </summary>
public abstract class AtomComponentBase : ComponentBase
{
    /// <summary>
    /// Cooperative cancellation token shared by every Atom component. When cancellation is
    /// requested the component renders nothing and any SVG build loop bails out early.
    /// Individual components honor it in their render path.
    /// </summary>
    [Parameter] public CancellationToken CancellationToken { get; set; } = default;

    /// <summary>Extra CSS class(es) appended after the component's own root class. Layer onto the
    /// defaults; use this rather than the attribute splat to add a class.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Extra inline style appended after the component's own root style (custom properties).
    /// Later declarations win, so this overrides the component defaults. Use this rather than the
    /// attribute splat to add a style.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Arbitrary unmatched HTML attributes (<c>title</c>, <c>data-*</c>, <c>id</c>, ARIA,
    /// event handlers, ...) splatted onto the root element. Rendered <b>before</b> the component's own
    /// structural attributes so it cannot clobber <c>class</c>/<c>style</c>/<c>role</c>/<c>data-*</c> —
    /// use the <see cref="CssClass"/>/<see cref="Style"/> parameters for those.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

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
