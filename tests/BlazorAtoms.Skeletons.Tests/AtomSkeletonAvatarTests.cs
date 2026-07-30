namespace BlazorAtoms.Skeletons.Tests;

public class AtomSkeletonAvatarTests : BunitContext
{
    [Fact]
    public void Adds_no_wrapper_element_of_its_own()
    {
        // It is a preset, not a container: one div, carrying both classes.
        var cut = Render<AtomSkeletonAvatar>();

        Assert.Single(cut.FindAll("div"));
        Assert.Equal("atom-skeleton-block atom-skeleton-avatar", cut.Find("div").GetAttribute("class"));
    }

    [Fact]
    public void Size_drives_both_axes_so_it_can_never_be_an_ellipse()
    {
        var cut = Render<AtomSkeletonAvatar>(p => p.Add(c => c.Size, "72px"));

        var style = cut.Find("div").GetAttribute("style");
        Assert.Contains("--skeleton-width:72px", style);
        Assert.Contains("--skeleton-height:72px", style);
    }

    [Fact]
    public void Defaults_to_a_forty_pixel_circle()
    {
        var cut = Render<AtomSkeletonAvatar>();

        var style = cut.Find("div").GetAttribute("style");
        Assert.Contains("--skeleton-width:40px", style);
        Assert.Contains("--skeleton-radius:50%", style);
    }

    [Theory]
    [InlineData(SkeletonAvatarShape.Circle, "--skeleton-radius:50%")]
    [InlineData(SkeletonAvatarShape.Square, "--skeleton-radius:0")]
    [InlineData(SkeletonAvatarShape.Rounded, "--skeleton-radius:0.5rem")]
    public void Shape_picks_the_corner_radius(SkeletonAvatarShape shape, string expected)
    {
        var cut = Render<AtomSkeletonAvatar>(p => p.Add(c => c.Shape, shape));

        Assert.Contains(expected, cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void A_circle_stays_round_at_any_size()
    {
        // The radius is a percentage rather than a length, which is the whole reason Shape owns it.
        var cut = Render<AtomSkeletonAvatar>(p => p
            .Add(c => c.Shape, SkeletonAvatarShape.Circle)
            .Add(c => c.Size, "8rem"));

        Assert.Contains("--skeleton-radius:50%", cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void Has_no_Radius_parameter()
    {
        // Deliberate: Shape owns the corners, and a Radius the default Circle silently ignored would
        // be a parameter that is invalid for the default value. AtomSkeletonBlock is where a free
        // radius lives — its own test pins that, so this pair fails loudly if either drifts.
        Assert.Null(typeof(AtomSkeletonAvatar).GetProperty("Radius"));
    }

    [Fact]
    public void Has_no_Width_or_Height_parameters_either()
    {
        Assert.Null(typeof(AtomSkeletonAvatar).GetProperty("Width"));
        Assert.Null(typeof(AtomSkeletonAvatar).GetProperty("Height"));
    }

    // ---- forwarding -----------------------------------------------------------------------------

    [Fact]
    public void Forwards_the_inherited_axes_to_the_block_it_renders()
    {
        var cut = Render<AtomSkeletonAvatar>(p => p
            .Add(c => c.Animation, SkeletonAnimation.Pulse)
            .Add(c => c.BaseColor, "#123")
            .Add(c => c.HighlightColor, "#456")
            .Add(c => c.Duration, "0.9s"));

        var root = cut.Find("div");
        Assert.Equal("pulse", root.GetAttribute("data-animation"));
        var style = root.GetAttribute("style");
        Assert.Contains("--skeleton-base-color:#123", style);
        Assert.Contains("--skeleton-highlight-color:#456", style);
        Assert.Contains("--skeleton-duration:0.9s", style);
    }

    [Fact]
    public void Forwards_visibility_and_the_accessible_name()
    {
        var cut = Render<AtomSkeletonAvatar>(p => p
            .Add(c => c.Visible, false)
            .Add(c => c.AriaLabel, "Loading avatar"));

        var root = cut.Find("div");
        Assert.Contains("display:none", root.GetAttribute("style"));
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("Loading avatar", root.GetAttribute("aria-label"));
    }

    [Fact]
    public void Forwards_the_attribute_splat()
    {
        var cut = Render<AtomSkeletonAvatar>(p => p.AddUnmatched("data-test", "x"));

        Assert.Equal("x", cut.Find("div").GetAttribute("data-test"));
    }
}
