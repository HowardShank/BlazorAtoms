using System.Reflection;

namespace BlazorAtoms.Barcodes.Tests;

/// <summary>
/// Pure-logic tests for the internal <c>BarcodeLink.TryResolveUrl</c> helper. The type is
/// internal so tests reach it via reflection; that also guards against accidental re-exposure
/// of the API surface (renaming the method here would break these tests loudly).
/// </summary>
public class BarcodeLinkTests
{
    private static bool Try(string? href, string? candidate, bool autoLink, out string url)
    {
        var t = typeof(BarcodeExportFormat).Assembly.GetType("BlazorAtoms.Barcodes.BarcodeLink")!;
        var m = t.GetMethod("TryResolveUrl", BindingFlags.Public | BindingFlags.Static)!;
        var args = new object?[] { href, candidate, autoLink, null };
        var ok = (bool)m.Invoke(null, args)!;
        url = (string)args[3]!;
        return ok;
    }

    [Fact]
    public void Explicit_Href_wins_over_AutoLink()
    {
        Assert.True(Try("https://explicit.example/", "http://ignored/", autoLink: true, out var url));
        Assert.Equal("https://explicit.example/", url);
    }

    [Fact]
    public void Explicit_Href_wins_even_when_AutoLink_off()
    {
        Assert.True(Try("mailto:foo@bar", null, autoLink: false, out var url));
        Assert.Equal("mailto:foo@bar", url);
    }

    [Theory]
    [InlineData("https://example.com/x")]
    [InlineData("http://example.com/x")]
    [InlineData("mailto:foo@example.com")]
    [InlineData("tel:+15551234567")]
    public void AutoLink_accepts_safe_schemes(string candidate)
    {
        Assert.True(Try(null, candidate, autoLink: true, out var url));
        Assert.NotEmpty(url);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/")]
    public void AutoLink_rejects_dangerous_or_unknown_schemes(string candidate)
    {
        Assert.False(Try(null, candidate, autoLink: true, out var url));
        Assert.Equal(string.Empty, url);
    }

    [Fact]
    public void AutoLink_off_never_resolves_a_url()
    {
        Assert.False(Try(null, "https://example.com/", autoLink: false, out _));
    }

    [Fact]
    public void Null_or_whitespace_candidate_returns_false()
    {
        Assert.False(Try(null, null, autoLink: true, out _));
        Assert.False(Try(null, "", autoLink: true, out _));
        Assert.False(Try(null, "   ", autoLink: true, out _));
    }

    [Fact]
    public void Malformed_string_returns_false()
    {
        Assert.False(Try(null, "not a url", autoLink: true, out _));
    }

    [Fact]
    public void Whitespace_Href_falls_through_to_AutoLink()
    {
        Assert.True(Try("   ", "https://example.com/", autoLink: true, out var url));
        Assert.Equal("https://example.com/", url);
    }
}
