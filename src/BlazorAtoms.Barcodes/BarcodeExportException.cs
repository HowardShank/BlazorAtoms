namespace BlazorAtoms.Barcodes;

/// <summary>
/// Thrown by <c>CopyAsync</c> / <c>SaveAsync</c> / <c>GetPngBytesAsync</c> when the underlying
/// JS interop call fails — the Blazor circuit disconnected, the browser rejected the clipboard
/// or save operation (permission, secure-context, unsupported API), or JS is unavailable
/// (prerender / SSR). The inner exception carries the original interop failure.
/// </summary>
public sealed class BarcodeExportException : Exception
{
    public BarcodeExportException(string message) : base(message) { }
    public BarcodeExportException(string message, Exception inner) : base(message, inner) { }
}
