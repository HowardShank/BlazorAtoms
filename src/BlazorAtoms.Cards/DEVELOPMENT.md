# BlazorAtoms.Cards — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage.

## Two families in one package, sharing no base

- **`AtomCardBase`** → `AtomCardReveal`, `AtomCardFlip`, `AtomCardExpand`, `AtomCardCurl`,
  `AtomCardSplit`. A fixed two-panel structure (themed face + body panel) with a mount-time entrance
  and a hover reveal. `Title`/`Subtitle`/`BackgroundImageUrl`/`AccentColor`/`DotCount` all exist to
  theme that face.
- **`AtomComponentBase`** → `AtomCard`; **`AtomCardSectionBase`** → `AtomCardHeader`/`Body`/`Footer`.
  A generic surface with arbitrary content in named sections.

They are not unified because neither base's parameters mean anything on the other family. A shared
"card base" would have to hold either a background image and dot count that `AtomCard` can't use, or a
section cascade that the reveal cards can't honor — the same "don't put a parameter on a type where it
is silently invalid" rule that keeps the five reveal cards separate components instead of one
`CardEffect` enum.

The names are close enough to be worth stating plainly: `CardEffect` (this family) is hover/press
treatment of the *frame*; the reveal family's per-component behavior is not an enum at all.

## Nullable section params instead of "was it set?" detection

Every parameter a section can inherit from its card (`Padding`, `Divider`) is nullable on the section.
Null means "not set", so `AtomCardSectionBase` consults the cascaded `CardContext`; a non-null value
wins. Precedence is therefore a plain `??` chain.

Compare `ButtonFamilyBase`, which has the same problem with **non-nullable enum axes** — there, "was
it set?" can't be read off the value (`Size="Medium"` inside a `Large` group must stay Medium), so it
overrides `SetParametersAsync` and records which parameter names were supplied. Nullable params make
that machinery unnecessary. Prefer nullable where the type allows it.

`CardContext` carries only what a section needs. Variant/elevation/effect describe the card's own
frame, and a section has no frame to treat.

## `CardContext` is rebuilt every render, not cached

`Context => new() { Padding = Padding, Divider = Divider }` is a computed property. Caching the
instance in a field would hand sections stale values after a parameter change, since
`<CascadingValue>` compares by reference for non-fixed values. `IsFixed="false"` is explicit for the
same reason. A test re-renders with a changed `Padding` and asserts it reaches the section — that is
the regression this guards.

## Root element by semantics: `<div>` / `<a>` / `<button>`

Same call as `AtomButton`: an `<a>` when `Href` is set (it navigates, so no `role="button"`, and
keyboard activation plus the context menu come from the platform), a `<button type="button">` when only
`OnClick` is set, a `<div>` otherwise. `Href` wins if both are given.

The slot markup is one `RenderFragment body` local rendered into whichever branch applies, rather than
duplicated three ways — declared at the top of the `.razor` because Razor resolves locals in source
order. The `<a>`/`<button>` roots then need a CSS reset (`font: inherit`, `text-align: inherit`,
`appearance: none`, no `text-decoration`) so all three roots look identical.

## Media placement needs no `order`

`Media` renders *before* the sections for `Top`/`Start` and *after* for `Bottom`/`End`, so DOM order
already matches visual order and the CSS only flips `flex-direction` to `row` for the inline cases. A
test asserts the source order for all four values — that is what keeps the CSS free of `order`, which
would otherwise desync focus order from visual order.

`data-media` is emitted **only when `Media` is non-null**, so the layout rules key off presence rather
than just the enum. Setting `MediaPosition="Start"` with no media must not turn the card into a row.

## Section CSS reads the card's tokens without `::deep`

The `--card-*` custom properties are declared on `.atom-card` and inherit down the DOM, so
`AtomCardHeader.razor.css` can write `var(--card-divider-color, …)` directly even though it is a
different scoped stylesheet. `::deep` is only needed for rules that *select* across a scope boundary,
and this family has none — the card styles its own `.atom-card-sections`/`.atom-card-media` wrappers,
which are in its own scope.

Each section's `var()` carries a fallback so it also works standalone, outside any card.

## `min-height: 0` twice, for one reason

`.atom-card-sections` and `.atom-card-body` both set it. A flex item won't shrink below its content
height by default, so without it a `Scrollable` body inside a fixed-`Height` card overflows the card
instead of scrolling inside itself.

## Heading level is built in code

`AtomCardHeader.HeadingElement` opens `h{level}` via `RenderTreeBuilder` because the element *name*
varies. A `@switch` over six near-identical heading branches in the `.razor` would say the same thing
at six times the length. The level is clamped 1–6, and the CSS resets the UA's per-level font size and
margin so `HeadingLevel` changes semantics only — never the visual weight.
