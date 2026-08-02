using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using BlazorAtoms.DynamicFormWizard.Files;

namespace BlazorAtoms.DynamicFormWizard.Validators;

/// <summary>Restricts a <see cref="WizardFileAttachment"/> collection property to files whose
/// extension (case-insensitive, with or without a leading dot) is in the allowed set.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class AllowedExtensionsAttribute : ValidationAttribute
{
    private readonly HashSet<string> _extensions;

    public AllowedExtensionsAttribute(params string[] extensions) =>
        _extensions = extensions.Select(Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string ext) => ext.StartsWith('.') ? ext : $".{ext}";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IEnumerable<WizardFileAttachment> files)
        {
            return ValidationResult.Success;
        }

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName);
            if (!_extensions.Contains(extension))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"'{file.FileName}' has an unsupported extension.",
                    new[] { validationContext.MemberName! });
            }
        }
        return ValidationResult.Success;
    }
}
