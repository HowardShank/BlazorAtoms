using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorAtoms.Shared;

namespace BlazorAtoms.Screensavers;

/// <summary>
/// Matrix-style digital rain screensaver. Renders a canvas element and drives the falling
/// characters via a tiny JS module. Supports glow, scanlines, background/text color, and
/// font-family parameters exposed to CSS/JS.
/// </summary>
public partial class AtomScreensaverRain : AtomComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Canvas/drop text color. Sets <c>--screensaver-rain-color</c>.</summary>
    [Parameter] public string? TextColor { get; set; } = "#00FF00";

    /// <summary>Background / glow tint color. Sets <c>--screensaver-rain-bg</c>.</summary>
    [Parameter] public string? BackgroundColor { get; set; } = "#1e3e1e";

    /// <summary>Font family for the rain characters. Sets <c>--screensaver-rain-font</c>.</summary>
    [Parameter] public string? FontFamily { get; set; } = "monospace";

    /// <summary>Font size for the rain characters (pixels). Sets <c>--screensaver-rain-font-size</c>.</summary>
    [Parameter] public double FontSize { get; set; } = 16;

    /// <summary>Animation speed multiplier. 1 = default, 0.5 = half speed, 2 = double speed. Sets <c>--screensaver-rain-speed</c>.</summary>
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

    private const string ModulePath = "./_content/BlazorAtoms.Screensavers/atom-screensavers.js";

    private readonly string _canvasId = $"atom-screensaver-rain-{Guid.NewGuid():N}";
    private ElementReference _canvasRef;
    private IJSObjectReference? _module;
    private bool _previousDisabled;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!RendererInfo.IsInteractive || CancellationToken.IsCancellationRequested)
        {
            return;
        }

        var module = await TryGetModuleAsync();
        if (module is null)
        {
            return;
        }

        if (Disabled)
        {
            await TryInvokeAsync(() => module.InvokeVoidAsync("dispose", _canvasId));
            _previousDisabled = true;
            return;
        }

        // Start on first render, or when re-enabling after being disabled.
        if (firstRender || _previousDisabled)
        {
            await TryInvokeAsync(() => module.InvokeVoidAsync("start", _canvasId));
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

        await TryInvokeAsync(() => _module.InvokeVoidAsync("start", _canvasId));
    }

    /// <summary>Stops the animation but leaves the canvas element in place.</summary>
    public async Task StopAsync()
    {
        if (_module is null || CancellationToken.IsCancellationRequested)
        {
            return;
        }

        await TryInvokeAsync(() => _module.InvokeVoidAsync("stop", _canvasId));
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            GC.SuppressFinalize(this);
            return;
        }

        // Two separate guarded calls, not one try block around both: a failing "dispose" must not
        // skip DisposeAsync, or the JS object reference leaks for the life of the circuit.
        await TryInvokeAsync(() => _module.InvokeVoidAsync("dispose", _canvasId));
        await TryInvokeAsync(() => _module.DisposeAsync());

        _module = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Imports the module once, returning <c>null</c> instead of throwing when interop is
    /// unavailable — a torn-down circuit, a cancelled render, or a module that failed to load.
    /// </summary>
    private async Task<IJSObjectReference?> TryGetModuleAsync()
    {
        if (_module is not null)
        {
            return _module;
        }

        try
        {
            return _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        }
        catch (JSDisconnectedException) { return null; }
        catch (OperationCanceledException) { return null; }
        catch (JSException) { return null; }
    }

    /// <summary>
    /// Runs a JS call, swallowing the three failures that mean "the browser is no longer there":
    /// a disconnected Blazor Server circuit, a cancelled render, and a JS-side error such as the
    /// canvas element already being gone. A screensaver that can't animate is not worth an
    /// unhandled exception.
    /// </summary>
    private static async Task TryInvokeAsync(Func<ValueTask> call)
    {
        try
        {
            await call();
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
    }

    private string RootStyle => string.Concat(
        Width is null ? "" : $"--screensaver-rain-width:{Width};",
        Height is null ? "" : $"--screensaver-rain-height:{Height};",
        TextColor is null ? "" : $"--screensaver-rain-color:{TextColor};",
        BackgroundColor is null ? "" : $"--screensaver-rain-bg:{BackgroundColor};",
        FontFamily is null ? "" : $"--screensaver-rain-font:{FontFamily};",
        FontSize == 16 ? "" : $"--screensaver-rain-font-size:{FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;",
        Speed == 1 ? "" : $"--screensaver-rain-speed:{Speed.ToString(System.Globalization.CultureInfo.InvariantCulture)};");
}


