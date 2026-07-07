using System.ComponentModel;

namespace BlazorAtoms.Badges.Tests;

public class AtomStaticBadgeTests : TestContext
{
    private enum Priority { [Description("High priority")] High }

    [Fact]
    public void Null_value_renders_nothing()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p.Add(c => c.Value, null));
        Assert.Empty(cut.FindAll(".atom-sbadge"));
    }

    [Fact]
    public void Empty_string_renders_nothing()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p.Add(c => c.Value, ""));
        Assert.Empty(cut.FindAll(".atom-sbadge"));
    }

    [Fact]
    public void Nonempty_value_renders_badge_with_text()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p.Add(c => c.Value, "NEW"));
        Assert.Equal("NEW", cut.Find(".atom-sbadge-text").TextContent);
    }

    [Fact]
    public void Zero_hidden_by_default_shown_with_ShowZero()
    {
        var hidden = RenderComponent<AtomStaticBadge>(p => p.Add(c => c.Value, 0));
        Assert.Empty(hidden.FindAll(".atom-sbadge"));

        var shown = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, 0)
            .Add(c => c.ShowZero, true));
        Assert.Equal("0", shown.Find(".atom-sbadge-text").TextContent);
    }

    [Fact]
    public void Numeric_over_max_shows_overflow()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, 125)
            .Add(c => c.Max, 99));
        Assert.Equal("99+", cut.Find(".atom-sbadge-text").TextContent);
    }

    [Fact]
    public void Dot_shows_without_text_for_true()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, true)
            .Add(c => c.Dot, true));

        Assert.Equal("true", cut.Find(".atom-sbadge").GetAttribute("data-dot"));
        Assert.Empty(cut.FindAll(".atom-sbadge-text"));

        var off = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.Dot, true));
        Assert.Empty(off.FindAll(".atom-sbadge"));
    }

    [Fact]
    public void Formatter_overrides_type_defaults()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, 42)
            .Add(c => c.Formatter, v => $"#{v}"));
        Assert.Equal("#42", cut.Find(".atom-sbadge-text").TextContent);
    }

    [Fact]
    public void Enum_uses_description_attribute()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p.Add(c => c.Value, Priority.High));
        Assert.Equal("High priority", cut.Find(".atom-sbadge-text").TextContent);
    }

    [Fact]
    public void Overlay_wraps_child_and_sets_placement()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.Placement, Placement.BottomStart)
            .AddChildContent("<button>bell</button>"));

        Assert.NotNull(cut.Find(".atom-sbadge-host"));
        Assert.Contains("bell", cut.Markup);
        Assert.Equal("bottom-start", cut.Find(".atom-sbadge").GetAttribute("data-placement"));
    }

    [Fact]
    public void Inline_when_no_child_content()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p.Add(c => c.Value, 3));
        Assert.Empty(cut.FindAll(".atom-sbadge-host"));
        Assert.NotNull(cut.Find(".atom-sbadge"));
    }

    [Fact]
    public void Has_status_role()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p.Add(c => c.Value, 3));
        Assert.Equal("status", cut.Find(".atom-sbadge").GetAttribute("role"));
    }

    [Fact]
    public void AriaLabel_overrides_display_text()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.AriaLabel, "3 unread messages"));
        Assert.Equal("3 unread messages", cut.Find(".atom-sbadge").GetAttribute("aria-label"));
    }

    [Fact]
    public void Css_shape_has_no_svg()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.Shape, Shape.Pill));
        Assert.Null(cut.Find(".atom-sbadge").GetAttribute("data-svg"));
        Assert.Empty(cut.FindAll(".atom-sbadge-svg"));
    }

    [Theory]
    [InlineData(Shape.Star, "star")]
    [InlineData(Shape.Hexagon, "hexagon")]
    [InlineData(Shape.Diamond, "diamond")]
    [InlineData(Shape.Shield, "shield")]
    [InlineData(Shape.Burst, "burst")]
    [InlineData(Shape.Ribbon, "ribbon")]
    public void Svg_shape_draws_path(Shape shape, string expected)
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.Shape, shape));

        var badge = cut.Find(".atom-sbadge");
        Assert.Equal(expected, badge.GetAttribute("data-shape"));
        Assert.Equal("true", badge.GetAttribute("data-svg"));
        var path = cut.Find(".sb-path");
        Assert.False(string.IsNullOrWhiteSpace(path.GetAttribute("d")));
    }

    [Fact]
    public void Size_and_color_tokens_emitted_on_inline_badge()
    {
        var cut = RenderComponent<AtomStaticBadge>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.Size, 40)
            .Add(c => c.Background, "#7c3aed")
            .Add(c => c.Width, "60px"));

        var style = cut.Find(".atom-sbadge").GetAttribute("style") ?? "";
        Assert.Contains("--sb-size:40px", style);
        Assert.Contains("--sb-bg:#7c3aed", style);
        Assert.Contains("--sb-width:60px", style);
    }
}
