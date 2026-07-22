using System.Text.RegularExpressions;

namespace BlazorAtoms.Highlights.Tests;

public class AtomHighlightTests : BunitContext
{
    [Fact]
    public void Renders_Mark_For_SingleTerm()
    {
        var cut = Render<AtomHighlight>(parameters => parameters
            .Add(p => p.Term, "Blazor")
            .AddChildContent("Build with Blazor."));

        var mark = cut.Find("mark");
        Assert.Equal("Blazor", mark.TextContent);
        Assert.Contains("atom-highlight", mark.ClassName);
        Assert.Equal("mark", mark.GetAttribute("data-style"));
    }

    [Fact]
    public void CaseInsensitive_By_Default()
    {
        var cut = Render<AtomHighlight>(parameters => parameters
            .Add(p => p.Term, "blazor")
            .AddChildContent("Use Blazor today."));

        Assert.Equal("Blazor", cut.Find("mark").TextContent);
    }

    [Fact]
    public void CaseSensitive_Respects_Casing()
    {
        var cut = Render<AtomHighlight>(parameters => parameters
            .Add(p => p.Term, "Blazor")
            .Add(p => p.CaseSensitive, true)
            .AddChildContent("blazor only"));

        Assert.Empty(cut.FindAll("mark"));
    }

    [Fact]
    public void WholeWord_Skips_Substrings()
    {
        var cut = Render<AtomHighlight>(parameters => parameters
            .Add(p => p.Term, "team")
            .Add(p => p.WholeWord, true)
            .AddChildContent("teamwork is not team"));

        Assert.Single(cut.FindAll("mark"));
    }

    [Fact]
    public void Multiple_Terms_Highlight()
    {
        var cut = Render<AtomHighlight>(parameters => parameters
            .Add(p => p.Terms, new[] { "Blazor", "CSS" })
            .AddChildContent("Blazor plus CSS."));

        Assert.Equal(2, cut.FindAll("mark").Count);
    }

    [Fact]
    public void Underline_Style_Renders_Data_Attribute()
    {
        var cut = Render<AtomHighlight>(parameters => parameters
            .Add(p => p.Term, "x")
            .Add(p => p.HighlightStyle, HighlightStyle.Underline)
            .AddChildContent("x"));

        Assert.Equal("underline", cut.Find("mark").GetAttribute("data-style"));
    }

    [Fact]
    public void No_Mark_When_No_Term()
    {
        var cut = Render<AtomHighlight>(parameters => parameters
            .AddChildContent("nothing to see here"));

        Assert.Empty(cut.FindAll("mark"));
        Assert.Contains("nothing to see here", cut.Find(".atom-highlight-root").TextContent);
    }
}
