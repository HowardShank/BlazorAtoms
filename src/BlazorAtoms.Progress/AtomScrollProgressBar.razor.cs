using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Progress;

/// <summary>A fixed reading-progress bar whose width tracks page scroll position. Always runs a
/// small one-time JS call to explicitly bind a named scroll-timeline to the real scroll
/// container (implicit "nearest ancestor" resolution doesn't work for a position:fixed element —
/// see the .razor file's comment); a full JS-driven fallback covers browsers without scroll-driven
/// animations at all.</summary>
public partial class AtomScrollProgressBar : IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.Progress/atom-progress.js";

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _trackRef;
    private ElementReference _barRef;
    private IJSObjectReference? _module;
    private bool _attachAttempted;
    private ScrollProgressPosition? _lastAttachedPosition;

    /// <summary>Color of the progress bar.</summary>
    [Parameter] public string Color { get; set; } = "#e6175d";

    /// <summary>Thickness of the bar. Any CSS length.</summary>
    [Parameter] public string Height { get; set; } = "12px";

    /// <summary>Which edge of the scroll container (not the raw viewport — see atom-progress.js)
    /// the bar sticks to.</summary>
    [Parameter] public ScrollProgressPosition Position { get; set; } = ScrollProgressPosition.Top;

    private string PositionClass => Position.ToString().ToLowerInvariant();

    private string RootStyle =>
        $"--atom-scroll-progress-color:{Color};" +
        $"--atom-scroll-progress-height:{Height};";

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_attachAttempted)
        {
            _attachAttempted = true;
            _lastAttachedPosition = Position;

            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
                await _module.InvokeVoidAsync("attachScrollProgress", _trackRef, _barRef, PositionClass);
            }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }
            catch (JSException) { }

            return;
        }

        // Attach only ever runs once (above). If Position changes afterward — e.g. a playground
        // dropdown — the track's inline top/bottom from the original attach would otherwise linger
        // and fight the new CSS class, so re-sync explicitly via updatePosition instead of
        // re-running the whole attach (which would rebind a second scroll-timeline needlessly).
        if (_module is not null && _lastAttachedPosition != Position)
        {
            _lastAttachedPosition = Position;

            try
            {
                await _module.InvokeVoidAsync("updatePosition", _trackRef, PositionClass);
            }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }
            catch (JSException) { }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }
}
