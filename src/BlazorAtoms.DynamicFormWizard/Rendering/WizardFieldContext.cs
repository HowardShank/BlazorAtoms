using Microsoft.AspNetCore.Components;
using BlazorAtoms.DynamicFormWizard.Schema;

namespace BlazorAtoms.DynamicFormWizard.Rendering;

/// <summary>
/// Passed to a consumer-supplied <c>FieldTemplate</c> (DESIGN-DISCUSSION.md A.2) for each
/// currently-visible field on the current step -- the whole-form render override. For a
/// single-type override instead, see the <c>FieldRenderers</c> type-registry
/// (DESIGN-DISCUSSION.md A.3, EXTENSIBILITY.md).
/// </summary>
public sealed class WizardFieldContext
{
    public WizardPropertySchema Property { get; }
    public object? Value { get; }
    public EventCallback<object?> ValueChanged { get; }

    internal WizardFieldContext(WizardPropertySchema property, object? value, EventCallback<object?> valueChanged)
    {
        Property = property;
        Value = value;
        ValueChanged = valueChanged;
    }
}
