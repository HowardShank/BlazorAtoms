using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorAtoms.DynamicFormWizard.Rendering;

/// <summary>Generic text-backed input for any <typeparamref name="TValue"/> implementing
/// <c>IParsable&lt;TValue&gt;</c> that isn't already covered by one of Blazor's own typed
/// <c>Input*</c> components (DESIGN-DISCUSSION.md A.4 tier 2b) -- e.g. <c>byte</c>, <c>sbyte</c>,
/// <c>short</c>, <c>ushort</c>, <c>uint</c>, <c>ulong</c>, <c>char</c>, <c>nint</c>, <c>nuint</c>,
/// <c>Guid</c>, <c>TimeSpan</c>. Mirrors the built-in <c>InputText</c> render tree exactly (same
/// element/attribute sequence numbers) so it participates in the same <c>EditContext</c>/
/// <c>FieldCssClassProvider</c> machinery every other built-in field gets for free. Deliberately
/// generic over the interface rather than one branch per type -- a consumer's own custom struct
/// that implements <c>IParsable&lt;T&gt;</c> gets a working input automatically, no registration
/// needed.</summary>
public sealed class WizardParsableInput<TValue> : InputBase<TValue> where TValue : IParsable<TValue>
{
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (TValue.TryParse(value, CultureInfo.CurrentCulture, out var parsed))
        {
            result = parsed;
            validationErrorMessage = null;
            return true;
        }

        result = default!;
        validationErrorMessage = $"The {FieldIdentifier.FieldName} field must be a valid {typeof(TValue).Name}.";
        return false;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass);
        builder.AddAttribute(3, "value", CurrentValueAsString);
        builder.AddAttribute(4, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
        builder.SetUpdatesAttributeName("value");
        builder.CloseElement();
    }
}

/// <summary>Nullable counterpart to <see cref="WizardParsableInput{TValue}"/> for a
/// <c>Nullable&lt;TValue&gt;</c> property (e.g. <c>byte?</c>, <c>Guid?</c>, <c>bool?</c>) --
/// generic over <typeparamref name="TValue"/> itself since <c>Nullable&lt;T&gt;</c> can never
/// satisfy an interface constraint like <c>IParsable&lt;T&gt;</c> in C# (a language rule, not a
/// gap in this component). An empty string parses to <c>null</c> rather than a validation error
/// -- a cleared field means "no value," not "invalid value."</summary>
public sealed class WizardNullableParsableInput<TValue> : InputBase<TValue?> where TValue : struct, IParsable<TValue>
{
    protected override bool TryParseValueFromString(string? value, out TValue? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        if (TValue.TryParse(value, CultureInfo.CurrentCulture, out var parsed))
        {
            result = parsed;
            validationErrorMessage = null;
            return true;
        }

        result = null;
        validationErrorMessage = $"The {FieldIdentifier.FieldName} field must be a valid {typeof(TValue).Name}.";
        return false;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass);
        builder.AddAttribute(3, "value", CurrentValueAsString);
        builder.AddAttribute(4, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
        builder.SetUpdatesAttributeName("value");
        builder.CloseElement();
    }
}
