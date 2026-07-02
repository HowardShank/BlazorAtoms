using BlazorAtoms.Shared;

namespace BlazorAtoms.ActivityIndicators;

/// <summary>
/// Marker base for the round <c>AtomActivity*</c> indicators. The <c>AtomActivityIndicator</c> wrapper
/// discovers every non-abstract subclass by reflection, so the candidate set is identified by
/// type, not by matching on a type-name prefix string.
/// <para>
/// This lives in the ActivityIndicators library (not <c>BlazorAtoms.Shared</c>) on purpose: it is
/// specific to this library. Shared source is compiled into every package, so a public type
/// there would be duplicated across assemblies and collide (CS0433) in any app that references
/// two BlazorAtoms libraries at once.
/// </para>
/// </summary>
public abstract class AtomActivityIndicatorBase : AtomComponentBase
{
}
