using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorAtoms.Highlights.Tests;

// AtomHighlighter highlights the live DOM via a self-imported JS module, which bUnit can't run for
// real. We assert the JS-interop contract (module import + the highlightTextInElement call and its
// arguments) and prove the component doesn't care about nesting depth by rendering an actual
// Grandparent -> Parent -> Child component chain as its ChildContent.
public class AtomHighlighterTests : TestContext
{
    public AtomHighlighterTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private const string ModulePath = "./_content/BlazorAtoms.Highlights/atom-highlighter.js";

    [Fact]
    public void Imports_its_own_js_module_on_first_render()
    {
        RenderComponent<AtomHighlighter>(p => p.AddChildContent("hello"));

        Assert.Contains(JSInterop.Invocations, i =>
            i.Identifier == "import" &&
            i.Arguments.Count > 0 &&
            (i.Arguments[0] as string) == ModulePath);
    }

    [Fact]
    public void Calls_highlightTextInElement_after_render_with_keywords_and_class()
    {
        JSInterop.SetupModule(ModulePath);
        var keywords = new[] { "Blazor", "C#" };

        RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Keywords, keywords)
            .Add(c => c.HighlightClass, "my-mark")
            .AddChildContent("Build with Blazor."));

        var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "highlightTextInElement");
        Assert.Equal(keywords, call.Arguments[1] as string[]);
        Assert.Equal("my-mark", call.Arguments[2] as string);
    }

    [Fact]
    public void Still_calls_highlight_with_empty_keywords_so_js_can_clear_stale_marks()
    {
        // The JS side unmarks its own previous matches before checking whether there's anything
        // to (re)highlight — so the call must still reach it when Keywords goes empty, or marks
        // left over from a previous non-empty Keywords value would never get cleaned up.
        JSInterop.SetupModule(ModulePath);

        RenderComponent<AtomHighlighter>(p => p.AddChildContent("Build with Blazor."));

        var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "highlightTextInElement");
        Assert.Empty(call.Arguments[1] as string[] ?? []);
    }

    [Fact]
    public void Renders_default_highlight_class_when_unset()
    {
        JSInterop.SetupModule(ModulePath);

        RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Keywords, new[] { "Blazor" })
            .AddChildContent("Build with Blazor."));

        var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "highlightTextInElement");
        Assert.Equal("atom-highlighter", call.Arguments[2] as string);
    }

    // The options arg is an anonymous type (not nameable from this assembly) — read its
    // properties via reflection on the runtime instance instead.
    private static T ReadOption<T>(object options, string name) =>
        (T)options.GetType().GetProperty(name)!.GetValue(options)!;

    [Theory]
    [InlineData(HighlightStyle.Mark, "mark")]
    [InlineData(HighlightStyle.Underline, "underline")]
    [InlineData(HighlightStyle.Outline, "outline")]
    public void HighlightStyle_maps_to_the_style_option(HighlightStyle style, string expected)
    {
        JSInterop.SetupModule(ModulePath);

        RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Keywords, new[] { "Blazor" })
            .Add(c => c.HighlightStyle, style)
            .AddChildContent("Build with Blazor."));

        var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "highlightTextInElement");
        Assert.Equal(expected, ReadOption<string>(call.Arguments[3]!, "style"));
    }

    [Fact]
    public void CaseSensitive_and_WholeWord_are_passed_through_as_options()
    {
        JSInterop.SetupModule(ModulePath);

        RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Keywords, new[] { "Blazor" })
            .Add(c => c.CaseSensitive, true)
            .Add(c => c.WholeWord, true)
            .AddChildContent("Build with Blazor."));

        var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "highlightTextInElement");
        Assert.True(ReadOption<bool>(call.Arguments[3]!, "caseSensitive"));
        Assert.True(ReadOption<bool>(call.Arguments[3]!, "wholeWord"));
    }

    [Fact]
    public void Two_instances_get_distinct_owner_ids_even_with_the_same_default_class()
    {
        // Guards against a nested-instance mark collision: if two AtomHighlighter instances ever
        // shared an owner id (e.g. a static counter reset, or no id at all), an outer instance's
        // unmark() could strip an inner instance's marks just because they share HighlightClass.
        JSInterop.SetupModule(ModulePath);

        RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Keywords, new[] { "Blazor" })
            .AddChildContent("Build with Blazor."));
        RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Keywords, new[] { "Blazor" })
            .AddChildContent("Build with Blazor."));

        // bUnit renders synchronously, so each RenderComponent call above fully completes
        // (including its own OnAfterRenderAsync) before the next one starts — invocation order
        // reflects render order, one per instance.
        var calls = JSInterop.Invocations.Where(i => i.Identifier == "highlightTextInElement").ToList();
        Assert.Equal(2, calls.Count);

        var ownerA = ReadOption<string>(calls[0].Arguments[3]!, "owner");
        var ownerB = ReadOption<string>(calls[1].Arguments[3]!, "owner");
        Assert.NotEmpty(ownerA);
        Assert.NotEmpty(ownerB);
        Assert.NotEqual(ownerA, ownerB);
    }

    [Fact]
    public void Owner_id_stays_stable_across_re_renders_of_the_same_instance()
    {
        // The owner id must NOT be regenerated per render — unmark() relies on it staying constant
        // across a given instance's lifetime to find and clean up that instance's own previous marks.
        JSInterop.SetupModule(ModulePath);

        var cut = RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Keywords, new[] { "Blazor" })
            .AddChildContent("Build with Blazor."));
        var firstOwner = ReadOption<string>(
            JSInterop.Invocations.Last(i => i.Identifier == "highlightTextInElement").Arguments[3]!, "owner");

        cut.SetParametersAndRender(p => p.Add(c => c.Keywords, new[] { "Blazor", "C#" }));
        var secondOwner = ReadOption<string>(
            JSInterop.Invocations.Last(i => i.Identifier == "highlightTextInElement").Arguments[3]!, "owner");

        Assert.Equal(firstOwner, secondOwner);
    }

    [Fact]
    public void Background_Color_Radius_Padding_emit_css_custom_properties_on_the_container()
    {
        JSInterop.SetupModule(ModulePath);

        var cut = RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Background, "#112233")
            .Add(c => c.Color, "#ffffff")
            .Add(c => c.Radius, 4)
            .Add(c => c.Padding, "0 4px")
            .AddChildContent("Build with Blazor."));

        var style = cut.Find("div").GetAttribute("style")!;
        Assert.Contains("--highlighter-bg:#112233", style);
        Assert.Contains("--highlighter-color:#ffffff", style);
        Assert.Contains("--highlighter-radius:4px", style);
        Assert.Contains("--highlighter-padding:0 4px", style);
    }

    [Fact]
    public void No_style_attribute_when_no_style_params_set()
    {
        var cut = RenderComponent<AtomHighlighter>(p => p.AddChildContent("Build with Blazor."));

        Assert.Null(cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void Works_through_nested_child_components_without_modifying_them()
    {
        JSInterop.SetupModule(ModulePath);

        var cut = RenderComponent<AtomHighlighter>(p => p
            .Add(c => c.Keywords, new[] { "Blazor", "C#" })
            .Add(c => c.ChildContent, (RenderTreeBuilder builder) =>
            {
                builder.OpenComponent<TestGrandparent>(0);
                builder.CloseComponent();
            }));

        Assert.Contains("Grandparent", cut.Markup);
        Assert.Contains("Parent", cut.Markup);
        Assert.Contains("Build with Blazor and C#.", cut.Markup);
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "highlightTextInElement");
    }

    private sealed class TestChild : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Build with Blazor and C#.");
            builder.CloseElement();
        }
    }

    private sealed class TestParent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "h2");
            builder.AddContent(1, "Parent");
            builder.CloseElement();
            builder.OpenComponent<TestChild>(2);
            builder.CloseComponent();
        }
    }

    private sealed class TestGrandparent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "h1");
            builder.AddContent(1, "Grandparent");
            builder.CloseElement();
            builder.OpenComponent<TestParent>(2);
            builder.CloseComponent();
        }
    }
}
