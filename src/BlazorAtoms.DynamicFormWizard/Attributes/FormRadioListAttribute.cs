using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Swaps an enum (or nullable-enum) property's default <c>&lt;select&gt;</c> dropdown rendering
/// for a stacked, vertical native radio group -- e.g. "Compared to our competitors, do you feel
/// the product is: Less expensive / Priced about the same / More expensive / Not sure"
/// (DESIGN-DISCUSSION.md section K). A bare marker: it doesn't configure anything, it only
/// changes which built-in component the engine opens for this one property. Works on a nullable
/// enum too (mirroring the existing nullable-enum dropdown's own handling) for the cases where "no
/// answer yet" is genuinely distinct from every listed choice -- unlike a plain enum, where every
/// member is a real, meaningful answer (e.g. "Not sure" as an actual member, not a stand-in for
/// null).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FormRadioListAttribute : Attribute
{
}
