using System.Collections.Generic;

namespace BlazorAtoms.DynamicFormWizard.Schema;

/// <summary>
/// One declared step: its authoring-key number and its properties in render order. Whether this
/// step is actually reachable for a given model *instance* is evaluated at runtime by the
/// navigation engine, not here -- this schema is static per-<see cref="System.Type"/>, computed
/// once regardless of the model's current field values.
/// </summary>
public sealed class WizardStepSchema
{
    public int StepNumber { get; }
    public IReadOnlyList<WizardPropertySchema> Properties { get; }

    internal WizardStepSchema(int stepNumber, IReadOnlyList<WizardPropertySchema> properties)
    {
        StepNumber = stepNumber;
        Properties = properties;
    }
}
