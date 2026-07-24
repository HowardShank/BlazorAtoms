using System.Linq;
using Bunit;
using BlazorAtoms.Typography;
using Xunit;

namespace BlazorAtoms.Typography.Tests;

public class AtomTextSparkleTests
{
    [Fact]
    public void Renders_label_and_glare_layer()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextSparkle>(p => p.Add(x => x.Text, "Click!"));

        var labels = cut.FindAll(".atom-text-sparkle-label");
        Assert.Equal(2, labels.Count);
        Assert.All(labels, l => Assert.Equal("Click!", l.TextContent));
        var glare = cut.Find(".atom-text-sparkle-glare");
        Assert.Equal("true", glare.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Empty_text_renders_nothing()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextSparkle>(p => p.Add(x => x.Text, ""));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Renders_SparkleCount_svgs_with_scattered_positions()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextSparkle>(p => p
            .Add(x => x.Text, "hi")
            .Add(x => x.SparkleCount, 7));

        var svgs = cut.FindAll(".atom-text-sparkle-svg");
        Assert.Equal(7, svgs.Count);

        // Distinct positions — a real scatter, not all sparkles stacked on one spot.
        var positions = svgs.Select(s => s.GetAttribute("style")).Distinct().ToList();
        Assert.True(positions.Count > 1);
    }

    [Fact]
    public void Href_set_renders_a_real_link()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextSparkle>(p => p
            .Add(x => x.Text, "hi")
            .Add(x => x.Href, "/somewhere"));

        var root = cut.Find(".atom-text-sparkle");
        Assert.Equal("/somewhere", root.GetAttribute("href"));
        Assert.Null(root.GetAttribute("tabindex"));
    }

    [Fact]
    public void Href_unset_renders_focusable_non_link()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextSparkle>(p => p.Add(x => x.Text, "hi"));

        var root = cut.Find(".atom-text-sparkle");
        Assert.Null(root.GetAttribute("href"));
        Assert.Equal("0", root.GetAttribute("tabindex"));
    }

    [Fact]
    public void Colors_flow_into_root_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextSparkle>(p => p
            .Add(x => x.Text, "hi")
            .Add(x => x.Color, "#111111")
            .Add(x => x.ShadowColor, "#222222")
            .Add(x => x.GlareColor, "#333333"));

        var root = cut.Find(".atom-text-sparkle");
        var style = root.GetAttribute("style");
        Assert.Contains("--atom-text-sparkle-color:#111111;", style);
        Assert.Contains("--atom-text-sparkle-shadow:#222222;", style);
        Assert.Contains("--atom-text-sparkle-glare:#333333;", style);
    }

    [Fact]
    public void FontSize_flows_into_root_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextSparkle>(p => p
            .Add(x => x.Text, "hi")
            .Add(x => x.FontSize, "2rem"));

        var root = cut.Find(".atom-text-sparkle");
        Assert.Contains("--atom-text-sparkle-font-size:2rem;", root.GetAttribute("style"));
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextSparkle>(p => p
            .Add(x => x.Text, "hi")
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "color:red;"));

        var root = cut.Find(".atom-text-sparkle");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("color:red;", root.GetAttribute("style"));
    }
}
