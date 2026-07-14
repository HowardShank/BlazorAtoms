// atom-highlighter.js
// Recursively walks a container's DOM and wraps keyword matches in <mark> elements, scoped
// strictly to that container. Called by AtomHighlighter after every render — including every
// keystroke while a caller is live-editing Keywords — so each call first unwraps this instance's
// own previous marks back to plain text before re-scanning. Without that, marks created under a
// transient keyword state (e.g. a single letter mid-edit) would be treated as "already handled"
// forever and never reconcile with the current keyword list.
//
// Ownership of a mark (which instance may unwrap it) is tracked by a per-instance `owner` id
// (options.owner), NOT by cssClass — cssClass is purely presentational. Two nested
// AtomHighlighter instances that both use the default HighlightClass would otherwise collide:
// an outer instance's querySelectorAll("mark") reaches into an inner instance's already-marked
// content too, since it's inside the outer's DOM subtree, and stripping-by-class would erase
// the inner instance's marks. Tagging each mark with dataset.owner lets unmark() tell "mine" from
// "someone else's" regardless of what class either instance uses.
//
// Uses DOM APIs only (createElement / createTextNode / DocumentFragment) — never innerHTML —
// so injected text can never be reinterpreted as markup.

function escapeRegExp(value) {
    return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function buildRegex(keywords, caseSensitive, wholeWord) {
    const terms = (keywords || [])
        .filter((k) => typeof k === "string" && k.length > 0)
        .map(escapeRegExp);
    if (terms.length === 0) return null;

    const alternation = `(?:${terms.join("|")})`;
    const pattern = wholeWord ? `\\b${alternation}\\b` : alternation;
    return new RegExp(`(${pattern})`, caseSensitive ? "g" : "gi");
}

// Unwraps every <mark> this instance owns (matched by dataset.owner, NOT cssClass) back into
// plain text, then normalizes so adjacent text nodes merge — letting the next scan match across
// what used to be a mark boundary. Marks belonging to a different AtomHighlighter instance (a
// different owner id) are left untouched; they aren't ours to unwrap, regardless of cssClass.
function unmark(container, owner) {
    const marks = Array.from(container.querySelectorAll("mark")).filter((m) =>
        m.dataset.owner === owner
    );
    for (const mark of marks) {
        mark.replaceWith(document.createTextNode(mark.textContent));
    }
    if (marks.length > 0) container.normalize();
}

function highlightTextNode(node, regex, cssClass, style, owner) {
    const text = node.nodeValue;
    regex.lastIndex = 0;
    if (!regex.test(text)) return false;
    regex.lastIndex = 0;

    const frag = document.createDocumentFragment();
    let lastIndex = 0;
    let match;
    while ((match = regex.exec(text)) !== null) {
        if (match.index > lastIndex) {
            frag.appendChild(document.createTextNode(text.slice(lastIndex, match.index)));
        }
        const mark = document.createElement("mark");
        mark.className = cssClass;
        mark.dataset.style = style;
        mark.dataset.owner = owner;
        mark.textContent = match[0];
        frag.appendChild(mark);
        lastIndex = match.index + match[0].length;
        if (match[0].length === 0) regex.lastIndex++;
    }
    if (lastIndex < text.length) {
        frag.appendChild(document.createTextNode(text.slice(lastIndex)));
    }

    node.replaceWith(frag);
    return true;
}

function walk(node, regex, cssClass, style, owner) {
    if (node.nodeType === Node.TEXT_NODE) {
        highlightTextNode(node, regex, cssClass, style, owner);
        return;
    }

    if (node.nodeType !== Node.ELEMENT_NODE) return;

    const tag = node.tagName;
    if (tag === "SCRIPT" || tag === "STYLE") return;
    // Any surviving <mark> at this point belongs to a different AtomHighlighter instance
    // (ours were just unwrapped) — leave its content alone.
    if (tag === "MARK") return;

    // Snapshot children first: highlighting a text node replaces it with new sibling nodes,
    // which would otherwise disturb a live NodeList mid-iteration.
    Array.from(node.childNodes).forEach((child) => walk(child, regex, cssClass, style, owner));
}

/**
 * Highlights every match of `keywords` inside `container`'s text content, wrapping each match in
 * a `<mark class="{cssClass}" data-style="{options.style}" data-owner="{options.owner}">`. Never
 * scans outside `container`. Idempotent against the current keyword list: first unwraps this
 * instance's own previous marks (by `options.owner`, a per-instance id — NOT `cssClass`, which is
 * purely presentational and may be shared by unrelated/nested instances), then re-scans, so
 * additions/removals/edits to `keywords` (or `options`) are reflected exactly without disturbing
 * marks any other AtomHighlighter instance created.
 * @param {Element} container
 * @param {string[]} keywords
 * @param {string} cssClass
 * @param {{style?: string, caseSensitive?: boolean, wholeWord?: boolean, owner?: string}} [options]
 */
export function highlightTextInElement(container, keywords, cssClass, options) {
    if (!container) return;
    const opts = options || {};
    unmark(container, opts.owner);

    const regex = buildRegex(keywords, !!opts.caseSensitive, !!opts.wholeWord);
    if (!regex) return;

    const style = opts.style || "mark";
    Array.from(container.childNodes).forEach((child) => walk(child, regex, cssClass, style, opts.owner));
}
