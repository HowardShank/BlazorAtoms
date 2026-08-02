using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace BlazorAtoms.DynamicFormWizard.Validators;

/// <summary>
/// Limits how many items a repeating <c>List&lt;T&gt;</c> property (DESIGN-DISCUSSION.md G.25)
/// may hold. Mirrors <see cref="MaxFileCountAttribute"/>'s shape -- a null/empty list always
/// passes, "0 or more" is the default; pair with <see cref="MinItemCountAttribute"/> if at least
/// one item is required.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MaxItemCountAttribute : ValidationAttribute
{
    private readonly int _max;

    public MaxItemCountAttribute(int max) => _max = max;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var count = (value as ICollection)?.Count ?? 0;
        return count <= _max
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"No more than {_max} item(s) allowed.", new[] { validationContext.MemberName! });
    }
}
