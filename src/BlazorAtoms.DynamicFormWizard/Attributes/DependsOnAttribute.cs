using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Hides a property unless another top-level property on the same model currently equals
/// <see cref="ExpectedValue"/>. Stackable (<see cref="AttributeUsageAttribute.AllowMultiple"/>) --
/// when a property carries more than one, ALL must match (AND-combined); there is no OR yet
/// (DESIGN-DISCUSSION.md C.11 -- deliberately deferred). <see cref="TargetProperty"/> only reaches
/// a top-level property name on the same model -- it cannot target a field nested inside another
/// complex-typed property (DESIGN-DISCUSSION.md B.6, a known v1 limitation, not yet needed by any
/// traced scenario). A step whose properties carry no <see cref="DependsOnAttribute"/> at all is
/// visible to every branch that reaches it -- this is how "rejoining" after a fork works, with no
/// separate merge construct (DESIGN-DISCUSSION.md C.8).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DependsOnAttribute : Attribute
{
    /// <summary>Name of the sibling top-level property whose value gates this one.</summary>
    public string TargetProperty { get; }

    /// <summary>The value <see cref="TargetProperty"/> must currently equal for this property to
    /// be visible. Must be an attribute-constant-compatible type (primitive, string, or enum).</summary>
    public object ExpectedValue { get; }

    public DependsOnAttribute(string targetProperty, object expectedValue)
    {
        TargetProperty = targetProperty;
        ExpectedValue = expectedValue;
    }
}
