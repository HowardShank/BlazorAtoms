using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Buttons.Tests;

public class AtomButtonTests : BunitContext
{
    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_a_native_button_of_type_button_by_default()
    {
        // Not HTML's `submit` default: a button dropped inside an EditForm must not submit by accident.
        var cut = Render<AtomButton>(p => p.Add(c => c.Text, "Save"));

        var button = cut.Find("button.atom-button");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Save", button.TextContent.Trim());
    }

    [Theory]
    [InlineData(ButtonType.Button, "button")]
    [InlineData(ButtonType.Submit, "submit")]
    [InlineData(ButtonType.Reset, "reset")]
    public void Type_maps_to_the_native_attribute(ButtonType type, string expected)
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Type, type));
        Assert.Equal(expected, cut.Find("button").GetAttribute("type"));
    }

    [Fact]
    public void ChildContent_wins_over_Text()
    {
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Text, "ignored")
            .AddChildContent("<span>markup</span>"));

        Assert.Equal("markup", cut.Find("button").TextContent.Trim());
    }

    [Fact]
    public void Start_and_end_icons_render_in_their_own_slots()
    {
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Text, "Next")
            .Add(c => c.StartIcon, b => b.AddMarkupContent(0, "<i>L</i>"))
            .Add(c => c.EndIcon, b => b.AddMarkupContent(0, "<i>R</i>")));

        Assert.Equal("L", cut.Find(".atom-button-icon-start").TextContent);
        Assert.Equal("R", cut.Find(".atom-button-icon-end").TextContent);
    }

    [Fact]
    public void Href_switches_to_an_anchor_and_keeps_link_semantics()
    {
        // A real <a> so middle-click / open-in-new-tab / copy-link work. role="button" would misreport
        // it, so it must NOT be present.
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Text, "Docs")
            .Add(c => c.Href, "/docs")
            .Add(c => c.Target, "_blank"));

        var anchor = cut.Find("a.atom-button");
        Assert.Equal("/docs", anchor.GetAttribute("href"));
        Assert.Equal("_blank", anchor.GetAttribute("target"));
        Assert.Null(anchor.GetAttribute("role"));
        Assert.Empty(cut.FindAll("button"));
    }

    // ---- click -------------------------------------------------------------------------------

    [Fact]
    public void OnClick_fires()
    {
        var clicks = 0;
        var cut = Render<AtomButton>(p => p
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Disabled_renders_native_disabled_and_swallows_the_click()
    {
        var clicks = 0;
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        var button = cut.Find("button");
        Assert.NotNull(button.GetAttribute("disabled"));
        Assert.Equal("disabled", button.GetAttribute("data-state"));

        // bUnit dispatches to the handler regardless of the attribute (a real browser wouldn't), so
        // this asserts the C# guard rather than the browser's behavior.
        button.Click();
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void Loading_shows_a_spinner_reports_busy_and_swallows_the_click()
    {
        var clicks = 0;
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Loading, true)
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        var button = cut.Find("button");
        Assert.NotNull(cut.Find(".atom-button-spinner"));
        Assert.Equal("true", button.GetAttribute("aria-busy"));
        Assert.Equal("loading", button.GetAttribute("data-state"));
        // The label stays in the DOM — CSS hides it with visibility so the box keeps its width.
        Assert.NotNull(cut.Find(".atom-button-content"));

        button.Click();
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void Blocked_link_drops_its_href_and_leaves_the_tab_order()
    {
        // There is no native disabled state for an anchor, so blocking has to remove the navigation.
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Href, "/docs")
            .Add(c => c.Disabled, true));

        var anchor = cut.Find("a");
        Assert.Null(anchor.GetAttribute("href"));
        Assert.Equal("-1", anchor.GetAttribute("tabindex"));
        Assert.Equal("true", anchor.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Disabled_wins_over_loading_in_data_state()
    {
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Loading, true));

        Assert.Equal("disabled", cut.Find("button").GetAttribute("data-state"));
    }

    [Fact]
    public void Normal_state_emits_no_data_state()
    {
        Assert.Null(Render<AtomButton>().Find("button").GetAttribute("data-state"));
    }

    // ---- styling axes ------------------------------------------------------------------------

    [Theory]
    [InlineData(ButtonVariant.Default, "default")]
    [InlineData(ButtonVariant.Primary, "primary")]
    [InlineData(ButtonVariant.Info, "info")]
    [InlineData(ButtonVariant.Success, "success")]
    [InlineData(ButtonVariant.Warning, "warning")]
    [InlineData(ButtonVariant.Danger, "danger")]
    public void Variant_emits_data_variant(ButtonVariant variant, string expected)
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Variant, variant));
        Assert.Equal(expected, cut.Find("button").GetAttribute("data-variant"));
    }

    [Theory]
    [InlineData(ButtonAppearance.Solid, "solid")]
    [InlineData(ButtonAppearance.Soft, "soft")]
    [InlineData(ButtonAppearance.Outline, "outline")]
    [InlineData(ButtonAppearance.Ghost, "ghost")]
    [InlineData(ButtonAppearance.Link, "link")]
    public void Appearance_emits_data_appearance(ButtonAppearance appearance, string expected)
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Appearance, appearance));
        Assert.Equal(expected, cut.Find("button").GetAttribute("data-appearance"));
    }

    [Theory]
    [InlineData(ButtonSize.Small, "small")]
    [InlineData(ButtonSize.Medium, "medium")]
    [InlineData(ButtonSize.Large, "large")]
    public void Size_emits_data_size(ButtonSize size, string expected)
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Size, size));
        Assert.Equal(expected, cut.Find("button").GetAttribute("data-size"));
    }

    [Theory]
    [InlineData(ButtonShape.Rounded, "rounded")]
    [InlineData(ButtonShape.Square, "square")]
    [InlineData(ButtonShape.Pill, "pill")]
    [InlineData(ButtonShape.Circle, "circle")]
    public void Shape_emits_data_shape(ButtonShape shape, string expected)
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Shape, shape));
        Assert.Equal(expected, cut.Find("button").GetAttribute("data-shape"));
    }

    [Theory]
    [InlineData(ButtonEffect.Press3d, "press-3d")]
    [InlineData(ButtonEffect.Bevel, "bevel")]
    [InlineData(ButtonEffect.GradientBorder, "gradient-border")]
    [InlineData(ButtonEffect.Rainbow, "rainbow")]
    [InlineData(ButtonEffect.Fizzy, "fizzy")]
    [InlineData(ButtonEffect.Storm, "storm")]
    [InlineData(ButtonEffect.ClickRipple, "click-ripple")]
    public void Effect_emits_kebab_data_effect(ButtonEffect effect, string expected)
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Effect, effect));
        Assert.Equal(expected, cut.Find("button").GetAttribute("data-effect"));
    }

    [Fact]
    public void Effect_None_emits_no_data_effect()
    {
        // CSS keys every effect off the attribute's presence, so an empty data-effect="" would be a
        // live selector target.
        Assert.Null(Render<AtomButton>().Find("button").GetAttribute("data-effect"));
    }

    [Fact]
    public void FullWidth_and_IconOnly_only_emit_when_set()
    {
        var plain = Render<AtomButton>().Find("button");
        Assert.Null(plain.GetAttribute("data-full-width"));
        Assert.Null(plain.GetAttribute("data-icon-only"));

        var flagged = Render<AtomButton>(p => p
            .Add(c => c.FullWidth, true)
            .Add(c => c.IconOnly, true)).Find("button");
        Assert.Equal("true", flagged.GetAttribute("data-full-width"));
        Assert.Equal("true", flagged.GetAttribute("data-icon-only"));
    }

    // ---- ripple ------------------------------------------------------------------------------

    [Fact]
    public void Ripple_span_is_absent_until_a_click()
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Effect, ButtonEffect.ClickRipple));
        Assert.Empty(cut.FindAll(".atom-button-ripple"));
    }

    [Fact]
    public void Ripple_renders_at_the_click_coordinates()
    {
        // Origin comes from MouseEventArgs.Offset*, which is why the ripple needs no JS measurement.
        var cut = Render<AtomButton>(p => p.Add(c => c.Effect, ButtonEffect.ClickRipple));

        cut.Find("button").Click(new MouseEventArgs { OffsetX = 42.5, OffsetY = 12 });

        var style = cut.Find(".atom-button-ripple").GetAttribute("style")!;
        Assert.Contains("--btn-ripple-x:42.5px;", style);
        Assert.Contains("--btn-ripple-y:12px;", style);
    }

    [Fact]
    public void Ripple_is_not_rendered_for_other_effects()
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Effect, ButtonEffect.Rainbow));

        cut.Find("button").Click(new MouseEventArgs { OffsetX = 5, OffsetY = 5 });

        Assert.Empty(cut.FindAll(".atom-button-ripple"));
    }

    [Fact]
    public void Blocked_click_leaves_no_ripple()
    {
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Effect, ButtonEffect.ClickRipple)
            .Add(c => c.Loading, true));

        cut.Find("button").Click(new MouseEventArgs { OffsetX = 5, OffsetY = 5 });

        Assert.Empty(cut.FindAll(".atom-button-ripple"));
    }

    // ---- theming -----------------------------------------------------------------------------

    [Fact]
    public void Theming_parameters_emit_btn_custom_properties()
    {
        var cut = Render<AtomButton>(p => p
            .Add(c => c.Background, "rebeccapurple")
            .Add(c => c.TextColor, "#fff")
            .Add(c => c.BorderColor, "#222")
            .Add(c => c.BorderWidth, 3d)
            .Add(c => c.Radius, 12d)
            .Add(c => c.Height, 44d)
            .Add(c => c.MinWidth, 120d)
            .Add(c => c.FontSize, 15d)
            .Add(c => c.FontFamily, "Inter")
            .Add(c => c.FontWeight, "700")
            .Add(c => c.LetterSpacing, ".05em")
            .Add(c => c.TextTransform, "uppercase"));

        var style = cut.Find("button").GetAttribute("style")!;
        Assert.Contains("--btn-accent:rebeccapurple;", style);
        Assert.Contains("--btn-color:#fff;", style);
        Assert.Contains("--btn-border-color:#222;", style);
        Assert.Contains("--btn-border-width:3px;", style);
        Assert.Contains("--btn-radius:12px;", style);
        Assert.Contains("--btn-height:44px;", style);
        Assert.Contains("--btn-min-width:120px;", style);
        Assert.Contains("--btn-font-size:15px;", style);
        Assert.Contains("--btn-font-family:Inter;", style);
        Assert.Contains("--btn-font-weight:700;", style);
        Assert.Contains("--btn-letter-spacing:.05em;", style);
        Assert.Contains("--btn-text-transform:uppercase;", style);
    }

    [Fact]
    public void Unset_theming_parameters_emit_no_style_attribute()
    {
        Assert.Null(Render<AtomButton>().Find("button").GetAttribute("style"));
    }

    [Fact]
    public void Visible_false_hides_via_display_none_and_stays_in_the_dom()
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find("button").GetAttribute("style")!);
        Assert.NotNull(cut.Find("button"));
    }

    [Fact]
    public void CssClass_Style_and_splat_land_on_the_root()
    {
        var cut = Render<AtomButton>(p => p
            .Add(c => c.CssClass, "mine")
            .Add(c => c.Style, "margin:1rem;")
            .Add(c => c.Radius, 4d)
            .AddUnmatched("title", "hi"));

        var button = cut.Find("button");
        Assert.Equal("atom-button mine", button.GetAttribute("class"));
        Assert.Equal("hi", button.GetAttribute("title"));
        // Caller Style is appended last so it wins over the component's own custom properties.
        Assert.EndsWith("margin:1rem;", button.GetAttribute("style"));
    }

    [Fact]
    public void AriaLabel_reaches_the_element_and_Pressed_is_absent_by_default()
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.AriaLabel, "Save document"));

        var button = cut.Find("button");
        Assert.Equal("Save document", button.GetAttribute("aria-label"));
        // A plain button must not claim toggle semantics.
        Assert.Null(button.GetAttribute("aria-pressed"));
        Assert.Null(button.GetAttribute("data-pressed"));
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Pressed_emits_both_aria_pressed_and_data_pressed(bool pressed, string expected)
    {
        var cut = Render<AtomButton>(p => p.Add(c => c.Pressed, pressed));

        var button = cut.Find("button");
        Assert.Equal(expected, button.GetAttribute("aria-pressed"));
        Assert.Equal(expected, button.GetAttribute("data-pressed"));
    }

    // ---- the Kebab helper --------------------------------------------------------------------

    [Theory]
    [InlineData("Solid", "solid")]
    [InlineData("GradientBorder", "gradient-border")]
    [InlineData("ClickRipple", "click-ripple")]
    // A digit run opens a word, but letters after it stay attached — press-3d, not press3d or press-3-d.
    [InlineData("Press3d", "press-3d")]
    [InlineData("Flip20Deep", "flip-20-deep")]
    public void Kebab_lowercases_and_hyphenates_word_boundaries(string input, string expected)
    {
        Assert.Equal(expected, ButtonFamilyBase.Kebab(input));
    }
}
