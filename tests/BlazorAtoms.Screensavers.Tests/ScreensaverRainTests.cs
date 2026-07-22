namespace BlazorAtoms.Screensavers.Tests;

public class ScreensaverRainTests : BunitContext
{
    private const string ModulePath = "./_content/BlazorAtoms.Screensavers/MatrixRain.js";

    public ScreensaverRainTests()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_Canvas_With_Expected_Role_And_Class()
    {
        var cut = Render<ScreensaverRain>();

        var canvas = cut.Find(".matrix-rain__canvas");
        Assert.NotNull(canvas.GetAttribute("id"));
        Assert.Equal("img", cut.Find(".matrix-rain").GetAttribute("role"));
    }

    [Fact]
    public void Disabled_Suppresses_Canvas()
    {
        var cut = Render<ScreensaverRain>(p => p.Add(c => c.Disabled, true));

        Assert.Empty(cut.FindAll(".matrix-rain__canvas"));
    }

    [Fact]
    public void Data_Glow_And_Scanlines_Attributes_Are_Set()
    {
        var cut = Render<ScreensaverRain>(p => p
            .Add(c => c.Glow, true)
            .Add(c => c.Scanlines, true));

        var root = cut.Find(".matrix-rain");
        Assert.Equal("true", root.GetAttribute("data-glow"));
        Assert.Equal("true", root.GetAttribute("data-scanlines"));
    }

    [Fact]
    public void Custom_Colors_Appear_In_Root_Style()
    {
        var cut = Render<ScreensaverRain>(p => p
            .Add(c => c.TextColor, "#FF0000")
            .Add(c => c.BackgroundColor, "#0000FF"));

        var style = cut.Find(".matrix-rain").GetAttribute("style");
        Assert.Contains("--mr-color:#FF0000", style);
        Assert.Contains("--mr-bg:#0000FF", style);
    }

    [Fact]
    public void Font_And_Size_Appear_In_Root_Style()
    {
        var cut = Render<ScreensaverRain>(p => p
            .Add(c => c.FontFamily, "'Courier New', monospace")
            .Add(c => c.FontSize, 24));

        var style = cut.Find(".matrix-rain").GetAttribute("style");
        Assert.Contains("--mr-font:'Courier New', monospace", style);
        Assert.Contains("--mr-font-size:24px", style);
    }

    [Fact]
    public void Speed_Appears_In_Root_Style()
    {
        var cut = Render<ScreensaverRain>(p => p.Add(c => c.Speed, 2.5));

        var style = cut.Find(".matrix-rain").GetAttribute("style");
        Assert.Contains("--mr-speed:2.5", style);
    }

    [Fact]
    public void Width_And_Height_Appear_In_Root_Style()
    {
        var cut = Render<ScreensaverRain>(p => p
            .Add(c => c.Width, "50%")
            .Add(c => c.Height, "400px"));

        var style = cut.Find(".matrix-rain").GetAttribute("style");
        Assert.Contains("--mr-width:50%", style);
        Assert.Contains("--mr-height:400px", style);
    }

    [Fact]
    public void OnAfterRender_Starts_Animation()
    {
        var module = JSInterop.SetupModule(ModulePath);

        Render<ScreensaverRain>();

        module.VerifyInvoke("start");
    }

    [Fact]
    public async Task StartAsync_Calls_Module_Start()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var cut = Render<ScreensaverRain>();
        await cut.Instance.StartAsync();

        Assert.Equal(2, module.Invocations.Count(i => i.Identifier == "start"));
    }

    [Fact]
    public async Task StopAsync_Calls_Module_Stop()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var cut = Render<ScreensaverRain>();
        await cut.Instance.StopAsync();

        module.VerifyInvoke("stop");
    }

    [Fact]
    public async Task DisposeAsync_Calls_Module_Dispose()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var cut = Render<ScreensaverRain>();
        await cut.Instance.DisposeAsync();

        module.VerifyInvoke("dispose");
    }
}
