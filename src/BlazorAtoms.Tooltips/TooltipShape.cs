namespace BlazorAtoms.Tooltips;

/// <summary>Outline shape of the <see cref="AtomTooltip"/> bubble (CSS-only rendering).</summary>
public enum TooltipShape
{
    /// <summary>Rounded rectangle (default). Corner rounding via <c>Radius</c>.</summary>
    Rectangle,

    /// <summary>Stadium / pill — fully rounded ends.</summary>
    Pill,

    /// <summary>Ellipse. Best for short content; text is inset to stay inside the curve.</summary>
    Ellipse,

    /// <summary>"Thinking" bubble — rounded body with a trail of shrinking circles pointing at
    /// the trigger instead of the triangle arrow (the trail obeys <c>ShowArrow</c>).</summary>
    Thought,

    /// <summary>Comic burst / spiky star (via <c>clip-path</c>). Fill only — border and arrow
    /// don't apply (the clip removes them); use <see cref="AtomShapedTooltip"/> for a bordered burst.</summary>
    Burst,

    /// <summary>Rectangle with a folded top-right corner (via <c>clip-path</c>). Fill only —
    /// border and arrow don't apply.</summary>
    FoldedCorner,
}
