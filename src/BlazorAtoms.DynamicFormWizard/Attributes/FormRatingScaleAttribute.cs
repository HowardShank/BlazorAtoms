using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Renders an <c>int?</c> property as a numbered rating scale -- a row of <see cref="Min"/>..
/// <see cref="Max"/> radio points flanked by two endpoint labels, e.g. "How satisfied are you with
/// the product?" with circles 1-5 between "Not satisfied" and "Completely satisfied"
/// (DESIGN-DISCUSSION.md section J). The property should be nullable so an unrated question isn't
/// silently recorded as a real rating -- a plain non-nullable <c>int</c> defaulting to 0 (or
/// <see cref="Min"/>) would misrecord "no opinion" as an actual answer.
///
/// Numeric points only, labeled at the two endpoints -- no per-point labeling (that's a distinct,
/// deferred idea; don't build it speculatively). Pair with <c>[Required]</c>/<c>[Range(Min, Max)]</c>
/// for enforcement -- this attribute only changes rendering, the same "reuse stock validation,
/// zero new validation code" pattern this engine uses everywhere else.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FormRatingScaleAttribute : Attribute
{
    public int Min { get; }
    public int Max { get; }
    public string MinLabel { get; }
    public string MaxLabel { get; }

    public FormRatingScaleAttribute(int min, int max, string minLabel, string maxLabel)
    {
        Min = min;
        Max = max;
        MinLabel = minLabel;
        MaxLabel = maxLabel;
    }
}
