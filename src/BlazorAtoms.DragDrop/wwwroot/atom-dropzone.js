// BlazorAtoms.DragDrop — auto-scroll during HTML5 drag.
//
// HTML5 DnD auto-scrolls the document but NOT arbitrary scrollable containers, so a long
// list inside a scrollable pane won't scroll while a user drags past the visible edge.
// This module wires a `dragover` listener + requestAnimationFrame loop to scroll the
// zone (or its nearest scrollable ancestor) toward the pointer whenever the pointer is
// within `edgeSize` pixels of an edge.

const HANDLES = new WeakMap();

function findScrollParent(el) {
    let n = el;
    while (n && n !== document.body) {
        const s = getComputedStyle(n);
        const oy = s.overflowY;
        if ((oy === 'auto' || oy === 'scroll') && n.scrollHeight > n.clientHeight) return n;
        const ox = s.overflowX;
        if ((ox === 'auto' || ox === 'scroll') && n.scrollWidth > n.clientWidth) return n;
        n = n.parentElement;
    }
    return document.scrollingElement || document.documentElement;
}

export function enableAutoScroll(zone, edgeSize, speed) {
    if (!zone) return null;
    if (HANDLES.has(zone)) disableAutoScroll(zone);

    const state = { x: 0, y: 0, active: false, rafId: 0, target: findScrollParent(zone) };
    const edge = Math.max(4, edgeSize | 0);
    const px = Math.max(1, speed | 0);

    const onOver = (e) => {
        state.x = e.clientX;
        state.y = e.clientY;
        if (!state.active) {
            state.active = true;
            state.rafId = requestAnimationFrame(tick);
        }
    };

    const onEnd = () => {
        state.active = false;
        if (state.rafId) cancelAnimationFrame(state.rafId);
        state.rafId = 0;
    };

    function tick() {
        if (!state.active) return;
        const t = state.target;
        const r = t === document.scrollingElement || t === document.documentElement
            ? { top: 0, left: 0, bottom: window.innerHeight, right: window.innerWidth }
            : t.getBoundingClientRect();

        let dy = 0, dx = 0;
        if (state.y < r.top + edge) dy = -px;
        else if (state.y > r.bottom - edge) dy = px;
        if (state.x < r.left + edge) dx = -px;
        else if (state.x > r.right - edge) dx = px;

        if (dx !== 0 || dy !== 0) {
            if (t === document.scrollingElement || t === document.documentElement) window.scrollBy(dx, dy);
            else t.scrollBy(dx, dy);
        }
        state.rafId = requestAnimationFrame(tick);
    }

    // dragover on the zone (bubbling from item wrappers/spacers).
    zone.addEventListener('dragover', onOver);
    // Any drag end/drop anywhere stops the loop.
    document.addEventListener('dragend', onEnd, true);
    document.addEventListener('drop', onEnd, true);

    HANDLES.set(zone, { onOver, onEnd });
    return null;
}

export function disableAutoScroll(zone) {
    const h = HANDLES.get(zone);
    if (!h) return;
    zone.removeEventListener('dragover', h.onOver);
    document.removeEventListener('dragend', h.onEnd, true);
    document.removeEventListener('drop', h.onEnd, true);
    HANDLES.delete(zone);
}
