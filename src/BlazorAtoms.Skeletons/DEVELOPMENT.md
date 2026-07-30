# BlazorAtoms.Skeletons — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage.

## One painted primitive, three presets

`AtomSkeletonBlock` is the only component in this library whose stylesheet paints anything. The other
three render `AtomSkeletonBlock` children and contribute layout only:

- `AtomSkeletonText` — a flex column of blocks.
- `AtomSkeletonAvatar` — exactly one block, no wrapper and **no stylesheet at all**.
- `AtomSkeletonCard` — a block for the media band, an avatar, and a text group.

The alternative was four stylesheets each with their own shimmer gradient, pulse keyframe and
`prefers-reduced-motion` block. That is four places for the animation to drift out of sync, for the sake
of avoiding a handful of forwarded attributes — the same trade `BlazorAtoms.Buttons` settled by having
`AtomIconButton` render an `AtomButton` rather than restyle one.

The cost is visible and deliberate: each preset forwards `Animation`, `BaseColor`, `HighlightColor` and
`Duration` by hand. A cascading value would remove the repetition, but nothing here needs to be
*notified* of anything — it is four attributes, evaluated once per render, and a `CascadingValue` would
be a heavier mechanism carrying no extra capability. (Contrast `BlazorAtoms.Tabs`, which cascades the
component itself because children genuinely must register and be focused.)

### The scoped-CSS consequence

Blazor's scope rewriter stamps the scope id on elements written in *that component's* `.razor` file. A
block rendered by `AtomSkeletonText` therefore carries `AtomSkeletonBlock`'s scope, not
`AtomSkeletonText`'s — so a rule like

```css
/* in AtomSkeletonText.razor.css — would never match */
.atom-skeleton-text > .atom-skeleton-block { height: 0.8rem; }
```

is dead on arrival. It compiles, it looks reasonable in review, and it silently does nothing.

Two rules follow, and both are load-bearing:

1. **Presets pass parameters, not classes.** Line height, radius and width reach the blocks as
   `Height=`/`Radius=`/`Width=`, which become inline custom properties.
2. **Anything a preset needs to style, it writes itself.** `AtomSkeletonCard` wraps its text group in
   `.atom-skeleton-card-lines` precisely so there is an element in *its own* markup to give
   `flex: 1 1 auto`. The wrapper is not decorative padding — remove it and the card's text stops
   filling the space beside the avatar.

This is the same rewriter behaviour `BlazorAtoms.Tabs` exploits from the other direction, where an
*ancestor* selector works because only the final selector gets the scope id.

## Why `AtomSkeletonAvatar` has no `Radius`

`Shape` owns the corner radius. A `Radius` parameter would be silently ignored by the default
`Shape="Circle"` — a parameter that is invalid for the default value, which is the repo's standing rule
against exactly this. Callers wanting a free radius want `AtomSkeletonBlock`.

The same reasoning gives it one `Size` rather than `Width`+`Height`: two independent axes would let a
caller build a shape no avatar can occupy (a "circle" with unequal axes renders as an ellipse).

Both absences are pinned by tests — `Has_no_Radius_parameter`, `Has_no_Width_or_Height_parameters_either`
— paired with `The_primitive_is_the_one_with_a_free_Radius` on the block, so the pair fails loudly if
either side drifts.

`Circle`'s radius is `50%` rather than a length, so it stays round at any `Size`. That is the whole
reason the shape resolves the radius in C# instead of the caller passing one.

## Accessibility: hidden by default, live region on request

`aria-hidden="true"` by default; `role="status" aria-live="polite"` when `AriaLabel` is set. The two are
mutually exclusive because a live region that is also `aria-hidden` announces nothing — a bug that would
be invisible to anyone not testing with a screen reader.

`AriaLabel` is deliberately **not** forwarded to composed children. A card renders five blocks; if the
label propagated, one card would be five live regions announcing the same load. A test asserts exactly
one `[role=status]` per skeleton.

The first draft of this library emitted `role`, `aria-live` and `aria-hidden` but forgot
`aria-label` itself, so a "named" skeleton was an anonymous live region. The tests caught it; that is why
there is a test asserting the label's presence and not just the role's.

## Shimmer implementation

A three-stop gradient at `background-size: 200% 100%`, animating `background-position`. The two outer
stops are the base colour, so what appears to travel is the middle highlight over an otherwise flat
shape. `background-position` avoids layout and stays off the main thread; the `Pulse` alternative
animates `opacity` only, which is cheaper still and is the one to reach for on very long lists.

Keyframe names are written literally in the `animation` shorthand. A scoped stylesheet cannot resolve a
`@keyframes` name through a `var()` — the name is not a value the cascade substitutes.

## Test notes

50 tests. The ones worth not deleting:

- **`Line_widths_are_identical_across_two_renders_of_the_same_input`** — determinism. Randomised widths
  would differ between the prerender and interactive passes and visibly jump on hydration, which is the
  bug `AtomTextSparkle` shipped and had to fix by replacing `Random` with a function of the index.
- **`The_inherited_axes_reach_every_composed_shape`** (card) and its text equivalent — the presets paint
  nothing, so if forwarding breaks, a themed skeleton silently renders default grey shimmer while
  claiming to be themed. Nothing else in the suite would notice.
- **`Adds_no_wrapper_element_of_its_own`** (avatar) — asserts a single `div`. The preset being *only* a
  preset is the design; a wrapper creeping in is a regression.
- **`Unset_tokens_emit_nothing_at_all`** — no `style` attribute rather than an empty one. The CSS
  defaults are the contract, and an inline empty declaration list would be noise in every consumer's DOM.

Nothing here needs bUnit's JSInterop, a renderer-info override, or `Loose` mode: the library makes no
interop calls at all.
