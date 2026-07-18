using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// Mirrors <see cref="BarcodeExportTests"/> for AtomQrCode. Also verifies the QR-specific
/// default: when PngPixelWidth is null, rasterization uses Size * 4 for print-crispness.
/// </summary>
public class QrCodeExportTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Barcodes/atom-barcode.js";

    [Fact]
    public void GetSvg_returns_current_markup()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p.Add(x => x.Value, "hello"));
        Assert.StartsWith("<svg", cut.Instance.GetSvg());
    }

    [Fact]
    public async Task CopyAsync_Svg_calls_copyText()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("copyText", _ => true).SetVoidResult();

        var cut = ctx.RenderComponent<AtomQrCode>(p => p.Add(x => x.Value, "hello"));
        await cut.Instance.CopyAsync(BarcodeExportFormat.Svg);

        module.VerifyInvoke("copyText");
    }

    [Fact]
    public async Task CopyAsync_Png_uses_size_times_four_when_PngPixelWidth_null()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("svgToPngClipboard", _ => true).SetVoidResult();

        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hello")
            .Add(x => x.Size, 200));

        await cut.Instance.CopyAsync(BarcodeExportFormat.Png);

        var call = module.VerifyInvoke("svgToPngClipboard");
        Assert.Equal(800, call.Arguments[1]);
    }

    [Fact]
    public async Task SaveAsync_Png_forwards_filename()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("svgToPngSave", _ => true).SetVoidResult();

        var cut = ctx.RenderComponent<AtomQrCode>(p => p.Add(x => x.Value, "hello"));

        await cut.Instance.SaveAsync(BarcodeExportFormat.Png, "qr.png");
        var call = module.VerifyInvoke("svgToPngSave");
        Assert.Equal("qr.png", call.Arguments[2]);
    }

    [Fact]
    public async Task SaveAsync_Svg_forwards_mime_and_filename()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("saveText", _ => true).SetVoidResult();

        var cut = ctx.RenderComponent<AtomQrCode>(p => p.Add(x => x.Value, "hello"));

        await cut.Instance.SaveAsync(BarcodeExportFormat.Svg, "qr.svg");
        var call = module.VerifyInvoke("saveText");
        Assert.Equal("image/svg+xml;charset=utf-8", call.Arguments[1]);
        Assert.Equal("qr.svg", call.Arguments[2]);
    }

    [Fact]
    public async Task CopyAsync_wraps_JSDisconnectedException_as_BarcodeExportException()
    {
        using var ctx = new TestContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("copyText", _ => true).SetException(new JSDisconnectedException("circuit gone"));

        var cut = ctx.RenderComponent<AtomQrCode>(p => p.Add(x => x.Value, "hello"));

        var ex = await Assert.ThrowsAsync<BarcodeExportException>(
            async () => await cut.Instance.CopyAsync(BarcodeExportFormat.Svg));
        Assert.IsType<JSDisconnectedException>(ex.InnerException);
    }

    [Fact]
    public async Task Dispose_is_idempotent()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p.Add(x => x.Value, "hello"));

        await cut.Instance.DisposeAsync();
        await cut.Instance.DisposeAsync();
    }
}
