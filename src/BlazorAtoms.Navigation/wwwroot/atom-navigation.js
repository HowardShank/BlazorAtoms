// atom-navigation.js — scroll behavior for BlazorAtoms.Navigation (AtomScrollTo).
//
// The library is otherwise data-driven from C#; this module is lazily imported by the component
// (via IJSObjectReference) — no <script> tag, no DI, nothing for the consumer to wire up. It does
// only what CSS/HTML can't do declaratively: programmatic smooth scrolling to a computed position,
// and an efficient scroll-position watcher (passive listener + rAF coalescing) that toggles the
// button's visibility class past a pixel threshold.

// Resolve the scroll container for a given button element.
//   containerSelector set → document.querySelector(containerSelector) (button need not be inside it)
//   scope === 'container' → nearest scrollable ancestor of the button
//   otherwise             → null = page/window
function resolveScroller(el, scope, containerSelector) {
    if (containerSelector) {
        try {
            const found = document.querySelector(containerSelector);
            if (found) return found;
        } catch { /* bad selector → fall through */ }
        return null; // named container missing → fall back to page
    }
    if (scope !== 'container') return null; // null = page/window
    let node = el ? el.parentElement : null;
    while (node) {
        const style = getComputedStyle(node);
        const oy = style.overflowY;
        if ((oy === 'auto' || oy === 'scroll' || oy === 'overlay') && node.scrollHeight > node.clientHeight)
            return node;
        node = node.parentElement;
    }
    return null; // fall back to page if nothing scrollable found
}

// mode: 'top' | 'bottom' | 'selector'; targetSelector used only for 'selector'.
// containerSelector (optional) names the scroller explicitly.
export function scrollToTarget(el, mode, targetSelector, scope, behavior, containerSelector) {
    try {
        const beh = behavior === 'auto' ? 'auto' : 'smooth';

        if (mode === 'selector' && targetSelector) {
            // Accept "#id", "name", or any CSS selector. Try id/name first, then querySelector.
            const target =
                document.getElementById(targetSelector) ||
                document.querySelector(`[name="${CSS.escape(targetSelector)}"]`) ||
                trySelector(targetSelector);
            if (target) { target.scrollIntoView({ behavior: beh, block: 'start' }); return true; }
            return false;
        }

        const scroller = resolveScroller(el, scope, containerSelector);
        if (scroller) {
            const top = mode === 'bottom' ? scroller.scrollHeight : 0;
            scroller.scrollTo({ top, behavior: beh });
        } else {
            const top = mode === 'bottom'
                ? (document.documentElement.scrollHeight || document.body.scrollHeight)
                : 0;
            window.scrollTo({ top, behavior: beh });
        }
        return true;
    } catch {
        return false;
    }
}

function trySelector(sel) {
    try { return document.querySelector(sel); } catch { return null; }
}

// ---- visibility watcher ------------------------------------------------------------------

// Keyed by a C#-generated string id, NOT by the ElementReference. During SPA-nav teardown the
// element reference can stop marshaling, so an unwatch keyed on it would silently no-op and leave
// the scroll listener attached — it then fires into an already-disposed DotNetObjectReference. A
// string id always marshals, so unwatch(id) reliably removes the listener regardless of DOM state.
const WATCHERS = new Map();

// Toggle a data attribute on the button once the scroller passes `threshold` px. Uses a passive
// listener and coalesces bursts of scroll events into one rAF callback so we touch the DOM at most
// once per frame regardless of scroll frequency. `dotNet` may be null — when the consumer wires no
// OnVisibilityChanged callback the component passes no DotNetObjectReference, and notify() no-ops.
export function watchVisibility(id, el, dotNet, threshold, scope, containerSelector) {
    if (!el) return;
    unwatch(id); // idempotent

    const scroller = resolveScroller(el, scope, containerSelector);
    const target = scroller || window;
    let ticking = false;
    let rafId = null;
    let lastVisible = null;
    let disposed = false;

    // Fire-and-forget: invokeMethodAsync returns a Promise, so a plain try/catch around the call
    // (which only throws synchronously) can't observe rejection — an unhandled rejection surfaces
    // as an uncaught error in the console (e.g. a stale call reaching an already-disposed
    // DotNetObjectReference). Attach .catch() explicitly to actually swallow it.
    const notify = (visible) => {
        if (!dotNet) return;
        try {
            const p = dotNet.invokeMethodAsync('OnVisibilityChangedInternal', visible);
            if (p && typeof p.catch === 'function') p.catch(() => { });
        } catch { /* circuit/instance already gone */ }
    };

    const measure = () => {
        rafId = null;
        ticking = false;
        if (disposed) return; // queued before teardown, torn down before this frame ran
        const y = scroller ? scroller.scrollTop : (window.scrollY || document.documentElement.scrollTop || 0);
        const visible = y >= threshold;
        if (visible !== lastVisible) {
            lastVisible = visible;
            el.setAttribute('data-visible', visible ? 'true' : 'false');
            notify(visible);
        }
    };

    const onScroll = () => {
        if (ticking) return;
        ticking = true;
        rafId = requestAnimationFrame(measure);
    };

    target.addEventListener('scroll', onScroll, { passive: true });
    WATCHERS.set(id, {
        target, onScroll,
        cancel: () => {
            disposed = true;
            if (rafId !== null) cancelAnimationFrame(rafId);
        },
    });
    measure(); // set initial state
}

export function unwatch(id) {
    const w = id != null && WATCHERS.get(id);
    if (w) {
        w.target.removeEventListener('scroll', w.onScroll);
        w.cancel();
        WATCHERS.delete(id);
    }
}

// ---- collision watcher (don't cover important content) -----------------------------------

// Keyed by the same C#-generated string id as WATCHERS, for the same teardown-safety reason.
const OBSERVERS = new Map();

// Fade the button out while any element matching hideNearSelector is visible in the scroller
// (footer, CTA, end-of-content), restore when it leaves. Sets data-collision on the button; the
// scoped CSS hides it. IntersectionObserver root = the resolved scroller (null → viewport).
export function watchCollision(id, el, hideNearSelector, scope, containerSelector) {
    if (!el || !hideNearSelector) return;
    unwatchCollision(id);

    let targets;
    try { targets = Array.from(document.querySelectorAll(hideNearSelector)); }
    catch { return; } // bad selector
    if (!targets.length) return;

    const root = resolveScroller(el, scope, containerSelector); // null = viewport
    const visible = new Set();
    const io = new IntersectionObserver((entries) => {
        for (const e of entries) {
            if (e.isIntersecting) visible.add(e.target); else visible.delete(e.target);
        }
        el.setAttribute('data-collision', visible.size ? 'true' : 'false');
    }, { root: root || null, threshold: 0 });

    targets.forEach(t => io.observe(t));
    OBSERVERS.set(id, io);
}

export function unwatchCollision(id) {
    const io = id != null && OBSERVERS.get(id);
    if (io) { io.disconnect(); OBSERVERS.delete(id); }
}
