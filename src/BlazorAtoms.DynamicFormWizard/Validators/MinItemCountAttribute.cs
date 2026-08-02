using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace BlazorAtoms.DynamicFormWizard.Validators;

/// <summary>
/// Requires a repeating <c>List&lt;T&gt;</c> property (DESIGN-DISCUSSION.md G.25) to hold at
/// least <see cref="_min"/> items. Mirrors <see cref="MaxFileCountAttribute"/>'s shape -- a
/// null list counts as zero items, so pairing this with a required minimum above zero works
/// without also needing <c>[Required]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MinItemCountAttribute : ValidationAttribute
{
    private readonly int _min;

    public MinItemCountAttribute(int min) => _min = min;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var count = (value as ICollection)?.Count ?? 0;
        return count >= _min
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"At least {_min} item(s) required.", new[] { validationContext.MemberName! });
    }
}
