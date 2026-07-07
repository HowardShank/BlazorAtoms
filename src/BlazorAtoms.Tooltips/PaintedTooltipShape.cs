namespace BlazorAtoms.Tooltips;

/// <summary>Outline shape of the <see cref="AtomPaintedTooltip"/> bubble. Every shape is an inline
/// SVG path, painted by the SVG itself (gradient or solid fill, gradient or solid stroke, optional
/// shadow).</summary>
public enum PaintedTooltipShape
{
    /// <summary>Rounded rectangle (default). Corner rounding via <c>Radius</c> (viewBox units).</summary>
    Rectangle,

    /// <summary>Stadium / pill — fully rounded ends.</summary>
    Pill,

    /// <summary>Ellipse. Best for short content.</summary>
    Ellipse,

    /// <summary>"Thinking" cloud — bumpy outline with a trail of shrinking circles toward the trigger.</summary>
    Cloud,

    /// <summary>Comic burst / spiky star.</summary>
    Burst,

    /// <summary>Rectangle with a folded top-right corner.</summary>
    FoldedCorner,
}
