using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BlazorAtoms.DynamicFormWizard.Files;

namespace BlazorAtoms.DynamicFormWizard.Validators;

/// <summary>Limits the size of each individual file in a <see cref="WizardFileAttachment"/>
/// collection property.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly long _maxBytesPerFile;

    public MaxFileSizeAttribute(long maxBytesPerFile) => _maxBytesPerFile = maxBytesPerFile;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IEnumerable<WizardFileAttachment> files)
        {
            return ValidationResult.Success;
        }

        foreach (var file in files)
        {
            if (file.Size > _maxBytesPerFile)
            {
                return new ValidationResult(
                    ErrorMessage ?? $"'{file.FileName}' exceeds the {_maxBytesPerFile:N0}-byte limit.",
                    new[] { validationContext.MemberName! });
            }
        }
        return ValidationResult.Success;
    }
}
