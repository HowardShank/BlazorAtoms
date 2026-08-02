using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace BlazorAtoms.DynamicFormWizard.Validators;

/// <summary>
/// Limits how many files a <see cref="Files.WizardFileAttachment"/> collection property may
/// hold. A null/empty collection always passes -- "0 or more" is the default (DESIGN-DISCUSSION.md
/// E); pair with a custom minimum-count validator if at least one file is required.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MaxFileCountAttribute : ValidationAttribute
{
    private readonly int _max;

    public MaxFileCountAttribute(int max) => _max = max;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var count = (value as ICollection)?.Count ?? 0;
        return count <= _max
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"No more than {_max} file(s) allowed.", new[] { validationContext.MemberName! });
    }
}
