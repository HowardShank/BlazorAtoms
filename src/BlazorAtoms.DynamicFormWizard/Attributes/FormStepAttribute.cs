using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Assigns a property to a step of the wizard. <see cref="StepNumber"/> is an internal authoring
/// key only -- it drives <see cref="DependsOnAttribute"/> targeting, navigation, and skip-logic,
/// but is never shown to the user directly. The step position/count actually shown to the user is
/// computed live from whichever step numbers currently have at least one visible property, not a
/// static count of every declared <see cref="StepNumber"/> (see DESIGN-DISCUSSION.md C.9). Order
/// of properties within one step comes from <see cref="FormOrderAttribute"/>, not from this
/// attribute (C.10).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FormStepAttribute : Attribute
{
    /// <summary>The step's internal key. Never shown to the user -- see the type summary.</summary>
    public int StepNumber { get; }

    /// <summary>Optional human label for this step. When set on more than one property sharing a
    /// <see cref="StepNumber"/>, the first non-null title among that step's currently *visible*
    /// properties wins; if none is set anywhere, the engine falls back to a computed
    /// "Step {position}" using the dynamic ordinal, never this attribute's raw
    /// <see cref="StepNumber"/>.</summary>
    public string? Title { get; }

    public FormStepAttribute(int stepNumber, string? title = null)
    {
        StepNumber = stepNumber;
        Title = title;
    }
}
