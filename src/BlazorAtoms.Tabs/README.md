# BlazorAtoms.Tabs

Tab components for Blazor. **`AtomTabs`** owns the selected value; **`AtomTab`** renders a strip button
and **`AtomTabPanel`** a content region, paired by a string `Value`.

Ships the full ARIA tabs pattern — `tablist`/`tab`/`tabpanel` roles, `aria-selected`,
`aria-controls`↔`aria-labelledby` id pairing, roving `tabindex`, and Arrow/Home/End navigation.

Carries one small JS module (`atom-tabs.js`, ~30 lines) whose only job is cancelling the browser's
default scrolling for the navigation keys — see [Keyboard](#keyboard). It is lazy-imported by the
component itself, so there is still **no `<script>` tag, no DI registration and nothing to wire up**, and
if it fails to load the tabs keep working.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Tabs\BlazorAtoms.Tabs.csproj" />
```
```razor
@using BlazorAtoms.Tabs
```

## Quick start

```razor
<AtomTabs @bind-Value="_tab" AriaLabel="Account settings">
    <TabList>
        <AtomTab Value="general" Title="General" />
        <AtomTab Value="billing" Title="Billing" Badge="3" />
        <AtomTab Value="admin" Title="Admin" Disabled="true" />
    </TabList>
    <Panels>
        <AtomTabPanel Value="general"><p>Profile and language.</p></AtomTabPanel>
        <AtomTabPanel Value="billing"><p>Cards and invoices.</p></AtomTabPanel>
        <AtomTabPanel Value="admin"><p>Restricted.</p></AtomTabPanel>
    </Panels>
</AtomTabs>

@code {
    private string _tab = "general";
}
```

### Why two named slots and not one child list

Blazor renders children in source order, so interleaved `<AtomTab>`/`<AtomTabPanel>` could not be laid
out as a strip above a panel region — that needs nodes hoisted across the render tree, which the
framework cannot do. `TabList` and `Panels` keep the DOM correct without asking you to declare things
twice in one specific order.

### An unset `Value` shows the first tab, and stays unset

`Value` is only ever written by a real selection. If it is null, empty, or doesn't match any tab, the
first enabled tab is shown *without* raising `ValueChanged` — the alternative (assigning the first
tab's value during initialization) mutates your bound field before the user has touched anything, which
is a surprising write for `@bind-Value` to perform on its own.

## Keyboard

| Key | Action |
|---|---|
| `→` / `←` | Previous/next tab (horizontal). Wraps at both ends. |
| `↓` / `↑` | Previous/next tab (vertical). |
| `Home` / `End` | First/last enabled tab. |
| `Enter` / `Space` | Select the focused tab. |
| `Tab` | Leave the strip — lands on the active panel, which is focusable. |

Arrow navigation **skips disabled tabs** entirely, and which axis's arrows step follows `Orientation`,
matching what `aria-orientation` advertises. Exactly one tab is in the page's tab order at a time
(roving `tabindex`), so Tab moves past the whole strip rather than through every tab.

`ActivationMode` picks what an arrow does: `Automatic` (default) moves focus *and* selects, so the panel
follows the focused tab; `Manual` moves focus only, and `Enter`/`Space` selects. Use `Manual` when
activating a tab is expensive — a fetch, a heavy panel — since arrowing past a tab then costs nothing.
Both are sanctioned by the ARIA authoring practices.

Modified keypresses are left alone: `Ctrl`+`Home` scrolls the page to the top rather than jumping to the
first tab, and the same goes for any Alt/Shift/Meta combination.

**Keyboard navigation needs an interactive render mode**, like any interactive component — under static
SSR no event handler runs at all, so nothing responds to a keypress. Clicking a tab works in every
render mode, since that is a plain button activation.

### Why there's a JS module

The C# handler implements the navigation, but the browser's *default* action for those keys — scrolling
whatever is scrollable behind the strip — still runs alongside it. Blazor decides
`@onkeydown:preventDefault` at render time rather than per event, so it could only be applied to *every*
keydown on the strip, which would also swallow `Tab` and trap focus inside the tablist.

So `atom-tabs.js` adds one `keydown` listener that calls `preventDefault()` for exactly the keys this
component consumes — and nothing else. It reads the axis from `aria-orientation` at event time, so a
horizontal strip leaves Up/Down alone (and vice versa), cancelling a default only where the component
actually replaces it. It also skips modified keypresses, and skips events originating in a text field, so
custom `ChildContent` containing an `<input>` keeps its caret keys.

The module is a convenience, not a dependency: if the import or the attach call fails, the tabs behave
exactly as they would without it — arrows still navigate, they just also scroll. Every failure path is
swallowed for that reason, and a test asserts navigation survives a failed attach.

## Panel rendering

`PanelRender` decides when a panel's content is in the DOM. It matters as soon as a panel holds state
the browser owns rather than your model — a half-filled form, a scroll position, a playing video.

| Value | Behavior |
|---|---|
| `Active` *(default)* | Only the selected panel is rendered. Lightest; switching away destroys the others' content and any browser-owned state in it. |
| `Always` | Every panel rendered up front, inactive ones carrying the HTML `hidden` attribute. Costs the full first render; scroll, uncommitted input and playback all survive a switch. |
| `Lazy` | A panel renders the first time it is selected and stays afterwards (`hidden` when inactive). Nothing is paid for a panel never opened, nothing lost once it has been. |

## Parameters

### AtomTabs

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Value` | `string?` | `null` | Selected tab's key. Bind with `@bind-Value`. |
| `ValueChanged` | `EventCallback<string>` | — | Backs `@bind-Value`. |
| `TabList` | `RenderFragment?` | `null` | The strip. Put `AtomTab`s here. |
| `Panels` | `RenderFragment?` | `null` | The content region. Put `AtomTabPanel`s here. |
| `Variant` | `TabsVariant` | `Line` | `Line`/`Enclosed`/`Pill`/`Bar` → `data-variant`. |
| `Size` | `TabsSize` | `Medium` | `Small`/`Medium`/`Large` → `data-size`. |
| `Orientation` | `TabsOrientation` | `Horizontal` | Also sets `aria-orientation`, which picks the arrow keys. |
| `Align` | `TabsAlign` | `Start` | `Start`/`Center`/`End`/`Stretch`. `Stretch` grows the tabs themselves. |
| `Effect` | `TabsEffect` | `None` | Opt-in CSS motion → `data-effect` (no attribute when `None`). |
| `ActivationMode` | `TabsActivation` | `Automatic` | See [Keyboard](#keyboard). |
| `PanelRender` | `TabPanelRender` | `Active` | See [Panel rendering](#panel-rendering). |
| `Scrollable` | `bool` | `false` | Strip scrolls along its axis instead of wrapping. |
| `AriaLabel` | `string?` | `null` | Names the tablist — set it when a page has more than one set. |
| `Visible` | `bool` | `true` | `false` hides via `display:none`, staying in the DOM. |
| `AccentColor` | `string?` | `null` | → `--tabs-accent`. Drives the indicator, active tab and focus ring. |
| `TabColor` / `ActiveTabColor` | `string?` | `null` | → `--tabs-tab-color` / `--tabs-active-tab-color`. |
| `IndicatorColor` | `string?` | `null` | → `--tabs-indicator-color`. Defaults to the accent. |
| `IndicatorThickness` | `double?` | `null` | px → `--tabs-indicator-thickness`. |
| `BorderColor` | `string?` | `null` | → `--tabs-border-color`. |
| `PanelBackgroundColor` | `string?` | `null` | → `--tabs-panel-bg`. |
| `Radius` | `double?` | `null` | px → `--tabs-radius`. |
| `PanelPadding` | `double?` | `null` | px → `--tabs-panel-padding`, cascaded to panels. A panel's own `Padding` wins. |
| `Gap` | `double?` | `null` | px between tabs → `--tabs-gap`. |
| `FontSize` | `double?` | `null` | px → `--tabs-font-size`. |
| `Duration` | `double?` | `null` | seconds → `--tabs-duration`. |

### AtomTab

| Parameter | Type | Notes |
|---|---|---|
| `Value` | `string` | **Required.** Must match its panel's `Value`, unique within the strip. |
| `Title` | `string?` | Caption. Ignored when `ChildContent` is set. |
| `ChildContent` | `RenderFragment?` | Custom caption markup; replaces `Title`. |
| `Icon` | `RenderFragment?` | Leading slot, `aria-hidden` (the caption already names the tab). |
| `Badge` | `string?` | Trailing count chip. Rendered as text, so it is announced with the tab — set `AriaLabel` if that reads badly. |
| `Disabled` | `bool` | Native `disabled` + `aria-disabled`; arrow navigation skips it. |
| `AriaLabel` | `string?` | Accessible name when the caption isn't the whole story. |

### AtomTabPanel

| Parameter | Type | Notes |
|---|---|---|
| `Value` | `string` | **Required.** Must match its tab's `Value`. |
| `ChildContent` | `RenderFragment?` | Panel content. |
| `Padding` | `double?` | px → `--tabs-panel-padding`. Null inherits the container's `PanelPadding`. |

All three also take `CssClass`, `Style` and arbitrary splatted attributes on their root element.

## Theming

One `--tabs-*` prefix. The tokens are declared on the `AtomTabs` root and inherit into the tabs and
panels, so setting one on an ancestor moves every tab set below it. Priority, lowest to highest: the CSS
defaults block → `[data-variant]`/`[data-size]`/`[data-orientation]` rules → a consumer stylesheet →
the parameters above (inline) → your `Style`.

Two tokens have no parameter: `--tabs-tab-pad-y` and `--tabs-tab-pad-x` (what `Size` presets). Set them
through `Style` for padding the three sizes don't cover.

`TabsEffect` is pure CSS — no C# state, identical in every render mode, all members
`prefers-reduced-motion` guarded. `FadePanel` and `SlidePanel` animate the panel; `HoverRaise`,
`ActiveGlow` and `GrowIndicator` animate the strip. `GrowIndicator` only does anything on
`TabsVariant.Line`, the one variant that draws an indicator rule.

## Notes

- **Ids are generated per instance.** `aria-controls`/`aria-labelledby` need real ids, so each
  `AtomTabs` mints a Guid-based prefix once in a field — stable across the prerender and interactive
  passes, and two tab sets on one page never collide. Your `Value` is slugged into the id, so keys with
  spaces or punctuation are safe.
- **Tabs are real `<button>`s.** Focus, `Enter`/`Space` activation and the disabled state come from the
  platform, which is why the component handles no activation keys of its own.
- **No scroll buttons on a scrollable strip.** Knowing whether the strip overflows means measuring it —
  more JS than the one listener this package is willing to ship. Native overflow scrolling covers wheel,
  trackpad, touch and keyboard.
- **The one JS module is `wwwroot/atom-tabs.js`.** Self-imported, ~30 lines, no shared JS dependency, and
  purely a `preventDefault` guard — all logic and state stay in C#.
