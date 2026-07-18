using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Barcodes;

/// <summary>
/// Displays a QR image the developer supplies — from a URL, a data URI, or a raw byte[]. Does
/// NOT generate a QR (see <see cref="AtomQrCode"/> for that). Optionally decodes the image
/// in-process (ZXing.Net + ImageSharp — no browser API, works identically in Server / WASM) and
/// wraps itself in an anchor when the payload parses as an allowed URL scheme.
/// </summary>
public partial class AtomQrCodeImage : AtomComponentBase, IAsyncDisposable
{
    private ElementReference _rootRef;
    private ElementReference _imgRef;
    private IJSObjectReference? _jsModule;
    private readonly CancellationTokenSource _cts = new();
    private string? _resolvedHref;
    private bool _decodeAttempted;
    private string? _lastDecodedFor;
    private string? _lastDecodedPayload;
    private string? _lastDecodeDiagnostic;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ---- inputs (mutually exclusive) -----------------------------------------------------------

    /// <summary>Image URL or data URI. Mutually exclusive with <see cref="Bytes"/>.</summary>
    [Parameter] public string? Src { get; set; }

    /// <summary>Raw image bytes (PNG / JPEG / GIF / BMP / WebP). Mutually exclusive with
    /// <see cref="Src"/>.</summary>
    [Parameter] public byte[]? Bytes { get; set; }

    /// <summary>MIME type used when <see cref="Bytes"/> is set. Default <c>"image/png"</c>.</summary>
    [Parameter] public string MimeType { get; set; } = "image/png";

    /// <summary>Optional width/height in pixels. Null = intrinsic image size.</summary>
    [Parameter] public int? Size { get; set; }

    /// <summary>Alt text. Default <c>"QR code"</c>.</summary>
    [Parameter] public string? Alt { get; set; }

    // ---- link parameters -----------------------------------------------------------------------

    /// <summary>Explicit URL to link to. Wins over <see cref="AutoLink"/>.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>When true and <see cref="Href"/> is null, the image is decoded via
    /// ZXing.Net + ImageSharp, and — when the payload parses as <c>http/https/mailto/tel</c> —
    /// the code is wrapped in an anchor.</summary>
    [Parameter] public bool AutoLink { get; set; }

    /// <summary>Anchor <c>target</c>. Defaults to <c>"_blank"</c>.</summary>
    [Parameter] public string LinkTarget { get; set; } = "_blank";

    /// <summary>Fires once per Src/Bytes value with the decoded payload (null on decode failure).</summary>
    [Parameter] public EventCallback<string?> OnDecoded { get; set; }

    // ---- resolvers -----------------------------------------------------------------------------

    internal string ResolvedSrc
    {
        get
        {
            if (!string.IsNullOrEmpty(Src)) return Src;
            if (Bytes is { Length: > 0 })
                return $"data:{MimeType};base64,{Convert.ToBase64String(Bytes)}";
            return string.Empty;
        }
    }

    internal string? SizeAttr => Size is int px ? px.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;

    /// <summary>Human-readable diagnostic from the most recent decode attempt. Null when decode
    /// succeeded (or never ran). Populated whenever decode returns no payload so consumers can
    /// surface *why*.</summary>
    public string? LastDecodeDiagnostic => _lastDecodeDiagnostic;

    // ---- lifecycle -----------------------------------------------------------------------------

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(Src) && Bytes is { Length: > 0 })
            throw new InvalidOperationException("AtomQrCodeImage: set either Src or Bytes, not both.");

        if (!string.IsNullOrWhiteSpace(Href))
        {
            _resolvedHref = BarcodeLink.TryResolveUrl(Href, null, false, out var u) ? u : null;
            return;
        }

        var currentSrcKey = ResolvedSrc;
        if (!string.Equals(currentSrcKey, _lastDecodedFor, StringComparison.Ordinal))
        {
            _decodeAttempted = false;
            _lastDecodedPayload = null;
            _lastDecodeDiagnostic = null;
            _resolvedHref = null;
        }
        else if (_lastDecodedPayload is not null)
        {
            _resolvedHref = BarcodeLink.TryResolveUrl(null, _lastDecodedPayload, AutoLink, out var u) ? u : null;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_decodeAttempted) return;
        if (!AutoLink && !OnDecoded.HasDelegate) return;
        if (!string.IsNullOrWhiteSpace(Href)) return;
        if (string.IsNullOrEmpty(ResolvedSrc)) return;

        _decodeAttempted = true;
        _lastDecodedFor = ResolvedSrc;

        var (payload, diag) = await DecodeCurrentSourceAsync(_cts.Token);
        _lastDecodedPayload = payload;
        _lastDecodeDiagnostic = diag;

        if (OnDecoded.HasDelegate)
            await OnDecoded.InvokeAsync(payload);

        if (AutoLink && BarcodeLink.TryResolveUrl(null, payload, true, out var url))
            _resolvedHref = url;

        // Always re-render so consumers observing LastDecodeDiagnostic / anchor state see the update.
        StateHasChanged();
    }

    // ---- decode --------------------------------------------------------------------------------

    // Resolves the current image to a byte[] and decodes it via QrImageDecoder. When Bytes is set
    // we already have them; when only Src is set, JS fetchToBase64 grabs the pixels first.
    private async ValueTask<QrImageDecoder.Result> DecodeCurrentSourceAsync(CancellationToken ct)
    {
        byte[]? bytes = Bytes;
        if (bytes is null || bytes.Length == 0)
        {
            if (string.IsNullOrEmpty(Src))
                return new QrImageDecoder.Result(null, "no Src and no Bytes.");
            try
            {
                var module = await LoadModuleAsync(ct);
                var b64 = await module.InvokeAsync<string>("fetchToBase64", ct, Src);
                if (string.IsNullOrEmpty(b64))
                    return new QrImageDecoder.Result(null, "browser fetchToBase64 returned empty (CORS? 404?).");
                bytes = Convert.FromBase64String(b64);
            }
            catch (OperationCanceledException) { throw; }
            catch (JSDisconnectedException ex) { return new QrImageDecoder.Result(null, $"Blazor circuit gone: {ex.Message}"); }
            catch (JSException ex)             { return new QrImageDecoder.Result(null, $"browser fetch failed: {ex.Message}"); }
            catch (InvalidOperationException ex) { return new QrImageDecoder.Result(null, $"JS interop unavailable: {ex.Message}"); }
        }

        var capturedBytes = bytes;
        // ImageSharp + ZXing decode is pure CPU work; run off the render thread.
        return await Task.Run(() => QrImageDecoder.TryDecode(capturedBytes), ct);
    }

    // ---- public API ----------------------------------------------------------------------------

    /// <summary>Returns the resolved image source (URL or data URI). Empty string when neither
    /// <see cref="Src"/> nor <see cref="Bytes"/> is set.</summary>
    public string GetSourceUri() => ResolvedSrc;

    /// <summary>Forces an in-process decode of the current image (or re-decodes if a decode already
    /// ran). Returns the decoded payload or null. Also fires <see cref="OnDecoded"/>, populates
    /// <see cref="LastDecodeDiagnostic"/>, and re-evaluates the AutoLink anchor.</summary>
    public async ValueTask<string?> TryDecodeAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ResolvedSrc)) return null;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
        var token = linked.Token;

        var (payload, diag) = await DecodeCurrentSourceAsync(token);

        _decodeAttempted = true;
        _lastDecodedFor = ResolvedSrc;
        _lastDecodedPayload = payload;
        _lastDecodeDiagnostic = diag;

        if (OnDecoded.HasDelegate)
            await OnDecoded.InvokeAsync(payload);

        _resolvedHref = BarcodeLink.TryResolveUrl(Href, payload, AutoLink, out var url) ? url : null;
        StateHasChanged();
        return payload;
    }

    /// <summary>Copies the image bytes to the clipboard as PNG (default) or the image URL as
    /// text when <paramref name="format"/> is <see cref="BarcodeExportFormat.Svg"/>.</summary>
    public async ValueTask CopyAsync(BarcodeExportFormat format, CancellationToken ct = default)
    {
        var src = ResolvedSrc;
        if (string.IsNullOrEmpty(src))
            throw new InvalidOperationException("AtomQrCodeImage has no image to copy.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
        var token = linked.Token;
        try
        {
            var module = await LoadModuleAsync(token);
            if (format == BarcodeExportFormat.Svg)
            {
                var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"><image href=\"{System.Net.WebUtility.HtmlEncode(src)}\"/></svg>";
                await module.InvokeVoidAsync("copyText", token, svg);
            }
            else
            {
                var b64 = await module.InvokeAsync<string>("fetchToBase64", token, src);
                await module.InvokeVoidAsync("copyPngBase64", token, b64);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (JSDisconnectedException ex)   { throw new BarcodeExportException("Blazor circuit disconnected — copy failed.", ex); }
        catch (JSException ex)               { throw new BarcodeExportException("Browser rejected the copy operation.", ex); }
        catch (InvalidOperationException ex) { throw new BarcodeExportException("JS interop unavailable (server prerender?).", ex); }
    }

    /// <summary>Saves the supplied image in the requested format.</summary>
    public async ValueTask SaveAsync(BarcodeExportFormat format, string fileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("A filename is required.", nameof(fileName));
        var src = ResolvedSrc;
        if (string.IsNullOrEmpty(src))
            throw new InvalidOperationException("AtomQrCodeImage has no image to save.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
        var token = linked.Token;
        try
        {
            var module = await LoadModuleAsync(token);
            if (format == BarcodeExportFormat.Svg)
            {
                var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"><image href=\"{System.Net.WebUtility.HtmlEncode(src)}\"/></svg>";
                await module.InvokeVoidAsync("saveText", token, svg, "image/svg+xml;charset=utf-8", fileName);
            }
            else
            {
                var b64 = await module.InvokeAsync<string>("fetchToBase64", token, src);
                await module.InvokeVoidAsync("savePngBase64", token, b64, fileName);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (JSDisconnectedException ex)   { throw new BarcodeExportException("Blazor circuit disconnected — save failed.", ex); }
        catch (JSException ex)               { throw new BarcodeExportException("Browser rejected the save operation.", ex); }
        catch (InvalidOperationException ex) { throw new BarcodeExportException("JS interop unavailable (server prerender?).", ex); }
    }

    private async ValueTask<IJSObjectReference> LoadModuleAsync(CancellationToken ct)
    {
        return _jsModule ??= await JS.InvokeAsync<IJSObjectReference>(
            "import", ct, "./_content/BlazorAtoms.Barcodes/atom-barcode.js");
    }

    public async ValueTask DisposeAsync()
    {
        if (!_cts.IsCancellationRequested) _cts.Cancel();

        if (_jsModule is not null)
        {
            try { await _jsModule.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            _jsModule = null;
        }

        _cts.Dispose();
    }
}
