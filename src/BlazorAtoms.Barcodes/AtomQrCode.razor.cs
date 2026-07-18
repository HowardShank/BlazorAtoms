using System.Net;
using System.Text;
using BlazorAtoms.Barcodes.Encoders;
using BlazorAtoms.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Barcodes;

/// <summary>
/// QR (2D matrix) barcode component. Renders as inline SVG via pure C#. Optional Copy / Save /
/// GetPngBytes methods lazily import a small JS module (invisible to consumers).
/// </summary>
public partial class AtomQrCode : AtomComponentBase, IAsyncDisposable
{
    private ElementReference _rootRef;
    private IJSObjectReference? _jsModule;
    private readonly CancellationTokenSource _cts = new();

    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ---- rendering parameters ------------------------------------------------------------------

    /// <summary>Text/data to encode (UTF-8).</summary>
    [Parameter, EditorRequired] public string Value { get; set; } = "";

    /// <summary>Rendered width/height in pixels.</summary>
    [Parameter] public int Size { get; set; } = 160;

    /// <summary>Error-correction level.</summary>
    [Parameter] public QrErrorCorrection EcLevel { get; set; } = QrErrorCorrection.M;

    /// <summary>Module (dark) color.</summary>
    [Parameter] public string Color { get; set; } = "#000000";

    /// <summary>Background fill. Null leaves it transparent.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Quiet-zone border width, in modules (spec minimum is 4).</summary>
    [Parameter] public int QuietZone { get; set; } = 4;

    /// <summary>Minimum QR version (1–40). The encoder auto-selects the smallest version that fits
    /// the payload; setting this floor forces a larger code even when the data would fit in a
    /// smaller one (useful for print pipelines that need a consistent module count regardless of
    /// payload length). Default 1 = "no floor".</summary>
    [Parameter] public int MinVersion { get; set; } = 1;

    /// <summary>Extra CSS class(es) on the generated <c>&lt;svg&gt;</c> element.</summary>
    [Parameter] public string? SvgClass { get; set; }

    /// <summary>Target pixel width when rasterizing to PNG. Null uses <c>Size * 4</c> for a
    /// crisp print-friendly render.</summary>
    [Parameter] public int? PngPixelWidth { get; set; }

    // ---- link parameters -----------------------------------------------------------------------

    /// <summary>When set, the code is wrapped in an anchor pointing at this URL. Wins over
    /// <see cref="AutoLink"/>.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>When true and <see cref="Href"/> is not set, the component inspects
    /// <see cref="Value"/> and wraps in an anchor when it parses as an <c>http/https/mailto/tel</c>
    /// URL. Rejects <c>javascript:</c>, <c>data:</c>, <c>file:</c>, etc.</summary>
    [Parameter] public bool AutoLink { get; set; }

    /// <summary>Anchor <c>target</c>. Defaults to <c>"_blank"</c>; pass empty string for same-tab.</summary>
    [Parameter] public string LinkTarget { get; set; } = "_blank";

    internal string? ResolvedHref => BarcodeLink.TryResolveUrl(Href, Value, AutoLink, out var u) ? u : null;

    // ---- module styling ------------------------------------------------------------------------

    /// <summary>Data-module shape.</summary>
    [Parameter] public ModuleShape ModuleShape { get; set; } = ModuleShape.Square;

    /// <summary>Rounded-corner radius for <see cref="ModuleShape.Rounded"/> (0.0–0.5, fraction of module).</summary>
    [Parameter] public double ModuleRadius { get; set; } = 0.0;

    // ---- eye styling ---------------------------------------------------------------------------

    /// <summary>Finder-eye outer frame shape.</summary>
    [Parameter] public EyeFrame EyeFrame { get; set; } = EyeFrame.Square;

    /// <summary>Finder-eye pupil shape.</summary>
    [Parameter] public EyePupil EyePupil { get; set; } = EyePupil.Square;

    /// <summary>Rounded-corner radius for the eye frame (0.0–0.5).</summary>
    [Parameter] public double EyeFrameRadius { get; set; } = 0.0;

    /// <summary>Rounded-corner radius for the eye pupil (0.0–0.5).</summary>
    [Parameter] public double EyePupilRadius { get; set; } = 0.0;

    /// <summary>Which corners of the eye frame get <see cref="EyeFrameRadius"/> applied.</summary>
    [Parameter] public EyeCorner EyeCornerMask { get; set; } = EyeCorner.All;

    /// <summary>Override colour for all three finder eyes (frame + pupil). Null = inherit foreground.</summary>
    [Parameter] public string? EyeColor { get; set; }

    // ---- colorization --------------------------------------------------------------------------

    /// <summary>Foreground fill style — solid or linear/radial gradient.</summary>
    [Parameter] public FillStyle ForegroundStyle { get; set; } = FillStyle.Solid;

    /// <summary>Solid foreground colour (used when <see cref="ForegroundStyle"/> is Solid). Legacy
    /// alias for <see cref="Color"/> — either sets the same fill.</summary>
    [Parameter] public string ForegroundColor { get; set; } = "#000000";

    /// <summary>Gradient endpoint 1 (start / centre).</summary>
    [Parameter] public string? ForegroundGradientFrom { get; set; }

    /// <summary>Gradient endpoint 2 (end / edge).</summary>
    [Parameter] public string? ForegroundGradientTo { get; set; }

    /// <summary>Linear-gradient angle in degrees (0 = horizontal, 90 = vertical).</summary>
    [Parameter] public double ForegroundGradientAngle { get; set; } = 0;

    // ---- logo overlay --------------------------------------------------------------------------

    /// <summary>Logo image URL / data URI. Mutually exclusive with <see cref="LogoBytes"/>.</summary>
    [Parameter] public string? LogoSrc { get; set; }

    /// <summary>Logo image bytes. Mutually exclusive with <see cref="LogoSrc"/>.</summary>
    [Parameter] public byte[]? LogoBytes { get; set; }

    /// <summary>MIME type used when <see cref="LogoBytes"/> is set.</summary>
    [Parameter] public string LogoMimeType { get; set; } = "image/png";

    /// <summary>Logo area fraction of QR area (0.05–0.30). EC-H recommended when set.</summary>
    [Parameter] public double LogoSize { get; set; } = 0.15;

    /// <summary>Backing shape drawn behind the logo image.</summary>
    [Parameter] public LogoShape LogoShape { get; set; } = LogoShape.Square;

    /// <summary>Pad colour behind the logo (visible when the logo has transparency).</summary>
    [Parameter] public string LogoPad { get; set; } = "#ffffff";

    // ---- outer frame + banner ------------------------------------------------------------------

    /// <summary>Outer decorative frame shape wrapping the QR.</summary>
    [Parameter] public FrameShape FrameShape { get; set; } = FrameShape.None;

    /// <summary>Rounded-corner radius for <see cref="FrameShape.Rounded"/> (fraction of frame side).</summary>
    [Parameter] public double FrameRadius { get; set; } = 0.05;

    /// <summary>Frame stroke colour.</summary>
    [Parameter] public string FrameStroke { get; set; } = "#000000";

    /// <summary>Frame stroke width, in module units.</summary>
    [Parameter] public double FrameStrokeWidth { get; set; } = 1.0;

    /// <summary>When true, the frame is filled dark with the QR sitting in a white knockout inside.</summary>
    [Parameter] public bool FrameInverted { get; set; }

    /// <summary>When true, emits an SVG drop-shadow behind the frame.</summary>
    [Parameter] public bool FrameShadow { get; set; }

    /// <summary>Drop-shadow colour (CSS colour with optional alpha).</summary>
    [Parameter] public string FrameShadowColor { get; set; } = "#00000040";

    /// <summary>Gap between the frame stroke and the QR quiet zone, in module units.</summary>
    [Parameter] public double FrameMargin { get; set; } = 0.5;

    /// <summary>Banner position for <see cref="FrameText"/>.</summary>
    [Parameter] public FrameBanner FrameBanner { get; set; } = FrameBanner.None;

    /// <summary>Banner label. Recommended length ≤15 characters.</summary>
    [Parameter] public string FrameText { get; set; } = "SCAN ME";

    /// <summary>Colour of the banner text.</summary>
    [Parameter] public string FrameTextColor { get; set; } = "#ffffff";

    /// <summary>Fill colour of the banner bar.</summary>
    [Parameter] public string FrameBannerColor { get; set; } = "#000000";

    /// <summary>Font family used for the banner text (any CSS font).</summary>
    [Parameter] public string FrameTextFontFamily { get; set; } = "sans-serif";

    // ---- public API ----------------------------------------------------------------------------

    /// <summary>Returns the currently rendered SVG markup as a string.</summary>
    public string GetSvg() => BuildSvg();

    /// <summary>Rasterizes the current SVG to a PNG byte array via the browser canvas.</summary>
    public async ValueTask<byte[]> GetPngBytesAsync(int? pixelWidth = null, CancellationToken ct = default)
    {
        var b64 = await SvgToPngBase64Async(BuildSvg(), pixelWidth ?? PngPixelWidth ?? Size * 4, ct);
        return Convert.FromBase64String(b64);
    }

    /// <summary>Copies the current QR code to the clipboard in the requested format.</summary>
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
                await module.InvokeVoidAsync("svgToPngClipboard", token, svg, PngPixelWidth ?? Size * 4);
        }
        catch (OperationCanceledException) { throw; }
        catch (JSDisconnectedException ex)   { throw new BarcodeExportException("Blazor circuit disconnected — copy failed.", ex); }
        catch (JSException ex)               { throw new BarcodeExportException("Browser rejected the copy operation.", ex); }
        catch (InvalidOperationException ex) { throw new BarcodeExportException("JS interop unavailable (server prerender?).", ex); }
    }

    /// <summary>Saves the current QR code in the requested format. Uses the File System Access
    /// API when available (native Save As dialog), otherwise auto-downloads to Downloads.
    /// PNG rasterization + save happens entirely in the browser.</summary>
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
                await module.InvokeVoidAsync("svgToPngSave", token, svg, PngPixelWidth ?? Size * 4, fileName);
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

    private static string Esc(string s) => WebUtility.HtmlEncode(s);

    private string BuildSvg()
    {
        if (CancellationToken.IsCancellationRequested) return string.Empty;

        bool[,] m;
        try
        {
            m = QrEncoder.Encode(Value, EcLevel, MinVersion);
        }
        catch (Exception ex)
        {
            return ErrorSvg(ex.Message);
        }

        // Legacy `Color` param remains the primary knob for the solid-foreground default. When the
        // consumer sets `ForegroundColor` to a non-black value, prefer that; otherwise honour
        // `Color`. Gradient overrides both when `ForegroundStyle != Solid`.
        var fgColor = ForegroundStyle == FillStyle.Solid
            ? (ForegroundColor != "#000000" ? ForegroundColor : Color)
            : ForegroundColor;

        try
        {
            var opts = new QrRenderer.Options
            {
                Matrix = m,
                QuietZone = QuietZone,
                Size = Size,
                SvgClass = SvgClass,
                Value = Value,
                Background = Background,
                ModuleShape = ModuleShape,
                ModuleRadius = ModuleRadius,
                EyeFrame = EyeFrame,
                EyePupil = EyePupil,
                EyeFrameRadius = EyeFrameRadius,
                EyePupilRadius = EyePupilRadius,
                EyeCornerMask = EyeCornerMask,
                EyeColor = EyeColor,
                ForegroundStyle = ForegroundStyle,
                ForegroundColor = fgColor,
                ForegroundGradientFrom = ForegroundGradientFrom,
                ForegroundGradientTo = ForegroundGradientTo,
                ForegroundGradientAngle = ForegroundGradientAngle,
                LogoSrc = LogoSrc,
                LogoBytes = LogoBytes,
                LogoMimeType = LogoMimeType,
                LogoSize = LogoSize,
                LogoShape = LogoShape,
                LogoPad = LogoPad,
                FrameShape = FrameShape,
                FrameRadius = FrameRadius,
                FrameStroke = FrameStroke,
                FrameStrokeWidth = FrameStrokeWidth,
                FrameInverted = FrameInverted,
                FrameShadow = FrameShadow,
                FrameShadowColor = FrameShadowColor,
                FrameMargin = FrameMargin,
                FrameBanner = FrameBanner,
                FrameText = FrameText,
                FrameTextColor = FrameTextColor,
                FrameBannerColor = FrameBannerColor,
                FrameTextFontFamily = FrameTextFontFamily,
            };
            return QrRenderer.Build(opts);
        }
        catch (Exception ex)
        {
            return ErrorSvg($"Render error: {ex.GetType().Name} — {ex.Message}");
        }
    }

    private string ErrorSvg(string message) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"atom-qrcode atom-qrcode-error\" role=\"img\" " +
        $"aria-label=\"QR error\" width=\"{Size}\" height=\"{Size}\" viewBox=\"0 0 {Size} {Size}\">" +
        $"<rect x=\"1\" y=\"1\" width=\"{Size - 2}\" height=\"{Size - 2}\" fill=\"none\" stroke=\"#c00\" stroke-dasharray=\"4 4\"/>" +
        $"<text x=\"{Size / 2}\" y=\"{Size / 2}\" text-anchor=\"middle\" dominant-baseline=\"middle\" " +
        $"font-family=\"ui-monospace, monospace\" font-size=\"10\" fill=\"#c00\">{Esc(message)}</text></svg>";
}
