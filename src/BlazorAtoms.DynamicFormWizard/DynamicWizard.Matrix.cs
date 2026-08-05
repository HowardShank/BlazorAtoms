using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorAtoms.DynamicFormWizard.Attributes;
using BlazorAtoms.DynamicFormWizard.Validators;

namespace BlazorAtoms.DynamicFormWizard;

/// <summary>Survey/Likert matrix rendering (DESIGN-DISCUSSION.md section I, #163/#164) -- a grid of
/// statements (rows, one per list item) rated against one shared fixed scale (columns), rendered as
/// a real <c>&lt;table&gt;</c> with a native radio-button group per row. Checked ahead of the
/// ordinary <c>List&lt;T&gt;</c> repeater (<c>DynamicWizard.Lists.cs</c>) in
/// <c>RenderDispatched</c>'s tier 1b, mirroring the one-file-per-concern convention
/// <c>Fields.cs</c>/<c>Lists.cs</c>/<c>Selects.cs</c> already establish.</summary>
public partial class DynamicWizard<TModel> where TModel : class, new()
{
    /// <summary>Renders a <c>List&lt;TItem&gt;</c> carrying <c>[FormMatrix]</c> as a table --
    /// <c>&lt;th scope="col"&gt;</c> for each answer-enum member (accessible column association for
    /// free, DESIGN-DISCUSSION.md F.17) and <c>&lt;th scope="row"&gt;</c> for each item's own
    /// instance-data label (read via <see cref="FormMatrixAttribute.LabelProperty"/> -- this is the
    /// one thing no existing tier could already express, since every other label comes from
    /// type-level metadata, fixed at compile time). Each row's radios share a <c>name</c> unique to
    /// that row so exactly one can be selected per statement using plain native HTML radio-group
    /// semantics -- no extra JS, no manual "only one checked" bookkeeping.
    ///
    /// Two indicators, both read directly off <see cref="Attributes.RequiredAttribute"/>/
    /// <see cref="RequiredUnlessAttribute"/> and the live <c>EditContext</c> rather than any new
    /// matrix-specific validation state (an unanswered-but-required row previously blocked `Next`
    /// with zero visual signal as to which row -- "fails silently"):
    /// - A required marker next to a row's label when that row's answer is actually required
    ///   *right now* -- either unconditionally (<see cref="RequiredAttribute"/>) or conditionally
    ///   (<see cref="RequiredUnlessAttribute"/>, false for this item) -- since <c>RequiredUnless</c>
    ///   makes "required" a per-*row* fact, not a per-*type* one, the marker has to be recomputed
    ///   per row, unlike every other required-field indicator in this engine.
    /// - An invalid-row class once that row's own <see cref="FormMatrixAttribute.AnswerProperty"/>
    ///   has a validation message in the store -- the same <c>EditContext.GetValidationMessages</c>
    ///   check every other built-in field already makes for its own invalid-state CSS class, just
    ///   evaluated per table row instead of per input.</summary>
    private void RenderMatrixGrid(RenderTreeBuilder builder, FieldTarget target, Type listType, Type itemType, FormMatrixAttribute matrix, object? value)
    {
        if (value is null)
        {
            value = Activator.CreateInstance(listType)!;
            target.SetValue(value);
        }
        var list = (IList)value;

        var answerProperty = itemType.GetProperty(matrix.AnswerProperty, BindingFlags.Public | BindingFlags.Instance)!;
        var labelProperty = itemType.GetProperty(matrix.LabelProperty, BindingFlags.Public | BindingFlags.Instance)!;
        // The answer property is expected to be a nullable enum (an unanswered statement must not
        // look like a real answer, unlike RenderEnumSelect's default-to-first-member behavior for a
        // non-nullable enum) -- unwrap defensively so column enumeration works either way.
        var answerEnumType = Nullable.GetUnderlyingType(answerProperty.PropertyType) ?? answerProperty.PropertyType;
        var columnNames = Enum.GetNames(answerEnumType);

        var alwaysRequired = answerProperty.GetCustomAttribute<RequiredAttribute>() is not null;
        var requiredUnless = answerProperty.GetCustomAttribute<RequiredUnlessAttribute>();
        var skipProperty = requiredUnless is null
            ? null
            : itemType.GetProperty(requiredUnless.SkipWhenProperty, BindingFlags.Public | BindingFlags.Instance);

        builder.OpenElement(0, "table");
        builder.AddAttribute(1, "class", "wizard-matrix");

        builder.OpenElement(2, "thead");
        builder.OpenElement(0, "tr");
        builder.OpenElement(1, "th"); // blank corner cell above the row labels
        builder.CloseElement();
        builder.OpenRegion(2);
        {
            var seq = 0;
            foreach (var name in columnNames)
            {
                var field = answerEnumType.GetField(name);
                var display = field?.GetCustomAttribute<DisplayAttribute>();

                builder.OpenRegion(seq++);
                builder.OpenElement(0, "th");
                builder.AddAttribute(1, "scope", "col");
                builder.AddContent(2, display?.Name ?? name);
                builder.CloseElement();
                builder.CloseRegion();
            }
        }
        builder.CloseRegion();
        builder.CloseElement(); // tr
        builder.CloseElement(); // thead

        builder.OpenElement(3, "tbody");
        builder.OpenRegion(4);
        {
            var rowSeq = 0;
            for (var index = 0; index < list.Count; index++)
            {
                var item = list[index];
                if (item is null)
                {
                    continue;
                }

                var rowLabel = labelProperty.GetValue(item)?.ToString() ?? string.Empty;
                var currentAnswer = answerProperty.GetValue(item);
                var groupName = $"{target.Info.Name}-{index}";
                var capturedItem = item;

                var skippedForThisRow = skipProperty?.GetValue(item) is true;
                var isRequired = alwaysRequired || (requiredUnless is not null && !skippedForThisRow);
                var isInvalid = _editContext.GetValidationMessages(new FieldIdentifier(item, matrix.AnswerProperty)).Any();

                builder.OpenRegion(rowSeq++);
                builder.OpenElement(0, "tr");
                builder.AddAttribute(1, "class", isInvalid ? "wizard-matrix__row--invalid" : null);
                builder.OpenElement(2, "th");
                builder.AddAttribute(3, "scope", "row");
                builder.AddContent(4, rowLabel);
                if (isRequired)
                {
                    builder.OpenElement(5, "span");
                    builder.AddAttribute(6, "class", "wizard-matrix__required");
                    builder.AddAttribute(7, "aria-label", "required");
                    builder.AddContent(8, " *");
                    builder.CloseElement();
                }
                builder.CloseElement(); // th

                builder.OpenRegion(9);
                {
                    var cellSeq = 0;
                    foreach (var name in columnNames)
                    {
                        var memberValue = Enum.Parse(answerEnumType, name);
                        var isChecked = currentAnswer is not null && currentAnswer.Equals(memberValue);

                        builder.OpenRegion(cellSeq++);
                        builder.OpenElement(0, "td");
                        builder.OpenElement(1, "input");
                        builder.AddAttribute(2, "type", "radio");
                        builder.AddAttribute(3, "name", groupName);
                        builder.AddAttribute(4, "checked", isChecked);
                        builder.AddAttribute(5, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                        {
                            answerProperty.SetValue(capturedItem, memberValue);
                            OnFieldChanged();
                        }));
                        builder.CloseElement(); // input
                        builder.CloseElement(); // td
                        builder.CloseRegion();
                    }
                }
                builder.CloseRegion();

                builder.CloseElement(); // tr
                builder.CloseRegion();
            }
        }
        builder.CloseRegion();
        builder.CloseElement(); // tbody

        builder.CloseElement(); // table
    }
}
