using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorAtoms.DynamicFormWizard.Schema;
using BlazorAtoms.DynamicFormWizard.Services;

namespace BlazorAtoms.DynamicFormWizard;

public partial class DynamicWizard<TModel> where TModel : class, new()
{
    /// <summary>Resolved lazily via <see cref="IServiceProvider.GetService(Type)"/>, not a
    /// required <c>[Inject]</c> -- a model that never uses <c>[FormDynamicSelect]</c> must not
    /// force every consumer to register <see cref="IWizardLookupService"/> just to use the wizard
    /// at all (DESIGN-DISCUSSION.md F.20 is a stated requirement *when the attribute is used*, not
    /// an unconditional one).</summary>
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _dynamicOptions = new();

    /// <summary>Fetches every distinct <c>[FormDynamicSelect]</c> provider key across the whole
    /// schema up front (not just the current step) so navigating to a later step never stalls on
    /// a fetch -- mirrors Ideas.md iteration 3's own up-front approach. Idempotent: a key already
    /// present in <see cref="_dynamicOptions"/> is never re-fetched, so repeated calls as parameters
    /// change are cheap no-ops after the first successful fetch.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (ServiceProvider.GetService(typeof(IWizardLookupService)) is not IWizardLookupService lookup)
        {
            return;
        }

        foreach (var step in WizardModelSchema.For<TModel>().Steps)
        {
            foreach (var property in step.Properties)
            {
                if (property.DynamicSelect is not null && !_dynamicOptions.ContainsKey(property.DynamicSelect.ProviderKey))
                {
                    _dynamicOptions[property.DynamicSelect.ProviderKey] = await lookup.GetOptionsAsync(property.DynamicSelect.ProviderKey);
                }
            }
        }
    }

    private void RenderSelect(RenderTreeBuilder builder, FieldTarget target, string? value, IEnumerable<(string Value, string Label)> options)
    {
        builder.OpenComponent(0, typeof(InputSelect<string>));
        builder.AddAttribute(1, "Value", value ?? string.Empty);
        builder.AddAttribute(2, "ValueChanged", CreateTypedValueChanged(target, typeof(string)));
        builder.AddAttribute(3, "ValueExpression", target.BuildValueExpression());
        builder.AddAttribute(4, "class", "wizard-field wizard-field--select");
        builder.AddAttribute(5, "ChildContent", (RenderFragment)(childBuilder =>
        {
            // Nested region: option count varies with the option list, so its own numbering must
            // not leak into this component's attribute sequence above (Ideas.md iteration 6's fix,
            // same as the built-in enum select).
            childBuilder.OpenRegion(0);
            childBuilder.OpenElement(0, "option");
            childBuilder.AddAttribute(1, "value", "");
            childBuilder.AddContent(2, "-- Select --");
            childBuilder.CloseElement();

            var seq = 3;
            foreach (var (optionValue, label) in options)
            {
                childBuilder.OpenElement(seq++, "option");
                childBuilder.AddAttribute(seq++, "value", optionValue);
                childBuilder.AddContent(seq++, label);
                childBuilder.CloseElement();
            }
            childBuilder.CloseRegion();
        }));
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(6, target.ExtraAttributes);
        }
        builder.CloseComponent();
    }

    /// <summary><c>[FormSelect]</c> -- a fixed set of choices declared right on the model, no
    /// lookup service needed.</summary>
    private void RenderStaticSelect(RenderTreeBuilder builder, FieldTarget target, string? value, IReadOnlyList<string> options) =>
        RenderSelect(builder, target, value, options.Select(o => (o, o)));

    /// <summary><c>[FormDynamicSelect]</c> -- options fetched asynchronously through
    /// <see cref="IWizardLookupService"/> by provider key (DESIGN-DISCUSSION.md F.20). Renders a
    /// disabled placeholder while the fetch is pending or if no lookup service is registered,
    /// rather than blocking the rest of the step or throwing.</summary>
    private void RenderDynamicSelect(RenderTreeBuilder builder, FieldTarget target, string? value, string providerKey)
    {
        if (!_dynamicOptions.TryGetValue(providerKey, out var options))
        {
            builder.OpenElement(0, "select");
            builder.AddAttribute(1, "class", "wizard-field wizard-field--select");
            builder.AddAttribute(2, "disabled", true);
            builder.OpenElement(3, "option");
            builder.AddContent(4, "Loading...");
            builder.CloseElement();
            builder.CloseElement();
            return;
        }

        RenderSelect(builder, target, value, options.Select(kvp => (kvp.Key, kvp.Value)));
    }
}
