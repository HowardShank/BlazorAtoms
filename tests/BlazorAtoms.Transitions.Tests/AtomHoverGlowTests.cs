using Bunit;
using Xunit;

namespace BlazorAtoms.Transitions.Tests;

/// <summary>bUnit coverage for <see cref="AtomHoverGlow"/>. The native (anchor-positioning) vs.
/// JS-fallback split happens in OnAfterRenderAsync via AtomBrowserSupport — not exercised here
/// (would need JSInterop setup like AtomTransitionTests); these assertions cover markup/style
/// wiring, which is identical regardless of which path is active.</summary>
public class AtomHoverGlowTests
{
    private const string ModulePath = "./_content/BlazorAtoms.Behaviors/atom-behaviors.js";

    // Stubs the native-support check to "supported" so OnAfterRenderAsync never attempts to
    // import atom-hover-glow.js's own fallback module — these tests cover markup/style wiring,
    // which is identical either way, so there's nothing gained by also exercising the fallback
    // import path here.
    private static void SetupNativeSupport(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("supportsCss", _ => true).SetResult(true);
    }

    [Fact]
    public void Renders_child_content_and_an_indicator_element()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx);

        var cut = ctx.Render<AtomHoverGlow>(p => p.AddChildContent("<a href=\"#\">Home</a><a href=\"#\">About</a>"));

        var root = cut.Find(".atom-hover-glow");
        Assert.Equal(2, root.QuerySelectorAll("a").Length);
        var indicator = cut.Find(".atom-hover-glow-indicator");
        Assert.Equal("true", indicator.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void GlowColor_Blur_and_Radius_flow_into_root_style()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx);

        var cut = ctx.Render<AtomHoverGlow>(p => p
            .AddChildContent("<span>hi</span>")
            .Add(x => x.GlowColor, "#00ffee")
            .Add(x => x.GlowBlur, "16px")
            .Add(x => x.GlowRadius, "1rem"));

        var root = cut.Find(".atom-hover-glow");
        var style = root.GetAttribute("style");
        Assert.Contains("--atom-hover-glow-color:#00ffee;", style);
        Assert.Contains("--atom-hover-glow-blur:16px;", style);
        Assert.Contains("--atom-hover-glow-radius:1rem;", style);
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx);

        var cut = ctx.Render<AtomHoverGlow>(p => p
            .AddChildContent("<span>hi</span>")
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "color:red;"));

        var root = cut.Find(".atom-hover-glow");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("color:red;", root.GetAttribute("style"));
    }

    [Fact]
    public void Works_with_arbitrary_non_link_children()
    {
        using var ctx = new BunitContext();
        SetupNativeSupport(ctx);

        var cut = ctx.Render<AtomHoverGlow>(p => p.AddChildContent(
            "<div class=\"card\">Card 1</div><button>Card 2</button><span>Card 3</span>"));

        var root = cut.Find(".atom-hover-glow");
        Assert.NotNull(root.QuerySelector("div.card"));
        Assert.NotNull(root.QuerySelector("button"));
        Assert.NotNull(root.QuerySelector("span"));
    }
}
