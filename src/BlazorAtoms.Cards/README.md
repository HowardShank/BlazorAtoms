# BlazorAtoms.Cards

Hover-reveal card components for Blazor. Each shows a themed face (title/subtitle/background
image/dot indicator) with a staggered entrance on mount, then uncovers a body panel on hover — the
difference is *how* it uncovers.

| Component | Hover behavior | Own params |
|---|---|---|
| **`AtomCardReveal`** | Overlay slides away along an axis; a sliver of the image stays | `Direction`, `RevealSize` |
| **`AtomCardFlip`** | Whole card rotates 180°; body is the back face | `FlipAxis`, `Perspective`, `BackColor` |
| **`AtomCardExpand`** | Card grows taller; body slides up from the bottom | `ExpandedHeight`, `BodyHeight`, `BodyColor` |
| **`AtomCardCurl`** | A corner peels back to uncover the body | `Corner`, `CurlSize`, `RestingCurlSize`, `FoldColor`, `BodyColor` |
| **`AtomCardSplit`** | Face splits down the middle; both halves swing open | `SplitAxis`, `Perspective`, `OpenDuration`, `ShowSeamCircle`, `SeamCircleColor`, `SeamCircleSize`, `BodyColor` |

All five inherit the same 13 shared params from **`AtomCardBase`** (see [Shared
parameters](#shared-parameters-atomcardbase)).

**Why separate components and not one `CardEffect` enum.** Each variant needs a parameter the others
cannot use — a reveal size, a flip axis, an expand height, a curl size, a seam axis. A shared enum would put all
of them on one type where most are silently invalid for any given value, with no compile-time guard.
Contrast `AtomTransition`, whose 20 effects *do* share one enum: they all take exactly the same
params (`Show` + duration) and add nothing per-effect. Same rule, opposite outcome.

**Tilt is deliberately not here.** A 3D tilt reveals nothing, so it needs no body panel and no card
structure — it's `HoverEffect.Tilt` on `BlazorAtoms.Transitions`'s `AtomHoverEffect`, which applies
to any content. Compose it around a card:

```razor
<AtomHoverEffect Effect="HoverEffect.Tilt" TiltDegrees="12">
    <AtomCardCurl Title="Beaches" BackgroundImageUrl="beaches.png" />
</AtomHoverEffect>
```

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Cards\BlazorAtoms.Cards.csproj" />
```
```razor
@using BlazorAtoms.Cards
```

## AtomCardReveal

```razor
<AtomCardReveal Title="Trees" BackgroundImageUrl="trees.png" AccentColor="#186218">
    <Subtitle>Kingdom: <em>Plantae</em></Subtitle>
    <BodyContent>
        <p><img class="inset" src="oak.jpg" />Trees are woody perennial plants...</p>
        <p>Apart from providing oxygen...</p>
    </BodyContent>
</AtomCardReveal>
```

Idle: shows the overlay (`Title` + `Subtitle` + background image + dot indicator), with a
staggered pop/slide entrance that plays once on mount. Hover: the overlay slides fully off-panel,
the background image widens, and the scrollable `BodyContent` panel is revealed underneath. Move
the mouse off and it reverts. No click/toggle, no C# state at all — pure `:hover` CSS, the same
"always-on declarative" shape as `AtomHoverGlow`.

### Parameters

Plus [the shared ones](#shared-parameters-atomcardbase).

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Direction` | `CardRevealDirection` | `Left` | Which way the overlay slides: `Left`/`Right`/`Up`/`Down`. Named for the overlay's own travel, since "left>right" is ambiguous about whether it describes the overlay or what's uncovered. The body panel is revealed on the opposite side. |
| `RevealSize` | `string` | `"70%"` | How much of the card the body panel occupies once revealed; the remaining `100% - RevealSize` stays visible as a sliver of the background image. Measured **along whichever axis `Direction` selects** — a width for `Left`/`Right`, a height for `Up`/`Down`, which is why it's "size" and not "width". A percentage resolves against the card, not the viewport. |

## AtomCardFlip

```razor
<AtomCardFlip Title="Beaches" BackgroundImageUrl="beaches.png" FlipAxis="CardFlipAxis.Y">
    <Subtitle>Vacation: <em>Relaxation</em></Subtitle>
    <BodyContent><p>Beaches are sandy shores by the ocean...</p></BodyContent>
</AtomCardFlip>
```

The front face carries the image and chrome; on hover the card rotates 180° and `BodyContent` is the
back face. Nothing is partially uncovered, so there is no `RevealSize` here.

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `FlipAxis` | `CardFlipAxis` | `Y` | `Y` turns left-to-right like a page; `X` turns top-to-bottom like a calendar. |
| `Perspective` | `string` | `"1200px"` | CSS `perspective` — smaller exaggerates the 3D foreshortening. Any CSS length. |
| `BackColor` | `string` | `"#fff"` | Background color of the back face. |

## AtomCardExpand

```razor
<AtomCardExpand Title="Beaches" BackgroundImageUrl="beaches.png" ExpandedHeight="75vmin">
    <Subtitle>Vacation: <em>Relaxation</em></Subtitle>
    <BodyContent><p>Beaches are sandy shores by the ocean...</p></BodyContent>
</AtomCardExpand>
```

Rests at `Height`, grows to `ExpandedHeight` on hover while the body panel slides up from the bottom
edge. It grows via `height`, not `transform: scale()` — scaling would distort the image and text,
whereas a height transition reflows the body into real space. That does mean the card pushes
surrounding content, which is the intended accordion behavior; leave room for it in the layout.

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `ExpandedHeight` | `string` | `"90vmin"` | Height the card grows to on hover. Should exceed `Height` — the difference is the space the body expands into. Any CSS length. |
| `BodyHeight` | `string` | `"75%"` | Height of the body panel once expanded, from the bottom edge. A percentage resolves against the expanded card. |
| `BodyColor` | `string` | `"#fff"` | Background color of the body panel. |

## AtomCardCurl

```razor
<AtomCardCurl Title="Beaches" BackgroundImageUrl="beaches.png" Corner="CardCurlCorner.BottomRight">
    <Subtitle>Vacation: <em>Relaxation</em></Subtitle>
    <BodyContent><p>Beaches are sandy shores by the ocean...</p></BodyContent>
</AtomCardCurl>
```

A corner peels back on hover to uncover the body, with a shaded triangular fold where the sheet
lifts.

> **This is a corner *fold*, not a photorealistic curl.** CSS cannot warp a plane — a true curl needs
> an SVG displacement filter or WebGL. The fold is the honest ceiling for a zero-JS implementation.

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Corner` | `CardCurlCorner` | `BottomRight` | Which corner peels: `BottomRight`/`BottomLeft`/`TopRight`/`TopLeft`. |
| `CurlSize` | `string` | `"60%"` | How far the corner peels on hover, along each edge from the corner. A percentage resolves against the card. |
| `RestingCurlSize` | `string` | `"2.5rem"` | Fold size before hover — a dog-ear hinting the card is peelable. `"0px"` for no hint. |
| `FoldColor` | `string` | `"#e8e8e8"` | Color of the lifted underside of the sheet. |
| `BodyColor` | `string` | `"#fff"` | Background color of the body panel under the sheet. |

## AtomCardSplit

```razor
<AtomCardSplit Title="Beaches" BackgroundImageUrl="beaches.png" ShowSeamCircle="true">
    <Subtitle>Vacation: <em>Relaxation</em></Subtitle>
    <BodyContent><p>Beaches are sandy shores by the ocean that provide relaxation and enjoyment.</p></BodyContent>
</AtomCardSplit>
```

The face is cut in two along `SplitAxis`; on hover both halves swing open, hinged on their outer
edges, exposing the body panel.

Two structural details worth knowing:

- **Each half carries the whole image**, sized to the full card and pinned to its own outer edge
  (left half pins left, right half pins right). Since each half is 50% wide with `overflow: hidden`,
  the pair reads as one unbroken picture while closed. Giving each half only "its" 50% instead would
  need cropping math and would break under any `background-size` other than `cover`.
- **The halves have no back faces** (`backface-visibility: hidden`, nothing behind them) — they stop
  being drawn once past 90°. That's what lets `BodyContent` be a *single* element underneath: the text
  is never split mid-glyph at the seam, never duplicated in the DOM (so screen readers read it once),
  and it scrolls normally. Splitting content across two back faces, as the source design did with an
  image, is fine for pictures but wrong for prose.

`Title` renders on the first half, `Subtitle` on the second, so neither straddles the seam.

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `SplitAxis` | `CardSplitAxis` | `Vertical` | `Vertical` seam → left/right halves swing on Y like shutters; `Horizontal` seam → top/bottom halves swing on X like a hatch. |
| `Perspective` | `string` | `"1000px"` | CSS `perspective`, set on the root so both halves share one viewing frustum — set per-half they foreshorten independently and the seam visibly disagrees mid-swing. Any CSS length. |
| `OpenDuration` | `string` | `".6s"` | How long a half takes to swing fully open. |
| `ShowSeamCircle` | `bool` | `false` | Renders a circle straddling the seam — whole while closed, halved as the shutters part. |
| `SeamCircleColor` | `string` | `"#fff"` | Seam circle color. Only used when `ShowSeamCircle` is set. |
| `SeamCircleSize` | `string` | `"100px"` | Seam circle diameter. Only used when `ShowSeamCircle` is set. Any CSS length. |
| `BodyColor` | `string` | `"#fff"` | Background color of the body panel under the halves. |

## Shared parameters (`AtomCardBase`)

Every card in the family exposes these, and emits them as `--atom-card-*` custom properties — a
family-wide prefix rather than a per-component one, since they mean the same thing everywhere.
Effect-specific properties keep their own prefix (e.g. `--atom-card-reveal-body-size`).

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Title` | `string` | `""` | Card heading. |
| `Subtitle` | `RenderFragment?` | `null` | Subheading under `Title`. Supports markup (e.g. `Kingdom: <em>Plantae</em>`). Omitted entirely when unset. |
| `BackgroundImageUrl` | `string` | `""` | Background image URL. |
| `AccentColor` | `string` | `"green"` | Theme color for the card's face (the overlay/front/sheet behind the image), and the default for `BorderColor` and `DotBorderColor`. Any CSS color. |
| `BorderWidth` | `string` | `"8px"` | Thickness of the frame around the card. **`"0"` removes the frame entirely.** Any CSS length. |
| `BorderColor` | `string?` | `null` | Frame color. Any CSS color. `null` follows `AccentColor` — which is exactly how the frame behaved before this param existed. |
| `Width` | `string` | `"85vmin"` | Card width. Any CSS length. |
| `Height` | `string` | `"65vmin"` | Card height (resting height, for `AtomCardExpand`). Any CSS length. |
| `DotCount` | `int` | `3` | Number of dots in the indicator. `0` hides it entirely. |
| `DotColor` | `string` | `"yellow"` | Dot fill color at rest. Any CSS color. |
| `DotBorderColor` | `string?` | `null` | Dot outline color. Any CSS color. `null` follows `AccentColor`. |
| `DotHoverColor` | `string` | `"#fff"` | Dot fill color while the card is hovered. Any CSS color. |
| `BodyContent` | `RenderFragment?` | `null` | Content of the panel revealed on hover. |

Plus the shared escape hatch on every Atom component (`CssClass`, `Style`, arbitrary splatted
attributes) on the root `<div>`.

### Removing the frame

```razor
<AtomCardFlip BorderWidth="0" AccentColor="#186218" />
```

`BorderWidth` exists because the frame used to be a hardcoded `8px` in `AccentColor`, leaving no way
to drop it. Setting `AccentColor="transparent"` didn't work: the frame kept its 8px of *space* (a gap
showing the page through it) **and** the card's face went see-through — on `AtomCardReveal` that
exposed the body panel before hover, breaking the idle state outright. `Style="border:none"` worked on
three of the four but never on `AtomCardFlip`, whose frame lives on `.atom-card-flip-face` rather
than the root (the root is only the perspective container), so an inline root style couldn't reach it.
Both params are inherited, so `BorderWidth="0"` now behaves identically on all four.

## Notes

- **Zero JS.** Purely declarative CSS — no JS module, no `BlazorAtoms.Behaviors` dependency, this
  package has zero BlazorAtoms deps.
- **Fixed layout, not a generic wrapper.** Unlike `AtomTransition`/`AtomHoverGlow` (arbitrary
  `ChildContent`), these components have a specific two-panel structure (face vs. body) matching
  its source design — `Title`/`Subtitle`/`BackgroundImageUrl`/`DotCount` theme the overlay panel,
  `BodyContent` is the only free-form slot.
- **Hover geometry is card-relative, never in viewport units.** The reveal split is driven entirely
  by `RevealSize` (default `70%`), resolved against the card. The original port hardcoded `60vmin`
  for the overlay's hover translate and the image/body-panel widths — inherited from a source
  design whose card was itself a hardcoded `85vmin`. Because `Width` is a parameter, that
  relationship silently broke: confirmed live on a 306px-wide card where `60vmin` resolved to
  432px, so the overlay slid clean past the card's left edge (image fully hidden, body panel
  filling everything). Percentages work here because the overlay is `inset: 0` — exactly
  card-sized — so `translateX(-70%)` on it means 70% *of the card*.
- **Title/subtitle sit outside the sliding overlay.** They must stay parked in the left sliver while
  the overlay slides out from under them. Nesting them inside would need a counter-translate, and a
  percentage `translateX` resolves against the element's *own* width — so it could never be made
  to track a card-relative reveal width. Keeping them as siblings means they simply never move.
- **The component sets its own `box-sizing` and `flex: 0 0 auto`.** The source design relied on a
  global `* { box-sizing: border-box }` reset that scoped CSS doesn't get (padding would otherwise
  inflate the panels past the percentages this layout depends on), and a bare `width` on a flex
  child still shrinks, since `flex-shrink` defaults to `1`.
- **`BackgroundImageUrl` is set inline, not via CSS custom property.** A relative `url()` inside a
  custom property (`--x: url('...')`, read via `var()`) resolves against the *stylesheet consuming
  the `var()`* — this library's own bundled `.razor.css` — not the caller's document. Confirmed
  live: a relative path 404'd with a `/_content/BlazorAtoms.Cards/` prefix. Fixed by writing
  `background-image` directly on the image `<div>`'s own `style=""` attribute instead, where
  `url()` resolves against the document as expected — a relative `BackgroundImageUrl` (e.g. an
  RCL static asset path like `_content/Demos.Shared/foo.jpg`) now works correctly.
- **Entrance choreography is fixed, not configurable.** The mount-time pop/slide stagger (overlay,
  title, subtitle, image, dots) is a deliberate fixed sequence, not a per-element enum — matches
  the design this component is based on.
