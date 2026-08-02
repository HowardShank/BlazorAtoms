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
using BlazorAtoms.DynamicFormWizard.Attributes;
using BlazorAtoms.DynamicFormWizard.Files;
using BlazorAtoms.DynamicFormWizard.Rendering;
using BlazorAtoms.DynamicFormWizard.Schema;

namespace BlazorAtoms.DynamicFormWizard;

public partial class DynamicWizard<TModel> where TModel : class, new()
{
    /// <summary>The object that actually owns a value and the reflected property that reads/
    /// writes it -- the top-level <see cref="Model"/> for a normal field, a nested group instance
    /// when recursing from <see cref="RenderExpandedGroup"/> (DESIGN-DISCUSSION.md B.5), or a
    /// <see cref="ListItemBox{TItem}"/> wrapper for a repeating list's own item (DESIGN-DISCUSSION.md
    /// G.25) -- see that type for why a wrapper, not a raw list index, is what makes list items
    /// reuse the exact same scalar/group dispatch as an ordinary property.</summary>
    private readonly struct FieldTarget
    {
        public object Owner { get; }
        public PropertyInfo Info { get; }
        public string Label { get; }

        /// <summary>Extra HTML attributes to splat onto the rendered input (label-position-driven
        /// aria-label/placeholder, merged with any consumer <see cref="FieldAttributes"/> --
        /// DESIGN-DISCUSSION.md H.31, #142/#143). Null for every nested-group member and list-item
        /// target -- both construct a <see cref="FieldTarget"/> via the 3-arg overload below, since
        /// <see cref="FieldAttributes"/> is deliberately top-level-only (same reach as
        /// <c>[DependsOn]</c>/<c>[FormSelect]</c>, B.6), a known scope limit, not an oversight.</summary>
        public IReadOnlyDictionary<string, object>? ExtraAttributes { get; }

        public FieldTarget(object owner, PropertyInfo info, string label)
            : this(owner, info, label, null)
        {
        }

        public FieldTarget(object owner, PropertyInfo info, string label, IReadOnlyDictionary<string, object>? extraAttributes)
        {
            Owner = owner;
            Info = info;
            Label = label;
            ExtraAttributes = extraAttributes;
        }

        public object? GetValue() => Info.GetValue(Owner);

        public void SetValue(object? value) => Info.SetValue(Owner, value);

        /// <summary>Builds <c>() =&gt; owner.Property</c> as a <see cref="LambdaExpression"/> whose
        /// *runtime* type is the concrete <c>Expression&lt;Func&lt;TValue&gt;&gt;</c> an
        /// <c>InputBase&lt;TValue&gt;</c>'s <c>ValueExpression</c> parameter needs -- reflection
        /// assignment checks the runtime type, and the zero-parameter overload of
        /// <see cref="Expression.Lambda(Expression)"/> already infers that exact delegate type
        /// from the body, so no explicit generic instantiation is required here.</summary>
        public LambdaExpression BuildValueExpression()
        {
            var constant = Expression.Constant(Owner);
            var member = Expression.Property(constant, Info);
            return Expression.Lambda(member);
        }
    }

    private static readonly MethodInfo CreateTypedCallbackMethod =
        typeof(DynamicWizard<TModel>).GetMethod(nameof(CreateTypedCallbackGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>The four-tier field-render dispatch (DESIGN-DISCUSSION.md A.4). Each top-level
    /// call opens its own region with a fixed sequence constant, following the fix Ideas.md
    /// iteration 6 landed on (<c>OpenRegion</c>/<c>CloseRegion</c>) -- a dynamically varying inner
    /// shape (an enum's option count, a group's property count) never corrupts the outer diffing
    /// sequence this way.</summary>
    private RenderFragment RenderField(WizardPropertySchema property) => builder =>
    {
        var target = new FieldTarget(Model, property.Property, property.Label, BuildExtraAttributes(property));
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
            target.SetValue(val);
            OnFieldChanged();
        });

    /// <summary>Merges the consumer's own <see cref="FieldAttributes"/> for this property,
    /// <c>[Display(Prompt=...)]</c>'s placeholder, and an <c>aria-label</c>/<c>placeholder</c>
    /// synthesized from <see cref="LabelPosition.Hidden"/>/<see cref="LabelPosition.Inline"/>
    /// (DESIGN-DISCUSSION.md H.31/H.32). Precedence, via <c>Dictionary.TryAdd</c> in this exact
    /// order -- consumer <see cref="FieldAttributes"/> beats <c>Prompt</c> beats the
    /// <see cref="LabelPosition.Inline"/> label-text fallback, same "explicit beats engine
    /// default, more specific beats less specific" rule as everywhere else here. <c>Prompt</c>
    /// applies regardless of <see cref="LabelPosition"/> -- a visible label above the field and a
    /// placeholder hint inside it aren't mutually exclusive. Returns null (no allocation) when
    /// there's nothing to add, so every splat call site below can skip itself on the common case.</summary>
    private Dictionary<string, object>? BuildExtraAttributes(WizardPropertySchema property)
    {
        Dictionary<string, object>? merged = null;
        if (FieldAttributes is not null && FieldAttributes.TryGetValue(property.Property.Name, out var consumerAttrs))
        {
            merged = new Dictionary<string, object>(consumerAttrs);
        }

        if (!string.IsNullOrEmpty(property.Placeholder))
        {
            merged ??= new Dictionary<string, object>();
            merged.TryAdd("placeholder", property.Placeholder);
        }

        var position = EffectiveLabelPosition(property);
        if (position == LabelPosition.Hidden)
        {
            merged ??= new Dictionary<string, object>();
            merged.TryAdd("aria-label", property.Label);
        }
        else if (position == LabelPosition.Inline)
        {
            merged ??= new Dictionary<string, object>();
            merged.TryAdd("placeholder", property.Label);
        }

        return merged;
    }

    /// <summary>Tiers 1, 1b, 2/2b, 3, 4 in priority order (DESIGN-DISCUSSION.md A.4). Assumes the
    /// caller has already opened an isolating region -- this method and everything it calls
    /// always starts numbering fresh at 0.</summary>
    private void RenderDispatched(RenderTreeBuilder builder, FieldTarget target, Type declaredType, object? value)
    {
        // [Editable(false)] overrides every tier below, including a consumer's own tier-1
        // registry match -- an explicit "don't let this be edited" beats any renderer's opinion
        // about how the type would otherwise render.
        if (target.Info.GetCustomAttribute<EditableAttribute>() is { AllowEdit: false })
        {
            RenderReadOnlyField(builder, target, value);
            return;
        }

        // Only tier 1's registry lookup and tier 3's complex-type check want the *unwrapped*
        // type (a consumer registering a nullable-capable custom struct should also catch its
        // Nullable<T> form; Nullable<T> is never itself a complex/class type regardless). Tier 2
        // needs the FULL declared type, nullable wrapper included, so it can dispatch
        // nullable-aware (DESIGN-DISCUSSION.md A.4 tier 2b) -- stripping it here, before tier 2
        // ever sees it, was the bug: every nullable branch inside TryRenderBuiltInScalar was
        // unreachable dead code because Nullable.GetUnderlyingType(valueType) was always null by
        // the time it ran.
        var registryLookupType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        // Tier 1: consumer type-registry match wins outright.
        if (FieldRenderers is not null && FieldRenderers.TryGetValue(registryLookupType, out var customComponentType))
        {
            RenderRegisteredComponent(builder, customComponentType, target, declaredType, value);
            return;
        }

        // Tier 1b: List<T> repeating groups (DESIGN-DISCUSSION.md G.25) -- checked ahead of tier 2
        // since a List<T> never matches any of its named scalar types anyway, and ahead of tier 3
        // since WizardTypeInspection.IsComplexType now deliberately excludes ALL collections
        // (including List<T>) to avoid the "Capacity" misdetection bug -- this is where List<T>
        // gets its own real handling instead.
        if (WizardTypeInspection.TryGetListItemType(declaredType, out var itemType))
        {
            RenderListProperty(builder, target, declaredType, itemType, value);
            return;
        }

        // Tier 2: known built-in scalar types (nullable-aware internally).
        if (TryRenderBuiltInScalar(builder, target, declaredType, value))
        {
            return;
        }

        // Tier 3: auto-expand -- a complex type's own properties become a field group.
        if (WizardTypeInspection.IsComplexType(registryLookupType))
        {
            RenderExpandedGroup(builder, target, registryLookupType, value);
            return;
        }

        // Tier 4: fallback -- never let an unhandled type silently disappear from the form.
        RenderFallback(builder, target, value);
    }

    /// <summary>Tier 1: a consumer's own registered component (EXTENSIBILITY.md's <c>Money</c>/
    /// <c>MoneyInput</c> example). Deliberately does NOT splat <see cref="FieldTarget.ExtraAttributes"/>
    /// here, unlike every built-in tier below -- an arbitrary consumer component has no guaranteed
    /// <c>AdditionalAttributes</c>/<c>CaptureUnmatchedValues</c> parameter to receive it, and adding
    /// an attribute a component doesn't declare throws at runtime ("does not have a property
    /// matching the name ..."), unlike a plain HTML element. A known scope limit for #142/#143
    /// (DESIGN-DISCUSSION.md H.31), not an oversight.</summary>
    private void RenderRegisteredComponent(RenderTreeBuilder builder, Type componentType, FieldTarget target, Type valueType, object? value)
    {
        builder.OpenComponent(0, componentType);
        builder.AddAttribute(1, "Value", value);
        builder.AddAttribute(2, "ValueChanged", CreateTypedValueChanged(target, valueType));
        builder.CloseComponent();
    }

    /// <summary>Native <c>InputNumber&lt;TValue&gt;</c> types -- Blazor's own component supports
    /// both these and their <c>Nullable&lt;T&gt;</c> forms directly.</summary>
    private static readonly HashSet<Type> NativeNumberTypes =
        [typeof(int), typeof(long), typeof(short), typeof(float), typeof(decimal), typeof(double)];

    /// <summary>Native <c>InputDate&lt;TValue&gt;</c> types -- also natively nullable-aware.</summary>
    private static readonly HashSet<Type> NativeDateTypes = [typeof(DateTime), typeof(DateOnly), typeof(TimeOnly)];

    /// <summary>[DataType] values that map to a plain HTML5 <c>&lt;input type="..."&gt;</c> hint on
    /// a string property -- deliberately a small, high-value subset (not every <see cref="DataType"/>
    /// member has an obvious single-input mapping; e.g. <c>Currency</c>/<c>PostalCode</c>/<c>CreditCard</c>
    /// are still plain text pending a real formatting/masking need).</summary>
    private static readonly Dictionary<DataType, string> StringInputHtmlTypes = new()
    {
        [DataType.Password] = "password",
        [DataType.EmailAddress] = "email",
        [DataType.PhoneNumber] = "tel",
        [DataType.Url] = "url",
    };

    private bool TryRenderBuiltInScalar(RenderTreeBuilder builder, FieldTarget target, Type valueType, object? value)
    {
        // Nullable<T> can never itself satisfy an interface constraint like IParsable<T> (a C#
        // language rule, not a gap we can close by wrapping) -- so every nullable branch below
        // dispatches on the *underlying* type instead, while still passing the full nullable
        // valueType through to the rendered component (whose Value/ValueChanged is typed as
        // T?, not T).
        var underlyingType = Nullable.GetUnderlyingType(valueType);
        var effectiveType = underlyingType ?? valueType;

        if (valueType == typeof(bool))
        {
            RenderInput(builder, typeof(InputCheckbox), typeof(bool), value ?? false, target, "wizard-field wizard-field--checkbox");
            return true;
        }
        if (effectiveType.IsEnum)
        {
            if (underlyingType is null)
            {
                RenderEnumSelect(builder, valueType, value, target);
            }
            else
            {
                RenderNullableEnumSelect(builder, underlyingType, valueType, value, target);
            }
            return true;
        }
        if (NativeDateTypes.Contains(effectiveType))
        {
            var dateComponentType = typeof(InputDate<>).MakeGenericType(valueType);
            RenderInput(builder, dateComponentType, valueType, value, target, "wizard-field wizard-field--date");
            return true;
        }
        if (NativeNumberTypes.Contains(effectiveType))
        {
            // Blazor's own InputNumber<TValue> only supports these six (and their nullable forms)
            // -- byte/sbyte/ushort/uint/ulong fall through to the generic IParsable tier below.
            var componentType = typeof(InputNumber<>).MakeGenericType(valueType);
            RenderInput(builder, componentType, valueType, value, target, "wizard-field wizard-field--number");
            return true;
        }
        if (valueType == typeof(string))
        {
            // [FormSelect]/[FormDynamicSelect] are intercepted earlier in RenderField, ahead of
            // this dispatch -- reaching here means neither is present. [DataType] picks a more
            // specific rendering for a handful of well-known string shapes; native InputText
            // hardcodes type="text" internally (its own AddAttribute for "type" is written after
            // AdditionalAttributes, so passing "type" through AdditionalAttributes can never win --
            // hence a raw manually-bound element below instead of trying to override InputText).
            var dataType = target.Info.GetCustomAttribute<DataTypeAttribute>()?.DataType;
            if (dataType == DataType.MultilineText)
            {
                RenderTextArea(builder, target, (string?)value ?? string.Empty);
                return true;
            }
            if (dataType is not null && StringInputHtmlTypes.TryGetValue(dataType.Value, out var htmlInputType))
            {
                RenderTypedTextInput(builder, target, (string?)value ?? string.Empty, htmlInputType);
                return true;
            }
            RenderInput(builder, typeof(InputText), typeof(string), (string?)value ?? string.Empty, target, "wizard-field wizard-field--text");
            return true;
        }
        if (valueType == typeof(IReadOnlyList<WizardFileAttachment>))
        {
            RenderFileUpload(builder, target);
            return true;
        }
        if (underlyingType is null && IsParsableType(valueType))
        {
            // Tier 2b: anything implementing IParsable<T> that isn't one of the named types above
            // -- byte, sbyte, short's unsigned sibling ushort, uint, ulong, char, nint, nuint,
            // Guid, TimeSpan, or a consumer's own custom struct. See WizardParsableInput.
            var componentType = typeof(WizardParsableInput<>).MakeGenericType(valueType);
            RenderInput(builder, componentType, valueType, value, target, "wizard-field wizard-field--parsable");
            return true;
        }
        if (underlyingType is not null && IsParsableType(underlyingType))
        {
            // Tier 2b, nullable form -- byte?, sbyte?, ushort?, uint?, ulong?, char?, nint?,
            // nuint?, Guid?, TimeSpan?, bool? (no native tri-state checkbox exists), or a
            // consumer's own nullable custom struct. WizardNullableParsableInput<T> treats an
            // empty string as null rather than a parse failure.
            var componentType = typeof(WizardNullableParsableInput<>).MakeGenericType(underlyingType);
            RenderInput(builder, componentType, valueType, value, target, "wizard-field wizard-field--parsable");
            return true;
        }
        return false;
    }

    /// <summary>True when <paramref name="type"/> implements <c>IParsable&lt;type&gt;</c> --
    /// checked reflectively since the generic argument (the type itself) is only known at
    /// runtime here.</summary>
    private static bool IsParsableType(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType
            && i.GetGenericTypeDefinition() == typeof(IParsable<>)
            && i.GetGenericArguments()[0] == type);

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
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(4, target.ExtraAttributes);
        }
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

        target.SetValue((IReadOnlyList<WizardFileAttachment>)attachments);
        OnFieldChanged();
    }

    /// <summary>Fixed default for v1, not yet configurable (DESIGN-DISCUSSION.md "Not yet
    /// decided") -- protects against reading an unexpectedly huge file into memory unbounded.</summary>
    private const long MaxFileReadBytes = 10 * 1024 * 1024;

    /// <summary>A raw, manually-bound <c>&lt;input&gt;</c> for a <see cref="DataType"/>-hinted
    /// string (password/email/tel/url) -- native <c>InputText</c> can't be reused here since it
    /// hardcodes <c>type="text"</c> itself (see the call site in <see cref="TryRenderBuiltInScalar"/>),
    /// so this reimplements just enough of what <see cref="RenderInput"/> gives every other tier-2
    /// field: the invalid-state CSS class and an onchange that writes straight back through
    /// <see cref="FieldTarget.SetValue"/>.</summary>
    private void RenderTypedTextInput(RenderTreeBuilder builder, FieldTarget target, string value, string htmlInputType)
    {
        var field = new FieldIdentifier(target.Owner, target.Info.Name);
        var cssClass = "wizard-field wizard-field--text";
        if (_editContext.GetValidationMessages(field).Any())
        {
            cssClass += " wizard-field--invalid";
        }

        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "type", htmlInputType);
        builder.AddAttribute(2, "class", cssClass);
        builder.AddAttribute(3, "value", value);
        builder.AddAttribute(4, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
        {
            target.SetValue(e.Value?.ToString() ?? string.Empty);
            OnFieldChanged();
        }));
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(5, target.ExtraAttributes);
        }
        builder.CloseElement();
    }

    /// <summary><c>[DataType(DataType.MultilineText)]</c> renders a <c>&lt;textarea&gt;</c> instead
    /// of a single-line input -- same manual-binding shape as <see cref="RenderTypedTextInput"/>,
    /// since a textarea isn't an <c>InputBase&lt;TValue&gt;</c>-derived component either.</summary>
    private void RenderTextArea(RenderTreeBuilder builder, FieldTarget target, string value)
    {
        var field = new FieldIdentifier(target.Owner, target.Info.Name);
        var cssClass = "wizard-field wizard-field--textarea";
        if (_editContext.GetValidationMessages(field).Any())
        {
            cssClass += " wizard-field--invalid";
        }

        builder.OpenElement(0, "textarea");
        builder.AddAttribute(1, "class", cssClass);
        builder.AddAttribute(2, "value", value);
        builder.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
        {
            target.SetValue(e.Value?.ToString() ?? string.Empty);
            OnFieldChanged();
        }));
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(4, target.ExtraAttributes);
        }
        builder.CloseElement();
    }

    /// <summary><c>[Editable(false)]</c>'s rendering -- a read-only span, same shape as
    /// <see cref="RenderFallback"/> but for a deliberately-read-only *known* type rather than an
    /// unhandled one, so it gets its own CSS hook (<c>wizard-field--readonly</c>, not
    /// <c>wizard-field--unhandled</c>) and no dev-time warning.</summary>
    private static void RenderReadOnlyField(RenderTreeBuilder builder, FieldTarget target, object? value)
    {
        var format = target.Info.GetCustomAttribute<DisplayFormatAttribute>();
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "wizard-field wizard-field--readonly");
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(2, target.ExtraAttributes);
        }
        builder.AddContent(3, FormatDisplayValue(value, format));
        builder.CloseElement();
    }

    /// <summary>Shared by <see cref="RenderReadOnlyField"/> and <see cref="RenderFallback"/> --
    /// honors <c>[DisplayFormat(NullDisplayText=..., DataFormatString=...)]</c> for read-only
    /// display. Deliberately not applied to any *editable* tier: <c>DataFormatString</c> is a
    /// display-mode format (<c>string.Format</c>), not an input mask, so applying it to a live
    /// input's bound value would corrupt round-tripping (e.g. a currency format string would need
    /// to be parsed back out on every keystroke, which no built-in Input* component does).</summary>
    private static string FormatDisplayValue(object? value, DisplayFormatAttribute? format)
    {
        if (value is null)
        {
            return format?.NullDisplayText ?? string.Empty;
        }
        if (!string.IsNullOrEmpty(format?.DataFormatString))
        {
            return string.Format(format.DataFormatString, value);
        }
        return value.ToString() ?? string.Empty;
    }

    private void RenderInput(RenderTreeBuilder builder, Type componentType, Type valueType, object? currentValue, FieldTarget target, string cssClass)
    {
        builder.OpenComponent(0, componentType);
        builder.AddAttribute(1, "Value", currentValue);
        builder.AddAttribute(2, "ValueChanged", CreateTypedValueChanged(target, valueType));
        builder.AddAttribute(3, "ValueExpression", target.BuildValueExpression());
        builder.AddAttribute(4, "class", cssClass);
        // Every built-in InputBase<TValue> declares [Parameter(CaptureUnmatchedValues = true)]
        // AdditionalAttributes, so any attribute name added here that ISN'T one of its own declared
        // parameters (aria-label, placeholder, data-testid, ...) is captured into it automatically
        // -- adding "AdditionalAttributes" itself BY NAME throws at runtime ("cannot be set
        // explicitly when also used to capture unmatched values"), which is why this splats each
        // key individually via AddMultipleAttributes rather than setting that parameter directly.
        // This is NOT safe on RenderRegisteredComponent (tier 1), whose component type is an
        // arbitrary consumer type with no guaranteed CaptureUnmatchedValues parameter at all;
        // adding any unmatched attribute there would throw the same way.
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(5, target.ExtraAttributes);
        }
        builder.CloseComponent();
    }

    private void RenderEnumSelect(RenderTreeBuilder builder, Type enumType, object? value, FieldTarget target)
    {
        var componentType = typeof(InputSelect<>).MakeGenericType(enumType);
        var current = value ?? Enum.GetValues(enumType).GetValue(0)!;

        builder.OpenComponent(0, componentType);
        builder.AddAttribute(1, "Value", current);
        builder.AddAttribute(2, "ValueChanged", CreateTypedValueChanged(target, enumType));
        builder.AddAttribute(3, "ValueExpression", target.BuildValueExpression());
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
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(6, target.ExtraAttributes);
        }
        builder.CloseComponent();
    }

    /// <summary>Nullable-enum counterpart to <see cref="RenderEnumSelect"/> -- adds a leading
    /// "-- none --" option representing <c>null</c> instead of defaulting to the first enum
    /// member, since an unset nullable enum genuinely means "no selection yet," not "the first
    /// value."</summary>
    private void RenderNullableEnumSelect(RenderTreeBuilder builder, Type enumType, Type nullableEnumType, object? value, FieldTarget target)
    {
        var componentType = typeof(InputSelect<>).MakeGenericType(nullableEnumType);

        builder.OpenComponent(0, componentType);
        builder.AddAttribute(1, "Value", value);
        builder.AddAttribute(2, "ValueChanged", CreateTypedValueChanged(target, nullableEnumType));
        builder.AddAttribute(3, "ValueExpression", target.BuildValueExpression());
        builder.AddAttribute(4, "class", "wizard-field wizard-field--select");
        builder.AddAttribute(5, "ChildContent", (RenderFragment)(childBuilder =>
        {
            childBuilder.OpenRegion(0);
            var seq = 0;
            childBuilder.OpenElement(seq++, "option");
            childBuilder.AddAttribute(seq++, "value", "");
            childBuilder.AddContent(seq++, "-- none --");
            childBuilder.CloseElement();
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
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(6, target.ExtraAttributes);
        }
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
            target.SetValue(groupInstance);
        }

        builder.OpenElement(0, "fieldset");
        builder.AddAttribute(1, "class", "wizard-field-group");
        builder.OpenElement(2, "legend");
        builder.AddContent(3, target.Label);
        builder.CloseElement(); // legend

        var nestedProperties = groupType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // Indexers (e.g. StringBuilder's Chars[int]) report CanRead/CanWrite too, but
            // GetValue()/SetValue() with no index arguments throws TargetParameterCountException
            // -- they aren't fields a form can render, so they're excluded here and in
            // WizardTypeInspection.IsComplexType, which must agree on the same definition.
            // [ScaffoldColumn(false)] is excluded for the same reason WizardModelSchema.Build
            // excludes it at the top level -- consistent behavior regardless of nesting depth.
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0
                && p.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold != false)
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
            $"[DynamicFormWizard] No renderer for type '{target.Info.PropertyType.FullName}' on '{target.Label}' -- rendering a read-only fallback.");

        var format = target.Info.GetCustomAttribute<DisplayFormatAttribute>();
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "wizard-field wizard-field--unhandled");
        builder.AddAttribute(2, "title", $"No renderer registered for type '{target.Info.PropertyType.Name}'.");
        if (target.ExtraAttributes is { Count: > 0 })
        {
            builder.AddMultipleAttributes(3, target.ExtraAttributes);
        }
        builder.AddContent(4, FormatDisplayValue(value, format));
        builder.CloseElement();
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
            target.SetValue(val);
            OnFieldChanged();
        }
        return generic.Invoke(null, [this, (Action<object?>)OnChanged])!;
    }

    private static object CreateTypedCallbackGeneric<TValue>(object receiver, Action<object?> onChanged) =>
        EventCallback.Factory.Create<TValue>(receiver, (TValue v) => onChanged(v));
}
