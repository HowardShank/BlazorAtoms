namespace BlazorAtoms.Badges;

/// <summary>Badge outline shape. The first four are pure-CSS boxes; the rest are drawn as inline
/// SVG paths so fill <em>and</em> border apply to every shape (no <c>clip-path</c> limitation).</summary>
/// <remarks>
/// Prefixed <c>Badge*</c> because <c>BlazorAtoms.Avatars</c> declares its own <c>Shape</c>: a page
/// that <c>@using</c>s both packages would otherwise have no unambiguous <c>Shape</c>. The parameter
/// on <see cref="AtomBadge"/> is still called <c>Shape</c>.
/// </remarks>
public enum BadgeShape
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
