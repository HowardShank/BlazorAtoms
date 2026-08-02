using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BlazorAtoms.DynamicFormWizard.Validators;

/// <summary>
/// Validates a string property against a regular expression. Empty/whitespace values pass through
/// untouched -- pair with <see cref="RequiredAttribute"/> if the field is also mandatory, so the
/// two concerns (required vs. correctly-formatted) stay independent. Picked up automatically by
/// the wizard's per-step validation (DESIGN-DISCUSSION.md D.13) -- no engine changes needed to add
/// this or any other <see cref="ValidationAttribute"/> subclass.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FormRegexAttribute : ValidationAttribute
{
    private readonly string _pattern;

    public FormRegexAttribute(string pattern, string defaultErrorMessage)
    {
        _pattern = pattern;
        ErrorMessage = defaultErrorMessage;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        return Regex.IsMatch(value.ToString()!, _pattern)
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? "The field format is invalid.", new[] { validationContext.MemberName! });
    }
}
