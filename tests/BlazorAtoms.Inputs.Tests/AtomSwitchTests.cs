using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomSwitchTests : BunitContext
{
    private sealed class TestModel
    {
        public bool Notify { get; set; }
    }

    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_native_checkbox_with_switch_role_plus_track_and_thumb()
    {
        var cut = Render<AtomSwitch>();

        var input = cut.Find("input.atom-switch-input");
        Assert.Equal("checkbox", input.GetAttribute("type"));
        Assert.Equal("switch", input.GetAttribute("role"));
        Assert.NotNull(cut.Find(".atom-switch-track"));
        Assert.NotNull(cut.Find(".atom-switch-thumb"));
    }

    [Fact]
    public void Value_true_renders_checked()
    {
        Assert.NotNull(Render<AtomSwitch>(p => p.Add(c => c.Value, true)).Find("input").GetAttribute("checked"));
        Assert.Null(Render<AtomSwitch>(p => p.Add(c => c.Value, false)).Find("input").GetAttribute("checked"));
    }

    [Fact]
    public void Text_renders_beside_the_track_with_a_placement_attribute()
    {
        var cut = Render<AtomSwitch>(p => p
            .Add(c => c.Text, "Email me")
            .Add(c => c.TextPlacement, LabelPlacement.Start));

        Assert.Equal("Email me", cut.Find(".atom-switch-text").TextContent);
        Assert.Equal("start", cut.Find(".atom-switch-wrap").GetAttribute("data-placement"));
    }

    [Fact]
    public void OnText_and_OffText_render_inside_the_track()
    {
        var cut = Render<AtomSwitch>(p => p
            .Add(c => c.OnText, "ON")
            .Add(c => c.OffText, "OFF"));

        Assert.Equal("ON", cut.Find(".atom-switch-state-on").TextContent);
        Assert.Equal("OFF", cut.Find(".atom-switch-state-off").TextContent);
    }

    [Fact]
    public void State_labels_are_absent_when_neither_is_set()
    {
        // Both spans are emitted or neither is — CSS shows whichever matches the current state, so a
        // half-configured pair would leave a stray empty span in the track.
        Assert.Empty(Render<AtomSwitch>().FindAll(".atom-switch-state"));
    }

    [Fact]
    public void ThumbContent_renders_inside_the_thumb()
    {
        var cut = Render<AtomSwitch>(p => p
            .Add(c => c.ThumbContent, b => b.AddMarkupContent(0, "<i>✓</i>")));

        Assert.Equal("✓", cut.Find(".atom-switch-thumb").TextContent);
    }

    [Fact]
    public void TrackWidth_and_TrackHeight_emit_track_vars()
    {
        var cut = Render<AtomSwitch>(p => p
            .Add(c => c.TrackWidth, 60d)
            .Add(c => c.TrackHeight, 28d));

        var style = cut.Find(".atom-switch").GetAttribute("style")!;
        Assert.Contains("--field-track-width:60px;", style);
        Assert.Contains("--field-track-height:28px;", style);
    }

    // ---- value flow --------------------------------------------------------------------------

    [Fact]
    public void Change_commits_both_directions()
    {
        var changes = new List<bool>();
        var cut = Render<AtomSwitch>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<bool>(this, v => changes.Add(v))));

        cut.Find("input").Change(true);
        cut.Find("input").Change(false);

        Assert.Equal(new[] { true, false }, changes);
    }

    // ---- state -------------------------------------------------------------------------------

    [Fact]
    public void ReadOnly_falls_back_to_native_disabled_but_keeps_its_own_data_state()
    {
        var cut = Render<AtomSwitch>(p => p.Add(c => c.ReadOnly, true));

        var input = cut.Find("input");
        Assert.NotNull(input.GetAttribute("disabled"));
        Assert.Null(input.GetAttribute("readonly"));
        Assert.Equal("readonly", cut.Find(".atom-switch").GetAttribute("data-state"));
    }

    [Fact]
    public void Disabled_blocks_commits()
    {
        var cut = Render<AtomSwitch>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.Disabled, true));

        cut.Find("input").Change(true);

        Assert.False(cut.Instance.Value);
    }

    [Fact]
    public void Visible_false_hides_via_display_none()
    {
        var cut = Render<AtomSwitch>(p => p.Add(c => c.Visible, false));
        Assert.Contains("display:none", cut.Find(".atom-switch").GetAttribute("style")!);
    }

    [Fact]
    public void AriaLabel_falls_back_to_label_then_to_a_default()
    {
        Assert.Equal("Switch", Render<AtomSwitch>().Find("input").GetAttribute("aria-label"));

        Assert.Equal("Notifications", Render<AtomSwitch>(p => p.Add(c => c.Label, "Notifications"))
            .Find("input").GetAttribute("aria-label"));
    }

    // ---- styling axes ------------------------------------------------------------------------

    [Fact]
    public void Variant_size_and_effect_emit_their_data_attributes()
    {
        var cut = Render<AtomSwitch>(p => p
            .Add(c => c.Variant, InputVariant.Underline)
            .Add(c => c.Size, InputSize.Small)
            .Add(c => c.Effect, InputEffect.FocusRaise));

        var root = cut.Find(".atom-switch");
        Assert.Equal("underline", root.GetAttribute("data-variant"));
        Assert.Equal("small", root.GetAttribute("data-size"));
        Assert.Equal("focus-raise", root.GetAttribute("data-effect"));
    }

    // ---- EditContext / validation ------------------------------------------------------------

    [Fact]
    public void Error_state_sets_aria_invalid_and_replaces_help_text()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Notify)), "Pick one");
        editContext.NotifyValidationStateChanged();

        var cut = Render<AtomSwitch>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Notify)
            .Add(c => c.HelpText, "Optional")
            .Add(c => c.ValidationFor, () => model.Notify));

        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Equal("error", cut.Find(".atom-switch").GetAttribute("data-state"));
        Assert.Equal("Pick one", cut.Find(".atom-switch-subtext").TextContent);
    }

    [Fact]
    public void Committing_a_value_notifies_the_EditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var notified = false;
        editContext.OnFieldChanged += (_, _) => notified = true;

        var cut = Render<AtomSwitch>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Notify)
            .Add(c => c.ValidationFor, () => model.Notify));

        cut.Find("input").Change(true);

        Assert.True(notified);
    }
}
