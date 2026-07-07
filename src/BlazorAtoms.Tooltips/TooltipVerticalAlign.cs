namespace BlazorAtoms.Tooltips;

/// <summary>Vertical alignment of the bubble's content within a fixed-height bubble. Shared by the
/// SVG tooltips (<see cref="AtomShapedTooltip"/>, <see cref="AtomPaintedTooltip"/>). Only visible
/// when <c>Height</c> gives the bubble more room than the content needs; otherwise content fills
/// the box. Leave the parameter null to keep the default (centered).</summary>
public enum TooltipVerticalAlign
{
    /// <summary>Align content to the top of the bubble.</summary>
    Top,
    /// <summary>Center content vertically.</summary>
    Center,
    /// <summary>Align content to the bottom of the bubble.</summary>
    Bottom,
}
