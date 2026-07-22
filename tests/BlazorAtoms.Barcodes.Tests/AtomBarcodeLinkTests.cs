namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// Mirror of <see cref="AtomQrCodeLinkTests"/> for the 1D barcode. Same shared BarcodeLink
/// helper drives both; these tests keep the razor wiring honest per component.
/// </summary>
public class AtomBarcodeLinkTests
{
    [Fact]
    public void No_anchor_by_default()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomBarcode>(p => p.Add(x => x.Value, "HELLO"));
        Assert.Empty(cut.FindAll("a.atom-barcode-link"));
    }

    [Fact]
    public void Explicit_Href_wraps_with_secure_defaults()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<AtomBarcode>(p => p
            .Add(x => x.Value, "HELLO")
            .Add(x => x.Href, "https://example.com/x"));
        var a = cut.Find("a.atom-barcode-link");
        Assert.Equal("noopener noreferrer", a.GetAttribute("rel"));
        Assert.Equal("_blank", a.GetAttribute("target"));
    }

    [Fact]
    public void AutoLink_derives_href_from_Value_when_url()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        // Code128 accepts ASCII 32-126 including the characters in a URL.
        var cut = ctx.Render<AtomBarcode>(p => p
            .Add(x => x.Value, "https://ex.com/x")
            .Add(x => x.AutoLink, true));
        Assert.Equal("https://ex.com/x", cut.Find("a.atom-barcode-link").GetAttribute("href"));
    }
}
