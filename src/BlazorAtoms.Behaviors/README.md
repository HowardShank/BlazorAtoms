# BlazorAtoms.Behaviors

Headless behavior helpers for Blazor — no visuals of their own. Ships two pieces this round:

- **`AtomBrowserSupport`** — a cached, runtime CSS-feature-support check (`CSS.supports()` via a
  tiny self-imported JS module), for components that need to pick between a modern CSS path and a
  JS fallback at runtime.
- **`TransitionState`** — a reusable enter/exit animation state machine. It's the engine behind
  `AtomTransition` (`BlazorAtoms.Transitions`), but any component can drive one directly without
  wrapping content in extra markup.

*Planned:* `ClickOutside`, `FocusTrap`, `Clipboard`, `Portal`.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Behaviors\BlazorAtoms.Behaviors.csproj" />
```
```razor
@using BlazorAtoms.Behaviors
```

## AtomBrowserSupport

```csharp
@inject IJSRuntime JS

bool supportsStartingStyle = await AtomBrowserSupport.SupportsCssAsync(JS, "transition-behavior", "allow-discrete");
```

A static class, not a component — call it directly from `OnAfterRenderAsync` or later (JS interop
isn't available during static SSR/prerender, so don't call it earlier in the lifecycle). Results
are cached per browser connection (each Blazor Server circuit gets its own `IJSRuntime`; WASM has
just the one for the whole app), so only the first caller for a given `(property, value)` pair
pays the JS interop round trip.

## TransitionState

```csharp
private readonly TransitionState _state = new();

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && await _state.InitializeAsync(JS, initialShow: Show))
    {
        StateHasChanged();
    }
}

protected override void OnParametersSet() => _state.SetShown(Show);
```

Render your CSS classes from `_state.Shown`. The owning element should stay mounted permanently —
visibility is purely a CSS class toggle (opacity/transform), never a DOM add/remove — which is
what lets every toggle *after* the first render be a plain synchronous class swap: no JS, no
waiting, animates correctly in any browser. The only moment that needs the hybrid path is an
instance's very first render if it's already meant to be shown (nothing to transition *from*
otherwise) — `InitializeAsync` resolves that once via `AtomBrowserSupport`, then gets out of the
way.

## Notes

- **Render modes.** JS interop can't run during static SSR / prerender. `AtomBrowserSupport` and
  `TransitionState` are both designed to only be invoked from `OnAfterRenderAsync` onward, so this
  isn't a concern in practice — just don't call them from `OnInitialized`/`OnParametersSet`.
- **Exception handling.** Every interop call is wrapped; `JSDisconnectedException`,
  `OperationCanceledException`, and `JSException` are swallowed so a dead circuit never throws
  into your UI (a failed capability check just falls back to `false`/the JS path).
