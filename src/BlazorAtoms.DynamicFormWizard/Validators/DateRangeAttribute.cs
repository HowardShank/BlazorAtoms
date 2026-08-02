using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorAtoms.DynamicFormWizard.Validators;

/// <summary>
/// Validates that a <see cref="DateTime"/> property falls within a window relative to
/// <see cref="DateTime.Now"/> at validation time (e.g. "tomorrow through 90 days out").
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class DateRangeAttribute : ValidationAttribute
{
    private readonly int _minDaysFromNow;
    private readonly int _maxDaysFromNow;

    public DateRangeAttribute(int minDaysFromNow, int maxDaysFromNow)
    {
        _minDaysFromNow = minDaysFromNow;
        _maxDaysFromNow = maxDaysFromNow;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime targetDate)
        {
            return new ValidationResult("Expected a date value.", new[] { validationContext.MemberName! });
        }

        var minAllowed = DateTime.Now.AddDays(_minDaysFromNow);
        var maxAllowed = DateTime.Now.AddDays(_maxDaysFromNow);

        return targetDate < minAllowed || targetDate > maxAllowed
            ? new ValidationResult(
                $"Date must be between {minAllowed:yyyy-MM-dd} and {maxAllowed:yyyy-MM-dd}.",
                new[] { validationContext.MemberName! })
            : ValidationResult.Success;
    }
}
