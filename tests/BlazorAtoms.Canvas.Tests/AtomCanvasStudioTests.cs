using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorAtoms.Canvas.Tests;

// The studio is a composite over AtomCanvas. bUnit can't run the real canvas/pointer JS, so we drive the
// public/imperative API (which the toolbar + context both call) and assert model state, tool->mode mapping,
// history, layers, JSON round-trip, and the slot/context extension points.
public class AtomCanvasStudioTests : BunitContext
{
    public AtomCanvasStudioTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private EventCallback<IReadOnlyList<CanvasShape>> Capture(Action<IReadOnlyList<CanvasShape>> a)
        => EventCallback.Factory.Create(this, a);

    [Fact]
    public void Default_toolbar_and_canvas_render()
    {
        var cut = Render<AtomCanvasStudio>();
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
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.Tool, tool));
        Assert.Equal(mode, cut.Find("canvas.atom-canvas").GetAttribute("data-mode"));
    }

    [Fact]
    public async Task Add_undo_redo_round_trip()
    {
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

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
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

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
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

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
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(rect));
        await cut.InvokeAsync(() => cut.Instance.ToggleVisibleAsync(rect.Id));

        Assert.False(cur!.Single().Visible);
    }

    [Fact]
    public async Task Save_then_load_json_round_trips_the_model()
    {
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));
        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(new CanvasRect(1, 2, 3, 4) { Fill = "#abc" }));
        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(new CanvasCircle(5, 5, 3)));
        var json = cut.Instance.SaveJson();

        var cut2 = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));
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
        var cut = Render<AtomCanvasStudio>();
        var ok = true;
        await cut.InvokeAsync(async () => ok = await cut.Instance.LoadJsonAsync("{ not json"));
        Assert.False(ok);
    }

    [Fact]
    public void Toolbar_slot_replaces_the_default_toolbar()
    {
        var cut = Render<AtomCanvasStudio>(p => p
            .Add(c => c.Toolbar, (AtomCanvasStudioContext ctx) =>
                (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-tb\">mine</div>"))));

        Assert.NotEmpty(cut.FindAll(".custom-tb"));
        Assert.Empty(cut.FindAll(".acs-group")); // default groups are gone
    }

    [Fact]
    public void ToolbarEnd_slot_receives_the_studio_context()
    {
        var cut = Render<AtomCanvasStudio>(p => p
            .Add(c => c.ToolbarEnd, (AtomCanvasStudioContext ctx) =>
                (RenderFragment)(b => b.AddMarkupContent(0, $"<span class=\"cnt\">{ctx.ShapeCount}</span>"))));

        Assert.Equal("0", cut.Find(".cnt").TextContent);
    }

    [Fact]
    public void Custom_stamps_surface_in_the_palette()
    {
        var stamps = new[] { new CanvasStamp("x", "MyStamp", p => new CanvasRect(p.X, p.Y, 5, 5), "Ⓩ") };
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.Stamps, stamps));

        Assert.Single(cut.FindAll(".acs-stamp"));
        Assert.Contains("Ⓩ", cut.Markup);
    }

    [Fact]
    public void Default_menu_bar_renders_standard_menus()
    {
        var cut = Render<AtomCanvasStudio>();
        Assert.NotNull(cut.Find(".acs-menubar"));
        Assert.Equal(5, cut.FindAll(".acs-menu").Count); // File / Edit / View / Object / Help
        foreach (var label in new[] { "File", "Edit", "View", "Object", "Help" })
            Assert.Contains(label, cut.Markup);
    }

    [Fact]
    public void ShowMenuBar_false_hides_the_menu_bar()
    {
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShowMenuBar, false));
        Assert.Empty(cut.FindAll(".acs-menubar"));
    }

    [Fact]
    public void Menu_slot_replaces_the_default_menu_bar()
    {
        var cut = Render<AtomCanvasStudio>(p => p
            .Add(c => c.Menu, (AtomCanvasStudioContext ctx) =>
                (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"custom-menu\">mine</div>"))));

        Assert.NotEmpty(cut.FindAll(".custom-menu"));
        Assert.Empty(cut.FindAll(".acs-menu")); // default menus gone
    }

    [Fact]
    public async Task View_menu_toggle_hides_the_layers_panel()
    {
        var cut = Render<AtomCanvasStudio>();
        Assert.NotNull(cut.Find(".acs-layers"));

        await cut.InvokeAsync(() => cut.Instance.ToggleLayers());

        Assert.Empty(cut.FindAll(".acs-layers"));
    }

    [Fact]
    public async Task SetFillColor_fills_the_selected_shape()
    {
        var rect = new CanvasRect(0, 0, 10, 10); // no fill
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(rect));
        await cut.InvokeAsync(() => cut.Instance.SelectShape(rect.Id));
        await cut.InvokeAsync(() => cut.Instance.SetFillColorAsync("#ff0000"));

        Assert.Equal("#ff0000", cur!.Single().Fill);
        Assert.Equal(rect.Id, cur!.Single().Id);
    }

    [Fact]
    public async Task SetPenColor_recolors_the_selected_shape_stroke()
    {
        var rect = new CanvasRect(0, 0, 10, 10);
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

        await cut.InvokeAsync(() => cut.Instance.AddShapeAsync(rect));
        await cut.InvokeAsync(() => cut.Instance.SelectShape(rect.Id));
        await cut.InvokeAsync(() => cut.Instance.SetPenColorAsync("#00ff00"));

        Assert.Equal("#00ff00", cur!.Single().Stroke);
    }

    [Fact]
    public async Task SetFillColor_with_no_selection_only_sets_the_default()
    {
        IReadOnlyList<CanvasShape>? cur = null;
        var cut = Render<AtomCanvasStudio>(p => p.Add(c => c.ShapesChanged, Capture(v => cur = v)));

        await cut.InvokeAsync(() => cut.Instance.SetFillColorAsync("#123456")); // nothing selected
        Assert.Null(cur); // no model change / ShapesChanged
    }

    [Fact]
    public void Fill_color_picker_is_enabled_by_default_with_a_no_fill_button()
    {
        var cut = Render<AtomCanvasStudio>();
        var fill = cut.FindAll("input[type=color]").First(i => i.GetAttribute("aria-label") == "Fill color");
        Assert.False(fill.HasAttribute("disabled")); // was disabled until a checkbox was ticked — the reported bug
        Assert.Contains("No fill", cut.Markup);
    }

    [Fact]
    public void FillColor_param_change_after_init_is_adopted()
    {
        var cut = Render<AtomCanvasStudio>(p => p
            .Add(c => c.FillColor, "#111111")
            .Add(c => c.ToolbarEnd, (AtomCanvasStudioContext ctx) =>
                (RenderFragment)(b => b.AddMarkupContent(0, $"<span class=\"fc\">{ctx.FillColor}</span>"))));

        Assert.Equal("#111111", cut.Find(".fc").TextContent);

        cut.Render(p => p.Add(c => c.FillColor, "#222222"));
        Assert.Equal("#222222", cut.Find(".fc").TextContent);
    }
}
