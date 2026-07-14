namespace BlazorAtoms.Highlights.Tests;

public class AtomHighlightDeepTests : TestContext
{
    [Fact]
    public void Highlights_Text_Inside_Markup()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "Blazor")
            .Add(p => p.Html, "<p>Build with <strong>Blazor</strong> today.</p>"));

        var root = cut.Find(".atom-highlight-root");
        Assert.Equal("Highlighted content", root.GetAttribute("aria-label"));

        var marks = cut.FindAll("mark.atom-highlight");
        Assert.Single(marks);
        Assert.Equal("Blazor", marks[0].TextContent);
        Assert.Equal("mark", marks[0].GetAttribute("data-style"));

        // Surrounding markup is preserved.
        Assert.Contains("<strong>", root.InnerHtml);
    }

    [Fact]
    public void Highlights_All_Occurrences_Across_Elements()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "CSS")
            .Add(p => p.Html, "<h4>CSS Example</h4><ul><li>CSS isolation</li><li>Modern CSS</li></ul>"));

        var marks = cut.FindAll("mark.atom-highlight");
        Assert.Equal(3, marks.Count);
        Assert.All(marks, m => Assert.Equal("CSS", m.TextContent));
    }

    [Fact]
    public void Multiple_Terms_Are_Highlighted()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Terms, new[] { "Blazor", "CSS" })
            .Add(p => p.Html, "<p>Blazor and CSS together.</p>"));

        var marks = cut.FindAll("mark.atom-highlight");
        Assert.Equal(2, marks.Count);
    }

    [Fact]
    public void Case_Insensitive_By_Default()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "blazor")
            .Add(p => p.Html, "<p>Blazor and BLAZOR.</p>"));

        var marks = cut.FindAll("mark.atom-highlight");
        Assert.Equal(2, marks.Count);
    }

    [Fact]
    public void Case_Sensitive_Respects_Casing()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "Blazor")
            .Add(p => p.CaseSensitive, true)
            .Add(p => p.Html, "<p>Blazor and blazor.</p>"));

        var marks = cut.FindAll("mark.atom-highlight");
        Assert.Single(marks);
        Assert.Equal("Blazor", marks[0].TextContent);
    }

    [Fact]
    public void WholeWord_Does_Not_Match_Substrings()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "b")
            .Add(p => p.WholeWord, true)
            .Add(p => p.Html, "<p>a button with b inside.</p>"));

        var marks = cut.FindAll("mark.atom-highlight");
        Assert.Single(marks);
        Assert.Equal("b", marks[0].TextContent);
    }

    [Fact]
    public void Does_Not_Highlight_Inside_Tags_Or_Attributes()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "target")
            .Add(p => p.Html, "<a href='x' target='_blank'>link target here</a>"));

        // Only the visible text 'target' should be wrapped, not the attribute name.
        var marks = cut.FindAll("mark.atom-highlight");
        Assert.Single(marks);
        Assert.Equal("target", marks[0].TextContent);

        var anchor = cut.Find("a");
        Assert.Equal("_blank", anchor.GetAttribute("target"));
    }

    [Fact]
    public void Empty_Terms_Render_Original_Markup_Unchanged()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Html, "<p>Nothing to highlight.</p>"));

        Assert.Empty(cut.FindAll("mark.atom-highlight"));
        Assert.Contains("Nothing to highlight.", cut.Find(".atom-highlight-root").InnerHtml);
    }

    [Fact]
    public void Re_Highlights_Correctly_When_Terms_Change()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "CSS")
            .Add(p => p.Html, "<p>Blazor and CSS and a button.</p>"));

        Assert.Single(cut.FindAll("mark.atom-highlight"));

        // Simulate the playground: change the term repeatedly.
        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Term, "Blazor"));
        var marks = cut.FindAll("mark.atom-highlight");
        Assert.Single(marks);
        Assert.Equal("Blazor", marks[0].TextContent);

        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Term, "b"));
        // "b" is a substring: Blazor, button -> 2 matches (case-insensitive).
        Assert.Equal(2, cut.FindAll("mark.atom-highlight").Count);

        cut.SetParametersAndRender(parameters => parameters.Add(p => p.Term, "and"));
        Assert.Equal(2, cut.FindAll("mark.atom-highlight").Count);
    }
}
