using System.ComponentModel;

namespace BlazorAtoms.AnimatedBadges.Tests;

public class AtomAnimatedBadgeTests : TestContext
{
    private enum Priority { [Description("High priority")] High }

    [Fact]
    public void Null_value_renders_nothing()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p.Add(c => c.Value, null));
        Assert.Empty(cut.FindAll(".atom-badge"));
    }

    [Fact]
    public void Empty_string_renders_nothing()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p.Add(c => c.Value, ""));
        Assert.Empty(cut.FindAll(".atom-badge"));
    }

    [Fact]
    public void Nonempty_value_renders_badge_with_text()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p.Add(c => c.Value, "NEW"));
        Assert.Equal("NEW", cut.Find(".atom-badge-text").TextContent);
    }

    [Fact]
    public void Zero_hidden_by_default_shown_with_ShowZero()
    {
        var hidden = RenderComponent<AtomAnimatedBadge>(p => p.Add(c => c.Value, 0));
        Assert.Empty(hidden.FindAll(".atom-badge"));

        var shown = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, 0)
            .Add(c => c.ShowZero, true));
        Assert.Equal("0", shown.Find(".atom-badge-text").TextContent);
    }

    [Fact]
    public void Numeric_over_max_shows_overflow()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, 125)
            .Add(c => c.Max, 99));
        Assert.Equal("99+", cut.Find(".atom-badge-text").TextContent);
    }

    [Fact]
    public void Dot_shows_without_text_for_true()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, true)
            .Add(c => c.Dot, true));

        Assert.Equal("true", cut.Find(".atom-badge").GetAttribute("data-dot"));
        Assert.Empty(cut.FindAll(".atom-badge-text"));

        // false → not present → hidden.
        var off = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.Dot, true));
        Assert.Empty(off.FindAll(".atom-badge"));
    }

    [Fact]
    public void Formatter_overrides_type_defaults()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, 42)
            .Add(c => c.Formatter, v => $"#{v}"));
        Assert.Equal("#42", cut.Find(".atom-badge-text").TextContent);
    }

    [Fact]
    public void Enum_uses_description_attribute()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p.Add(c => c.Value, Priority.High));
        Assert.Equal("High priority", cut.Find(".atom-badge-text").TextContent);
    }

    [Fact]
    public void Overlay_wraps_child_and_sets_placement()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.Placement, Placement.BottomStart)
            .AddChildContent("<button>bell</button>"));

        Assert.NotNull(cut.Find(".atom-badge-host"));
        Assert.Contains("bell", cut.Markup);
        Assert.Equal("bottom-start", cut.Find(".atom-badge").GetAttribute("data-placement"));
    }

    [Fact]
    public void Inline_when_no_child_content()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p.Add(c => c.Value, 3));
        Assert.Empty(cut.FindAll(".atom-badge-host"));
        Assert.NotNull(cut.Find(".atom-badge"));
    }

    [Fact]
    public void Has_status_role_and_live_region()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p.Add(c => c.Value, 3));
        var badge = cut.Find(".atom-badge");
        Assert.Equal("status", badge.GetAttribute("role"));
        Assert.Equal("polite", badge.GetAttribute("aria-live"));
    }

    [Fact]
    public void AriaLabel_overrides_display_text()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.AriaLabel, "3 unread messages"));
        Assert.Equal("3 unread messages", cut.Find(".atom-badge").GetAttribute("aria-label"));
    }

    [Fact]
    public void Animation_and_trigger_set_data_attributes()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.Animation, BadgeAnimation.Bounce)
            .Add(c => c.Trigger, AnimationTrigger.OnChange));

        var badge = cut.Find(".atom-badge");
        Assert.Equal("bounce", badge.GetAttribute("data-anim"));
        Assert.Equal("onchange", badge.GetAttribute("data-trigger"));
    }

    [Fact]
    public void Size_timing_tokens_emitted_on_inline_badge()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.Width, "40px")
            .Add(c => c.Height, "24px")
            .Add(c => c.Duration, 1.5)
            .Add(c => c.Delay, 0.25));

        var style = cut.Find(".atom-badge").GetAttribute("style") ?? "";
        Assert.Contains("--badge-width:40px", style);
        Assert.Contains("--badge-height:24px", style);
        Assert.Contains("--badge-anim-duration:1.5s", style);
        Assert.Contains("--badge-anim-delay:0.25s", style);
    }

    [Fact]
    public void Ping_animation_renders_ring()
    {
        var cut = RenderComponent<AtomAnimatedBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.Animation, BadgeAnimation.Ping));
        Assert.NotNull(cut.Find(".atom-badge-ring"));
    }
}
