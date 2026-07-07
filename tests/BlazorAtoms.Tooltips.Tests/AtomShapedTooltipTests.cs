namespace BlazorAtoms.Tooltips.Tests;

public class AtomShapedTooltipTests : TestContext
{
    [Fact]
    public void Renders_trigger_bubble_and_svg()
    {
        var cut = RenderComponent<AtomShapedTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Contains("Trigger", cut.Markup);
        Assert.Contains("Hello", cut.Markup);
        Assert.NotNull(cut.Find("[role='tooltip']"));
        // Outline is an inline SVG.
        Assert.NotNull(cut.Find(".atom-stooltip-svg"));
        Assert.NotNull(cut.Find(".ast-path"));
    }

    [Fact]
    public void Aria_describedby_matches_bubble_id()
    {
        var cut = RenderComponent<AtomShapedTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        var describedBy = cut.Find(".atom-stooltip-trigger").GetAttribute("aria-describedby");
        Assert.Equal(cut.Find("[role='tooltip']").GetAttribute("id"), describedBy);
    }

    [Theory]
    [InlineData(Placement.Top, "top")]
    [InlineData(Placement.BottomEnd, "bottom-end")]
    [InlineData(Placement.TopLeft, "top-left")]
    public void Placement_sets_data_placement(Placement placement, string expected)
    {
        var cut = RenderComponent<AtomShapedTooltip>(p => p
            .Add(c => c.Placement, placement)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal(expected, cut.Find("[role='tooltip']").GetAttribute("data-placement"));
    }

    [Theory]
    [InlineData(ShapedTooltipShape.Rectangle, "rectangle")]
    [InlineData(ShapedTooltipShape.Pill, "pill")]
    [InlineData(ShapedTooltipShape.Ellipse, "ellipse")]
    [InlineData(ShapedTooltipShape.Cloud, "cloud")]
    [InlineData(ShapedTooltipShape.Burst, "burst")]
    [InlineData(ShapedTooltipShape.FoldedCorner, "folded")]
    public void Shape_sets_data_shape_and_renders_svg_path(ShapedTooltipShape shape, string expected)
    {
        var cut = RenderComponent<AtomShapedTooltip>(p => p
            .Add(c => c.Shape, shape)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal(expected, cut.Find("[role='tooltip']").GetAttribute("data-shape"));
        // Every shape draws an SVG outline path (so border/fill apply uniformly).
        Assert.NotNull(cut.Find(".ast-path"));
    }

    [Theory]
    [InlineData(ShapedTooltipShape.Burst)]
    [InlineData(ShapedTooltipShape.FoldedCorner)]
    public void Clip_style_shapes_have_no_separate_arrow(ShapedTooltipShape shape)
    {
        var cut = RenderComponent<AtomShapedTooltip>(p => p
            .Add(c => c.Shape, shape)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Empty(cut.FindAll(".atom-stooltip-arrow"));
    }

    [Fact]
    public void Disabled_suppresses_bubble()
    {
        var cut = RenderComponent<AtomShapedTooltip>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Contains("Trigger", cut.Markup);
        Assert.Empty(cut.FindAll("[role='tooltip']"));
    }

    [Fact]
    public void Cursor_placement_loads_js_module()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = RenderComponent<AtomShapedTooltip>(p => p
            .Add(c => c.Placement, Placement.Cursor)
            .Add(c => c.Text, "Follows cursor")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal("cursor", cut.Find("[role='tooltip']").GetAttribute("data-placement"));
        Assert.Contains(JSInterop.Invocations, i =>
            i.Identifier == "import" &&
            i.Arguments.Count > 0 &&
            (i.Arguments[0] as string) == "./_content/BlazorAtoms.Tooltips/atom-shaped-tooltip.js");
    }
}
