namespace BlazorAtoms.Badges.Tests;

public class AtomTagTests : BunitContext
{
    [Fact]
    public void Renders_label_from_text()
    {
        var cut = Render<AtomTag>(p => p.Add(c => c.Text, "bug"));
        Assert.Equal("bug", cut.Find(".atom-tag-label").TextContent);
    }

    [Fact]
    public void ChildContent_overrides_text()
    {
        var cut = Render<AtomTag>(p => p
            .Add(c => c.Text, "ignored")
            .AddChildContent("<em>wontfix</em>"));
        Assert.Contains("<em>wontfix</em>", cut.Find(".atom-tag-label").InnerHtml);
    }

    [Fact]
    public void Icon_slot_renders()
    {
        var cut = Render<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Icon, b => b.AddMarkupContent(0, "<i class=\"dot\"></i>")));
        Assert.NotNull(cut.Find(".atom-tag-icon .dot"));
    }

    [Fact]
    public void Default_appearance_is_solid()
    {
        var cut = Render<AtomTag>(p => p.Add(c => c.Text, "t"));
        Assert.Equal("solid", cut.Find(".atom-tag").GetAttribute("data-appearance"));
    }

    [Fact]
    public void Variant_and_appearance_set_data_attributes()
    {
        var cut = Render<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Variant, BadgeVariant.Danger)
            .Add(c => c.Appearance, BadgeAppearance.Soft));
        var tag = cut.Find(".atom-tag");
        Assert.Equal("danger", tag.GetAttribute("data-variant"));
        Assert.Equal("soft", tag.GetAttribute("data-appearance"));
    }

    [Fact]
    public void Not_clickable_no_role_or_tabindex()
    {
        var cut = Render<AtomTag>(p => p.Add(c => c.Text, "t"));
        var tag = cut.Find(".atom-tag");
        Assert.Null(tag.GetAttribute("role"));
        Assert.Null(tag.GetAttribute("tabindex"));
    }

    [Fact]
    public void Removable_fires_onremove()
    {
        var removed = 0;
        var cut = Render<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Removable, true)
            .Add(c => c.OnRemove, () => removed++));
        cut.Find(".atom-tag-remove").Click();
        Assert.Equal(1, removed);
    }

    [Fact]
    public void No_remove_button_by_default()
    {
        var cut = Render<AtomTag>(p => p.Add(c => c.Text, "t"));
        Assert.Empty(cut.FindAll(".atom-tag-remove"));
    }

    [Fact]
    public void Color_override_tokens_emitted()
    {
        var cut = Render<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Background, "#0a0")
            .Add(c => c.Size, 22)
            .Add(c => c.Radius, 3));
        var style = cut.Find(".atom-tag").GetAttribute("style") ?? "";
        Assert.Contains("--tag-bg:#0a0", style);
        Assert.Contains("--tag-size:22px", style);
        Assert.Contains("--tag-radius:3px", style);
    }

    [Fact]
    public void Height_token_emitted_independent_of_size()
    {
        var cut = Render<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Size, 20)
            .Add(c => c.Height, 34));
        var style = cut.Find(".atom-tag").GetAttribute("style") ?? "";
        Assert.Contains("--tag-size:20px", style);
        Assert.Contains("--tag-height:34px", style);
    }

    [Fact]
    public void Font_styling_tokens_emitted()
    {
        var cut = Render<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.FontFamily, "Inter")
            .Add(c => c.FontSize, 15)
            .Add(c => c.FontWeight, "700")
            .Add(c => c.FontStyle, "italic")
            .Add(c => c.LetterSpacing, ".05em")
            .Add(c => c.TextTransform, "uppercase"));
        var style = cut.Find(".atom-tag").GetAttribute("style") ?? "";
        Assert.Contains("--tag-font-family:Inter", style);
        Assert.Contains("--tag-font-size:15px", style);
        Assert.Contains("--tag-font-weight:700", style);
        Assert.Contains("--tag-font-style:italic", style);
        Assert.Contains("--tag-letter-spacing:.05em", style);
        Assert.Contains("--tag-text-transform:uppercase", style);
    }
}
