using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// Locks in the SVG shape emitted per styling parameter. These aren't decode roundtrips (see
/// <see cref="QrStyleRoundtripTests"/> for those); they verify that flipping a parameter
/// actually flows through to the markup so future refactors can't silently drop a feature.
/// </summary>
public class QrStyleRenderTests
{
    [Fact]
    public void Default_still_emits_single_path_backwards_compatible()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p.Add(x => x.Value, "hi"));
        var svg = cut.Instance.GetSvg();
        Assert.Contains("<path", svg);
        Assert.Contains("fill=\"#000000\"", svg);
    }

    [Theory]
    [InlineData(ModuleShape.Square)]
    [InlineData(ModuleShape.Rounded)]
    [InlineData(ModuleShape.Dot)]
    [InlineData(ModuleShape.Ellipse)]
    [InlineData(ModuleShape.Diamond)]
    [InlineData(ModuleShape.Star)]
    [InlineData(ModuleShape.Pill)]
    [InlineData(ModuleShape.Blob)]
    public void ModuleShape_produces_svg(ModuleShape shape)
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hello")
            .Add(x => x.ModuleShape, shape));
        var svg = cut.Instance.GetSvg();
        Assert.StartsWith("<svg", svg);
        Assert.Contains("<path", svg);
    }

    [Theory]
    [InlineData(EyeFrame.Square)]
    [InlineData(EyeFrame.Circle)]
    [InlineData(EyeFrame.Rounded)]
    public void EyeFrame_renders(EyeFrame frame)
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.EyeFrame, frame));
        Assert.StartsWith("<svg", cut.Instance.GetSvg());
    }

    [Theory]
    [InlineData(EyePupil.Square)]
    [InlineData(EyePupil.Circle)]
    [InlineData(EyePupil.Rounded)]
    [InlineData(EyePupil.Rhombus)]
    public void EyePupil_renders(EyePupil pupil)
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.EyePupil, pupil));
        Assert.StartsWith("<svg", cut.Instance.GetSvg());
    }

    [Fact]
    public void EyeColor_overrides_foreground_fill()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.EyeColor, "#ff0000"));
        Assert.Contains("#ff0000", cut.Instance.GetSvg());
    }

    [Fact]
    public void LinearGradient_emits_defs()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.ForegroundStyle, FillStyle.LinearGradient)
            .Add(x => x.ForegroundGradientFrom, "#054080")
            .Add(x => x.ForegroundGradientTo, "#f30505"));
        var svg = cut.Instance.GetSvg();
        Assert.Contains("<linearGradient", svg);
        Assert.Contains("#054080", svg);
        Assert.Contains("#f30505", svg);
        Assert.Contains("url(#ba-qr-fg-", svg);
    }

    [Fact]
    public void RadialGradient_emits_defs()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.ForegroundStyle, FillStyle.RadialGradient)
            .Add(x => x.ForegroundGradientFrom, "#000000")
            .Add(x => x.ForegroundGradientTo, "#0000ff"));
        Assert.Contains("<radialGradient", cut.Instance.GetSvg());
    }

    [Fact]
    public void Logo_emits_image_element()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.EcLevel, QrErrorCorrection.H)
            .Add(x => x.LogoSrc, "https://example.com/logo.png"));
        var svg = cut.Instance.GetSvg();
        Assert.Contains("<image", svg);
        Assert.Contains("https://example.com/logo.png", svg);
    }

    [Fact]
    public void LogoBytes_serializes_data_uri()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.LogoBytes, bytes));
        Assert.Contains("data:image/png;base64,iVBOR", cut.Instance.GetSvg());
    }
}
