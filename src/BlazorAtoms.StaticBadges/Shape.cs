namespace BlazorAtoms.StaticBadges;

/// <summary>Badge outline shape. The first four are pure-CSS boxes; the rest are drawn as inline
/// SVG paths so fill <em>and</em> border apply to every shape (no <c>clip-path</c> limitation).</summary>
public enum Shape
{
    /// <summary>Stadium / pill — fully rounded ends (default).</summary>
    Pill,
    /// <summary>Circle — equal width/height, for single digits or a dot.</summary>
    Circle,
    /// <summary>Square with sharp corners.</summary>
    Square,
    /// <summary>Rounded rectangle; corner radius via <c>Radius</c>.</summary>
    Rounded,
    /// <summary>Five-point star (SVG).</summary>
    Star,
    /// <summary>Flat-top hexagon (SVG).</summary>
    Hexagon,
    /// <summary>Diamond / rhombus (SVG).</summary>
    Diamond,
    /// <summary>Shield / crest (SVG).</summary>
    Shield,
    /// <summary>12-point starburst / seal (SVG).</summary>
    Burst,
    /// <summary>Horizontal ribbon banner with notched ends (SVG). Wider by default.</summary>
    Ribbon,
}
