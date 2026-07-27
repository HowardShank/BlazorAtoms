using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCardExpand"/>. Purely declarative — no JS interop.</summary>
public class AtomCardExpandTests
{
    [Fact]
    public void Renders_chrome_and_body()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardExpand>(p => p
            .Add(x => x.Title, "Trees")
            .Add(x => x.BodyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>woody plants</p>"))));

        Assert.Contains("Trees", cut.Find(".atom-card-expand-title").TextContent);
        Assert.Contains("woody plants", cut.Find(".atom-card-expand-body").InnerHtml);
    }

    [Fact]
    public void Expand_params_flow_into_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardExpand>(p => p
            .Add(x => x.ExpandedHeight, "700px")
            .Add(x => x.BodyHeight, "50%")
            .Add(x => x.BodyColor, "#fafafa"));

        var style = cut.Find(".atom-card-expand").GetAttribute("style");
        Assert.Contains("--atom-card-expand-expanded-height:700px;", style);
        Assert.Contains("--atom-card-expand-body-height:50%;", style);
        Assert.Contains("--atom-card-expand-body-color:#fafafa;", style);
    }

    [Fact]
    public void Inherits_shared_card_params_from_the_base()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardExpand>(p => p
            .Add(x => x.AccentColor, "#186218")
            .Add(x => x.Height, "300px"));

        var style = cut.Find(".atom-card-expand").GetAttribute("style");
        Assert.Contains("--atom-card-accent:#186218;", style);
        Assert.Contains("--atom-card-height:300px;", style);
    }

    [Fact]
    public void DotBorderColor_unset_follows_AccentColor()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardExpand>(p => p.Add(x => x.AccentColor, "#123456"));

        Assert.Contains("--atom-card-dot-border-color:#123456;",
            cut.Find(".atom-card-expand").GetAttribute("style"));
    }

    [Fact]
    public void DotCount_zero_hides_dots()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardExpand>(p => p.Add(x => x.DotCount, 0));

        Assert.Empty(cut.FindAll(".atom-card-expand-dots"));
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardExpand>(p => p
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "opacity:.9;"));

        var root = cut.Find(".atom-card-expand");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("opacity:.9;", root.GetAttribute("style"));
    }
}
