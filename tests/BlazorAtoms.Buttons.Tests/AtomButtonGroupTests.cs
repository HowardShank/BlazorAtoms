namespace BlazorAtoms.Buttons.Tests;

public class AtomButtonGroupTests : BunitContext
{
    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_a_labelled_group_role_around_its_children()
    {
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.AriaLabel, "Text alignment")
            .AddChildContent<AtomButton>(b => b.Add(x => x.Text, "Left"))
            .AddChildContent<AtomButton>(b => b.Add(x => x.Text, "Right")));

        var group = cut.Find(".atom-button-group");
        Assert.Equal("group", group.GetAttribute("role"));
        Assert.Equal("Text alignment", group.GetAttribute("aria-label"));
        Assert.Equal(2, cut.FindAll("button").Count);
    }

    [Fact]
    public void Defaults_to_an_attached_horizontal_row()
    {
        var cut = Render<AtomButtonGroup>(p => p
            .AddChildContent<AtomButton>(b => b.Add(x => x.Text, "One")));

        var group = cut.Find(".atom-button-group");
        Assert.Equal("horizontal", group.GetAttribute("data-orientation"));
        Assert.Equal("true", group.GetAttribute("data-attached"));
    }

    [Theory]
    [InlineData(ButtonGroupOrientation.Horizontal, "horizontal")]
    [InlineData(ButtonGroupOrientation.Vertical, "vertical")]
    public void Orientation_emits_data_orientation(ButtonGroupOrientation orientation, string expected)
    {
        var cut = Render<AtomButtonGroup>(p => p.Add(c => c.Orientation, orientation));
        Assert.Equal(expected, cut.Find(".atom-button-group").GetAttribute("data-orientation"));
    }

    [Fact]
    public void Attached_false_and_Gap_drive_the_spaced_layout()
    {
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.Attached, false)
            .Add(c => c.Gap, 12d));

        var group = cut.Find(".atom-button-group");
        Assert.Equal("false", group.GetAttribute("data-attached"));
        Assert.Contains("--btn-group-gap:12px;", group.GetAttribute("style")!);
    }

    [Fact]
    public void FullWidth_only_emits_when_set()
    {
        Assert.Null(Render<AtomButtonGroup>().Find(".atom-button-group").GetAttribute("data-full-width"));

        Assert.Equal("true", Render<AtomButtonGroup>(p => p.Add(c => c.FullWidth, true))
            .Find(".atom-button-group").GetAttribute("data-full-width"));
    }

    [Fact]
    public void Visible_false_hides_via_display_none()
    {
        var cut = Render<AtomButtonGroup>(p => p.Add(c => c.Visible, false));
        Assert.Contains("display:none", cut.Find(".atom-button-group").GetAttribute("style")!);
    }

    [Fact]
    public void Empty_group_renders_without_throwing()
    {
        Assert.NotNull(Render<AtomButtonGroup>().Find(".atom-button-group"));
    }

    // ---- cascade -----------------------------------------------------------------------------

    [Fact]
    public void Children_inherit_the_groups_axes()
    {
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.Variant, ButtonVariant.Success)
            .Add(c => c.Appearance, ButtonAppearance.Outline)
            .Add(c => c.Size, ButtonSize.Large)
            .Add(c => c.Shape, ButtonShape.Pill)
            .AddChildContent<AtomButton>(b => b.Add(x => x.Text, "One")));

        var button = cut.Find("button");
        Assert.Equal("success", button.GetAttribute("data-variant"));
        Assert.Equal("outline", button.GetAttribute("data-appearance"));
        Assert.Equal("large", button.GetAttribute("data-size"));
        Assert.Equal("pill", button.GetAttribute("data-shape"));
    }

    [Fact]
    public void A_childs_own_value_beats_the_group()
    {
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.Variant, ButtonVariant.Success)
            .AddChildContent<AtomButton>(b => b
                .Add(x => x.Text, "Danger")
                .Add(x => x.Variant, ButtonVariant.Danger)));

        Assert.Equal("danger", cut.Find("button").GetAttribute("data-variant"));
    }

    [Fact]
    public void An_explicit_value_that_equals_the_enum_default_still_beats_the_group()
    {
        // The reason ButtonFamilyBase tracks supplied parameter names instead of comparing against the
        // enum defaults: Size="Medium" inside a Large group must stay Medium.
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.Size, ButtonSize.Large)
            .AddChildContent<AtomButton>(b => b
                .Add(x => x.Text, "Medium")
                .Add(x => x.Size, ButtonSize.Medium)));

        Assert.Equal("medium", cut.Find("button").GetAttribute("data-size"));
    }

    [Fact]
    public void Effect_is_not_cascaded()
    {
        // A group-wide effect (seven rainbow buttons) is nobody's intent, so Effect stays per-button.
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.Variant, ButtonVariant.Primary)
            .AddChildContent<AtomButton>(b => b
                .Add(x => x.Text, "One")
                .Add(x => x.Effect, ButtonEffect.Bevel))
            .AddChildContent<AtomButton>(b => b.Add(x => x.Text, "Two")));

        var buttons = cut.FindAll("button");
        Assert.Equal("bevel", buttons[0].GetAttribute("data-effect"));
        Assert.Null(buttons[1].GetAttribute("data-effect"));
    }

    [Fact]
    public void Icon_buttons_inherit_the_group_axes_too()
    {
        // AtomIconButton forwards already-resolved axes, so the cascade has to survive that hop.
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.Variant, ButtonVariant.Warning)
            .Add(c => c.Size, ButtonSize.Small)
            .AddChildContent<AtomIconButton>(b => b.Add(x => x.AriaLabel, "Bold")));

        var button = cut.Find("button");
        Assert.Equal("warning", button.GetAttribute("data-variant"));
        Assert.Equal("small", button.GetAttribute("data-size"));
    }

    [Fact]
    public void A_changed_group_axis_reaches_already_rendered_children()
    {
        // The cascade is IsFixed="false" and the context is rebuilt per render; a fixed cascade would
        // leave the children on the original value.
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.Variant, ButtonVariant.Info)
            .AddChildContent<AtomButton>(b => b.Add(x => x.Text, "One")));

        Assert.Equal("info", cut.Find("button").GetAttribute("data-variant"));

        cut.Render(p => p.Add(c => c.Variant, ButtonVariant.Danger));

        Assert.Equal("danger", cut.Find("button").GetAttribute("data-variant"));
    }

    [Fact]
    public void CssClass_Style_and_splat_land_on_the_group_root()
    {
        var cut = Render<AtomButtonGroup>(p => p
            .Add(c => c.CssClass, "mine")
            .Add(c => c.Style, "margin:1rem;")
            .Add(c => c.Gap, 4d)
            .AddUnmatched("title", "hi"));

        var group = cut.Find(".atom-button-group");
        Assert.Equal("atom-button-group mine", group.GetAttribute("class"));
        Assert.Equal("hi", group.GetAttribute("title"));
        Assert.EndsWith("margin:1rem;", group.GetAttribute("style"));
    }
}
