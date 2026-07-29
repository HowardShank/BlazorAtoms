using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Buttons.Tests;

public class AtomToggleButtonTests : BunitContext
{
    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_through_AtomButton_and_reports_its_pressed_state()
    {
        // aria-pressed (not a checkbox role) is the correct contract for a toolbar toggle.
        var cut = Render<AtomToggleButton>(p => p.Add(c => c.Text, "Bold"));

        var button = cut.Find("button");
        Assert.Contains("atom-button", button.GetAttribute("class"));
        Assert.Contains("atom-toggle-button", button.GetAttribute("class"));
        Assert.Equal("false", button.GetAttribute("aria-pressed"));
        Assert.Equal("false", button.GetAttribute("data-pressed"));
    }

    [Fact]
    public void Value_true_renders_pressed()
    {
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.Text, "Bold")
            .Add(c => c.Value, true));

        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.Equal("true", cut.Find("button").GetAttribute("data-pressed"));
    }

    // ---- value flow --------------------------------------------------------------------------

    [Fact]
    public void Clicking_flips_the_value_and_raises_ValueChanged()
    {
        var changes = new List<bool>();
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.Text, "Bold")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<bool>(this, v => changes.Add(v))));

        cut.Find("button").Click();
        cut.Find("button").Click();

        Assert.Equal(new[] { true, false }, changes);
        Assert.False(cut.Instance.Value);
    }

    [Fact]
    public void OnClick_runs_after_the_state_flip()
    {
        // A handler that reads Value must see the new state, not the old one.
        bool? seen = null;
        var cut = Render<AtomToggleButton>(p => p.Add(c => c.Text, "Pin"));
        cut.Render(p => p
            .Add(c => c.Text, "Pin")
            .Add(c => c.OnClick, EventCallback.Factory.Create<MouseEventArgs>(
                this, () => seen = cut.Instance.Value)));

        cut.Find("button").Click();

        Assert.True(seen);
    }

    [Fact]
    public void Blocked_toggle_does_not_flip()
    {
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.Text, "Bold")
            .Add(c => c.Disabled, true));

        cut.Find("button").Click();

        Assert.False(cut.Instance.Value);
    }

    // ---- state-dependent content -------------------------------------------------------------

    [Fact]
    public void PressedText_replaces_the_label_while_on()
    {
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.Text, "Follow")
            .Add(c => c.PressedText, "Following"));

        Assert.Equal("Follow", cut.Find("button").TextContent.Trim());

        cut.Find("button").Click();

        Assert.Equal("Following", cut.Find("button").TextContent.Trim());
    }

    [Fact]
    public void Without_PressedText_the_label_is_unchanged_by_the_state()
    {
        var cut = Render<AtomToggleButton>(p => p.Add(c => c.Text, "Mute"));

        cut.Find("button").Click();

        Assert.Equal("Mute", cut.Find("button").TextContent.Trim());
    }

    [Fact]
    public void PressedContent_replaces_ChildContent_while_on()
    {
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<span>off</span>"))
            .Add(c => c.PressedContent, b => b.AddMarkupContent(0, "<span>on</span>")));

        Assert.Equal("off", cut.Find("button").TextContent.Trim());

        cut.Find("button").Click();

        Assert.Equal("on", cut.Find("button").TextContent.Trim());
    }

    [Fact]
    public void Icon_and_IconOnly_reach_the_inner_button()
    {
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.IconOnly, true)
            .Add(c => c.AriaLabel, "Bold")
            .Add(c => c.Icon, b => b.AddMarkupContent(0, "<svg data-icon=\"bold\"></svg>")));

        Assert.Equal("true", cut.Find("button").GetAttribute("data-icon-only"));
        Assert.NotNull(cut.Find("svg[data-icon=bold]"));
    }

    // ---- forwarding --------------------------------------------------------------------------

    [Fact]
    public void Forwards_the_axes_and_theming()
    {
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.Text, "Bold")
            .Add(c => c.Variant, ButtonVariant.Info)
            .Add(c => c.Appearance, ButtonAppearance.Ghost)
            .Add(c => c.Size, ButtonSize.Small)
            .Add(c => c.Shape, ButtonShape.Pill)
            .Add(c => c.Effect, ButtonEffect.Bevel)
            .Add(c => c.Background, "olive"));

        var button = cut.Find("button");
        Assert.Equal("info", button.GetAttribute("data-variant"));
        Assert.Equal("ghost", button.GetAttribute("data-appearance"));
        Assert.Equal("small", button.GetAttribute("data-size"));
        Assert.Equal("pill", button.GetAttribute("data-shape"));
        Assert.Equal("bevel", button.GetAttribute("data-effect"));
        Assert.Contains("--btn-accent:olive;", button.GetAttribute("style")!);
    }

    [Fact]
    public void Never_renders_an_anchor()
    {
        // Href is inherited from the family base but deliberately not forwarded: a link that holds a
        // toggle state is a contradiction.
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.Text, "Bold")
            .Add(c => c.Href, "/somewhere"));

        Assert.Empty(cut.FindAll("a"));
        Assert.NotNull(cut.Find("button"));
    }

    [Fact]
    public void Loading_shows_the_spinner_and_blocks_the_flip()
    {
        var cut = Render<AtomToggleButton>(p => p
            .Add(c => c.Text, "Bold")
            .Add(c => c.Loading, true));

        Assert.NotNull(cut.Find(".atom-button-spinner"));

        cut.Find("button").Click();
        Assert.False(cut.Instance.Value);
    }
}
