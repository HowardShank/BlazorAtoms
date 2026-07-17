using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Data.Tests;

public class AtomDataHasherTests : TestContext
{
    // Enum-name -> data-attribute-string wire form. Data-driven so new HashAlgorithmKind values
    // are exercised automatically the moment the dictionary grows a matching entry.
    public static readonly Dictionary<HashAlgorithmKind, string> AlgoAttr = new()
    {
        [HashAlgorithmKind.Crc32] = "crc32",
        [HashAlgorithmKind.Crc64] = "crc64",
        [HashAlgorithmKind.Md5] = "md5",
        [HashAlgorithmKind.Sha256] = "sha256",
        [HashAlgorithmKind.Sha512] = "sha512",
    };

    public static IEnumerable<object[]> AllAlgos =>
        Enum.GetValues<HashAlgorithmKind>().Select(a => new object[] { a, AlgoAttr[a] });

    // ---- rendering / defaults ---------------------------------------------------------------

    [Fact]
    public void Renders_textarea_by_default()
    {
        var cut = RenderComponent<AtomDataHasher>();

        Assert.NotNull(cut.Find("textarea"));
        Assert.Empty(cut.FindAll("input[type=text]"));
    }

    [Fact]
    public void Multiline_false_renders_input_type_text()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Multiline, false));

        Assert.NotNull(cut.Find("input[type=text]"));
        Assert.Empty(cut.FindAll("textarea"));
    }

    [Fact]
    public void Default_algorithm_is_crc32()
    {
        var cut = RenderComponent<AtomDataHasher>();
        Assert.Equal("crc32", cut.Find(".atom-data-hasher").GetAttribute("data-algorithm"));
    }

    [Fact]
    public void Show_algorithm_picker_default_true_renders_select()
    {
        var cut = RenderComponent<AtomDataHasher>();
        Assert.NotNull(cut.Find(".atom-data-hasher-select"));
    }

    [Fact]
    public void Show_algorithm_picker_false_omits_select()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.ShowAlgorithmPicker, false));
        Assert.Empty(cut.FindAll(".atom-data-hasher-select"));
    }

    // ---- algorithm mapping ------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(AllAlgos))]
    public void Algorithm_maps_to_data_attribute(HashAlgorithmKind alg, string expected)
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Algorithm, alg));
        Assert.Equal(expected, cut.Find(".atom-data-hasher").GetAttribute("data-algorithm"));
    }

    // ---- live hashing -----------------------------------------------------------------------

    [Fact]
    public void Result_updates_when_value_provided()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Value, "123456789")
            .Add(c => c.Algorithm, HashAlgorithmKind.Crc32));

        Assert.Contains("CBF43926", cut.Find(".atom-data-hasher-result-value").TextContent);
    }

    [Theory]
    [MemberData(nameof(AllAlgos))]
    public void Result_matches_computer_output(HashAlgorithmKind alg, string _)
    {
        const string input = "test-payload";
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Value, input)
            .Add(c => c.Algorithm, alg));

        var expected = HashComputer.Compute(alg, input);
        Assert.Contains(expected, cut.Find(".atom-data-hasher-result-value").TextContent);
    }

    [Fact]
    public void Result_empty_when_value_null_or_empty()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Value, string.Empty));
        Assert.Equal("true", cut.Find(".atom-data-hasher-result-value").GetAttribute("data-empty"));
    }

    [Fact]
    public void Result_public_property_matches_rendered_value()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.Algorithm, HashAlgorithmKind.Sha256));

        Assert.Equal(HashComputer.Compute(HashAlgorithmKind.Sha256, "abc"), cut.Instance.ResultText);
    }

    // ---- two-way binding --------------------------------------------------------------------

    [Fact]
    public void Two_way_binding_updates_value_on_input()
    {
        string? bound = null;
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v)));

        cut.Find("textarea").Input("goodbye");
        Assert.Equal("goodbye", bound);
    }

    [Fact]
    public void Two_way_binding_algorithm_updates_on_select_change()
    {
        HashAlgorithmKind? bound = null;
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Algorithm, HashAlgorithmKind.Crc32)
            .Add(c => c.AlgorithmChanged, EventCallback.Factory.Create<HashAlgorithmKind>(this, v => bound = v)));

        cut.Find("select").Change(HashAlgorithmKind.Sha256.ToString());
        Assert.Equal(HashAlgorithmKind.Sha256, bound);
    }

    // ---- structure params -------------------------------------------------------------------

    [Fact]
    public void Label_renders_when_set_and_omitted_when_null()
    {
        var withLabel = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Label, "Payload"));
        Assert.Contains("Payload", withLabel.Find("label").TextContent);

        var without = RenderComponent<AtomDataHasher>();
        // Algo picker label is a <label> too; expect zero visible top-level Label, so the
        // OUTER form-label class ".atom-data-hasher-label" must not appear.
        Assert.Empty(without.FindAll(".atom-data-hasher-label"));
    }

    [Fact]
    public void LabelCol_and_ControlCol_apply_classes()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Label, "L")
            .Add(c => c.LabelCol, "custom-label-col")
            .Add(c => c.ControlCol, "custom-control-col"));

        Assert.Contains("custom-label-col", cut.Find(".atom-data-hasher-label").GetAttribute("class")!);
        Assert.Contains("custom-control-col", cut.Find(".atom-data-hasher-control").GetAttribute("class")!);
    }

    [Fact]
    public void HelpText_renders_in_subtext()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.HelpText, "Digest updates live."));
        Assert.Contains("Digest updates live.", cut.Find(".atom-data-hasher-subtext").TextContent);
    }

    [Fact]
    public void AlgorithmLabel_and_ResultLabel_customize_visible_text()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.AlgorithmLabel, "Engine")
            .Add(c => c.ResultLabel, "Digest"));

        Assert.Contains("Engine", cut.Find(".atom-data-hasher-algo-label").TextContent);
        Assert.Contains("Digest", cut.Find(".atom-data-hasher-result-label").TextContent);
    }

    [Fact]
    public void Placeholder_flows_through_to_field()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Placeholder, "type here"));
        Assert.Equal("type here", cut.Find("textarea").GetAttribute("placeholder"));
    }

    [Fact]
    public void AriaLabel_takes_precedence_over_Label()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Label, "LabelText")
            .Add(c => c.AriaLabel, "Explicit ARIA"));

        Assert.Equal("Explicit ARIA", cut.Find("textarea").GetAttribute("aria-label"));
    }

    [Fact]
    public void AriaLabel_falls_back_to_Label_when_null()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Label, "LabelText"));
        Assert.Equal("LabelText", cut.Find("textarea").GetAttribute("aria-label"));
    }

    [Fact]
    public void Rows_flows_through_to_textarea()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Rows, 9));
        Assert.Equal("9", cut.Find("textarea").GetAttribute("rows"));
    }

    // ---- state ------------------------------------------------------------------------------

    [Fact]
    public void Disabled_greys_out_and_blocks_input()
    {
        string? changed = null;
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Value, "before")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => changed = v)));

        Assert.Equal("disabled", cut.Find(".atom-data-hasher").GetAttribute("data-state"));
        Assert.NotNull(cut.Find("textarea").GetAttribute("disabled"));
        cut.Find("textarea").Input("after");
        Assert.Null(changed);
    }

    [Fact]
    public void ReadOnly_equates_to_disabled()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.ReadOnly, true));
        Assert.Equal("disabled", cut.Find(".atom-data-hasher").GetAttribute("data-state"));
        Assert.NotNull(cut.Find("textarea").GetAttribute("disabled"));
    }

    [Fact]
    public void Visible_false_hides_via_display_none()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Visible, false));
        Assert.Contains("display:none", cut.Find(".atom-data-hasher").GetAttribute("style") ?? "");
    }

    // ---- styling params ---------------------------------------------------------------------

    [Fact]
    public void Width_emits_custom_property()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p.Add(c => c.Width, 640d));
        Assert.Contains("--hasher-width:640px", cut.Find(".atom-data-hasher").GetAttribute("style") ?? "");
    }

    [Fact]
    public void ResultColor_and_ResultBackgroundColor_emit_custom_properties()
    {
        var cut = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.ResultColor, "#00ff41")
            .Add(c => c.ResultBackgroundColor, "#000"));

        var style = cut.Find(".atom-data-hasher").GetAttribute("style")!;
        Assert.Contains("--hasher-result-color:#00ff41", style);
        Assert.Contains("--hasher-result-bg:#000", style);
    }

    [Fact]
    public void ResultColor_defaults_absent_when_unset()
    {
        var cut = RenderComponent<AtomDataHasher>();
        var style = cut.Find(".atom-data-hasher").GetAttribute("style") ?? "";
        Assert.DoesNotContain("--hasher-result-color", style);
        Assert.DoesNotContain("--hasher-result-bg", style);
    }

    // ---- encoding ---------------------------------------------------------------------------

    [Fact]
    public void Encoding_change_alters_digest()
    {
        var utf8 = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Value, "é")
            .Add(c => c.Algorithm, HashAlgorithmKind.Sha256)
            .Add(c => c.Encoding, Encoding.UTF8));

        var latin1 = RenderComponent<AtomDataHasher>(p => p
            .Add(c => c.Value, "é")
            .Add(c => c.Algorithm, HashAlgorithmKind.Sha256)
            .Add(c => c.Encoding, Encoding.Latin1));

        Assert.NotEqual(
            utf8.Find(".atom-data-hasher-result-value").TextContent,
            latin1.Find(".atom-data-hasher-result-value").TextContent);
    }

    // ---- EditContext ------------------------------------------------------------------------

    [Fact]
    public void EditContext_validation_shows_error_state()
    {
        var model = new TestModel { Text = "" };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Text)), "Required");
        editContext.NotifyValidationStateChanged();

        var cut = RenderComponent<AtomDataHasher>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Text)
            .Add(c => c.ValidationFor, () => model.Text));

        Assert.Equal("error", cut.Find(".atom-data-hasher").GetAttribute("data-state"));
        Assert.Equal("true", cut.Find("textarea").GetAttribute("aria-invalid"));
        Assert.Contains("Required", cut.Find(".atom-data-hasher-subtext").TextContent);
    }

    [Fact]
    public void ValidationFor_falls_back_to_ValueExpression()
    {
        var model = new TestModel { Text = "" };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Text)), "Required");
        editContext.NotifyValidationStateChanged();

        Expression<Func<string?>> valueExpr = () => model.Text;

        var cut = RenderComponent<AtomDataHasher>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Text)
            .Add(c => c.ValueExpression, valueExpr));

        Assert.Equal("error", cut.Find(".atom-data-hasher").GetAttribute("data-state"));
    }

    // ---- accessibility ---------------------------------------------------------------------

    [Fact]
    public void Result_panel_is_polite_live_region()
    {
        var cut = RenderComponent<AtomDataHasher>();
        var panel = cut.Find(".atom-data-hasher-result");
        Assert.Equal("status", panel.GetAttribute("role"));
        Assert.Equal("polite", panel.GetAttribute("aria-live"));
    }

    [Fact]
    public void Field_disables_native_spellcheck()
    {
        var cut = RenderComponent<AtomDataHasher>();
        Assert.Equal("false", cut.Find("textarea").GetAttribute("spellcheck"));
    }

    private sealed class TestModel
    {
        [Required]
        public string? Text { get; set; }
    }
}
