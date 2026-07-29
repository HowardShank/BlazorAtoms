using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomRadioGroupTests : BunitContext
{
    private enum Size { Small, Medium, Large }

    private sealed record Plan(string Code, string Name);

    private sealed class TestModel
    {
        // Non-nullable so ValidationFor matches AtomRadioGroup<string>'s Expression<Func<string>>.
        [Required(ErrorMessage = "Pick a size")]
        public string Choice { get; set; } = "";
    }

    private static readonly string[] Colors = ["Red", "Green", "Blue"];

    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_one_native_radio_per_option_inside_a_radiogroup()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p.Add(c => c.Options, Colors));

        var inputs = cut.FindAll("input[type=radio]");
        Assert.Equal(3, inputs.Count);
        Assert.NotNull(cut.Find("[role=radiogroup]"));
        Assert.Equal(3, cut.FindAll(".atom-radio-group-mark").Count);
    }

    [Fact]
    public void All_radios_share_one_generated_name()
    {
        // Mutual exclusivity and arrow-key navigation are the platform's, and both key off `name`.
        var cut = Render<AtomRadioGroup<string>>(p => p.Add(c => c.Options, Colors));

        var names = cut.FindAll("input").Select(i => i.GetAttribute("name")).Distinct().ToList();
        Assert.Single(names);
        Assert.False(string.IsNullOrEmpty(names[0]));
    }

    [Fact]
    public void Two_groups_get_different_generated_names()
    {
        // A shared name across two groups would make them exclusive with each other.
        var a = Render<AtomRadioGroup<string>>(p => p.Add(c => c.Options, Colors));
        var b = Render<AtomRadioGroup<string>>(p => p.Add(c => c.Options, Colors));

        Assert.NotEqual(a.Find("input").GetAttribute("name"), b.Find("input").GetAttribute("name"));
    }

    [Fact]
    public void Explicit_Name_wins()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Name, "colors"));

        Assert.All(cut.FindAll("input"), i => Assert.Equal("colors", i.GetAttribute("name")));
    }

    [Fact]
    public void Null_options_render_an_empty_group_without_throwing()
    {
        var cut = Render<AtomRadioGroup<string>>();

        Assert.Empty(cut.FindAll("input"));
        Assert.NotNull(cut.Find("[role=radiogroup]"));
    }

    [Fact]
    public void OptionLabel_supplies_the_caption()
    {
        var cut = Render<AtomRadioGroup<Plan>>(p => p
            .Add(c => c.Options, [new Plan("A", "Basic"), new Plan("B", "Pro")])
            .Add(c => c.OptionLabel, o => o.Name));

        var texts = cut.FindAll(".atom-radio-group-text").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Basic", "Pro" }, texts);
    }

    [Fact]
    public void Without_OptionLabel_the_caption_is_ToString()
    {
        var cut = Render<AtomRadioGroup<Size>>(p => p.Add(c => c.Options, [Size.Small, Size.Large]));

        var texts = cut.FindAll(".atom-radio-group-text").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Small", "Large" }, texts);
    }

    [Fact]
    public void OptionTemplate_wins_over_OptionLabel()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, ["Red"])
            .Add(c => c.OptionLabel, o => "label-" + o)
            .Add(c => c.OptionTemplate, o => b => b.AddMarkupContent(0, $"<b>{o}!</b>")));

        Assert.Equal("Red!", cut.Find(".atom-radio-group-text").TextContent.Trim());
    }

    // ---- selection ---------------------------------------------------------------------------

    [Fact]
    public void Matching_option_renders_checked_and_flags_the_row()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, "Green"));

        var inputs = cut.FindAll("input");
        Assert.Null(inputs[0].GetAttribute("checked"));
        Assert.NotNull(inputs[1].GetAttribute("checked"));
        Assert.Equal("true", cut.FindAll(".atom-radio-group-option")[1].GetAttribute("data-selected"));
    }

    [Fact]
    public void Selecting_commits_the_option_object_itself()
    {
        // The handler closes over the option rather than parsing the DOM value back, so reference
        // types and records survive the round trip.
        var plans = new[] { new Plan("A", "Basic"), new Plan("B", "Pro") };
        Plan? changedTo = null;

        var cut = Render<AtomRadioGroup<Plan>>(p => p
            .Add(c => c.Options, plans)
            .Add(c => c.OptionLabel, o => o.Name)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<Plan>(this, v => changedTo = v)));

        cut.FindAll("input")[1].Change(true);

        Assert.Same(plans[1], changedTo);
    }

    [Fact]
    public void Enum_options_round_trip()
    {
        var cut = Render<AtomRadioGroup<Size>>(p => p
            .Add(c => c.Options, [Size.Small, Size.Medium, Size.Large])
            .Add(c => c.Value, Size.Small));

        cut.FindAll("input")[2].Change(true);

        Assert.Equal(Size.Large, cut.Instance.Value);
    }

    [Fact]
    public void Selecting_the_already_selected_option_raises_nothing()
    {
        var raised = 0;
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, "Red")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string>(this, _ => raised++)));

        cut.FindAll("input")[0].Change(true);

        Assert.Equal(0, raised);
    }

    // ---- per-option and whole-control disabling -----------------------------------------------

    [Fact]
    public void OptionDisabled_disables_only_the_matching_options()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.OptionDisabled, o => o == "Green"));

        var inputs = cut.FindAll("input");
        Assert.Null(inputs[0].GetAttribute("disabled"));
        Assert.NotNull(inputs[1].GetAttribute("disabled"));
        Assert.Equal("true", cut.FindAll(".atom-radio-group-option")[1].GetAttribute("data-disabled"));
    }

    [Fact]
    public void Whole_control_Disabled_disables_every_option()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Disabled, true));

        Assert.All(cut.FindAll("input"), i => Assert.NotNull(i.GetAttribute("disabled")));
        Assert.Equal("disabled", cut.Find(".atom-radio-group").GetAttribute("data-state"));
    }

    [Fact]
    public void ReadOnly_falls_back_to_disabled_but_keeps_its_own_data_state()
    {
        // No `readonly` exists for a radio, so blocking it means the native `disabled` attribute.
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, "Red")
            .Add(c => c.ReadOnly, true));

        Assert.All(cut.FindAll("input"), i => Assert.NotNull(i.GetAttribute("disabled")));
        Assert.Equal("readonly", cut.Find(".atom-radio-group").GetAttribute("data-state"));

        cut.FindAll("input")[1].Change(true);
        Assert.Equal("Red", cut.Instance.Value);
    }

    // ---- layout / styling axes ---------------------------------------------------------------

    [Fact]
    public void Orientation_defaults_to_vertical_and_emits_data_orientation()
    {
        Assert.Equal("vertical", Render<AtomRadioGroup<string>>(p => p.Add(c => c.Options, Colors))
            .Find(".atom-radio-group").GetAttribute("data-orientation"));

        Assert.Equal("horizontal", Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Orientation, Orientation.Horizontal))
            .Find(".atom-radio-group").GetAttribute("data-orientation"));
    }

    [Fact]
    public void TextPlacement_emits_data_placement_on_every_option()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.TextPlacement, LabelPlacement.Start));

        Assert.All(cut.FindAll(".atom-radio-group-option"),
            e => Assert.Equal("start", e.GetAttribute("data-placement")));
    }

    [Fact]
    public void MarkSize_emits_control_size_var()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.MarkSize, 24d));

        Assert.Contains("--field-control-size:24px;", cut.Find(".atom-radio-group").GetAttribute("style")!);
    }

    [Fact]
    public void Variant_size_and_effect_emit_their_data_attributes()
    {
        var cut = Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Variant, InputVariant.Underline)
            .Add(c => c.Size, InputSize.Small)
            .Add(c => c.Effect, InputEffect.ShakeOnError));

        var root = cut.Find(".atom-radio-group");
        Assert.Equal("underline", root.GetAttribute("data-variant"));
        Assert.Equal("small", root.GetAttribute("data-size"));
        Assert.Equal("shake-on-error", root.GetAttribute("data-effect"));
    }

    // ---- EditContext / validation ------------------------------------------------------------

    [Fact]
    public void Error_state_sets_aria_invalid_on_the_group_and_replaces_help_text()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Choice)), "Pick a size");
        editContext.NotifyValidationStateChanged();

        var cut = Render<AtomRadioGroup<string>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, model.Choice)
            .Add(c => c.HelpText, "Any color")
            .Add(c => c.ValidationFor, () => model.Choice));

        Assert.Equal("true", cut.Find("[role=radiogroup]").GetAttribute("aria-invalid"));
        Assert.Equal("error", cut.Find(".atom-radio-group").GetAttribute("data-state"));
        Assert.Equal("Pick a size", cut.Find(".atom-radio-group-subtext").TextContent);
    }

    [Fact]
    public void Committing_a_value_notifies_the_EditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var notified = false;
        editContext.OnFieldChanged += (_, _) => notified = true;

        var cut = Render<AtomRadioGroup<string>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, model.Choice)
            .Add(c => c.ValidationFor, () => model.Choice));

        cut.FindAll("input")[0].Change(true);

        Assert.True(notified);
    }

    [Fact]
    public void AriaLabel_falls_back_to_label_then_to_a_default()
    {
        Assert.Equal("Options", Render<AtomRadioGroup<string>>(p => p.Add(c => c.Options, Colors))
            .Find("[role=radiogroup]").GetAttribute("aria-label"));

        Assert.Equal("Color", Render<AtomRadioGroup<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Label, "Color"))
            .Find("[role=radiogroup]").GetAttribute("aria-label"));
    }
}
