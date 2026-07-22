namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// Frame-shape + banner-position + effect coverage. Each param flip must reach the SVG.
/// </summary>
public class QrFrameRenderTests
{
    [Theory]
    [InlineData(FrameShape.Square, "<rect")]
    [InlineData(FrameShape.Rounded, "<rect")]
    [InlineData(FrameShape.Circle, "<circle")]
    [InlineData(FrameShape.DottedCircle, "stroke-dasharray")]
    [InlineData(FrameShape.DoubleCircle, "<circle")]
    [InlineData(FrameShape.Blob, "<path")]
    [InlineData(FrameShape.Torn, "<rect")]
    public void FrameShape_emits_expected_element(FrameShape shape, string mustContain)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.FrameShape, shape));
        Assert.Contains(mustContain, cut.Instance.GetSvg());
    }

    [Fact]
    public void No_frame_by_default_backwards_compatible()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomQrCode>(p => p.Add(x => x.Value, "hi"));
        // Default FrameShape.None → no filter defs, no banner text.
        var svg = cut.Instance.GetSvg();
        Assert.DoesNotContain("<filter", svg);
        Assert.DoesNotContain("SCAN ME", svg);
    }

    [Theory]
    [InlineData(FrameBanner.Bottom)]
    [InlineData(FrameBanner.BottomPointer)]
    [InlineData(FrameBanner.Top)]
    [InlineData(FrameBanner.BottomPill)]
    [InlineData(FrameBanner.Inline)]
    public void FrameBanner_emits_text(FrameBanner banner)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.FrameShape, FrameShape.Square)
            .Add(x => x.FrameBanner, banner)
            .Add(x => x.FrameText, "SCAN ME"));
        Assert.Contains("SCAN ME", cut.Instance.GetSvg());
    }

    [Fact]
    public void FrameShadow_emits_filter_def()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.FrameShape, FrameShape.Rounded)
            .Add(x => x.FrameShadow, true));
        var svg = cut.Instance.GetSvg();
        Assert.Contains("<feDropShadow", svg);
        Assert.Contains("filter=\"url(#ba-qr-shadow", svg);
    }

    [Fact]
    public void FrameInverted_fills_with_stroke_color()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomQrCode>(p => p
            .Add(x => x.Value, "hi")
            .Add(x => x.FrameShape, FrameShape.Rounded)
            .Add(x => x.FrameInverted, true)
            .Add(x => x.FrameStroke, "#123456"));
        Assert.Contains("fill=\"#123456\"", cut.Instance.GetSvg());
    }
}
