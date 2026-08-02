using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Hides a property unless another top-level property on the same model currently satisfies
/// <see cref="Operator"/> against <see cref="ExpectedValue"/> (equality by default). Stackable
/// (<see cref="AttributeUsageAttribute.AllowMultiple"/>) -- when a property carries more than one,
/// ALL must match (AND-combined); there is no OR (DESIGN-DISCUSSION.md C.11/G.28 -- deliberately
/// deferred; a range condition is expressed by stacking two conditions on the same property, e.g.
/// GreaterThanOrEqual 18 AND LessThanOrEqual 65, reusing this same AND-combine rule).
/// <see cref="TargetProperty"/> only reaches a top-level property name on the same model -- it
/// cannot target a field nested inside another complex-typed property (DESIGN-DISCUSSION.md B.6, a
/// known v1 limitation, not yet needed by any traced scenario). A step whose properties carry no
/// <see cref="DependsOnAttribute"/> at all is visible to every branch that reaches it -- this is
/// how "rejoining" after a fork works, with no separate merge construct (DESIGN-DISCUSSION.md C.8).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DependsOnAttribute : Attribute
{
    /// <summary>Name of the sibling top-level property whose value gates this one.</summary>
    public string TargetProperty { get; }

    /// <summary>The value <see cref="TargetProperty"/> is compared against via <see cref="Operator"/>.
    /// Must be an attribute-constant-compatible type (primitive, string, or enum).</summary>
    public object ExpectedValue { get; }

    /// <summary>How <see cref="TargetProperty"/>'s current value is compared against
    /// <see cref="ExpectedValue"/>. Defaults to <see cref="ComparisonOperator.Equals"/> -- existing
    /// two-argument usages are unaffected.</summary>
    public ComparisonOperator Operator { get; }

    public DependsOnAttribute(string targetProperty, object expectedValue, ComparisonOperator @operator = ComparisonOperator.Equals)
    {
        TargetProperty = targetProperty;
        ExpectedValue = expectedValue;
        Operator = @operator;
    }
}
