using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Renders a string property as a dropdown whose options are fetched asynchronously through
/// <see cref="Services.IWizardLookupService"/> by <see cref="ProviderKey"/>. Requires the consumer
/// to register an <see cref="Services.IWizardLookupService"/> implementation in DI
/// (DESIGN-DISCUSSION.md F.20) -- otherwise the dropdown never populates.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FormDynamicSelectAttribute : Attribute
{
    public string ProviderKey { get; }

    public FormDynamicSelectAttribute(string providerKey) => ProviderKey = providerKey;
}
