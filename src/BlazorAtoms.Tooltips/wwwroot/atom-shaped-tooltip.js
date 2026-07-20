// Cursor-follow positioning for AtomShapedTooltip's Placement.Cursor mode.
// Lazy-loaded by the component (IJSObjectReference) only when Cursor placement is used —
// no <script> tag, no DI. Writes the pointer position onto the bubble as CSS custom
// properties; the stylesheet does the placing.

const HANDLER = Symbol("atomShapedTooltipCursor");

export function attach(trigger, bubble) {
    if (!trigger || !bubble) return;
    detach(trigger);
    const onMove = (e) => {
        bubble.style.setProperty("--tip-cursor-x", Math.round(e.clientX) + "px");
        bubble.style.setProperty("--tip-cursor-y", Math.round(e.clientY) + "px");
    };
    trigger[HANDLER] = onMove;
    trigger.addEventListener("pointermove", onMove, { passive: true });
}

export function detach(trigger) {
    const onMove = trigger && trigger[HANDLER];
    if (onMove) {
        trigger.removeEventListener("pointermove", onMove);
        delete trigger[HANDLER];
    }
}
