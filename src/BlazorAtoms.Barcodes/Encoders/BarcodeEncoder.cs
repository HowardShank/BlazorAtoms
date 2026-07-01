namespace BlazorAtoms.Barcodes.Encoders;

/// <summary>
/// Dispatches to the per-symbology linear encoders. Each returns a module pattern:
/// one bool per narrow unit, <c>true</c> = bar (dark), <c>false</c> = space (light).
/// No quiet zone is included — the renderer adds it.
/// </summary>
internal static class BarcodeEncoder
{
    public static bool[] Encode(BarcodeSymbology symbology, string value) => symbology switch
    {
        BarcodeSymbology.Code128 => Code128Encoder.Encode(value),
        BarcodeSymbology.Code39 => Code39Encoder.Encode(value),
        BarcodeSymbology.Ean13 => Ean13Encoder.Encode(value),
        BarcodeSymbology.UpcA => UpcAEncoder.Encode(value),
        _ => throw new NotSupportedException($"{symbology} encoding is not implemented yet."),
    };
}
