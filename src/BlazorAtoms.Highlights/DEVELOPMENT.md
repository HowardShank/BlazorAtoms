# BlazorAtoms.Highlights — Development Notes

Internal architecture notes for maintainers. This is not consumer-facing documentation; see
`README.md` for usage.

## AtomHighlight / AtomHighlightDeep — zero-JS render-pass architecture

Both components produce their highlighted output entirely during Blazor's own render pass —
`AtomHighlight` by emitting `<mark>` elements directly into the `RenderTreeBuilder`
(`OnParametersSet` builds a `RenderFragment` that walks the regex matches and calls
`builder.OpenElement("mark")` around each one), and `AtomHighlightDeep` by producing a
`MarkupString` it fully owns (`HtmlHighlighter.Highlight(Html, regex, StyleValue)` runs over the
supplied HTML string and returns a new string with `<mark>` wrapping applied to text nodes only,
leaving tags/attributes untouched).

Because the highlighted markup is generated fresh on every render from the current
parameters, there is no persistent DOM state to reconcile and no JavaScript involved at all —
which is what makes both components safe across re-renders and usable in every render mode
(Server, WebAssembly, static SSR). This is in contrast to `AtomHighlighter`, which mutates the
live DOM after render and therefore needs the ownership scheme described below.

`AtomHighlightDeep` is a candidate for future removal per the repo owner's plan. No action is
needed here — this note just records that fact for whoever maintains this file next.

## AtomHighlighter — DOM-walking JS module and mark ownership

`AtomHighlighter` (`AtomHighlighter.razor.cs`) takes a different approach from the other two
components: instead of rendering `<mark>` elements itself, it wraps `ChildContent` in a container
and, after every render (`OnAfterRenderAsync`), calls into a self-imported JS module
(`wwwroot/atom-highlighter.js`) that walks the container's real DOM subtree and wraps keyword
matches in `<mark>` elements directly. This is what lets it highlight content rendered by
arbitrarily nested child components without those components needing any awareness of
`AtomHighlighter` — it operates on rendered output, not Blazor's render tree, so nesting depth is
irrelevant.

### Why mark ownership needs a GUID, not `HighlightClass`

Every call to `highlightTextInElement` first unwraps ("unmarks") this instance's own previously
injected `<mark>` elements back into plain text before re-scanning (see `unmark()` in
`atom-highlighter.js`). This re-scan-from-scratch approach is necessary because the component
calls through on every render, including every keystroke while a caller is live-editing
`Keywords` — without unmarking first, marks created under a transient keyword state (e.g. a
single partially-typed letter) would look "already handled" and never reconcile with the current
keyword list.

The unmark step needs to know which `<mark>` elements belong to *this* `AtomHighlighter`
instance. It would be natural to use `HighlightClass` for that, but `HighlightClass` is purely
presentational and may be shared across nested instances (e.g. two nested `AtomHighlighter`s both
left at the default `"atom-highlighter"` class). If unmark matched by class, an outer instance's
`querySelectorAll("mark")` would also reach into an inner instance's already-marked content
(it's inside the outer's DOM subtree), and stripping by class would erase the inner instance's
marks.

To avoid that collision, `AtomHighlighter` generates a stable per-instance id
(`_instanceId = Guid.NewGuid().ToString("N")`, set once for the component's lifetime) and passes
it through as `options.owner` on every JS call. Each injected `<mark>` is tagged with
`dataset.owner = owner`. `unmark()` only unwraps marks whose `dataset.owner` matches the calling
instance's id, leaving marks owned by any other instance (nested or otherwise) untouched,
regardless of what `HighlightClass` either instance uses. A real remount of the component gets a
fresh instance id, which is fine — there's nothing to reconcile across remounts.

### `atom-highlighter.js` internals

- `highlightTextInElement(container, keywords, cssClass, options)` is the JS entry point invoked
  from C#. It always unmarks this instance's previous matches first, then (if there are keywords)
  builds a case-sensitivity/whole-word-aware regex and walks the container.
- `walk()` recurses through `container`'s child nodes, skipping `SCRIPT`, `STYLE`, and `MARK`
  elements (a surviving `MARK` at that point belongs to a different `AtomHighlighter` instance,
  since this instance's own marks were already unwrapped). Children are snapshotted via
  `Array.from(node.childNodes)` before recursing, because highlighting a text node replaces it
  with new sibling nodes, which would otherwise disturb a live `NodeList` mid-iteration.
- `highlightTextNode()` builds a `DocumentFragment` of plain text nodes and `<mark>` elements and
  replaces the original text node with it in one shot.
- All DOM construction uses `createElement` / `createTextNode` / `DocumentFragment` — never
  `innerHTML` — so matched text can never be reinterpreted as markup.
