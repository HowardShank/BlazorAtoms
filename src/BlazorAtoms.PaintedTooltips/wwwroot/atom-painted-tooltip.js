// Cursor-follow positioning for AtomPaintedTooltip's Placement.Cursor mode.
// Lazy-loaded by the component only when Cursor placement is used — no <script> tag, no DI.

const HANDLER = Symbol("atomPaintedTooltipCursor");

export function attach(trigger, bubble) {
    if (!trigger || !bubble) return;
    detach(trigger);
    const onMove = (e) => {
        bubble.style.setProperty("--tip-cursor-x", e.clientX + "px");
        bubble.style.setProperty("--tip-cursor-y", e.clientY + "px");
    };
    trigger[HANDLER] = onMove;
    trigger.addEventListener("pointermove", onMove);
}

export function detach(trigger) {
    const onMove = trigger && trigger[HANDLER];
    if (onMove) {
        trigger.removeEventListener("pointermove", onMove);
        delete trigger[HANDLER];
    }
}
