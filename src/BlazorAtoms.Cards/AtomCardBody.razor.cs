using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Cards;

/// <summary>
/// The main content section of an <see cref="AtomCard"/>. Optionally scrolls its own overflow rather
/// than growing the card. Works standalone as well as nested.
/// </summary>
public partial class AtomCardBody : AtomCardSectionBase
{
    /// <summary>When true, overflow scrolls inside the body instead of stretching the card. Pair with
    /// <see cref="MaxHeight"/> (or a fixed card <c>Height</c>) — with neither there is nothing to
    /// overflow.</summary>
    [Parameter] public bool Scrollable { get; set; }

    /// <summary>Ceiling for the body's height. Any CSS length. Independent of
    /// <see cref="Scrollable"/>, though the two are usually set together.</summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary><c>data-scrollable</c>, emitted only when scrolling is on.</summary>
    private string? ScrollableAttr => Scrollable ? "true" : null;

    /// <summary>A scroll container must be keyboard-reachable, or its content is unreachable without a
    /// mouse. <c>0</c> only when scrolling is on; null (no attribute) otherwise, so a plain body adds
    /// nothing to the tab order.</summary>
    private string? ScrollTabIndex => Scrollable ? "0" : null;

    private string? BodyStyle => MaxHeight is null ? null : $"max-height:{MaxHeight};";
}
