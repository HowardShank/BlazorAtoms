using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCardReveal"/>. Purely declarative — no JS interop,
/// so no JSInterop setup is needed for any of these.</summary>
public class AtomCardRevealTests
{
    [Fact]
    public void Renders_title_and_body_content()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p
            .Add(x => x.Title, "Beaches")
            .Add(x => x.BodyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Beaches are sandy shores by the ocean that provide relaxation and enjoyment.</p>"))));

        Assert.Contains("Beaches", cut.Find(".atom-card-reveal-title").TextContent);
        Assert.Contains("Beaches are sandy shores by the ocean that provide relaxation and enjoyment.", cut.Markup);
    }

    [Fact]
    public void Subtitle_renders_when_set()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p
            .Add(x => x.Subtitle, (RenderFragment)(b => b.AddMarkupContent(0, "Vacation: <em>Relaxation</em>"))));

        var subtitle = cut.Find(".atom-card-reveal-subtitle");
        Assert.Contains("Vacation:", subtitle.TextContent);
        Assert.Contains("<em>Relaxation</em>", subtitle.InnerHtml);
    }

    [Fact]
    public void Subtitle_omitted_when_null()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>();

        Assert.Empty(cut.FindAll(".atom-card-reveal-subtitle"));
    }

    [Fact]
    public void Default_dot_count_is_three()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>();

        Assert.Equal(3, cut.FindAll(".atom-card-reveal-dot").Count);
    }

    [Fact]
    public void DotCount_zero_hides_dots_container()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.DotCount, 0));

        Assert.Empty(cut.FindAll(".atom-card-reveal-dots"));
    }

    [Fact]
    public void DotCount_custom_renders_that_many_dots_with_increasing_delay()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.DotCount, 5));

        var dots = cut.FindAll(".atom-card-reveal-dot");
        Assert.Equal(5, dots.Count);
        Assert.Contains("animation-delay:1.8s;", dots[0].GetAttribute("style"));
        Assert.Contains("animation-delay:2.1s;", dots[1].GetAttribute("style"));
        Assert.Contains("animation-delay:3s;", dots[4].GetAttribute("style"));
    }

    [Fact]
    public void AccentColor_Width_Height_and_BackgroundImageUrl_flow_into_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p
            .Add(x => x.AccentColor, "#0a5")
            .Add(x => x.Width, "500px")
            .Add(x => x.Height, "400px")
            .Add(x => x.BackgroundImageUrl, "trees.png"));

        var style = cut.Find(".atom-card-reveal").GetAttribute("style");
        Assert.Contains("--atom-card-accent:#0a5;", style);
        Assert.Contains("--atom-card-width:500px;", style);
        Assert.Contains("--atom-card-height:400px;", style);

        // Set inline on the image div itself, not via CSS custom property — a relative url()
        // inside a custom property resolves against the stylesheet consuming var(), not the
        // caller's document (verified live: broke with a /_content/BlazorAtoms.Cards/ prefix).
        var imageStyle = cut.Find(".atom-card-reveal-image").GetAttribute("style");
        Assert.Contains("background-image:url('trees.png')", imageStyle);
    }

    [Fact]
    public void Dot_colors_flow_into_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p
            .Add(x => x.DotColor, "#ff0")
            .Add(x => x.DotBorderColor, "#303")
            .Add(x => x.DotHoverColor, "#eee"));

        var style = cut.Find(".atom-card-reveal").GetAttribute("style");
        Assert.Contains("--atom-card-dot-color:#ff0;", style);
        Assert.Contains("--atom-card-dot-border-color:#303;", style);
        Assert.Contains("--atom-card-dot-hover-color:#eee;", style);
    }

    [Fact]
    public void DotBorderColor_unset_follows_AccentColor()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.AccentColor, "#186218"));

        var style = cut.Find(".atom-card-reveal").GetAttribute("style");
        Assert.Contains("--atom-card-dot-border-color:#186218;", style);
    }

    [Fact]
    public void Default_border_reproduces_the_previous_hardcoded_frame()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.AccentColor, "#186218"));

        // Adding BorderWidth/BorderColor must not change how existing markup renders: the frame was
        // a hardcoded 8px in the accent color before these params existed.
        var style = cut.Find(".atom-card-reveal").GetAttribute("style");
        Assert.Contains("--atom-card-border-width:8px;", style);
        Assert.Contains("--atom-card-border-color:#186218;", style);
    }

    [Fact]
    public void BorderWidth_zero_removes_the_frame()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.BorderWidth, "0"));

        Assert.Contains("--atom-card-border-width:0;", cut.Find(".atom-card-reveal").GetAttribute("style"));
    }

    [Fact]
    public void BorderColor_is_independent_of_AccentColor()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p
            .Add(x => x.AccentColor, "#186218")
            .Add(x => x.BorderColor, "#222"));

        // The whole point of splitting these: the face stays accent-colored while the frame doesn't.
        var style = cut.Find(".atom-card-reveal").GetAttribute("style");
        Assert.Contains("--atom-card-accent:#186218;", style);
        Assert.Contains("--atom-card-border-color:#222;", style);
    }

    [Fact]
    public void Default_direction_is_left()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>();

        Assert.Contains("atom-card-reveal-left", cut.Find(".atom-card-reveal").ClassList);
    }

    [Theory]
    [InlineData(CardRevealDirection.Left, "atom-card-reveal-left")]
    [InlineData(CardRevealDirection.Right, "atom-card-reveal-right")]
    [InlineData(CardRevealDirection.Up, "atom-card-reveal-up")]
    [InlineData(CardRevealDirection.Down, "atom-card-reveal-down")]
    public void Direction_selects_matching_axis_class(CardRevealDirection direction, string expectedClass)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.Direction, direction));

        Assert.Contains(expectedClass, cut.Find(".atom-card-reveal").ClassList);
    }

    [Fact]
    public void Shared_card_vars_use_the_family_prefix_not_a_per_component_one()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.AccentColor, "#186218"));

        // Params inherited from AtomCardBase emit --atom-card-*, so every card in the family
        // exposes the same name for the same concept; only effect-specific props carry a
        // per-component prefix (--atom-card-reveal-body-size).
        var style = cut.Find(".atom-card-reveal").GetAttribute("style");
        Assert.Contains("--atom-card-accent:#186218;", style);
        Assert.DoesNotContain("--atom-card-reveal-accent", style);
    }

    [Fact]
    public void Default_RevealSize_is_a_card_relative_percentage()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>();

        // Must stay a percentage (or other card-relative length), never a viewport unit: the
        // original port hardcoded 60vmin, which broke the hover reveal whenever the card was
        // narrower than 60vmin (overlay translated clean off the left edge, image fully hidden).
        Assert.Contains("--atom-card-reveal-body-size:70%;", cut.Find(".atom-card-reveal").GetAttribute("style"));
    }

    [Fact]
    public void RevealSize_flows_into_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.RevealSize, "400px"));

        Assert.Contains("--atom-card-reveal-body-size:400px;", cut.Find(".atom-card-reveal").GetAttribute("style"));
    }

    [Fact]
    public void Overlay_content_is_a_sibling_of_the_overlay_not_a_child()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p.Add(x => x.Title, "Trees"));

        // Title/subtitle must sit OUTSIDE the sliding overlay so they stay parked in the left
        // sliver without a counter-translate — a percentage translate would resolve against the
        // element's own width, so it could never track the card-relative reveal width.
        Assert.Empty(cut.FindAll(".atom-card-reveal-overlay .atom-card-reveal-overlay-content"));
        Assert.Single(cut.FindAll(".atom-card-reveal > .atom-card-reveal-overlay-content"));
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardReveal>(p => p
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "opacity:.9;"));

        var root = cut.Find(".atom-card-reveal");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("opacity:.9;", root.GetAttribute("style"));
    }
}
