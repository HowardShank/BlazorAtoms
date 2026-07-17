# BlazorAtoms.DragDrop

Generic drag-and-drop for Blazor. Standalone, no runtime dependencies beyond
`Microsoft.AspNetCore.Components.Web`, no JavaScript, works in every render mode.

## Components

### `AtomDropzone<TItem>`

A list drop zone that renders `Items` via `ChildContent`, wrapping each row in a native HTML5
draggable element with insert-target spacers above and below. Zones nested inside an
`<AtomDropzoneGroup>` share a drag context and swap items between one another; standalone zones
reorder in place.

Native HTML5 DnD — no JS interop, no polyfill, no setup call. Runs identically in Server,
WebAssembly, and Auto render modes.

### `AtomDropzoneGroup<TItem>`

Optional wrapper that cascades a shared drag context to its descendants. Without a group, each
`AtomDropzone` owns its own context and only supports single-list reorder.

### Minimal usage

```razor
<AtomDropzone TItem="string" Items="items">
    <ChildContent Context="s">@s</ChildContent>
</AtomDropzone>

@code {
    private List<string> items = new() { "Alpha", "Beta", "Gamma" };
}
```

### Full example — kanban with three zones, one shared group

```razor
<AtomDropzoneGroup TItem="Card">
    <AtomDropzone TItem="Card" Items="Backlog" Group="cards"
                  Accepts="(active, target) => active.Category != \"Bug\" || target?.Category != \"Feature\""
                  MaxItems="10" OnItemDrop="OnDropped">
        <ChildContent Context="card">
            <div class="card">@card.Title</div>
        </ChildContent>
    </AtomDropzone>
    <AtomDropzone TItem="Card" Items="InProgress" Group="cards" InstantReplace="true">
        <ChildContent Context="card">
            <div class="card">@card.Title</div>
        </ChildContent>
    </AtomDropzone>
    <AtomDropzone TItem="Card" Items="Done" Group="cards"
                  AllowsDrag="c => c.Category != \"Chore\"">
        <ChildContent Context="card">
            <div class="card">@card.Title</div>
        </ChildContent>
    </AtomDropzone>
</AtomDropzoneGroup>

@code {
    public record Card(string Title, string Category);
    private List<Card> Backlog { get; } = new();
    private List<Card> InProgress { get; } = new();
    private List<Card> Done { get; } = new();
    private Task OnDropped(Card c) => Task.CompletedTask;
}
```

### Parameters — `AtomDropzone<TItem>`

`TItem` is constrained to `class` so equality and lookups are well-defined and duplicate
value-type entries can't collide.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Items` | `IList<TItem>` | *required* | Bound list. Mutated in place on drop. |
| `ChildContent` | `RenderFragment<TItem>` | *required* | Per-item template. |
| `Footer` | `RenderFragment?` | null | Rendered after the last item. |
| `EmptyContent` | `RenderFragment?` | null | Rendered when `Items` is empty (falls back to a "Drop here" label). |
| `Group` | `string?` | null | Only zones sharing this key exchange items. `null` on both sides means unscoped within the same `AtomDropzoneGroup`. |
| `Accepts` | `Func<TItem, TItem?, bool>?` | null | Predicate — false rejects the drop and fires `OnItemDropRejected`. |
| `AllowsDrag` | `Func<TItem, bool>?` | null | Per-item pickup gate. |
| `CopyItem` | `Func<TItem, TItem>?` | null | If set, cross-zone drops clone instead of move. |
| `MaxItems` | `int?` | null | Cross-zone drops beyond this fire `OnItemDropRejectedByMaxItems`. |
| `InstantReplace` | `bool` | false | Swap items on drag-hover instead of on drop. |
| `Orientation` | `DropzoneOrientation` | Vertical | Vertical / Horizontal / Grid. |
| `ItemWrapperClass` | `Func<TItem, string>?` | null | Extra classes for the wrapper on a specific item. |
| `Virtualize` | `bool` | false | Wrap items in `<Virtualize>`. |
| `VirtualizeOptions` | `VirtualizeOptions<TItem>?` | null | Overscan / item size / items provider. |
| `Gap` | `string?` | null | CSS gap between items → `--dropzone-gap`. |
| `HighlightColor` | `string?` | null | Accept-highlight color → `--dropzone-highlight-color`. |
| `DenyColor` | `string?` | null | Reject-highlight color → `--dropzone-deny-color`. |
| `Visible` | `bool` | true | `false` hides via `display:none`. |
| `Disabled` | `bool` | false | Blocks pickup and drop. |
| `OnItemDrop` | `EventCallback<TItem>` | — | Fires after a successful drop. |
| `OnReplacedItemDrop` | `EventCallback<TItem>` | — | Fires with the displaced target when `InstantReplace` swaps. |
| `OnItemDropRejected` | `EventCallback<TItem>` | — | Fires when `Accepts` returns false. |
| `OnItemDropRejectedByMaxItems` | `EventCallback<TItem>` | — | Fires when `MaxItems` would be exceeded. |
| `OnDragEnd` | `EventCallback<TItem>` | — | Fires when a drag operation ends. |

Standard atom parameters `CssClass`, `Style`, and unmatched attributes flow through to the root.

### Direct API — `DropzoneEngine`

The list-mutation rules are exposed as pure static helpers so callers building custom drag
surfaces (touch, keyboard reorder, headless tests) can reuse them.

```csharp
// Same-list move — corrects for the index shift caused by RemoveAt.
DropzoneEngine.InsertAt(list, list, active, targetIndex);

// Cross-list move — removes from source unless copyItem is provided.
DropzoneEngine.InsertAt(source, target, active, targetIndex,
    copyItem: item => item with { Id = Guid.NewGuid() });

// One-for-one swap within a list.
DropzoneEngine.Swap(list, list, list[0], list[3]);

// Capacity + accept predicates.
if (DropzoneEngine.IsAtCapacity(target, active, maxItems: 5)) return;
if (!DropzoneEngine.ShouldAccept(active, hoverTarget, accepts)) return;
```

### Notes

- **`TItem : class`** — reference-only. Deliberate constraint that avoids null-NRE and the
  `IndexOf` collisions the reference `blazor-dragdrop` library hits with value-type or duplicate
  items.
- **Group scoping** — `Group="key"` opts *in* to cross-zone transfer; zones without a matching
  key (or without an `AtomDropzoneGroup` ancestor) only reorder in place. Explicit by design —
  fixes the reference library's implicit-global-scope quirk where every `Dropzone<string>` on
  the page interoperated whether that was wanted or not.
- **No JavaScript** — every DnD event uses Blazor's own `@ondrag*` bindings, so the component
  runs identically in Server, WebAssembly, and Auto modes without a JS module or `AddDragDrop()`
  registration.
- **Mobile touch** — native HTML5 DnD doesn't fire on iOS Safari or older Android browsers.
  Consumers who need mobile support can add the `mobile-drag-drop` polyfill from a `<script>`
  tag in their host `App.razor` / `index.html`; the polyfill translates touch to DnD events and
  the component picks them up unchanged.

### Future Enhancements

- Add locking mechanism to prevent one or more items from being moved.
- Add support for nested drop zones with hierarchical data structures.
- Add support for custom drag previews and ghost elements.
- Add support for keyboard accessibility and ARIA attributes for better accessibility.
- Add drag handles to allow users to drag items by a specific part of the item rather than the entire item.
- Add swap ability.
- Add multi-drag support to allow users to select and drag multiple items at once.
