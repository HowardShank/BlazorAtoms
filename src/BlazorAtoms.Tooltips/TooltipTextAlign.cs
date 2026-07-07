namespace BlazorAtoms.Tooltips;

/// <summary>Horizontal alignment of the bubble's text/content. Shared by the SVG tooltips
/// (<see cref="AtomShapedTooltip"/>, <see cref="AtomPaintedTooltip"/>). Leave the parameter
/// null to keep each shape's built-in default (start, or centered for cloud/ellipse).</summary>
public enum TooltipTextAlign
{
    /// <summary>Align to the inline start (left in LTR).</summary>
    Start,
    /// <summary>Center horizontally.</summary>
    Center,
    /// <summary>Align to the inline end (right in LTR).</summary>
    End,
}
