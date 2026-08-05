using System;

namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>
/// Declares a <c>List&lt;TItem&gt;</c> property as a survey/Likert-style matrix -- a grid of
/// statements (rows, one per list item) rated against one shared fixed scale (columns), rendered
/// as a native <c>&lt;table&gt;</c> with a radio-button group per row (DESIGN-DISCUSSION.md
/// section I). <see cref="AnswerProperty"/> should be a nullable enum on <c>TItem</c> (nullable so
/// "not yet answered" is representable -- a non-nullable enum would silently pre-select its first
/// member the way an ordinary enum property does today, which is actively wrong for survey data).
/// <see cref="LabelProperty"/> is <c>TItem</c>'s own per-row label -- instance data, not
/// <c>[Display(Name=...)]</c> (which is per-*type* metadata, fixed at compile time and shared by
/// every row).
///
/// Rendering this attribute drives (<c>DynamicWizard.Matrix.cs</c>'s <c>RenderMatrixGrid</c>) is
/// tracked separately (#164) -- this attribute and its schema wiring alone do not yet change how a
/// carrying <c>List&lt;TItem&gt;</c> property renders.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FormMatrixAttribute : Attribute
{
    /// <summary>Name of the property on <c>TItem</c> holding the per-row answer (a nullable enum).</summary>
    public string AnswerProperty { get; }

    /// <summary>Name of the property on <c>TItem</c> holding the per-row label (instance data, e.g.
    /// the statement text).</summary>
    public string LabelProperty { get; }

    public FormMatrixAttribute(string answerProperty, string labelProperty)
    {
        AnswerProperty = answerProperty;
        LabelProperty = labelProperty;
    }
}
