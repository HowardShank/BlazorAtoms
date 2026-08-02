using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Pins a property's render order within its <see cref="FormStepAttribute"/> step. Kept as a
/// separate attribute rather than a parameter on <see cref="FormStepAttribute"/> because raw
/// reflection property enumeration is not guaranteed stable across an inheritance hierarchy -- a
/// real .NET gotcha, not a hypothetical one (DESIGN-DISCUSSION.md C.10). Properties without this
/// attribute sort after every explicitly-ordered property in the same step, in reflection
/// encounter order (best-effort only).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FormOrderAttribute : Attribute
{
    public int Order { get; }

    public FormOrderAttribute(int order) => Order = order;
}
