// Suppresses the browser's default scrolling for the tab strip's own navigation keys.
//
// Why this exists at all: the C# handler already implements Arrow/Home/End navigation, but Blazor
// decides `@onkeydown:preventDefault` at RENDER time, not per event — so it can only be all-or-nothing
// for keydown on the strip, and applying it to everything would also swallow Tab and trap focus inside
// the tablist. That leaves the default action running alongside our handler: an arrow key both moves
// the selection and scrolls whatever is scrollable behind the strip. Most visible with vertical tabs,
// whose Up/Down keys usually have somewhere to scroll.
//
// The library needs no <script> tag and no DI registration — AtomTabs lazy-imports this module itself
// via IJSObjectReference. Logic and state stay in C#; this only cancels a default action that C# has
// no way to reach.

const HANDLER = Symbol("atomTabsKeyGuard");

// Keys this component consumes, per axis. Deliberately mirrors the C# switch in AtomTabs: cancelling
// the default for a key the component does NOT handle would silently break legitimate page scrolling,
// so a horizontal strip must leave Up/Down alone and a vertical one Left/Right.
const KEYS = {
    horizontal: ["ArrowLeft", "ArrowRight", "Home", "End"],
    vertical: ["ArrowUp", "ArrowDown", "Home", "End"],
};

// Elements whose own arrow-key behavior must win. A caller can put arbitrary markup in a tab via
// ChildContent, including a text field — cancelling arrows there would break the caret.
function isTextEntry(el) {
    if (!el) return false;
    if (el.isContentEditable) return true;

    const tag = el.tagName;
    return tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT";
}

// Attach the guard to a tablist element. Idempotent: re-attaching first detaches, so a re-render that
// somehow calls this twice cannot stack listeners.
export function attach(tablist) {
    if (!tablist) return;
    detach(tablist);

    const onKeyDown = (e) => {
        // Read the axis off the element each time rather than capturing it, so changing the
        // Orientation parameter at runtime needs no re-attach.
        const axis = tablist.getAttribute("aria-orientation") === "vertical" ? "vertical" : "horizontal";
        if (!KEYS[axis].includes(e.key)) return;
        if (isTextEntry(e.target)) return;

        // A modified keypress is a browser/OS shortcut (e.g. Home with Ctrl), not tab navigation —
        // and the C# handler ignores those too.
        if (e.ctrlKey || e.metaKey || e.altKey || e.shiftKey) return;

        e.preventDefault();
    };

    tablist[HANDLER] = onKeyDown;
    tablist.addEventListener("keydown", onKeyDown);
}

// Remove the listener added by attach(). Safe to call more than once, and on an element that was
// never attached.
export function detach(tablist) {
    const onKeyDown = tablist && tablist[HANDLER];
    if (onKeyDown) {
        tablist.removeEventListener("keydown", onKeyDown);
        delete tablist[HANDLER];
    }
}
