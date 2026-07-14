# BlazorAtoms.Highlights

Text highlighter components for Blazor.

- **`<AtomHighlight>`** — light, zero-JS highlighter for plain-text child content. Works in every render mode.
- **`<AtomHighlightDeep>`** — zero-JS highlighter for rich HTML content (mixed markup: headings, lists, tables, links). Pass the content as an HTML string.
- **`<AtomHighlighter>`** — highlights keyword matches in the live DOM via a small self-imported JS module, scoped to its own container. Works through arbitrarily nested child components — a Grandparent hosting a Parent hosting a Child is highlighted correctly with no changes to any of them.

## When to use which

| Component | Use when | JavaScript | Content |
|---|---|---|---|
| `AtomHighlight` | The child content is plain text (or you only care about the text part). | None | `ChildContent` (text) |
| `AtomHighlightDeep` | The content is rich HTML markup you can supply as a trusted string. | None | `Html` (string) |
| `AtomHighlighter` | The content is arbitrary, possibly deeply nested, child **components** you don't control or can't flatten to a string. | Self-imported module, scoped to its container | `ChildContent` (any markup/components) |

## AtomHighlighter example

```razor
@using BlazorAtoms.Highlights

<AtomHighlighter Keywords="@(new[] { "Blazor", "C#" })" HighlightClass="atom-highlighter">
    <h1>Grandparent</h1>
    <Parent />  @* Parent renders Child; neither knows AtomHighlighter exists *@
</AtomHighlighter>
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `ChildContent` | `RenderFragment` | required | Content to highlight. May contain arbitrarily nested child components. |
| `Keywords` | `string[]` | `[]` | Keywords to match. |
| `HighlightClass` | `string` | `"atom-highlighter"` | CSS class applied to each injected `<mark>`. Ship your own CSS for a different class to restyle. |
| `CaseSensitive` | `bool` | `false` | Respect casing. |
| `WholeWord` | `bool` | `false` | Match whole words only. |
| `HighlightStyle` | `HighlightStyle` | `Mark` | `Mark`, `Underline`, or `Outline` — sets `data-style` on each `<mark>`. |
| `Background` | `string?` | null | Highlight color. Sets `--highlighter-bg`. |
| `Color` | `string?` | null | Text color of highlighted matches. Sets `--highlighter-color`. |
| `Radius` | `double?` | null | Corner radius in px. Sets `--highlighter-radius`. |
| `Padding` | `string?` | null | Inline padding. Sets `--highlighter-padding`. |

## Example

```razor
@using BlazorAtoms.Highlights

<AtomHighlight Term="Blazor" HighlightStyle="HighlightStyle.Mark">
    Build user interfaces with Blazor.
</AtomHighlight>

<AtomHighlightDeep Terms="@(new[] { "Blazor", "C#" })" WholeWord="true"
                   Html="@article" />
```

## Shared parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Term` | `string?` | null | Single search term. |
| `Terms` | `IReadOnlyList<string>?` | null | Multiple search terms. |
| `CaseSensitive` | `bool` | false | Respect casing. |
| `WholeWord` | `bool` | false | Match whole words only. |
| `HighlightStyle` | `HighlightStyle` | `Mark` | `Mark`, `Underline`, or `Outline`. |
| `Background` | `string?` | null | Highlight color CSS value. |
| `Color` | `string?` | null | Text color CSS value. |
| `Radius` | `double?` | null | Corner radius in px. |
| `Padding` | `string?` | null | Inline padding. |
| `AriaLabel` | `string` | "Highlighted content" | Accessible label. |
