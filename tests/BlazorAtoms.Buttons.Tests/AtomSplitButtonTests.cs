using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Buttons.Tests;

public class AtomSplitButtonTests : BunitContext
{
    private static readonly RenderFragment Menu = b => b.AddMarkupContent(0,
        "<button type=\"button\" role=\"menuitem\" data-item=\"save-as\">Save as…</button>");

    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_an_action_button_plus_a_native_details_menu()
    {
        // <details> is what supplies open/close state, keyboard activation, and the expanded
        // announcement without any JS or C# state.
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.MenuContent, Menu));

        Assert.NotNull(cut.Find(".atom-split-button"));
        Assert.NotNull(cut.Find("button.atom-split-button-action"));
        Assert.NotNull(cut.Find("details.atom-split-button-menu"));
        Assert.NotNull(cut.Find("summary.atom-split-button-toggle"));
        Assert.Equal("Save", cut.Find(".atom-split-button-action").TextContent.Trim());
    }

    [Fact]
    public void Menu_starts_closed()
    {
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.MenuContent, Menu));

        Assert.Null(cut.Find("details").GetAttribute("open"));
    }

    [Fact]
    public void MenuContent_renders_inside_a_menu_role_panel()
    {
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.MenuContent, Menu));

        var panel = cut.Find(".atom-split-button-panel");
        Assert.Equal("menu", panel.GetAttribute("role"));
        Assert.NotNull(cut.Find("[data-item=save-as]"));
    }

    [Fact]
    public void Toggle_half_carries_an_accessible_name_with_a_sensible_default()
    {
        Assert.Equal("More actions", Render<AtomSplitButton>(p => p.Add(c => c.Text, "Save"))
            .Find("summary").GetAttribute("aria-label"));

        Assert.Equal("Save options", Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.ToggleAriaLabel, "Save options"))
            .Find("summary").GetAttribute("aria-label"));
    }

    [Fact]
    public void Icon_and_ChildContent_land_in_the_action_half()
    {
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Icon, b => b.AddMarkupContent(0, "<svg data-icon=\"disk\"></svg>"))
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<em>Publish</em>")));

        var action = cut.Find(".atom-split-button-action");
        Assert.Contains("Publish", action.TextContent);
        Assert.NotNull(cut.Find("svg[data-icon=disk]"));
    }

    [Fact]
    public void Renders_without_menu_content_rather_than_throwing()
    {
        var cut = Render<AtomSplitButton>(p => p.Add(c => c.Text, "Save"));

        Assert.NotNull(cut.Find("details"));
        Assert.Equal("", cut.Find(".atom-split-button-panel").TextContent.Trim());
    }

    // ---- click -------------------------------------------------------------------------------

    [Fact]
    public void OnClick_fires_from_the_action_half_only()
    {
        var clicks = 0;
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.MenuContent, Menu)
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        cut.Find(".atom-split-button-action").Click();
        Assert.Equal(1, clicks);

        // The menu half must not be able to run the primary action. It carries no Blazor handler at
        // all — opening is the platform's job — and bUnit reports exactly that, which is the strongest
        // form of this assertion: there is nothing wired to reach OnClick.
        Assert.Throws<MissingEventHandlerException>(() => cut.Find("summary").Click());
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Href_makes_the_action_half_an_anchor_and_leaves_the_menu_alone()
    {
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Docs")
            .Add(c => c.Href, "/docs")
            .Add(c => c.MenuContent, Menu));

        Assert.Equal("/docs", cut.Find("a.atom-split-button-action").GetAttribute("href"));
        Assert.NotNull(cut.Find("summary"));
    }

    // ---- state -------------------------------------------------------------------------------

    [Fact]
    public void Disabled_blocks_both_halves()
    {
        // <details> has no disabled attribute, so the arrow is blocked the way a disabled link is:
        // out of the tab order plus aria-disabled, with pointer-events off in CSS.
        var clicks = 0;
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Disabled, true)
            .Add(c => c.MenuContent, Menu)
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++)));

        Assert.Equal("disabled", cut.Find(".atom-split-button").GetAttribute("data-state"));
        Assert.NotNull(cut.Find(".atom-split-button-action").GetAttribute("disabled"));

        var summary = cut.Find("summary");
        Assert.Equal("true", summary.GetAttribute("aria-disabled"));
        Assert.Equal("-1", summary.GetAttribute("tabindex"));

        cut.Find(".atom-split-button-action").Click();
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void Loading_flags_the_root_and_shows_the_spinner_in_the_action_half()
    {
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Loading, true));

        Assert.Equal("loading", cut.Find(".atom-split-button").GetAttribute("data-state"));
        Assert.NotNull(cut.Find(".atom-button-spinner"));
    }

    [Fact]
    public void Visible_false_hides_the_whole_control()
    {
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-split-button").GetAttribute("style")!);
    }

    // ---- styling axes ------------------------------------------------------------------------

    [Fact]
    public void Axes_land_on_the_root_and_are_forwarded_to_the_action_half()
    {
        // The root needs them because the <summary> can't be an AtomButton — it matches the look from
        // the root's own data-* rules and the inherited --btn-* tokens.
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Variant, ButtonVariant.Success)
            .Add(c => c.Appearance, ButtonAppearance.Outline)
            .Add(c => c.Size, ButtonSize.Large)
            .Add(c => c.Shape, ButtonShape.Pill));

        var root = cut.Find(".atom-split-button");
        Assert.Equal("success", root.GetAttribute("data-variant"));
        Assert.Equal("outline", root.GetAttribute("data-appearance"));
        Assert.Equal("large", root.GetAttribute("data-size"));
        Assert.Equal("pill", root.GetAttribute("data-shape"));

        var action = cut.Find(".atom-split-button-action");
        Assert.Equal("success", action.GetAttribute("data-variant"));
        Assert.Equal("outline", action.GetAttribute("data-appearance"));
    }

    [Theory]
    [InlineData(SplitMenuAlign.Start, "start")]
    [InlineData(SplitMenuAlign.End, "end")]
    public void MenuAlign_emits_data_menu_align(SplitMenuAlign align, string expected)
    {
        var cut = Render<AtomSplitButton>(p => p.Add(c => c.MenuAlign, align));
        Assert.Equal(expected, cut.Find(".atom-split-button").GetAttribute("data-menu-align"));
    }

    [Fact]
    public void MenuWidth_rides_in_on_the_shared_style_attribute()
    {
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.MenuWidth, 240d)
            .Add(c => c.Radius, 10d));

        var style = cut.Find(".atom-split-button").GetAttribute("style")!;
        Assert.Contains("--btn-menu-width:240px;", style);
        Assert.Contains("--btn-radius:10px;", style);
    }

    [Fact]
    public void Splat_goes_to_the_action_half_not_the_wrapper()
    {
        // A caller's title/data-* belongs on the thing being clicked.
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .AddUnmatched("title", "Save the document"));

        Assert.Equal("Save the document", cut.Find(".atom-split-button-action").GetAttribute("title"));
        Assert.Null(cut.Find(".atom-split-button").GetAttribute("title"));
    }

    [Fact]
    public void CssClass_and_Style_land_on_the_wrapper()
    {
        var cut = Render<AtomSplitButton>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.CssClass, "mine")
            .Add(c => c.Style, "margin:1rem;"));

        var root = cut.Find(".atom-split-button");
        Assert.Equal("atom-split-button mine", root.GetAttribute("class"));
        Assert.EndsWith("margin:1rem;", root.GetAttribute("style"));
    }
}
