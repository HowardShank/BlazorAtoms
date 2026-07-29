using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Progress.Tests;

/// <summary>bUnit coverage for <see cref="AtomProgressRing"/>. Purely declarative — no JS interop.
/// The arc geometry is the interesting part: <c>pathLength="100"</c> re-bases the dash math onto a
/// 0-100 scale, so the offset is <c>100 - percent</c> at any radius.</summary>
public class AtomProgressRingTests
{
    [Fact]
    public void Arc_uses_pathLength_100_so_the_dash_offset_is_just_the_remainder()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p.Add(x => x.Value, 25d));

        var fill = cut.Find(".atom-progress-ring-fill");
        Assert.Equal("100", fill.GetAttribute("pathLength"));
        Assert.Equal("100", fill.GetAttribute("stroke-dasharray"));
        Assert.Equal("75", fill.GetAttribute("stroke-dashoffset"));
    }

    [Fact]
    public void Dash_offset_is_radius_independent()
    {
        using var ctx = new BunitContext();

        // Same percentage at a very different diameter must give the same offset — that is the whole
        // point of pathLength over computing 2*pi*r.
        var small = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Value, 40d)
            .Add(x => x.Diameter, 32d));
        var large = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Value, 40d)
            .Add(x => x.Diameter, 400d));

        Assert.Equal(
            small.Find(".atom-progress-ring-fill").GetAttribute("stroke-dashoffset"),
            large.Find(".atom-progress-ring-fill").GetAttribute("stroke-dashoffset"));
    }

    [Fact]
    public void Diameter_drives_the_viewBox_one_to_one()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p.Add(x => x.Diameter, 120d));

        var svg = cut.Find(".atom-progress-ring-svg");
        // 1 user unit == 1 px, which is what lets stroke-width be a plain px number.
        Assert.Equal("0 0 120 120", svg.GetAttribute("viewBox"));
        Assert.Equal("120", svg.GetAttribute("width"));
        Assert.Equal("120", svg.GetAttribute("height"));
    }

    [Fact]
    public void Radius_is_inset_by_half_the_stroke_so_the_ring_is_not_clipped()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Diameter, 100d)
            .Add(x => x.Thickness, 10d));

        // centre 50, stroke 10 → centreline radius 45, so the outer edge lands exactly on the box.
        Assert.Equal("45", cut.Find(".atom-progress-ring-fill").GetAttribute("r"));
        Assert.Equal("50", cut.Find(".atom-progress-ring-fill").GetAttribute("cx"));
        Assert.Equal("10", cut.Find(".atom-progress-ring-fill").GetAttribute("stroke-width"));
    }

    [Theory]
    [InlineData(ProgressSize.Small, "6")]
    [InlineData(ProgressSize.Medium, "8")]
    [InlineData(ProgressSize.Large, "12")]
    public void Stroke_width_falls_back_to_a_per_size_default(ProgressSize size, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p.Add(x => x.Size, size));

        // Resolved in C# rather than CSS because the radius depends on it.
        Assert.Equal(expected, cut.Find(".atom-progress-ring-fill").GetAttribute("stroke-width"));
    }

    [Fact]
    public void Stroke_wider_than_the_radius_is_clamped_so_the_hole_survives()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Diameter, 40d)
            .Add(x => x.Thickness, 999d));

        Assert.Equal("20", cut.Find(".atom-progress-ring-fill").GetAttribute("stroke-width"));
        Assert.Equal("10", cut.Find(".atom-progress-ring-fill").GetAttribute("r"));
    }

    [Fact]
    public void StartAngle_defaults_to_twelve_oclock_and_is_overridable()
    {
        using var ctx = new BunitContext();

        var dflt = ctx.Render<AtomProgressRing>(p => p.Add(x => x.Diameter, 100d));
        Assert.Equal("rotate(-90 50 50)", dflt.Find(".atom-progress-ring-fill").GetAttribute("transform"));

        var rotated = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Diameter, 100d)
            .Add(x => x.StartAngle, 45d));
        Assert.Equal("rotate(45 50 50)", rotated.Find(".atom-progress-ring-fill").GetAttribute("transform"));
    }

    [Theory]
    [InlineData(ProgressRingCap.Butt, "butt")]
    [InlineData(ProgressRingCap.Round, "round")]
    public void Cap_maps_onto_stroke_linecap(ProgressRingCap cap, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p.Add(x => x.Cap, cap));

        Assert.Equal(expected, cut.Find(".atom-progress-ring-fill").GetAttribute("stroke-linecap"));
    }

    [Fact]
    public void Null_value_is_indeterminate_with_a_full_offset_and_no_valuenow()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>();

        Assert.Equal("true", cut.Find(".atom-progress-ring").GetAttribute("data-indeterminate"));
        // Empty arc inline; the CSS supplies its own dash pair and spins it.
        Assert.Equal("100", cut.Find(".atom-progress-ring-fill").GetAttribute("stroke-dashoffset"));
        Assert.Null(cut.Find(".atom-progress-ring-figure").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void ShowValue_renders_a_centered_readout()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Value, 66d)
            .Add(x => x.ShowValue, true));

        Assert.Equal("66%", cut.Find(".atom-progress-ring-value").TextContent);
    }

    [Fact]
    public void CenterContent_wins_over_the_readout()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Value, 66d)
            .Add(x => x.ShowValue, true)
            .Add(x => x.CenterContent, (RenderFragment)(b => b.AddMarkupContent(0, "<b>3 of 7</b>"))));

        Assert.Contains("3 of 7", cut.Find(".atom-progress-ring-center").InnerHtml);
        Assert.Empty(cut.FindAll(".atom-progress-ring-value"));
    }

    [Fact]
    public void No_center_element_when_there_is_nothing_to_put_in_it()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p.Add(x => x.Value, 10d));

        Assert.Empty(cut.FindAll(".atom-progress-ring-center"));
    }

    [Fact]
    public void Svg_is_hidden_from_assistive_tech_while_the_figure_carries_the_role()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p.Add(x => x.Value, 10d));

        Assert.Equal("true", cut.Find(".atom-progress-ring-svg").GetAttribute("aria-hidden"));
        Assert.Equal("progressbar", cut.Find(".atom-progress-ring-figure").GetAttribute("role"));
    }

    [Fact]
    public void Label_renders_below_and_names_the_control()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Value, 10d)
            .Add(x => x.Label, "Storage"));

        Assert.Equal("Storage", cut.Find(".atom-progress-ring-label").TextContent);
        Assert.Equal("Storage", cut.Find(".atom-progress-ring-figure").GetAttribute("aria-label"));
    }

    [Fact]
    public void Default_aria_label_is_used_when_nothing_else_names_it()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>();

        Assert.Equal("Progress", cut.Find(".atom-progress-ring-figure").GetAttribute("aria-label"));
    }

    [Fact]
    public void No_diameter_token_is_emitted_since_nothing_reads_it()
    {
        using var ctx = new BunitContext();

        // The geometry is already on the SVG's own attributes; a --progress-diameter token would be
        // dead weight in every rendered style attribute.
        var cut = ctx.Render<AtomProgressRing>(p => p.Add(x => x.Diameter, 120d));

        Assert.DoesNotContain("--progress-diameter", cut.Find(".atom-progress-ring").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Axes_and_theming_reach_the_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomProgressRing>(p => p
            .Add(x => x.Variant, ProgressVariant.Success)
            .Add(x => x.Size, ProgressSize.Small)
            .Add(x => x.Effect, ProgressEffect.Glow)
            .Add(x => x.Diameter, 64d)
            .Add(x => x.FillColor, "#f0f"));

        var root = cut.Find(".atom-progress-ring");
        Assert.Equal("success", root.GetAttribute("data-variant"));
        Assert.Equal("small", root.GetAttribute("data-size"));
        Assert.Equal("glow", root.GetAttribute("data-effect"));

        var style = root.GetAttribute("style") ?? "";
        Assert.Contains("--progress-fill-color:#f0f", style);
        // The resolved (clamped) stroke width is exported for the effect rules to read.
        Assert.Contains("--progress-thickness:6px", style);
    }
}
