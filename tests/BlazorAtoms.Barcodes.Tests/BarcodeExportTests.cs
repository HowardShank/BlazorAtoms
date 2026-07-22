using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// bUnit-level coverage for AtomBarcode's Copy/Save/GetPngBytes surface. Uses bUnit's JS interop
/// mock to catch call shape drift (module path, function names, arg positions). The lazy import
/// happens the first time any of the export methods is called — none of these tests need to wait
/// on OnAfterRender since import fires on-demand.
/// </summary>
public class BarcodeExportTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Barcodes/atom-barcode.js";

    [Fact]
    public void GetSvg_returns_current_markup()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<AtomBarcode>(p => p.Add(x => x.Value, "HELLO"));

        var svg = cut.Instance.GetSvg();
        Assert.StartsWith("<svg", svg);
        Assert.Contains("Barcode: HELLO", svg);
    }

    [Fact]
    public async Task CopyAsync_Svg_calls_copyText_with_svg_string()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("copyText", _ => true).SetVoidResult();

        var cut = ctx.Render<AtomBarcode>(p => p.Add(x => x.Value, "HELLO"));

        await cut.Instance.CopyAsync(BarcodeExportFormat.Svg);

        var call = module.VerifyInvoke("copyText");
        Assert.Single(call.Arguments);
        Assert.Contains("<svg", (string?)call.Arguments[0] ?? "");
    }

    [Fact]
    public async Task CopyAsync_Png_calls_svgToPngClipboard_single_shot()
    {
        // PNG copy is a single JS call — SVG in, clipboard write happens entirely browser-side to
        // avoid crossing the SignalR message-size limit with the PNG payload.
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("svgToPngClipboard", _ => true).SetVoidResult();

        var cut = ctx.Render<AtomBarcode>(p => p.Add(x => x.Value, "HELLO"));

        await cut.Instance.CopyAsync(BarcodeExportFormat.Png);

        var call = module.VerifyInvoke("svgToPngClipboard");
        Assert.Contains("<svg", (string?)call.Arguments[0] ?? "");
    }

    [Fact]
    public async Task SaveAsync_Svg_calls_saveText_with_filename_and_svg_mime()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("saveText", _ => true).SetVoidResult();

        var cut = ctx.Render<AtomBarcode>(p => p.Add(x => x.Value, "HELLO"));

        await cut.Instance.SaveAsync(BarcodeExportFormat.Svg, "barcode.svg");

        var call = module.VerifyInvoke("saveText");
        Assert.Contains("<svg", (string?)call.Arguments[0] ?? "");
        Assert.Equal("image/svg+xml;charset=utf-8", call.Arguments[1]);
        Assert.Equal("barcode.svg", call.Arguments[2]);
    }

    [Fact]
    public async Task SaveAsync_Png_calls_svgToPngSave_single_shot()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("svgToPngSave", _ => true).SetVoidResult();

        var cut = ctx.Render<AtomBarcode>(p => p.Add(x => x.Value, "HELLO"));

        await cut.Instance.SaveAsync(BarcodeExportFormat.Png, "barcode.png");

        var call = module.VerifyInvoke("svgToPngSave");
        Assert.Contains("<svg", (string?)call.Arguments[0] ?? "");
        Assert.Equal("barcode.png", call.Arguments[2]);
    }

    [Fact]
    public async Task PngPixelWidth_flows_to_JS_as_second_arg_of_svgToPngSave()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("svgToPngSave", _ => true).SetVoidResult();

        var cut = ctx.Render<AtomBarcode>(p => p
            .Add(x => x.Value, "HELLO")
            .Add(x => x.PngPixelWidth, 800));

        await cut.Instance.SaveAsync(BarcodeExportFormat.Png, "b.png");

        var call = module.VerifyInvoke("svgToPngSave");
        Assert.Equal(800, call.Arguments[1]);
    }

    [Fact]
    public async Task CopyAsync_wraps_JSException_as_BarcodeExportException()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("copyText", _ => true).SetException(new JSException("Clipboard denied"));

        var cut = ctx.Render<AtomBarcode>(p => p.Add(x => x.Value, "HELLO"));

        var ex = await Assert.ThrowsAsync<BarcodeExportException>(
            async () => await cut.Instance.CopyAsync(BarcodeExportFormat.Svg));
        Assert.IsType<JSException>(ex.InnerException);
    }

    [Fact]
    public async Task SaveAsync_empty_filename_throws_ArgumentException()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomBarcode>(p => p.Add(x => x.Value, "HELLO"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await cut.Instance.SaveAsync(BarcodeExportFormat.Svg, "   "));
    }
}
