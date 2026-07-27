using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCardCurl"/>. Purely declarative — no JS interop.</summary>
public class AtomCardCurlTests
{
    [Fact]
    public void Renders_sheet_body_and_fold()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardCurl>(p => p
            .Add(x => x.Title, "Trees")
            .Add(x => x.BodyContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>woody plants</p>"))));

        Assert.NotNull(cut.Find(".atom-card-curl-sheet"));
        Assert.NotNull(cut.Find(".atom-card-curl-fold"));
        Assert.Contains("woody plants", cut.Find(".atom-card-curl-body").InnerHtml);
    }

    [Fact]
    public void Fold_is_decorative_and_hidden_from_assistive_tech()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardCurl>();

        Assert.Equal("true", cut.Find(".atom-card-curl-fold").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Default_corner_is_bottom_right()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardCurl>();

        Assert.Contains("atom-card-curl-bottomright", cut.Find(".atom-card-curl").ClassList);
    }

    [Theory]
    [InlineData(CardCurlCorner.BottomRight, "atom-card-curl-bottomright")]
    [InlineData(CardCurlCorner.BottomLeft, "atom-card-curl-bottomleft")]
    [InlineData(CardCurlCorner.TopRight, "atom-card-curl-topright")]
    [InlineData(CardCurlCorner.TopLeft, "atom-card-curl-topleft")]
    public void Corner_selects_matching_class(CardCurlCorner corner, string expectedClass)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardCurl>(p => p.Add(x => x.Corner, corner));

        Assert.Contains(expectedClass, cut.Find(".atom-card-curl").ClassList);
    }

    [Fact]
    public void Curl_params_flow_into_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardCurl>(p => p
            .Add(x => x.CurlSize, "50%")
            .Add(x => x.RestingCurlSize, "1rem")
            .Add(x => x.FoldColor, "#ddd")
            .Add(x => x.BodyColor, "#fafafa"));

        var style = cut.Find(".atom-card-curl").GetAttribute("style");
        Assert.Contains("--atom-card-curl-size:50%;", style);
        Assert.Contains("--atom-card-curl-resting-size:1rem;", style);
        Assert.Contains("--atom-card-curl-fold-color:#ddd;", style);
        Assert.Contains("--atom-card-curl-body-color:#fafafa;", style);
    }

    [Fact]
    public void Inherits_shared_card_params_from_the_base()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardCurl>(p => p.Add(x => x.AccentColor, "#186218"));

        Assert.Contains("--atom-card-accent:#186218;", cut.Find(".atom-card-curl").GetAttribute("style"));
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardCurl>(p => p
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "opacity:.9;"));

        var root = cut.Find(".atom-card-curl");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("opacity:.9;", root.GetAttribute("style"));
    }
}
