using Microsoft.JSInterop;

namespace BlazorAtoms.Behaviors;

/// <summary>
/// Reusable enter/exit animation state machine — the engine behind <c>AtomTransition</c>
/// (BlazorAtoms.Transitions), but usable directly by any component that wants the same CSS-native
/// / JS-fallback hybrid without wrapping content in extra markup (e.g. a future carousel driving
/// one instance per slide).
///
/// The owning element is expected to stay mounted permanently — visibility is purely CSS-driven
/// (an "-shown" class toggling opacity/transform), never a DOM add/remove. That means the "wait a
/// real frame before flipping the class" problem only matters once: the moment an instance first
/// renders already shown (nothing to transition *from* otherwise). Every later <see cref="SetShown"/>
/// call is an ordinary CSS class swap and animates fine in any browser, JS-free.
/// </summary>
public sealed class TransitionState
{
    private const string StartingStyleProperty = "transition-behavior";
    private const string StartingStyleValue = "allow-discrete";

    private bool _initialized;
    private int _generation; // bumped by SetShown, so a pending InitializeAsync fallback wait
                              // can detect it's been superseded and not clobber a newer value.

    /// <summary>Current shown/hidden state to render CSS classes from.</summary>
    public bool Shown { get; private set; }

    /// <summary>
    /// Call once, from the owning component's first <c>OnAfterRenderAsync(firstRender: true)</c>,
    /// passing the <c>Show</c> parameter's value at that moment. The component's very first
    /// synchronous render must always render <see cref="Shown"/>'s default (<c>false</c>) —
    /// there's no way to resolve the CSS-native/JS-fallback capability check before that first
    /// paint happens. Returns <c>true</c> if the caller must call <c>StateHasChanged()</c>
    /// afterwards to actually reveal it; <c>false</c> means nothing changed (there was nothing to
    /// show, or a newer <see cref="SetShown"/> call already superseded this one).
    /// </summary>
    public async Task<bool> InitializeAsync(IJSRuntime js, bool initialShow)
    {
        if (_initialized)
        {
            return false;
        }

        _initialized = true;

        if (!initialShow)
        {
            Shown = false;
            return false;
        }

        // A CSS-native browser doesn't need the frame wait: @starting-style fires on an
        // element's first style calculation regardless of how many Blazor render passes led up
        // to it, so flipping the class on this next render (rather than the first) still gets
        // the declarative transition. A non-native browser genuinely needs two SEPARATE painted
        // frames — the closed look this first render already produced, then the shown look —
        // for its plain CSS transition to have a real "from" value to interpolate.
        var generation = _generation;
        var nativeSupport = await AtomBrowserSupport.SupportsCssAsync(js, StartingStyleProperty, StartingStyleValue);
        if (!nativeSupport)
        {
            await AtomBrowserSupport.NextFrameAsync(js);
        }

        if (generation != _generation)
        {
            // A SetShown call landed while we were resolving this — that's the current truth
            // now, don't clobber it with the stale "shown at mount" outcome.
            return false;
        }

        Shown = true;
        return true;
    }

    /// <summary>Ordinary post-first-render toggle — synchronous, no JS interop, plain CSS class
    /// swap. Safe to call even before <see cref="InitializeAsync"/> has completed; the async
    /// first-paint sequencing only matters for the very first shown-at-mount case.</summary>
    public void SetShown(bool show)
    {
        _generation++;
        Shown = show;
    }
}
