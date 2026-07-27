using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCardFlip"/>. Purely declarative — no JS interop.</summary>
public class AtomCardFlipTests
{
    [Fact]
    public void Renders_front_chrome_and_back_body()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFlip>(p => p
            .Add(x => x.Title, "Trees")
            .Add(x => x.BodyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>woody plants</p>"))));

        Assert.Contains("Trees", cut.Find(".atom-card-flip-title").TextContent);
        Assert.Contains("woody plants", cut.Find(".atom-card-flip-back").InnerHtml);
    }

    [Fact]
    public void Default_flip_axis_is_y()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFlip>();

        Assert.Contains("atom-card-flip-axis-y", cut.Find(".atom-card-flip").ClassList);
    }

    [Theory]
    [InlineData(CardFlipAxis.Y, "atom-card-flip-axis-y")]
    [InlineData(CardFlipAxis.X, "atom-card-flip-axis-x")]
    public void FlipAxis_selects_matching_class(CardFlipAxis axis, string expectedClass)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFlip>(p => p.Add(x => x.FlipAxis, axis));

        Assert.Contains(expectedClass, cut.Find(".atom-card-flip").ClassList);
    }

    [Fact]
    public void Does_not_expose_a_reveal_size()
    {
        // AtomCardFlip is its own component precisely so RevealSize does not appear on a type where
        // nothing is partially uncovered. If a shared enum ever merges these, this fails first.
        Assert.Null(typeof(AtomCardFlip).GetProperty("RevealSize"));
    }

    [Fact]
    public void Perspective_and_BackColor_flow_into_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFlip>(p => p
            .Add(x => x.Perspective, "900px")
            .Add(x => x.BackColor, "#eee"));

        var style = cut.Find(".atom-card-flip").GetAttribute("style");
        Assert.Contains("--atom-card-flip-perspective:900px;", style);
        Assert.Contains("--atom-card-flip-back-color:#eee;", style);
    }

    [Fact]
    public void Inherits_shared_card_params_from_the_base()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFlip>(p => p
            .Add(x => x.AccentColor, "#186218")
            .Add(x => x.Width, "400px")
            .Add(x => x.DotColor, "#ff0"));

        var style = cut.Find(".atom-card-flip").GetAttribute("style");
        Assert.Contains("--atom-card-accent:#186218;", style);
        Assert.Contains("--atom-card-width:400px;", style);
        Assert.Contains("--atom-card-dot-color:#ff0;", style);
    }

    [Fact]
    public void DotCount_zero_hides_dots()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFlip>(p => p.Add(x => x.DotCount, 0));

        Assert.Empty(cut.FindAll(".atom-card-flip-dots"));
    }

    [Fact]
    public void Border_params_reach_the_faces_where_the_frame_actually_lives()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFlip>(p => p
            .Add(x => x.BorderWidth, "0")
            .Add(x => x.BorderColor, "#222"));

        // Flip's frame is on .atom-card-flip-face, not the root — the root is only the perspective
        // container. That is why Style="border:none" could never remove it and BorderWidth had to
        // exist: the custom properties are set on the root and inherit down to the faces.
        var style = cut.Find(".atom-card-flip").GetAttribute("style");
        Assert.Contains("--atom-card-border-width:0;", style);
        Assert.Contains("--atom-card-border-color:#222;", style);
        Assert.Equal(2, cut.FindAll(".atom-card-flip-face").Count);
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFlip>(p => p
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "opacity:.9;"));

        var root = cut.Find(".atom-card-flip");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("opacity:.9;", root.GetAttribute("style"));
    }
}
