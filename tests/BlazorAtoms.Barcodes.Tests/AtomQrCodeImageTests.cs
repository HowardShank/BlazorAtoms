using Microsoft.JSInterop;

namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// bUnit coverage for <see cref="AtomQrCodeImage"/> — the viewer component. Covers the input
/// contract (Src OR Bytes but not both), source resolution, explicit link wrap, browser-decode
/// AutoLink flow (mocked via JSInterop), and the OnDecoded callback fan-out.
/// </summary>
public class AtomQrCodeImageTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Barcodes/atom-barcode.js";

    [Fact]
    public void Renders_img_with_supplied_Src()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomQrCodeImage>(p => p.Add(x => x.Src, "https://ex.com/qr.png"));
        var img = cut.Find("img");
        Assert.Equal("https://ex.com/qr.png", img.GetAttribute("src"));
    }

    [Fact]
    public void Renders_img_with_data_uri_from_Bytes()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var cut = ctx.Render<AtomQrCodeImage>(p => p.Add(x => x.Bytes, bytes));
        var src = cut.Find("img").GetAttribute("src");
        Assert.StartsWith("data:image/png;base64,", src);
    }

    [Fact]
    public void Both_Src_and_Bytes_throws()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Assert.Throws<InvalidOperationException>(() =>
            ctx.Render<AtomQrCodeImage>(p => p
                .Add(x => x.Src, "https://x/y.png")
                .Add(x => x.Bytes, new byte[] { 1, 2, 3 })));
    }

    [Fact]
    public void Explicit_Href_wraps_with_secure_rel()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomQrCodeImage>(p => p
            .Add(x => x.Src, "https://x/y.png")
            .Add(x => x.Href, "https://target.example/"));
        var a = cut.Find("a.atom-qrcode-image-link");
        Assert.Equal("noopener noreferrer", a.GetAttribute("rel"));
        Assert.Equal("https://target.example/", a.GetAttribute("href"));
    }

    [Fact]
    public void No_anchor_when_neither_Href_nor_AutoLink()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomQrCodeImage>(p => p.Add(x => x.Src, "https://x/y.png"));
        Assert.Empty(cut.FindAll("a.atom-qrcode-image-link"));
    }

    [Fact]
    public void No_decode_JS_call_when_AutoLink_off_and_no_OnDecoded()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.Render<AtomQrCodeImage>(p => p.Add(x => x.Src, "https://x/y.png"));
        // Component should never call fetchToBase64 nor any decode-related JS when neither
        // AutoLink is on nor OnDecoded is wired.
        Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "fetchToBase64");
    }

    [Fact]
    public void AutoLink_with_Src_triggers_fetchToBase64_for_pixels()
    {
        // With Src (not Bytes), the component asks JS to fetch the bytes; then decodes in C#.
        // We don't drive a real QR through the ImageSharp/ZXing path here (empty bytes) — this
        // test just verifies the fetch handshake happens.
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<string>("fetchToBase64", _ => true).SetResult("");

        var cut = ctx.Render<AtomQrCodeImage>(p => p
            .Add(x => x.Src, "https://x/y.png")
            .Add(x => x.AutoLink, true));

        cut.WaitForAssertion(() => module.VerifyInvoke("fetchToBase64"));
    }

    [Fact]
    public void AutoLink_with_Bytes_never_calls_JS_fetch()
    {
        // Bytes already available in-process — no round-trip to the browser.
        using var ctx = new BunitContext();
        ctx.JSInterop.SetupModule(ModulePath);

        var cut = ctx.Render<AtomQrCodeImage>(p => p
            .Add(x => x.Bytes, new byte[] { 0x00, 0x01 })
            .Add(x => x.AutoLink, true));

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "fetchToBase64"));
    }

    [Fact]
    public void OnDecoded_fires_with_null_when_bytes_are_not_a_valid_QR()
    {
        // Garbage bytes → ImageSharp throws → QrImageDecoder returns null → OnDecoded fires with null.
        // Decoder runs on Task.Run so give the bUnit poll a generous window.
        using var ctx = new BunitContext();
        ctx.JSInterop.SetupModule(ModulePath);
        var fired = false;
        string? seen = "sentinel";

        var cut = ctx.Render<AtomQrCodeImage>(p => p
            .Add(x => x.Bytes, new byte[] { 1, 2, 3, 4 })
            .Add(x => x.OnDecoded, v => { seen = v; fired = true; }));

        cut.WaitForAssertion(() => Assert.True(fired), TimeSpan.FromSeconds(10));
        Assert.Null(seen);
    }

    // ---- RGBA pixel decode (the WASM hot path — QrImageDecoder.TryDecodeRgba) -------------------

    // Build a black/white RGBA buffer from a ZXing-encoded QR — mirrors what the browser <canvas>
    // getImageData hands back (alpha already flattened to 255). No ImageSharp, no browser needed.
    private static byte[] EncodeQrToRgba(string payload, out int width, out int height)
    {
        var writer = new ZXing.BarcodeWriterGeneric
        {
            Format = ZXing.BarcodeFormat.QR_CODE,
            Options = new ZXing.Common.EncodingOptions { Width = 250, Height = 250, Margin = 4 },
        };
        var matrix = writer.Encode(payload);
        width = matrix.Width;
        height = matrix.Height;
        var rgba = new byte[width * height * 4];
        var o = 0;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var v = matrix[x, y] ? (byte)0 : (byte)255; // black module vs white
                rgba[o++] = v; rgba[o++] = v; rgba[o++] = v; rgba[o++] = 255;
            }
        return rgba;
    }

    [Fact]
    public void TryDecodeRgba_round_trips_a_generated_qr()
    {
        const string payload = "https://example.com/atom";
        var rgba = EncodeQrToRgba(payload, out var w, out var h);
        var result = QrImageDecoder.TryDecodeRgba(rgba, w, h);
        Assert.Equal(payload, result.Payload);
        Assert.Null(result.Diagnostic);
    }

    [Fact]
    public void TryDecodeRgba_returns_diagnostic_on_blank_pixels()
    {
        var rgba = new byte[16 * 16 * 4];
        Array.Fill(rgba, (byte)255); // all white → no QR
        var result = QrImageDecoder.TryDecodeRgba(rgba, 16, 16);
        Assert.Null(result.Payload);
        Assert.NotNull(result.Diagnostic);
    }

    [Fact]
    public void TryDecodeRgba_guards_undersized_buffer()
    {
        var result = QrImageDecoder.TryDecodeRgba(new byte[10], 100, 100);
        Assert.Null(result.Payload);
        Assert.Contains("too small", result.Diagnostic);
    }
}
