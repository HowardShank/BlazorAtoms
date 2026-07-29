using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomCheckboxTests : BunitContext
{
    private sealed class TestModel
    {
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept")]
        public bool Accepted { get; set; }
    }

    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_native_checkbox_plus_a_painted_box()
    {
        // The native input carries semantics (focus, tab order, submission) and the span carries the
        // pixels — losing either half breaks a11y or styling.
        var cut = Render<AtomCheckbox>();

        Assert.Equal("checkbox", cut.Find("input.atom-checkbox-input").GetAttribute("type"));
        Assert.NotNull(cut.Find(".atom-checkbox-box"));
        Assert.NotNull(cut.Find(".atom-checkbox-check"));
    }

    [Fact]
    public void Native_input_is_nested_in_the_wrap_label_for_implicit_association()
    {
        // No generated id/for pair: an id minted per instance would differ between the prerender and
        // interactive passes.
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.Text, "Accept"));

        var wrap = cut.Find("label.atom-checkbox-wrap");
        Assert.Contains("atom-checkbox-input", wrap.InnerHtml);
        Assert.Null(cut.Find("input").GetAttribute("id"));
    }

    [Fact]
    public void Value_true_renders_checked()
    {
        Assert.NotNull(Render<AtomCheckbox>(p => p.Add(c => c.Value, true)).Find("input").GetAttribute("checked"));
        Assert.Null(Render<AtomCheckbox>(p => p.Add(c => c.Value, false)).Find("input").GetAttribute("checked"));
    }

    [Fact]
    public void Text_renders_beside_the_box()
    {
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.Text, "Accept terms"));
        Assert.Equal("Accept terms", cut.Find(".atom-checkbox-text").TextContent);
    }

    [Fact]
    public void No_text_renders_no_text_span()
    {
        Assert.Empty(Render<AtomCheckbox>().FindAll(".atom-checkbox-text"));
    }

    [Theory]
    [InlineData(LabelPlacement.Start, "start")]
    [InlineData(LabelPlacement.End, "end")]
    public void TextPlacement_emits_data_placement(LabelPlacement placement, string expected)
    {
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.TextPlacement, placement));
        Assert.Equal(expected, cut.Find(".atom-checkbox-wrap").GetAttribute("data-placement"));
    }

    [Theory]
    [InlineData(CheckShape.Square, "square")]
    [InlineData(CheckShape.Rounded, "rounded")]
    [InlineData(CheckShape.Circle, "circle")]
    public void BoxShape_emits_data_shape(CheckShape shape, string expected)
    {
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.BoxShape, shape));
        Assert.Equal(expected, cut.Find(".atom-checkbox").GetAttribute("data-shape"));
    }

    [Fact]
    public void BoxSize_emits_control_size_var()
    {
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.BoxSize, 26d));
        Assert.Contains("--field-control-size:26px;", cut.Find(".atom-checkbox").GetAttribute("style")!);
    }

    // ---- indeterminate -----------------------------------------------------------------------

    [Fact]
    public void Indeterminate_flags_the_root_and_reports_mixed()
    {
        // The native `indeterminate` flag is JS-only, so the mixed state is a data attribute the CSS
        // keys the dash off, plus aria-checked for assistive tech.
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.Indeterminate, true));

        Assert.Equal("true", cut.Find(".atom-checkbox").GetAttribute("data-indeterminate"));
        Assert.Equal("mixed", cut.Find("input").GetAttribute("aria-checked"));
        Assert.NotNull(cut.Find(".atom-checkbox-dash"));
    }

    [Fact]
    public void Not_indeterminate_emits_neither_attribute()
    {
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.Value, true));

        Assert.Null(cut.Find(".atom-checkbox").GetAttribute("data-indeterminate"));
        Assert.Null(cut.Find("input").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Indeterminate_is_presentational_and_does_not_block_toggling()
    {
        var cut = Render<AtomCheckbox>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.Indeterminate, true));

        cut.Find("input").Change(true);

        Assert.True(cut.Instance.Value);
    }

    // ---- value flow --------------------------------------------------------------------------

    [Fact]
    public void Change_commits_both_directions()
    {
        var changes = new List<bool>();
        var cut = Render<AtomCheckbox>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<bool>(this, v => changes.Add(v))));

        cut.Find("input").Change(true);
        cut.Find("input").Change(false);

        Assert.Equal(new[] { true, false }, changes);
        Assert.False(cut.Instance.Value);
    }

    // ---- state -------------------------------------------------------------------------------

    [Fact]
    public void ReadOnly_falls_back_to_native_disabled_but_keeps_its_own_data_state()
    {
        // The HTML spec ignores `readonly` on a checkbox, so the only way to block it is `disabled`.
        // data-state stays "readonly" so CSS (and the consumer) can still tell the two apart.
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.ReadOnly, true));

        var input = cut.Find("input");
        Assert.NotNull(input.GetAttribute("disabled"));
        Assert.Null(input.GetAttribute("readonly"));
        Assert.Equal("readonly", cut.Find(".atom-checkbox").GetAttribute("data-state"));
    }

    [Fact]
    public void Disabled_renders_native_disabled_and_blocks_commits()
    {
        var cut = Render<AtomCheckbox>(p => p
            .Add(c => c.Value, false)
            .Add(c => c.Disabled, true));

        Assert.NotNull(cut.Find("input").GetAttribute("disabled"));
        Assert.Equal("disabled", cut.Find(".atom-checkbox").GetAttribute("data-state"));

        cut.Find("input").Change(true);
        Assert.False(cut.Instance.Value);
    }

    [Fact]
    public void Visible_false_hides_via_display_none_and_stays_in_the_dom()
    {
        var cut = Render<AtomCheckbox>(p => p.Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-checkbox").GetAttribute("style")!);
        Assert.NotNull(cut.Find("input"));
    }

    [Fact]
    public void AriaLabel_falls_back_to_label_then_to_a_default()
    {
        Assert.Equal("Checkbox", Render<AtomCheckbox>().Find("input").GetAttribute("aria-label"));

        Assert.Equal("Terms", Render<AtomCheckbox>(p => p.Add(c => c.Label, "Terms"))
            .Find("input").GetAttribute("aria-label"));
    }

    // ---- styling axes ------------------------------------------------------------------------

    [Fact]
    public void Variant_size_and_effect_emit_their_data_attributes()
    {
        var cut = Render<AtomCheckbox>(p => p
            .Add(c => c.Variant, InputVariant.Filled)
            .Add(c => c.Size, InputSize.Large)
            .Add(c => c.Effect, InputEffect.FocusUnderline));

        var root = cut.Find(".atom-checkbox");
        Assert.Equal("filled", root.GetAttribute("data-variant"));
        Assert.Equal("large", root.GetAttribute("data-size"));
        Assert.Equal("focus-underline", root.GetAttribute("data-effect"));
    }

    // ---- EditContext / validation ------------------------------------------------------------

    [Fact]
    public void Error_state_sets_aria_invalid_and_replaces_help_text()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Accepted)), "You must accept");
        editContext.NotifyValidationStateChanged();

        var cut = Render<AtomCheckbox>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Accepted)
            .Add(c => c.HelpText, "Required to continue")
            .Add(c => c.ValidationFor, () => model.Accepted));

        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Equal("error", cut.Find(".atom-checkbox").GetAttribute("data-state"));
        Assert.Equal("You must accept", cut.Find(".atom-checkbox-subtext").TextContent);
    }

    [Fact]
    public void Committing_a_value_notifies_the_EditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var notified = false;
        editContext.OnFieldChanged += (_, _) => notified = true;

        var cut = Render<AtomCheckbox>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Accepted)
            .Add(c => c.ValidationFor, () => model.Accepted));

        cut.Find("input").Change(true);

        Assert.True(notified);
    }
}
