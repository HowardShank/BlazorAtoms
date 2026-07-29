using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomSelectTests : BunitContext
{
    private enum Tier { Free, Pro, Team }

    private sealed record Plan(string Code, string Name);

    private sealed class TestModel
    {
        // Non-nullable so ValidationFor matches AtomSelect<string>'s Expression<Func<string>>.
        [Required(ErrorMessage = "Choose a tier")]
        public string Tier { get; set; } = "";
    }

    private static readonly string[] Colors = ["Red", "Green", "Blue"];

    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_native_select_with_one_option_per_value_plus_a_drawn_arrow()
    {
        var cut = Render<AtomSelect<string>>(p => p.Add(c => c.Options, Colors));

        Assert.NotNull(cut.Find("select.atom-select-field"));
        Assert.Equal(3, cut.FindAll("option").Count);
        // The platform arrow can't be styled, so appearance:none removes it and this SVG replaces it.
        Assert.NotNull(cut.Find(".atom-select-arrow"));
    }

    [Fact]
    public void Null_options_render_an_empty_select_without_throwing()
    {
        var cut = Render<AtomSelect<string>>();

        Assert.Empty(cut.FindAll("option"));
        Assert.NotNull(cut.Find("select"));
    }

    [Fact]
    public void OptionLabel_supplies_the_display_text_while_value_stays_the_key()
    {
        var cut = Render<AtomSelect<Plan>>(p => p
            .Add(c => c.Options, [new Plan("A", "Basic"), new Plan("B", "Pro")])
            .Add(c => c.OptionLabel, o => o.Name));

        var options = cut.FindAll("option");
        Assert.Equal("Basic", options[0].TextContent);
        Assert.Equal("Pro", options[1].TextContent);
    }

    [Fact]
    public void Placeholder_renders_a_leading_empty_option_disabled_by_default()
    {
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Placeholder, "Choose one…"));

        var first = cut.FindAll("option")[0];
        Assert.Equal("Choose one…", first.TextContent);
        Assert.Equal("", first.GetAttribute("value"));
        Assert.NotNull(first.GetAttribute("disabled"));
    }

    [Fact]
    public void PlaceholderSelectable_drops_the_disabled_attribute()
    {
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Placeholder, "Any")
            .Add(c => c.PlaceholderSelectable, true));

        Assert.Null(cut.FindAll("option")[0].GetAttribute("disabled"));
    }

    [Fact]
    public void No_placeholder_means_no_empty_option()
    {
        var cut = Render<AtomSelect<string>>(p => p.Add(c => c.Options, Colors));
        Assert.DoesNotContain(cut.FindAll("option"), o => o.GetAttribute("value") == "");
    }

    [Fact]
    public void ChildContent_appends_raw_option_markup()
    {
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Options, ["Red"])
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<option value=\"Custom\">Custom</option>")));

        var values = cut.FindAll("option").Select(o => o.GetAttribute("value")).ToList();
        Assert.Equal(new[] { "Red", "Custom" }, values);
    }

    [Fact]
    public void OptionDisabled_greys_out_matching_options()
    {
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.OptionDisabled, o => o == "Blue"));

        var options = cut.FindAll("option");
        Assert.Null(options[0].GetAttribute("disabled"));
        Assert.NotNull(options[2].GetAttribute("disabled"));
    }

    // ---- value flow --------------------------------------------------------------------------

    [Fact]
    public void Value_renders_as_the_select_value()
    {
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, "Green"));

        Assert.Equal("Green", cut.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void Change_resolves_back_to_the_option_object_itself()
    {
        // Matching against Options (rather than converting the string) is what makes reference types
        // and records work.
        var plans = new[] { new Plan("A", "Basic"), new Plan("B", "Pro") };
        var cut = Render<AtomSelect<Plan>>(p => p
            .Add(c => c.Options, plans)
            .Add(c => c.OptionLabel, o => o.Name)
            .Add(c => c.Value, plans[0]));

        cut.Find("select").Change(plans[1].ToString());

        Assert.Same(plans[1], cut.Instance.Value);
    }

    [Fact]
    public void Enum_values_round_trip()
    {
        var cut = Render<AtomSelect<Tier>>(p => p
            .Add(c => c.Options, [Tier.Free, Tier.Pro, Tier.Team])
            .Add(c => c.Value, Tier.Free));

        cut.Find("select").Change("Team");

        Assert.Equal(Tier.Team, cut.Instance.Value);
    }

    [Fact]
    public void Numeric_values_round_trip_invariantly()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var cut = Render<AtomSelect<double>>(p => p
                .Add(c => c.Options, [1.5, 2.5])
                .Add(c => c.Value, 1.5));

            // Keys are written invariant, so they must be read back invariant too — a de-DE parse of
            // "2.5" would land on 25.
            Assert.Equal("1.5", cut.Find("select").GetAttribute("value"));

            cut.Find("select").Change("2.5");
            Assert.Equal(2.5, cut.Instance.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void A_value_only_present_in_ChildContent_resolves_by_conversion()
    {
        var cut = Render<AtomSelect<int>>(p => p
            .Add(c => c.Options, [1, 2])
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<option value=\"99\">Ninety-nine</option>")));

        cut.Find("select").Change("99");

        Assert.Equal(99, cut.Instance.Value);
    }

    [Fact]
    public void Selecting_the_empty_placeholder_clears_a_nullable_value()
    {
        var cut = Render<AtomSelect<string?>>(p => p
            .Add(c => c.Options, Colors!)
            .Add(c => c.Placeholder, "Any")
            .Add(c => c.PlaceholderSelectable, true)
            .Add(c => c.Value, "Red"));

        cut.Find("select").Change("");

        Assert.Null(cut.Instance.Value);
    }

    [Fact]
    public void Empty_selection_is_ignored_when_TValue_cannot_be_empty()
    {
        // Defaulting to 0 / the first enum member would silently invent a choice the user never made.
        var cut = Render<AtomSelect<Tier>>(p => p
            .Add(c => c.Options, [Tier.Free, Tier.Pro])
            .Add(c => c.Value, Tier.Pro));

        cut.Find("select").Change("");

        Assert.Equal(Tier.Pro, cut.Instance.Value);
    }

    [Fact]
    public void Unresolvable_selection_is_dropped()
    {
        var cut = Render<AtomSelect<int>>(p => p
            .Add(c => c.Options, [1, 2])
            .Add(c => c.Value, 2));

        cut.Find("select").Change("not-a-number");

        Assert.Equal(2, cut.Instance.Value);
    }

    // ---- state -------------------------------------------------------------------------------

    [Fact]
    public void ReadOnly_falls_back_to_disabled_but_keeps_its_own_data_state()
    {
        // <select> has no `readonly` in the HTML spec at all.
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.ReadOnly, true));

        var select = cut.Find("select");
        Assert.NotNull(select.GetAttribute("disabled"));
        Assert.Null(select.GetAttribute("readonly"));
        Assert.Equal("readonly", cut.Find(".atom-select").GetAttribute("data-state"));
    }

    [Fact]
    public void Disabled_blocks_commits()
    {
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, "Red")
            .Add(c => c.Disabled, true));

        cut.Find("select").Change("Blue");

        Assert.Equal("Red", cut.Instance.Value);
    }

    [Fact]
    public void Visible_false_hides_via_display_none()
    {
        var cut = Render<AtomSelect<string>>(p => p.Add(c => c.Visible, false));
        Assert.Contains("display:none", cut.Find(".atom-select").GetAttribute("style")!);
    }

    [Fact]
    public void AriaLabel_falls_back_to_label_then_to_a_default()
    {
        Assert.Equal("Select", Render<AtomSelect<string>>().Find("select").GetAttribute("aria-label"));

        Assert.Equal("Tier", Render<AtomSelect<string>>(p => p.Add(c => c.Label, "Tier"))
            .Find("select").GetAttribute("aria-label"));
    }

    // ---- styling axes ------------------------------------------------------------------------

    [Fact]
    public void Variant_size_and_effect_emit_their_data_attributes()
    {
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Variant, InputVariant.Filled)
            .Add(c => c.Size, InputSize.Large)
            .Add(c => c.Effect, InputEffect.FocusGlow));

        var root = cut.Find(".atom-select");
        Assert.Equal("filled", root.GetAttribute("data-variant"));
        Assert.Equal("large", root.GetAttribute("data-size"));
        Assert.Equal("focus-glow", root.GetAttribute("data-effect"));
    }

    [Fact]
    public void Theming_parameters_emit_field_custom_properties()
    {
        var cut = Render<AtomSelect<string>>(p => p
            .Add(c => c.Width, 200d)
            .Add(c => c.BorderColor, "#333"));

        var style = cut.Find(".atom-select").GetAttribute("style")!;
        Assert.Contains("--field-width:200px;", style);
        Assert.Contains("--field-border-color:#333;", style);
    }

    // ---- EditContext / validation ------------------------------------------------------------

    [Fact]
    public void Error_state_sets_aria_invalid_and_replaces_help_text()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Tier)), "Choose a tier");
        editContext.NotifyValidationStateChanged();

        var cut = Render<AtomSelect<string>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, model.Tier)
            .Add(c => c.HelpText, "Pick any")
            .Add(c => c.ValidationFor, () => model.Tier));

        Assert.Equal("true", cut.Find("select").GetAttribute("aria-invalid"));
        Assert.Equal("error", cut.Find(".atom-select").GetAttribute("data-state"));
        Assert.Equal("Choose a tier", cut.Find(".atom-select-subtext").TextContent);
    }

    [Fact]
    public void Committing_a_value_notifies_the_EditContext()
    {
        var model = new TestModel();
        var editContext = new EditContext(model);
        var notified = false;
        editContext.OnFieldChanged += (_, _) => notified = true;

        var cut = Render<AtomSelect<string>>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Options, Colors)
            .Add(c => c.Value, model.Tier)
            .Add(c => c.ValidationFor, () => model.Tier));

        cut.Find("select").Change("Blue");

        Assert.True(notified);
    }
}
