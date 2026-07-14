using System.Text.Json;

namespace BlazorAtoms.Highlights.Tests;

public class AtomHighlightDeepTests : TestContext
{
    public AtomHighlightDeepTests()
    {
        var module = JSInterop.SetupModule("./_content/BlazorAtoms.Highlights/atom-highlight-deep.js");
        module.SetupVoid("highlight", _ => true).SetVoidResult();
        module.SetupVoid("clear", _ => true).SetVoidResult();
    }

    [Fact]
    public void Renders_Child_Content_And_Data_Attributes()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "Blazor")
            .AddChildContent("<p>Build with Blazor.</p>"));

        var root = cut.Find(".atom-highlight-root");
        Assert.Equal("Highlighted content", root.GetAttribute("aria-label"));
        Assert.Equal("mark", root.GetAttribute("data-highlight-style"));
        Assert.Contains("Build with Blazor.", root.InnerHtml);

        var termsRaw = root.GetAttribute("data-highlight-terms");
        Assert.NotNull(termsRaw);
        var terms = JsonSerializer.Deserialize<string[]>(termsRaw);
        Assert.NotNull(terms);
        Assert.Equal(["Blazor"], terms);
    }

    [Fact]
    public void Options_Reflect_Case_And_WholeWord()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Term, "test")
            .Add(p => p.CaseSensitive, true)
            .Add(p => p.WholeWord, true));

        var options = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            cut.Find(".atom-highlight-root").GetAttribute("data-highlight-options")!);

        Assert.True(options!["caseSensitive"]);
        Assert.True(options["wholeWord"]);
    }

    [Fact]
    public void Multiple_Terms_Serialized()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .Add(p => p.Terms, new[] { "Blazor", "C#" }));

        var root = cut.Find(".atom-highlight-root");
        var termsRaw = root.GetAttribute("data-highlight-terms");
        Assert.NotNull(termsRaw);
        var terms = JsonSerializer.Deserialize<string[]>(termsRaw);
        Assert.NotNull(terms);
        Assert.Equal(["Blazor", "C#"], terms);
    }

    [Fact]
    public void Empty_Term_List_Skips_Config_Attributes()
    {
        var cut = RenderComponent<AtomHighlightDeep>(parameters => parameters
            .AddChildContent("<p>Nothing.</p>"));

        Assert.Null(cut.Find(".atom-highlight-root").GetAttribute("data-highlight-terms"));
    }
}
