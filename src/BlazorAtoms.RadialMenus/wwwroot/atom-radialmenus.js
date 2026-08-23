// AtomRadialMenu's JS module. Everything positional is computed in C# from RadialLayout; this file
// only supplies the three things the browser alone knows:
//
//   1. how much room the menu's container actually has (RadiusMode.FitContainer),
//   2. how wide a string really is in the resolved font (RadialMenuSizeMode.Measure),
//   3. that a pointer went down somewhere else on the page (CloseOnOutsideClick).
//
// The alternative to (3) is a full-viewport backdrop element, which works but swallows pointer
// events over the rest of the page and so breaks Trigger=Hover. Six lines of listener is cheaper
// than that trade.
//
// The module is imported by the component itself — no <script> tag and no DI registration.

const instances = new WeakMap();

// One reused canvas for text measurement. Creating a context per call is the expensive part;
// measureText itself is cheap, which is why the C# side batches whole label sets through it.
let measureCanvas = null;

/**
 * Wires up one menu instance.
 * @param {HTMLElement} host the menu's root element
 * @param {object} dotNetRef .NET reference receiving OnHostResized / OnOutsideClick
 * @param {{watchResize?: boolean, outsideClick?: boolean}} options which features to attach
 */
export function attach(host, dotNetRef, options) {
    if (!host || instances.has(host)) return;

    const opts = options ?? {};
    const state = { host, dotNetRef, observer: null, onPointerDown: null };

    if (opts.watchResize) {
        // The menu's own box is only as big as its center button — it is the PARENT that bounds how
        // far the ring may reach, so that is what gets observed.
        const box = host.parentElement ?? host;
        state.observer = new ResizeObserver(entries => {
            for (const entry of entries) {
                const r = entry.contentRect;
                invoke(state, "OnHostResized", r.width, r.height);
            }
        });
        state.observer.observe(box);

        // Report once immediately: a ResizeObserver fires on observe in every current browser, but
        // relying on that leaves the first layout dependent on unspecified behaviour.
        const initial = box.getBoundingClientRect();
        invoke(state, "OnHostResized", initial.width, initial.height);
    }

    if (opts.outsideClick) {
        state.onPointerDown = event => {
            if (!host.isConnected) return;
            if (event.composedPath().includes(host)) return;
            invoke(state, "OnOutsideClick");
        };
        // Capture phase, so a handler that stops propagation cannot leave the menu stuck open.
        document.addEventListener("pointerdown", state.onPointerDown, true);
    }

    instances.set(host, state);
}

/** Removes every listener and observer for one menu. Safe to call more than once. */
export function detach(host) {
    const state = host && instances.get(host);
    if (!state) return;

    state.observer?.disconnect();
    if (state.onPointerDown) {
        document.removeEventListener("pointerdown", state.onPointerDown, true);
    }

    state.dotNetRef = null;
    instances.delete(host);
}

/**
 * Real widths for a batch of labels, in the font the menu actually renders in.
 *
 * The font is read back off the live element rather than rebuilt from parameters, so an inherited
 * family, weight or stretch is accounted for — a width measured in the wrong family is worse than
 * an honest estimate.
 *
 * @param {HTMLElement} host the menu's root element, used to resolve the computed font
 * @param {string} fallbackSize e.g. "13px", used when the element is not laid out yet
 * @param {string[]} labels strings to measure
 * @returns {number[]} widths in CSS pixels, in the same order
 */
export function measure(host, fallbackSize, labels) {
    if (!Array.isArray(labels) || labels.length === 0) return [];

    measureCanvas ??= document.createElement("canvas");
    const ctx = measureCanvas.getContext("2d");
    if (!ctx) return labels.map(() => 0);

    ctx.font = resolveFont(host, fallbackSize);
    return labels.map(text => ctx.measureText(text ?? "").width);
}

/** The host's box, for a caller that wants to fit the menu to it without observing changes. */
export function rect(host) {
    if (!host) return { width: 0, height: 0 };
    const box = (host.parentElement ?? host).getBoundingClientRect();
    return { width: box.width, height: box.height };
}

function resolveFont(host, fallbackSize) {
    const style = host ? getComputedStyle(host) : null;
    if (!style || !style.fontFamily) return `${fallbackSize} sans-serif`;

    const size = style.fontSize && style.fontSize !== "0px" ? style.fontSize : fallbackSize;
    const weight = style.fontWeight || "normal";
    const fontStyle = style.fontStyle || "normal";
    return `${fontStyle} ${weight} ${size} ${style.fontFamily}`;
}

function invoke(state, method, ...args) {
    // A circuit can drop between the browser event and the interop call; the component's own
    // teardown handles the .NET side, so a rejected invoke here is expected and not an error.
    state.dotNetRef?.invokeMethodAsync(method, ...args).catch(() => { });
}
