using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomTextFieldTests : BunitContext
{
    private sealed class TestModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string? Name { get; set; }
    }

    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_root_box_and_native_input()
    {
        var cut = Render<AtomTextField>();

        Assert.NotNull(cut.Find(".atom-text-field"));
        Assert.NotNull(cut.Find(".atom-text-field-box"));
        Assert.Equal("text", cut.Find("input.atom-text-field-field").GetAttribute("type"));
    }

    [Fact]
    public void Label_renders_in_label_column_and_required_adds_asterisk()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Label, "Name")
            .Add(c => c.LabelCol, "clr-col-4")
            .Add(c => c.Required, true));

        var label = cut.Find("label.atom-text-field-label");
        Assert.Contains("clr-col-4", label.GetAttribute("class"));
        Assert.Contains("Name", label.TextContent);
        Assert.NotNull(cut.Find(".atom-text-field-required"));
        Assert.NotNull(cut.Find("input").GetAttribute("required"));
    }

    [Fact]
    public void No_label_renders_no_label_element()
    {
        var cut = Render<AtomTextField>();
        Assert.Empty(cut.FindAll("label"));
    }

    [Fact]
    public void HelpText_renders_as_subtext()
    {
        var cut = Render<AtomTextField>(p => p.Add(c => c.HelpText, "Your full name"));
        Assert.Equal("Your full name", cut.Find(".atom-text-field-subtext").TextContent);
    }

    [Theory]
    [InlineData(TextFieldType.Text, "text")]
    [InlineData(TextFieldType.Email, "email")]
    [InlineData(TextFieldType.Url, "url")]
    [InlineData(TextFieldType.Tel, "tel")]
    [InlineData(TextFieldType.Search, "search")]
    [InlineData(TextFieldType.Password, "password")]
    public void Type_maps_to_native_input_type(TextFieldType type, string expected)
    {
        var cut = Render<AtomTextField>(p => p.Add(c => c.Type, type));
        Assert.Equal(expected, cut.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void Placeholder_maxlength_autocomplete_inputmode_reach_the_input()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Placeholder, "you@example.com")
            .Add(c => c.MaxLength, 40)
            .Add(c => c.Autocomplete, "email")
            .Add(c => c.InputMode, "email"));

        var input = cut.Find("input");
        Assert.Equal("you@example.com", input.GetAttribute("placeholder"));
        Assert.Equal("40", input.GetAttribute("maxlength"));
        Assert.Equal("email", input.GetAttribute("autocomplete"));
        Assert.Equal("email", input.GetAttribute("inputmode"));
    }

    [Fact]
    public void Unset_optional_attributes_are_omitted_entirely()
    {
        // A rendered-but-empty maxlength/autocomplete would change browser behavior, so null must
        // mean "no attribute" rather than "empty attribute".
        var input = Render<AtomTextField>().Find("input");

        Assert.Null(input.GetAttribute("maxlength"));
        Assert.Null(input.GetAttribute("autocomplete"));
        Assert.Null(input.GetAttribute("inputmode"));
        Assert.Null(input.GetAttribute("spellcheck"));
        Assert.Null(input.GetAttribute("placeholder"));
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Spellcheck_renders_explicit_string(bool value, string expected)
    {
        var cut = Render<AtomTextField>(p => p.Add(c => c.Spellcheck, value));
        Assert.Equal(expected, cut.Find("input").GetAttribute("spellcheck"));
    }

    [Fact]
    public void Prefix_and_suffix_render_in_their_own_slots()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.PrefixContent, b => b.AddMarkupContent(0, "<i>$</i>"))
            .Add(c => c.SuffixContent, b => b.AddMarkupContent(0, "<i>kg</i>")));

        Assert.Equal("$", cut.Find(".atom-text-field-prefix").TextContent);
        Assert.Equal("kg", cut.Find(".atom-text-field-suffix").TextContent);
    }

    // ---- value flow --------------------------------------------------------------------------

    [Fact]
    public void Value_renders_and_input_event_commits_by_default()
    {
        string? changedTo = null;
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => changedTo = v)));

        Assert.Equal("abc", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Input("abcd");

        Assert.Equal("abcd", changedTo);
        Assert.Equal("abcd", cut.Instance.Value);
    }

    [Fact]
    public void UpdateOn_Change_ignores_input_and_commits_on_change()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.UpdateOn, InputUpdateOn.Change));

        cut.Find("input").Input("typed");
        Assert.Equal("abc", cut.Instance.Value);

        cut.Find("input").Change("committed");
        Assert.Equal("committed", cut.Instance.Value);
    }

    [Fact]
    public void Unchanged_value_does_not_raise_ValueChanged()
    {
        var raised = 0;
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Value, "same")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => raised++)));

        cut.Find("input").Input("same");

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Clearable_shows_button_only_with_a_value_and_clearing_commits_null()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Clearable, true)
            .Add(c => c.Value, "abc"));

        cut.Find(".atom-text-field-clear").Click();

        Assert.Null(cut.Instance.Value);
        Assert.Empty(cut.FindAll(".atom-text-field-clear"));
    }

    [Fact]
    public void Clear_button_is_hidden_when_input_is_blocked()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Clearable, true)
            .Add(c => c.Value, "abc")
            .Add(c => c.ReadOnly, true));

        Assert.Empty(cut.FindAll(".atom-text-field-clear"));
    }

    // ---- state -------------------------------------------------------------------------------

    [Fact]
    public void Disabled_renders_native_disabled_and_blocks_commits()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.Disabled, true));

        var input = cut.Find("input");
        Assert.NotNull(input.GetAttribute("disabled"));
        Assert.Equal("true", input.GetAttribute("aria-disabled"));
        Assert.Equal("disabled", cut.Find(".atom-text-field").GetAttribute("data-state"));

        input.Input("nope");
        Assert.Equal("abc", cut.Instance.Value);
    }

    [Fact]
    public void ReadOnly_renders_native_readonly_not_disabled_and_still_blocks_commits()
    {
        // Unlike AtomRangeInput (where the platform ignores `readonly` on a range), a text input has
        // a real read-only state: focusable and submitted, but not editable.
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.ReadOnly, true));

        var input = cut.Find("input");
        Assert.NotNull(input.GetAttribute("readonly"));
        Assert.Null(input.GetAttribute("disabled"));
        Assert.Equal("readonly", cut.Find(".atom-text-field").GetAttribute("data-state"));

        input.Input("nope");
        Assert.Equal("abc", cut.Instance.Value);
    }

    [Fact]
    public void Visible_false_hides_via_display_none_and_stays_in_the_dom()
    {
        var cut = Render<AtomTextField>(p => p.Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-text-field").GetAttribute("style")!);
        Assert.NotNull(cut.Find("input"));
    }

    [Fact]
    public void AriaLabel_falls_back_to_label_then_to_a_default()
    {
        Assert.Equal("Text field", Render<AtomTextField>().Find("input").GetAttribute("aria-label"));

        Assert.Equal("Name", Render<AtomTextField>(p => p.Add(c => c.Label, "Name"))
            .Find("input").GetAttribute("aria-label"));

        Assert.Equal("Explicit", Render<AtomTextField>(p => p
            .Add(c => c.Label, "Name")
            .Add(c => c.AriaLabel, "Explicit")).Find("input").GetAttribute("aria-label"));
    }

    // ---- styling axes ------------------------------------------------------------------------

    [Theory]
    [InlineData(InputVariant.Outline, "outline")]
    [InlineData(InputVariant.Filled, "filled")]
    [InlineData(InputVariant.Underline, "underline")]
    public void Variant_emits_data_variant(InputVariant variant, string expected)
    {
        var cut = Render<AtomTextField>(p => p.Add(c => c.Variant, variant));
        Assert.Equal(expected, cut.Find(".atom-text-field").GetAttribute("data-variant"));
    }

    [Theory]
    [InlineData(InputSize.Small, "small")]
    [InlineData(InputSize.Medium, "medium")]
    [InlineData(InputSize.Large, "large")]
    public void Size_emits_data_size(InputSize size, string expected)
    {
        var cut = Render<AtomTextField>(p => p.Add(c => c.Size, size));
        Assert.Equal(expected, cut.Find(".atom-text-field").GetAttribute("data-size"));
    }

    [Theory]
    [InlineData(InputEffect.FocusGlow, "focus-glow")]
    [InlineData(InputEffect.FocusRaise, "focus-raise")]
    [InlineData(InputEffect.FocusUnderline, "focus-underline")]
    [InlineData(InputEffect.ShakeOnError, "shake-on-error")]
    public void Effect_emits_kebab_data_effect(InputEffect effect, string expected)
    {
        var cut = Render<AtomTextField>(p => p.Add(c => c.Effect, effect));
        Assert.Equal(expected, cut.Find(".atom-text-field").GetAttribute("data-effect"));
    }

    [Fact]
    public void Effect_None_emits_no_data_effect_attribute()
    {
        // The default must cost nothing in the DOM — CSS keys every effect off the attribute's
        // presence, so an empty data-effect="" would be a live selector target.
        var cut = Render<AtomTextField>();
        Assert.Null(cut.Find(".atom-text-field").GetAttribute("data-effect"));
    }

    [Fact]
    public void Theming_parameters_emit_field_custom_properties()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.Width, 320d)
            .Add(c => c.FontSize, 15d)
            .Add(c => c.Radius, 10d)
            .Add(c => c.BorderWidth, 2d)
            .Add(c => c.TextColor, "#111")
            .Add(c => c.BackgroundColor, "#eee")
            .Add(c => c.BorderColor, "#999")
            .Add(c => c.AccentColor, "rebeccapurple")
            .Add(c => c.FocusColor, "teal")
            .Add(c => c.ErrorColor, "crimson"));

        var style = cut.Find(".atom-text-field").GetAttribute("style")!;
        Assert.Contains("--field-width:320px;", style);
        Assert.Contains("--field-font-size:15px;", style);
        Assert.Contains("--field-radius:10px;", style);
        Assert.Contains("--field-border-width:2px;", style);
        Assert.Contains("--field-text-color:#111;", style);
        Assert.Contains("--field-bg:#eee;", style);
        Assert.Contains("--field-border-color:#999;", style);
        Assert.Contains("--field-accent:rebeccapurple;", style);
        Assert.Contains("--field-focus-color:teal;", style);
        Assert.Contains("--field-error-color:crimson;", style);
    }

    [Fact]
    public void Unset_theming_parameters_emit_no_style_attribute()
    {
        Assert.Null(Render<AtomTextField>().Find(".atom-text-field").GetAttribute("style"));
    }

    [Fact]
    public void CssClass_Style_and_splat_land_on_the_root()
    {
        var cut = Render<AtomTextField>(p => p
            .Add(c => c.CssClass, "mine")
            .Add(c => c.Style, "margin:1rem;")
            .Add(c => c.Radius, 4d)
            .AddUnmatched("title", "hi"));

        var root = cut.Find(".atom-text-field");
        Assert.Equal("atom-text-field mine", root.GetAttribute("class"));
        Assert.Equal("hi", root.GetAttribute("title"));
        // Caller Style is appended last so it wins over the component's own custom properties.
        Assert.EndsWith("margin:1rem;", root.GetAttribute("style"));
    }

    // ---- EditContext / validation ------------------------------------------------------------

    [Fact]
    public void Error_state_sets_aria_invalid_data_state_and_replaces_help_text()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Name)), "Name is required");
        editContext.NotifyValidationStateChanged();

        var cut = Render<AtomTextField>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Name)
            .Add(c => c.HelpText, "Your full name")
            .Add(c => c.ValidationFor, () => model.Name));

        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Equal("error", cut.Find(".atom-text-field").GetAttribute("data-state"));
        Assert.Equal("Name is required", cut.Find(".atom-text-field-subtext").TextContent);
    }

    [Fact]
    public void No_error_leaves_aria_invalid_and_data_state_off()
    {
        var model = new TestModel { Name = "Ada" };
        var editContext = new EditContext(model);

        var cut = Render<AtomTextField>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Name)
            .Add(c => c.ValidationFor, () => model.Name));

        Assert.Null(cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Null(cut.Find(".atom-text-field").GetAttribute("data-state"));
    }

    [Fact]
    public void Committing_a_value_notifies_the_EditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var notified = false;
        editContext.OnFieldChanged += (_, _) => notified = true;

        var cut = Render<AtomTextField>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Name)
            .Add(c => c.ValidationFor, () => model.Name));

        cut.Find("input").Input("Ada");

        Assert.True(notified);
    }

    [Fact]
    public async Task Validation_state_change_rerenders_the_field()
    {
        // The component subscribes to OnValidationStateChanged; without that, a validation pass
        // triggered elsewhere (a form submit) would leave this field showing stale help text.
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);

        var cut = Render<AtomTextField>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Name)
            .Add(c => c.HelpText, "Your full name")
            .Add(c => c.ValidationFor, () => model.Name));

        Assert.Null(cut.Find(".atom-text-field").GetAttribute("data-state"));

        // Raised on the renderer's dispatcher, which is where a real EditForm/validator raises it —
        // the handler calls StateHasChanged (same as Blazor's own InputBase), so an off-thread
        // notification would throw rather than re-render.
        await cut.InvokeAsync(() =>
        {
            messages.Add(editContext.Field(nameof(TestModel.Name)), "Name is required");
            editContext.NotifyValidationStateChanged();
        });

        Assert.Equal("error", cut.Find(".atom-text-field").GetAttribute("data-state"));
    }

    [Fact]
    public void Works_with_no_EditContext_at_all()
    {
        // Every field must be usable as a plain bound control outside an EditForm.
        var cut = Render<AtomTextField>(p => p.Add(c => c.HelpText, "help"));

        cut.Find("input").Input("free");

        Assert.Equal("free", cut.Instance.Value);
        Assert.Equal("help", cut.Find(".atom-text-field-subtext").TextContent);
    }
}
