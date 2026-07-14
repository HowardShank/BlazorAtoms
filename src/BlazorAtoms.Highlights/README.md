# BlazorAtoms.Highlights

Text highlighter components for Blazor.

- **`<AtomHighlight>`** — light, zero-JS highlighter for plain-text child content. Works in every render mode.
- **`<AtomHighlightDeep>`** — full DOM text-node highlighter, similar to `jquery.highlight`, for arbitrary child content including markup and nested components. Uses a tiny self-loaded JS module.

## When to use which

| Component | Use when | JavaScript |
|---|---|---|
| `AtomHighlight` | You know the child content is plain text (or you only care about the text part). | None |
| `AtomHighlightDeep` | The content comes from other components, contains mixed markup, or is otherwise unknown. | Lazy-loaded on first use |

## Example

```razor
@using BlazorAtoms.Highlights

<AtomHighlight Term="Blazor" HighlightStyle="HighlightStyle.Mark">
    Build user interfaces with Blazor.
</AtomHighlight>

<AtomHighlightDeep Terms="@(new[] { "Blazor", "C#" })" WholeWord="true">
    <MyMarkdownViewer Source="@article" />
</AtomHighlightDeep>
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
