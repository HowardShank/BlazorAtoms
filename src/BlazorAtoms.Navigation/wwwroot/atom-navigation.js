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

const WATCHERS = new WeakMap();

// Toggle a data attribute on the button once the scroller passes `threshold` px. Uses a passive
// listener and coalesces bursts of scroll events into one rAF callback so we touch the DOM at most
// once per frame regardless of scroll frequency.
export function watchVisibility(el, dotNet, threshold, scope, containerSelector) {
    if (!el) return;
    unwatch(el); // idempotent

    const scroller = resolveScroller(el, scope, containerSelector);
    const target = scroller || window;
    let ticking = false;
    let lastVisible = null;

    const measure = () => {
        ticking = false;
        const y = scroller ? scroller.scrollTop : (window.scrollY || document.documentElement.scrollTop || 0);
        const visible = y >= threshold;
        if (visible !== lastVisible) {
            lastVisible = visible;
            el.setAttribute('data-visible', visible ? 'true' : 'false');
            if (dotNet) {
                // Fire-and-forget; ignore if the circuit is gone.
                try { dotNet.invokeMethodAsync('OnVisibilityChangedInternal', visible); } catch { }
            }
        }
    };

    const onScroll = () => {
        if (ticking) return;
        ticking = true;
        requestAnimationFrame(measure);
    };

    target.addEventListener('scroll', onScroll, { passive: true });
    WATCHERS.set(el, { target, onScroll });
    measure(); // set initial state
}

export function unwatch(el) {
    const w = el && WATCHERS.get(el);
    if (w) {
        w.target.removeEventListener('scroll', w.onScroll);
        WATCHERS.delete(el);
    }
}

// ---- collision watcher (don't cover important content) -----------------------------------

const OBSERVERS = new WeakMap();

// Fade the button out while any element matching hideNearSelector is visible in the scroller
// (footer, CTA, end-of-content), restore when it leaves. Sets data-collision on the button; the
// scoped CSS hides it. IntersectionObserver root = the resolved scroller (null → viewport).
export function watchCollision(el, hideNearSelector, scope, containerSelector) {
    if (!el || !hideNearSelector) return;
    unwatchCollision(el);

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
    OBSERVERS.set(el, io);
}

export function unwatchCollision(el) {
    const io = el && OBSERVERS.get(el);
    if (io) { io.disconnect(); OBSERVERS.delete(el); }
}
