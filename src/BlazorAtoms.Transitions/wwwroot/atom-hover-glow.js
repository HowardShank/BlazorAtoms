// JS fallback for browsers without CSS anchor positioning (anchor-name/position-anchor/anchor()).
// AtomHoverGlow only imports this module when AtomBrowserSupport reports no native support — on
// browsers that do, the effect is pure CSS and this file is never fetched. Event delegation on
// the container (rather than per-child listeners) means it works for any number/shape of direct
// children without enumerating them up front.
export function attachHoverGlow(container, indicator) {
    function directChildOf(target) {
        let el = target;
        while (el && el.parentElement !== container) {
            el = el.parentElement;
        }
        return el === container ? null : el;
    }

    function show(child) {
        const containerRect = container.getBoundingClientRect();
        const rect = child.getBoundingClientRect();
        indicator.style.left = (rect.left - containerRect.left) + 'px';
        indicator.style.top = (rect.top - containerRect.top) + 'px';
        indicator.style.width = rect.width + 'px';
        indicator.style.height = rect.height + 'px';
        indicator.style.opacity = '1';
    }

    function hide() {
        indicator.style.opacity = '0';
    }

    container.addEventListener('mouseover', e => {
        const child = directChildOf(e.target);
        if (child) show(child);
    });
    container.addEventListener('mouseout', e => {
        if (!e.relatedTarget || !container.contains(e.relatedTarget)) hide();
    });
    container.addEventListener('focusin', e => {
        const child = directChildOf(e.target);
        if (child) show(child);
    });
    container.addEventListener('focusout', e => {
        if (!e.relatedTarget || !container.contains(e.relatedTarget)) hide();
    });
    // Listeners are attached directly to the container DOM node and never detached explicitly —
    // when Blazor removes the component's subtree, the node (and its listeners) are garbage
    // collected with it, so there's nothing to leak.
}
