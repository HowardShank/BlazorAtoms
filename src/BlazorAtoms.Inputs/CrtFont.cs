namespace BlazorAtoms.Inputs;

/// <summary>
/// Font choice for <see cref="AtomCrtInput"/>. <see cref="Vt323"/> / <see cref="PressStart2P"/>
/// require the matching <c>.woff2</c> file to be present in the library's
/// <c>wwwroot/fonts/</c> folder — see the fonts <c>README.md</c> in that folder for the specific
/// files. If a bundled font is selected but the file isn't present, the browser falls back to the
/// system monospace stack automatically (no error, just a less-authentic look).
/// </summary>
public enum CrtFont
{
    /// <summary>System monospace stack — always works, no bundled font needed. Least authentic.</summary>
    System,

    /// <summary>VT323 — thin terminal-style CRT font. Requires
    /// <c>wwwroot/fonts/VT323.woff2</c> to be present.</summary>
    Vt323,

    /// <summary>Press Start 2P — chunky pixel/arcade font. Requires
    /// <c>wwwroot/fonts/PressStart2P.woff2</c> to be present.</summary>
    PressStart2P,
}
