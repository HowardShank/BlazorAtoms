using System.Globalization;
using System.Text;

namespace BlazorAtoms.RadialMenus;

/// <summary>
/// Turns a <see cref="RadialMenuShape"/> into SVG geometry, and answers the sizing question that
/// makes radial menus awkward: how wide must the shape be for its label to fit *inside the shape*
/// rather than inside the shape's bounding box.
/// </summary>
/// <remarks>
/// <para><b>Everything is drawn in a fixed 100x100 box</b> and scaled by CSS, so the points depend
/// only on side count and rotation — never on the item's pixel size. A ring that shrinks or a menu
/// that resizes reuses the same path.</para>
/// <para><b>Vertices.</b> Vertex <c>k</c> of a regular <c>n</c>-gon sits at
/// <c>a = 360k/n + rotation</c>, at <c>(50 + 50*sin a, 50 - 50*cos a)</c> — the same
/// up-is-zero, clockwise convention the ring itself uses, so a shape's rotation and an item's
/// angle are the same kind of number.</para>
/// <para><b>Sizing.</b> A label has to fit inside the polygon's inscribed circle, whose radius is
/// the inradius <c>r = (S/2)*cos(180/n)</c>, not inside the bounding box. Fitting a
/// <c>w x h</c> rectangle inside a circle of radius <c>r</c> needs
/// <c>r &gt;= sqrt(w^2 + h^2)/2</c>, which rearranges to
/// <c>S = sqrt(w^2 + h^2) / cos(180/n)</c>. That is why the same label needs 1.41x its diagonal in
/// a circle, 1.63x in a hexagon and 2.83x in a triangle — low-sided shapes are mostly unusable
/// corner.</para>
/// <para>Results are computed rather than cached: the cost is a handful of trig calls per item, and
/// a cache keyed on a consumer-supplied (possibly animated) rotation would grow without bound.</para>
/// </remarks>
public static class RadialShapeGeometry
{
    /// <summary>Side of the coordinate box every shape is drawn in.</summary>
    public const double Box = 100;

    private const double Deg2Rad = Math.PI / 180.0;

    /// <summary>
    /// Side count for a shape, or <c>null</c> for a round one (<see cref="RadialMenuShape.Circle"/>,
    /// <see cref="RadialMenuShape.Squircle"/>) and for <see cref="RadialMenuShape.Custom"/>, whose
    /// geometry the consumer owns.
    /// </summary>
    /// <param name="shape">The requested shape.</param>
    /// <param name="explicitSides">Side count for <see cref="RadialMenuShape.Polygon"/>. Values
    /// below 3 are raised to 3 — there is no 2-sided polygon to draw.</param>
    public static int? Sides(RadialMenuShape shape, int? explicitSides = null) => shape switch
    {
        RadialMenuShape.Triangle => 3,
        RadialMenuShape.Square or RadialMenuShape.Diamond => 4,
        RadialMenuShape.Pentagon => 5,
        RadialMenuShape.Hexagon => 6,
        RadialMenuShape.Heptagon => 7,
        RadialMenuShape.Octagon => 8,
        RadialMenuShape.Polygon => Math.Max(3, explicitSides ?? 6),
        _ => null, // Circle, Squircle, Custom
    };

    /// <summary>
    /// Rotation in degrees baked into a named shape so it looks the way its name implies. Zero puts
    /// a vertex at the top, which is what most shapes want; <see cref="RadialMenuShape.Square"/>
    /// and <see cref="RadialMenuShape.Octagon"/> want a flat top instead. The consumer's
    /// <c>ShapeRotation</c> is added on top of this.
    /// </summary>
    public static double BaseRotation(RadialMenuShape shape) => shape switch
    {
        RadialMenuShape.Square => 45,     // a point-up "square" is a diamond
        RadialMenuShape.Octagon => 22.5,  // flat top and bottom, like a road sign
        _ => 0,
    };

    /// <summary>
    /// The <c>points</c> attribute for an SVG <c>&lt;polygon&gt;</c> inscribed in the
    /// <see cref="Box"/>-sized coordinate box.
    /// </summary>
    /// <param name="sides">Side count; values below 3 are raised to 3.</param>
    /// <param name="rotationDegrees">Clockwise rotation. 0 puts a vertex straight up.</param>
    public static string PolygonPoints(int sides, double rotationDegrees = 0)
    {
        var n = Math.Max(3, sides);
        var c = Box / 2;
        var sb = new StringBuilder(n * 14);

        for (var k = 0; k < n; k++)
        {
            var a = (360.0 * k / n + rotationDegrees) * Deg2Rad;
            if (k > 0) sb.Append(' ');
            sb.Append(Fmt(c + c * Math.Sin(a)))
              .Append(',')
              .Append(Fmt(c - c * Math.Cos(a)));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Inradius as a fraction of the circumradius: <c>cos(180/n)</c>. 1 for a round shape, since
    /// its inscribed and circumscribed circles are the same circle.
    /// </summary>
    public static double InradiusRatio(int? sides)
    {
        if (sides is not int n || n < 3) return 1;
        return Math.Cos(180.0 / n * Deg2Rad);
    }

    /// <summary>
    /// Diameter a shape needs for a <paramref name="textWidth"/> x <paramref name="textHeight"/>
    /// label to fit inside its outline.
    /// </summary>
    /// <param name="textWidth">Measured or estimated label width in pixels.</param>
    /// <param name="textHeight">Label height in pixels (font size times line height times lines).</param>
    /// <param name="sides">Side count, or null for a round shape.</param>
    /// <param name="fitFactor">Fraction of the inscribed circle the text is allowed to fill. Below
    /// 1 leaves breathing room; values at or below 0 are ignored.</param>
    /// <returns>The required diameter, or 0 when there is no text to fit.</returns>
    public static double RequiredSize(double textWidth, double textHeight, int? sides, double fitFactor = 0.95)
    {
        if (textWidth <= 0 && textHeight <= 0) return 0;

        var diagonal = Math.Sqrt(textWidth * textWidth + textHeight * textHeight);
        var usable = InradiusRatio(sides) * (fitFactor > 0 ? fitFactor : 1);
        return diagonal / usable;
    }

    /// <summary>
    /// Estimated label width for <see cref="RadialMenuSizeMode.FromFont"/>: character count times
    /// font size times an average glyph-width ratio. An estimate by construction — a proportional
    /// font makes "WWWW" and "llll" the same width here and very different on screen. Use
    /// <see cref="RadialMenuSizeMode.Measure"/> when that matters.
    /// </summary>
    public static double EstimateTextWidth(string? label, double fontSize, double charWidthRatio = 0.55)
        => string.IsNullOrEmpty(label) ? 0 : label.Length * fontSize * charWidthRatio;

    private static string Fmt(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
