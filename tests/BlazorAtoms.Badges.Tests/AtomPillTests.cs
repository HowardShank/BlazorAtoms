namespace BlazorAtoms.Badges.Tests;

public class AtomPillTests : TestContext
{
    [Fact]
    public void Renders_label_from_text()
    {
        var cut = RenderComponent<AtomPill>(p => p.Add(c => c.Text, "Active"));
        Assert.Equal("Active", cut.Find(".atom-pill-label").TextContent);
    }

    [Fact]
    public void Dot_shown_by_default()
    {
        var cut = RenderComponent<AtomPill>(p => p.Add(c => c.Text, "Active"));
        Assert.NotNull(cut.Find(".atom-pill-dot"));
    }

    [Fact]
    public void Dot_hidden_when_false()
    {
        var cut = RenderComponent<AtomPill>(p => p
            .Add(c => c.Text, "Active")
            .Add(c => c.Dot, false));
        Assert.Empty(cut.FindAll(".atom-pill-dot"));
    }

    [Fact]
    public void Icon_replaces_dot()
    {
        var cut = RenderComponent<AtomPill>(p => p
            .Add(c => c.Text, "Active")
            .Add(c => c.Icon, b => b.AddMarkupContent(0, "<i class=\"ico\"></i>")));
        Assert.NotNull(cut.Find(".atom-pill-icon .ico"));
        Assert.Empty(cut.FindAll(".atom-pill-dot"));
    }

    [Fact]
    public void Has_status_role()
    {
        var cut = RenderComponent<AtomPill>(p => p.Add(c => c.Text, "Active"));
        Assert.Equal("status", cut.Find(".atom-pill").GetAttribute("role"));
    }

    [Fact]
    public void Default_appearance_is_soft()
    {
        var cut = RenderComponent<AtomPill>(p => p.Add(c => c.Text, "Active"));
        Assert.Equal("soft", cut.Find(".atom-pill").GetAttribute("data-appearance"));
    }

    [Fact]
    public void Variant_sets_data_attribute()
    {
        var cut = RenderComponent<AtomPill>(p => p
            .Add(c => c.Text, "Active")
            .Add(c => c.Variant, Variant.Success));
        Assert.Equal("success", cut.Find(".atom-pill").GetAttribute("data-variant"));
    }

    [Fact]
    public void Color_override_tokens_emitted()
    {
        var cut = RenderComponent<AtomPill>(p => p
            .Add(c => c.Text, "Active")
            .Add(c => c.Background, "#222")
            .Add(c => c.TextColor, "#eee")
            .Add(c => c.Size, 28));
        var style = cut.Find(".atom-pill").GetAttribute("style") ?? "";
        Assert.Contains("--pill-bg:#222", style);
        Assert.Contains("--pill-color:#eee", style);
        Assert.Contains("--pill-size:28px", style);
    }
}
