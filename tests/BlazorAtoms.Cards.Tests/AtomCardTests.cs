using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace BlazorAtoms.Cards.Tests;

/// <summary>bUnit coverage for <see cref="AtomCard"/>. Purely declarative — no JS interop. The
/// interesting parts are the root element following the semantics (div/a/button), the slot-vs-nested
/// section equivalence, and the cascade to sections.</summary>
public class AtomCardTests
{
    private static RenderFragment Markup(string html) => b => b.AddMarkupContent(0, html);

    [Fact]
    public void Plain_card_is_a_div_with_no_interactive_marker()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p.AddChildContent("<p>hello</p>"));

        var root = cut.Find(".atom-card");
        Assert.Equal("DIV", root.TagName);
        Assert.Null(root.GetAttribute("data-interactive"));
        Assert.Contains("hello", cut.Markup);
    }

    [Fact]
    public void Href_renders_a_real_anchor_and_never_claims_role_button()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Href, "/docs")
            .Add(x => x.Target, "_blank"));

        var root = cut.Find(".atom-card");
        Assert.Equal("A", root.TagName);
        Assert.Equal("/docs", root.GetAttribute("href"));
        Assert.Equal("_blank", root.GetAttribute("target"));
        // It navigates, so role="button" would be a lie.
        Assert.Null(root.GetAttribute("role"));
        Assert.Equal("true", root.GetAttribute("data-interactive"));
    }

    [Fact]
    public void OnClick_alone_renders_a_button_so_the_keyboard_works()
    {
        using var ctx = new BunitContext();
        var clicks = 0;

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.OnClick, () => clicks++));

        var root = cut.Find(".atom-card");
        Assert.Equal("BUTTON", root.TagName);
        Assert.Equal("button", root.GetAttribute("type"));

        root.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Href_wins_over_OnClick_for_the_root_element()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Href, "/docs")
            .Add(x => x.OnClick, () => { }));

        Assert.Equal("A", cut.Find(".atom-card").TagName);
    }

    [Fact]
    public void AriaLabel_names_a_clickable_card()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Href, "/docs")
            .Add(x => x.AriaLabel, "Read the docs"));

        Assert.Equal("Read the docs", cut.Find(".atom-card").GetAttribute("aria-label"));
    }

    [Fact]
    public void Slots_render_the_matching_section_components()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Header, Markup("<span>head</span>"))
            .Add(x => x.Body, Markup("<span>body</span>"))
            .Add(x => x.Footer, Markup("<span>foot</span>")));

        Assert.Contains("head", cut.Find(".atom-card-header").TextContent);
        Assert.Contains("body", cut.Find(".atom-card-body").TextContent);
        Assert.Contains("foot", cut.Find(".atom-card-footer").TextContent);
    }

    [Fact]
    public void Nested_sections_work_the_same_as_slots()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p.AddChildContent<AtomCardBody>(b => b
            .AddChildContent("<span>body</span>")));

        Assert.Contains("body", cut.Find(".atom-card-body").TextContent);
    }

    [Fact]
    public void Sections_inherit_the_cards_padding_and_divider_defaults()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Padding, 24d)
            .Add(x => x.Divider, false)
            .Add(x => x.Header, Markup("h"))
            .Add(x => x.Footer, Markup("f")));

        Assert.Contains("--card-section-padding:24px", cut.Find(".atom-card-header").GetAttribute("style"));
        Assert.Null(cut.Find(".atom-card-header").GetAttribute("data-divider"));
        Assert.Null(cut.Find(".atom-card-footer").GetAttribute("data-divider"));
    }

    [Fact]
    public void A_sections_own_parameter_beats_the_cascade()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Padding, 24d)
            .AddChildContent<AtomCardBody>(b => b.Add(x => x.Padding, 4d)));

        Assert.Contains("--card-section-padding:4px", cut.Find(".atom-card-body").GetAttribute("style"));
    }

    [Fact]
    public void Cascaded_values_track_a_parameter_change()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Padding, 8d)
            .Add(x => x.Body, Markup("b")));
        Assert.Contains("--card-section-padding:8px", cut.Find(".atom-card-body").GetAttribute("style"));

        // The context is rebuilt each render, so a new Padding actually reaches the section.
        cut.Render(p => p
            .Add(x => x.Padding, 32d)
            .Add(x => x.Body, Markup("b")));
        Assert.Contains("--card-section-padding:32px", cut.Find(".atom-card-body").GetAttribute("style"));
    }

    [Fact]
    public void No_media_element_and_no_media_attribute_without_a_media_slot()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p.Add(x => x.MediaPosition, CardMediaPosition.Start));

        Assert.Empty(cut.FindAll(".atom-card-media"));
        // The layout rules key off presence, not just the enum, so the attribute stays absent.
        Assert.Null(cut.Find(".atom-card").GetAttribute("data-media"));
    }

    [Theory]
    [InlineData(CardMediaPosition.Top, "top")]
    [InlineData(CardMediaPosition.Bottom, "bottom")]
    [InlineData(CardMediaPosition.Start, "start")]
    [InlineData(CardMediaPosition.End, "end")]
    public void MediaPosition_is_emitted_when_media_is_present(CardMediaPosition position, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Media, Markup("<img src='x.png' alt='' />"))
            .Add(x => x.MediaPosition, position));

        Assert.Equal(expected, cut.Find(".atom-card").GetAttribute("data-media"));
        Assert.Single(cut.FindAll(".atom-card-media"));
    }

    [Theory]
    [InlineData(CardMediaPosition.Top, true)]
    [InlineData(CardMediaPosition.Start, true)]
    [InlineData(CardMediaPosition.Bottom, false)]
    [InlineData(CardMediaPosition.End, false)]
    public void Media_is_rendered_before_or_after_the_sections_in_dom_order(CardMediaPosition position, bool first)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Media, Markup("<img src='x.png' alt='' />"))
            .Add(x => x.MediaPosition, position)
            .Add(x => x.Body, Markup("body")));

        // Source order matches the visual order, so the CSS needs no `order` property.
        var markup = cut.Markup;
        var mediaAt = markup.IndexOf("atom-card-media", StringComparison.Ordinal);
        var sectionsAt = markup.IndexOf("atom-card-sections", StringComparison.Ordinal);
        Assert.Equal(first, mediaAt < sectionsAt);
    }

    [Fact]
    public void MediaSize_becomes_the_media_column_token()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Media, Markup("<img src='x.png' alt='' />"))
            .Add(x => x.MediaPosition, CardMediaPosition.Start)
            .Add(x => x.MediaSize, "12rem"));

        Assert.Contains("--card-media-size:12rem", cut.Find(".atom-card").GetAttribute("style"));
    }

    [Theory]
    [InlineData(CardVariant.Elevated, "elevated")]
    [InlineData(CardVariant.Outlined, "outlined")]
    [InlineData(CardVariant.Filled, "filled")]
    [InlineData(CardVariant.Flat, "flat")]
    public void Variant_is_emitted(CardVariant variant, string expected)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p.Add(x => x.Variant, variant));

        Assert.Equal(expected, cut.Find(".atom-card").GetAttribute("data-variant"));
    }

    [Fact]
    public void Elevation_defaults_to_medium_and_is_emitted()
    {
        using var ctx = new BunitContext();

        var dflt = ctx.Render<AtomCard>();
        Assert.Equal("medium", dflt.Find(".atom-card").GetAttribute("data-elevation"));

        var large = ctx.Render<AtomCard>(p => p.Add(x => x.Elevation, CardElevation.Large));
        Assert.Equal("large", large.Find(".atom-card").GetAttribute("data-elevation"));
    }

    [Fact]
    public void Default_effect_emits_no_attribute_and_multiword_effects_are_kebab_cased()
    {
        using var ctx = new BunitContext();

        var none = ctx.Render<AtomCard>();
        Assert.Null(none.Find(".atom-card").GetAttribute("data-effect"));

        var lift = ctx.Render<AtomCard>(p => p.Add(x => x.Effect, CardEffect.HoverLift));
        Assert.Equal("hover-lift", lift.Find(".atom-card").GetAttribute("data-effect"));
    }

    [Fact]
    public void Theming_parameters_become_custom_properties_and_sizes()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.Radius, 0d)
            .Add(x => x.BorderWidth, 2d)
            .Add(x => x.BackgroundColor, "#111")
            .Add(x => x.AccentColor, "#0f0")
            .Add(x => x.Duration, 0.5)
            .Add(x => x.Width, "22rem")
            .Add(x => x.Height, "300px"));

        var style = cut.Find(".atom-card").GetAttribute("style") ?? "";
        Assert.Contains("--card-radius:0px", style);
        Assert.Contains("--card-border-width:2px", style);
        Assert.Contains("--card-bg:#111", style);
        Assert.Contains("--card-accent:#0f0", style);
        // Invariant culture: "0,5s" would be an invalid declaration.
        Assert.Contains("--card-duration:0.5s", style);
        Assert.Contains("width:22rem", style);
        Assert.Contains("height:300px", style);
    }

    [Fact]
    public void Unset_theming_parameters_emit_nothing()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>();

        var style = cut.Find(".atom-card").GetAttribute("style");
        Assert.True(string.IsNullOrEmpty(style));
    }

    [Fact]
    public void Visible_false_hides_without_leaving_the_dom()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p.Add(x => x.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-card").GetAttribute("style"));
    }

    [Fact]
    public void CssClass_Style_and_splatted_attributes_layer_onto_the_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomCard>(p => p
            .Add(x => x.CssClass, "mine")
            .Add(x => x.Style, "opacity:.5")
            .Add(x => x.Radius, 4d)
            .AddUnmatched("data-testid", "card-1"));

        var root = cut.Find(".atom-card");
        Assert.Contains("mine", root.GetAttribute("class"));
        Assert.Equal("card-1", root.GetAttribute("data-testid"));
        var style = root.GetAttribute("style") ?? "";
        Assert.True(style.IndexOf("opacity:.5") > style.IndexOf("--card-radius"));
    }
}
