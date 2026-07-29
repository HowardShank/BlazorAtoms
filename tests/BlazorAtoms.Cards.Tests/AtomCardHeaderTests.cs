using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCardHeader"/>. Purely declarative — no JS interop.</summary>
public class AtomCardHeaderTests
{
    private static RenderFragment Markup(string html) => b => b.AddMarkupContent(0, html);

    [Fact]
    public void Title_renders_as_an_h3_by_default()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardHeader>(p => p.Add(x => x.Title, "Weather"));

        var title = cut.Find(".atom-card-header-title");
        Assert.Equal("H3", title.TagName);
        Assert.Equal("Weather", title.TextContent);
    }

    [Theory]
    [InlineData(1, "H1")]
    [InlineData(2, "H2")]
    [InlineData(6, "H6")]
    public void HeadingLevel_picks_a_real_heading_element(int level, string expected)
    {
        using var ctx = new BunitContext();

        // A real <h*> rather than role="heading" — cards sit at different depths on different pages.
        var cut = ctx.Render<AtomCardHeader>(p => p
            .Add(x => x.Title, "Weather")
            .Add(x => x.HeadingLevel, level));

        Assert.Equal(expected, cut.Find(".atom-card-header-title").TagName);
    }

    [Theory]
    [InlineData(0, "H1")]
    [InlineData(99, "H6")]
    public void Out_of_range_heading_levels_are_clamped_to_valid_elements(int level, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardHeader>(p => p
            .Add(x => x.Title, "Weather")
            .Add(x => x.HeadingLevel, level));

        Assert.Equal(expected, cut.Find(".atom-card-header-title").TagName);
    }

    [Fact]
    public void No_title_element_when_no_title_is_supplied()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardHeader>();

        Assert.Empty(cut.FindAll(".atom-card-header-title"));
    }

    [Fact]
    public void Subtitle_supports_markup()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardHeader>(p => p
            .Add(x => x.Title, "Weather")
            .Add(x => x.Subtitle, Markup("Updated <em>now</em>")));

        Assert.Contains("<em>now</em>", cut.Find(".atom-card-header-subtitle").InnerHtml);
    }

    [Fact]
    public void ChildContent_replaces_Title_and_Subtitle_entirely()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardHeader>(p => p
            .Add(x => x.Title, "Weather")
            .Add(x => x.Subtitle, Markup("sub"))
            .AddChildContent("<span>custom</span>"));

        Assert.Contains("custom", cut.Find(".atom-card-header-text").TextContent);
        Assert.Empty(cut.FindAll(".atom-card-header-title"));
        Assert.Empty(cut.FindAll(".atom-card-header-subtitle"));
    }

    [Fact]
    public void Avatar_and_Actions_slots_render_only_when_supplied()
    {
        using var ctx = new BunitContext();

        var bare = ctx.Render<AtomCardHeader>(p => p.Add(x => x.Title, "Weather"));
        Assert.Empty(bare.FindAll(".atom-card-header-avatar"));
        Assert.Empty(bare.FindAll(".atom-card-header-actions"));

        var full = ctx.Render<AtomCardHeader>(p => p
            .Add(x => x.Title, "Weather")
            .Add(x => x.Avatar, Markup("<img src='a.png' alt='' />"))
            .Add(x => x.Actions, Markup("<button>x</button>")));
        Assert.Single(full.FindAll(".atom-card-header-avatar"));
        Assert.Single(full.FindAll(".atom-card-header-actions"));
    }

    [Fact]
    public void Divider_is_on_by_default_and_off_when_asked()
    {
        using var ctx = new BunitContext();

        var on = ctx.Render<AtomCardHeader>();
        Assert.Equal("true", on.Find(".atom-card-header").GetAttribute("data-divider"));

        var off = ctx.Render<AtomCardHeader>(p => p.Add(x => x.Divider, false));
        Assert.Null(off.Find(".atom-card-header").GetAttribute("data-divider"));
    }

    [Fact]
    public void Works_standalone_outside_a_card()
    {
        using var ctx = new BunitContext();

        // No cascaded CardContext — the CSS defaults apply and nothing throws.
        var cut = ctx.Render<AtomCardHeader>(p => p.Add(x => x.Title, "Weather"));

        Assert.Null(cut.Find(".atom-card-header").GetAttribute("style"));
    }

    [Fact]
    public void Padding_and_BackgroundColor_become_section_custom_properties()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCardHeader>(p => p
            .Add(x => x.Padding, 12d)
            .Add(x => x.BackgroundColor, "#f5f5f5"));

        var style = cut.Find(".atom-card-header").GetAttribute("style") ?? "";
        Assert.Contains("--card-section-padding:12px", style);
        Assert.Contains("--card-section-bg:#f5f5f5", style);
    }
}
