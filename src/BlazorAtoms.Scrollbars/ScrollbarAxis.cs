namespace BlazorAtoms.Scrollbars;

/// <summary>Which direction(s) <see cref="AtomScrollbar"/>'s box scrolls (and therefore which
/// edge(s) show the themed scrollbar).</summary>
public enum ScrollbarAxis
{
    /// <summary>Scrolls vertically only (<c>overflow-y: auto</c>); horizontal overflow is clipped.</summary>
    Vertical,

    /// <summary>Scrolls horizontally only (<c>overflow-x: auto</c>); vertical overflow is clipped.</summary>
    Horizontal,

    /// <summary>Scrolls in both directions.</summary>
    Both,
}
