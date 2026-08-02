using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>Overrides <see cref="DynamicWizard{TModel}.DefaultLabelPosition"/> for one property
/// (DESIGN-DISCUSSION.md H.31, #142) -- same override-wins-over-default pattern as every other
/// per-field attribute in this engine.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FormLabelAttribute : Attribute
{
    public LabelPosition Position { get; }

    public FormLabelAttribute(LabelPosition position)
    {
        Position = position;
    }
}
