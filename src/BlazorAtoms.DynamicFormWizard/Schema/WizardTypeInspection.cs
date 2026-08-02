using System;
using System.Linq;
using System.Reflection;

namespace BlazorAtoms.DynamicFormWizard.Schema;

/// <summary>
/// Shared "is this a scalar or a group" test, used identically by validation (nested
/// <c>Validator.TryValidateObject</c> vs. leaf <c>TryValidateValue</c>) and by the field-render
/// dispatch's auto-expand tier (DESIGN-DISCUSSION.md A.4/B.5) -- the two must agree on the same
/// types, or a property could render as a group but validate as a leaf (or vice versa).
/// </summary>
public static class WizardTypeInspection
{
    /// <summary>True when <paramref name="type"/> is a reference type with its own public
    /// read/write properties -- i.e. a candidate for auto-expansion into a field group, rather
    /// than a single scalar input. <see cref="string"/> is deliberately excluded even though it's
    /// a reference type -- it's a built-in scalar (DESIGN-DISCUSSION.md A.4 tier 2), not a group.</summary>
    public static bool IsComplexType(Type type) =>
        type.IsClass
        && type != typeof(string)
        && type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.CanRead && p.CanWrite);
}
