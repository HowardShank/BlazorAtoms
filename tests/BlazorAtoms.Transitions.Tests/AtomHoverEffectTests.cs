using System.Linq;
using Bunit;
using Xunit;

namespace BlazorAtoms.Transitions.Tests;

/// <summary>bUnit coverage for <see cref="AtomHoverEffect"/> — a zero-JS hover wrapper whose
/// trigger is plain CSS :hover/:active, so there's no state-flip behavior to test like
/// <see cref="AtomTransition"/>; assertions here cover markup/class/style wiring only.</summary>
public class AtomHoverEffectTests
{
    [Fact]
    public void Renders_child_content_inside_content_wrapper()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomHoverEffect>(p => p.AddChildContent("<span>Click!</span>"));

        var content = cut.Find(".atom-hover-effect-content");
        Assert.Equal("Click!", content.TextContent);
    }

    [Fact]
    public void Default_effect_is_sparkle_and_renders_SparkleCount_svgs()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomHoverEffect>(p => p
            .AddChildContent("<span>hi</span>")
            .Add(x => x.SparkleCount, 6));

        var root = cut.Find(".atom-hover-effect");
        Assert.Contains("atom-hover-effect-sparkle", root.ClassList);

        var svgs = cut.FindAll(".atom-hover-effect-svg");
        Assert.Equal(6, svgs.Count);
        var positions = svgs.Select(s => s.GetAttribute("style")).Distinct().ToList();
        Assert.True(positions.Count > 1);
    }

    [Fact]
    public void Href_set_renders_a_real_link()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomHoverEffect>(p => p
            .AddChildContent("<span>hi</span>")
            .Add(x => x.Href, "/somewhere"));

        var root = cut.Find(".atom-hover-effect");
        Assert.Equal("/somewhere", root.GetAttribute("href"));
        Assert.Null(root.GetAttribute("tabindex"));
    }

    [Fact]
    public void Href_unset_renders_focusable_non_link()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomHoverEffect>(p => p.AddChildContent("<span>hi</span>"));

        var root = cut.Find(".atom-hover-effect");
        Assert.Null(root.GetAttribute("href"));
        Assert.Equal("0", root.GetAttribute("tabindex"));
    }

    [Fact]
    public void GlowColor_and_ScaleAmount_flow_into_root_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomHoverEffect>(p => p
            .AddChildContent("<span>hi</span>")
            .Add(x => x.GlowColor, "#00ffee")
            .Add(x => x.ScaleAmount, 1.2));

        var root = cut.Find(".atom-hover-effect");
        var style = root.GetAttribute("style");
        Assert.Contains("--atom-hover-effect-glow:#00ffee;", style);
        Assert.Contains("--atom-hover-effect-scale:1.2;", style);
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomHoverEffect>(p => p
            .AddChildContent("<span>hi</span>")
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "color:red;"));

        var root = cut.Find(".atom-hover-effect");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("color:red;", root.GetAttribute("style"));
    }
}
