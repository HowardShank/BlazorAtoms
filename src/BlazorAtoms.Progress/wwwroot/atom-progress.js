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
//    scroll-timeline on it and point the bar's animation-timeline at that name — a named timeline
//    is resolved by name rather than through the fixed element's containing-block chain, so fixed
//    positioning no longer interferes. Note what this does NOT mean: a named timeline is still
//    scoped to the DESCENDANTS of the element declaring it, so the bar must be inside the
//    container it tracks for the name to be visible at all. See problem 8.
//
// 2. Sizing/position: a position:fixed element's percentage width/left, and its top:0/bottom:0,
//    resolve against the VIEWPORT, not its scroll container. Fix: measure the scroll container's
//    actual bounding rect and set the outer TRACK element's left/width/top-or-bottom as literal
//    px, on attach, on resize, on the container's own scroll, AND whenever Position/Width/Align
//    change at runtime (updateLayout).
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
//
// 5. Container resolution is a RACE, so it must be repeatable, not once-and-done.
//    findScrollParent only accepts a candidate that is *already overflowing* — correct (an
//    overflow:auto box that never scrolls is not this bar's scroller), but it means the answer
//    depends on when you ask. Under Blazor's InteractiveAuto the component is instantiated twice:
//    the server pass attaches before the content has overflowed, resolves to the document, and
//    the bar renders as a full-width strip across the top of the viewport until the WebAssembly
//    pass re-attaches. Same class of bug with lazily-loaded content or streamed SSR in any
//    hosting model. Fix: two signals, both funnelling into one rAF-debounced re-resolve.
//      - A ResizeObserver on document.body + the current container. Catches the container (or the
//        page) actually changing size — a window resize, a sidebar opening.
//      - A CAPTURE-phase scroll listener on document. Needed because ResizeObserver reports a
//        change to an element's own box, never to its scrollHeight: in an app-shell layout
//        (main { height:100vh; overflow:hidden } wrapping an inner-scrolling content div) neither
//        body nor the content div ever resizes when content grows inside it, so the
//        not-overflowing → overflowing transition is invisible to it. Scroll events don't bubble,
//        but a capture-phase listener on document sees them from any element, so the first scroll
//        of the real container is the signal that it has become scrollable.
//    The C# side additionally keeps the track hidden until attach reports a successful measure, so
//    a mis-resolved bar is never painted in the first place.
//
// 6. Multiple bars on one page must not kill each other. The scroll-timeline name is per-instance
//    random but lives on the CONTAINER, so two bars resolving to the same container had the
//    second attach overwrite scroll-timeline-name — leaving the first bar's animation-timeline
//    pointing at a name present on no element, silently frozen (native/Chromium branch only).
//    Fix: one timeline per container, reference-counted in a registry on the container. Same
//    container + same axis means sharing the timeline is the correct semantics, not a workaround.
//
// 7. Teardown. Earlier revisions registered window/container listeners and claimed "nothing
//    DOM-scoped to leak" — untrue: those outlive the component that created them. Everything
//    registered here is tracked on the track element and removed by detachScrollProgress, which
//    AtomScrollProgressBar calls from DisposeAsync.
//
// 8. The native path requires the bar to be INSIDE the container it tracks. A named scroll
//    timeline is referenceable only by descendants of the element that declares it, so pointing a
//    bar at an unrelated element via ScrollContainer leaves animation-timeline unresolvable — and
//    an inactive timeline still applies the keyframes' fill (problem 4), so the bar sits at 100%
//    and ignores scrolling. Confirmed live with a bar aimed at a sibling scroll box. bind() tests
//    container.contains(track) and drops that binding to the manual path, which needs nothing but
//    scrollTop. This is per-binding, not per-browser, hence state.usingTimeline alongside
//    state.supportsTimeline: the same page can run one bar natively and another manually.

const REGISTRY_KEY = '__atomScrollProgressTimeline'; // on the container: { name, refs:Set<track> }
const STATE_KEY = '__atomScrollProgressState';       // on the track

// ---- container resolution ----------------------------------------------------------------------

// Same forgiving-selector idiom as BlazorAtoms.Navigation's AtomScrollTo: a bad selector from a
// consumer must not throw out of the whole attach.
function safeQuery(selector) {
    if (!selector) return null;
    try { return document.querySelector(selector); } catch { return null; }
}

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

// An explicit ScrollContainer selector wins outright; if it matches nothing we fall back to the
// ancestor walk rather than silently doing nothing (mirrors AtomScrollTo.ScrollContainer).
function resolveContainer(track, scrollContainer) {
    return safeQuery(scrollContainer) || findScrollParent(track);
}

function isDocumentScroller(el) {
    return el === document.documentElement || el === document.body;
}

// ---- shared scroll-timeline, reference counted per container -----------------------------------

function acquireTimeline(container, track) {
    let registry = container[REGISTRY_KEY];
    if (!registry) {
        registry = {
            name: '--atom-scroll-progress-' + Math.random().toString(36).slice(2),
            refs: new Set(),
        };
        container[REGISTRY_KEY] = registry;
        container.style.setProperty('scroll-timeline-name', registry.name);
        container.style.setProperty('scroll-timeline-axis', 'y');
    }
    registry.refs.add(track);
    return registry.name;
}

function releaseTimeline(container, track) {
    const registry = container && container[REGISTRY_KEY];
    if (!registry) return;
    registry.refs.delete(track);
    if (registry.refs.size === 0) {
        container.style.removeProperty('scroll-timeline-name');
        container.style.removeProperty('scroll-timeline-axis');
        delete container[REGISTRY_KEY];
    }
}

// ---- geometry ----------------------------------------------------------------------------------

function resolveTrackWidth(state, containerRect) {
    if (!state.width) return containerRect.width;

    // Probe inside the actual container (or body, for whole-page scroll) so percentages/vw/
    // rem/calc() etc. resolve against the right reference box, not the viewport.
    const probeParent = state.isDocumentScroller ? document.body : state.container;
    const probe = document.createElement('div');
    probe.style.cssText = 'position:absolute; visibility:hidden; height:0; pointer-events:none;';
    probe.style.width = state.width;
    probeParent.appendChild(probe);
    const resolved = probe.getBoundingClientRect().width;
    probe.remove();
    return resolved;
}

function syncTrackGeometry(state) {
    const containerRect = state.isDocumentScroller
        ? { left: 0, width: window.innerWidth, top: 0, bottom: window.innerHeight }
        : state.container.getBoundingClientRect();

    const trackWidth = resolveTrackWidth(state, containerRect);
    let trackLeft;
    if (state.align === 'center') {
        trackLeft = containerRect.left + (containerRect.width - trackWidth) / 2;
    } else if (state.align === 'end') {
        trackLeft = containerRect.left + containerRect.width - trackWidth;
    } else {
        trackLeft = containerRect.left;
    }

    state.track.style.left = trackLeft + 'px';
    state.track.style.width = trackWidth + 'px';

    if (state.position === 'top') {
        state.track.style.top = containerRect.top + 'px';
        state.track.style.bottom = '';
    } else {
        state.track.style.bottom = (window.innerHeight - containerRect.bottom) + 'px';
        state.track.style.top = '';
    }
}

function updateFallbackFill(state) {
    const scrollTop = state.isDocumentScroller
        ? (window.scrollY || state.container.scrollTop)
        : state.container.scrollTop;
    const scrollable = state.container.scrollHeight - state.container.clientHeight;
    const pct = scrollable > 0 ? (scrollTop / scrollable) * 100 : 0;
    state.bar.style.width = pct + '%';
}

// ---- bind / unbind ----------------------------------------------------------------------------

function bind(state) {
    state.container = resolveContainer(state.track, state.scrollContainer);
    state.isDocumentScroller = isDocumentScroller(state.container);

    syncTrackGeometry(state);

    state.onResize = () => syncTrackGeometry(state);
    window.addEventListener('resize', state.onResize);

    if (!state.isDocumentScroller) {
        // The container's own position/size can shift (e.g. its own scroll, or content above it
        // resizing) independent of a window resize — re-sync on its scroll too, cheap since it's
        // just a getBoundingClientRect read + a few style writes, not a full layout thrash.
        state.onContainerScroll = state.onResize;
        state.container.addEventListener('scroll', state.onContainerScroll, { passive: true });
    }

    // A named scroll-timeline is only referenceable by DESCENDANTS of the element that declares
    // it, and the bar is not necessarily inside the container it tracks — that's the whole point of
    // ScrollContainer, which can name any element on the page. When the track sits outside,
    // animation-timeline resolves to nothing, and an INACTIVE timeline still applies the keyframes'
    // fill (see problem 4): the bar pins at 100% and ignores scrolling entirely. Detect that and
    // take the manual path, which only needs scrollTop and works from anywhere in the DOM.
    // (CSS timeline-scope could widen the name's visibility instead and keep the native path — a
    // possible optimisation, but it's a second platform dependency plus list management on a
    // shared property, for a case that is rare by construction.)
    state.usingTimeline = state.supportsTimeline && state.container.contains(state.track);

    if (state.usingTimeline) {
        const name = acquireTimeline(state.container, state.track);
        // Clear any animation-name:none left by a previous fallback binding, or the native
        // animation below stays disabled after a rebind.
        state.bar.style.removeProperty('animation-name');
        // Never set animation-name to a literal from here — Blazor's CSS isolation renames
        // atom-scroll-progress-kf with a build-time scope hash (e.g.
        // atom-scroll-progress-kf-b-xxxxx), so a hardcoded JS-side literal doesn't match and
        // silently no-ops the animation (broke Chrome the first time this was tried). The name
        // and keyframes stay declared in AtomScrollProgressBar.razor.css. Here we only ever set
        // animation-timeline + play-state.
        state.bar.style.setProperty('animation-timeline', name);
        state.bar.style.setProperty('animation-play-state', 'running');
        return;
    }

    // Manual path: browsers without scroll-driven animations at all, and any binding where the
    // track sits outside the container (above). Must fully disable the CSS animation (not just
    // leave it paused) — see problem 4 above.
    state.bar.style.setProperty('animation-name', 'none');
    state.onFallbackScroll = () => updateFallbackFill(state);
    state.fallbackTarget = state.isDocumentScroller ? window : state.container;
    state.fallbackTarget.addEventListener('scroll', state.onFallbackScroll, { passive: true });
    window.addEventListener('resize', state.onFallbackScroll);
    updateFallbackFill(state);
}

function unbind(state) {
    if (state.onResize) {
        window.removeEventListener('resize', state.onResize);
    }
    if (state.onContainerScroll && state.container) {
        state.container.removeEventListener('scroll', state.onContainerScroll);
    }
    if (state.onFallbackScroll) {
        if (state.fallbackTarget) {
            state.fallbackTarget.removeEventListener('scroll', state.onFallbackScroll);
        }
        window.removeEventListener('resize', state.onFallbackScroll);
    }
    // usingTimeline, not supportsTimeline: a binding can fall back to the manual path on a
    // timeline-capable browser (track outside the container), and then there is no claim to release.
    if (state.usingTimeline) {
        releaseTimeline(state.container, state.track);
        state.usingTimeline = false;
    }

    state.onResize = null;
    state.onContainerScroll = null;
    state.onFallbackScroll = null;
    state.fallbackTarget = null;
}

function rebind(state) {
    const previousContainer = state.container;
    unbind(state);
    bind(state);

    if (state.resizeObserver) {
        if (previousContainer && previousContainer !== state.container && !isDocumentScroller(previousContainer)) {
            state.resizeObserver.unobserve(previousContainer);
        }
        if (!state.isDocumentScroller) {
            state.resizeObserver.observe(state.container);
        }
    }
}

// The correct container depends on what is overflowing RIGHT NOW (see problem 5), so re-resolve
// whenever that could have changed. Both signals below funnel through here; the rAF guard means a
// burst of layout changes, or a stream of scroll events, costs one pass per frame at most.
function scheduleRecheck(state) {
    if (state.rafId) return;

    state.rafId = requestAnimationFrame(() => {
        state.rafId = 0;
        if (!state.track.isConnected) return;

        const next = resolveContainer(state.track, state.scrollContainer);
        if (next !== state.container) {
            rebind(state);
        } else {
            syncTrackGeometry(state);
        }
    });
}

// Not bound in bind()/unbind(): neither of these is container-scoped, so they survive a rebind and
// are torn down only by detachScrollProgress.
function observeLayout(state) {
    // Signal 1 — the container or the page changing size.
    if (typeof ResizeObserver !== 'undefined') {
        state.resizeObserver = new ResizeObserver(() => scheduleRecheck(state));
        state.resizeObserver.observe(document.body);
        if (!state.isDocumentScroller) {
            state.resizeObserver.observe(state.container);
        }
    }

    // Signal 2 — anything on the page scrolling. This is the one that catches a container which
    // has become scrollable since we last resolved, which a ResizeObserver structurally cannot
    // see (its own box never changed — only its scrollHeight did). Capture phase because scroll
    // events do not bubble; on document, capture still sees them from any element.
    state.onAnyScroll = () => scheduleRecheck(state);
    document.addEventListener('scroll', state.onAnyScroll, { capture: true, passive: true });
}

// ---- exports ----------------------------------------------------------------------------------

// Returns true once the track has been measured against a resolved container. AtomScrollProgressBar
// keeps the track hidden until this resolves true, so the bar is never painted at the pre-JS
// full-viewport default.
export function attachScrollProgress(track, bar, position, width, align, scrollContainer) {
    if (track[STATE_KEY]) return true; // already attached (defensive; C# attaches once)

    const state = {
        track,
        bar,
        position,
        width,
        align,
        scrollContainer,
        container: null,
        isDocumentScroller: false,
        supportsTimeline: typeof CSS !== 'undefined' && !!CSS.supports &&
            CSS.supports('animation-timeline', 'scroll()'),
        usingTimeline: false, // set per binding — see bind()

        onResize: null,
        onContainerScroll: null,
        onFallbackScroll: null,
        fallbackTarget: null,
        onAnyScroll: null,
        resizeObserver: null,
        rafId: 0,
    };

    track[STATE_KEY] = state;
    bind(state);
    observeLayout(state);
    return true;
}

// Called from OnAfterRenderAsync whenever Position/Width/Align/ScrollContainer change after the
// initial attach (attach itself only ever runs once per component instance). Re-syncs the track's
// inline geometry — without this, changing one of these swaps CSS classes/parameters but leaves the
// OLD inline geometry in place, which wins over new values and the bar visually doesn't update. A
// changed ScrollContainer additionally needs a full rebind, since it names a different scroller.
export function updateLayout(track, position, width, align, scrollContainer) {
    const state = track && track[STATE_KEY];
    if (!state) return false; // attach hasn't run yet (e.g. still in SSR/prerender)

    const containerChanged = state.scrollContainer !== scrollContainer;
    state.position = position;
    state.width = width;
    state.align = align;
    state.scrollContainer = scrollContainer;

    if (containerChanged) {
        rebind(state);
    } else {
        syncTrackGeometry(state);
    }
    return true;
}

// Full teardown — see problem 7. Every listener, the ResizeObserver, this track's claim on the
// container's shared scroll-timeline, and the inline geometry all go.
export function detachScrollProgress(track) {
    const state = track && track[STATE_KEY];
    if (!state) return;

    if (state.rafId) {
        cancelAnimationFrame(state.rafId);
        state.rafId = 0;
    }
    if (state.resizeObserver) {
        state.resizeObserver.disconnect();
        state.resizeObserver = null;
    }
    if (state.onAnyScroll) {
        // Capture flag must match the one used to register, or the removal silently no-ops.
        document.removeEventListener('scroll', state.onAnyScroll, { capture: true });
        state.onAnyScroll = null;
    }
    unbind(state);

    track.style.left = '';
    track.style.width = '';
    track.style.top = '';
    track.style.bottom = '';

    delete track[STATE_KEY];
}
