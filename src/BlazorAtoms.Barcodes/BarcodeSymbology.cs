namespace BlazorAtoms.Barcodes;

/// <summary>Linear (1D) barcode symbologies supported by <c>AtomBarcode</c>.</summary>
public enum BarcodeSymbology
{
    /// <summary>Code 128 — full ASCII, code sets A/B/C, mod-103 checksum.</summary>
    Code128,

    /// <summary>EAN-13 — 12 data digits + mod-10 check digit.</summary>
    Ean13,

    /// <summary>UPC-A — 11 data digits + mod-10 check digit.</summary>
    UpcA,

    /// <summary>Code 39 — uppercase alphanumeric, no checksum required.</summary>
    Code39,

    /// <summary>Interleaved 2 of 5 — numeric, even digit count.</summary>
    Itf,

    /// <summary>Codabar — numeric plus a few symbols, letter start/stop.</summary>
    Codabar,
}
