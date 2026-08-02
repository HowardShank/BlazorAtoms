using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Pins a property's render order within its <see cref="FormStepAttribute"/> step. Kept as a
/// separate attribute rather than a parameter on <see cref="FormStepAttribute"/> because raw
/// reflection property enumeration is not guaranteed stable across an inheritance hierarchy -- a
/// real .NET gotcha, not a hypothetical one (DESIGN-DISCUSSION.md C.10). Properties without this
/// attribute sort after every explicitly-ordered property in the same step, in reflection
/// encounter order (best-effort only).
///
/// <para><b>Candidate for future removal (DESIGN-DISCUSSION.md H.33).</b> This duplicates
/// <see cref="DisplayAttribute.Order"/>, which already exists for exactly this purpose --
/// <see cref="Schema.WizardModelSchema"/> now reads <c>Display.GetOrder()</c> as a fallback when
/// this attribute isn't present, so a plain <c>[Display(Order = N)]</c> works today without this
/// type at all. Kept for now purely for backward compatibility with existing consumers (this
/// package's own playgrounds included) -- new code should prefer <c>[Display(Order = N)]</c>
/// (pairing naturally with <c>[Display(Name = ...)]</c>/<c>[Display(Prompt = ...)]</c>, which this
/// engine already reads for label/placeholder) over adding this attribute. A future major version
/// may remove this type once existing consumers have had a chance to migrate.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FormOrderAttribute : Attribute
{
    public int Order { get; }

    public FormOrderAttribute(int order) => Order = order;
}
