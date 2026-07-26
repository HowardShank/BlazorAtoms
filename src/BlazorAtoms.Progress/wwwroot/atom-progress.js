// AtomScrollProgressBar always imports and runs this module — not just as a fallback.
//
// Problems solved here, all confirmed live in-browser before fixing:
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
//    px, on attach, on resize, on the container's own scroll, AND whenever Position/Width/Align
//    change at runtime (updateLayout, called from OnAfterRenderAsync — attach only ever runs
//    once, so a control changing one of these after first render needs its own resync path or
//    the OLD values linger and fight the new ones).
//
// 3. Arbitrary Width units: rather than reimplementing CSS length-unit math (%, px, rem, vw,
//    calc(), ...) in JS, resolve Width the way the browser already knows how to: apply it to a
//    hidden probe element placed INSIDE the actual scroll container (so percentages resolve
//    against it, not the viewport), read the probe's real rendered pixel width, then apply that
//    px value to the (position:fixed) track. This is the standard trick for resolving arbitrary
//    CSS units against a specific reference box without hardcoding unit conversion.
//
// 4. Fallback animation interference: CSS animations (even animation-play-state:paused, with
//    fill-mode:both) still force their "from"/"to" keyframe value onto the animated property AS
//    AN ANIMATED VALUE, which takes precedence over inline styles in the cascade — confirmed live
//    that a 0-duration animation (the default when no animation-duration is set) is immediately
//    "at end" regardless of play-state, permanently pinning width to 100% even while paused. Fix:
//    the fallback branch must fully disable the animation (animation-name:none) before touching
//    width, not just leave it paused.
export function attachScrollProgress(track, bar, position, width, align) {
    const state = { position, width, align };

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

    function resolveTrackWidth(containerRect) {
        if (!state.width) return containerRect.width;

        // Probe inside the actual container (or body, for whole-page scroll) so percentages/vw/
        // rem/calc() etc. resolve against the right reference box, not the viewport.
        const probeParent = isDocumentScroller ? document.body : scrollParent;
        const probe = document.createElement('div');
        probe.style.cssText = 'position:absolute; visibility:hidden; height:0; pointer-events:none;';
        probe.style.width = state.width;
        probeParent.appendChild(probe);
        const resolved = probe.getBoundingClientRect().width;
        probe.remove();
        return resolved;
    }

    function syncTrackGeometry() {
        const containerRect = isDocumentScroller
            ? { left: 0, width: window.innerWidth, top: 0, bottom: window.innerHeight }
            : scrollParent.getBoundingClientRect();

        const trackWidth = resolveTrackWidth(containerRect);
        let trackLeft;
        if (state.align === 'center') {
            trackLeft = containerRect.left + (containerRect.width - trackWidth) / 2;
        } else if (state.align === 'end') {
            trackLeft = containerRect.left + containerRect.width - trackWidth;
        } else {
            trackLeft = containerRect.left;
        }

        track.style.left = trackLeft + 'px';
        track.style.width = trackWidth + 'px';

        if (state.position === 'top') {
            track.style.top = containerRect.top + 'px';
            track.style.bottom = '';
        } else {
            track.style.bottom = (window.innerHeight - containerRect.bottom) + 'px';
            track.style.top = '';
        }
    }

    syncTrackGeometry();
    window.addEventListener('resize', syncTrackGeometry);
    if (!isDocumentScroller) {
        // The container's own position/size can shift (e.g. its own scroll, or content above it
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
        // and keyframes stay declared in AtomScrollProgressBar.razor.css. Here we only ever set
        // animation-timeline + play-state.
        bar.style.setProperty('animation-timeline', name);
        bar.style.setProperty('animation-play-state', 'running');
        return;
    }

    // Fallback for browsers without scroll-driven animations: track scroll manually. Must fully
    // disable the CSS animation (not just leave it paused) — see problem 4 above.
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

// Called from OnAfterRenderAsync whenever Position/Width/Align change after the initial attach
// (attach itself only ever runs once per component instance). Re-syncs the track's inline
// geometry — without this, changing one of these swaps CSS classes/parameters but leaves the OLD
// inline geometry in place, which wins over new values and the bar visually doesn't update.
export function updateLayout(track, position, width, align) {
    const state = track.__atomScrollProgressState;
    const sync = track.__atomScrollProgressSync;
    if (!state || !sync) return; // attach hasn't run yet (e.g. still in SSR/prerender)
    state.position = position;
    state.width = width;
    state.align = align;
    sync();
}
