# Bundled CRT fonts for AtomCrtInput

`AtomCrtInput`'s `Font` parameter supports two bundled CRT fonts. To activate them, drop the
matching `.woff2` file into **this** folder. Both are under the SIL Open Font License 1.1
(redistributable, free for any use), so the file ships inside the NuGet package once you add it.

## Files expected

| Filename | Font | Source |
|---|---|---|
| `VT323.woff2` | VT323 (Peter Hull) | https://fonts.google.com/specimen/VT323 → "Get font" → grab the `.woff2` |
| `PressStart2P.woff2` | Press Start 2P (Cody "CodeMan38" Boisclair) | https://fonts.google.com/specimen/Press+Start+2P |

The `@font-face` rules in `AtomCrtInput.razor.css` reference these paths via
`_content/BlazorAtoms.Inputs/fonts/…`. If the file isn't present, the browser silently falls back
to the system monospace stack — `AtomCrtInput` still works, it just doesn't look as authentically
CRT.

## License

Both fonts are licensed under the SIL Open Font License 1.1 — copy the `OFL.txt` from each font's
Google Fonts download into this folder as well (rename to `OFL-VT323.txt` / `OFL-PressStart2P.txt`
if you ship both). The license permits redistribution.
