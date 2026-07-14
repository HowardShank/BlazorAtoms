# BlazorAtoms.Barcodes — internals

Notes for maintainers touching the encoders. None of this is needed to *use* the library — see
`README.md` for that.

## Shared contract

`Encoders/BarcodeEncoder.cs` dispatches `BarcodeSymbology` to one of six internal encoders. Every
linear encoder returns a flat `bool[]` — one entry per **narrow module**, `true` = bar (dark),
`false` = space (light) — with **no quiet zone**; `AtomBarcode.razor` adds the quiet zone and draws
runs of consecutive `true` values as merged `<rect>`s (so a 20-module-wide bar is one `<rect>`, not
20). `QrEncoder.Encode` returns a `bool[,]` module grid instead (function patterns + data + mask
already applied); `AtomQrCode.razor` draws each horizontal run of dark modules the same way.

Both components catch encoding exceptions (`FormatException`/`ArgumentNullException`) at the render
boundary and draw a dashed error tile instead of letting the exception surface into the render tree.

## Linear encoders

- **Code 39** (`Code39Encoder`) and **Codabar** (`CodabarEncoder`) both use a wide/narrow element
  table packed into a single `int` per character: bit `(N-1-i)` set means element `i` is wide. For
  Code 39 there are 9 elements (`N=9`, mask width 8 bits down from bit 8); Codabar has 7. In both,
  **even element index = bar, odd = space** — that convention is what lets `AppendCharacter` (and
  the Codabar loop) build the pattern generically without a separate bar/space table.
- **Code 128** (`Code128Encoder`) only implements Code Set B (ASCII 32–126). Code Set C
  (digit-pair compaction, halves the module count for long numeric strings) is deliberately not
  implemented — Set B alone is a fully valid, standard-conformant encoding, just not the most
  compact one for numeric payloads. The `Patterns` table is indexed by symbol value 0–106 and each
  entry is a run of element widths (units, not modules) alternating bar/space starting with a bar;
  the mod-103 checksum is `(startCode + Σ value·position) % 103`.
- **EAN-13** (`Ean13Encoder`) is the base encoding; **UPC-A** (`UpcAEncoder`) is *not* a separate
  symbology at the module level — a UPC-A value is zero-padded to 13 digits and delegates straight
  to `Ean13Encoder.Encode`, because UPC-A is defined as EAN-13 with an implied leading `0` (and the
  check digit math is identical once you line up the padding). The first digit of an EAN-13 isn't
  encoded as its own digit pattern; it's carried implicitly by which of the L-parity/G-parity table
  (`Parity` lookup) is used for the six left-hand digits — that's the standard EAN-13 mechanism, not
  something specific to this implementation, but it's easy to forget when reading `Build()` and
  wondering where digit 0 went.
- **ITF / Interleaved 2 of 5** (`ItfEncoder`) interleaves digit pairs: digit *n*'s five elements
  become bars, digit *n+1*'s five elements become the spaces between them. Odd-length input gets a
  leading `0` (ITF requires an even digit count so pairing works).

## QR encoder

`QrEncoder` is a from-scratch implementation of the ISO/IEC 18004 pipeline (byte mode only, no
alphanumeric/numeric/kanji mode compaction — byte mode is simplest and universally valid, just not
maximally compact for all-numeric or all-uppercase-alphanumeric payloads):

1. **Version selection** — tries versions 1–40 in order and picks the smallest whose data capacity
   (`NumDataCodewords`) fits the character-count-indicator + payload bits, per `EcLevel`.
2. **Bitstream** — mode indicator (`0100` for byte mode) + character count (8 or 16 bits depending on
   version) + raw UTF-8 bytes, padded with the terminator, byte-aligned, then padded to capacity by
   alternating the two standard pad codewords (`0xEC`/`0x11`).
3. **Reed–Solomon ECC over GF(256)** (`RsComputeDivisor`/`RsComputeRemainder`/`RsMul`) — data is
   split into blocks per `NumBlocks`/`EccPerBlock` (indexed `[ecLevel][version]`), each block gets its
   own RS remainder, and codewords are interleaved column-major across blocks (short blocks first)
   per spec §7.6.
4. **Function patterns** — finder (corners), timing (row/col 6), alignment patterns (position table
   computed, not hard-coded, via `AlignmentPatternPositions`), and reserved format/version-info areas
   are drawn before data placement so `_isFunction` can mask them out.
5. **Data placement** — the standard boustrophedon (zig-zag) column-pair traversal, skipping the
   vertical timing column (`right == 6` shifts to `5`).
6. **Mask selection** — all 8 mask patterns are applied in turn, each scored by the 4-part penalty
   function from the spec (adjacent same-color runs, 2×2 blocks, false-finder patterns, dark/light
   balance), and the lowest-penalty mask wins. This is the most expensive part of the pipeline (8
   full-grid passes) — it's why large, high-EC-level QR codes are the case the shared
   `CancellationToken` is most useful for.

If you need to add alphanumeric/numeric mode compaction or a decoder, note that none of the module
grid, RS tables, or masking logic assume byte mode specifically except step 2 (bitstream assembly) —
the rest of the pipeline is mode-agnostic.

## A stale comment to be aware of

The doc comment at the top of `AtomBarcode.razor` currently reads *"Implemented symbologies: Code39.
Others fall back to an error tile until added."* That's out of date — all six `BarcodeSymbology`
values are implemented and dispatched in `BarcodeEncoder.Encode`; only a truly unrecognized enum
value would hit the `NotSupportedException` fallback. Worth fixing next time that file is touched.
