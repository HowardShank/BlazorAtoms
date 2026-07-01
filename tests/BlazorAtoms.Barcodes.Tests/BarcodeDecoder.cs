using System.Collections.Generic;
using ZXing;
using ZXing.Common;

namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// Test-only verification: rasterize an encoder's module pattern into a clean 1-bit-per-module
/// grayscale image and decode it with ZXing.Net. If it round-trips to the original value, the
/// encoder (and its pattern tables) are correct. Keeps the shipped library dependency-free —
/// ZXing lives only here.
/// </summary>
internal static class BarcodeDecoder
{
    public static string? Decode(bool[] modules, BarcodeFormat format, int scale = 4, int quiet = 12)
    {
        var totalMods = modules.Length + quiet * 2;
        var w = totalMods * scale;
        const int h = 40;

        var gray = new byte[w * h];
        for (var k = 0; k < gray.Length; k++) gray[k] = 255; // white background

        for (var m = 0; m < modules.Length; m++)
        {
            if (!modules[m]) continue;
            var xStart = (quiet + m) * scale;
            for (var p = 0; p < scale; p++)
            {
                var x = xStart + p;
                for (var y = 0; y < h; y++) gray[y * w + x] = 0; // dark bar
            }
        }

        var source = new RGBLuminanceSource(gray, w, h, RGBLuminanceSource.BitmapFormat.Gray8);
        var reader = new BarcodeReaderGeneric
        {
            Options = new DecodingOptions
            {
                PureBarcode = true,
                PossibleFormats = new List<BarcodeFormat> { format },
            },
        };

        return reader.Decode(source)?.Text;
    }
}
