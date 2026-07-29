namespace BlazorAtoms.Inputs;

/// <summary>
/// Layout axis, shared by the controls in this library that have one — <see cref="AtomRangeInput{TValue}"/>'s
/// track and <see cref="AtomRadioGroup{TValue}"/>'s option list. Each component states its own
/// default; the meaning of each member is per-component and documented below.
/// </summary>
public enum Orientation
{
    /// <summary>Range: left-to-right track (its default). Radio group: options in a row.</summary>
    Horizontal,

    /// <summary>Range: bottom-to-top track (min at bottom, max at top — standard vertical-slider
    /// convention). Radio group: options stacked (its default). Note the range renders the same
    /// horizontal internals rotated in place (see <c>AtomRangeInput.razor.css</c>) — every other
    /// feature (fill, handle offset/rotation/shape, icons) keeps working unchanged.</summary>
    Vertical,
}
