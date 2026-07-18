using System.Globalization;
using System.Net;
using System.Text;
using BlazorAtoms.Barcodes.Encoders;
using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Barcodes;

/// <summary>
/// Linear (1D) barcode component. Renders as inline SVG via pure C#. Optional Copy / Save /
/// GetPngBytes methods lazily import a small JS module (invisible to consumers) that reaches
/// the browser clipboard and canvas — no NuGet dep and no setup required.
/// </summary>
public partial class AtomBarcode : AtomComponentBase, IAsyncDisposable
{
    private ElementReference _rootRef;
    private IJSObjectReference? _jsModule;
    private readonly CancellationTokenSource _cts = new();

    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ---- rendering parameters ------------------------------------------------------------------

    /// <summary>Data to encode.</summary>
    [Parameter, EditorRequired] public string Value { get; set; } = "";

    /// <summary>Which linear symbology to render.</summary>
    [Parameter] public BarcodeSymbology Symbology { get; set; } = BarcodeSymbology.Code128;

    /// <summary>Bar height in pixels (excludes the human-readable text line).</summary>
    [Parameter] public double Height { get; set; } = 60;

    /// <summary>Width of the narrowest bar/module, in pixels.</summary>
    [Parameter] public double ModuleWidth { get; set; } = 2;

    /// <summary>Quiet-zone width on each side, in narrow modules (spec minimum is 10).</summary>
    [Parameter] public int QuietZone { get; set; } = 10;

    /// <summary>Bar color (any CSS color).</summary>
    [Parameter] public string Color { get; set; } = "#000000";

    /// <summary>Background fill. Null leaves it transparent (raster PNG will also be transparent —
    /// set an opaque color for print pipelines that need a white background).</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Show the human-readable value beneath the bars.</summary>
    [Parameter] public bool ShowText { get; set; } = true;

    /// <summary>Extra CSS class(es) on the generated <c>&lt;svg&gt;</c> element (not the root; use the
    /// inherited <c>Class</c> for that).</summary>
    [Parameter] public string? SvgClass { get; set; }

    /// <summary>Target pixel width used when rasterizing to PNG via <see cref="GetPngBytesAsync"/>,
    /// <see cref="CopyAsync"/>, or <see cref="SaveAsync"/>. Null uses the SVG's intrinsic width.</summary>
    [Parameter] public int? PngPixelWidth { get; set; }

    // ---- link parameters -----------------------------------------------------------------------

    /// <summary>When set, the barcode is wrapped in an anchor pointing at this URL. Wins over
    /// <see cref="AutoLink"/>.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>When true and <see cref="Href"/> is not set, the component inspects
    /// <see cref="Value"/> and wraps in an anchor when it parses as an <c>http/https/mailto/tel</c>
    /// URL. Rejects <c>javascript:</c>, <c>data:</c>, <c>file:</c>, etc.</summary>
    [Parameter] public bool AutoLink { get; set; }

    /// <summary>Anchor <c>target</c>. Defaults to <c>"_blank"</c>.</summary>
    [Parameter] public string LinkTarget { get; set; } = "_blank";

    internal string? ResolvedHref => BarcodeLink.TryResolveUrl(Href, Value, AutoLink, out var u) ? u : null;

    // ---- public API ----------------------------------------------------------------------------

    /// <summary>Returns the currently rendered SVG markup as a string.</summary>
    public string GetSvg() => BuildSvg();

    /// <summary>Rasterizes the current SVG to a PNG byte array via the browser canvas.</summary>
    /// <param name="pixelWidth">Overrides <see cref="PngPixelWidth"/> when supplied.</param>
    public async ValueTask<byte[]> GetPngBytesAsync(int? pixelWidth = null, CancellationToken ct = default)
    {
        var b64 = await SvgToPngBase64Async(BuildSvg(), pixelWidth ?? PngPixelWidth, ct);
        return Convert.FromBase64String(b64);
    }

    /// <summary>Copies the current barcode to the clipboard. PNG rasterization + clipboard write
    /// happen entirely in the browser so the payload never crosses the Blazor Server SignalR
    /// boundary (whose default 32 KB receive limit would close the circuit on a large PNG).</summary>
    public async ValueTask CopyAsync(BarcodeExportFormat format, CancellationToken ct = default)
    {
        var svg = BuildSvg();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
        var token = linked.Token;
        try
        {
            var module = await LoadModuleAsync(token);
            if (format == BarcodeExportFormat.Svg)
                await module.InvokeVoidAsync("copyText", token, svg);
            else
                await module.InvokeVoidAsync("svgToPngClipboard", token, svg, PngPixelWidth);
        }
        catch (OperationCanceledException) { throw; }
        catch (JSDisconnectedException ex)   { throw new BarcodeExportException("Blazor circuit disconnected — copy failed.", ex); }
        catch (JSException ex)               { throw new BarcodeExportException("Browser rejected the copy operation.", ex); }
        catch (InvalidOperationException ex) { throw new BarcodeExportException("JS interop unavailable (server prerender?).", ex); }
    }

    /// <summary>Saves the current barcode in the requested format. Uses the File System Access
    /// API when available (native Save As dialog), otherwise auto-downloads to Downloads.
    /// PNG rasterization + save happen entirely in the browser.</summary>
    /// <param name="fileName">Suggested filename (with extension). Browser sandbox forbids a
    /// full path.</param>
    public async ValueTask SaveAsync(BarcodeExportFormat format, string fileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("A filename is required.", nameof(fileName));

        var svg = BuildSvg();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
        var token = linked.Token;
        try
        {
            var module = await LoadModuleAsync(token);
            if (format == BarcodeExportFormat.Svg)
                await module.InvokeVoidAsync("saveText", token, svg, "image/svg+xml;charset=utf-8", fileName);
            else
                await module.InvokeVoidAsync("svgToPngSave", token, svg, PngPixelWidth, fileName);
        }
        catch (OperationCanceledException) { throw; }
        catch (JSDisconnectedException ex)   { throw new BarcodeExportException("Blazor circuit disconnected — save failed.", ex); }
        catch (JSException ex)               { throw new BarcodeExportException("Browser rejected the save operation.", ex); }
        catch (InvalidOperationException ex) { throw new BarcodeExportException("JS interop unavailable (server prerender?).", ex); }
    }

    private async ValueTask<string> SvgToPngBase64Async(string svg, int? pixelWidth, CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
        var token = linked.Token;
        try
        {
            var module = await LoadModuleAsync(token);
            return await module.InvokeAsync<string>("svgToPngBase64", token, svg, pixelWidth);
        }
        catch (OperationCanceledException) { throw; }
        catch (JSDisconnectedException ex)   { throw new BarcodeExportException("Blazor circuit disconnected — PNG rasterization failed.", ex); }
        catch (JSException ex)               { throw new BarcodeExportException("Browser rejected the PNG rasterization.", ex); }
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

    // ---- SVG builder (pure C#) -----------------------------------------------------------------

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Esc(string s) => WebUtility.HtmlEncode(s);

    private string BuildSvg()
    {
        if (CancellationToken.IsCancellationRequested) return string.Empty;

        bool[] modules;
        try
        {
            modules = BarcodeEncoder.Encode(Symbology, Value);
        }
        catch (Exception ex)
        {
            return ErrorSvg(ex.Message);
        }

        var mw = ModuleWidth;
        var textH = ShowText ? 18 : 0;
        var box = Height + textH;
        var totalMods = modules.Length + QuietZone * 2;
        var w = totalMods * mw;
        var cls = SvgClass is null ? "atom-barcode" : "atom-barcode " + Esc(SvgClass);

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"{cls}\" role=\"img\" ")
          .Append($"aria-label=\"Barcode: {Esc(Value)}\" width=\"{F(w)}\" height=\"{F(box)}\" viewBox=\"0 0 {F(w)} {F(box)}\">");

        if (Background is not null)
            sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(w)}\" height=\"{F(box)}\" fill=\"{Background}\"/>");

        var i = 0;
        while (i < modules.Length)
        {
            if (CancellationToken.IsCancellationRequested) return string.Empty;
            if (!modules[i]) { i++; continue; }
            var run = 1;
            while (i + run < modules.Length && modules[i + run]) run++;
            var x = (QuietZone + i) * mw;
            sb.Append($"<rect x=\"{F(x)}\" y=\"0\" width=\"{F(run * mw)}\" height=\"{F(Height)}\" fill=\"{Color}\"/>");
            i += run;
        }

        if (ShowText)
            sb.Append($"<text x=\"{F(w / 2)}\" y=\"{F(Height + 13)}\" text-anchor=\"middle\" ")
              .Append($"font-family=\"ui-monospace, monospace\" font-size=\"12\" fill=\"{Color}\">{Esc(Value)}</text>");

        sb.Append("</svg>");
        return sb.ToString();
    }

    private string ErrorSvg(string message)
    {
        var box = Height + 18;
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"atom-barcode atom-barcode-error\" " +
               $"role=\"img\" aria-label=\"Barcode error\" width=\"240\" height=\"{F(box)}\" viewBox=\"0 0 240 {F(box)}\">" +
               $"<rect x=\"1\" y=\"1\" width=\"238\" height=\"{F(box - 2)}\" fill=\"none\" stroke=\"#c00\" stroke-dasharray=\"4 4\"/>" +
               $"<text x=\"120\" y=\"{F(box / 2)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" " +
               $"font-family=\"ui-monospace, monospace\" font-size=\"10\" fill=\"#c00\">{Esc(message)}</text></svg>";
    }
}
