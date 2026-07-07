namespace BlazorAtoms.Tooltips;

/// <summary>Where the tooltip bubble is placed relative to its trigger (or the pointer).</summary>
public enum Placement
{
    Top,
    TopStart,
    TopEnd,
    Bottom,
    BottomStart,
    BottomEnd,
    Left,
    LeftStart,
    LeftEnd,
    Right,
    RightStart,
    RightEnd,

    /// <summary>Diagonally off the trigger's top-left corner (above and to the left).</summary>
    TopLeft,
    /// <summary>Diagonally off the trigger's top-right corner (above and to the right).</summary>
    TopRight,
    /// <summary>Diagonally off the trigger's bottom-left corner (below and to the left).</summary>
    BottomLeft,
    /// <summary>Diagonally off the trigger's bottom-right corner (below and to the right).</summary>
    BottomRight,

    /// <summary>Follows the mouse pointer while hovering the trigger. Requires JS interop
    /// (a tiny module the component loads itself); has no effect under static SSR.</summary>
    Cursor,
}
