using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorAtoms.Canvas.Tests;

// The studio is a composite over AtomCanvas. bUnit can't run the real canvas/pointer JS, so we drive the
// public/imperative API (which the toolbar + context both call) and assert model state, tool->mode mapping,
// history, layers, JSON round-trip, and the slot/context extension points.
public class AtomCanvasStudioTests : TestContext
{
    public AtomCanvasStudioTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private EventCallback<IReadOnlyList<CanvasShape>> Capture(Action<IReadOnlyList<CanvasShape>> a)
        => EventCallback.Factory.Create(this, a);

    [Fact]
    public void Default_toolbar_and_canvas_render()
    {
        var cut = RenderComponent<AtomCanvasStudio>();
        Assert.NotNull(cut.Find(".acs-toolbar"));
        Assert.Contains("Select", cut.Markup);
        Assert.Contains("Draw", cut.Markup);
        Assert.Single(cut.FindAll("canvas.atom-canvas"));
        Assert.NotNull(cut.Find(".acs-layers"));   // default layers panel
        Assert.NotNull(cut.Find(".acs-status"));   // default status bar
    }

    [Theory]
    [InlineData(CanvasTool.Select, "select")]
    [InlineData(CanvasTool.Draw, "draw")]
    [InlineData(CanvasTool.Pan, "pan")]
    [InlineData(CanvasTool.Insert, "static")]
    [InlineData(CanvasTool.Erase, "static")]
    public void Tool_maps_to_canvas_mode(CanvasTool tool, string mode)
    {
        var cut = RenderComponent<AtomCanvasStudio>(p => p.Add(c => c.Tool, tool));
        Assert.Equal(mode, cut.Find("canvas.atom-canvas").GetAttribute("data-mode"));
    }

    [Fact]
    public async Task Add_undo_redo_round_trip()
    {
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = RenderComponent<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(new CanvasRect(0, 0, 10, 10)));
        Assert.Single(cur!);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.Empty(cur!);

        await cut.InvokeAsync(() => cut.Instance.RedoAsync());
        Assert.Single(cur!);
    }

    [Fact]
    public async Task Delete_selected_removes_the_shape()
    {
        var rect = new CanvasRect(0, 0, 10, 10);
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = RenderComponent<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(rect));
        await cut.InvokeAsync(() => cut.Instance.SelectShape(rect.Id));
        await cut.InvokeAsync(() => cut.Instance.DeleteSelectedAsync());

        Assert.Empty(cur!);
    }

    [Fact]
    public async Task Bring_to_front_reorders_the_model()
    {
        var a = new CanvasRect(0, 0, 1, 1);
        var b = new CanvasCircle(5, 5, 2);
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = RenderComponent<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(a)); // [a]
        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(b)); // [a, b]
        await cut.InvokeAsync(() => cut.Instance.BringToFrontAsync(a.Id)); // [b, a]

        Assert.Equal(b.Id, cur![0].Id);
        Assert.Equal(a.Id, cur![^1].Id);
    }

    [Fact]
    public async Task Toggle_visibility_flips_the_flag()
    {
        var rect = new CanvasRect(0, 0, 1, 1);
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = RenderComponent<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(rect));
        await cut.InvokeAsync(() => cut.Instance.ToggleVisibleAsync(rect.Id));

        Assert.False(cur!.Single().Visible);
    }

    [Fact]
    public async Task Save_then_load_json_round_trips_the_model()
    {
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = RenderComponent<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));
        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(new CanvasRect(1, 2, 3, 4) { Fill = "#abc" }));
        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(new CanvasCircle(5, 5, 3)));
        var json = cut.Instance.SaveJson();

        var cut2 = RenderComponent<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));
        var ok = false;
        await cut2.InvokeAsync(async () => ok = await cut2.Instance.LoadJsonAsync(json));

        Assert.True(ok);
        Assert.Equal(2, cur!.Count);
        Assert.IsType<CanvasRect>(cur![0]);
        Assert.IsType<CanvasCircle>(cur![1]);
    }

    [Fact]
    public async Task Load_invalid_json_returns_false()
    {
        var cut = RenderComponent<AtomCanvasStudio>();
        var ok = true;
        await cut.InvokeAsync(async () => ok = await cut.Instance.LoadJsonAsync("{ not json"));
        Assert.False(ok);
    }

    [Fact]
    public void Toolbar_slot_replaces_the_default_toolbar()
    {
        var cut = RenderComponent<AtomCanvasStudio>(p => p
            .Add(c => c.Toolbar, (AtomCanvasStudioContext ctx) =>
                (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-tb\">mine</div>"))));

        Assert.NotEmpty(cut.FindAll(".custom-tb"));
        Assert.Empty(cut.FindAll(".acs-group")); // default groups are gone
    }

    [Fact]
    public void ToolbarEnd_slot_receives_the_studio_context()
    {
        var cut = RenderComponent<AtomCanvasStudio>(p => p
            .Add(c => c.ToolbarEnd, (AtomCanvasStudioContext ctx) =>
                (RenderFragment)(b => b.AddMarkupContent(0, $"<span class=\"cnt\">{ctx.ShapeCount}</span>"))));

        Assert.Equal("0", cut.Find(".cnt").TextContent);
    }

    [Fact]
    public void Custom_stamps_surface_in_the_palette()
    {
        var stamps = new[] { new CanvasStamp("x", "MyStamp", p => new CanvasRect(p.X, p.Y, 5, 5), "Ⓩ") };
        var cut = RenderComponent<AtomCanvasStudio>(p => p.Add(c => c.Stamps, stamps));

        Assert.Single(cut.FindAll(".acs-stamp"));
        Assert.Contains("Ⓩ", cut.Markup);
    }
}
