using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Screensavers;

/// <summary>
/// Matrix-style digital rain screensaver. Renders a canvas element and drives the falling
/// characters via a tiny JS module. Supports glow, scanlines, background/text color, and
/// font-family parameters exposed to CSS/JS.
/// </summary>
public partial class ScreensaverRain : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Canvas/drop text color. Sets <c>--mr-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; } = "#00FF00";

    /// <summary>Background / glow tint color. Sets <c>--mr-bg</c>.</summary>
    [Parameter] public string? BackgroundColor { get; set; } = "#1e3e1e";

    /// <summary>Font family for the rain characters. Sets <c>--mr-font</c>.</summary>
    [Parameter] public string? FontFamily { get; set; } = "monospace";

    /// <summary>Font size for the rain characters (pixels). Sets <c>--mr-font-size</c>.</summary>
    [Parameter] public double FontSize { get; set; } = 16;

    /// <summary>Animation speed multiplier. 1 = default, 0.5 = half speed, 2 = double speed. Sets <c>--mr-speed</c>.</summary>
    [Parameter] public double Speed { get; set; } = 1;

    /// <summary>Container width (CSS length). Default: 100%.</summary>
    [Parameter] public string? Width { get; set; } = "100%";

    /// <summary>Container height (CSS length). Default: 100%.</summary>
    [Parameter] public string? Height { get; set; } = "100%";

    /// <summary>When true, a glow effect is applied to the rain.</summary>
    [Parameter] public bool Glow { get; set; } = true;

    /// <summary>When true, scanlines are overlaid on the display.</summary>
    [Parameter] public bool Scanlines { get; set; } = false;

    /// <summary>When true, the canvas never renders.</summary>
    [Parameter] public bool Disabled { get; set; } = false;

    private readonly string _canvasId = $"matrix-rain-{Guid.NewGuid():N}";
    private ElementReference _canvasRef;
    private IJSObjectReference? _module;
    private bool _previousDisabled;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!RendererInfo.IsInteractive || CancellationToken.IsCancellationRequested)
        {
            return;
        }

        _module ??= await JS.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorAtoms.Screensavers/MatrixRain.js");

        if (Disabled)
        {
            await _module.InvokeVoidAsync("dispose", _canvasId);
            _previousDisabled = true;
            return;
        }

        // Start on first render, or when re-enabling after being disabled.
        if (firstRender || _previousDisabled)
        {
            await _module.InvokeVoidAsync("start", _canvasId);
        }

        _previousDisabled = false;
    }

    /// <summary>Starts the Matrix rain animation. Safe to call repeatedly; restarts cleanly.</summary>
    public async Task StartAsync()
    {
        if (_module is null || CancellationToken.IsCancellationRequested)
        {
            return;
        }

        await _module.InvokeVoidAsync("start", _canvasId);
    }

    /// <summary>Stops the animation but leaves the canvas element in place.</summary>
    public async Task StopAsync()
    {
        if (_module is null || CancellationToken.IsCancellationRequested)
        {
            return;
        }

        await _module.InvokeVoidAsync("stop", _canvasId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            GC.SuppressFinalize(this);
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("dispose", _canvasId);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }

        _module = null;
        GC.SuppressFinalize(this);
    }

    private string RootStyle => string.Concat(
        Width is null ? "" : $"--mr-width:{Width};",
        Height is null ? "" : $"--mr-height:{Height};",
        TextColor is null ? "" : $"--mr-color:{TextColor};",
        BackgroundColor is null ? "" : $"--mr-bg:{BackgroundColor};",
        FontFamily is null ? "" : $"--mr-font:{FontFamily};",
        FontSize == 16 ? "" : $"--mr-font-size:{FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;",
        Speed == 1 ? "" : $"--mr-speed:{Speed.ToString(System.Globalization.CultureInfo.InvariantCulture)};");
}


