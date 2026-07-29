# BlazorAtoms.Buttons — development notes

Internal implementation notes for maintainers. Not packed into the NuGet package; see `README.md` for
consumer-facing usage docs.

## One stylesheet, five components

`AtomButton.razor.css` is the family's entire look — skeleton, variants, appearances, sizes, shapes,
states, and all seven effects. The other components do **not** re-implement any of it:

- `AtomIconButton` and `AtomToggleButton` *render* an `<AtomButton>` and pass
  `CssClass="atom-icon-button"` / `"atom-toggle-button"`, which `ClassAttr` appends after
  `atom-button`. Because the element is rendered by `AtomButton`, it carries **AtomButton's** scope
  attribute, so that stylesheet applies and the extra class is available for anything specific.
- `AtomSplitButton` hosts an `AtomButton` for its action half and styles only its own `<summary>` and
  panel.

Why this and not per-component CSS (the pattern `BlazorAtoms.Inputs` uses): there are seven effects,
several of them long, and duplicating them five ways would mean five places to edit for every change.
The inputs family had ~1 short effect block per component, where duplication was the cheaper trade.

**Consequence:** the axes are forwarded from the wrappers *already resolved* — `EffectiveVariant`, not
`Variant`. That settles any `AtomButtonGroup` inheritance before the inner button sees the value, so
the inner button's own "was it explicitly set" tracking always says yes and never re-consults the
group.

## Group inheritance without a sentinel

`ButtonFamilyBase.SetParametersAsync` records **which** of the four axis parameters the caller
supplied, in four bools, then `Effective*` falls back to the cascaded `ButtonGroupContext` only for
the ones that weren't.

The obvious alternative — compare against the enum default (`Size == Medium` → use the group's) —
breaks a real case: `Size="Medium"` inside a `Large` group would be indistinguishable from not setting
it, and would get silently overridden. `AtomButtonGroupTests` has a case pinning exactly that.

Four bools rather than a `HashSet<string>` because `SetParametersAsync` runs on every parameter change;
the set would allocate per render for no benefit.

`Effect` is deliberately **not** in the context. A group-wide effect (seven rainbow buttons) is nobody's
intent, and the axes that *are* cascaded are all "how this set looks as a unit".

## Why AtomButtonGroup isn't a ButtonFamilyBase

It has no click, no loading state, no `href`, no `type` — inheriting the base would give it a surface
it can't honor. It takes `AtomComponentBase` and declares only the four axes it cascades plus its own
layout parameters.

Its child rules need `::deep`, since the buttons are rendered by `AtomButton` (a different scope) and
not by the group's own markup.

## The seam

Attached mode flattens the inner corners and pulls each neighbour back by one border width
(`margin-left: calc(-1 * var(--btn-border-width, 1px))`), so two adjacent 1px borders read as one line
rather than a 2px one. The fallback in that `var()` matters: `--btn-border-width` is declared on
`.atom-button`, so it isn't visible from the group's own rules.

`position: relative` plus `z-index: 1` on `:hover`/`:focus-visible` lifts the active button's borders
above its neighbours, or the seam clips its focus ring.

One special case: `Press3d` reserves `margin-bottom` for its ledge, which would break a **vertical**
attached seam. The group zeroes the ledge for all but the last child there.

## Effects

Layer discipline, so two effects never fight over the same pseudo-element:

- `::before` — sweep/overlay layers (`storm`).
- `::after` — texture layers (`fizzy`).
- `.atom-button-ripple` — a real element, one per click.

`overflow: hidden` on the root clips the ripple and the texture layers to the button's shape. It does
**not** clip `Press3d`'s ledge or the focus ring: a `box-shadow` and an `outline` both draw outside the
border box and aren't subject to the element's own overflow.

Hover/active states darken by `color-mix`ing the accent toward black rather than hard-coding a second
color, so a caller-supplied `Background` keeps a correct hover without a second parameter.

`GradientBorder` uses the two-background trick — a flat fill clipped to `padding-box`, a conic gradient
clipped to `border-box` — so only the border is painted. Its rotation animates
`@property --btn-angle`; a plain custom property can't be interpolated, and where `@property` is
unsupported the gradient simply renders static (an acceptable degradation, not a broken state).

### ClickRipple is the only effect with C#

The origin comes from `MouseEventArgs.OffsetX/OffsetY`, which Blazor supplies without any JS
measurement. Restarting the keyframe on a repeat click is the interesting part: re-rendering the same
element does **not** restart a CSS animation, so the span is keyed on an incrementing click counter
(`@key="RippleKey"`), which makes Blazor replace the element and the animation run from zero. The
counter doubles as the "has ever been clicked" flag that keeps the span out of the DOM until needed.

## Kebab treats a digit run as a word

`ButtonFamilyBase.Kebab` hyphenates before an uppercase letter *and* before the first digit of a run:
`Press3d` → `press-3d` (not `press3d`), while letters following the digits stay attached (not
`press-3-d`). Without the digit rule the CSS selector would have to be `[data-effect="press3d"]`, which
reads badly next to `gradient-border`. `BlazorAtoms.Inputs` has its own copy of `Kebab` without this
rule; the two are independent by design (no cross-library dependencies), and its enums have no digits.

## Native elements, and where the platform has no state

| Need | Platform gives us | What we do |
|---|---|---|
| Disabled button | `disabled` | Render it; also guard `OnClick` in C#, since `Loading` isn't a native state. |
| Disabled link | *nothing* | Drop `href`, `tabindex="-1"`, `aria-disabled`, `pointer-events: none`. |
| Toggle | `aria-pressed` | `Pressed` is `bool?` so a plain button never claims toggle semantics. |
| Menu open/close | `<details>`/`<summary>` | Used as-is: no C# state, no JS. |
| Disabled `<details>` | *nothing* | Same treatment as the disabled link. |

`Type` defaults to `Button`, not HTML's `submit`. A button dropped inside an `EditForm` submitting by
accident is a worse default than having to opt into `Submit`.

The `<a>` branch gets **no** `role="button"`: it navigates, and claiming otherwise would misreport it
to assistive tech. That's also why `Href` exists at all rather than telling callers to wrap a link.

## Deliberate omissions

- **Segmented single-select on the group** — would make it `AtomButtonGroup<TValue>` with selection
  semantics overlapping `BlazorAtoms.Inputs.AtomRadioGroup`. `AtomToggleButton`'s own `@bind-Value`
  covers it.
- **Click-outside close / collision flipping on the split menu** — both need JS (a document listener,
  a viewport measurement). See the README section; `BlazorAtoms.Overlays.AtomDropdown` is where that
  lands.
- **Two-way `Open` on `AtomSplitButton`** — Blazor has no built-in `ontoggle` event mapping, so binding
  `<details open>` would need a custom event registration. The element owns its own state instead.
- **A bundled icon set** — `Icon`/`StartIcon`/`EndIcon` take any `RenderFragment`; a curated set is
  `BlazorAtoms.Icons` (planned).

## Testing notes

Two bUnit behaviors to know when adding cases:

- **bUnit dispatches to handlers regardless of `disabled`.** A real browser fires nothing on a disabled
  button, so `Disabled_renders_native_disabled_and_swallows_the_click` is asserting the *C# guard*, not
  the browser's behavior. The guard has to exist anyway for `Loading` and for blocked links.
- **Clicking an element with no handler throws `MissingEventHandlerException`.** For
  `AtomSplitButton`'s `<summary>` that exception *is* the assertion: it proves nothing is wired that
  could reach `OnClick`, which is the invariant worth pinning.
