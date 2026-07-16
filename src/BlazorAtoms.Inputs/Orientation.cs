namespace BlazorAtoms.Inputs;

/// <summary>
/// Layout axis of <see cref="AtomRangeInput{TValue}"/>'s track. Vertical renders the same
/// horizontal-range internals rotated in place (see <c>AtomRangeInput.razor.css</c>) — every other
/// feature (fill, handle offset/rotation/shape, icons) keeps working unchanged.
/// </summary>
public enum Orientation
{
    /// <summary>Default left-to-right track.</summary>
    Horizontal,

    /// <summary>Bottom-to-top track (min at bottom, max at top — standard vertical-slider convention).</summary>
    Vertical,
}
