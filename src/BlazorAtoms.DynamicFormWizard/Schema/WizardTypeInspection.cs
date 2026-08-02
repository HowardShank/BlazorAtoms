using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorAtoms.DynamicFormWizard.Schema;

/// <summary>
/// Shared "is this a scalar, a group, or a repeating list" tests, used identically by validation
/// (<see cref="Navigation.WizardNavigator.ValidateCurrentStep"/>'s
/// <c>Validator.TryValidateObject</c> vs. leaf <c>TryValidateValue</c> branch) and by the
/// field-render dispatch (DESIGN-DISCUSSION.md A.4/B.5/G.25) -- all call sites must agree on the
/// same types, or a property could render one way and validate another.
/// </summary>
public static class WizardTypeInspection
{
    /// <summary>True when <paramref name="type"/> is a reference type with its own public
    /// read/write properties -- i.e. a candidate for auto-expansion into a field group, rather
    /// than a single scalar input. <see cref="string"/> is deliberately excluded even though it's
    /// a reference type -- it's a built-in scalar (DESIGN-DISCUSSION.md A.4 tier 2), not a group.
    /// Indexers (e.g. <see cref="System.Text.StringBuilder"/>'s <c>Chars[int]</c>) report
    /// <c>CanRead</c>/<c>CanWrite</c> too but require index arguments to get/set -- excluded here
    /// so a type is never misclassified as a renderable group solely because of one. Collection
    /// types (anything implementing non-generic <see cref="System.Collections.IEnumerable"/>) are
    /// also excluded -- <c>List&lt;T&gt;</c>'s only public read/write, non-indexer property is
    /// <c>Capacity</c> (an <c>int</c>), so without this check a <c>List&lt;string&gt;</c> property
    /// would auto-expand into a field group showing only a "Capacity" number input, hiding every
    /// actual list item. <c>List&lt;T&gt;</c> itself is handled by its own dedicated tier instead
    /// -- see <see cref="TryGetListItemType"/> -- checked *before* this method is ever consulted
    /// for one, so this exclusion is what makes that ordering safe. Every other collection shape
    /// (<c>Dictionary</c>, <c>HashSet</c>, arrays, a consumer's own custom collection) is
    /// genuinely out of scope and correctly falls to the tier-4 fallback (DESIGN-DISCUSSION.md
    /// G.25 -- deliberately scoped to <c>List&lt;T&gt;</c> only for v1).</summary>
    public static bool IsComplexType(Type type) =>
        type.IsClass
        && type != typeof(string)
        && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
        && type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);

    /// <summary>True when <paramref name="type"/> is exactly <c>List&lt;TItem&gt;</c> for some
    /// closed <paramref name="itemType"/> (DESIGN-DISCUSSION.md G.25) -- deliberately narrower
    /// than "any collection": <c>IList&lt;T&gt;</c>/<c>ICollection&lt;T&gt;</c>/arrays/etc. are not
    /// matched, keeping the repeating-group feature's surface small and its mutation model simple
    /// (<c>Activator.CreateInstance(typeof(List&lt;&gt;).MakeGenericType(itemType))</c>,
    /// <c>IList.Add</c>/<c>RemoveAt</c> all just work against the concrete type). Used by both the
    /// field-render dispatch (to pick the repeating-group vs. repeating-scalar-row renderer) and
    /// step validation (to also validate each item when <paramref name="itemType"/> is itself a
    /// complex type).</summary>
    public static bool TryGetListItemType(Type type, out Type itemType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            itemType = type.GetGenericArguments()[0];
            return true;
        }
        itemType = typeof(object);
        return false;
    }
}
