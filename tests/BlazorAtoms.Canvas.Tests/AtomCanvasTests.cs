namespace BlazorAtoms.Canvas.Tests;

// Markup contract + the JS-interop wiring (mocked). The pointer gesture itself is JS and can't run in
// bUnit, so we exercise the [JSInvokable] callbacks directly — that is the JS -> C# half of the contract.
public class AtomCanvasTests : TestContext
{
    public AtomCanvasTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_canvas_with_size_role_and_aria()
    {
        var cut = RenderComponent<AtomCanvas>(p => p
            .Add(c => c.Width, 320)
            .Add(c => c.Height, 200)
            .Add(c => c.AriaLabel, "Sketch"));

        var canvas = cut.Find("canvas.atom-canvas");
        Assert.Equal("320", canvas.GetAttribute("width"));
        Assert.Equal("200", canvas.GetAttribute("height"));
        Assert.Equal("img", canvas.GetAttribute("role"));
        Assert.Equal("Sketch", canvas.GetAttribute("aria-label"));
        Assert.Equal("static", canvas.GetAttribute("data-mode"));
    }

    [Theory]
    [InlineData(CanvasMode.Static, "static")]
    [InlineData(CanvasMode.Draw, "draw")]
    [InlineData(CanvasMode.Select, "select")]
    public void Mode_sets_data_mode_attribute(CanvasMode mode, string expected)
    {
        var cut = RenderComponent<AtomCanvas>(p => p.Add(c => c.Mode, mode));
        Assert.Equal(expected, cut.Find("canvas").GetAttribute("data-mode"));
    }

    [Fact]
    public void Size_and_background_emit_style_variables()
    {
        var cut = RenderComponent<AtomCanvas>(p => p
            .Add(c => c.Width, 100)
            .Add(c => c.Height, 50)
            .Add(c => c.BackgroundColor, "#eef"));

        var style = cut.Find("canvas").GetAttribute("style")!;
        Assert.Contains("--canvas-w:100px", style);
        Assert.Contains("--canvas-h:50px", style);
        Assert.Contains("--canvas-bg:#eef", style);
    }

    [Fact]
    public void Disabled_sets_data_disabled()
    {
        var cut = RenderComponent<AtomCanvas>(p => p.Add(c => c.Disabled, true));
        Assert.Equal("true", cut.Find("canvas").GetAttribute("data-disabled"));
    }

    [Fact]
    public void Imports_its_own_js_module_on_first_render()
    {
        RenderComponent<AtomCanvas>();

        Assert.Contains(JSInterop.Invocations, i =>
            i.Identifier == "import" &&
            i.Arguments.Count > 0 &&
            (i.Arguments[0] as string) == "./_content/BlazorAtoms.Canvas/atom-canvas.js");
    }

    [Fact]
    public async Task OnStrokeCommitted_appends_path_and_raises_changed_with_new_list()
    {
        IReadOnlyList<CanvasShape>? captured = null;
        var cut = RenderComponent<AtomCanvas>(p => p
            .Add(c => c.Mode, CanvasMode.Draw)
            .Add(c => c.Shapes, new List<CanvasShape>())
            .Add(c => c.ShapesChanged,
                EventCallback.Factory.Create<IReadOnlyList<CanvasShape>>(this, v => captured = v)));

        await cut.InvokeAsync(() =>
            cut.Instance.OnStrokeCommitted(new[] { new CanvasPoint(1, 1), new CanvasPoint(5, 5) }));

        Assert.NotNull(captured);
        var path = Assert.IsType<CanvasPath>(Assert.Single(captured!));
        Assert.Equal(2, path.Points.Count);
    }

    [Fact]
    public async Task OnShapeMoved_translates_matching_shape()
    {
        var rect = new CanvasRect(10, 10, 40, 20);
        IReadOnlyList<CanvasShape>? captured = null;
        var cut = RenderComponent<AtomCanvas>(p => p
            .Add(c => c.Mode, CanvasMode.Select)
            .Add(c => c.Shapes, new List<CanvasShape> { rect })
            .Add(c => c.ShapesChanged,
                EventCallback.Factory.Create<IReadOnlyList<CanvasShape>>(this, v => captured = v)));

        await cut.InvokeAsync(() => cut.Instance.OnShapeMoved(rect.Id, 5, 7));

        var moved = Assert.IsType<CanvasRect>(Assert.Single(captured!));
        Assert.Equal(15, moved.X);
        Assert.Equal(17, moved.Y);
        Assert.Equal(rect.Id, moved.Id);
    }

    [Fact]
    public async Task OnShapeClicked_raises_callback_with_id()
    {
        string? clicked = null;
        var cut = RenderComponent<AtomCanvas>(p => p
            .Add(c => c.OnShapeClick, EventCallback.Factory.Create<string>(this, id => clicked = id)));

        await cut.InvokeAsync(() => cut.Instance.OnShapeClicked("s123"));

        Assert.Equal("s123", clicked);
    }
}
