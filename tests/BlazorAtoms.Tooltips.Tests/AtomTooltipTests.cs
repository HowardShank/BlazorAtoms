namespace BlazorAtoms.Tooltips.Tests;

// Smoke tests — prove the trigger/bubble markup, aria wiring, placement, arrow, and
// disabled-state behavior are wired correctly. No JS to test: everything is static Razor
// output plus CSS, so bUnit's rendered markup is the full contract.
public class AtomTooltipTests : TestContext
{
    [Fact]
    public void Renders_trigger_and_bubble()
    {
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Contains("Trigger", cut.Markup);
        Assert.Contains("Hello", cut.Markup);
        Assert.NotNull(cut.Find("[role='tooltip']"));
    }

    [Fact]
    public void Aria_describedby_matches_bubble_id()
    {
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        var describedBy = cut.Find(".atom-tooltip-trigger").GetAttribute("aria-describedby");
        var bubbleId = cut.Find("[role='tooltip']").GetAttribute("id");

        Assert.False(string.IsNullOrEmpty(describedBy));
        Assert.Equal(bubbleId, describedBy);
    }

    [Theory]
    [InlineData(Placement.Top, "top")]
    [InlineData(Placement.BottomStart, "bottom-start")]
    [InlineData(Placement.RightEnd, "right-end")]
    [InlineData(Placement.TopLeft, "top-left")]
    [InlineData(Placement.TopRight, "top-right")]
    [InlineData(Placement.BottomLeft, "bottom-left")]
    [InlineData(Placement.BottomRight, "bottom-right")]
    public void Placement_sets_data_placement_attribute(Placement placement, string expected)
    {
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Placement, placement)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal(expected, cut.Find("[role='tooltip']").GetAttribute("data-placement"));
    }

    [Theory]
    [InlineData(Shape.Rectangle, "rectangle")]
    [InlineData(Shape.Pill, "pill")]
    [InlineData(Shape.Ellipse, "ellipse")]
    [InlineData(Shape.Thought, "thought")]
    [InlineData(Shape.Burst, "burst")]
    [InlineData(Shape.FoldedCorner, "folded")]
    public void Shape_sets_data_shape_attribute(Shape shape, string expected)
    {
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Shape, shape)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal(expected, cut.Find("[role='tooltip']").GetAttribute("data-shape"));
    }

    [Theory]
    [InlineData(Shape.Burst)]
    [InlineData(Shape.FoldedCorner)]
    public void Clip_path_shapes_suppress_arrow(Shape shape)
    {
        // clip-path clips the arrow away, so it isn't rendered even with ShowArrow=true (default).
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Shape, shape)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Empty(cut.FindAll(".atom-tooltip-arrow"));
    }

    [Fact]
    public void Thought_shape_keeps_arrow_element_for_trail()
    {
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Shape, Shape.Thought)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        // Thought reuses the arrow element (CSS turns it into the circle trail).
        Assert.Single(cut.FindAll(".atom-tooltip-arrow"));
    }

    [Fact]
    public void Cursor_placement_loads_js_module_and_attaches()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Placement, Placement.Cursor)
            .Add(c => c.Text, "Follows cursor")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal("cursor", cut.Find("[role='tooltip']").GetAttribute("data-placement"));
        // Cursor mode has no fixed edge → no arrow even though ShowArrow defaults true.
        Assert.Empty(cut.FindAll(".atom-tooltip-arrow"));
        // The component lazy-imported its own JS module (rather than needing DI/script setup).
        Assert.Contains(JSInterop.Invocations, i =>
            i.Identifier == "import" &&
            i.Arguments.Count > 0 &&
            (i.Arguments[0] as string) == "./_content/BlazorAtoms.Tooltips/atom-tooltip.js");
    }

    [Fact]
    public void ShowArrow_false_omits_arrow_element()
    {
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.ShowArrow, false)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Empty(cut.FindAll(".atom-tooltip-arrow"));
    }

    [Fact]
    public void Disabled_suppresses_bubble_but_keeps_trigger()
    {
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Contains("Trigger", cut.Markup);
        Assert.Empty(cut.FindAll("[role='tooltip']"));
        Assert.Null(cut.Find(".atom-tooltip-trigger").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void TooltipContent_takes_priority_over_Text()
    {
        var cut = RenderComponent<AtomTooltip>(p => p
            .Add(c => c.Text, "Ignored")
            .Add(c => c.TooltipContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<strong>Rich</strong>")))
            .AddChildContent("<button>Trigger</button>"));

        Assert.Contains("Rich", cut.Markup);
        Assert.DoesNotContain("Ignored", cut.Markup);
    }
}
