using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using BlazorAtoms.DynamicFormWizard.Attributes;

namespace BlazorAtoms.DynamicFormWizard.Schema;

/// <summary>
/// Everything the engine needs about one property, reflected once and cached for the lifetime of
/// the process (DESIGN-DISCUSSION.md F.19) -- never re-walked per render or per keystroke.
/// </summary>
public sealed class WizardPropertySchema
{
    public PropertyInfo Property { get; }
    public int StepNumber { get; }
    public string? StepTitle { get; }
    public int Order { get; }
    public string Label { get; }
    public IReadOnlyList<DependsOnAttribute> Dependencies { get; }
    public IReadOnlyList<FormPathEndAttribute> PathEndConditions { get; }
    public IReadOnlyList<ValidationAttribute> Validators { get; }
    public FormSelectAttribute? Select { get; }
    public FormDynamicSelectAttribute? DynamicSelect { get; }
    public FormLayoutAttribute? Layout { get; }

    /// <summary>From <c>[FormMatrix(answerProperty, labelProperty)]</c> (DESIGN-DISCUSSION.md
    /// section I, #163) -- present only on a <c>List&lt;TItem&gt;</c> property meant to render as a
    /// survey/Likert matrix. Reading this alone does not yet change rendering; that dispatch lands
    /// separately (#164).</summary>
    public FormMatrixAttribute? Matrix { get; }

    /// <summary>Null when the property carries no <see cref="FormLabelAttribute"/> -- the
    /// effective position then falls back to <see cref="DynamicWizard{TModel}.DefaultLabelPosition"/>,
    /// which is a runtime component parameter, not something this process-lifetime-cached schema
    /// can bake in.</summary>
    public LabelPosition? LabelPositionOverride { get; }

    /// <summary>From <c>[Display(Prompt = "...")]</c> -- stock DataAnnotations' own placeholder/
    /// watermark field, reused rather than inventing a new attribute. Applies regardless of
    /// <see cref="LabelPositionOverride"/>: a visible label above the field and a placeholder hint
    /// inside it are not mutually exclusive.</summary>
    public string? Placeholder { get; }

    /// <summary>Reflection encounter order -- the tie-break for properties with no explicit
    /// <see cref="FormOrderAttribute"/> (DESIGN-DISCUSSION.md C.10). Not guaranteed stable across
    /// an inheritance hierarchy on its own, which is exactly why an explicit order wins first.</summary>
    internal int EncounterIndex { get; }

    internal WizardPropertySchema(
        PropertyInfo property,
        int stepNumber,
        string? stepTitle,
        int order,
        string label,
        IReadOnlyList<DependsOnAttribute> dependencies,
        IReadOnlyList<FormPathEndAttribute> pathEndConditions,
        IReadOnlyList<ValidationAttribute> validators,
        FormSelectAttribute? select,
        FormDynamicSelectAttribute? dynamicSelect,
        FormLayoutAttribute? layout,
        LabelPosition? labelPositionOverride,
        string? placeholder,
        FormMatrixAttribute? matrix,
        int encounterIndex)
    {
        Property = property;
        StepNumber = stepNumber;
        StepTitle = stepTitle;
        Order = order;
        Label = label;
        Dependencies = dependencies;
        PathEndConditions = pathEndConditions;
        Validators = validators;
        Select = select;
        DynamicSelect = dynamicSelect;
        Layout = layout;
        LabelPositionOverride = labelPositionOverride;
        Placeholder = placeholder;
        Matrix = matrix;
        EncounterIndex = encounterIndex;
    }
}
