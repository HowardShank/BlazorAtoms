namespace BlazorAtoms.Tooltips;

/// <summary>Where the tooltip bubble is placed relative to its trigger (or the pointer).</summary>
/// <remarks>
/// Prefixed <c>Tooltip*</c> per the repo convention that a cross-package enum name carries its
/// package's noun — this and <c>BadgePlacement</c> would otherwise both be a bare <c>Placement</c>,
/// leaving no unambiguous name for a page that <c>@using</c>s both. It also matches this package's
/// other enums (<c>TooltipShape</c>, <c>TooltipTextAlign</c>, <c>TooltipVerticalAlign</c>). The
/// parameter on each component is still called <c>Placement</c>.
/// </remarks>
public enum TooltipPlacement
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
