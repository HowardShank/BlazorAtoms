# BlazorAtoms.Buttons

Buttons for Blazor. Five components on one base: `AtomButton`, `AtomIconButton`, `AtomToggleButton`,
`AtomButtonGroup`, `AtomSplitButton`. Every one wraps a native element (`<button>`, `<a>`,
`<details>`), so keyboard behavior, form submission, and link affordances come from the platform.
No JavaScript, no dependencies, works in Server or WebAssembly and every render mode.

## Install

```
dotnet add package BlazorAtoms.Buttons
```

No setup, no DI registration, no `<script>` tag.

## Usage

```razor
@using BlazorAtoms.Buttons

@* The workhorse *@
<AtomButton Text="Save changes" Variant="ButtonVariant.Primary" OnClick="SaveAsync" />

@* Icons in either slot, or icon-only *@
<AtomButton Text="Next" Variant="ButtonVariant.Primary">
    <EndIcon><svg viewBox="0 0 24 24"><path d="M9 6l6 6-6 6" /></svg></EndIcon>
</AtomButton>

<AtomIconButton AriaLabel="Settings" Appearance="ButtonAppearance.Ghost" OnClick="OpenSettings">
    <Icon><svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="3.5" /></svg></Icon>
</AtomIconButton>

@* A link that looks like a button — a real anchor, so middle-click and "open in new tab" work *@
<AtomButton Text="Read the docs" Href="/docs" Target="_blank" Appearance="ButtonAppearance.Outline" />

@* Busy state: content keeps its space, click is blocked, aria-busy is reported *@
<AtomButton Text="Uploading…" Loading="@_busy" OnClick="UploadAsync" />

@* Toggle — aria-pressed, with an optional label change *@
<AtomToggleButton @bind-Value="_following" Text="Follow" PressedText="Following" />

@* One seamed control; the group sets the axes once *@
<AtomButtonGroup Variant="ButtonVariant.Primary" Appearance="ButtonAppearance.Outline" AriaLabel="Alignment">
    <AtomButton Text="Left" OnClick="AlignLeft" />
    <AtomButton Text="Center" OnClick="AlignCenter" />
    <AtomButton Text="Right" OnClick="AlignRight" />
</AtomButtonGroup>

@* Primary action plus a menu *@
<AtomSplitButton Text="Save" Variant="ButtonVariant.Primary" OnClick="SaveAsync">
    <MenuContent>
        <button type="button" role="menuitem" @onclick="SaveAsAsync">Save as…</button>
        <button type="button" role="menuitem" @onclick="ExportAsync">Export…</button>
    </MenuContent>
</AtomSplitButton>

@code {
    bool _busy;
    bool _following;
}
```

## Shared parameters

All five components take every parameter below, from `ButtonFamilyBase`.

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Variant` | `ButtonVariant` | `Default` | `Default` / `Primary` / `Info` / `Success` / `Warning` / `Danger` → `data-variant`. |
| `Appearance` | `ButtonAppearance` | `Solid` | `Solid` / `Soft` / `Outline` / `Ghost` / `Link` → `data-appearance`. |
| `Size` | `ButtonSize` | `Medium` | `Small` / `Medium` / `Large` → `data-size`. |
| `Shape` | `ButtonShape` | `Rounded` | `Rounded` / `Square` / `Pill` / `Circle` → `data-shape`. |
| `Effect` | `ButtonEffect` | `None` | Opt-in motion → `data-effect`. See below. |
| `Disabled` | `bool` | `false` | Native `disabled`; on a link, drops `href` and leaves the tab order. |
| `Loading` | `bool` | `false` | Spinner replaces the content (which keeps its space), click blocked, `aria-busy`. |
| `Visible` | `bool` | `true` | `false` hides via `display:none`, keeping the element in the DOM. |
| `FullWidth` | `bool` | `false` | Stretches to the container. |
| `Type` | `ButtonType` | `Button` | Native `type`. Deliberately **not** HTML's `submit` default. |
| `Href` / `Target` | `string?` | — | Renders an `<a>` instead of a `<button>`. |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | Not invoked while `Disabled` or `Loading`. |
| `AriaLabel` | `string?` | — | Required when there's no text content. |
| `Background` | `string?` | — | Accent → `--btn-accent`. |
| `TextColor` | `string?` | — | → `--btn-color`. |
| `BorderColor` | `string?` | — | → `--btn-border-color`. |
| `BorderWidth` | `double?` | — | px → `--btn-border-width`. |
| `Radius` | `double?` | — | px → `--btn-radius`; overrides `Shape`. |
| `Height` | `double?` | — | px → `--btn-height`. |
| `MinWidth` | `double?` | — | px → `--btn-min-width`. Stops a label change from resizing a row. |
| `FontSize`, `FontFamily`, `FontWeight`, `LetterSpacing`, `TextTransform` | | — | → the matching `--btn-*` token. |
| `CssClass` / `Style` | `string?` | — | Appended after the component's own; `Style` wins over the tokens. |

## Per-component parameters

**`AtomButton`** — `Text` (string shorthand), `ChildContent` (wins over `Text`), `StartIcon`,
`EndIcon`, `IconOnly`, `Pressed` (`bool?`; non-null turns it into a toggle — normally set for you by
`AtomToggleButton`).

**`AtomIconButton`** — `Icon`. Defaults `Shape="Circle"`. Set `AriaLabel`: with no text there is
nothing else to name the control.

**`AtomToggleButton`** — `Value` (`@bind-Value`), `ValueChanged`, `Text`, `PressedText` (label while
on), `ChildContent`, `PressedContent`, `Icon`, `IconOnly`. Reports `aria-pressed`. `Href` is inherited
but not honored — a link holding a toggle state is a contradiction.

**`AtomButtonGroup`** — `ChildContent`, `Variant`, `Appearance`, `Size`, `Shape` (all cascaded),
`Orientation` (`Horizontal`/`Vertical`), `Attached` (default `true`), `Gap`, `FullWidth`, `Visible`,
`AriaLabel`. Layout and cascade only: it holds no selected value — use `AtomToggleButton`'s own
`@bind-Value` per button for a segmented control.

**`AtomSplitButton`** — `Text`, `ChildContent`, `Icon`, `MenuContent`, `MenuAlign`
(`Start`/`End`), `MenuWidth`, `ToggleAriaLabel` (default `"More actions"`).

## Group inheritance

A group supplies the four styling axes to any child that didn't set them:

```razor
<AtomButtonGroup Size="ButtonSize.Large" Variant="ButtonVariant.Primary">
    <AtomButton Text="Keep" />                                   @* large, primary *@
    <AtomButton Text="Delete" Variant="ButtonVariant.Danger" />   @* large, danger  *@
    <AtomButton Text="Small" Size="ButtonSize.Small" />           @* small, primary *@
</AtomButtonGroup>
```

A child's explicit value always wins — including one that happens to equal the enum default, so
`Size="Medium"` inside a `Large` group stays medium. `Effect` is **not** cascaded: it's a per-button
decision.

## Effects

`Effect` is opt-in and off by default. Every member except `ClickRipple` is pure CSS driven by
`:hover`/`:active`/`:focus-visible` — no C# state, identical behavior in every render mode — and all
of them are suppressed under `prefers-reduced-motion: reduce`.

| Member | What it does |
|---|---|
| `None` | Just the standard color transition. Default; emits no attribute. |
| `Press3d` | Rests on a colored ledge and travels down into it when pressed. |
| `Bevel` | Chiselled edges, inverting while pressed. |
| `GradientBorder` | Rotating conic gradient in the border only. |
| `Rainbow` | Continuous hue rotation across the whole surface. |
| `Fizzy` | Bubbles rise across the face on hover. |
| `Storm` | Sweeping highlight plus a brightness flicker on hover. |
| `ClickRipple` | Ripple expanding from the pointer. |

`ClickRipple` is the one member with any C#: the origin comes from `MouseEventArgs.OffsetX/Y` (no JS
measurement) and a per-click render key restarts the keyframe.

`GradientBorder` animates a registered custom property (`@property --btn-angle`). Chromium, Safari,
and Firefox 128+ honor it; where they don't, the gradient renders static rather than rotating.

## Theming with `--btn-*`

The whole family shares the `btn` prefix, so one declaration moves every button:

```css
/* App-wide, without touching any component's parameters */
.atom-button {
    --btn-accent: #7c3aed;
    --btn-radius: 10px;
    --btn-press-depth: 6px;
}
```

Priority, ascending: this package's `[data-*]` rules → an enclosing `AtomButtonGroup` (which cascades
the axes, not the properties) → your stylesheet → the component's parameters (inline style, always
wins).

`AtomButton.razor.css` is the family's single stylesheet — `AtomIconButton` and `AtomToggleButton`
render an `AtomButton` and add their own class to its root, so every rule reaches them.

## CSS hooks

```
.atom-button[data-variant][data-appearance][data-size][data-shape][data-effect][data-state][data-pressed][data-icon-only]
  .atom-button-content     label + icon slots (visibility:hidden while loading)
    .atom-button-icon        each icon slot (-start / -end)
    .atom-button-label       the Text shorthand
  .atom-button-spinner     loading indicator
  .atom-button-ripple      one per click, ClickRipple only

.atom-button-group[data-orientation][data-attached][data-full-width]
.atom-split-button[data-variant][data-appearance][data-size][data-shape][data-state][data-menu-align]
  .atom-split-button-action   the primary AtomButton
  .atom-split-button-toggle   the <summary> arrow half
  .atom-split-button-panel    the dropped menu
```

`data-state` is `disabled` → `loading`, absent when normal. `data-effect` is absent for `None`.

## AtomSplitButton and the missing dropdown

The menu is a native `<details>`/`<summary>`, which buys open/close state, keyboard activation, and
the expanded/collapsed announcement with no JS. Two consequences, both deliberate:

- **No collision flipping.** The panel always drops below, aligned per `MenuAlign`. Choosing a side
  automatically requires measuring the viewport, i.e. JS.
- **No click-outside close.** It closes on its own summary and on `Esc` where supported, but a click
  elsewhere leaves it open — detecting that needs a document-level listener.

A full combobox/menu with smart positioning is `BlazorAtoms.Overlays.AtomDropdown` (planned);
`AtomSplitButton` can compose it once it exists.
