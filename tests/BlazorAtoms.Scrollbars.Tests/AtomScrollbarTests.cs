using Bunit;
using Xunit;

namespace BlazorAtoms.Scrollbars.Tests;

/// <summary>bUnit coverage for <see cref="AtomScrollbar"/>. Purely declarative styling — no JS
/// interop at all, so no JSInterop setup is needed for any of these.</summary>
public class AtomScrollbarTests
{
    [Fact]
    public void Renders_child_content()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p.AddChildContent("<p>hello</p>"));

        Assert.Contains("hello", cut.Markup);
    }

    [Fact]
    public void Default_axis_is_vertical()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>();

        var root = cut.Find(".atom-scrollbar");
        Assert.Contains("atom-scrollbar-axis-vertical", root.ClassList);
    }

    [Theory]
    [InlineData(ScrollbarAxis.Horizontal, "atom-scrollbar-axis-horizontal")]
    [InlineData(ScrollbarAxis.Both, "atom-scrollbar-axis-both")]
    public void Axis_selects_matching_class(ScrollbarAxis axis, string expectedClass)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p.Add(x => x.Axis, axis));

        var root = cut.Find(".atom-scrollbar");
        Assert.Contains(expectedClass, root.ClassList);
    }

    [Fact]
    public void Colors_size_and_box_dimensions_flow_into_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p
            .Add(x => x.BoxHeight, "400px")
            .Add(x => x.BoxWidth, "50%")
            .Add(x => x.ScrollbarSize, "8px")
            .Add(x => x.TrackColor, "#111")
            .Add(x => x.ThumbColor, "#0ae"));

        var style = cut.Find(".atom-scrollbar").GetAttribute("style");
        Assert.Contains("--atom-scrollbar-box-height:400px;", style);
        Assert.Contains("--atom-scrollbar-box-width:50%;", style);
        Assert.Contains("--atom-scrollbar-size:8px;", style);
        Assert.Contains("--atom-scrollbar-track-color:#111;", style);
        Assert.Contains("--atom-scrollbar-thumb-bg:#0ae;", style);
        Assert.Contains("--atom-scrollbar-firefox-color:#0ae #111;", style);
    }

    [Fact]
    public void ThumbGradientEnd_set_produces_linear_gradient_background()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p
            .Add(x => x.ThumbColor, "#4D9C41")
            .Add(x => x.ThumbGradientEnd, "#19911D")
            .Add(x => x.ThumbGradientAngle, "45deg"));

        var style = cut.Find(".atom-scrollbar").GetAttribute("style");
        Assert.Contains("--atom-scrollbar-thumb-bg:linear-gradient(45deg, #4D9C41, #19911D);", style);
    }

    [Fact]
    public void ThumbHoverColor_unset_falls_back_to_thumb_background()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p.Add(x => x.ThumbColor, "#555"));

        var style = cut.Find(".atom-scrollbar").GetAttribute("style");
        Assert.Contains("--atom-scrollbar-thumb-hover-bg:#555;", style);
    }

    [Fact]
    public void ThumbHoverColor_set_overrides_default_hover_background()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p
            .Add(x => x.ThumbColor, "#555")
            .Add(x => x.ThumbHoverColor, "#D62929"));

        var style = cut.Find(".atom-scrollbar").GetAttribute("style");
        Assert.Contains("--atom-scrollbar-thumb-hover-bg:#D62929;", style);
    }

    [Fact]
    public void ThumbBorder_unset_renders_none()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>();

        var style = cut.Find(".atom-scrollbar").GetAttribute("style");
        Assert.Contains("--atom-scrollbar-thumb-border:none;", style);
    }

    [Fact]
    public void ThumbBorder_set_flows_through_verbatim()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p.Add(x => x.ThumbBorder, "2px solid #555555"));

        var style = cut.Find(".atom-scrollbar").GetAttribute("style");
        Assert.Contains("--atom-scrollbar-thumb-border:2px solid #555555;", style);
    }

    [Theory]
    [InlineData("6px", "thin")]
    [InlineData("10px", "thin")]
    [InlineData("12px", "auto")]
    [InlineData("20px", "auto")]
    public void ScrollbarSize_maps_to_firefox_thin_or_auto(string size, string expectedWidth)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p.Add(x => x.ScrollbarSize, size));

        var style = cut.Find(".atom-scrollbar").GetAttribute("style");
        Assert.Contains($"--atom-scrollbar-firefox-width:{expectedWidth};", style);
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomScrollbar>(p => p
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "opacity:.9;"));

        var root = cut.Find(".atom-scrollbar");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("opacity:.9;", root.GetAttribute("style"));
    }
}
