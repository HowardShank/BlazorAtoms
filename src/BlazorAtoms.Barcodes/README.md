# BlazorAtoms.Barcodes

Barcode and QR code **generation** for Blazor, rendered as pure inline SVG — no JavaScript, no
third-party dependencies. Works in Server or WebAssembly and every render mode. Two components:

- **`AtomBarcode`** — linear (1D) barcodes: **Code 128**, **EAN-13**, **UPC-A**, **Code 39**,
  **Interleaved 2 of 5 (ITF)**, **Codabar**.
- **`AtomQrCode`** — QR codes (2D matrix): byte/8-bit mode, versions 1–40 (auto-selected to fit the
  data), all four error-correction levels (L/M/Q/H).

This library only **generates** codes for display/printing — it does not read, decode, or scan them.

## Install

```xml
<ProjectReference Include="..\BlazorAtoms.Barcodes\BlazorAtoms.Barcodes.csproj" />
```
```razor
@using BlazorAtoms.Barcodes
```

No JS module, no DI registration, no `<script>` tag — the SVG markup is built entirely in C# and
injected via `MarkupString`.

## AtomBarcode

```razor
<AtomBarcode Value="BLAZORATOMS-123" Symbology="BarcodeSymbology.Code128" />

@* EAN-13 — pass 12 digits and the check digit is computed for you *@
<AtomBarcode Value="590123412345" Symbology="BarcodeSymbology.Ean13" />

@* Styled, no human-readable text line *@
<AtomBarcode Value="A1234B" Symbology="BarcodeSymbology.Code39"
             Height="80" ModuleWidth="3" Color="#0f172a" Background="#ffffff"
             ShowText="false" QuietZone="12" />
```

Invalid input for the chosen symbology (wrong character set, wrong digit count, bad check digit)
doesn't throw into the render tree — the component catches the encoding error and renders a small
dashed-border error tile containing the message instead.

### Symbologies (`BarcodeSymbology`)

| Value | Accepts | Check digit |
|---|---|---|
| `Code128` *(default)* | Any ASCII 32–126 (Code Set B) | Mod-103, computed automatically |
| `Ean13` | 12 digits (check computed) or 13 digits (check validated) | Mod-10 |
| `UpcA` | 11 digits (check computed) or 12 digits (check validated) | Mod-10 |
| `Code39` | `0-9 A-Z - . space $ / + %` (letters are upper-cased automatically) | None |
| `Itf` (Interleaved 2 of 5) | Digits only (an odd count gets a leading `0`) | None |
| `Codabar` | `0-9 - $ : / . +`, optionally framed by `A`/`B`/`C`/`D` guards (added automatically if missing) | None |

### `AtomBarcode` parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Value` | `string` (required) | — | Data to encode. Validity rules depend on `Symbology` (see table above). |
| `Symbology` | `BarcodeSymbology` | `Code128` | Which linear symbology to render. |
| `Height` | `double` | `60` | Bar height in px (excludes the human-readable text line). |
| `ModuleWidth` | `double` | `2` | Width of the narrowest bar/space module, in px. |
| `QuietZone` | `int` | `10` | Quiet-zone width on each side, in narrow modules (spec minimum is 10). |
| `Color` | `string` | `#000000` | Bar (and text) color — any CSS color. |
| `Background` | `string?` | `null` | Background fill. `null` leaves the SVG transparent. |
| `ShowText` | `bool` | `true` | Show the human-readable value beneath the bars. |
| `SvgClass` | `string?` | `null` | Extra CSS class(es) on the generated `<svg>` element itself (not the wrapping `<div>` — use `CssClass` for that). |

Plus the shared escape hatch on every Atom component (from `AtomComponentBase`): `CssClass`, `Style`,
and arbitrary splatted attributes (`title`, `data-*`, `id`, ARIA, event handlers, …) on the root `<div>`.

The rendered SVG carries `role="img"` and an `aria-label` of `"Barcode: {Value}"`.

## AtomQrCode

```razor
<AtomQrCode Value="https://example.com" />

@* Higher error correction, custom size and colors *@
<AtomQrCode Value="https://example.com" Size="240" EcLevel="QrErrorCorrection.H"
            Color="#111827" Background="#ffffff" QuietZone="4" />
```

The QR version (1–40) is chosen automatically — the smallest version whose data capacity fits the
UTF-8 byte length of `Value` at the requested `EcLevel`. Longer values, or higher EC levels (which
trade capacity for damage tolerance), push the version up. If the value is too long to fit any
version at that EC level, the component renders an error tile instead of throwing.

### Error correction (`QrErrorCorrection`)

| Value | Approx. recovery |
|---|---|
| `L` | ~7% |
| `M` *(default)* | ~15% |
| `Q` | ~25% |
| `H` | ~30% |

### `AtomQrCode` parameters

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Value` | `string` (required) | — | Text/data to encode (UTF-8, byte mode). |
| `Size` | `int` | `160` | Rendered width/height in px (the SVG is always square). |
| `EcLevel` | `QrErrorCorrection` | `M` | Error-correction level — see table above. |
| `Color` | `string` | `#000000` | Dark-module color — any CSS color. |
| `Background` | `string?` | `null` | Background fill. `null` leaves the SVG transparent. |
| `QuietZone` | `int` | `4` | Quiet-zone border width, in modules (spec minimum is 4). |
| `SvgClass` | `string?` | `null` | Extra CSS class(es) on the generated `<svg>` element itself (not the wrapping `<div>` — use `CssClass` for that). |

Plus the shared escape hatch on every Atom component (from `AtomComponentBase`): `CssClass`, `Style`,
and arbitrary splatted attributes (`title`, `data-*`, `id`, ARIA, event handlers, …) on the root `<div>`.

The rendered SVG carries `role="img"`, an `aria-label` of `"QR code: {Value}"`, and
`shape-rendering="crispEdges"` so modules stay crisp at any zoom level.

## Notes

- **Output.** Both components render inline `<svg>` (via `MarkupString`) inside a wrapping `<div>` —
  no `<img>`, no data URL, no canvas. That means the code is selectable/inspectable in the DOM and
  scales losslessly; it also means the raw `Value` text is HTML-encoded before being placed in
  `aria-label`/`<text>`, so untrusted input is safe to pass through.
- **Cancellation.** Both components honor the shared `CancellationToken` parameter — if cancellation
  is requested mid-build (large QR codes can be expensive to mask/score), the component renders
  nothing rather than a partial SVG.
- **Generation only.** There is no decoder/scanner in this library — it produces codes for display or
  printing, not for reading barcodes from images or a camera.
