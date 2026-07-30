namespace BlazorAtoms.Screensavers.Tests;

public class AtomScreensaverRainTests : BunitContext
{
    private const string ModulePath = "./_content/BlazorAtoms.Screensavers/atom-screensavers.js";

    public AtomScreensaverRainTests()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_Canvas_With_Expected_Role_And_Class()
    {
        var cut = Render<AtomScreensaverRain>();

        var canvas = cut.Find(".atom-screensaver-rain__canvas");
        Assert.NotNull(canvas.GetAttribute("id"));
        Assert.Equal("img", cut.Find(".atom-screensaver-rain").GetAttribute("role"));
    }

    [Fact]
    public void Disabled_Suppresses_Canvas()
    {
        var cut = Render<AtomScreensaverRain>(p => p.Add(c => c.Disabled, true));

        Assert.Empty(cut.FindAll(".atom-screensaver-rain__canvas"));
    }

    [Fact]
    public void Data_Glow_And_Scanlines_Attributes_Are_Set()
    {
        var cut = Render<AtomScreensaverRain>(p => p
            .Add(c => c.Glow, true)
            .Add(c => c.Scanlines, true));

        var root = cut.Find(".atom-screensaver-rain");
        Assert.Equal("true", root.GetAttribute("data-glow"));
        Assert.Equal("true", root.GetAttribute("data-scanlines"));
    }

    [Fact]
    public void Custom_Colors_Appear_In_Root_Style()
    {
        var cut = Render<AtomScreensaverRain>(p => p
            .Add(c => c.TextColor, "#FF0000")
            .Add(c => c.BackgroundColor, "#0000FF"));

        var style = cut.Find(".atom-screensaver-rain").GetAttribute("style");
        Assert.Contains("--screensaver-rain-color:#FF0000", style);
        Assert.Contains("--screensaver-rain-bg:#0000FF", style);
    }

    [Fact]
    public void Font_And_Size_Appear_In_Root_Style()
    {
        var cut = Render<AtomScreensaverRain>(p => p
            .Add(c => c.FontFamily, "'Courier New', monospace")
            .Add(c => c.FontSize, 24));

        var style = cut.Find(".atom-screensaver-rain").GetAttribute("style");
        Assert.Contains("--screensaver-rain-font:'Courier New', monospace", style);
        Assert.Contains("--screensaver-rain-font-size:24px", style);
    }

    [Fact]
    public void Speed_Appears_In_Root_Style()
    {
        var cut = Render<AtomScreensaverRain>(p => p.Add(c => c.Speed, 2.5));

        var style = cut.Find(".atom-screensaver-rain").GetAttribute("style");
        Assert.Contains("--screensaver-rain-speed:2.5", style);
    }

    [Fact]
    public void Width_And_Height_Appear_In_Root_Style()
    {
        var cut = Render<AtomScreensaverRain>(p => p
            .Add(c => c.Width, "50%")
            .Add(c => c.Height, "400px"));

        var style = cut.Find(".atom-screensaver-rain").GetAttribute("style");
        Assert.Contains("--screensaver-rain-width:50%", style);
        Assert.Contains("--screensaver-rain-height:400px", style);
    }

    [Fact]
    public void OnAfterRender_Starts_Animation()
    {
        var module = JSInterop.SetupModule(ModulePath);

        Render<AtomScreensaverRain>();

        module.VerifyInvoke("start");
    }

    [Fact]
    public async Task StartAsync_Calls_Module_Start()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var cut = Render<AtomScreensaverRain>();
        await cut.Instance.StartAsync();

        Assert.Equal(2, module.Invocations.Count(i => i.Identifier == "start"));
    }

    [Fact]
    public async Task StopAsync_Calls_Module_Stop()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var cut = Render<AtomScreensaverRain>();
        await cut.Instance.StopAsync();

        module.VerifyInvoke("stop");
    }

    [Fact]
    public async Task DisposeAsync_Calls_Module_Dispose()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var cut = Render<AtomScreensaverRain>();
        await cut.Instance.DisposeAsync();

        module.VerifyInvoke("dispose");
    }

    // Interop failure paths. Every call site is guarded against the three exceptions that mean the
    // browser is gone (JSDisconnectedException, OperationCanceledException, JSException); the first
    // two can't be provoked from a test, but JSException stands in for all three since one catch
    // block per site handles them together. A screensaver that can't animate must not take the page
    // down with it.

    [Fact]
    public void A_failing_start_does_not_throw_out_of_render()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("start", _ => true).SetException(new JSException("no canvas"));

        var cut = Render<AtomScreensaverRain>();

        // Assert the throwing call actually ran: without this the test would pass just as happily
        // if the matcher never matched and nothing ever threw.
        module.VerifyInvoke("start");
        // Markup is Blazor's, not JS's, so the canvas is still there to be animated later.
        Assert.NotNull(cut.Find(".atom-screensaver-rain__canvas"));
    }

    [Fact]
    public async Task A_failing_stop_does_not_throw()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("stop", _ => true).SetException(new JSException("no canvas"));
        var cut = Render<AtomScreensaverRain>();

        await cut.Instance.StopAsync();

        module.VerifyInvoke("stop");
    }

    [Fact]
    public async Task A_failing_dispose_does_not_throw()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("dispose", _ => true).SetException(new JSException("already gone"));
        var cut = Render<AtomScreensaverRain>();

        // Disposal is the likeliest failure point: the circuit is usually already tearing down.
        await cut.Instance.DisposeAsync();

        module.VerifyInvoke("dispose");
    }

    // No test for a failing module *import*: bUnit rejects Setup<IJSObjectReference>("import", …)
    // outright ("Use one of the SetupModule() methods instead"), and SetupModule has no way to make
    // the import fail. Leaving the module unplanned under strict mode is not a substitute — that
    // throws bUnit's own JSRuntimeUnhandledInvocationException, which is not a production exception
    // type, so a correctly-written guard would rightly not catch it and the test would fail for a
    // reason that says nothing about real behavior. TryGetModuleAsync's guard is inspection-only.
}
