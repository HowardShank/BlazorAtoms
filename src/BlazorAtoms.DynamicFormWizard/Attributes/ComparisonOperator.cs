namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>How a <see cref="DependsOnAttribute"/> condition compares the target property's
/// current value against <see cref="DependsOnAttribute.ExpectedValue"/> (DESIGN-DISCUSSION.md
/// G.28). <see cref="Equals"/> is the default -- existing two-argument
/// <c>DependsOnAttribute(targetProperty, expectedValue)</c> usages are unaffected. The ordering
/// operators require the target property's actual value to implement <see cref="System.IComparable"/>
/// and be comparable against <see cref="DependsOnAttribute.ExpectedValue"/>'s runtime type (the
/// same requirement C# comparison operators themselves have). There is no OR combinator --
/// stacking two conditions on the same property (e.g. GreaterThanOrEqual 18 AND LessThanOrEqual
/// 65) already expresses a range using the existing AND-combine rule, with no new construct
/// needed.</summary>
public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
}
