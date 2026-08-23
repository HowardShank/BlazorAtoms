namespace BlazorAtoms.RadialMenus.Tests;

/// <summary>
/// Geometry coverage for <see cref="RadialLayout"/> — no renderer, no DOM, no JS. Every number
/// asserted here was computed by hand from the formulas documented on <see cref="RadialLayout"/>,
/// so a regression shows up as a wrong value rather than as a self-consistent tautology.
/// </summary>
public class RadialLayoutTests
{
    private const double Tol = 0.01;

    private static RadialLayoutRequest Req(int count) => new() { ItemCount = count };

    private static void Near(double expected, double actual, string because = "") =>
        Assert.True(Math.Abs(expected - actual) < Tol,
            $"expected {expected} but got {actual}{(because.Length > 0 ? $" ({because})" : "")}");

    private static double[] Angles(RadialLayoutResult r) => r.Slots.Select(s => s.AngleDegrees).ToArray();

    // ---- sweep normalization ------------------------------------------------------------------

    [Theory]
    [InlineData(0, 360, 360)]   // a full turn
    [InlineData(0, 720, 360)]   // more than a full turn clamps
    [InlineData(0, 0, 360)]     // zero width reads as closed, the only drawable reading
    [InlineData(0, 90, 90)]
    [InlineData(300, 60, 120)]  // wraps forward through 0
    [InlineData(0, -90, 270)]   // a backwards end sweeps the long way round clockwise
    public void ResolveSweep_normalizes_to_a_positive_arc(double start, double end, double expected)
        => Near(expected, RadialLayout.ResolveSweep(start, end));

    // ---- distribution -------------------------------------------------------------------------

    [Fact]
    public void Auto_on_a_closed_arc_is_Cyclic_so_four_items_land_on_the_compass_points()
    {
        var r = RadialLayout.Solve(Req(4));
        Assert.Equal(RadialMenuDistribution.Cyclic, r.ResolvedDistribution);
        Assert.Equal([0, 90, 180, 270], Angles(r));
    }

    [Fact]
    public void Auto_on_a_partial_arc_is_Endpoints_so_the_first_and_last_item_sit_on_the_ends()
    {
        var r = RadialLayout.Solve(Req(3) with { EndAngle = 180 });
        Assert.Equal(RadialMenuDistribution.Endpoints, r.ResolvedDistribution);
        Assert.Equal([0, 90, 180], Angles(r));
    }

    [Fact]
    public void Cyclic_on_a_partial_arc_starts_on_StartAngle_and_stops_a_step_short()
    {
        var r = RadialLayout.Solve(Req(3) with
        {
            EndAngle = 180,
            Distribution = RadialMenuDistribution.Cyclic,
        });
        Assert.Equal([0, 60, 120], Angles(r));
    }

    [Fact]
    public void Padded_insets_every_item_half_a_step_from_both_arc_ends()
    {
        var r = RadialLayout.Solve(Req(3) with
        {
            EndAngle = 180,
            Distribution = RadialMenuDistribution.Padded,
        });
        Assert.Equal([30, 90, 150], Angles(r));
    }

    [Fact]
    public void FixedStep_owns_the_spacing_and_ignores_EndAngle()
    {
        var full = RadialLayout.Solve(Req(4) with
        {
            Distribution = RadialMenuDistribution.FixedStep,
            AngleStep = 30,
        });
        var clipped = RadialLayout.Solve(Req(4) with
        {
            EndAngle = 90,
            Distribution = RadialMenuDistribution.FixedStep,
            AngleStep = 30,
        });

        Assert.Equal([0, 30, 60, 90], Angles(full));
        Assert.Equal(Angles(full), Angles(clipped));
    }

    [Fact]
    public void FixedStep_without_an_AngleStep_falls_back_and_says_so()
    {
        var r = RadialLayout.Solve(Req(4) with { Distribution = RadialMenuDistribution.FixedStep });
        Assert.Equal(RadialMenuDistribution.Cyclic, r.ResolvedDistribution);
        Assert.Contains(r.Advisories, a => a.Contains("FixedStep needs a positive AngleStep"));
    }

    [Fact]
    public void A_single_item_centers_on_the_arc_under_Endpoints()
    {
        var r = RadialLayout.Solve(Req(1) with { EndAngle = 180 });
        Near(90, r.Slots[0].AngleDegrees);
    }

    [Fact]
    public void A_single_item_sits_on_StartAngle_under_Cyclic()
    {
        var r = RadialLayout.Solve(Req(1) with
        {
            StartAngle = 45,
            EndAngle = 180,
            Distribution = RadialMenuDistribution.Cyclic,
        });
        Near(45, r.Slots[0].AngleDegrees);
    }

    [Fact]
    public void CounterClockwise_mirrors_the_arc_about_StartAngle()
    {
        var r = RadialLayout.Solve(Req(3) with
        {
            EndAngle = 180,
            Direction = RadialMenuDirection.CounterClockwise,
        });
        Assert.Equal([0, 270, 180], Angles(r));
    }

    [Fact]
    public void An_arc_wrapping_through_zero_is_placed_across_the_seam()
    {
        var r = RadialLayout.Solve(Req(3) with { StartAngle = 300, EndAngle = 60 });
        Near(120, r.Sweep);
        Assert.Equal([300, 0, 60], Angles(r));
    }

    // ---- cartesian conversion -----------------------------------------------------------------

    [Fact]
    public void Zero_degrees_is_straight_up_and_ninety_is_to_the_right()
    {
        var r = RadialLayout.Solve(Req(4));
        var radius = r.Radius;

        Near(0, r.Slots[0].X, "angle 0");
        Near(-radius, r.Slots[0].Y, "angle 0 is up, so Y is negative");
        Near(radius, r.Slots[1].X, "angle 90");
        Near(0, r.Slots[1].Y, "angle 90");
        Near(0, r.Slots[2].X, "angle 180");
        Near(radius, r.Slots[2].Y, "angle 180 is down");
        Near(-radius, r.Slots[3].X, "angle 270");
    }

    // ---- radius solve -------------------------------------------------------------------------

    [Fact]
    public void Radius_is_driven_by_the_center_button_when_the_ring_is_sparse()
    {
        // Four items 90 degrees apart need only (48+8)/(2*sin45) = 39.6 to clear each other, but
        // (64+48)/2 + 8 = 64 to clear the center button. The larger constraint wins.
        var r = RadialLayout.Solve(Req(4));
        Near(64, r.Radius);
    }

    [Fact]
    public void Radius_is_solved_against_the_measured_wrap_gap_not_the_nominal_step()
    {
        // Three items on a 350-degree arc: nominal step 175, but the first and last are only 10
        // degrees apart across the wrap. Solving against 175 would give 28; the truth is 321.
        var r = RadialLayout.Solve(Req(3) with
        {
            EndAngle = 350,
            Distribution = RadialMenuDistribution.Endpoints,
        });

        Near(175, r.StepDegrees);
        Near(10, r.MinSeparationDegrees);
        Near(321.26, r.Radius, "(48+8)/(2*sin(5deg))");
    }

    [Fact]
    public void Growing_the_item_count_grows_the_radius_rather_than_dropping_items()
    {
        var last = 0.0;
        for (var n = 2; n <= 24; n++)
        {
            var r = RadialLayout.Solve(Req(n));
            Assert.Equal(n, r.Slots.Count);
            Assert.True(r.Radius >= last, $"radius shrank going from {n - 1} to {n} items");
            last = r.Radius;
        }
    }

    [Fact]
    public void Endpoints_on_a_closed_arc_reports_the_duplicate_position()
    {
        var r = RadialLayout.Solve(Req(4) with { Distribution = RadialMenuDistribution.Endpoints });
        Assert.Contains(r.Advisories, a => a.Contains("same place") && a.Contains("Cyclic"));
    }

    [Fact]
    public void Crowding_past_the_threshold_is_an_advisory_not_a_refusal()
    {
        var r = RadialLayout.Solve(Req(20) with { CrowdingWarnThreshold = 12 });
        Assert.Equal(20, r.Slots.Count);
        Assert.Contains(r.Advisories, a => a.Contains("CrowdingWarnThreshold"));
    }

    // ---- radius mode --------------------------------------------------------------------------

    [Fact]
    public void RadiusMode_Fixed_is_honored_exactly_even_when_it_overlaps()
    {
        var r = RadialLayout.Solve(Req(12) with
        {
            RadiusMode = RadialMenuRadiusMode.Fixed,
            Radius = 30,
        });
        Near(30, r.Radius);
        Assert.Contains(r.Advisories, a => a.Contains("will overlap"));
    }

    [Fact]
    public void RadiusMode_Auto_treats_a_supplied_Radius_as_a_floor()
    {
        var pushedOut = RadialLayout.Solve(Req(4) with { Radius = 200 });
        Near(200, pushedOut.Radius);

        var tooSmall = RadialLayout.Solve(Req(4) with { Radius = 10 });
        Near(64, tooSmall.Radius, "the collision solve still wins");
    }

    // ---- overflow: shrink ---------------------------------------------------------------------

    [Fact]
    public void Shrink_trades_item_size_for_a_capped_radius_and_quantizes_the_result()
    {
        // 12 items 30 degrees apart want radius 108.19; capped at 100 the chord allows
        // 2*100*sin(15deg) - 8 = 43.76 of item, quantized down to a multiple of 4.
        var r = RadialLayout.Solve(Req(12) with
        {
            CenterSize = 40,
            MaxRadius = 100,
            Overflow = RadialMenuOverflow.Shrink,
        });

        Near(100, r.Radius);
        Near(40, r.ItemSize);
        Assert.All(r.Slots, s => Near(40, s.Size));
        Assert.Empty(r.Advisories);
    }

    [Fact]
    public void Shrink_stops_at_MinItemSize_and_reports_the_remaining_overlap()
    {
        var r = RadialLayout.Solve(Req(16) with
        {
            CenterSize = 40,
            MaxRadius = 40,
            Overflow = RadialMenuOverflow.Shrink,
        });

        Near(24, r.ItemSize);
        Assert.Contains(r.Advisories, a => a.Contains("MinItemSize") && a.Contains("still overlap"));
    }

    [Fact]
    public void GrowRadius_blows_past_MaxRadius_deliberately_and_names_the_alternatives()
    {
        var r = RadialLayout.Solve(Req(16) with { MaxRadius = 50 });
        Assert.True(r.Radius > 50);
        Assert.Contains(r.Advisories, a => a.Contains("exceeds MaxRadius") && a.Contains("Rings"));
    }

    // ---- overflow: rings ----------------------------------------------------------------------

    [Fact]
    public void Rings_wraps_the_surplus_outward_and_staggers_every_other_ring()
    {
        var r = RadialLayout.Solve(Req(10) with
        {
            Overflow = RadialMenuOverflow.Rings,
            MaxPerRing = 4,
        });

        Assert.Equal(3, r.RingCount);
        Assert.Equal(10, r.Slots.Count);

        // Ring 0 at the ordinary solved radius, on the compass points.
        Near(64, r.Radius);
        Assert.Equal([0, 90, 180, 270], Angles(r).Take(4).ToArray());
        Assert.All(r.Slots.Take(4), s => Assert.Equal(0, s.Ring));

        // Ring 1 one item-plus-gap further out, nudged half a step so it does not line up.
        Assert.All(r.Slots.Skip(4).Take(4), s => Assert.Equal(1, s.Ring));
        Assert.Equal([45, 135, 225, 315], Angles(r).Skip(4).Take(4).ToArray());
        Near(128, Radius(r.Slots[4]), "64 + (48 + 16)");

        // Ring 2 holds the remaining two, back in phase with ring 0.
        Assert.Equal([0, 90], Angles(r).Skip(8).ToArray());
        Near(192, Radius(r.Slots[8]));
        Near(192, r.OuterRadius);
        Near(216, r.Extent, "outer radius plus half an item");
    }

    [Fact]
    public void Rings_collapses_to_one_ring_when_everything_already_fits()
    {
        var r = RadialLayout.Solve(Req(3) with
        {
            Overflow = RadialMenuOverflow.Rings,
            MaxPerRing = 8,
        });
        Assert.Equal(1, r.RingCount);
        Assert.All(r.Slots, s => Assert.Equal(0, s.Ring));
    }

    [Fact]
    public void Rings_with_nothing_to_wrap_against_says_so_rather_than_guessing()
    {
        var r = RadialLayout.Solve(Req(10) with { Overflow = RadialMenuOverflow.Rings });
        Assert.Equal(1, r.RingCount);
        Assert.Contains(r.Advisories, a => a.Contains("needs MaxPerRing or MaxRadius"));
    }

    [Fact]
    public void Rings_derives_its_wrap_point_from_MaxRadius_when_MaxPerRing_is_unset()
    {
        var r = RadialLayout.Solve(Req(12) with
        {
            Overflow = RadialMenuOverflow.Rings,
            MaxRadius = 100,
        });

        Assert.True(r.RingCount > 1, "12 items cannot fit inside radius 100 on one ring");
        Assert.All(r.Slots, s => Assert.True(Radius(s) <= 100 + Tol || s.Ring > 0,
            "the innermost ring must respect the cap"));
    }

    // ---- overflow: paginate -------------------------------------------------------------------

    [Fact]
    public void Paginate_puts_steppers_on_the_ring_beside_the_page()
    {
        var r = RadialLayout.Solve(Req(10) with
        {
            Overflow = RadialMenuOverflow.Paginate,
            PageSize = 4,
            PageIndex = 1,
        });

        Assert.Equal(3, r.PageCount);
        Assert.Equal(1, r.PageIndex);
        Assert.Equal(6, r.Slots.Count);

        Assert.Equal(RadialMenuSlotKind.PagePrev, r.Slots[0].Kind);
        Assert.Equal(-1, r.Slots[0].ItemIndex);
        Assert.Equal([4, 5, 6, 7], r.Slots.Skip(1).Take(4).Select(s => s.ItemIndex).ToArray());
        Assert.Equal(RadialMenuSlotKind.PageNext, r.Slots[^1].Kind);
    }

    [Fact]
    public void The_first_page_has_no_previous_stepper_and_the_last_has_no_next()
    {
        var first = RadialLayout.Solve(Req(10) with
        {
            Overflow = RadialMenuOverflow.Paginate,
            PageSize = 4,
        });
        Assert.Equal(5, first.Slots.Count);
        Assert.DoesNotContain(first.Slots, s => s.Kind == RadialMenuSlotKind.PagePrev);

        var last = first with { };
        var lastPage = RadialLayout.Solve(Req(10) with
        {
            Overflow = RadialMenuOverflow.Paginate,
            PageSize = 4,
            PageIndex = 2,
        });
        Assert.Equal(3, lastPage.Slots.Count);
        Assert.DoesNotContain(lastPage.Slots, s => s.Kind == RadialMenuSlotKind.PageNext);
        Assert.NotNull(last);
    }

    [Fact]
    public void An_out_of_range_page_is_clamped_not_thrown_on()
    {
        var r = RadialLayout.Solve(Req(10) with
        {
            Overflow = RadialMenuOverflow.Paginate,
            PageSize = 4,
            PageIndex = 99,
        });
        Assert.Equal(2, r.PageIndex);
    }

    // ---- overflow: spin -----------------------------------------------------------------------

    [Fact]
    public void Spin_shows_exactly_the_window_and_never_stacks_two_items_at_the_seam()
    {
        var r = RadialLayout.Solve(Req(10) with
        {
            Overflow = RadialMenuOverflow.Spin,
            VisibleCount = 4,
        });

        Assert.Equal(4, r.Slots.Count);
        Assert.Equal([0, 90, 180, 270], Angles(r));
        Assert.Equal([0, 1, 2, 3], r.Slots.Select(s => s.ItemIndex).ToArray());
        Assert.Contains(r.Advisories, a => a.Contains("shows 4 of 10"));
    }

    [Fact]
    public void Spinning_a_whole_step_brings_the_far_end_of_the_belt_back_around()
    {
        var r = RadialLayout.Solve(Req(10) with
        {
            Overflow = RadialMenuOverflow.Spin,
            VisibleCount = 4,
            SpinOffset = 90,
        });

        Assert.Equal(4, r.Slots.Count);
        var byIndex = r.Slots.ToDictionary(s => s.ItemIndex, s => s.AngleDegrees);
        Near(90, byIndex[0]);
        Near(180, byIndex[1]);
        Near(270, byIndex[2]);
        Near(0, byIndex[9]);
    }

    [Fact]
    public void Spin_keeps_a_constant_radius_and_pitch_while_it_rotates()
    {
        RadialLayoutResult At(double offset) => RadialLayout.Solve(Req(10) with
        {
            Overflow = RadialMenuOverflow.Spin,
            VisibleCount = 4,
            SpinOffset = offset,
        });

        var a = At(0);
        foreach (var offset in new double[] { 17, 45, 90, 213 })
        {
            var b = At(offset);
            Near(a.Radius, b.Radius, $"radius moved at SpinOffset={offset}");
            Near(a.StepDegrees, b.StepDegrees, $"pitch moved at SpinOffset={offset}");
        }
    }

    // ---- degenerate ---------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void No_items_is_an_empty_layout_not_an_exception(int count)
    {
        var r = RadialLayout.Solve(Req(count));
        Assert.Empty(r.Slots);
        Assert.Empty(r.Advisories);
    }

    // ---- helpers ------------------------------------------------------------------------------

    [Fact]
    public void Quantize_rounds_down_to_the_step_but_never_below_the_floor()
    {
        Near(40, RadialLayout.Quantize(43.76, 4, 24));
        Near(24, RadialLayout.Quantize(25.9, 4, 24), "would round to 24 anyway");
        Near(24, RadialLayout.Quantize(9, 4, 24), "floor wins");
        Near(43.76, RadialLayout.Quantize(43.76, 0, 24), "a non-positive step disables quantization");
    }

    [Fact]
    public void Quantize_is_stable_so_repeated_solves_cannot_jitter()
    {
        var once = RadialLayout.Quantize(43.76, 4, 24);
        Near(once, RadialLayout.Quantize(once, 4, 24));
    }

    private static double Radius(RadialSlot s) => Math.Sqrt(s.X * s.X + s.Y * s.Y);
}
