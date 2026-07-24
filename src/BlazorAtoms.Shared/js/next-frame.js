// Shared across packages via build/SharedJs.props, which copies this file into each importing
// package's own wwwroot/shared/ at build time (same "compile-in, zero runtime dependency"
// property as build/Shared.props does for .cs — see that file's comment).

// Resolve after the browser has painted at least one frame. Callers mount an element in its
// hidden CSS state, wait for this, then flip to the visible state — giving the transition a start
// state to animate from. Without a real frame gap, Blazor's renders can coalesce into a single
// paint and the element just snaps to its final state. Double rAF: the first callback runs before
// paint, the second after it.
export function nextFrame() {
    return new Promise(resolve =>
        requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
}
