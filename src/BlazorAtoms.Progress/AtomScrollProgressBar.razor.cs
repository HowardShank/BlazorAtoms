using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Progress;

/// <summary>A fixed reading-progress bar whose width tracks page scroll position. Always runs a
/// small one-time JS call to explicitly bind a named scroll-timeline to the real scroll
/// container (implicit "nearest ancestor" resolution doesn't work for a position:fixed element —
/// see the .razor file's comment); a full JS-driven fallback covers browsers without scroll-driven
/// animations at all. The track stays hidden until that call reports a successful measure, because
/// its pre-JS CSS default spans the whole viewport — see <see cref="Measured"/>.</summary>
public partial class AtomScrollProgressBar : IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorAtoms.Progress/atom-progress.js";

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _trackRef;
    private ElementReference _barRef;
    private IJSObjectReference? _module;
    private bool _attachAttempted;
    private bool _measured;
    private (ScrollProgressPosition Position, string? Width, ScrollProgressAlign Align, string? ScrollContainer)? _lastLayout;

    /// <summary>Color of the progress bar.</summary>
    [Parameter] public string Color { get; set; } = "#e6175d";

    /// <summary>Thickness of the bar. Any CSS length.</summary>
    [Parameter] public string Height { get; set; } = "12px";

    /// <summary>Which edge of the scroll container (not the raw viewport — see atom-progress.js)
    /// the bar sticks to.</summary>
    [Parameter] public ScrollProgressPosition Position { get; set; } = ScrollProgressPosition.Top;

    /// <summary>Width of the track. Any standard CSS length (<c>"50%"</c>, <c>"300px"</c>,
    /// <c>"20rem"</c>, ...) — resolved against the scroll container, not the viewport. Null
    /// (default) spans the full container width.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Horizontal alignment of the track within the scroll container, when
    /// <see cref="Width"/> makes the track narrower than the container.</summary>
    [Parameter] public ScrollProgressAlign Align { get; set; } = ScrollProgressAlign.Start;

    /// <summary>CSS selector naming the scrollable element this bar tracks. When set, the bar can
    /// live anywhere in the markup and no longer relies on walking its own ancestors to find a
    /// scroller — useful when several bars on one page track different containers, or when the
    /// container only becomes scrollable after content loads. Unset (default) = walk up from the
    /// bar for the nearest ancestor that is actually scrolling; a selector matching nothing falls
    /// back to that same walk. Mirrors <c>AtomScrollTo.ScrollContainer</c>.</summary>
    [Parameter] public string? ScrollContainer { get; set; }

    /// <summary>Whether the track has been measured against its resolved scroll container. False
    /// until the JS module reports a successful attach — during static SSR/prerender, and on
    /// browsers where the module never loads, it stays false and the track renders hidden. That is
    /// deliberate: a position:fixed track's pre-JS width is the whole viewport, so painting it
    /// early shows a full-width bar in the wrong place, and the fill can't advance without JS
    /// anyway (either a scroll-timeline or the fallback listener drives it).</summary>
    protected bool Measured => _measured;

    private string PositionClass => Position.ToString().ToLowerInvariant();

    private string RootStyle =>
        $"--atom-scroll-progress-color:{Color};" +
        $"--atom-scroll-progress-height:{Height};";

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var current = (Position, Width, Align, ScrollContainer);

        if (firstRender && !_attachAttempted)
        {
            _attachAttempted = true;
            _lastLayout = current;

            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
                var measured = await _module.InvokeAsync<bool>("attachScrollProgress",
                    _trackRef, _barRef, PositionClass, Width,
                    Align.ToString().ToLowerInvariant(), ScrollContainer);

                if (measured && !_measured)
                {
                    // Reveals the track (see Measured). One extra render, only ever on first attach.
                    _measured = true;
                    StateHasChanged();
                }
            }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }
            catch (JSException) { }

            return;
        }

        // Attach only ever runs once (above). If Position/Width/Align/ScrollContainer change
        // afterward — e.g. a playground control — the track's inline geometry from the original
        // attach would otherwise linger and fight the new values, so re-sync explicitly via
        // updateLayout instead of re-running the whole attach (which would rebind a second
        // scroll-timeline needlessly). updateLayout rebinds the container itself when
        // ScrollContainer is what changed.
        if (_module is not null && _lastLayout != current)
        {
            _lastLayout = current;

            try
            {
                await _module.InvokeAsync<bool>("updateLayout", _trackRef,
                    PositionClass, Width, Align.ToString().ToLowerInvariant(), ScrollContainer);
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
            // Everything attachScrollProgress registered lives on window / the scroll container and
            // outlives this component, so detaching is required, not optional.
            try
            {
                await _module.InvokeVoidAsync("detachScrollProgress", _trackRef);
            }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }
            catch (JSException) { }

            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }
}
