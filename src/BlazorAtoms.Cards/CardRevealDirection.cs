namespace BlazorAtoms.Cards;

/// <summary>Which way <see cref="AtomCardReveal"/>'s overlay slides away on hover. Named for the
/// overlay's own travel direction rather than "left&gt;right"-style sweep wording, which is
/// ambiguous about whether it describes the overlay or the panel being uncovered.
/// <para>The body panel occupies the opposite side, and a sliver of the background image stays
/// visible on the side the overlay slid toward.</para></summary>
public enum CardRevealDirection
{
    /// <summary>Overlay slides left; body panel is revealed on the right.</summary>
    Left,

    /// <summary>Overlay slides right; body panel is revealed on the left.</summary>
    Right,

    /// <summary>Overlay slides up; body panel is revealed along the bottom.</summary>
    Up,

    /// <summary>Overlay slides down; body panel is revealed along the top.</summary>
    Down,
}
