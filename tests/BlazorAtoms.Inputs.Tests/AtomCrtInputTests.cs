using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorAtoms.Inputs.Tests;

public class AtomCrtInputTests : TestContext
{
    // Enum-name -> data-attribute-string. Keeps the Font theories data-driven so new CrtFont
    // values (e.g. SpecialElite, CutiveMono) are automatically covered as they're added — plus one
    // dictionary entry mapping the enum name to its lowercase-hyphenated wire form.
    public static readonly Dictionary<CrtFont, string> FontDataAttr = new()
    {
        [CrtFont.System] = "system",
        [CrtFont.Vt323] = "vt323",
        [CrtFont.PressStart2P] = "press-start-2p",
    };

    public static IEnumerable<object[]> AllFontsData =>
        Enum.GetValues<CrtFont>().Select(f => new object[] { f, FontDataAttr[f] });

    [Fact]
    public void Renders_textarea_by_default()
    {
        var cut = RenderComponent<AtomCrtInput>();

        Assert.NotNull(cut.Find("textarea"));
        Assert.Empty(cut.FindAll("input[type=text]"));
    }

    [Fact]
    public void Multiline_false_renders_input_type_text()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.Multiline, false));

        Assert.NotNull(cut.Find("input[type=text]"));
        Assert.Empty(cut.FindAll("textarea"));
    }

    [Fact]
    public void Two_way_binding_updates_value_on_input()
    {
        string? bound = null;
        var cut = RenderComponent<AtomCrtInput>(p => p
            .Add(c => c.Value, "hello")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v)));

        cut.Find("textarea").Input("hello world");

        Assert.Equal("hello world", bound);
    }

    [Theory]
    [InlineData(CrtPhosphor.Green, "green")]
    [InlineData(CrtPhosphor.Amber, "amber")]
    [InlineData(CrtPhosphor.Blue, "blue")]
    [InlineData(CrtPhosphor.Red, "red")]
    [InlineData(CrtPhosphor.White, "white")]
    public void Phosphor_maps_to_data_attribute(CrtPhosphor phosphor, string expected)
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.Phosphor, phosphor));

        Assert.Equal(expected, cut.Find(".atom-crt-input").GetAttribute("data-phosphor"));
    }

    [Theory]
    [MemberData(nameof(AllFontsData))]
    public void Font_maps_to_data_attribute(CrtFont font, string expected)
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.Font, font));

        Assert.Equal(expected, cut.Find(".atom-crt-input").GetAttribute("data-font"));
    }

    [Fact]
    public void Effect_flags_emit_data_attributes_when_true()
    {
        var cut = RenderComponent<AtomCrtInput>();

        var root = cut.Find(".atom-crt-input");
        Assert.Equal("true", root.GetAttribute("data-glow"));
        Assert.Equal("true", root.GetAttribute("data-scanlines"));
        Assert.Equal("true", root.GetAttribute("data-bezel"));
        Assert.Equal("true", root.GetAttribute("data-cursor"));
    }

    [Fact]
    public void Effect_flags_omit_data_attributes_when_false()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p
            .Add(c => c.Glow, false)
            .Add(c => c.Scanlines, false)
            .Add(c => c.Bezel, false)
            .Add(c => c.CursorBlink, false));

        var root = cut.Find(".atom-crt-input");
        Assert.Null(root.GetAttribute("data-glow"));
        Assert.Null(root.GetAttribute("data-scanlines"));
        Assert.Null(root.GetAttribute("data-bezel"));
        Assert.Null(root.GetAttribute("data-cursor"));
    }

    [Fact]
    public void Color_and_BackgroundColor_emit_custom_properties()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p
            .Add(c => c.Color, "#00ff41")
            .Add(c => c.BackgroundColor, "#000000"));

        var style = cut.Find(".atom-crt-input").GetAttribute("style")!;
        Assert.Contains("--crt-color:#00ff41", style);
        Assert.Contains("--crt-bg:#000000", style);
    }

    [Fact]
    public void Width_height_fontSize_emit_custom_properties()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p
            .Add(c => c.Width, 480d)
            .Add(c => c.Height, 240d)
            .Add(c => c.FontSize, 20d));

        var style = cut.Find(".atom-crt-input").GetAttribute("style")!;
        Assert.Contains("--crt-width:480px", style);
        Assert.Contains("--crt-height:240px", style);
        Assert.Contains("--crt-font-size:20px", style);
    }

    [Fact]
    public void Disabled_greys_out_and_blocks_input()
    {
        string? changed = null;
        var cut = RenderComponent<AtomCrtInput>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Value, "before")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => changed = v)));

        Assert.Equal("disabled", cut.Find(".atom-crt-input").GetAttribute("data-state"));
        Assert.NotNull(cut.Find("textarea").GetAttribute("disabled"));

        cut.Find("textarea").Input("after");
        Assert.Null(changed);
    }

    [Fact]
    public void ReadOnly_equates_to_disabled()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.ReadOnly, true));

        Assert.Equal("disabled", cut.Find(".atom-crt-input").GetAttribute("data-state"));
        Assert.NotNull(cut.Find("textarea").GetAttribute("disabled"));
    }

    [Fact]
    public void Visible_false_hides_via_display_none()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.Visible, false));

        var style = cut.Find(".atom-crt-input").GetAttribute("style");
        Assert.Contains("display:none", style ?? "");
    }

    [Fact]
    public void Rows_and_placeholder_flow_through_to_textarea()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p
            .Add(c => c.Rows, 8)
            .Add(c => c.Placeholder, "> _"));

        var ta = cut.Find("textarea");
        Assert.Equal("8", ta.GetAttribute("rows"));
        Assert.Equal("> _", ta.GetAttribute("placeholder"));
    }

    [Fact]
    public void EditContext_validation_shows_error_state()
    {
        var model = new TestModel { Text = "" };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Text)), "Required");
        editContext.NotifyValidationStateChanged();

        var cut = RenderComponent<AtomCrtInput>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Text)
            .Add(c => c.ValidationFor, () => model.Text));

        Assert.Equal("error", cut.Find(".atom-crt-input").GetAttribute("data-state"));
        Assert.Equal("true", cut.Find("textarea").GetAttribute("aria-invalid"));
        Assert.Contains("Required", cut.Find(".atom-crt-input-subtext").TextContent);
    }

    // ---- gap-fill tests --------------------------------------------------------------------

    [Fact]
    public void Label_renders_as_label_element_when_set()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.Label, "Foo"));

        var label = cut.Find("label");
        Assert.Contains("Foo", label.TextContent);
    }

    [Fact]
    public void Label_omitted_when_null()
    {
        var cut = RenderComponent<AtomCrtInput>();

        Assert.Empty(cut.FindAll("label"));
    }

    [Fact]
    public void LabelCol_and_ControlCol_apply_classes()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p
            .Add(c => c.Label, "L")
            .Add(c => c.LabelCol, "custom-label-col")
            .Add(c => c.ControlCol, "custom-control-col"));

        Assert.Contains("custom-label-col", cut.Find("label").GetAttribute("class")!);
        Assert.Contains("custom-control-col", cut.Find(".atom-crt-input-control").GetAttribute("class")!);
    }

    [Fact]
    public void HelpText_renders_in_subtext_on_happy_path()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.HelpText, "Press ENTER to continue"));

        var sub = cut.Find(".atom-crt-input-subtext");
        Assert.Contains("Press ENTER to continue", sub.TextContent);
    }

    [Fact]
    public void AriaLabel_takes_precedence_over_Label()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p
            .Add(c => c.Label, "LabelText")
            .Add(c => c.AriaLabel, "Explicit ARIA"));

        Assert.Equal("Explicit ARIA", cut.Find("textarea").GetAttribute("aria-label"));
    }

    [Fact]
    public void AriaLabel_falls_back_to_Label_when_null()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.Label, "LabelText"));

        Assert.Equal("LabelText", cut.Find("textarea").GetAttribute("aria-label"));
    }

    [Fact]
    public void Cols_flows_through_to_textarea()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.Cols, 40));

        Assert.Equal("40", cut.Find("textarea").GetAttribute("cols"));
    }

    [Fact]
    public void Height_emits_crt_height_var_alone()
    {
        var cut = RenderComponent<AtomCrtInput>(p => p.Add(c => c.Height, 240d));

        var style = cut.Find(".atom-crt-input").GetAttribute("style") ?? "";
        Assert.Contains("--crt-height:240px", style);
        Assert.DoesNotContain("--crt-width", style);
        Assert.DoesNotContain("--crt-font-size", style);
    }

    [Fact]
    public void ValidationFor_falls_back_to_ValueExpression()
    {
        // Simulates @bind-Value inside an EditForm with no explicit ValidationFor: Blazor's own
        // binding infrastructure would supply ValueExpression; asserting we honor it as the
        // validation-field selector.
        var model = new TestModel { Text = "" };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(editContext.Field(nameof(TestModel.Text)), "Required");
        editContext.NotifyValidationStateChanged();

        Expression<Func<string?>> valueExpr = () => model.Text;

        var cut = RenderComponent<AtomCrtInput>(p => p
            .AddCascadingValue(editContext)
            .Add(c => c.Value, model.Text)
            .Add(c => c.ValueExpression, valueExpr));

        Assert.Equal("error", cut.Find(".atom-crt-input").GetAttribute("data-state"));
    }

    [Fact]
    public void Field_disables_native_spellcheck()
    {
        var cut = RenderComponent<AtomCrtInput>();

        Assert.Equal("false", cut.Find("textarea").GetAttribute("spellcheck"));
    }

    [Fact]
    public void Default_phosphor_is_green()
    {
        var cut = RenderComponent<AtomCrtInput>();

        Assert.Equal("green", cut.Find(".atom-crt-input").GetAttribute("data-phosphor"));
    }

    private sealed class TestModel
    {
        [Required]
        public string? Text { get; set; }
    }
}
