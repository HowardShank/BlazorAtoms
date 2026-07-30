namespace BlazorAtoms.Skeletons.Tests;

public class AtomSkeletonBlockTests : BunitContext
{
    [Fact]
    public void Renders_one_div_with_the_root_class()
    {
        var cut = Render<AtomSkeletonBlock>();

        var root = cut.Find(".atom-skeleton-block");
        Assert.Equal("DIV", root.TagName);
        Assert.Single(cut.FindAll("div"));
    }

    // ---- animation axis -------------------------------------------------------------------------

    [Fact]
    public void Shimmer_is_the_default_and_emits_the_attribute()
    {
        var cut = Render<AtomSkeletonBlock>();

        Assert.Equal("shimmer", cut.Find(".atom-skeleton-block").GetAttribute("data-animation"));
    }

    [Fact]
    public void Pulse_emits_its_own_attribute_value()
    {
        var cut = Render<AtomSkeletonBlock>(p => p.Add(c => c.Animation, SkeletonAnimation.Pulse));

        Assert.Equal("pulse", cut.Find(".atom-skeleton-block").GetAttribute("data-animation"));
    }

    [Fact]
    public void None_emits_no_attribute_so_the_static_look_is_the_css_default()
    {
        var cut = Render<AtomSkeletonBlock>(p => p.Add(c => c.Animation, SkeletonAnimation.None));

        Assert.False(cut.Find(".atom-skeleton-block").HasAttribute("data-animation"));
    }

    // ---- theming tokens -------------------------------------------------------------------------

    [Fact]
    public void Every_theming_parameter_reaches_the_root_as_a_custom_property()
    {
        var cut = Render<AtomSkeletonBlock>(p => p
            .Add(c => c.BaseColor, "#111")
            .Add(c => c.HighlightColor, "#222")
            .Add(c => c.Duration, "2s")
            .Add(c => c.Width, "80%")
            .Add(c => c.Height, "2rem")
            .Add(c => c.Radius, "50%"));

        var style = cut.Find(".atom-skeleton-block").GetAttribute("style");
        Assert.Contains("--skeleton-base-color:#111", style);
        Assert.Contains("--skeleton-highlight-color:#222", style);
        Assert.Contains("--skeleton-duration:2s", style);
        Assert.Contains("--skeleton-width:80%", style);
        Assert.Contains("--skeleton-height:2rem", style);
        Assert.Contains("--skeleton-radius:50%", style);
    }

    [Fact]
    public void Unset_tokens_emit_nothing_at_all()
    {
        var cut = Render<AtomSkeletonBlock>();

        // No style attribute rather than an empty one: the CSS defaults are the whole point.
        Assert.False(cut.Find(".atom-skeleton-block").HasAttribute("style"));
    }

    [Fact]
    public void Caller_style_is_appended_last_so_it_wins()
    {
        var cut = Render<AtomSkeletonBlock>(p => p
            .Add(c => c.Width, "50%")
            .Add(c => c.Style, "--skeleton-width:90%;"));

        var style = cut.Find(".atom-skeleton-block").GetAttribute("style")!;
        Assert.True(style.IndexOf("--skeleton-width:50%") < style.IndexOf("--skeleton-width:90%"));
    }

    [Fact]
    public void CssClass_is_appended_after_the_root_class()
    {
        var cut = Render<AtomSkeletonBlock>(p => p.Add(c => c.CssClass, "mine"));

        Assert.Equal("atom-skeleton-block mine", cut.Find("div").GetAttribute("class"));
    }

    // ---- visibility -----------------------------------------------------------------------------

    [Fact]
    public void Invisible_stays_in_the_dom_but_is_display_none()
    {
        var cut = Render<AtomSkeletonBlock>(p => p.Add(c => c.Visible, false));

        var root = cut.Find(".atom-skeleton-block");
        Assert.Contains("display:none", root.GetAttribute("style"));
    }

    // ---- accessibility --------------------------------------------------------------------------

    [Fact]
    public void Unnamed_skeletons_are_hidden_from_assistive_tech()
    {
        var cut = Render<AtomSkeletonBlock>();

        var root = cut.Find(".atom-skeleton-block");
        Assert.Equal("true", root.GetAttribute("aria-hidden"));
        Assert.False(root.HasAttribute("role"));
        Assert.False(root.HasAttribute("aria-live"));
    }

    [Fact]
    public void An_AriaLabel_turns_it_into_a_polite_live_region_and_drops_aria_hidden()
    {
        var cut = Render<AtomSkeletonBlock>(p => p.Add(c => c.AriaLabel, "Loading posts"));

        var root = cut.Find(".atom-skeleton-block");
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("polite", root.GetAttribute("aria-live"));
        Assert.Equal("Loading posts", root.GetAttribute("aria-label"));
        // Mutually exclusive: a live region that is also aria-hidden announces nothing.
        Assert.False(root.HasAttribute("aria-hidden"));
    }

    // ---- parameter surface ----------------------------------------------------------------------

    [Fact]
    public void The_primitive_is_the_one_with_a_free_Radius()
    {
        // Pinned as the counterpart to AtomSkeletonAvatar, which deliberately has no Radius.
        Assert.NotNull(typeof(AtomSkeletonBlock).GetProperty("Radius"));
    }
}
