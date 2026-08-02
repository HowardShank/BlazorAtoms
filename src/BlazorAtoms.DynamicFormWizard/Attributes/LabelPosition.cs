namespace BlazorAtoms.DynamicFormWizard.Attributes;

/// <summary>Where a field's label renders relative to its input (DESIGN-DISCUSSION.md H.31,
/// #142). <see cref="Above"/> is the default and matches every existing model's current look --
/// nothing changes unless a model or the wizard opts into a different value. <see cref="Hidden"/>
/// still needs an accessible name, so it moves the label text onto the rendered input's
/// <c>aria-label</c> instead of dropping it; <see cref="Inline"/> moves it onto the input's
/// <c>placeholder</c>.</summary>
public enum LabelPosition
{
    Above,
    Left,
    Inline,
    Hidden,
}
