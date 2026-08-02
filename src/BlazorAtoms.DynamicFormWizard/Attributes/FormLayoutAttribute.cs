using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Lays a field out in a CSS Grid alongside its step siblings instead of stacking one field per
/// row -- a bare-CSS re-derivation of `Ideas.md` iteration 4's Bootstrap `col-md-N` column span
/// (DESIGN-DISCUSSION.md F.21), driven by a `--wizard-column-span` custom property rather than
/// framework classes, so it themes the same way as every other BlazorAtoms component regardless
/// of which CSS approach the consumer uses.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FormLayoutAttribute : Attribute
{
    /// <summary>How many of the grid's columns this field occupies, clamped into
    /// 1..<see cref="TotalColumns"/>.</summary>
    public int Span { get; }

    /// <summary>Total columns in the step's grid. Fixed at 12 for v1 (DESIGN-DISCUSSION.md) --
    /// stored per attribute instance for future flexibility, but the wizard's grid container
    /// currently always renders 12 columns regardless of this value.</summary>
    public int TotalColumns { get; }

    public FormLayoutAttribute(int span, int totalColumns = 12)
    {
        TotalColumns = totalColumns;
        Span = Math.Clamp(span, 1, totalColumns);
    }
}
