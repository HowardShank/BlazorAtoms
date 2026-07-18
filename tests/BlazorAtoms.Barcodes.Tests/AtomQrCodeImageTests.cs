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
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCodeImage>(p => p.Add(x => x.Src, "https://ex.com/qr.png"));
        var img = cut.Find("img");
        Assert.Equal("https://ex.com/qr.png", img.GetAttribute("src"));
    }

    [Fact]
    public void Renders_img_with_data_uri_from_Bytes()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var cut = ctx.RenderComponent<AtomQrCodeImage>(p => p.Add(x => x.Bytes, bytes));
        var src = cut.Find("img").GetAttribute("src");
        Assert.StartsWith("data:image/png;base64,", src);
    }

    [Fact]
    public void Both_Src_and_Bytes_throws()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        Assert.Throws<InvalidOperationException>(() =>
            ctx.RenderComponent<AtomQrCodeImage>(p => p
                .Add(x => x.Src, "https://x/y.png")
                .Add(x => x.Bytes, new byte[] { 1, 2, 3 })));
    }

    [Fact]
    public void Explicit_Href_wraps_with_secure_rel()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCodeImage>(p => p
            .Add(x => x.Src, "https://x/y.png")
            .Add(x => x.Href, "https://target.example/"));
        var a = cut.Find("a.atom-qrcode-image-link");
        Assert.Equal("noopener noreferrer", a.GetAttribute("rel"));
        Assert.Equal("https://target.example/", a.GetAttribute("href"));
    }

    [Fact]
    public void No_anchor_when_neither_Href_nor_AutoLink()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCodeImage>(p => p.Add(x => x.Src, "https://x/y.png"));
        Assert.Empty(cut.FindAll("a.atom-qrcode-image-link"));
    }

    [Fact]
    public void No_decode_JS_call_when_AutoLink_off_and_no_OnDecoded()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.SetupModule(ModulePath);
        var cut = ctx.RenderComponent<AtomQrCodeImage>(p => p.Add(x => x.Src, "https://x/y.png"));
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
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<string>("fetchToBase64", _ => true).SetResult("");

        var cut = ctx.RenderComponent<AtomQrCodeImage>(p => p
            .Add(x => x.Src, "https://x/y.png")
            .Add(x => x.AutoLink, true));

        cut.WaitForAssertion(() => module.VerifyInvoke("fetchToBase64"));
    }

    [Fact]
    public void AutoLink_with_Bytes_never_calls_JS_fetch()
    {
        // Bytes already available in-process — no round-trip to the browser.
        using var ctx = new TestContext();
        ctx.JSInterop.SetupModule(ModulePath);

        var cut = ctx.RenderComponent<AtomQrCodeImage>(p => p
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
        using var ctx = new TestContext();
        ctx.JSInterop.SetupModule(ModulePath);
        var fired = false;
        string? seen = "sentinel";

        var cut = ctx.RenderComponent<AtomQrCodeImage>(p => p
            .Add(x => x.Bytes, new byte[] { 1, 2, 3, 4 })
            .Add(x => x.OnDecoded, v => { seen = v; fired = true; }));

        cut.WaitForAssertion(() => Assert.True(fired), TimeSpan.FromSeconds(10));
        Assert.Null(seen);
    }
}
