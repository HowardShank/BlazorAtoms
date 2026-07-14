const HIGHLIGHT = 'atom-highlight';

// Per-root snapshot of the original, un-highlighted child nodes. Keeping the
// pristine content lets us fully restore the DOM before every re-highlight so
// term changes never depend on the (possibly fragmented) previous state. Using
// a WeakMap means snapshots are released automatically when a root is removed.
const snapshots = new WeakMap();

function ensureSnapshot(root) {
    // (Re)capture the pristine content whenever the root currently holds no marks
    // of ours. If Blazor re-rendered the child content, the old marks are gone and
    // the DOM now reflects the new, authoritative content that we should snapshot.
    if (snapshots.has(root) && root.querySelector(`mark.${HIGHLIGHT}`)) return;
    const fragment = document.createDocumentFragment();
    for (const child of root.childNodes) {
        fragment.appendChild(child.cloneNode(true));
    }
    snapshots.set(root, fragment);
}

function restoreSnapshot(root) {
    const snapshot = snapshots.get(root);
    if (!snapshot) return false;
    root.replaceChildren();
    for (const child of snapshot.childNodes) {
        root.appendChild(child.cloneNode(true));
    }
    return true;
}

function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function buildRegExp(root) {
    const termsJson = root.dataset.highlightTerms;
    const optionsJson = root.dataset.highlightOptions;
    if (!termsJson) return null;

    let terms;
    let options;
    try {
        terms = JSON.parse(termsJson);
        options = optionsJson ? JSON.parse(optionsJson) : {};
    } catch (err) {
        console.error('Failed to parse highlight data attributes for', root, err);
        return null;
    }

    if (!Array.isArray(terms) || terms.length === 0) return null;

    const escaped = terms.map(escapeRegExp).join('|');
    const pattern = options.wholeWord ? `\\b(?:${escaped})\\b` : escaped;
    const flags = options.caseSensitive ? 'g' : 'gi';
    return new RegExp(pattern, flags);
}

function shouldSkip(node) {
    const parent = node.parentElement;
    if (!parent) return false;
    return parent.tagName === 'SCRIPT'
        || parent.tagName === 'STYLE'
        || parent.tagName === 'NOSCRIPT'
        || parent.classList.contains(HIGHLIGHT);
}

function wrapMatches(node, regex, style) {
    const text = node.textContent;
    if (!text) return;

    // Use a fresh regex for both detection and extraction so the global flag's
    // lastIndex cannot leak across text nodes or between test() and matchAll().
    const workingRegex = new RegExp(regex.source, regex.flags);
    if (!workingRegex.test(text)) return;

    const matches = Array.from(text.matchAll(workingRegex));
    if (!matches.length) return;

    const parent = node.parentNode;
    let lastIndex = 0;
    const fragment = document.createDocumentFragment();

    for (const match of matches) {
        const index = match.index;
        if (index > lastIndex) {
            fragment.appendChild(document.createTextNode(text.slice(lastIndex, index)));
        }
        const mark = document.createElement('mark');
        mark.className = HIGHLIGHT;
        mark.setAttribute('data-style', style);
        mark.textContent = match[0];
        fragment.appendChild(mark);
        lastIndex = index + match[0].length;
    }

    if (lastIndex < text.length) {
        fragment.appendChild(document.createTextNode(text.slice(lastIndex)));
    }

    parent.replaceChild(fragment, node);
}

export function highlight(element) {
    if (!element) return;

    // Capture the pristine content the first time we touch this root, then always
    // restore it before walking. This guarantees the tree walker sees the original
    // text nodes (never our own <mark> wrappers or fragmented leftovers), so any
    // change to the search terms re-highlights correctly and idempotently.
    ensureSnapshot(element);
    restoreSnapshot(element);

    const regex = buildRegExp(element);
    if (!regex) return;

    const style = element.dataset.highlightStyle || 'mark';
    const walker = document.createTreeWalker(
        element,
        NodeFilter.SHOW_TEXT,
        {
            acceptNode: (node) => shouldSkip(node)
                ? NodeFilter.FILTER_REJECT
                : NodeFilter.FILTER_ACCEPT
        },
        false);

    const nodes = [];
    let n = walker.nextNode();
    while (n) {
        nodes.push(n);
        n = walker.nextNode();
    }

    for (const node of nodes) {
        wrapMatches(node, regex, style);
    }
}

export function clear(element) {
    if (!element) return;

    // Prefer restoring the pristine snapshot so the DOM returns to exactly its
    // original, un-highlighted form regardless of how it was fragmented.
    if (restoreSnapshot(element)) return;

    // Fallback when no snapshot exists (e.g. clear called before highlight):
    // unwrap any marks and merge adjacent text nodes so terms remain matchable.
    const marks = element.querySelectorAll(`mark.${HIGHLIGHT}`);
    for (const mark of marks) {
        const parent = mark.parentNode;
        if (!parent) continue;

        const replacement = document.createTextNode(mark.textContent ?? '');
        parent.replaceChild(replacement, mark);

        if (replacement.previousSibling && replacement.previousSibling.nodeType === Node.TEXT_NODE) {
            replacement.textContent = replacement.previousSibling.textContent + replacement.textContent;
            parent.removeChild(replacement.previousSibling);
        }
        if (replacement.nextSibling && replacement.nextSibling.nodeType === Node.TEXT_NODE) {
            replacement.textContent = replacement.textContent + replacement.nextSibling.textContent;
            parent.removeChild(replacement.nextSibling);
        }
    }
}
