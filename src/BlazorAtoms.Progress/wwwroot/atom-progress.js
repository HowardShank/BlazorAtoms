// AtomScrollProgressBar always imports and runs this module — not just as a fallback.
//
// Three problems, all confirmed live in-browser before fixing:
//
// 1. Scroll tracking: animation-timeline: scroll()'s default "nearest ancestor" auto-detection
//    does not work for a position:fixed element. Chrome resolves "nearest" via the fixed
//    element's containing-block chain (reparented to the viewport), not its DOM/flat-tree
//    ancestry — so inside an app-shell layout (fixed sidebar + an inner-scrolling content div,
//    not the whole document), a bare scroll() binds to the wrong scroller and the bar never
//    moves. Fix: find the real scroll container ourselves, then EXPLICITLY name a
//    scroll-timeline on it and point the bar's animation-timeline at that name — named
//    timelines are looked up by name, not by ancestry, so fixed positioning no longer interferes.
//
// 2. Sizing/position: a position:fixed element's percentage width/left, and its top:0/bottom:0,
//    resolve against the VIEWPORT, not its scroll container. Fix: measure the scroll container's
//    actual bounding rect and set the outer TRACK element's left/width/top-or-bottom as literal
//    px, on attach, on resize, on the container's own scroll, AND whenever the Position parameter
//    changes at runtime (updatePosition, called from OnParametersSetAsync — attach only ever
//    runs once, so a dropdown flipping Top→Bottom after first render needs its own resync path or
//    the OLD axis's inline style lingers and fights the new CSS class).
//
// 3. Fallback animation interference: CSS animations (even animation-play-state:paused, with
//    fill-mode:both) still force their "from" keyframe value onto the animated property AS AN
//    ANIMATED VALUE, which takes precedence over inline styles in the cascade. On a browser
//    without scroll-driven-animation support, the fallback branch drives width via
//    bar.style.width — but the paused atom-scroll-progress-kf animation (still declared in CSS,
//    for the native branch's benefit) pins width to 0% regardless, so the bar never showed any
//    progress at all. Fix: the fallback branch must fully disable the animation
//    (animation-name:none) before touching width, not just leave it paused.
export function attachScrollProgress(track, bar, position) {
    const state = { position };

    function findScrollParent(el) {
        let node = el.parentElement;
        while (node && node !== document.body) {
            const overflowY = getComputedStyle(node).overflowY;
            if ((overflowY === 'auto' || overflowY === 'scroll') && node.scrollHeight > node.clientHeight) {
                return node;
            }
            node = node.parentElement;
        }
        return document.scrollingElement || document.documentElement; // whole-page scroll
    }

    const scrollParent = findScrollParent(track);
    const isDocumentScroller = scrollParent === document.documentElement || scrollParent === document.body;

    function syncTrackGeometry() {
        const rect = isDocumentScroller
            ? { left: 0, width: window.innerWidth, top: 0, bottom: window.innerHeight }
            : scrollParent.getBoundingClientRect();

        track.style.left = rect.left + 'px';
        track.style.width = rect.width + 'px';

        if (state.position === 'top') {
            track.style.top = rect.top + 'px';
            track.style.bottom = '';
        } else {
            track.style.bottom = (window.innerHeight - rect.bottom) + 'px';
            track.style.top = '';
        }
    }

    syncTrackGeometry();
    window.addEventListener('resize', syncTrackGeometry);
    if (!isDocumentScroller) {
        // The container's own position can shift (e.g. its own scroll, or content above it
        // resizing) independent of a window resize — re-sync on its scroll too, cheap since it's
        // just a getBoundingClientRect read + a few style writes, not a full layout thrash.
        scrollParent.addEventListener('scroll', syncTrackGeometry, { passive: true });
    }

    track.__atomScrollProgressState = state;
    track.__atomScrollProgressSync = syncTrackGeometry;

    const supportsTimeline = typeof CSS !== 'undefined' && CSS.supports &&
        CSS.supports('animation-timeline', 'scroll()');

    if (supportsTimeline) {
        const name = '--atom-scroll-progress-' + Math.random().toString(36).slice(2);
        scrollParent.style.setProperty('scroll-timeline-name', name);
        scrollParent.style.setProperty('scroll-timeline-axis', 'y');
        // Never set animation-name from here — Blazor's CSS isolation renames
        // atom-scroll-progress-kf with a build-time scope hash (e.g.
        // atom-scroll-progress-kf-b-xxxxx), so a hardcoded JS-side literal doesn't match and
        // silently no-ops the animation (broke Chrome the first time this was tried). The name
        // and keyframes stay declared in AtomScrollProgressBar.razor.css, defaulting to
        // animation-play-state:paused. Here we only ever set animation-timeline + play-state.
        bar.style.setProperty('animation-timeline', name);
        bar.style.setProperty('animation-play-state', 'running');
        return;
    }

    // Fallback for browsers without scroll-driven animations: track scroll manually. Must fully
    // disable the CSS animation (not just leave it paused) — see problem 3 above.
    bar.style.setProperty('animation-name', 'none');

    const listenTarget = isDocumentScroller ? window : scrollParent;

    function update() {
        const scrollTop = isDocumentScroller ? (window.scrollY || scrollParent.scrollTop) : scrollParent.scrollTop;
        const scrollable = scrollParent.scrollHeight - scrollParent.clientHeight;
        const pct = scrollable > 0 ? (scrollTop / scrollable) * 100 : 0;
        bar.style.width = pct + '%';
    }

    listenTarget.addEventListener('scroll', update, { passive: true });
    window.addEventListener('resize', update);
    update();
    // No listener cleanup: attached to the scroll container (or window) directly, and the bar
    // element is disposed with its component — nothing DOM-scoped to leak.
}

// Called from OnParametersSetAsync whenever Position changes after the initial attach (attach
// itself only ever runs once per component instance). Re-syncs the track's inline top/bottom —
// without this, switching the Position dropdown swaps the CSS class but leaves the OLD axis's
// inline style in place, which wins over the new class's rule and the bar visually doesn't move.
export function updatePosition(track, position) {
    const state = track.__atomScrollProgressState;
    const sync = track.__atomScrollProgressSync;
    if (!state || !sync) return; // attach hasn't run yet (e.g. still in SSR/prerender)
    state.position = position;
    sync();
}
