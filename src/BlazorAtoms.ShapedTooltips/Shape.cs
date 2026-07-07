namespace BlazorAtoms.ShapedTooltips;

/// <summary>Outline shape of the tooltip bubble. Every shape is drawn as an inline SVG path,
/// so border (stroke) and fill apply uniformly — including on Burst, FoldedCorner, and Cloud.</summary>
public enum Shape
{
    /// <summary>Rounded rectangle (default). Corner rounding via <c>Radius</c> (viewBox units).</summary>
    Rectangle,

    /// <summary>Stadium / pill — fully rounded ends.</summary>
    Pill,

    /// <summary>Ellipse. Best for short content.</summary>
    Ellipse,

    /// <summary>"Thinking" cloud — bumpy outline with a trail of shrinking circles toward the
    /// trigger (the trail obeys <c>ShowArrow</c>).</summary>
    Cloud,

    /// <summary>Comic burst / spiky star. Border + fill apply (SVG stroke).</summary>
    Burst,

    /// <summary>Rectangle with a folded top-right corner. Border + fill apply (SVG stroke).</summary>
    FoldedCorner,
}
