using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using BlazorAtoms.DynamicFormWizard.Attributes;

namespace BlazorAtoms.DynamicFormWizard.Schema;

/// <summary>
/// The full attribute-driven schema for one model type -- steps, per-step property order,
/// dependencies, validators, and select metadata. Reflected exactly once per distinct
/// <see cref="Type"/> and cached forever (DESIGN-DISCUSSION.md F.19); every render, keystroke, and
/// navigation check reads this cache instead of re-walking reflection, unlike every Ideas.md
/// iteration (which re-reflected on every render).
/// </summary>
public sealed class WizardModelSchema
{
    private static readonly ConcurrentDictionary<Type, WizardModelSchema> Cache = new();

    /// <summary>Untagged properties default to step 1 -- a model with no <see cref="FormStepAttribute"/>
    /// anywhere still works, as a single-step form.</summary>
    private const int DefaultStepNumber = 1;

    public IReadOnlyList<WizardStepSchema> Steps { get; }
    private readonly IReadOnlyDictionary<string, WizardPropertySchema> _byName;

    private WizardModelSchema(IReadOnlyList<WizardStepSchema> steps, IReadOnlyDictionary<string, WizardPropertySchema> byName)
    {
        Steps = steps;
        _byName = byName;
    }

    /// <summary>Returns the cached schema for <typeparamref name="TModel"/>, building it on first use.</summary>
    public static WizardModelSchema For<TModel>() where TModel : class, new() => For(typeof(TModel));

    /// <summary>Returns the cached schema for <paramref name="modelType"/>, building it on first use.</summary>
    public static WizardModelSchema For(Type modelType) => Cache.GetOrAdd(modelType, Build);

    /// <summary>Looks up a property's schema by its declared name -- used to resolve
    /// <see cref="DependsOnAttribute.TargetProperty"/> without re-reflecting (DESIGN-DISCUSSION.md
    /// B.6: only top-level property names are reachable this way).</summary>
    public WizardPropertySchema? TryGetByName(string propertyName) =>
        _byName.TryGetValue(propertyName, out var schema) ? schema : null;

    private static WizardModelSchema Build(Type modelType)
    {
        var properties = modelType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // GetIndexParameters().Length == 0 excludes indexers -- they report CanRead/CanWrite
            // too but aren't renderable fields (see WizardTypeInspection.IsComplexType, which
            // must agree on the same definition). [ScaffoldColumn(false)] is excluded entirely --
            // it never becomes a step property at all, so it's never rendered, never validated,
            // and never counted toward a step's visibility, matching its EF/scaffolding intent of
            // "this doesn't exist for generated UI purposes."
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0
                && p.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold != false)
            .ToArray();

        var schemas = new List<WizardPropertySchema>(properties.Length);
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var formStep = property.GetCustomAttribute<FormStepAttribute>();
            var formOrder = property.GetCustomAttribute<FormOrderAttribute>();
            var display = property.GetCustomAttribute<DisplayAttribute>();

            schemas.Add(new WizardPropertySchema(
                property,
                stepNumber: formStep?.StepNumber ?? DefaultStepNumber,
                stepTitle: formStep?.Title,
                // [FormOrder] wins if present (explicit-attribute-wins, same pattern as every
                // other override here); falls back to [Display(Order=N)] -- GetOrder(), never the
                // `Order` property getter directly, since DisplayAttribute.Order throws
                // InvalidOperationException when never explicitly set ("Use the GetOrder method").
                order: formOrder?.Order ?? display?.GetOrder() ?? int.MaxValue,
                label: display?.Name ?? property.Name,
                dependencies: property.GetCustomAttributes<DependsOnAttribute>().ToArray(),
                pathEndConditions: property.GetCustomAttributes<FormPathEndAttribute>().ToArray(),
                validators: property.GetCustomAttributes<ValidationAttribute>().ToArray(),
                select: property.GetCustomAttribute<FormSelectAttribute>(),
                dynamicSelect: property.GetCustomAttribute<FormDynamicSelectAttribute>(),
                layout: property.GetCustomAttribute<FormLayoutAttribute>(),
                labelPositionOverride: property.GetCustomAttribute<FormLabelAttribute>()?.Position,
                placeholder: display?.Prompt,
                encounterIndex: i));
        }

        var steps = schemas
            .GroupBy(s => s.StepNumber)
            .OrderBy(g => g.Key)
            .Select(g => new WizardStepSchema(
                g.Key,
                g.OrderBy(s => s.Order).ThenBy(s => s.EncounterIndex).ToArray()))
            .ToArray();

        var byName = schemas.ToDictionary(s => s.Property.Name, s => s);

        return new WizardModelSchema(steps, byName);
    }
}
