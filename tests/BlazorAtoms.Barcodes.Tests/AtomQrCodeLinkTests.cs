namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// bUnit coverage for the anchor-wrap contract on <see cref="AtomQrCode"/>. Locks in the
/// security defaults (rel="noopener noreferrer" always present) plus the resolve-precedence
/// rules the shared BarcodeLink helper enforces.
/// </summary>
public class AtomQrCodeLinkTests
{
    [Fact]
    public void No_anchor_when_neither_Href_nor_AutoLink_set()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p.Add(x => x.Value, "https://example.com/"));
        Assert.Empty(cut.FindAll("a.atom-qrcode-link"));
    }

    [Fact]
    public void Explicit_Href_renders_anchor_with_target_and_secure_rel()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "hello")
            .Add(x => x.Href, "https://example.com/x"));

        var a = cut.Find("a.atom-qrcode-link");
        Assert.Equal("https://example.com/x", a.GetAttribute("href"));
        Assert.Equal("_blank", a.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", a.GetAttribute("rel"));
    }

    [Fact]
    public void AutoLink_wraps_when_Value_is_url()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "https://example.com/x")
            .Add(x => x.AutoLink, true));
        Assert.Equal("https://example.com/x", cut.Find("a.atom-qrcode-link").GetAttribute("href"));
    }

    [Fact]
    public void AutoLink_ignores_non_url_Value()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "not a url")
            .Add(x => x.AutoLink, true));
        Assert.Empty(cut.FindAll("a.atom-qrcode-link"));
    }

    [Fact]
    public void LinkTarget_flows_to_anchor()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.RenderComponent<AtomQrCode>(p => p
            .Add(x => x.Value, "x")
            .Add(x => x.Href, "https://example.com/")
            .Add(x => x.LinkTarget, "_self"));
        Assert.Equal("_self", cut.Find("a.atom-qrcode-link").GetAttribute("target"));
    }
}
