using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace BlazorAtoms.Behaviors;

/// <summary>
/// Runtime CSS feature-support check — e.g. <c>SupportsCssAsync(js, "transition-behavior",
/// "allow-discrete")</c> as a proxy for <c>@starting-style</c> support (they shipped together in
/// every browser, the same detection MDN/web.dev recommend for this exact feature). Not a Razor
/// component — a static helper any component calls directly, only from
/// <c>OnAfterRenderAsync</c> or later (JS interop isn't available during static SSR/prerender).
/// </summary>
public static class AtomBrowserSupport
{
    private const string ModulePath = "./_content/BlazorAtoms.Behaviors/atom-behaviors.js";

    // Keyed by the IJSRuntime instance rather than a flat static cache: on Blazor Server each
    // circuit (browser connection) has its own IJSRuntime, and a module reference or capability
    // result from one user's browser must never be reused for another's. ConditionalWeakTable
    // also means an ended circuit's entry is garbage-collected instead of leaking for the life of
    // the process. On WASM there's just the one JSRuntime, so this caches for the whole app run.
    private static readonly ConditionalWeakTable<IJSRuntime, State> States = new();

    private sealed class State
    {
        public readonly ConcurrentDictionary<(string Property, string Value), Task<bool>> SupportsCache = new();
        public Task<IJSObjectReference>? ModulePromise;
    }

    /// <summary>Does this browser support the given CSS property/value pair? Result is cached per
    /// JS runtime (browser doesn't change capability mid-session), so only the first caller for a
    /// given (property, value) pays the JS interop round trip.</summary>
    public static Task<bool> SupportsCssAsync(IJSRuntime js, string property, string value)
    {
        var state = States.GetOrCreateValue(js);
        return state.SupportsCache.GetOrAdd((property, value),
            key => ComputeSupportsAsync(js, state, key.Property, key.Value));
    }

    /// <summary>Resolves after the browser has painted at least one real frame — used by
    /// <see cref="TransitionState"/>'s JS-fallback path. See
    /// <c>src/BlazorAtoms.Shared/js/next-frame.js</c> for why a double-rAF wait is needed.</summary>
    public static async Task NextFrameAsync(IJSRuntime js)
    {
        try
        {
            var module = await EnsureModuleAsync(js, States.GetOrCreateValue(js));
            if (module is null) return; // import unavailable (SSR/prerender, or an unconfigured test double)
            await module.InvokeVoidAsync("nextFrame");
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
    }

    private static async Task<bool> ComputeSupportsAsync(IJSRuntime js, State state, string property, string value)
    {
        try
        {
            var module = await EnsureModuleAsync(js, state);
            if (module is null) return false; // import unavailable (SSR/prerender, or an unconfigured test double)
            return await module.InvokeAsync<bool>("supportsCss", property, value);
        }
        catch (JSDisconnectedException) { return false; }
        catch (OperationCanceledException) { return false; }
        catch (JSException) { return false; }
    }

    private static Task<IJSObjectReference> EnsureModuleAsync(IJSRuntime js, State state)
        => state.ModulePromise ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
}
