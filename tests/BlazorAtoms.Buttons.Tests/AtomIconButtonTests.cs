using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Buttons.Tests;

public class AtomIconButtonTests : BunitContext
{
    private static readonly RenderFragment Gear = b => b.AddMarkupContent(0,
        "<svg viewBox=\"0 0 24 24\" data-icon=\"gear\"><circle cx=\"12\" cy=\"12\" r=\"4\" /></svg>");

    [Fact]
    public void Renders_through_AtomButton_flagged_icon_only()
    {
        // The whole point of wrapping AtomButton is inheriting its stylesheet, which keys off the
        // .atom-button class and data-icon-only.
        var cut = Render<AtomIconButton>(p => p
            .Add(c => c.Icon, Gear)
            .Add(c => c.AriaLabel, "Settings"));

        var button = cut.Find("button");
        Assert.Contains("atom-button", button.GetAttribute("class"));
        Assert.Contains("atom-icon-button", button.GetAttribute("class"));
        Assert.Equal("true", button.GetAttribute("data-icon-only"));
        Assert.NotNull(cut.Find("svg[data-icon=gear]"));
        Assert.Equal("Settings", button.GetAttribute("aria-label"));
    }

    [Fact]
    public void Defaults_to_a_circle()
    {
        Assert.Equal("circle", Render<AtomIconButton>(p => p.Add(c => c.Icon, Gear))
            .Find("button").GetAttribute("data-shape"));
    }

    [Fact]
    public void Explicit_shape_overrides_the_circle_default()
    {
        var cut = Render<AtomIconButton>(p => p
            .Add(c => c.Icon, Gear)
            .Add(c => c.Shape, ButtonShape.Square));

        Assert.Equal("square", cut.Find("button").GetAttribute("data-shape"));
    }

    [Fact]
    public void Renders_without_an_icon_rather_than_throwing()
    {
        // A caller may be waiting on data for the glyph; an empty button beats a crash.
        var cut = Render<AtomIconButton>(p => p.Add(c => c.AriaLabel, "Settings"));

        Assert.NotNull(cut.Find("button"));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Fact]
    public void Forwards_the_axes_state_and_theming_to_the_inner_button()
    {
        var cut = Render<AtomIconButton>(p => p
            .Add(c => c.Icon, Gear)
            .Add(c => c.Variant, ButtonVariant.Danger)
            .Add(c => c.Appearance, ButtonAppearance.Outline)
            .Add(c => c.Size, ButtonSize.Large)
            .Add(c => c.Effect, ButtonEffect.ClickRipple)
            .Add(c => c.Background, "teal")
            .Add(c => c.Radius, 9d));

        var button = cut.Find("button");
        Assert.Equal("danger", button.GetAttribute("data-variant"));
        Assert.Equal("outline", button.GetAttribute("data-appearance"));
        Assert.Equal("large", button.GetAttribute("data-size"));
        Assert.Equal("click-ripple", button.GetAttribute("data-effect"));

        var style = button.GetAttribute("style")!;
        Assert.Contains("--btn-accent:teal;", style);
        Assert.Contains("--btn-radius:9px;", style);
    }

    [Fact]
    public void Forwards_the_click_and_honors_blocking()
    {
        var clicks = 0;
        var cut = Render<AtomIconButton>(p => p
            .Add(c => c.Icon, Gear)
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        cut.Find("button").Click();
        Assert.Equal(1, clicks);

        var blocked = Render<AtomIconButton>(p => p
            .Add(c => c.Icon, Gear)
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        blocked.Find("button").Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Href_still_produces_an_anchor()
    {
        var cut = Render<AtomIconButton>(p => p
            .Add(c => c.Icon, Gear)
            .Add(c => c.Href, "/settings"));

        Assert.Equal("/settings", cut.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void Caller_CssClass_is_appended_after_the_component_classes()
    {
        var cut = Render<AtomIconButton>(p => p
            .Add(c => c.Icon, Gear)
            .Add(c => c.CssClass, "mine"));

        Assert.Equal("atom-button atom-icon-button mine", cut.Find("button").GetAttribute("class"));
    }

    [Fact]
    public void Splat_and_Style_reach_the_rendered_element()
    {
        var cut = Render<AtomIconButton>(p => p
            .Add(c => c.Icon, Gear)
            .Add(c => c.Style, "margin:2px;")
            .AddUnmatched("title", "Settings"));

        var button = cut.Find("button");
        Assert.Equal("Settings", button.GetAttribute("title"));
        Assert.EndsWith("margin:2px;", button.GetAttribute("style"));
    }
}
