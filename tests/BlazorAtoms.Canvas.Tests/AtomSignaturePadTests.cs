namespace BlazorAtoms.Canvas.Tests;

// AtomSignaturePad is a preset over AtomCanvas Draw mode. Verify it renders that inner canvas correctly,
// forwards its knobs, and that Clear() empties the model and resets the bound Value.
public class AtomSignaturePadTests : TestContext
{
    public AtomSignaturePadTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Renders_inner_canvas_in_draw_mode()
    {
        var cut = RenderComponent<AtomSignaturePad>(p => p.Add(c => c.AriaLabel, "Sign here"));

        var canvas = cut.Find("canvas.atom-canvas");
        Assert.Equal("draw", canvas.GetAttribute("data-mode"));
        Assert.Equal("Sign here", canvas.GetAttribute("aria-label"));
    }

    [Fact]
    public void Forwards_size_and_background_to_the_canvas()
    {
        var cut = RenderComponent<AtomSignaturePad>(p => p
            .Add(c => c.Width, 500)
            .Add(c => c.Height, 200)
            .Add(c => c.BackgroundColor, "#eee"));

        var style = cut.Find("canvas").GetAttribute("style")!;
        Assert.Contains("--canvas-w:500px", style);
        Assert.Contains("--canvas-h:200px", style);
        Assert.Contains("--canvas-bg:#eee", style);
    }

    [Fact]
    public async Task Clear_empties_strokes_and_resets_value()
    {
        string? value = "seed";
        IReadOnlyList<CanvasShape>? strokes =
            new List<CanvasShape> { new CanvasPath(new List<CanvasPoint> { new(0, 0), new(1, 1) }) };

        var cut = RenderComponent<AtomSignaturePad>(p => p
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string>(this, v => value = v))
            .Add(c => c.Strokes, strokes)
            .Add(c => c.StrokesChanged,
                EventCallback.Factory.Create<IReadOnlyList<CanvasShape>>(this, v => strokes = v)));

        Assert.False(cut.Instance.IsEmpty);

        await cut.InvokeAsync(() => cut.Instance.Clear());

        // Two-way bind: the callbacks fired; feed the new values back like a parent would.
        cut.SetParametersAndRender(p => p
            .Add(c => c.Value, value)
            .Add(c => c.Strokes, strokes));

        Assert.Empty(strokes!);
        Assert.Equal("", value);
        Assert.True(cut.Instance.IsEmpty);
    }
}
