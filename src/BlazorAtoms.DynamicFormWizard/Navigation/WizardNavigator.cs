using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using BlazorAtoms.DynamicFormWizard.Schema;

namespace BlazorAtoms.DynamicFormWizard.Navigation;

/// <summary>
/// Stateful navigation engine over one model instance's <see cref="WizardModelSchema"/> --
/// current step, dynamic effective step count/position, DependsOn visibility evaluation, and
/// partial per-step validation. Deliberately framework-agnostic (no dependency on a live render)
/// so it's directly unit-testable; the wizard component (DynamicWizard&lt;TModel&gt;) wraps one of
/// these per instance and owns the EditContext/re-render side of things.
/// </summary>
public sealed class WizardNavigator
{
    private readonly WizardModelSchema _schema;
    private readonly object _model;

    public WizardNavigator(WizardModelSchema schema, object model)
    {
        _schema = schema;
        _model = model;
        CurrentStep = schema.Steps.Count > 0 ? schema.Steps[0].StepNumber : 0;
    }

    /// <summary>The raw declared step number the wizard is currently on -- an internal key, never
    /// shown to the user directly (DESIGN-DISCUSSION.md C.9).</summary>
    public int CurrentStep { get; private set; }

    /// <summary>Whether a property is currently visible: true when it has no
    /// <see cref="Attributes.DependsOnAttribute"/>s, or when every one of its stacked
    /// dependencies currently matches (AND-combined -- DESIGN-DISCUSSION.md C.11).</summary>
    public bool IsVisible(WizardPropertySchema property)
    {
        foreach (var dep in property.Dependencies)
        {
            var target = _schema.TryGetByName(dep.TargetProperty);
            var actual = target?.Property.GetValue(_model);
            if (actual is null || !actual.Equals(dep.ExpectedValue))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>True when any property in <paramref name="stepNumber"/>'s step carries a
    /// <see cref="Attributes.FormPathEndAttribute"/> whose stacked conditions all currently match
    /// (AND-combined, same shape as <see cref="IsVisible"/>). An authoritative "the path stops
    /// here" -- unlike ordinary visibility, this is evaluated independently of whether the
    /// property itself is otherwise shown, and it overrides whatever is declared on later steps
    /// regardless of their own (mis)configuration (DESIGN-DISCUSSION.md G.29 addendum).</summary>
    public bool IsPathEndMarked(int stepNumber)
    {
        var step = _schema.Steps.FirstOrDefault(s => s.StepNumber == stepNumber);
        if (step is null)
        {
            return false;
        }

        foreach (var property in step.Properties)
        {
            if (property.PathEndConditions.Count == 0)
            {
                continue;
            }

            var allMatch = true;
            foreach (var end in property.PathEndConditions)
            {
                var target = _schema.TryGetByName(end.TargetProperty);
                var actual = target?.Property.GetValue(_model);
                if (actual is null || !actual.Equals(end.ExpectedValue))
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The declared step numbers reachable on the current path, ascending: each step
    /// with at least one visible property, up to and including the first one whose
    /// <see cref="IsPathEndMarked"/> is true (a marked step is included, then the walk stops --
    /// nothing after it counts, no matter how it's configured). Recomputed live on every call --
    /// never a static snapshot (DESIGN-DISCUSSION.md C.9) -- since two branches through the same
    /// model can have different true lengths.</summary>
    public IReadOnlyList<int> EffectiveStepNumbers()
    {
        var result = new List<int>();
        foreach (var step in _schema.Steps)
        {
            if (step.Properties.Any(IsVisible))
            {
                result.Add(step.StepNumber);
            }
            if (IsPathEndMarked(step.StepNumber))
            {
                break;
            }
        }
        return result;
    }

    /// <summary>Properties of <see cref="CurrentStep"/> that are currently visible, in render order.</summary>
    public IReadOnlyList<WizardPropertySchema> VisiblePropertiesForCurrentStep() =>
        VisiblePropertiesFor(CurrentStep);

    private IReadOnlyList<WizardPropertySchema> VisiblePropertiesFor(int stepNumber)
    {
        var step = _schema.Steps.FirstOrDefault(s => s.StepNumber == stepNumber);
        return step is null ? Array.Empty<WizardPropertySchema>() : step.Properties.Where(IsVisible).ToArray();
    }

    /// <summary>1-based position of <see cref="CurrentStep"/> among the effective (currently
    /// reachable) steps, and their total count -- e.g. "Step 2 of 3." Never the raw declared step
    /// number or a static declared total (DESIGN-DISCUSSION.md C.9): two branches through the same
    /// model can have different true lengths.</summary>
    public (int Position, int Count) DisplayPosition()
    {
        var effective = EffectiveStepNumbers();
        var index = -1;
        for (var i = 0; i < effective.Count; i++)
        {
            if (effective[i] == CurrentStep)
            {
                index = i;
                break;
            }
        }
        return (index + 1, effective.Count);
    }

    /// <summary>The step's human label: the first non-null <c>Title</c> among its currently
    /// *visible* properties, or a computed "Step {position}" fallback using the dynamic ordinal
    /// above -- never the raw declared step number (DESIGN-DISCUSSION.md C.9).</summary>
    public string DisplayTitle()
    {
        var step = _schema.Steps.FirstOrDefault(s => s.StepNumber == CurrentStep);
        var title = step?.Properties.Where(IsVisible).Select(p => p.StepTitle).FirstOrDefault(t => t is not null);
        if (title is not null)
        {
            return title;
        }

        var (position, _) = DisplayPosition();
        return $"Step {position}";
    }

    /// <summary>Advances past <see cref="CurrentStep"/> to the next declared step number that
    /// currently has a visible property, skipping any that don't (DESIGN-DISCUSSION.md C.8) --
    /// this skip is the entire mechanism behind both branching and "rejoining" after a fork, with
    /// no separate merge construct. Does not validate; call <see cref="ValidateCurrentStep"/>
    /// first if that's required before advancing. A no-op if nothing further is reachable.</summary>
    public void GoNext()
    {
        if (IsPathEndMarked(CurrentStep))
        {
            return; // authoritative stop -- never advance past a marked step, regardless of what follows
        }

        var candidates = _schema.Steps.Select(s => s.StepNumber).Where(n => n > CurrentStep).OrderBy(n => n);
        foreach (var candidate in candidates)
        {
            if (VisiblePropertiesFor(candidate).Count > 0)
            {
                CurrentStep = candidate;
                return;
            }
        }
    }

    /// <summary>Mirrors <see cref="GoNext"/> backward -- walks to the nearest earlier declared
    /// step number with a currently-visible property, skipping any without. A no-op at the first
    /// reachable step.</summary>
    public void GoPrevious()
    {
        var candidates = _schema.Steps.Select(s => s.StepNumber).Where(n => n < CurrentStep).OrderByDescending(n => n);
        foreach (var candidate in candidates)
        {
            if (VisiblePropertiesFor(candidate).Count > 0)
            {
                CurrentStep = candidate;
                return;
            }
        }
    }

    /// <summary>True when <see cref="CurrentStep"/> is authoritatively marked
    /// (<see cref="IsPathEndMarked"/>) or, absent a marker, once no declared step after it has
    /// anything visible -- i.e. this is the last screen on the current path, and a Submit action
    /// (not Next) should show.</summary>
    public bool IsFinalStep() =>
        IsPathEndMarked(CurrentStep)
        || _schema.Steps.Select(s => s.StepNumber).Where(n => n > CurrentStep).All(n => VisiblePropertiesFor(n).Count == 0);

    /// <summary>Validates only <see cref="CurrentStep"/>'s currently-visible properties (partial,
    /// per-step -- DESIGN-DISCUSSION.md D.12), never the whole model, so a hidden/not-yet-relevant
    /// step's rules can never block navigation on this one. Clears and repopulates
    /// <paramref name="store"/>; the caller owns calling
    /// <see cref="EditContext.NotifyValidationStateChanged"/> afterward, since only the caller
    /// knows when a re-render should happen.</summary>
    public bool ValidateCurrentStep(ValidationMessageStore store)
    {
        store.Clear();
        var isValid = true;

        foreach (var property in VisiblePropertiesForCurrentStep())
        {
            var value = property.Property.GetValue(_model);
            var field = new FieldIdentifier(_model, property.Property.Name);
            var results = new List<ValidationResult>();

            // Auto-expanded complex-typed properties (DESIGN-DISCUSSION.md B.5) validate
            // recursively via TryValidateObject; every scalar leaf uses TryValidateValue against
            // its own cached ValidationAttributes. A null group has nothing to recurse into.
            var propertyType = property.Property.PropertyType;
            bool ok;
            if (WizardTypeInspection.IsComplexType(propertyType))
            {
                ok = value is null || Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
            }
            else
            {
                var context = new ValidationContext(_model) { MemberName = property.Property.Name };
                ok = Validator.TryValidateValue(value!, context, results, property.Validators);
            }

            if (!ok)
            {
                isValid = false;
                foreach (var result in results)
                {
                    store.Add(field, result.ErrorMessage ?? "Invalid value.");
                }
            }
        }

        return isValid;
    }
}
