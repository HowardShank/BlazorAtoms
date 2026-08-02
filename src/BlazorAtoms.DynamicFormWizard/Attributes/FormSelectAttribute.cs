using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Renders a string property as a dropdown over a fixed set of choices declared right on the
/// model -- no lookup service needed. For options that must come from a database/API instead, see
/// <see cref="FormDynamicSelectAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FormSelectAttribute : Attribute
{
    public string[] Options { get; }

    public FormSelectAttribute(params string[] options) => Options = options ?? Array.Empty<string>();
}
