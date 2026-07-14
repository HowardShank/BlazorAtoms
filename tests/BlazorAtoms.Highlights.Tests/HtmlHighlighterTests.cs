using System.Text.RegularExpressions;

namespace BlazorAtoms.Highlights.Tests;

public class HtmlHighlighterTests
{
    private static Regex Rx(string pattern, RegexOptions options = RegexOptions.IgnoreCase) =>
        new(pattern, options);

    [Fact]
    public void Wraps_Matches_In_Text_Only()
    {
        var result = HtmlHighlighter.Highlight(
            "<p>Build with Blazor.</p>", Rx("Blazor"), "mark");

        Assert.Equal(
            "<p>Build with <mark class=\"atom-highlight\" data-style=\"mark\">Blazor</mark>.</p>",
            result);
    }

    [Fact]
    public void Preserves_Tags_And_Attributes()
    {
        var result = HtmlHighlighter.Highlight(
            "<a href='blazor.html' target='_blank'>Blazor</a>", Rx("blazor"), "mark");

        // Attribute value 'blazor.html' and target are untouched; only inner text wrapped.
        Assert.Contains("href='blazor.html'", result);
        Assert.Contains("target='_blank'", result);
        Assert.Contains("<mark class=\"atom-highlight\" data-style=\"mark\">Blazor</mark>", result);
    }

    [Fact]
    public void Does_Not_Highlight_Inside_Script_Or_Style()
    {
        var html = "<style>.blazor{color:red}</style><script>var blazor=1;</script><p>Blazor</p>";
        var result = HtmlHighlighter.Highlight(html, Rx("blazor"), "mark");

        Assert.Contains("<style>.blazor{color:red}</style>", result);
        Assert.Contains("var blazor=1;", result);
        Assert.Contains("<mark class=\"atom-highlight\" data-style=\"mark\">Blazor</mark>", result);
        // Exactly one mark (only the paragraph text).
        Assert.Single(Regex.Matches(result, "<mark "));
    }

    [Fact]
    public void Encodes_Special_Characters_In_Text()
    {
        var result = HtmlHighlighter.Highlight(
            "<p>a & b Blazor c</p>", Rx("Blazor"), "mark");

        Assert.Contains("a &amp; b", result);
    }

    [Fact]
    public void Leaves_Html_Comments_Untouched()
    {
        var result = HtmlHighlighter.Highlight(
            "<!-- Blazor comment --><p>Blazor</p>", Rx("Blazor"), "mark");

        Assert.Contains("<!-- Blazor comment -->", result);
        Assert.Single(Regex.Matches(result, "<mark "));
    }

    [Fact]
    public void Null_Regex_Returns_Input_Unchanged()
    {
        const string html = "<p>Blazor</p>";
        Assert.Equal(html, HtmlHighlighter.Highlight(html, null, "mark"));
    }

    [Fact]
    public void Empty_Input_Returns_Empty()
    {
        Assert.Equal(string.Empty, HtmlHighlighter.Highlight("", Rx("x"), "mark"));
        Assert.Equal(string.Empty, HtmlHighlighter.Highlight(null, Rx("x"), "mark"));
    }

    [Fact]
    public void Highlights_Multiple_Occurrences()
    {
        var result = HtmlHighlighter.Highlight(
            "<p>CSS and CSS</p>", Rx("CSS"), "mark");

        Assert.Equal(2, Regex.Matches(result, "<mark ").Count);
    }

    [Fact]
    public void Style_Value_Is_Written_To_Data_Style()
    {
        var result = HtmlHighlighter.Highlight(
            "<p>Blazor</p>", Rx("Blazor"), "underline");

        Assert.Contains("data-style=\"underline\"", result);
    }
}
