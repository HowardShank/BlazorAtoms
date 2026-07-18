namespace BlazorAtoms.Barcodes;

/// <summary>
/// Output format selector for <c>CopyAsync</c> / <c>SaveAsync</c> on <see cref="AtomBarcode"/>
/// and <see cref="AtomQrCode"/>.
/// </summary>
public enum BarcodeExportFormat
{
    /// <summary>Inline SVG markup (text).</summary>
    Svg,

    /// <summary>PNG raster, produced client-side by drawing the SVG into an offscreen canvas.</summary>
    Png,
}
