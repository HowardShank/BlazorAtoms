using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCardSplit"/>. Purely declarative — no JS interop.</summary>
public class AtomCardSplitTests
{
    [Fact]
    public void Renders_two_halves_over_one_body_panel()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p
            .Add(x => x.BodyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>woody plants</p>"))));

        Assert.Equal(2, cut.FindAll(".atom-card-split-half").Count);
        Assert.Single(cut.FindAll(".atom-card-split-body"));
        Assert.Contains("woody plants", cut.Find(".atom-card-split-body").InnerHtml);
    }

    [Fact]
    public void Body_content_is_not_duplicated_across_the_halves()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p
            .Add(x => x.BodyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>read me once</p>"))));

        // The whole reason the halves have no back faces: content split across two backs would render
        // twice (screen readers read it twice) and could break mid-glyph at the seam.
        var occurrences = cut.Markup.Split("read me once").Length - 1;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void Each_half_carries_its_own_copy_of_the_background_image()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p.Add(x => x.BackgroundImageUrl, "trees.png"));

        // Both halves hold the FULL image pinned to opposite edges — that anchoring is what makes the
        // closed card read as one unbroken picture.
        var images = cut.FindAll(".atom-card-split-image");
        Assert.Equal(2, images.Count);
        Assert.All(images, i => Assert.Contains("background-image:url('trees.png')", i.GetAttribute("style")));
    }

    [Fact]
    public void Default_split_axis_is_vertical()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>();

        Assert.Contains("atom-card-split-vertical", cut.Find(".atom-card-split").ClassList);
    }

    [Theory]
    [InlineData(CardSplitAxis.Vertical, "atom-card-split-vertical")]
    [InlineData(CardSplitAxis.Horizontal, "atom-card-split-horizontal")]
    public void SplitAxis_selects_matching_class(CardSplitAxis axis, string expectedClass)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p.Add(x => x.SplitAxis, axis));

        Assert.Contains(expectedClass, cut.Find(".atom-card-split").ClassList);
    }

    [Fact]
    public void Seam_circle_is_opt_in_and_off_by_default()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>();

        Assert.Empty(cut.FindAll(".atom-card-split-circle"));
    }

    [Fact]
    public void Seam_circle_renders_one_half_per_side_and_is_decorative()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p.Add(x => x.ShowSeamCircle, true));

        // One per half: each is clipped to its own side, so together they form a whole circle while
        // the card is closed and split apart as the shutters open.
        var circles = cut.FindAll(".atom-card-split-circle");
        Assert.Equal(2, circles.Count);
        Assert.All(circles, c => Assert.Equal("true", c.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Split_params_flow_into_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p
            .Add(x => x.Perspective, "800px")
            .Add(x => x.OpenDuration, "1s")
            .Add(x => x.SeamCircleColor, "#ff0")
            .Add(x => x.SeamCircleSize, "60px")
            .Add(x => x.BodyColor, "#fafafa"));

        var style = cut.Find(".atom-card-split").GetAttribute("style");
        Assert.Contains("--atom-card-split-perspective:800px;", style);
        Assert.Contains("--atom-card-split-duration:1s;", style);
        Assert.Contains("--atom-card-split-circle-color:#ff0;", style);
        Assert.Contains("--atom-card-split-circle-size:60px;", style);
        Assert.Contains("--atom-card-split-body-color:#fafafa;", style);
    }

    [Fact]
    public void Inherits_shared_card_params_including_the_border_ones()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p
            .Add(x => x.AccentColor, "#186218")
            .Add(x => x.BorderWidth, "0"));

        var style = cut.Find(".atom-card-split").GetAttribute("style");
        Assert.Contains("--atom-card-accent:#186218;", style);
        Assert.Contains("--atom-card-border-width:0;", style);
        Assert.Contains("--atom-card-border-color:#186218;", style);
    }

    [Fact]
    public void DotCount_zero_hides_dots()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p.Add(x => x.DotCount, 0));

        Assert.Empty(cut.FindAll(".atom-card-split-dots"));
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardSplit>(p => p
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "opacity:.9;"));

        var root = cut.Find(".atom-card-split");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("opacity:.9;", root.GetAttribute("style"));
    }
}
