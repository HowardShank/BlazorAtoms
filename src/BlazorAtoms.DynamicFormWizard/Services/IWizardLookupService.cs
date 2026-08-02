using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorAtoms.DynamicFormWizard.Services;

/// <summary>
/// Resolves <see cref="Attributes.FormDynamicSelectAttribute"/> provider keys to a set of
/// options. Register an implementation in DI to enable dynamic dropdowns -- see
/// DESIGN-DISCUSSION.md F.20. Lives in this package (not a third-party dependency) but is a
/// stated consumer setup requirement: without a registration, dynamic dropdowns simply never
/// populate.
/// </summary>
public interface IWizardLookupService
{
    /// <summary>Returns a value -> display-label dictionary for the given provider key.</summary>
    Task<IReadOnlyDictionary<string, string>> GetOptionsAsync(string providerKey);
}
