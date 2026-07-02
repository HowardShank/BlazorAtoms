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
}
