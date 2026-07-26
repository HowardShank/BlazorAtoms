// AtomScrollProgressBar always imports and runs this module — not just as a fallback.
//
// Two problems, both confirmed live in-browser before fixing:
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
// 2. Horizontal sizing: a position:fixed element's percentage width/left resolve against the
//    VIEWPORT, not its scroll container — so even with tracking fixed, a naive fixed bar would
//    span the full screen width instead of matching a narrower content column (e.g. next to a
//    sidebar). Fix: measure the scroll container's actual bounding rect and set the outer TRACK
//    element's left/width as literal px, once on attach and again on resize. The inner fill bar
//    (0%→100%) then sizes relative to that correctly-positioned track, not the viewport.
export function attachScrollProgress(track, bar) {
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
        if (isDocumentScroller) {
            track.style.left = '0px';
            track.style.width = '100%';
            return;
        }
        const rect = scrollParent.getBoundingClientRect();
        track.style.left = rect.left + 'px';
        track.style.width = rect.width + 'px';
    }

    syncTrackGeometry();
    window.addEventListener('resize', syncTrackGeometry);

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
        // animation-play-state:paused (so browsers that never reach this branch — no
        // scroll-driven animation support — never run it at all, avoiding the separate
        // instant-snap-to-100%-and-hold bug that caused Firefox's "always full width" symptom).
        // Here we only ever set animation-timeline + play-state.
        bar.style.setProperty('animation-timeline', name);
        bar.style.setProperty('animation-play-state', 'running');
        return;
    }

    // Fallback for browsers without scroll-driven animations: track scroll manually.
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
