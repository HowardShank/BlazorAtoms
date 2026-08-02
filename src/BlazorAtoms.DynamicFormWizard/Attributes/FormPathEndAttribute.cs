using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Declares an authoritative end to the wizard's path: when every stacked
/// <see cref="FormPathEndAttribute"/> on this property currently matches (same AND-combined,
/// stackable shape as <see cref="DependsOnAttribute"/>), the step this property belongs to (via
/// its own <see cref="FormStepAttribute"/>) is treated as final -- navigation stops there
/// regardless of what is declared on later steps, even if a later property happens to be
/// (perhaps mistakenly) unconditionally visible.
///
/// Without this, "final" is purely derived -- "no declared step after this one currently has
/// anything visible." That works when every later-branch field is correctly gated, but it fails
/// silently the moment one is not: a missing <see cref="DependsOnAttribute"/> on a later field
/// means "no condition," which already means "always visible," so a branch that was meant to end
/// earlier would walk straight into it. As branch count grows, so does the number of
/// <see cref="DependsOnAttribute"/>s a consumer must get right for every later step, multiplying
/// the odds of exactly this mistake. One <see cref="FormPathEndAttribute"/> per branch's true
/// termination point is authoritative regardless of how later steps are (mis)configured -- fewer
/// attributes overall, not more, and safe by construction rather than by careful bookkeeping.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class FormPathEndAttribute : Attribute
{
    /// <summary>Name of the sibling top-level property whose value gates this end marker. Same
    /// top-level-only reach as <see cref="DependsOnAttribute.TargetProperty"/>.</summary>
    public string TargetProperty { get; }

    /// <summary>The value <see cref="TargetProperty"/> must currently equal for this step to be
    /// treated as an authoritative path end.</summary>
    public object ExpectedValue { get; }

    public FormPathEndAttribute(string targetProperty, object expectedValue)
    {
        TargetProperty = targetProperty;
        ExpectedValue = expectedValue;
    }
}
