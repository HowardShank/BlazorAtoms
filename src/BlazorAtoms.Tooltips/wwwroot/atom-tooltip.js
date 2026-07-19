// Cursor-follow positioning for AtomTooltip's Placement.Cursor mode.
//
// The library is otherwise JS-free; this module is loaded lazily by the component
// (via IJSObjectReference) only when a tooltip actually uses Cursor placement — no
// <script> tag, no DI registration, nothing for the consumer to wire up.
//
// It writes the pointer position onto the bubble as CSS custom properties; the
// stylesheet does the actual placing. Logic/state stay in C#; JS only reports the
// coordinate the platform won't expose to CSS.

const HANDLER = Symbol("atomTooltipCursor");

// Attach a pointermove listener to `trigger` that pushes the cursor position onto
// `bubble` as --tip-cursor-x / --tip-cursor-y. Idempotent: re-attaching first detaches.
export function attach(trigger, bubble) {
    if (!trigger || !bubble) return;
    detach(trigger);

    const onMove = (e) => {
        bubble.style.setProperty("--tip-cursor-x", Math.round(e.clientX) + "px");
        bubble.style.setProperty("--tip-cursor-y", Math.round(e.clientY) + "px");
    };

    trigger[HANDLER] = onMove;
    // pointermove covers mouse, pen, and touch-drag uniformly.
    trigger.addEventListener("pointermove", onMove, {passive: true});
}

// Remove the listener previously added by attach(). Safe to call more than once.
export function detach(trigger) {
    const onMove = trigger && trigger[HANDLER];
    if (onMove) {
        trigger.removeEventListener("pointermove", onMove);
        delete trigger[HANDLER];
    }
}
