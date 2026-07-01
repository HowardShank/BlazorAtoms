using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Shared;

/// <summary>
/// Base class for all public BlazorAtoms components. Currently a marker; shared component
/// behavior can be added here later without editing each library. Compiled into every
/// library via <c>build/Shared.props</c> — it is not a separate package.
/// </summary>
public abstract class AtomComponentBase : ComponentBase
{
}
