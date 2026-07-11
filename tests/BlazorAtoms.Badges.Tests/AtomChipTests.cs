using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Badges.Tests;

public class AtomChipTests : TestContext
{
    [Fact]
    public void Renders_label_from_text()
    {
        var cut = RenderComponent<AtomChip>(p => p.Add(c => c.Text, "React"));
        Assert.Equal("React", cut.Find(".atom-chip-label").TextContent);
    }

    [Fact]
    public void ChildContent_overrides_text()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "ignored")
            .AddChildContent("<b>bold</b>"));
        Assert.Contains("<b>bold</b>", cut.Find(".atom-chip-label").InnerHtml);
    }

    [Fact]
    public void Icon_slot_renders_before_label()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "tag")
            .Add(c => c.Icon, b => b.AddMarkupContent(0, "<i class=\"star\"></i>")));
        Assert.NotNull(cut.Find(".atom-chip-icon .star"));
    }

    [Fact]
    public void Variant_and_appearance_set_data_attributes()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Variant, Variant.Success)
            .Add(c => c.Appearance, Appearance.Outline));
        var chip = cut.Find(".atom-chip");
        Assert.Equal("success", chip.GetAttribute("data-variant"));
        Assert.Equal("outline", chip.GetAttribute("data-appearance"));
    }

    [Fact]
    public void Default_appearance_is_soft()
    {
        var cut = RenderComponent<AtomChip>(p => p.Add(c => c.Text, "x"));
        Assert.Equal("soft", cut.Find(".atom-chip").GetAttribute("data-appearance"));
    }

    [Fact]
    public void Not_a_button_without_onclick()
    {
        var cut = RenderComponent<AtomChip>(p => p.Add(c => c.Text, "x"));
        var chip = cut.Find(".atom-chip");
        Assert.Null(chip.GetAttribute("role"));
        Assert.Null(chip.GetAttribute("tabindex"));
        Assert.Null(chip.GetAttribute("data-clickable"));
    }

    [Fact]
    public void Clickable_when_onclick_set_and_reflects_selected()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Selected, true)
            .Add(c => c.OnClick, () => { }));
        var chip = cut.Find(".atom-chip");
        Assert.Equal("button", chip.GetAttribute("role"));
        Assert.Equal("0", chip.GetAttribute("tabindex"));
        Assert.Equal("true", chip.GetAttribute("aria-pressed"));
        Assert.Equal("true", chip.GetAttribute("data-selected"));
    }

    [Fact]
    public void OnClick_fires_on_click_and_on_enter()
    {
        var clicks = 0;
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.OnClick, () => clicks++));

        cut.Find(".atom-chip").Click();
        cut.Find(".atom-chip").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(2, clicks);
    }

    [Fact]
    public void Disabled_blocks_click_and_flags_attributes()
    {
        var clicks = 0;
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, () => clicks++));

        var chip = cut.Find(".atom-chip");
        Assert.Equal("true", chip.GetAttribute("aria-disabled"));
        Assert.Equal("true", chip.GetAttribute("data-disabled"));
        chip.Click();
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void Removable_renders_button_and_fires_onremove()
    {
        var removed = 0;
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Removable, true)
            .Add(c => c.OnRemove, () => removed++));

        var btn = cut.Find(".atom-chip-remove");
        btn.Click();
        Assert.Equal(1, removed);
    }

    [Fact]
    public void No_remove_button_when_not_removable()
    {
        var cut = RenderComponent<AtomChip>(p => p.Add(c => c.Text, "x"));
        Assert.Empty(cut.FindAll(".atom-chip-remove"));
    }

    [Fact]
    public void Color_override_tokens_emitted()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Background, "#123456")
            .Add(c => c.TextColor, "#fff")
            .Add(c => c.Size, 40)
            .Add(c => c.Radius, 8));
        var style = cut.Find(".atom-chip").GetAttribute("style") ?? "";
        Assert.Contains("--chip-bg:#123456", style);
        Assert.Contains("--chip-color:#fff", style);
        Assert.Contains("--chip-size:40px", style);
        Assert.Contains("--chip-radius:8px", style);
    }

    [Fact]
    public void Height_token_emitted_independent_of_size()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Size, 30)
            .Add(c => c.Height, 44));
        var style = cut.Find(".atom-chip").GetAttribute("style") ?? "";
        Assert.Contains("--chip-size:30px", style);
        Assert.Contains("--chip-height:44px", style);
    }

    [Fact]
    public void Font_styling_tokens_emitted()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.FontFamily, "Inter")
            .Add(c => c.FontSize, 15)
            .Add(c => c.FontWeight, "700")
            .Add(c => c.FontStyle, "italic")
            .Add(c => c.LetterSpacing, ".05em")
            .Add(c => c.TextTransform, "uppercase"));
        var style = cut.Find(".atom-chip").GetAttribute("style") ?? "";
        Assert.Contains("--chip-font-family:Inter", style);
        Assert.Contains("--chip-font-size:15px", style);
        Assert.Contains("--chip-font-weight:700", style);
        Assert.Contains("--chip-font-style:italic", style);
        Assert.Contains("--chip-letter-spacing:.05em", style);
        Assert.Contains("--chip-text-transform:uppercase", style);
    }

    // --- Escape hatch (AtomComponentBase: Class / Style / AdditionalAttributes) ---

    [Fact]
    public void Class_param_appends_after_root_class()
    {
        var cut = RenderComponent<AtomChip>(p => p.Add(c => c.Text, "x").Add(c => c.CssClass, "brand"));
        var cls = cut.Find(".atom-chip").GetAttribute("class") ?? "";
        Assert.Equal("atom-chip brand", cls);
    }

    [Fact]
    public void Style_param_appends_after_root_style()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Size, 40)
            .Add(c => c.Style, "color:red"));
        var style = cut.Find(".atom-chip").GetAttribute("style") ?? "";
        Assert.Equal("--chip-size:40px;color:red", style);
    }

    [Fact]
    public void No_empty_style_attribute_when_nothing_set()
    {
        var cut = RenderComponent<AtomChip>(p => p.Add(c => c.Text, "x"));
        Assert.Null(cut.Find(".atom-chip").GetAttribute("style"));
    }

    [Fact]
    public void Additional_attributes_splat_onto_root()
    {
        var cut = RenderComponent<AtomChip>(p => p
            .Add(c => c.Text, "x")
            .AddUnmatched("title", "hi")
            .AddUnmatched("data-test", "42"));
        var root = cut.Find(".atom-chip");
        Assert.Equal("hi", root.GetAttribute("title"));
        Assert.Equal("42", root.GetAttribute("data-test"));
    }
}
