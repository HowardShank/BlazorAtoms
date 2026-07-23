// atom-layout.js — optional interop for BlazorAtoms.Layout (AtomDrawer).
//
// The component renders and animates in pure C#/CSS; this module only handles the things the DOM
// won't do declaratively for a modal: trapping Tab focus inside the panel, closing on Escape, and
// locking body scroll while open. Lazily imported by the component when the drawer opens.

const STATE = new WeakMap();

// Resolve after the browser has painted at least one frame. The drawer mounts in its hidden CSS
// state, waits for this, then adds the open class — giving the transition a start state to animate
// from. Without a real frame gap Blazor's two renders coalesce into a single paint and the drawer
// just snaps open. Double rAF: the first callback runs before paint, the second after it.
export function nextFrame() {
    return new Promise(resolve =>
        requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
}

const FOCUSABLE =
    'a[href],area[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),' +
    'textarea:not([disabled]),[tabindex]:not([tabindex="-1"]),[contenteditable="true"]';

function focusables(el) {
    return Array.from(el.querySelectorAll(FOCUSABLE))
        .filter(n => n.offsetWidth > 0 || n.offsetHeight > 0 || n === document.activeElement);
}

// Fire-and-forget JS→.NET call. invokeMethodAsync returns a Promise; attach .catch() so a stale
// call reaching an already-disposed reference doesn't surface as an uncaught rejection.
function notifyClose(dotNet) {
    if (!dotNet) return;
    try {
        const p = dotNet.invokeMethodAsync('CloseFromJsAsync');
        if (p && typeof p.catch === 'function') p.catch(() => { });
    } catch { /* circuit/instance already gone */ }
}

// Turn the panel into a modal: focus it, trap Tab, wire Escape, lock body scroll.
export function activate(el, dotNet, options) {
    if (!el) return;
    deactivate(el); // idempotent

    const opts = options || {};
    const prevFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const prevBodyOverflow = opts.lockScroll ? document.body.style.overflow : null;
    if (opts.lockScroll) document.body.style.overflow = 'hidden';

    const onKeyDown = (e) => {
        if (opts.closeOnEscape && e.key === 'Escape') {
            e.preventDefault();
            notifyClose(dotNet);
            return;
        }
        if (!opts.trapFocus || e.key !== 'Tab') return;

        const items = focusables(el);
        if (items.length === 0) { e.preventDefault(); el.focus({ preventScroll: true }); return; }

        const first = items[0];
        const last = items[items.length - 1];
        const active = document.activeElement;
        if (e.shiftKey && (active === first || !el.contains(active))) {
            e.preventDefault(); last.focus({ preventScroll: true });
        } else if (!e.shiftKey && (active === last || !el.contains(active))) {
            e.preventDefault(); first.focus({ preventScroll: true });
        }
    };

    el.addEventListener('keydown', onKeyDown);
    STATE.set(el, { onKeyDown, prevFocus, prevBodyOverflow, lockScroll: !!opts.lockScroll });

    // Initial focus: first focusable element, else the panel itself (needs tabindex="-1").
    // { preventScroll: true } is required here: a Container-anchored drawer sliding in via
    // transform (Slide/Bounce) is still mid-animation at this point, so its untransformed box can
    // sit partially outside the container's visible area. `overflow: hidden` on that container
    // still permits PROGRAMMATIC scrolling even with no visible scrollbar, and a plain .focus()
    // call asks the browser to scroll the newly-focused element into view — so as the drawer's
    // transform continued to animate, the browser fought to keep scrolling the container to
    // compensate, panning the container's own content (very visible for Right/Bottom positions,
    // whose off-screen box sits further from the container's scroll origin than Left/Top's).
    // preventScroll suppresses that entirely; the drawer's own CSS transition is what should move
    // it into view, not a browser-driven scroll of an ancestor.
    const items = focusables(el);
    (items[0] || el).focus({ preventScroll: true });
}

// Undo activate(): remove the listener, unlock scroll, restore focus to where it was before open.
export function deactivate(el) {
    const s = el && STATE.get(el);
    if (!s) return;
    el.removeEventListener('keydown', s.onKeyDown);
    if (s.lockScroll) document.body.style.overflow = s.prevBodyOverflow || '';
    STATE.delete(el);
    try { s.prevFocus?.focus({ preventScroll: true }); } catch { /* element gone */ }
}
