# BlazorAtoms — Task Difficulty Estimates

Implementation-effort notes for libraries where the build cost isn't obvious from the catalog.
Tier in `LIBRARY-CATALOG.md` measures *fit* (JS-free / 0-dep); this file measures *effort to write*.

---

## `BlazorAtoms.Barcodes` — 1D barcodes & QR generators

Two very different beasts under one umbrella library (`AtomBarcode` = 1D, `AtomQrCode` = 2D). Both are generation-only, pure C# → SVG, 0-dep
(no QRCoder / ZXing in the shipped package). Scanning/reading is explicitly out of scope
(camera + JS = a separate Tier C concern).

### 1D barcodes — Easy–Moderate

Each symbology is essentially: lookup tables + a checksum + emit bar widths as SVG rects.
No real math.

| Symbology | Difficulty | Notes |
|---|---|---|
| Code39 | Trivial | fixed pattern per char, checksum optional. ~½ day |
| Code128 | Moderate | code sets A/B/C, mod-103 checksum, start/stop. ~1 day |
| EAN-13 / UPC-A | Moderate | L/G/R digit tables, mod-10 check, guard bars. ~1 day |
| ITF / Codabar | Easy | ~½ day each |

**Budget**: ~2–3 days for a solid Code128 + EAN-13 + Code39 set. Low risk — ship these first.

### QR codes — Moderate–High (the real work)

Intricate but fully documented (ISO/IEC 18004; the thonky.com tutorial is the practical reference).
Deterministic and testable; no external math library required. Pipeline:

1. **Data encode** — mode indicator + char-count + payload bitstream + pad bytes.
   *Simplification*: support **byte mode only** (UTF-8) → covers any input, skips the
   numeric/alphanumeric/kanji encoders.
2. **Reed–Solomon error correction over GF(256)** — the meat. exp/log tables (primitive
   polynomial `0x11D`), generator polynomials per EC codeword count, polynomial division.
   ~150–250 lines. Intimidating name, well-trodden path.
3. **Version + EC block layout** — large capacity / block-structure lookup tables
   (versions 1–40 × EC levels L/M/Q/H).
4. **Module placement** — finder patterns, separators, timing patterns, alignment patterns
   (per-version position table), dark module, then zig-zag data fill skipping function modules.
5. **Masking** — apply all 8 mask patterns, score the 4 penalty rules, keep the lowest; then
   write format-info and version-info bits with their BCH error correction.
6. **Render** — matrix → SVG rects (trivial, on-brand).

**Estimate**: ~600–1000 lines including tables. ~2–4 focused days if familiar with Reed–Solomon;
add 1–2 days if learning it. Bug-prone spots: GF(256) RS arithmetic, mask penalty scoring,
alignment-pattern placement, format/version BCH bits.

### Pragmatics

- **Test strategy**: reference **ZXing.Net in the *test project only*** — decode our generated
  output and assert it round-trips to the input. The shipped package stays 0-dep while tests get
  strong end-to-end verification.
- **MVP path**: byte mode + EC level M + versions 1–10 covers most real payloads; extend to the
  full 1–40 range later (more tables, same shape).
- **Sequence**: barcodes → QR byte-mode MVP → full QR.

**Bottom line**: barcodes are a warm-up; QR is the one genuinely meaty Tier-A library. Very doable,
but budget days plus a real test suite — not hours.
