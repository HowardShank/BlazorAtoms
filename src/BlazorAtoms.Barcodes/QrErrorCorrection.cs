namespace BlazorAtoms.Barcodes;

/// <summary>QR error-correction level — higher levels recover from more damage at the cost of capacity.</summary>
public enum QrErrorCorrection
{
    /// <summary>~7% recovery.</summary>
    L,

    /// <summary>~15% recovery (default).</summary>
    M,

    /// <summary>~25% recovery.</summary>
    Q,

    /// <summary>~30% recovery.</summary>
    H,
}
