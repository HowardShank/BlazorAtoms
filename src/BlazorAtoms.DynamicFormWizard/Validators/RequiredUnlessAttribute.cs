using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace BlazorAtoms.DynamicFormWizard.Validators;

/// <summary>
/// Requires a non-null (or non-empty-string) value unless a sibling <c>bool</c> property on the
/// SAME instance is currently <c>true</c> -- the per-instance counterpart to stock
/// <see cref="RequiredAttribute"/>. Needed because <see cref="RequiredAttribute"/> is type-level:
/// it can't express "most survey statements are mandatory, but this particular one is optional"
/// when that varies per list item, not per type (DESIGN-DISCUSSION.md section I addendum --
/// "user can skip"). Reflects <see cref="ValidationContext.ObjectInstance"/> for
/// <see cref="SkipWhenProperty"/> the same way <c>[Compare]</c> already reflects a sibling
/// property off the whole model (H.29) -- no new plumbing, the exact mechanism this engine's
/// validation already relies on.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class RequiredUnlessAttribute : ValidationAttribute
{
    /// <summary>Name of the sibling <c>bool</c> property, on the same instance, that opts this
    /// one out of being required when <c>true</c>.</summary>
    public string SkipWhenProperty { get; }

    public RequiredUnlessAttribute(string skipWhenProperty)
    {
        SkipWhenProperty = skipWhenProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var skipProperty = validationContext.ObjectInstance.GetType()
            .GetProperty(SkipWhenProperty, BindingFlags.Public | BindingFlags.Instance);
        if (skipProperty?.GetValue(validationContext.ObjectInstance) is true)
        {
            return ValidationResult.Success;
        }

        var isEmpty = value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            _ => false,
        };

        return isEmpty
            ? new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.", new[] { validationContext.MemberName! })
            : ValidationResult.Success;
    }
}
