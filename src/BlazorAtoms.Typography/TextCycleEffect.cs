namespace BlazorAtoms.Typography;

/// <summary>How <see cref="AtomTextCycle"/>'s words transition into and out of view — both the
/// axis/style of motion and its direction.</summary>
public enum TextCycleEffect
{
    /// <summary>Slide: words rise — the next word enters from the bottom, the current one exits the top.</summary>
    SlideBottomToTop,

    /// <summary>Slide: words fall — the next word enters from the top, the current one exits the bottom.</summary>
    SlideTopToBottom,

    /// <summary>Slide: words travel rightward — the next word enters from the left, the current one exits right.</summary>
    SlideLeftToRight,

    /// <summary>Slide: words travel leftward — the next word enters from the right, the current one exits left.</summary>
    SlideRightToLeft,

    /// <summary>Spin: words sit on a rotating drum (propeller-style, like a flip clock) and rotate
    /// clockwise into their next, upright, readable position.</summary>
    SpinClockwise,

    /// <summary>Spin: same rotating-drum motion as <see cref="SpinClockwise"/>, in the opposite
    /// rotational direction.</summary>
    SpinCounterClockwise,
}
