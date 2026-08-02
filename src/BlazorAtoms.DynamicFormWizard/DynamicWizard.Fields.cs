using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorAtoms.DynamicFormWizard.Files;
using BlazorAtoms.DynamicFormWizard.Rendering;
using BlazorAtoms.DynamicFormWizard.Schema;

namespace BlazorAtoms.DynamicFormWizard;

public partial class DynamicWizard<TModel> where TModel : class, new()
{
    /// <summary>The object that actually owns a value and the reflected property that reads/
    /// writes it -- the top-level <see cref="Model"/> for a normal field, or a nested group
    /// instance when recursing from <see cref="RenderExpandedGroup"/> (DESIGN-DISCUSSION.md B.5).</summary>
    private readonly record struct FieldTarget(object Owner, PropertyInfo Info, string Label);

    private static readonly MethodInfo CreateTypedCallbackMethod =
        typeof(DynamicWizard<TModel>).GetMethod(nameof(CreateTypedCallbackGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>The four-tier field-render dispatch (DESIGN-DISCUSSION.md A.4). Each top-level
    /// call opens its own region with a fixed sequence constant, following the fix Ideas.md
    /// iteration 6 landed on (<c>OpenRegion</c>/<c>CloseRegion</c>) -- a dynamically varying inner
    /// shape (an enum's option count, a group's property count) never corrupts the outer diffing
    /// sequence this way.</summary>
    private RenderFragment RenderField(WizardPropertySchema property) => builder =>
    {
        var target = new FieldTarget(Model, property.Property, property.Label);
        var value = property.Property.GetValue(Model);

        if (FieldTemplate is not null)
        {
            var context = new WizardFieldContext(property, value, MakeUntypedValueChanged(target));
            builder.AddContent(0, FieldTemplate(context));
            return;
        }

        builder.OpenRegion(0);
        // [FormSelect]/[FormDynamicSelect] are schema-level metadata on top-level properties only
        // (not evaluated for auto-expanded nested group members -- consistent with DependsOn's own
        // top-level-only reach, B.6) -- checked here, ahead of the type-dispatch tiers, since a
        // string property carrying either attribute should never fall through to plain InputText.
        if (property.Property.PropertyType == typeof(string) && property.Select is not null)
        {
            RenderStaticSelect(builder, target, (string?)value, property.Select.Options);
        }
        else if (property.Property.PropertyType == typeof(string) && property.DynamicSelect is not null)
        {
            RenderDynamicSelect(builder, target, (string?)value, property.DynamicSelect.ProviderKey);
        }
        else
        {
            RenderDispatched(builder, target, property.Property.PropertyType, value);
        }
        builder.CloseRegion();
    };

    private EventCallback<object?> MakeUntypedValueChanged(FieldTarget target) =>
        EventCallback.Factory.Create<object?>(this, val =>
        {
            target.Info.SetValue(target.Owner, val);
            OnFieldChanged();
        });

    /// <summary>Tiers 1-4 in priority order. Assumes the caller has already opened an isolating
    /// region -- this method and everything it calls always starts numbering fresh at 0.</summary>
    private void RenderDispatched(RenderTreeBuilder builder, FieldTarget target, Type declaredType, object? value)
    {
        var valueType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        // Tier 1: consumer type-registry match wins outright.
        if (FieldRenderers is not null && FieldRenderers.TryGetValue(valueType, out var customComponentType))
        {
            RenderRegisteredComponent(builder, customComponentType, target, valueType, value);
            return;
        }

        // Tier 2: known built-in scalar types.
        if (TryRenderBuiltInScalar(builder, target, valueType, value))
        {
            return;
        }

        // Tier 3: auto-expand -- a complex type's own properties become a field group.
        if (WizardTypeInspection.IsComplexType(valueType))
        {
            RenderExpandedGroup(builder, target, valueType, value);
            return;
        }

        // Tier 4: fallback -- never let an unhandled type silently disappear from the form.
        RenderFallback(builder, target, value);
    }

    private void RenderRegisteredComponent(RenderTreeBuilder builder, Type componentType, FieldTarget target, Type valueType, object? value)
    {
        builder.OpenComponent(0, componentType);
        builder.AddAttribute(1, "Value", value);
        builder.AddAttribute(2, "ValueChanged", CreateTypedValueChanged(target, valueType));
        builder.CloseComponent();
    }

    private bool TryRenderBuiltInScalar(RenderTreeBuilder builder, FieldTarget target, Type valueType, object? value)
    {
        if (valueType == typeof(bool))
        {
            RenderInput(builder, typeof(InputCheckbox), typeof(bool), value ?? false, target, "wizard-field wizard-field--checkbox");
            return true;
        }
        if (valueType.IsEnum)
        {
            RenderEnumSelect(builder, valueType, value, target);
            return true;
        }
        if (valueType == typeof(DateTime))
        {
            RenderInput(builder, typeof(InputDate<DateTime>), typeof(DateTime), value ?? default(DateTime), target, "wizard-field wizard-field--date");
            return true;
        }
        if (valueType == typeof(int) || valueType == typeof(decimal) || valueType == typeof(double))
        {
            var componentType = typeof(InputNumber<>).MakeGenericType(valueType);
            RenderInput(builder, componentType, valueType, value ?? Activator.CreateInstance(valueType)!, target, "wizard-field wizard-field--number");
            return true;
        }
        if (valueType == typeof(string))
        {
            // [FormSelect]/[FormDynamicSelect] are intercepted earlier in RenderField, ahead of
            // this dispatch -- reaching here means neither is present, so plain text.
            RenderInput(builder, typeof(InputText), typeof(string), (string?)value ?? string.Empty, target, "wizard-field wizard-field--text");
            return true;
        }
        if (valueType == typeof(IReadOnlyList<WizardFileAttachment>))
        {
            RenderFileUpload(builder, target);
            return true;
        }
        return false;
    }

    /// <summary>File uploads get their own render branch (DESIGN-DISCUSSION.md E.14) -- native
    /// <c>InputFile</c> has no <c>Value</c>/<c>ValueChanged</c>/<c>ValueExpression</c> contract at
    /// all, just <c>OnChange</c>, so it can't reuse <see cref="RenderInput"/>. Manual invalid-state
    /// class since <c>InputFile</c> doesn't derive from <c>InputBase&lt;TValue&gt;</c> and so gets
    /// no automatic <see cref="Rendering.WizardFieldCssClassProvider"/> wiring the way every other
    /// built-in field does.</summary>
    private void RenderFileUpload(RenderTreeBuilder builder, FieldTarget target)
    {
        var field = new FieldIdentifier(target.Owner, target.Info.Name);
        var cssClass = "wizard-field wizard-field--file";
        if (_editContext.GetValidationMessages(field).Any())
        {
            cssClass += " wizard-field--invalid";
        }

        builder.OpenComponent(0, typeof(InputFile));
        builder.AddAttribute(1, "multiple", true);
        builder.AddAttribute(2, "class", cssClass);
        builder.AddAttribute(3, "OnChange", EventCallback.Factory.Create<InputFileChangeEventArgs>(
            this, e => HandleFilesSelected(target, e)));
        builder.CloseComponent();
    }

    /// <summary>The one genuinely-<c>async</c> single-field handler in an otherwise fully
    /// synchronous property-set pipeline (DESIGN-DISCUSSION.md E.16) -- reading a browser file
    /// stream is unavoidably <c>Task</c>-returning, and must be awaited *before* the property is
    /// considered "set," so partial-step validation sees the real value, not an empty collection.
    /// Bytes are copied into a wizard-owned <see cref="WizardFileAttachment"/> immediately
    /// (DESIGN-DISCUSSION.md E.15) -- never a raw <c>IBrowserFile</c> handle, whose stream is tied
    /// to the current circuit/render and can't be held indefinitely.</summary>
    private async Task HandleFilesSelected(FieldTarget target, InputFileChangeEventArgs e)
    {
        var attachments = new List<WizardFileAttachment>();
        foreach (var file in e.GetMultipleFiles())
        {
            await using var stream = file.OpenReadStream(MaxFileReadBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            attachments.Add(new WizardFileAttachment(file.Name, file.ContentType, file.Size, buffer.ToArray()));
        }

        target.Info.SetValue(target.Owner, (IReadOnlyList<WizardFileAttachment>)attachments);
        OnFieldChanged();
    }

    /// <summary>Fixed default for v1, not yet configurable (DESIGN-DISCUSSION.md "Not yet
    /// decided") -- protects against reading an unexpectedly huge file into memory unbounded.</summary>
    private const long MaxFileReadBytes = 10 * 1024 * 1024;

    private void RenderInput(RenderTreeBuilder builder, Type componentType, Type valueType, object currentValue, FieldTarget target, string cssClass)
    {
        builder.OpenComponent(0, componentType);
        builder.AddAttribute(1, "Value", currentValue);
        builder.AddAttribute(2, "ValueChanged", CreateTypedValueChanged(target, valueType));
        builder.AddAttribute(3, "ValueExpression", BuildValueExpression(target.Owner, target.Info));
        builder.AddAttribute(4, "class", cssClass);
        builder.CloseComponent();
    }

    private void RenderEnumSelect(RenderTreeBuilder builder, Type enumType, object? value, FieldTarget target)
    {
        var componentType = typeof(InputSelect<>).MakeGenericType(enumType);
        var current = value ?? Enum.GetValues(enumType).GetValue(0)!;

        builder.OpenComponent(0, componentType);
        builder.AddAttribute(1, "Value", current);
        builder.AddAttribute(2, "ValueChanged", CreateTypedValueChanged(target, enumType));
        builder.AddAttribute(3, "ValueExpression", BuildValueExpression(target.Owner, target.Info));
        builder.AddAttribute(4, "class", "wizard-field wizard-field--select");
        builder.AddAttribute(5, "ChildContent", (RenderFragment)(childBuilder =>
        {
            // Nested region: the enum's option count varies per type, so its own numbering must
            // not leak into this component's attribute sequence above (Ideas.md iteration 6's fix).
            childBuilder.OpenRegion(0);
            var seq = 0;
            foreach (var name in Enum.GetNames(enumType))
            {
                var field = enumType.GetField(name);
                var display = field?.GetCustomAttribute<DisplayAttribute>();
                childBuilder.OpenElement(seq++, "option");
                childBuilder.AddAttribute(seq++, "value", name);
                childBuilder.AddContent(seq++, display?.Name ?? name);
                childBuilder.CloseElement();
            }
            childBuilder.CloseRegion();
        }));
        builder.CloseComponent();
    }

    /// <summary>Tier 3: recurses into a complex type's own public read/write properties, rendering
    /// them as a field group inside the owning step (DESIGN-DISCUSSION.md B.5) -- the
    /// `CustomerInfo`/`ManagerAccount` intuition from the account-type scenario, with no path-based
    /// `DependsOn` targeting or recursive step-graph needed.</summary>
    private void RenderExpandedGroup(RenderTreeBuilder builder, FieldTarget target, Type groupType, object? groupInstance)
    {
        if (groupInstance is null)
        {
            groupInstance = Activator.CreateInstance(groupType)!;
            target.Info.SetValue(target.Owner, groupInstance);
        }

        builder.OpenElement(0, "fieldset");
        builder.AddAttribute(1, "class", "wizard-field-group");
        builder.OpenElement(2, "legend");
        builder.AddContent(3, target.Label);
        builder.CloseElement(); // legend

        var nestedProperties = groupType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

        builder.OpenRegion(4);
        var seq = 0;
        foreach (var nested in nestedProperties)
        {
            var nestedLabel = nested.GetCustomAttribute<DisplayAttribute>()?.Name ?? nested.Name;
            var nestedTarget = new FieldTarget(groupInstance, nested, nestedLabel);
            var nestedValue = nested.GetValue(groupInstance);

            builder.OpenRegion(seq++);
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "wizard-field-group__item");
            builder.OpenElement(2, "label");
            builder.AddContent(3, nestedLabel);
            builder.CloseElement(); // label
            builder.OpenRegion(4);
            RenderDispatched(builder, nestedTarget, nested.PropertyType, nestedValue);
            builder.CloseRegion();
            builder.CloseElement(); // div
            builder.CloseRegion();
        }
        builder.CloseRegion();

        builder.CloseElement(); // fieldset
    }

    private static void RenderFallback(RenderTreeBuilder builder, FieldTarget target, object? value)
    {
        Debug.WriteLine(
            $"[DynamicFormWizard] No renderer for type '{target.Info.PropertyType.FullName}' on property '{target.Info.Name}' -- rendering a read-only fallback.");

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "wizard-field wizard-field--unhandled");
        builder.AddAttribute(2, "title", $"No renderer registered for type '{target.Info.PropertyType.Name}'.");
        builder.AddContent(3, value?.ToString() ?? string.Empty);
        builder.CloseElement();
    }

    /// <summary>Builds <c>() =&gt; owner.Property</c> as a <see cref="LambdaExpression"/> whose
    /// *runtime* type is the concrete <c>Expression&lt;Func&lt;TValue&gt;&gt;</c> an
    /// <c>InputBase&lt;TValue&gt;</c>'s <c>ValueExpression</c> parameter needs -- reflection
    /// assignment checks the runtime type, and the zero-parameter overload of
    /// <see cref="Expression.Lambda(Expression)"/> already infers that exact delegate type from
    /// the body, so no explicit generic instantiation is required here.</summary>
    private static LambdaExpression BuildValueExpression(object owner, PropertyInfo property)
    {
        var constant = Expression.Constant(owner);
        var member = Expression.Property(constant, property);
        return Expression.Lambda(member);
    }

    /// <summary>Builds a boxed, strongly-typed <c>EventCallback&lt;TValue&gt;</c> via reflection so
    /// it binds correctly to a component parameter declared as that exact generic type (e.g. a
    /// built-in <c>InputNumber&lt;decimal&gt;.ValueChanged</c>, or a consumer's own
    /// <c>EventCallback&lt;Money&gt;</c> from the type-registry -- EXTENSIBILITY.md) -- a plain
    /// <c>EventCallback&lt;object?&gt;</c> would not satisfy that parameter's static type.</summary>
    private object CreateTypedValueChanged(FieldTarget target, Type valueType)
    {
        var generic = CreateTypedCallbackMethod.MakeGenericMethod(valueType);
        void OnChanged(object? val)
        {
            target.Info.SetValue(target.Owner, val);
            OnFieldChanged();
        }
        return generic.Invoke(null, [this, (Action<object?>)OnChanged])!;
    }

    private static object CreateTypedCallbackGeneric<TValue>(object receiver, Action<object?> onChanged) =>
        EventCallback.Factory.Create<TValue>(receiver, (TValue v) => onChanged(v));
}
