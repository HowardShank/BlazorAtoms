namespace BlazorAtoms.Navigation;

/// <summary>Which end to scroll toward (and which default chevron glyph to draw).</summary>
public enum ScrollDirection
{
    /// <summary>Scroll to the start (top). Default.</summary>
    Up,
    /// <summary>Scroll to the end (bottom).</summary>
    Down,
}

/// <summary>What the button scrolls: the whole page or the nearest scrollable container.</summary>
public enum ScrollScope
{
    /// <summary>Scroll the window/document. Default.</summary>
    Page,
    /// <summary>Scroll the nearest scrollable ancestor of the button (or the element named by
    /// <see cref="AtomScrollTo.ScrollContainer"/>).</summary>
    Container,
}

/// <summary>Scroll animation, mapped to the DOM <c>ScrollOptions.behavior</c>.</summary>
public enum ScrollMotion
{
    /// <summary>Animated smooth scroll. Default.</summary>
    Smooth,
    /// <summary>Instant jump.</summary>
    Auto,
}

/// <summary>Self-positioning for the button — pins it to a viewport corner (<c>Fixed*</c>) or a
/// positioned-ancestor corner (<c>Absolute*</c>) with no consumer CSS. <see cref="Inline"/> leaves
/// the button in normal flow.</summary>
public enum ScrollPosition
{
    /// <summary>Flows in place; position it yourself via <c>Style</c>/<c>CssClass</c>. Default.</summary>
    Inline,

    /// <summary><c>position:fixed</c> to the viewport's bottom-right corner.</summary>
    FixedBottomRight,
    /// <summary><c>position:fixed</c> to the viewport's bottom-left corner.</summary>
    FixedBottomLeft,
    /// <summary><c>position:fixed</c> to the viewport's top-right corner.</summary>
    FixedTopRight,
    /// <summary><c>position:fixed</c> to the viewport's top-left corner.</summary>
    FixedTopLeft,
    /// <summary><c>position:fixed</c>, centered along the bottom edge.</summary>
    FixedBottomCenter,
    /// <summary><c>position:fixed</c>, centered along the top edge.</summary>
    FixedTopCenter,

    /// <summary><c>position:absolute</c> to the nearest positioned ancestor's bottom-right corner.</summary>
    AbsoluteBottomRight,
    /// <summary><c>position:absolute</c> to the nearest positioned ancestor's bottom-left corner.</summary>
    AbsoluteBottomLeft,
    /// <summary><c>position:absolute</c> to the nearest positioned ancestor's top-right corner.</summary>
    AbsoluteTopRight,
    /// <summary><c>position:absolute</c> to the nearest positioned ancestor's top-left corner.</summary>
    AbsoluteTopLeft,
}
