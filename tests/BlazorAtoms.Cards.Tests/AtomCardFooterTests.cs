using Bunit;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCardFooter"/>. Purely declarative — no JS interop.</summary>
public class AtomCardFooterTests
{
    [Fact]
    public void Renders_its_content()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFooter>(p => p.AddChildContent("<button>Save</button>"));

        Assert.Contains("Save", cut.Find(".atom-card-footer").TextContent);
    }

    [Theory]
    [InlineData(CardSectionAlign.Start, "start")]
    [InlineData(CardSectionAlign.Center, "center")]
    [InlineData(CardSectionAlign.End, "end")]
    [InlineData(CardSectionAlign.Between, "between")]
    public void Align_is_emitted(CardSectionAlign align, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFooter>(p => p.Add(x => x.Align, align));

        Assert.Equal(expected, cut.Find(".atom-card-footer").GetAttribute("data-align"));
    }

    [Fact]
    public void Default_align_is_start()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFooter>();

        Assert.Equal("start", cut.Find(".atom-card-footer").GetAttribute("data-align"));
    }

    [Fact]
    public void Divider_is_on_by_default_and_off_when_asked()
    {
        using var ctx = new BunitContext();

        var on = ctx.Render<AtomCardFooter>();
        Assert.Equal("true", on.Find(".atom-card-footer").GetAttribute("data-divider"));

        var off = ctx.Render<AtomCardFooter>(p => p.Add(x => x.Divider, false));
        Assert.Null(off.Find(".atom-card-footer").GetAttribute("data-divider"));
    }

    [Fact]
    public void Section_styling_parameters_become_custom_properties()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFooter>(p => p
            .Add(x => x.Padding, 10d)
            .Add(x => x.BackgroundColor, "#fafafa"));

        var style = cut.Find(".atom-card-footer").GetAttribute("style") ?? "";
        Assert.Contains("--card-section-padding:10px", style);
        Assert.Contains("--card-section-bg:#fafafa", style);
    }

    [Fact]
    public void Works_standalone_outside_a_card()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardFooter>(p => p.AddChildContent("x"));

        Assert.Null(cut.Find(".atom-card-footer").GetAttribute("style"));
        Assert.Equal("true", cut.Find(".atom-card-footer").GetAttribute("data-divider"));
    }
}
