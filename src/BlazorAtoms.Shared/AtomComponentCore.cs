using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Shared;

/// <summary>
/// Minimal base for BlazorAtoms components that wrap arbitrary content without owning a
/// styleable visual root of their own (e.g. a JS-interop container). Provides only the
/// members every Atom component needs regardless of styling surface. <see cref="AtomComponentBase"/>
/// builds on this to add <c>CssClass</c>/<c>Style</c> for components that do own a styled root.
/// </summary>
public abstract class AtomComponentCore : ComponentBase
{
    /// <summary>
    /// Cooperative cancellation token shared by every Atom component. When cancellation is
    /// requested the component renders nothing and any SVG build loop bails out early.
    /// Individual components honor it in their render path.
    /// </summary>
    [Parameter] public CancellationToken CancellationToken { get; set; } = default;

    /// <summary>Arbitrary unmatched HTML attributes (<c>title</c>, <c>data-*</c>, <c>id</c>, ARIA,
    /// event handlers, ...) splatted onto the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}
