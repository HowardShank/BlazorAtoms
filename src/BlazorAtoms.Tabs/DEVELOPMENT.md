# BlazorAtoms.Tabs — Development Notes

Internal architecture notes for maintainers. See `README.md` for consumer-facing usage.

## The cascade carries the component, not a DTO

`ButtonGroupContext` and `CardContext` are plain value carriers, because their children only need to
*read* inherited styling. Tabs needs behavior: a tab must register itself so the strip has an ordered,
focusable list to arrow through, must be told to re-render when the selection moves, and must be
focusable *by* the parent. So `AtomTabs` cascades `this`.

That has one consequence worth spelling out. The cascade is `IsFixed="true"` — honest, since the
reference never changes — and a `CascadingValue` only notifies subscribers when the **value** changes.
A non-fixed cascade of an unchanging reference would therefore never notify anyone either. Selection
changes are propagated explicitly instead, by `NotifyChildren()` calling `StateHasChanged()` on each
registered tab plus this component. Panels need no notification: they are rendered inside `AtomTabs`'
own tree, so its re-render re-invokes the `Panels` fragment and they pick up the new `ActiveValue`.

Registration order == DOM order, because Blazor initializes children in the order their parent renders
them. That is the whole basis for "next tab" meaning what it looks like. `AtomTab` unregisters in
`Dispose`, so a tab removed by an `@if` stops being a navigation target.

`AtomTabPanel` deliberately does **not** register: it needs nothing but the active value and the shared
id prefix, both read at render time. Only the strip needs an ordered list.

## `ActiveValue` is derived, never assigned

```csharp
internal string? ActiveValue =>
    !string.IsNullOrEmpty(Value) && _tabs.Any(t => t.Value == Value)
        ? Value
        : _tabs.FirstOrDefault(t => !t.Disabled)?.Value;
```

The obvious alternative — writing the first tab's value into `Value` and raising `ValueChanged` during
registration — mutates the caller's bound field from inside a child's initialization, before any
interaction. Deriving it means an unset or stale `Value` still shows a sensible panel, `@bind-Value`
performs no surprise write on first render, and the caller's field only ever changes from a real
selection. A test asserts `ValueChanged` is not raised on initial render with no `Value`.

Note the fallback skips disabled tabs, so a set whose first tab is disabled still shows something
selectable.

## Ids: generated, and slugged

`aria-controls`/`aria-labelledby` need real ids — there is no implicit-association escape hatch here
the way there is for a `<label>` wrapping its input (which is why `BlazorAtoms.Inputs` mints no ids). A
Guid-based prefix is generated **once in a field**, not per render, so it is identical across the
prerender and interactive passes; seven other components in the repo already do this.

The tab's `Value` is slugged into the id (`Slug`), because values are author-supplied keys and may
contain spaces or slashes — and an id containing a space silently breaks the `aria-controls` reference.

## Keyboard

One `@onkeydown` on the tablist rather than one per tab: keydown bubbles from the buttons, and the strip
is the element that owns "which tab is next". The handler works off `_focusedValue ?? ActiveValue`, which
is what lets Manual mode move the roving `tabindex` without moving the selection.

No activation keys are handled. The tabs are real `<button>`s, so the browser already turns
`Enter`/`Space` into a click — handling them again would double-fire.

### `FocusAsync` is interop, and is guarded

`ElementReference.FocusAsync()` is framework interop, not a module this package ships, so the
0-dependency and no-`<script>` promises hold. This is the repo's first use of it.

It is **not** a static-SSR caveat in any interesting sense: under static SSR the `@onkeydown` handler
does not run either, so keyboard navigation is absent for the ordinary reason that nothing is
interactive. An earlier draft of the README singled out `FocusAsync` here, which wrongly implied the
rest of the handler worked.

What did need fixing: the call is wrapped in the same three-way guard every other interop call site in
the repo uses (`JSDisconnectedException`, `OperationCanceledException`, `JSException`). Pressing an
arrow key while a Blazor Server circuit is tearing down, or just after an `@if` removed the target tab,
otherwise throws out of an event handler. The user was moving focus, not performing an operation whose
failure is worth surfacing.

bUnit's default strict JSInterop mode throws on the invocation, so the keyboard tests set
`JSInterop.Mode = Loose`; the assertions are on selection and the roving tabindex, which are the
observable results either way.

### `atom-tabs.js` — the one module, and why it exists

Blazor evaluates `:preventDefault` at render time, not per event, so it can only be all-or-nothing for
keydown on the strip — and applying it to everything would swallow `Tab` and trap focus inside the
tablist. That leaves the browser's default action running alongside the C# handler: an arrow key both
moves the selection and scrolls whatever is behind the strip. Most visible with
`Orientation="Vertical"`, whose Up/Down keys usually have somewhere to go; horizontal tabs use
Left/Right, which often don't, and the focus-scroll-into-view masks it further.

So the package ships one module, self-imported on first use per the repo's JavaScript policy — the same
pattern as `AtomTooltip`, `AtomHighlighter` and `AtomScrollProgressBar`. It is ~30 lines and does exactly
one thing.

**Three details that keep it honest:**

- **The key list mirrors the C# switch, per axis.** Cancelling the default for a key the component does
  *not* handle would silently break legitimate page scrolling, so `KEYS.horizontal` omits Up/Down and
  `KEYS.vertical` omits Left/Right. The axis is read off `aria-orientation` at event time rather than
  captured at attach, so changing `Orientation` at runtime needs no re-attach — and a test asserts
  `attach` is invoked exactly once across a re-render that flips it.
- **Modified keypresses are skipped in both places.** `Ctrl`+`Home` is "scroll to top of page", not "go
  to first tab". The C# handler bails on any modifier and so does the module; they must agree, or the
  guard would suppress a scroll the handler declined to replace. This required adding the modifier check
  to C# — it wasn't there, so the first draft of the module's comment was describing behavior that
  didn't exist.
- **Text-entry targets are skipped.** `ChildContent` can put an `<input>` inside a tab; cancelling
  arrows there would break the caret.

**It is a convenience, not a dependency.** Navigation is the C# handler and works with or without the
module, so a failed import or attach degrades to precisely the pre-module behavior. Every path is
wrapped in the standard three-way guard, and `A_failing_key_guard_leaves_navigation_working` plans the
module with `attach` throwing a real `JSException` and asserts the arrows still move the selection.

### Verifying the key guard by hand

No test renderer can cover this. bUnit dispatches synthetic events and has no layout, so "the browser's
default scroll did not happen" is not a statement it can make. The assertion has to be made in a real
browser, which makes this a manual procedure rather than a gap in the suite.

#### Procedure

**Setup**

1. Run any demo host and navigate to `/playground/tabs`.
2. Set **Orientation → Vertical**. Leave every other control at its default.
3. Shrink the window height until the page clearly scrolls. The demo shell scrolls `article.content`, not
   the document, so `window.scrollY` stays `0` throughout — watch the page, or read
   `document.querySelector('article.content').scrollTop`.
4. Scroll so the tab strip sits mid-screen with visible room **below** it. Both matter: all tabs on screen
   means focus-scroll-into-view can't be mistaken for the default scroll, and room below means the default
   scroll has somewhere to go if it fires.
5. Click the first tab. Confirm it has a focus ring — every step below needs focus inside the strip, and a
   stray click on the panel (which is focusable) silently invalidates the run.

Vertical is the case worth testing because Up/Down almost always have somewhere to scroll to; horizontal
tabs use Left/Right, which frequently don't, and focus-scroll-into-view masks what's left.

**Keys — one at a time, checking both columns before moving on**

| # | press | selection / focus | page |
| --- | --- | --- | --- |
| 6 | `ArrowDown` | advances one tab | **does not move** |
| 7 | `ArrowDown` again | advances one more | does not move |
| 8 | `Home` | jumps to first tab | does not move |
| 9 | `Ctrl+Home` | **unchanged** | **jumps to top** — modifiers are left to the browser |
| 10 | scroll back, then `PageDown` | unchanged | **scrolls** — a key the component does not consume |
| 11 | `ArrowRight` | unchanged | does not move — wrong axis for a vertical strip |
| 12 | `Tab` | focus **leaves** the strip, lands on the panel | — |

Steps 6–8 are the fix. Steps 9–12 are the controls: each is a key the guard must *not* touch, and together
they're the evidence it isn't over-reaching. Step 12 is the specific regression `:preventDefault` would
have caused, which is why it's on the list. A run where step 6 passes but 9–12 also show "does not move"
is a **failure**, not a pass — it means the guard is cancelling everything.

**Horizontal pass (optional)**

13. Set **Orientation → Horizontal** without reloading, then repeat steps 5–6 with `ArrowRight`.

It should behave the same, and `ArrowDown` should now scroll the page instead of moving the selection —
the mirror image of step 11. This is also the regression test for the axis being read at event time rather
than captured at attach: it passes without a reload only because `attach` re-reads `aria-orientation` on
every keypress.

For a direct before/after, strip the listener off the live element in the DevTools console:

```js
const tl = document.querySelector('.atom-tabs [role="tablist"]');
const s = Object.getOwnPropertySymbols(tl).find(x => x.description === 'atomTabsKeyGuard');
tl.removeEventListener('keydown', tl[s]);
```

`ArrowDown` then moves the selection *and* scrolls the page — the original bug, reproducible on demand.
Reload to restore. This works only because `attach` parks the handler on the element under a `Symbol`
rather than in a closure; keep it that way, since it is also how `detach` finds the listener to remove.
The same call confirms the guard is attached at all:

```js
Object.getOwnPropertySymbols(
    document.querySelector('.atom-tabs [role="tablist"]')).map(s => s.description);
// includes "atomTabsKeyGuard"
```

**Caveat if automating this.** A CDP-driven browser pane was tried first and could verify the logic but
not the outcome: its injected keys arrive with `isTrusted: true` and reach DOM listeners, yet produce no
default scroll — a focused scroll container did not move on `PageDown` either. So it can prove
`preventDefault()` ran (read `event.defaultPrevented` from a **bubble**-phase listener on `document`,
which runs after the tablist's guard; a capture-phase listener runs before it and always reports `false`)
but it cannot prove the scroll it suppresses. Confirmed that way: `ArrowDown` and `Home` prevented,
`ArrowRight` and `Ctrl+ArrowDown` not, selection moving only for the first two. The scroll itself still
needs human eyes.

### Testing note: the `FocusAsync` guard is not covered

bUnit does **not** route `ElementReference.FocusAsync()` through its JSInterop mock — verified
empirically: a strict-mode render with `atom-tabs.js` planned but focus unplanned passes cleanly. So
there is no way from a test to make that call fail, and its guard is asserted only by inspection.

Worth recording because an earlier test claimed to cover it by rendering under strict mode and passed —
for the wrong reason. It was proving nothing. The `atom-tabs.js` calls *do* go through the mock, which is
why those failure modes are genuinely tested. Note also that bUnit's own
`JSRuntimeUnhandledInvocationException` is not one of the three production exception types, so an
unplanned invocation is not a valid stand-in for a real interop failure either.

## CSS: ancestor selectors instead of `::deep`

The three components are three scopes, but a tab's whole appearance depends on axes that are parameters
on the *container*. Rather than `::deep` from `AtomTabs.razor.css`, every tab and panel rule lives in
that component's own stylesheet and keys off an ancestor `.atom-tabs[data-*]`:

```css
.atom-tabs[data-variant="pill"] .atom-tab[data-active] { ... }
```

Blazor's scope rewriter appends the scope id to the **final** selector only, so the ancestor part
matches by class across scopes and the rule works. Net effect: `AtomTabs.razor.css` owns only the
wrappers it renders itself, each component's look is in one file, and the family uses no `::deep` at
all. The `--tabs-*` tokens arrive by ordinary inheritance for the same reason.

`TabsAlign.Stretch` is the one axis that is not a `justify-content` value — it grows the tabs, so it
lives on `.atom-tab` as `flex: 1 1 0` rather than on the strip.

## Panel animation keys off `[data-active]`, not element creation

Under `PanelRender.Active` a switch creates a new element, so a keyframe would replay naturally. Under
`Always`/`Lazy` the element persists and only `hidden` toggles — nothing new to trigger an animation. So
the panel effects are written against `.atom-tab-panel[data-active]`: adding the attribute starts the
animation, which makes `FadePanel`/`SlidePanel` behave identically in all three modes.

`.atom-tab-panel[hidden] { display: none }` is explicit because the UA's `[hidden]` loses to any
`display` a consumer sets on the panel, and the two persistent strategies depend on `hidden` actually
hiding.

## AtomTabPanel latches Lazy in `OnParametersSet`

`_hasBeenActive` is set in `OnParametersSet`, not in the render path, so the component never mutates
state while rendering. It works because the parent's re-render re-invokes the `Panels` fragment, which
calls `SetParametersAsync` on each panel — that is also why panels need no explicit notification.
