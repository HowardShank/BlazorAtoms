namespace BlazorAtoms.Badges.Tests;

public class AtomTagTests : TestContext
{
    [Fact]
    public void Renders_label_from_text()
    {
        var cut = RenderComponent<AtomTag>(p => p.Add(c => c.Text, "bug"));
        Assert.Equal("bug", cut.Find(".atom-tag-label").TextContent);
    }

    [Fact]
    public void ChildContent_overrides_text()
    {
        var cut = RenderComponent<AtomTag>(p => p
            .Add(c => c.Text, "ignored")
            .AddChildContent("<em>wontfix</em>"));
        Assert.Contains("<em>wontfix</em>", cut.Find(".atom-tag-label").InnerHtml);
    }

    [Fact]
    public void Icon_slot_renders()
    {
        var cut = RenderComponent<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Icon, b => b.AddMarkupContent(0, "<i class=\"dot\"></i>")));
        Assert.NotNull(cut.Find(".atom-tag-icon .dot"));
    }

    [Fact]
    public void Default_appearance_is_solid()
    {
        var cut = RenderComponent<AtomTag>(p => p.Add(c => c.Text, "t"));
        Assert.Equal("solid", cut.Find(".atom-tag").GetAttribute("data-appearance"));
    }

    [Fact]
    public void Variant_and_appearance_set_data_attributes()
    {
        var cut = RenderComponent<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Variant, Variant.Danger)
            .Add(c => c.Appearance, Appearance.Soft));
        var tag = cut.Find(".atom-tag");
        Assert.Equal("danger", tag.GetAttribute("data-variant"));
        Assert.Equal("soft", tag.GetAttribute("data-appearance"));
    }

    [Fact]
    public void Not_clickable_no_role_or_tabindex()
    {
        var cut = RenderComponent<AtomTag>(p => p.Add(c => c.Text, "t"));
        var tag = cut.Find(".atom-tag");
        Assert.Null(tag.GetAttribute("role"));
        Assert.Null(tag.GetAttribute("tabindex"));
    }

    [Fact]
    public void Removable_fires_onremove()
    {
        var removed = 0;
        var cut = RenderComponent<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Removable, true)
            .Add(c => c.OnRemove, () => removed++));
        cut.Find(".atom-tag-remove").Click();
        Assert.Equal(1, removed);
    }

    [Fact]
    public void No_remove_button_by_default()
    {
        var cut = RenderComponent<AtomTag>(p => p.Add(c => c.Text, "t"));
        Assert.Empty(cut.FindAll(".atom-tag-remove"));
    }

    [Fact]
    public void Color_override_tokens_emitted()
    {
        var cut = RenderComponent<AtomTag>(p => p
            .Add(c => c.Text, "t")
            .Add(c => c.Background, "#0a0")
            .Add(c => c.Size, 22)
            .Add(c => c.Radius, 3));
        var style = cut.Find(".atom-tag").GetAttribute("style") ?? "";
        Assert.Contains("--tag-bg:#0a0", style);
        Assert.Contains("--tag-size:22px", style);
        Assert.Contains("--tag-radius:3px", style);
    }
}
