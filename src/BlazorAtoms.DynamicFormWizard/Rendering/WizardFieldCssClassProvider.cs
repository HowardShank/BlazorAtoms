using System.Linq;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.DynamicFormWizard.Rendering;

/// <summary>
/// Maps <see cref="EditContext"/> field state to bare, framework-agnostic CSS classes
/// (<c>wizard-field--invalid</c>/<c>wizard-field--valid</c>) -- never Bootstrap's
/// <c>is-invalid</c>/<c>is-valid</c> (DESIGN-DISCUSSION.md F.21: no framework classes anywhere,
/// CSS-variable overrides for consumers regardless of which CSS approach they use).
/// </summary>
public sealed class WizardFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        if (editContext.GetValidationMessages(fieldIdentifier).Any())
        {
            return "wizard-field--invalid";
        }

        return editContext.IsModified(fieldIdentifier) ? "wizard-field--valid" : string.Empty;
    }
}
