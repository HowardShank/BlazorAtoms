namespace BlazorAtoms.Shared;

/// <summary>
/// Marker base for the round <c>AtomBusy*</c> indicators. The <c>AtomBusyIndicator</c> wrapper
/// discovers every non-abstract subclass by reflection, so the candidate set is identified by
/// type, not by matching on a type-name prefix string.
/// </summary>
public abstract class AtomBusyIndicatorBase : AtomComponentBase
{
}
