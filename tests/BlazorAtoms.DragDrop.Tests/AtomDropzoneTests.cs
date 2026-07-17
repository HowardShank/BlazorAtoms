using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.DragDrop.Tests;

/// <summary>
/// bUnit-level tests for <see cref="AtomDropzone{TItem}"/>. Locks in the rendered DOM shape:
/// draggable wrapper per item, spacer top+bottom, root data-* attributes, orientation mapping,
/// group scoping, disabled state, empty-state slot, and event fan-out. Exercising real HTML5 DnD
/// events isn't feasible in bUnit — the interactive behaviour is covered by the DropzoneEngine
/// unit tests, and manual verification runs through the playground.
/// </summary>
public class AtomDropzoneTests
{
    private sealed record Card(string Title);

    private static readonly RenderFragment<Card> CardTemplate = card => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "card-title");
        builder.AddContent(2, card.Title);
        builder.CloseElement();
    };

    [Fact]
    public void Renders_root_with_expected_class_and_orientation_attributes()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate));

        var root = cut.Find(".atom-dropzone");
        Assert.Equal("vertical", root.GetAttribute("data-orientation"));
        Assert.Equal("list", root.GetAttribute("role"));
    }

    [Fact]
    public void Renders_one_draggable_wrapper_per_item()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A"), new("B"), new("C") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate));

        var wrappers = cut.FindAll(".atom-dropzone-item");
        Assert.Equal(3, wrappers.Count);
        Assert.All(wrappers, w => Assert.Equal("true", w.GetAttribute("draggable")));
    }

    [Fact]
    public void Renders_spacer_above_first_item_and_below_every_item()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A"), new("B") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate));

        var spacers = cut.FindAll(".atom-dropzone-spacer");
        // one above index 0, one below each of the two items = 3.
        Assert.Equal(3, spacers.Count);
    }

    [Fact]
    public void Renders_empty_state_when_items_is_empty()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card>())
            .Add(x => x.ChildContent, CardTemplate));

        Assert.NotNull(cut.Find(".atom-dropzone-empty"));
        Assert.Contains("Drop here", cut.Markup);
    }

    [Fact]
    public void EmptyContent_slot_overrides_default_empty_label()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, new List<Card>())
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.EmptyContent, (RenderFragment)(b => b.AddContent(0, "Nothing here"))));

        Assert.Contains("Nothing here", cut.Markup);
        Assert.DoesNotContain("Drop here", cut.Markup);
    }

    [Theory]
    [InlineData(DropzoneOrientation.Vertical, "vertical")]
    [InlineData(DropzoneOrientation.Horizontal, "horizontal")]
    [InlineData(DropzoneOrientation.Grid, "grid")]
    public void Orientation_maps_to_data_orientation_attribute(DropzoneOrientation orientation, string expected)
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.Orientation, orientation));

        Assert.Equal(expected, cut.Find(".atom-dropzone").GetAttribute("data-orientation"));
    }

    [Fact]
    public void Group_flows_through_to_data_group_attribute()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.Group, "cards"));

        Assert.Equal("cards", cut.Find(".atom-dropzone").GetAttribute("data-group"));
    }

    [Fact]
    public void AllowsDrag_false_disables_draggable_and_marks_wrapper_nodrag()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.AllowsDrag, _ => false));

        var wrapper = cut.Find(".atom-dropzone-item");
        Assert.Equal("false", wrapper.GetAttribute("draggable"));
        Assert.Contains("atom-dropzone-nodrag", wrapper.GetAttribute("class"));
    }

    [Fact]
    public void Disabled_zone_marks_every_wrapper_undraggable()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A"), new("B") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.Disabled, true));

        foreach (var w in cut.FindAll(".atom-dropzone-item"))
            Assert.Equal("false", w.GetAttribute("draggable"));
    }

    [Fact]
    public void ItemWrapperClass_delegate_appends_extra_classes_per_item()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A"), new("B") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.ItemWrapperClass, c => c.Title == "A" ? "highlight" : ""));

        var wrappers = cut.FindAll(".atom-dropzone-item");
        Assert.Contains("highlight", wrappers[0].GetAttribute("class"));
        Assert.DoesNotContain("highlight", wrappers[1].GetAttribute("class"));
    }

    [Fact]
    public void Visible_false_emits_display_none_in_root_style()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-dropzone").GetAttribute("style") ?? "");
    }

    [Fact]
    public void HighlightColor_DenyColor_Gap_emit_custom_properties()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.HighlightColor, "#00ff00")
            .Add(x => x.DenyColor, "#ff0000")
            .Add(x => x.Gap, "1rem"));

        var style = cut.Find(".atom-dropzone").GetAttribute("style") ?? "";
        Assert.Contains("--dropzone-highlight-color:#00ff00", style);
        Assert.Contains("--dropzone-deny-color:#ff0000", style);
        Assert.Contains("--dropzone-gap:1rem", style);
    }

    [Fact]
    public void Footer_content_renders_after_items()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("A") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate)
            .Add(x => x.Footer, (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.AddAttribute(1, "class", "custom-footer");
                b.AddContent(2, "Add card");
                b.CloseElement();
            })));

        Assert.NotNull(cut.Find(".custom-footer"));
        Assert.Contains("Add card", cut.Markup);
    }

    [Fact]
    public void ChildContent_template_receives_each_item()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var items = new List<Card> { new("Alpha"), new("Beta") };

        var cut = ctx.RenderComponent<AtomDropzone<Card>>(p => p
            .Add(x => x.Items, items)
            .Add(x => x.ChildContent, CardTemplate));

        Assert.Contains("Alpha", cut.Markup);
        Assert.Contains("Beta", cut.Markup);
    }

    [Fact]
    public void Read_only_Items_throws_on_render()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        IList<Card> readOnly = new List<Card> { new("A") }.AsReadOnly();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ctx.RenderComponent<AtomDropzone<Card>>(p => p
                .Add(x => x.Items, readOnly)
                .Add(x => x.ChildContent, CardTemplate)));

        Assert.Contains("mutable", ex.Message);
    }

    [Fact]
    public void AtomDropzoneGroup_cascades_context_to_children()
    {
        // A group wraps two zones — both mount without error and the context resolves.
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var a = new List<Card> { new("A") };
        var b = new List<Card> { new("B") };

        var cut = ctx.RenderComponent<AtomDropzoneGroup<Card>>(p => p
            .Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<AtomDropzone<Card>>(0);
                builder.AddAttribute(1, "Items", a);
                builder.AddAttribute(2, "ChildContent", CardTemplate);
                builder.CloseComponent();
                builder.OpenComponent<AtomDropzone<Card>>(3);
                builder.AddAttribute(4, "Items", b);
                builder.AddAttribute(5, "ChildContent", CardTemplate);
                builder.CloseComponent();
            })));

        Assert.Equal(2, cut.FindAll(".atom-dropzone").Count);
    }
}
