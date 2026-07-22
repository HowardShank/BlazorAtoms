using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZXing;
using ZXing.Common;

namespace BlazorAtoms.Barcodes;

/// <summary>
/// In-process QR image decoder. Uses SixLabors.ImageSharp to decode PNG/JPEG/GIF/BMP/WebP bytes
/// into a raw pixel buffer, then ZXing.Net's BarcodeReaderGeneric (with AutoRotate + TryInverted
/// + TryHarder) against a luminance source. Pure managed — works identically in Blazor Server,
/// WebAssembly, and any other .NET host.
/// </summary>
internal static class QrImageDecoder
{
    /// <summary>Result payload plus a human-readable diagnostic when decode fails.</summary>
    public readonly record struct Result(string? Payload, string? Diagnostic);

    /// <summary>
    /// Decode the first QR code in the supplied image bytes. Attempts several strategies before
    /// giving up (native size, then upscaled for tiny screenshots). Always returns — the diagnostic
    /// tells the caller *why* decode failed when <see cref="Result.Payload"/> is null.
    /// </summary>
    public static Result TryDecode(byte[] imageBytes)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            return new Result(null, "no bytes supplied");

        Image<Rgba32> image;
        try
        {
            image = Image.Load<Rgba32>(imageBytes);
        }
        catch (Exception ex)
        {
            return new Result(null, $"ImageSharp couldn't decode the bytes ({ex.GetType().Name}: {ex.Message})");
        }

        try
        {
            // Flatten transparency onto white — QRs saved with alpha would otherwise read as pure
            // black (RGB (0,0,0) hidden behind alpha=0) and the finder-pattern search fails on a
            // uniformly-black image.
            image.Mutate(ctx => ctx.BackgroundColor(Color.White));

            // Strategy 1: clean-rasterized QR (PureBarcode=true — skips finder-pattern search,
            // reads modules directly off the grid). Fast + reliable for computer-generated QRs.
            var payload = DecodePixels(image, pureBarcode: true);
            if (payload is not null) return new Result(payload, null);

            // Strategy 2: general path (PureBarcode=false + TryHarder + TryInverted + AutoRotate)
            // — handles photos of QRs, tilted / distorted / low-contrast images.
            payload = DecodePixels(image, pureBarcode: false);
            if (payload is not null) return new Result(payload, null);

            // Strategy 3: threshold to pure b/w first. Kills anti-aliasing seams that appear
            // when a browser rasterizes stacked <rect> runs (visible as horizontal hairline
            // artifacts in the pixels) — those sub-pixel gray rows defeat ZXing's grid alignment.
            using (var thresholded = image.Clone(ctx => ctx.Grayscale().BinaryThreshold(0.5f)))
            {
                payload = DecodePixels(thresholded, pureBarcode: true) ?? DecodePixels(thresholded, pureBarcode: false);
                if (payload is not null) return new Result(payload, null);
            }

            // Strategy 4: 2× upscale — helps when the pasted QR is smaller than ZXing's
            // finder-pattern recogniser expects (common with tiny clipboard screenshots).
            using var upscaled = image.Clone(ctx => ctx.Resize(image.Width * 2, image.Height * 2));
            payload = DecodePixels(upscaled, pureBarcode: true) ?? DecodePixels(upscaled, pureBarcode: false);
            if (payload is not null) return new Result(payload, null);

            return new Result(null, $"no QR code detected (ZXing gave up after native {image.Width}×{image.Height} pure + general + threshold + 2× upscale on white background).");
        }
        catch (Exception ex)
        {
            return new Result(null, $"decoder exception ({ex.GetType().Name}: {ex.Message})");
        }
        finally
        {
            image.Dispose();
        }
    }

    /// <summary>
    /// Decode a QR straight from a raw RGBA pixel buffer (row-major, 4 bytes/pixel, alpha already
    /// flattened onto white by the caller). Used by the WebAssembly hot path: the browser decodes
    /// and downscales the image natively via a &lt;canvas&gt; (instant) and hands the pixels here,
    /// so ImageSharp — brutally slow in the interpreted WASM runtime — is skipped entirely. ZXing's
    /// own binarizer handles thresholding, and TryHarder covers scale, so the pure + general passes
    /// are enough (no ImageSharp-only threshold/upscale strategies).
    /// </summary>
    public static Result TryDecodeRgba(byte[] rgba, int width, int height)
    {
        if (rgba is null || rgba.Length == 0)
            return new Result(null, "no pixel bytes supplied");
        if (width <= 0 || height <= 0 || rgba.Length < width * height * 4)
            return new Result(null, $"pixel buffer too small for {width}×{height} RGBA");

        try
        {
            var luminance = new RGBLuminanceSource(rgba, width, height, RGBLuminanceSource.BitmapFormat.RGBA32);
            var payload = DecodeLuminance(luminance, pureBarcode: true)
                          ?? DecodeLuminance(luminance, pureBarcode: false);
            return payload is not null
                ? new Result(payload, null)
                : new Result(null, $"no QR code detected on {width}×{height} RGBA pixels (pure + general).");
        }
        catch (Exception ex)
        {
            return new Result(null, $"decoder exception ({ex.GetType().Name}: {ex.Message})");
        }
    }

    private static string? DecodePixels(Image<Rgba32> image, bool pureBarcode)
    {
        var w = image.Width;
        var h = image.Height;
        var rgb = new byte[w * h * 3];

        image.ProcessPixelRows(accessor =>
        {
            var offset = 0;
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var p = row[x];
                    // Manual composite over white for any residual alpha < 255 (defensive; the
                    // BackgroundColor mutation above should have flattened everything already).
                    if (p.A < 255)
                    {
                        var a = p.A / 255f;
                        var inv = 1f - a;
                        rgb[offset++] = (byte)(p.R * a + 255 * inv);
                        rgb[offset++] = (byte)(p.G * a + 255 * inv);
                        rgb[offset++] = (byte)(p.B * a + 255 * inv);
                    }
                    else
                    {
                        rgb[offset++] = p.R;
                        rgb[offset++] = p.G;
                        rgb[offset++] = p.B;
                    }
                }
            }
        });

        var luminance = new RGBLuminanceSource(rgb, w, h, RGBLuminanceSource.BitmapFormat.RGB24);
        return DecodeLuminance(luminance, pureBarcode);
    }

    private static string? DecodeLuminance(LuminanceSource luminance, bool pureBarcode)
    {
        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = !pureBarcode,
            Options = new DecodingOptions
            {
                PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                TryHarder = !pureBarcode,
                TryInverted = !pureBarcode,
                PureBarcode = pureBarcode,
            },
        };

        try { return reader.Decode(luminance)?.Text; }
        catch { return null; }
    }
}
