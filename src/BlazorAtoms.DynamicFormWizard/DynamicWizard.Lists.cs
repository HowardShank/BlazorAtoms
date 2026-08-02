using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorAtoms.DynamicFormWizard.Schema;

namespace BlazorAtoms.DynamicFormWizard;

/// <summary>A settable proxy for one <c>list[index]</c> slot, letting a repeating scalar item
/// reuse the ordinary property-owned <c>FieldTarget</c>/<c>ValueExpression</c> machinery every
/// other field already uses (DESIGN-DISCUSSION.md G.25). This exists because it was tried the
/// "obvious" way first and failed at runtime: <c>FieldIdentifier.Create</c> explicitly rejects
/// index expressions ("FieldIdentifier only supports simple member accessors (fields, properties)
/// of an object") -- there is no supported way to build a <c>ValueExpression</c> that resolves to
/// <c>list[i]</c> directly. Wrapping the slot in a one-property box sidesteps the limitation: the
/// box's own <see cref="Value"/> property *is* a simple member accessor, and its setter writes
/// through to the real list slot.
///
/// Deliberately a *top-level* type, not nested inside <c>DynamicWizard&lt;TModel&gt;</c> --
/// nesting it there was tried first and produced a reproducible, otherwise-inexplicable failure:
/// <c>typeof(ListItemBox&lt;&gt;).MakeGenericType(itemType)</c> (called on the *nested* form, and
/// discarding the result) reliably corrupted <c>ElementReference</c> state used later in the same
/// render by <c>DynamicWizard.OnAfterRenderAsync</c>'s step-heading focus call, throwing
/// "ElementReference has not been configured correctly" -- confirmed by bisection down to that
/// single reflection call, with everything else (the loop, the render calls, the add/remove
/// buttons) ruled out one at a time. Closing a *non-nested* generic (e.g. <c>List&lt;&gt;</c>) the
/// same way never reproduced it. Rather than depend on understanding that CLR/JIT interaction
/// precisely, the type was moved out here -- avoiding "generic nested inside a generic component"
/// entirely sidesteps whatever the underlying cause is.</summary>
internal sealed class ListItemBox<TItem>
{
    private readonly IList _list;
    private readonly int _index;

    public ListItemBox(IList list, int index)
    {
        _list = list;
        _index = index;
    }

    public TItem Value
    {
        get => (TItem)_list[_index]!;
        set => _list[_index] = value;
    }
}

/// <summary>Repeating <c>List&lt;T&gt;</c> support (DESIGN-DISCUSSION.md G.25) -- tier 1b of the
/// field-render dispatch, checked in <c>DynamicWizard.Fields.cs</c>'s <c>RenderDispatched</c>
/// right after the consumer type-registry (tier 1) and before the built-in scalar tier (tier 2),
/// since a <c>List&lt;T&gt;</c> never matches any of tier 2's named types anyway. Deliberately
/// scoped to exactly <c>List&lt;TItem&gt;</c> (see <see cref="WizardTypeInspection.TryGetListItemType"/>
/// for why), split into two shapes depending on <c>TItem</c>: a repeating group of sub-forms when
/// it's complex (the "add N beneficiaries" case), or a repeating row of single inputs when it's a
/// scalar the engine already knows how to render.</summary>
public partial class DynamicWizard<TModel> where TModel : class, new()
{
    /// <summary>Box instances keyed by (list, index), reused across renders for as long as that
    /// slot keeps referring to "the same" item. This matters because <c>EditContext</c> tracks
    /// modified/invalid state by <c>FieldIdentifier</c> equality, which compares the *owner
    /// object* -- a fresh box every render would mean a field that was just marked invalid or
    /// modified forgets that state on the very next render, since the new box's identity doesn't
    /// match the old one's. <see cref="InvalidateListItemBoxes"/> deliberately evicts every box for
    /// a list whenever it's structurally mutated (add/remove), since indices shift meaning at that
    /// point and inheriting stale state from the old occupant of an index would be wrong, not just
    /// unnecessary.</summary>
    private readonly Dictionary<(IList List, int Index), object> _listItemBoxes = new();

    private static object GetOrCreateListItemBox(Dictionary<(IList List, int Index), object> cache, Type itemType, IList list, int index)
    {
        var key = (list, index);
        if (cache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var boxType = typeof(ListItemBox<>).MakeGenericType(itemType);
        var box = Activator.CreateInstance(boxType, list, index)!;
        cache[key] = box;
        return box;
    }

    private void InvalidateListItemBoxes(IList list)
    {
        foreach (var key in _listItemBoxes.Keys.Where(k => ReferenceEquals(k.List, list)).ToArray())
        {
            _listItemBoxes.Remove(key);
        }
    }

    private void RenderListProperty(RenderTreeBuilder builder, FieldTarget target, Type listType, Type itemType, object? value)
    {
        if (value is null)
        {
            value = Activator.CreateInstance(listType)!;
            target.SetValue(value);
        }

        var list = (IList)value;

        if (WizardTypeInspection.IsComplexType(itemType))
        {
            RenderComplexItemRepeater(builder, target, itemType, list);
        }
        else
        {
            RenderScalarItemRepeater(builder, target, itemType, list);
        }
    }

    /// <summary>A repeating row of single-value inputs -- <c>List&lt;string&gt;</c>,
    /// <c>List&lt;int&gt;</c>, <c>List&lt;Guid&gt;</c>, etc. Each row's field reuses the exact same
    /// tier 2/2b dispatch a normal scalar property would get (<see cref="RenderDispatched"/>,
    /// targeting a <see cref="ListItemBox{TItem}"/> instead of a real property owner), so a new
    /// item type Just Works the moment tier 2/2b supports it -- no separate registration.</summary>
    private void RenderScalarItemRepeater(RenderTreeBuilder builder, FieldTarget target, Type itemType, IList list)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "wizard-list-repeater");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "wizard-list-repeater__label");
        builder.AddContent(4, target.Label);
        builder.CloseElement(); // label div

        var boxType = typeof(ListItemBox<>).MakeGenericType(itemType);
        var valueProperty = boxType.GetProperty(nameof(ListItemBox<object>.Value))!;

        builder.OpenRegion(5);
        var seq = 0;
        for (var i = 0; i < list.Count; i++)
        {
            var index = i;
            var box = GetOrCreateListItemBox(_listItemBoxes, itemType, list, index);
            var itemTarget = new FieldTarget(box, valueProperty, $"{target.Label} {index + 1}");
            var itemValue = list[index];

            builder.OpenRegion(seq++);
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "wizard-list-repeater__row");
            builder.OpenRegion(2);
            RenderDispatched(builder, itemTarget, itemType, itemValue);
            builder.CloseRegion();
            builder.OpenElement(3, "button");
            builder.AddAttribute(4, "type", "button");
            builder.AddAttribute(5, "class", "wizard-list-repeater__remove");
            builder.AddAttribute(6, "onclick", EventCallback.Factory.Create(this, () =>
            {
                list.RemoveAt(index);
                InvalidateListItemBoxes(list);
                OnFieldChanged();
            }));
            builder.AddContent(7, "Remove");
            builder.CloseElement(); // button
            builder.CloseElement(); // row div
            builder.CloseRegion();
        }
        builder.CloseRegion();

        builder.OpenElement(6, "button");
        builder.AddAttribute(7, "type", "button");
        builder.AddAttribute(8, "class", "wizard-list-repeater__add");
        builder.AddAttribute(9, "onclick", EventCallback.Factory.Create(this, () =>
        {
            list.Add(DefaultItemValue(itemType));
            OnFieldChanged();
        }));
        builder.AddContent(10, $"+ Add {target.Label}");
        builder.CloseElement(); // add button

        builder.CloseElement(); // outer div
    }

    /// <summary>A repeating group of sub-forms -- <c>List&lt;Beneficiary&gt;</c>-style. Each
    /// item's own properties render via the identical per-property loop
    /// <see cref="RenderExpandedGroup"/> uses for a single nested object, just once per list item
    /// -- a list item's fields are ordinary property-owned <see cref="FieldTarget"/>s (owner =
    /// the item instance itself, which is reference-stable across renders since it's the actual
    /// list element, not a wrapper), so they get full validation support *within* the item exactly
    /// like today's nested groups do. What a list item's fields can't do is depend on a *sibling*
    /// top-level property outside the list (DESIGN-DISCUSSION.md B.6/G.27 -- the same
    /// nested-DependsOn limitation nested groups already have).</summary>
    private void RenderComplexItemRepeater(RenderTreeBuilder builder, FieldTarget target, Type itemType, IList list)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "wizard-list-repeater wizard-list-repeater--complex");

        var itemProperties = itemType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0
                && p.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold != false)
            .ToArray();

        builder.OpenRegion(2);
        var seq = 0;
        for (var i = 0; i < list.Count; i++)
        {
            var index = i;
            var itemInstance = list[index]!;

            builder.OpenRegion(seq++);
            builder.OpenElement(0, "fieldset");
            builder.AddAttribute(1, "class", "wizard-field-group wizard-list-repeater__item");
            builder.OpenElement(2, "legend");
            builder.AddContent(3, $"{target.Label} {index + 1}");
            builder.CloseElement(); // legend

            builder.OpenRegion(4);
            var propSeq = 0;
            foreach (var itemProperty in itemProperties)
            {
                var propLabel = itemProperty.GetCustomAttribute<DisplayAttribute>()?.Name ?? itemProperty.Name;
                var propTarget = new FieldTarget(itemInstance, itemProperty, propLabel);
                var propValue = itemProperty.GetValue(itemInstance);

                builder.OpenRegion(propSeq++);
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "wizard-field-group__item");
                builder.OpenElement(2, "label");
                builder.AddContent(3, propLabel);
                builder.CloseElement(); // label
                builder.OpenRegion(4);
                RenderDispatched(builder, propTarget, itemProperty.PropertyType, propValue);
                builder.CloseRegion();
                builder.CloseElement(); // item div
                builder.CloseRegion();
            }
            builder.CloseRegion();

            builder.OpenElement(5, "button");
            builder.AddAttribute(6, "type", "button");
            builder.AddAttribute(7, "class", "wizard-list-repeater__remove");
            builder.AddAttribute(8, "onclick", EventCallback.Factory.Create(this, () =>
            {
                list.RemoveAt(index);
                OnFieldChanged();
            }));
            builder.AddContent(9, "Remove");
            builder.CloseElement(); // remove button

            builder.CloseElement(); // fieldset
            builder.CloseRegion();
        }
        builder.CloseRegion();

        builder.OpenElement(3, "button");
        builder.AddAttribute(4, "type", "button");
        builder.AddAttribute(5, "class", "wizard-list-repeater__add");
        builder.AddAttribute(6, "onclick", EventCallback.Factory.Create(this, () =>
        {
            list.Add(Activator.CreateInstance(itemType));
            OnFieldChanged();
        }));
        builder.AddContent(7, $"+ Add {target.Label}");
        builder.CloseElement(); // add button

        builder.CloseElement(); // outer div
    }

    /// <summary>What a freshly-Added scalar list item should start as. Non-generic
    /// <see cref="IList.Add"/> rejects a plain <c>null</c> for a genuinely non-nullable value type
    /// (e.g. <c>List&lt;int&gt;</c>) -- it needs a real default instance instead; a
    /// <see cref="Nullable{T}"/> item type accepts <c>null</c> just fine (an unset optional item
    /// is a legitimate value, not an error), and so does any reference type.</summary>
    private static object? DefaultItemValue(Type itemType)
    {
        if (itemType == typeof(string))
        {
            return string.Empty;
        }
        if (!itemType.IsValueType || Nullable.GetUnderlyingType(itemType) is not null)
        {
            return null;
        }
        return Activator.CreateInstance(itemType);
    }
}
