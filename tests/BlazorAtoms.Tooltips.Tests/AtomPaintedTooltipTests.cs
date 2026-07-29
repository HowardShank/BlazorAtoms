namespace BlazorAtoms.Tooltips.Tests;

public class AtomPaintedTooltipTests : BunitContext
{
    [Fact]
    public void Renders_trigger_bubble_and_svg()
    {
        var cut = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Contains("Trigger", cut.Markup);
        Assert.NotNull(cut.Find("[role='tooltip']"));
        Assert.NotNull(cut.Find(".atom-ptooltip-svg"));
        Assert.NotNull(cut.Find(".apt-path"));
    }

    [Theory]
    [InlineData(PaintedTooltipShape.Rectangle, "rectangle")]
    [InlineData(PaintedTooltipShape.Cloud, "cloud")]
    [InlineData(PaintedTooltipShape.Burst, "burst")]
    [InlineData(PaintedTooltipShape.FoldedCorner, "folded")]
    public void Shape_sets_data_shape_and_renders_path(PaintedTooltipShape shape, string expected)
    {
        var cut = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.Shape, shape)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal(expected, cut.Find("[role='tooltip']").GetAttribute("data-shape"));
        Assert.NotNull(cut.Find(".apt-path"));
    }

    [Fact]
    public void No_gradient_renders_no_defs_and_solid_fill()
    {
        var cut = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Empty(cut.FindAll("linearGradient"));
        // No inline fill override → path falls back to the CSS token fill.
        var fillStyle = cut.Find(".apt-path").GetAttribute("style");
        Assert.True(string.IsNullOrEmpty(fillStyle) || !fillStyle.Contains("url("));
    }

    [Fact]
    public void Gradient_renders_defs_and_path_references_it()
    {
        var cut = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .Add(c => c.GradientFrom, "#f97316")
            .Add(c => c.GradientTo, "#7c3aed")
            .AddChildContent("<button>Trigger</button>"));

        var grad = cut.Find("linearGradient");
        var gradId = grad.GetAttribute("id");
        Assert.False(string.IsNullOrEmpty(gradId));

        // Two stops with the requested colors.
        var stops = cut.FindAll("linearGradient stop");
        Assert.Equal(2, stops.Count);
        Assert.Equal("#f97316", stops[0].GetAttribute("stop-color"));
        Assert.Equal("#7c3aed", stops[1].GetAttribute("stop-color"));

        // Path fill points at the gradient by id.
        var fillStyle = cut.Find(".apt-path").GetAttribute("style");
        Assert.Contains($"url(#{gradId})", fillStyle);
    }

    [Fact]
    public void Shadow_toggles_has_shadow_class()
    {
        var on = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));
        Assert.Contains("has-shadow", on.Find("[role='tooltip']").GetAttribute("class"));

        var off = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.Shadow, false)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));
        Assert.DoesNotContain("has-shadow", off.Find("[role='tooltip']").GetAttribute("class"));
    }

    [Fact]
    public void No_alignment_set_omits_align_attributes()
    {
        var cut = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        var bubble = cut.Find("[role='tooltip']");
        Assert.False(bubble.HasAttribute("data-halign"));
        Assert.False(bubble.HasAttribute("data-valign"));
    }

    [Theory]
    [InlineData(TooltipTextAlign.Start, "start")]
    [InlineData(TooltipTextAlign.Center, "center")]
    [InlineData(TooltipTextAlign.End, "end")]
    public void TextAlign_sets_data_halign(TooltipTextAlign align, string expected)
    {
        var cut = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.TextAlign, align)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal(expected, cut.Find("[role='tooltip']").GetAttribute("data-halign"));
    }

    [Theory]
    [InlineData(TooltipVerticalAlign.Top, "top")]
    [InlineData(TooltipVerticalAlign.Center, "center")]
    [InlineData(TooltipVerticalAlign.Bottom, "bottom")]
    public void VerticalAlign_sets_data_valign(TooltipVerticalAlign align, string expected)
    {
        var cut = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.VerticalAlign, align)
            .Add(c => c.Text, "Hello")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal(expected, cut.Find("[role='tooltip']").GetAttribute("data-valign"));
    }

    [Fact]
    public void Cursor_placement_loads_js_module()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<AtomPaintedTooltip>(p => p
            .Add(c => c.Placement, TooltipPlacement.Cursor)
            .Add(c => c.Text, "Follows cursor")
            .AddChildContent("<button>Trigger</button>"));

        Assert.Equal("cursor", cut.Find("[role='tooltip']").GetAttribute("data-placement"));
        Assert.Contains(JSInterop.Invocations, i =>
            i.Identifier == "import" &&
            i.Arguments.Count > 0 &&
            (i.Arguments[0] as string) == "./_content/BlazorAtoms.Tooltips/atom-painted-tooltip.js");
    }
}
