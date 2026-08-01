namespace BlazorAtoms.Charts;

/// <summary>Drop-shadow/gloss treatment for a gauge-family component. Emitted as
/// <c>data-elevation</c>; CSS-filter based (drop-shadow + a blurred sheen shape), never a 3D/perspective
/// transform — see the gauge components' stylesheets.</summary>
public enum GaugeElevation
{
    /// <summary>No shadow, no sheen — matches the flat look of the rest of <c>BlazorAtoms.Charts</c>.</summary>
    Flat,

    /// <summary>A single soft drop-shadow.</summary>
    Raised,

    /// <summary>The default: a two-tier drop-shadow plus a blurred highlight sheen, reading as a
    /// glossy object floating above the page.</summary>
    Floating,
}
