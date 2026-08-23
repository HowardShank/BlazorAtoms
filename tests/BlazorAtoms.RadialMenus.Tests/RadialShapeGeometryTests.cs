using System.Globalization;

namespace BlazorAtoms.RadialMenus.Tests;

/// <summary>
/// Coverage for <see cref="RadialShapeGeometry"/>: side counts and base rotations per named shape,
/// SVG vertex generation in the fixed 100x100 box, and the inradius-based sizing formula that
/// decides how big a shape must be for its label to fit inside the outline.
/// </summary>
public class RadialShapeGeometryTests
{
    private const double Tol = 0.001;

    private static void Near(double expected, double actual, string because = "") =>
        Assert.True(Math.Abs(expected - actual) < Tol,
            $"expected {expected} but got {actual}{(because.Length > 0 ? $" ({because})" : "")}");

    // ---- side counts --------------------------------------------------------------------------

    [Theory]
    [InlineData(RadialMenuShape.Triangle, 3)]
    [InlineData(RadialMenuShape.Square, 4)]
    [InlineData(RadialMenuShape.Diamond, 4)]
    [InlineData(RadialMenuShape.Pentagon, 5)]
    [InlineData(RadialMenuShape.Hexagon, 6)]
    [InlineData(RadialMenuShape.Heptagon, 7)]
    [InlineData(RadialMenuShape.Octagon, 8)]
    public void Named_polygons_report_their_side_count(RadialMenuShape shape, int expected)
        => Assert.Equal(expected, RadialShapeGeometry.Sides(shape));

    [Theory]
    [InlineData(RadialMenuShape.Circle)]
    [InlineData(RadialMenuShape.Squircle)]
    [InlineData(RadialMenuShape.Custom)]
    public void Round_and_custom_shapes_have_no_side_count(RadialMenuShape shape)
        => Assert.Null(RadialShapeGeometry.Sides(shape));

    [Theory]
    [InlineData(null, 6)]  // an unspecified Polygon is a hexagon
    [InlineData(12, 12)]
    [InlineData(2, 3)]     // there is no 2-sided polygon
    [InlineData(0, 3)]
    public void Polygon_takes_its_side_count_from_the_parameter_with_a_floor_of_three(int? given, int expected)
        => Assert.Equal(expected, RadialShapeGeometry.Sides(RadialMenuShape.Polygon, given));

    [Theory]
    [InlineData(RadialMenuShape.Square, 45)]    // point-up would be a diamond
    [InlineData(RadialMenuShape.Octagon, 22.5)] // flat top, like a road sign
    [InlineData(RadialMenuShape.Diamond, 0)]
    [InlineData(RadialMenuShape.Hexagon, 0)]
    [InlineData(RadialMenuShape.Circle, 0)]
    public void Named_shapes_carry_the_rotation_their_name_implies(RadialMenuShape shape, double expected)
        => Near(expected, RadialShapeGeometry.BaseRotation(shape));

    // ---- vertices -----------------------------------------------------------------------------

    [Fact]
    public void An_unrotated_quad_is_a_diamond_with_a_vertex_straight_up()
        => Assert.Equal("50,0 100,50 50,100 0,50", RadialShapeGeometry.PolygonPoints(4));

    [Fact]
    public void A_quad_turned_forty_five_degrees_is_an_axis_aligned_square()
        => Assert.Equal(
            "85.355,14.645 85.355,85.355 14.645,85.355 14.645,14.645",
            RadialShapeGeometry.PolygonPoints(4, 45));

    [Fact]
    public void A_triangle_points_up_with_a_flat_base()
        => Assert.Equal("50,0 93.301,75 6.699,75", RadialShapeGeometry.PolygonPoints(3));

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(17)]
    public void Every_vertex_sits_on_the_circumscribed_circle_of_the_box(int sides)
    {
        foreach (var (x, y) in Parse(RadialShapeGeometry.PolygonPoints(sides, 13)))
        {
            var r = Math.Sqrt((x - 50) * (x - 50) + (y - 50) * (y - 50));
            Assert.True(Math.Abs(r - 50) < 0.01, $"vertex {x},{y} is {r} from center, expected 50");
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public void Rotating_by_one_full_step_reproduces_the_same_outline(int sides)
    {
        var at0 = Ordered(RadialShapeGeometry.PolygonPoints(sides));
        var atStep = Ordered(RadialShapeGeometry.PolygonPoints(sides, 360.0 / sides));

        Assert.Equal(at0.Length, atStep.Length);
        for (var i = 0; i < at0.Length; i++)
        {
            Near(at0[i].X, atStep[i].X, "rotational symmetry");
            Near(at0[i].Y, atStep[i].Y, "rotational symmetry");
        }
    }

    [Fact]
    public void Fewer_than_three_sides_is_raised_to_a_triangle_rather_than_producing_a_degenerate_path()
        => Assert.Equal(RadialShapeGeometry.PolygonPoints(3), RadialShapeGeometry.PolygonPoints(1));

    // ---- sizing -------------------------------------------------------------------------------

    [Theory]
    [InlineData(null, 1.0)]      // a round shape's inscribed and circumscribed circles coincide
    [InlineData(3, 0.5)]         // cos 60
    [InlineData(4, 0.707107)]    // cos 45
    [InlineData(6, 0.866025)]    // cos 30
    [InlineData(8, 0.923880)]    // cos 22.5
    public void InradiusRatio_is_the_cosine_of_half_the_central_angle(int? sides, double expected)
        => Near(expected, RadialShapeGeometry.InradiusRatio(sides));

    [Theory]
    [InlineData(null, 1.414214)] // circle: just the diagonal
    [InlineData(8, 1.530734)]
    [InlineData(6, 1.632993)]
    [InlineData(4, 2.0)]
    [InlineData(3, 2.828427)]    // a triangle is mostly unusable corner
    public void RequiredSize_scales_a_square_label_by_the_shapes_wasted_space(int? sides, double factor)
    {
        const double t = 20;
        Near(factor * t, RadialShapeGeometry.RequiredSize(t, t, sides, fitFactor: 1));
    }

    [Fact]
    public void RequiredSize_fits_the_labels_diagonal_inside_the_inscribed_circle()
    {
        // A 40x10 label has the same 41.23 diagonal whichever way round it is stated, so the
        // required diameter is the same — the constraint is the circle, not the width.
        var wide = RadialShapeGeometry.RequiredSize(40, 10, null, fitFactor: 1);
        var tall = RadialShapeGeometry.RequiredSize(10, 40, null, fitFactor: 1);
        Near(Math.Sqrt(40 * 40 + 10 * 10), wide);
        Near(wide, tall);
    }

    [Fact]
    public void A_fit_factor_below_one_leaves_breathing_room_by_demanding_a_bigger_shape()
    {
        var snug = RadialShapeGeometry.RequiredSize(20, 20, null, fitFactor: 1);
        var roomy = RadialShapeGeometry.RequiredSize(20, 20, null, fitFactor: 0.95);
        Near(snug / 0.95, roomy);
        Assert.True(roomy > snug);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_non_positive_fit_factor_is_ignored_rather_than_dividing_by_zero(double factor)
        => Near(
            RadialShapeGeometry.RequiredSize(20, 20, null, fitFactor: 1),
            RadialShapeGeometry.RequiredSize(20, 20, null, factor));

    [Fact]
    public void No_text_needs_no_size()
        => Near(0, RadialShapeGeometry.RequiredSize(0, 0, 6));

    [Fact]
    public void EstimateTextWidth_is_characters_times_font_size_times_ratio()
    {
        Near(44, RadialShapeGeometry.EstimateTextWidth("Hello", 16, 0.55));
        Near(0, RadialShapeGeometry.EstimateTextWidth(null, 16));
        Near(0, RadialShapeGeometry.EstimateTextWidth("", 16));
    }

    [Fact]
    public void EstimateTextWidth_cannot_tell_wide_glyphs_from_narrow_ones()
    {
        // Documents the known limitation that justifies RadialMenuSizeMode.Measure existing at all.
        Near(
            RadialShapeGeometry.EstimateTextWidth("WWWW", 16),
            RadialShapeGeometry.EstimateTextWidth("llll", 16));
    }

    // ---- helpers ------------------------------------------------------------------------------

    private static (double X, double Y)[] Parse(string points) => points
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(p => p.Split(','))
        .Select(p => (
            double.Parse(p[0], CultureInfo.InvariantCulture),
            double.Parse(p[1], CultureInfo.InvariantCulture)))
        .ToArray();

    private static (double X, double Y)[] Ordered(string points) => Parse(points)
        .OrderBy(p => Math.Round(p.X, 3))
        .ThenBy(p => Math.Round(p.Y, 3))
        .ToArray();
}
