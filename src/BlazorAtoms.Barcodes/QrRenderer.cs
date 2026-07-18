using System.Globalization;
using System.Net;
using System.Text;

namespace BlazorAtoms.Barcodes;

/// <summary>
/// Central SVG builder for <see cref="AtomQrCode"/>. Extracted here to keep the component's
/// code-behind readable and to allow rendering to be unit-tested without a Blazor host.
///
/// Rendering order (from outermost to innermost):
///   1. defs (gradients, drop-shadow filter)
///   2. outer frame + banner (when FrameShape != None)
///   3. background rect (inside frame area)
///   4. data modules (per ModuleShape)
///   5. finder eyes (per EyeFrame × EyePupil)
///   6. logo overlay + white pad
///
/// All emitted coordinates are in "module units" (1 unit = 1 QR module). The outer &lt;svg&gt;
/// element rescales via <c>width</c>/<c>height</c> attributes on the wrapping element.
/// </summary>
internal static class QrRenderer
{
    // ------------------------------------------------------------------ options record

    internal sealed class Options
    {
        public required bool[,] Matrix;
        public required int QuietZone;
        public required int Size;              // pixel size on the outer <svg>
        public string? SvgClass;
        public string Value = "";
        public string? Background;              // null = transparent

        // Modules
        public ModuleShape ModuleShape;
        public double ModuleRadius;

        // Eyes
        public EyeFrame EyeFrame;
        public EyePupil EyePupil;
        public double EyeFrameRadius;
        public double EyePupilRadius;
        public EyeCorner EyeCornerMask;
        public string? EyeColor;

        // Colorization
        public FillStyle ForegroundStyle;
        public string ForegroundColor = "#000000";
        public string? ForegroundGradientFrom;
        public string? ForegroundGradientTo;
        public double ForegroundGradientAngle;

        // Logo
        public string? LogoSrc;
        public byte[]? LogoBytes;
        public string LogoMimeType = "image/png";
        public double LogoSize;
        public LogoShape LogoShape;
        public string LogoPad = "#ffffff";

        // Frame
        public FrameShape FrameShape;
        public double FrameRadius;
        public string FrameStroke = "#000000";
        public double FrameStrokeWidth;
        public bool FrameInverted;
        public bool FrameShadow;
        public string FrameShadowColor = "#00000040";
        public double FrameMargin;
        public FrameBanner FrameBanner;
        public string FrameText = "";
        public string FrameTextColor = "#ffffff";
        public string FrameBannerColor = "#000000";
        public string FrameTextFontFamily = "sans-serif";
    }

    // ------------------------------------------------------------------ public entry point

    public static string Build(Options o)
    {
        var n = o.Matrix.GetLength(0);
        var q = o.QuietZone;
        var inner = n + 2 * q;
        var hasFrame = o.FrameShape != FrameShape.None;
        var bannerH = o.FrameBanner switch
        {
            FrameBanner.None => 0,
            FrameBanner.Inline => 0,
            _ => 5,
        };
        var frameMargin = hasFrame ? o.FrameMargin + 1 : 0;
        var totalDim = inner + 2 * frameMargin;
        var totalHeight = totalDim + (o.FrameBanner == FrameBanner.Top || o.FrameBanner == FrameBanner.Bottom || o.FrameBanner == FrameBanner.BottomPointer || o.FrameBanner == FrameBanner.BottomPill ? bannerH : 0);
        var topBanner = o.FrameBanner == FrameBanner.Top;
        var innerX = frameMargin;
        var innerY = frameMargin + (topBanner ? bannerH : 0);

        var cls = o.SvgClass is null ? "atom-qrcode" : "atom-qrcode " + Esc(o.SvgClass);
        var sb = new StringBuilder();
        var uid = System.Threading.Interlocked.Increment(ref _uidCounter);
        var fgId = $"ba-qr-fg-{uid}";
        var shadowId = $"ba-qr-shadow-{uid}";
        var tornId = $"ba-qr-torn-{uid}";

        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" ")
          .Append($"class=\"{cls}\" role=\"img\" aria-label=\"QR code: {Esc(o.Value)}\" ")
          .Append($"width=\"{o.Size}\" height=\"{o.Size}\" ")
          .Append($"viewBox=\"0 0 {F(totalDim)} {F(totalHeight)}\" shape-rendering=\"crispEdges\">");

        // 1. defs
        EmitDefs(sb, o, fgId, shadowId, tornId);

        // 2. frame background (behind everything)
        if (hasFrame)
            EmitFrame(sb, o, totalDim, totalHeight, bannerH, topBanner, shadowId, tornId);

        // 3. QR background rect
        if (!string.IsNullOrEmpty(o.Background))
            sb.Append($"<rect x=\"{F(innerX)}\" y=\"{F(innerY)}\" width=\"{F(inner)}\" height=\"{F(inner)}\" fill=\"{o.Background}\"/>");

        // 4/5. data modules + finder eyes (foreground fill = solid color or gradient url)
        var fgRef = o.ForegroundStyle == FillStyle.Solid ? o.ForegroundColor : $"url(#{fgId})";
        EmitDataModules(sb, o, innerX + q, innerY + q, n, fgRef);
        EmitFinderEyes(sb, o, innerX + q, innerY + q, n, fgRef);

        // 6. logo
        if (!string.IsNullOrEmpty(o.LogoSrc) || (o.LogoBytes is { Length: > 0 }))
            EmitLogo(sb, o, innerX + q, innerY + q, n);

        // 7. banner
        if (o.FrameBanner != FrameBanner.None && hasFrame)
            EmitBanner(sb, o, totalDim, totalHeight, bannerH, topBanner);

        sb.Append("</svg>");
        return sb.ToString();
    }

    // ------------------------------------------------------------------ defs

    private static void EmitDefs(StringBuilder sb, Options o, string fgId, string shadowId, string tornId)
    {
        var hasGradient = o.ForegroundStyle != FillStyle.Solid && !string.IsNullOrEmpty(o.ForegroundGradientFrom) && !string.IsNullOrEmpty(o.ForegroundGradientTo);
        var hasShadow = o.FrameShadow && o.FrameShape != FrameShape.None;
        var hasTorn = o.FrameShape == FrameShape.Torn;
        if (!hasGradient && !hasShadow && !hasTorn) return;

        sb.Append("<defs>");
        if (hasGradient)
        {
            if (o.ForegroundStyle == FillStyle.LinearGradient)
            {
                var angle = o.ForegroundGradientAngle * System.Math.PI / 180.0;
                var dx = System.Math.Cos(angle);
                var dy = System.Math.Sin(angle);
                sb.Append($"<linearGradient id=\"{fgId}\" x1=\"{F(0.5 - dx * 0.5)}\" y1=\"{F(0.5 - dy * 0.5)}\" x2=\"{F(0.5 + dx * 0.5)}\" y2=\"{F(0.5 + dy * 0.5)}\">")
                  .Append($"<stop offset=\"0%\" stop-color=\"{o.ForegroundGradientFrom}\"/>")
                  .Append($"<stop offset=\"100%\" stop-color=\"{o.ForegroundGradientTo}\"/>")
                  .Append("</linearGradient>");
            }
            else
            {
                sb.Append($"<radialGradient id=\"{fgId}\" cx=\"0.5\" cy=\"0.5\" r=\"0.5\">")
                  .Append($"<stop offset=\"0%\" stop-color=\"{o.ForegroundGradientFrom}\"/>")
                  .Append($"<stop offset=\"100%\" stop-color=\"{o.ForegroundGradientTo}\"/>")
                  .Append("</radialGradient>");
            }
        }
        if (hasShadow)
        {
            sb.Append($"<filter id=\"{shadowId}\" x=\"-10%\" y=\"-10%\" width=\"120%\" height=\"120%\">")
              .Append($"<feDropShadow dx=\"0.6\" dy=\"0.6\" stdDeviation=\"0.5\" flood-color=\"{o.FrameShadowColor}\"/>")
              .Append("</filter>");
        }
        if (hasTorn)
        {
            sb.Append($"<filter id=\"{tornId}\" x=\"-5%\" y=\"-5%\" width=\"110%\" height=\"110%\">")
              .Append("<feTurbulence type=\"fractalNoise\" baseFrequency=\"0.8\" numOctaves=\"2\" seed=\"7\"/>")
              .Append("<feDisplacementMap in=\"SourceGraphic\" scale=\"0.6\"/>")
              .Append("</filter>");
        }
        sb.Append("</defs>");
    }

    // ------------------------------------------------------------------ frame

    private static void EmitFrame(StringBuilder sb, Options o, double totalDim, double totalHeight, double bannerH, bool topBanner, string shadowId, string tornId)
    {
        var shadowAttr = o.FrameShadow ? $" filter=\"url(#{shadowId})\"" : "";
        var tornAttr = o.FrameShape == FrameShape.Torn ? $" filter=\"url(#{tornId})\"" : "";
        var fill = o.FrameInverted ? o.FrameStroke : "none";
        var stroke = o.FrameStroke;
        var sw = F(o.FrameStrokeWidth);

        // Frame reserves the whole viewBox area (excluding banner for Top variant).
        var y0 = 0.0;
        var y1 = totalHeight;
        // Some banners sit outside the frame rect (Bottom/BottomPointer/BottomPill).
        var extBottom = o.FrameBanner == FrameBanner.Bottom || o.FrameBanner == FrameBanner.BottomPointer || o.FrameBanner == FrameBanner.BottomPill;
        if (extBottom) y1 = totalHeight - bannerH;
        if (topBanner) y0 = bannerH;

        var w = totalDim;
        var h = y1 - y0;
        var cx = w / 2;
        var cy = y0 + h / 2;
        var r = System.Math.Min(w, h) / 2;

        switch (o.FrameShape)
        {
            case FrameShape.Square:
                sb.Append($"<rect x=\"0\" y=\"{F(y0)}\" width=\"{F(w)}\" height=\"{F(h)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{sw}\"{shadowAttr}/>");
                break;
            case FrameShape.Rounded:
                var rr = o.FrameRadius * System.Math.Min(w, h);
                sb.Append($"<rect x=\"0\" y=\"{F(y0)}\" width=\"{F(w)}\" height=\"{F(h)}\" rx=\"{F(rr)}\" ry=\"{F(rr)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{sw}\"{shadowAttr}/>");
                break;
            case FrameShape.Circle:
                sb.Append($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{sw}\"{shadowAttr}/>");
                break;
            case FrameShape.DottedCircle:
                sb.Append($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{sw}\" stroke-dasharray=\"1 1.5\"{shadowAttr}/>");
                break;
            case FrameShape.DoubleCircle:
                sb.Append($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{sw}\"{shadowAttr}/>")
                  .Append($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r - 1)}\" fill=\"none\" stroke=\"{stroke}\" stroke-width=\"{F(o.FrameStrokeWidth * 0.6)}\"/>");
                break;
            case FrameShape.Blob:
                sb.Append($"<path d=\"{BuildBlobPath(cx, cy, r, 12)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{sw}\"{shadowAttr}/>");
                break;
            case FrameShape.Torn:
                sb.Append($"<rect x=\"0\" y=\"{F(y0)}\" width=\"{F(w)}\" height=\"{F(h)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{sw}\"{tornAttr}{shadowAttr}/>");
                break;
        }
    }

    private static string BuildBlobPath(double cx, double cy, double r, int lobes)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < lobes; i++)
        {
            var a1 = i * 2 * System.Math.PI / lobes;
            var a2 = (i + 1) * 2 * System.Math.PI / lobes;
            var rr1 = i % 2 == 0 ? r : r * 0.9;
            var rr2 = (i + 1) % 2 == 0 ? r : r * 0.9;
            var x1 = cx + rr1 * System.Math.Cos(a1);
            var y1 = cy + rr1 * System.Math.Sin(a1);
            var x2 = cx + rr2 * System.Math.Cos(a2);
            var y2 = cy + rr2 * System.Math.Sin(a2);
            if (i == 0) sb.Append($"M{F(x1)} {F(y1)}");
            var midA = (a1 + a2) / 2;
            var midR = (rr1 + rr2) / 2 * 1.15;
            var mx = cx + midR * System.Math.Cos(midA);
            var my = cy + midR * System.Math.Sin(midA);
            sb.Append($"Q{F(mx)} {F(my)} {F(x2)} {F(y2)}");
        }
        sb.Append("Z");
        return sb.ToString();
    }

    // ------------------------------------------------------------------ banner

    private static void EmitBanner(StringBuilder sb, Options o, double totalDim, double totalHeight, double bannerH, bool topBanner)
    {
        if (string.IsNullOrEmpty(o.FrameText)) return;
        var w = totalDim;
        var by = topBanner ? 0 : totalHeight - bannerH;
        var textY = by + bannerH * 0.68;
        var fontSize = bannerH * 0.55;

        switch (o.FrameBanner)
        {
            case FrameBanner.Bottom:
            case FrameBanner.Top:
                sb.Append($"<rect x=\"0\" y=\"{F(by)}\" width=\"{F(w)}\" height=\"{F(bannerH)}\" fill=\"{o.FrameBannerColor}\"/>");
                break;
            case FrameBanner.BottomPill:
                var pillW = w * 0.55;
                var pillX = (w - pillW) / 2;
                sb.Append($"<rect x=\"{F(pillX)}\" y=\"{F(by)}\" width=\"{F(pillW)}\" height=\"{F(bannerH)}\" rx=\"{F(bannerH / 2)}\" ry=\"{F(bannerH / 2)}\" fill=\"{o.FrameBannerColor}\"/>");
                break;
            case FrameBanner.BottomPointer:
                sb.Append($"<rect x=\"0\" y=\"{F(by)}\" width=\"{F(w)}\" height=\"{F(bannerH)}\" fill=\"{o.FrameBannerColor}\"/>")
                  .Append($"<polygon points=\"{F(w / 2 - 2)},{F(by)} {F(w / 2 + 2)},{F(by)} {F(w / 2)},{F(by - 1.5)}\" fill=\"{o.FrameBannerColor}\"/>");
                break;
            case FrameBanner.Inline:
                textY = totalHeight - 0.8;
                break;
        }

        sb.Append($"<text x=\"{F(w / 2)}\" y=\"{F(textY)}\" text-anchor=\"middle\" ")
          .Append($"font-family=\"{Esc(o.FrameTextFontFamily)}\" font-size=\"{F(fontSize)}\" ")
          .Append($"font-weight=\"600\" fill=\"{o.FrameTextColor}\">{Esc(o.FrameText)}</text>");
    }

    // ------------------------------------------------------------------ data modules

    private static void EmitDataModules(StringBuilder sb, Options o, double ox, double oy, int n, string fillRef)
    {
        // Skip finder-pattern regions — they render via EmitFinderEyes.
        bool InFinder(int r, int c) =>
            (r < 7 && c < 7) || (r < 7 && c >= n - 7) || (r >= n - 7 && c < 7);

        switch (o.ModuleShape)
        {
            case ModuleShape.Square:
            case ModuleShape.Rounded:
                {
                    var d = new StringBuilder();
                    var rx = o.ModuleShape == ModuleShape.Rounded ? o.ModuleRadius : 0;
                    for (var r = 0; r < n; r++)
                    {
                        var c = 0;
                        while (c < n)
                        {
                            if (InFinder(r, c) || !o.Matrix[r, c]) { c++; continue; }
                            if (rx > 0)
                            {
                                // Per-module rounded rect (path so all data modules can share one fill/stroke).
                                d.Append(RoundedRectPath(ox + c, oy + r, 1, 1, rx));
                                c++;
                            }
                            else
                            {
                                var run = 1;
                                while (c + run < n && !InFinder(r, c + run) && o.Matrix[r, c + run]) run++;
                                d.Append($"M{F(ox + c)} {F(oy + r)}h{run}v1h-{run}z");
                                c += run;
                            }
                        }
                    }
                    if (d.Length > 0) sb.Append($"<path d=\"{d}\" fill=\"{fillRef}\"/>");
                    break;
                }
            case ModuleShape.Dot:
                {
                    var d = new StringBuilder();
                    for (var r = 0; r < n; r++)
                        for (var c = 0; c < n; c++)
                        {
                            if (InFinder(r, c) || !o.Matrix[r, c]) continue;
                            var cx = ox + c + 0.5;
                            var cy = oy + r + 0.5;
                            d.Append($"M{F(cx - 0.5)} {F(cy)}a0.5 0.5 0 1 0 1 0a0.5 0.5 0 1 0 -1 0z");
                        }
                    if (d.Length > 0) sb.Append($"<path d=\"{d}\" fill=\"{fillRef}\"/>");
                    break;
                }
            case ModuleShape.Ellipse:
                {
                    var d = new StringBuilder();
                    for (var r = 0; r < n; r++)
                        for (var c = 0; c < n; c++)
                        {
                            if (InFinder(r, c) || !o.Matrix[r, c]) continue;
                            var cx = ox + c + 0.5;
                            var cy = oy + r + 0.5;
                            d.Append($"M{F(cx - 0.5)} {F(cy)}a0.5 0.35 0 1 0 1 0a0.5 0.35 0 1 0 -1 0z");
                        }
                    if (d.Length > 0) sb.Append($"<path d=\"{d}\" fill=\"{fillRef}\"/>");
                    break;
                }
            case ModuleShape.Diamond:
                {
                    var d = new StringBuilder();
                    for (var r = 0; r < n; r++)
                        for (var c = 0; c < n; c++)
                        {
                            if (InFinder(r, c) || !o.Matrix[r, c]) continue;
                            var cx = ox + c + 0.5;
                            var cy = oy + r + 0.5;
                            d.Append($"M{F(cx)} {F(cy - 0.5)}L{F(cx + 0.5)} {F(cy)}L{F(cx)} {F(cy + 0.5)}L{F(cx - 0.5)} {F(cy)}z");
                        }
                    if (d.Length > 0) sb.Append($"<path d=\"{d}\" fill=\"{fillRef}\"/>");
                    break;
                }
            case ModuleShape.Star:
                {
                    var d = new StringBuilder();
                    for (var r = 0; r < n; r++)
                        for (var c = 0; c < n; c++)
                        {
                            if (InFinder(r, c) || !o.Matrix[r, c]) continue;
                            var cx = ox + c + 0.5;
                            var cy = oy + r + 0.5;
                            // 4-point star.
                            d.Append($"M{F(cx)} {F(cy - 0.5)}L{F(cx + 0.15)} {F(cy - 0.15)}L{F(cx + 0.5)} {F(cy)}L{F(cx + 0.15)} {F(cy + 0.15)}L{F(cx)} {F(cy + 0.5)}L{F(cx - 0.15)} {F(cy + 0.15)}L{F(cx - 0.5)} {F(cy)}L{F(cx - 0.15)} {F(cy - 0.15)}z");
                        }
                    if (d.Length > 0) sb.Append($"<path d=\"{d}\" fill=\"{fillRef}\"/>");
                    break;
                }
            case ModuleShape.Pill:
                {
                    var d = new StringBuilder();
                    for (var r = 0; r < n; r++)
                    {
                        var c = 0;
                        while (c < n)
                        {
                            if (InFinder(r, c) || !o.Matrix[r, c]) { c++; continue; }
                            var run = 1;
                            while (c + run < n && !InFinder(r, c + run) && o.Matrix[r, c + run]) run++;
                            var x = ox + c;
                            var y = oy + r;
                            d.Append($"M{F(x + 0.5)} {F(y)}h{run - 1}a0.5 0.5 0 0 1 0 1h-{run - 1}a0.5 0.5 0 0 1 0 -1z");
                            c += run;
                        }
                    }
                    if (d.Length > 0) sb.Append($"<path d=\"{d}\" fill=\"{fillRef}\"/>");
                    break;
                }
            case ModuleShape.Blob:
                {
                    // Simplified: dot with slight overflow to visually merge adjacent dots.
                    var d = new StringBuilder();
                    for (var r = 0; r < n; r++)
                        for (var c = 0; c < n; c++)
                        {
                            if (InFinder(r, c) || !o.Matrix[r, c]) continue;
                            var cx = ox + c + 0.5;
                            var cy = oy + r + 0.5;
                            d.Append($"M{F(cx - 0.6)} {F(cy)}a0.6 0.6 0 1 0 1.2 0a0.6 0.6 0 1 0 -1.2 0z");
                        }
                    if (d.Length > 0) sb.Append($"<path d=\"{d}\" fill=\"{fillRef}\" fill-rule=\"nonzero\"/>");
                    break;
                }
        }
    }

    // ------------------------------------------------------------------ finder eyes

    private static void EmitFinderEyes(StringBuilder sb, Options o, double ox, double oy, int n, string fillRef)
    {
        var eyeFill = o.EyeColor ?? fillRef;
        DrawEye(sb, o, ox, oy, eyeFill);
        DrawEye(sb, o, ox + n - 7, oy, eyeFill);
        DrawEye(sb, o, ox, oy + n - 7, eyeFill);
    }

    private static void DrawEye(StringBuilder sb, Options o, double x, double y, string fill)
    {
        // Outer frame: 7x7 hollow with 1-module thickness.
        switch (o.EyeFrame)
        {
            case EyeFrame.Square:
                sb.Append(SquareRing(x, y, 7, 1, 0, EyeCorner.None, fill));
                break;
            case EyeFrame.Rounded:
                sb.Append(SquareRing(x, y, 7, 1, o.EyeFrameRadius * 3, o.EyeCornerMask, fill));
                break;
            case EyeFrame.Circle:
                sb.Append($"<path d=\"M{F(x + 3.5)} {F(y)}a3.5 3.5 0 1 0 0 7a3.5 3.5 0 1 0 0 -7zM{F(x + 3.5)} {F(y + 1)}a2.5 2.5 0 1 1 0 5a2.5 2.5 0 1 1 0 -5z\" fill=\"{fill}\" fill-rule=\"evenodd\"/>");
                break;
        }

        // Pupil: 3x3 centered at x+2,y+2.
        var px2 = x + 2;
        var py2 = y + 2;
        switch (o.EyePupil)
        {
            case EyePupil.Square:
                sb.Append($"<rect x=\"{F(px2)}\" y=\"{F(py2)}\" width=\"3\" height=\"3\" fill=\"{fill}\"/>");
                break;
            case EyePupil.Rounded:
                var pr = o.EyePupilRadius * 1.5;
                sb.Append($"<rect x=\"{F(px2)}\" y=\"{F(py2)}\" width=\"3\" height=\"3\" rx=\"{F(pr)}\" ry=\"{F(pr)}\" fill=\"{fill}\"/>");
                break;
            case EyePupil.Circle:
                sb.Append($"<circle cx=\"{F(px2 + 1.5)}\" cy=\"{F(py2 + 1.5)}\" r=\"1.5\" fill=\"{fill}\"/>");
                break;
            case EyePupil.Rhombus:
                sb.Append($"<path d=\"M{F(px2 + 1.5)} {F(py2)}L{F(px2 + 3)} {F(py2 + 1.5)}L{F(px2 + 1.5)} {F(py2 + 3)}L{F(px2)} {F(py2 + 1.5)}z\" fill=\"{fill}\"/>");
                break;
        }
    }

    private static string SquareRing(double x, double y, double size, double thickness, double radius, EyeCorner mask, string fill)
    {
        // Outer + inner path (fill-rule evenodd cuts the hole).
        var outer = radius <= 0
            ? $"M{F(x)} {F(y)}h{F(size)}v{F(size)}h-{F(size)}z"
            : MaskedRoundedRect(x, y, size, size, radius, mask);
        var inner = radius <= 0
            ? $"M{F(x + thickness)} {F(y + thickness)}h{F(size - 2 * thickness)}v{F(size - 2 * thickness)}h-{F(size - 2 * thickness)}z"
            : MaskedRoundedRect(x + thickness, y + thickness, size - 2 * thickness, size - 2 * thickness, System.Math.Max(0, radius - thickness), mask);
        return $"<path d=\"{outer} {inner}\" fill=\"{fill}\" fill-rule=\"evenodd\"/>";
    }

    private static string MaskedRoundedRect(double x, double y, double w, double h, double r, EyeCorner mask)
    {
        var tl = (mask & EyeCorner.TopLeft) != 0 ? r : 0;
        var tr = (mask & EyeCorner.TopRight) != 0 ? r : 0;
        var br = (mask & EyeCorner.BottomRight) != 0 ? r : 0;
        var bl = (mask & EyeCorner.BottomLeft) != 0 ? r : 0;
        var sb = new StringBuilder();
        sb.Append($"M{F(x + tl)} {F(y)}");
        sb.Append($"H{F(x + w - tr)}");
        if (tr > 0) sb.Append($"A{F(tr)} {F(tr)} 0 0 1 {F(x + w)} {F(y + tr)}");
        sb.Append($"V{F(y + h - br)}");
        if (br > 0) sb.Append($"A{F(br)} {F(br)} 0 0 1 {F(x + w - br)} {F(y + h)}");
        sb.Append($"H{F(x + bl)}");
        if (bl > 0) sb.Append($"A{F(bl)} {F(bl)} 0 0 1 {F(x)} {F(y + h - bl)}");
        sb.Append($"V{F(y + tl)}");
        if (tl > 0) sb.Append($"A{F(tl)} {F(tl)} 0 0 1 {F(x + tl)} {F(y)}");
        sb.Append("Z");
        return sb.ToString();
    }

    private static string RoundedRectPath(double x, double y, double w, double h, double r)
    {
        return $"M{F(x + r)} {F(y)}h{F(w - 2 * r)}a{F(r)} {F(r)} 0 0 1 {F(r)} {F(r)}v{F(h - 2 * r)}a{F(r)} {F(r)} 0 0 1 -{F(r)} {F(r)}h-{F(w - 2 * r)}a{F(r)} {F(r)} 0 0 1 -{F(r)} -{F(r)}v-{F(h - 2 * r)}a{F(r)} {F(r)} 0 0 1 {F(r)} -{F(r)}z";
    }


    // ------------------------------------------------------------------ logo

    private static void EmitLogo(StringBuilder sb, Options o, double ox, double oy, int n)
    {
        var area = System.Math.Clamp(o.LogoSize, 0.05, 0.30);
        var side = System.Math.Sqrt(area) * n;
        var cx = ox + n / 2.0;
        var cy = oy + n / 2.0;
        var lx = cx - side / 2;
        var ly = cy - side / 2;
        var padSize = side + 0.6;
        var padX = cx - padSize / 2;
        var padY = cy - padSize / 2;

        // Pad shape.
        switch (o.LogoShape)
        {
            case LogoShape.Circle:
                sb.Append($"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(padSize / 2)}\" fill=\"{o.LogoPad}\"/>");
                break;
            case LogoShape.Rounded:
                sb.Append($"<rect x=\"{F(padX)}\" y=\"{F(padY)}\" width=\"{F(padSize)}\" height=\"{F(padSize)}\" rx=\"{F(padSize * 0.15)}\" ry=\"{F(padSize * 0.15)}\" fill=\"{o.LogoPad}\"/>");
                break;
            default:
                sb.Append($"<rect x=\"{F(padX)}\" y=\"{F(padY)}\" width=\"{F(padSize)}\" height=\"{F(padSize)}\" fill=\"{o.LogoPad}\"/>");
                break;
        }

        var href = !string.IsNullOrEmpty(o.LogoSrc)
            ? o.LogoSrc
            : $"data:{o.LogoMimeType};base64,{Convert.ToBase64String(o.LogoBytes!)}";
        sb.Append($"<image x=\"{F(lx)}\" y=\"{F(ly)}\" width=\"{F(side)}\" height=\"{F(side)}\" preserveAspectRatio=\"xMidYMid meet\" xlink:href=\"{Esc(href)}\" href=\"{Esc(href)}\"/>");
    }

    // ------------------------------------------------------------------ helpers

    private static int _uidCounter;

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Esc(string s) => WebUtility.HtmlEncode(s);
}
