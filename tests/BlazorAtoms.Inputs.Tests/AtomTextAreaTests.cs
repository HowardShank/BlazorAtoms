using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomTextAreaTests : BunitContext
{
    private sealed class TestModel
    {
        [StringLength(10, ErrorMessage = "Too long")]
        public string? Notes { get; set; }
    }

    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_root_box_and_native_textarea_with_default_rows()
    {
        var cut = Render<AtomTextArea>();

        Assert.NotNull(cut.Find(".atom-text-area-box"));
        Assert.Equal("4", cut.Find("textarea.atom-text-area-field").GetAttribute("rows"));
    }

    [Fact]
    public void Value_renders_as_the_value_attribute_not_child_content()
    {
        // Blazor diffs a textarea through its `value` attribute (same as its own InputTextArea);
        // seeding child content instead would leave later updates unapplied.
        var cut = Render<AtomTextArea>(p => p.Add(c => c.Value, "line one"));

        var textarea = cut.Find("textarea");
        Assert.Equal("line one", textarea.GetAttribute("value"));
        Assert.Equal("", textarea.TextContent);
    }

    [Fact]
    public void Rows_placeholder_and_maxlength_reach_the_textarea()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Rows, 8)
            .Add(c => c.Placeholder, "Notes…")
            .Add(c => c.MaxLength, 200));

        var textarea = cut.Find("textarea");
        Assert.Equal("8", textarea.GetAttribute("rows"));
        Assert.Equal("Notes…", textarea.GetAttribute("placeholder"));
        Assert.Equal("200", textarea.GetAttribute("maxlength"));
    }

    [Fact]
    public void Height_emits_field_height_var_and_overrides_rows_visually()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Rows, 4)
            .Add(c => c.Height, 180d));

        Assert.Contains("--field-height:180px;", cut.Find(".atom-text-area").GetAttribute("style")!);
    }

    [Theory]
    [InlineData(TextAreaResize.None, "none")]
    [InlineData(TextAreaResize.Vertical, "vertical")]
    [InlineData(TextAreaResize.Horizontal, "horizontal")]
    [InlineData(TextAreaResize.Both, "both")]
    public void Resize_emits_data_resize_on_the_textarea(TextAreaResize resize, string expected)
    {
        var cut = Render<AtomTextArea>(p => p.Add(c => c.Resize, resize));
        Assert.Equal(expected, cut.Find("textarea").GetAttribute("data-resize"));
    }

    [Fact]
    public void Footer_is_absent_when_there_is_no_subtext_and_no_counter()
    {
        Assert.Empty(Render<AtomTextArea>().FindAll(".atom-text-area-footer"));
    }

    // ---- counter -----------------------------------------------------------------------------

    [Fact]
    public void Counter_shows_length_over_maxlength()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.MaxLength, 10)
            .Add(c => c.ShowCounter, true));

        Assert.Equal("3 / 10", cut.Find(".atom-text-area-counter").TextContent);
    }

    [Fact]
    public void Counter_shows_bare_length_without_a_maxlength()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, "abcd")
            .Add(c => c.ShowCounter, true));

        Assert.Equal("4", cut.Find(".atom-text-area-counter").TextContent);
        Assert.Null(cut.Find(".atom-text-area-counter").GetAttribute("data-state"));
    }

    [Theory]
    [InlineData(8, null)]     // 8/10 = 0.8, under the 0.9 default
    [InlineData(9, "near")]   // exactly at the threshold
    [InlineData(10, "near")]
    public void Counter_flags_near_state_at_the_warn_threshold(int length, string? expected)
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, new string('x', length))
            .Add(c => c.MaxLength, 10)
            .Add(c => c.ShowCounter, true));

        Assert.Equal(expected, cut.Find(".atom-text-area-counter").GetAttribute("data-state"));
    }

    [Fact]
    public void CounterWarnAt_moves_the_threshold()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, new string('x', 5))
            .Add(c => c.MaxLength, 10)
            .Add(c => c.CounterWarnAt, 0.5)
            .Add(c => c.ShowCounter, true));

        Assert.Equal("near", cut.Find(".atom-text-area-counter").GetAttribute("data-state"));
    }

    [Fact]
    public void Counter_is_absent_unless_asked_for()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.MaxLength, 10)
            .Add(c => c.HelpText, "help"));

        Assert.Empty(cut.FindAll(".atom-text-area-counter"));
    }

    // ---- value flow --------------------------------------------------------------------------

    [Fact]
    public void Input_event_commits_by_default()
    {
        string? changedTo = null;
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, "a")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => changedTo = v)));

        cut.Find("textarea").Input("ab");

        Assert.Equal("ab", changedTo);
        Assert.Equal("ab", cut.Instance.Value);
    }

    [Fact]
    public void UpdateOn_Change_ignores_input_and_commits_on_change()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, "a")
            .Add(c => c.UpdateOn, InputUpdateOn.Change));

        cut.Find("textarea").Input("typed");
        Assert.Equal("a", cut.Instance.Value);

        cut.Find("textarea").Change("committed");
        Assert.Equal("committed", cut.Instance.Value);
    }

    // ---- state -------------------------------------------------------------------------------

    [Fact]
    public void Disabled_renders_native_disabled_and_blocks_commits()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, "a")
            .Add(c => c.Disabled, true));

        Assert.NotNull(cut.Find("textarea").GetAttribute("disabled"));
        Assert.Equal("disabled", cut.Find(".atom-text-area").GetAttribute("data-state"));

        cut.Find("textarea").Input("nope");
        Assert.Equal("a", cut.Instance.Value);
    }

    [Fact]
    public void ReadOnly_renders_native_readonly_not_disabled()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Value, "a")
            .Add(c => c.ReadOnly, true));

        var textarea = cut.Find("textarea");
        Assert.NotNull(textarea.GetAttribute("readonly"));
        Assert.Null(textarea.GetAttribute("disabled"));
        Assert.Equal("readonly", cut.Find(".atom-text-area").GetAttribute("data-state"));
    }

    [Fact]
    public void Visible_false_hides_via_display_none_and_stays_in_the_dom()
    {
        var cut = Render<AtomTextArea>(p => p.Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-text-area").GetAttribute("style")!);
        Assert.NotNull(cut.Find("textarea"));
    }

    [Fact]
    public void AriaLabel_falls_back_to_label_then_to_a_default()
    {
        Assert.Equal("Text area", Render<AtomTextArea>().Find("textarea").GetAttribute("aria-label"));

        Assert.Equal("Notes", Render<AtomTextArea>(p => p.Add(c => c.Label, "Notes"))
            .Find("textarea").GetAttribute("aria-label"));
    }

    // ---- styling axes ------------------------------------------------------------------------

    [Fact]
    public void Variant_size_and_effect_emit_their_data_attributes()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Variant, InputVariant.Underline)
            .Add(c => c.Size, InputSize.Large)
            .Add(c => c.Effect, InputEffect.ShakeOnError));

        var root = cut.Find(".atom-text-area");
        Assert.Equal("underline", root.GetAttribute("data-variant"));
        Assert.Equal("large", root.GetAttribute("data-size"));
        Assert.Equal("shake-on-error", root.GetAttribute("data-effect"));
    }

    [Fact]
    public void Theming_parameters_and_height_share_one_style_attribute()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.Radius, 12d)
            .Add(c => c.Height, 90d)
            .Add(c => c.BorderColor, "#abc"));

        var style = cut.Find(".atom-text-area").GetAttribute("style")!;
        Assert.Contains("--field-radius:12px;", style);
        Assert.Contains("--field-border-color:#abc;", style);
        Assert.Contains("--field-height:90px;", style);
    }

    [Fact]
    public void CssClass_Style_and_splat_land_on_the_root()
    {
        var cut = Render<AtomTextArea>(p => p
            .Add(c => c.CssClass, "mine")
            .Add(c => c.Style, "margin:1rem;")
            .AddUnmatched("title", "hi"));

        var root = cut.Find(".atom-text-area");
        Assert.Equal("atom-text-area mine", root.GetAttribute("class"));
        Assert.Equal("hi", root.GetAttribute("title"));
        Assert.Equal("margin:1rem;", root.GetAttribute("style"));
    }

    // ---- EditContext / validation ------------------------------------------------------------

    [Fact]
    public void Error_state_sets_aria_invalid_data_state_and_replaces_help_text()
    {
        var model = new TestModel { Notes = "way too long to pass" };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Notes)), "Too long");
        editContext.NotifyValidationStateChanged();

        var cut = Render<AtomTextArea>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Notes)
            .Add(c => c.HelpText, "Optional notes")
            .Add(c => c.ValidationFor, () => model.Notes));

        Assert.Equal("true", cut.Find("textarea").GetAttribute("aria-invalid"));
        Assert.Equal("error", cut.Find(".atom-text-area").GetAttribute("data-state"));
        Assert.Equal("Too long", cut.Find(".atom-text-area-subtext").TextContent);
    }

    [Fact]
    public void Committing_a_value_notifies_the_EditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var notified = false;
        editContext.OnFieldChanged += (_, _) => notified = true;

        var cut = Render<AtomTextArea>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Notes)
            .Add(c => c.ValidationFor, () => model.Notes));

        cut.Find("textarea").Input("hi");

        Assert.True(notified);
    }
}
