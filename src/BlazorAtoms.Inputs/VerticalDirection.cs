namespace BlazorAtoms.Inputs;

/// <summary>
/// Which end of a vertical <see cref="AtomRangeInput{TValue}"/> holds the maximum value. Ignored
/// when <see cref="AtomRangeInput{TValue}.Orientation"/> is <see cref="Inputs.Orientation.Horizontal"/>.
/// </summary>
public enum VerticalDirection
{
    /// <summary>Max at the top, min at the bottom (default — standard vertical-slider convention).</summary>
    BottomToTop,

    /// <summary>Max at the bottom, min at the top.</summary>
    TopToBottom,
}
