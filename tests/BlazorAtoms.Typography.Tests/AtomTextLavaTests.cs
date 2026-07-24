using Bunit;
using BlazorAtoms.Typography;
using Xunit;

namespace BlazorAtoms.Typography.Tests;

public class AtomTextLavaTests
{
    [Fact]
    public void Renders_word_inside_lava_background()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p.Add(x => x.Word, "MOLTEN"));

        var root = cut.Find(".atom-text-lava");
        Assert.NotNull(root);
        var word = cut.Find(".atom-text-lava-word");
        Assert.Equal("MOLTEN", word.TextContent);
    }

    [Fact]
    public void Replay_forces_a_remount()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p.Add(x => x.Word, "MOLTEN"));
        var firstRoot = cut.Find(".atom-text-lava");

        cut.InvokeAsync(cut.Instance.Replay);
        var secondRoot = cut.Find(".atom-text-lava");

        // Same @key-remount trick AtomTextScramble's Replay() uses — restarts the CSS animation
        // from 0% (rising from below) rather than wherever it currently sits.
        Assert.NotSame(firstRoot, secondRoot);
    }

    [Fact]
    public void Empty_word_renders_nothing()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p.Add(x => x.Word, ""));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Loop_true_by_default_sets_infinite_alternate()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p.Add(x => x.Word, "hi"));

        var word = cut.Find(".atom-text-lava-word");
        var style = word.GetAttribute("style");
        Assert.Contains("animation-iteration-count:infinite;", style);
        Assert.Contains("animation-direction:alternate;", style);
        Assert.DoesNotContain("animation-fill-mode:forwards;", style);
    }

    [Fact]
    public void Loop_false_rises_once_and_holds()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p
            .Add(x => x.Word, "hi")
            .Add(x => x.Loop, false));

        var word = cut.Find(".atom-text-lava-word");
        var style = word.GetAttribute("style");
        Assert.Contains("animation-iteration-count:1;", style);
        Assert.Contains("animation-direction:normal;", style);
        Assert.Contains("animation-fill-mode:forwards;", style);
    }

    [Fact]
    public void RiseDistance_and_Duration_flow_into_word_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p
            .Add(x => x.Word, "hi")
            .Add(x => x.RiseDistance, "3rem")
            .Add(x => x.Duration, "2s"));

        var word = cut.Find(".atom-text-lava-word");
        var style = word.GetAttribute("style");
        Assert.Contains("--atom-text-lava-rise-distance:3rem;", style);
        Assert.Contains("animation-duration:2s;", style);
    }

    [Fact]
    public void GlowColor_flows_into_root_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p
            .Add(x => x.Word, "hi")
            .Add(x => x.GlowColor, "#00ffee"));

        var root = cut.Find(".atom-text-lava");
        Assert.Contains("--atom-text-lava-glow:#00ffee;", root.GetAttribute("style"));
    }

    [Fact]
    public void BgColors_flow_into_root_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p
            .Add(x => x.Word, "hi")
            .Add(x => x.BgColorHot, "#111111")
            .Add(x => x.BgColorCool, "#222222")
            .Add(x => x.BgColorBaseDark, "#333333")
            .Add(x => x.BgColorBaseLight, "#444444"));

        var root = cut.Find(".atom-text-lava");
        var style = root.GetAttribute("style");
        Assert.Contains("--atom-text-lava-bg-hot:#111111;", style);
        Assert.Contains("--atom-text-lava-bg-cool:#222222;", style);
        Assert.Contains("--atom-text-lava-bg-base-dark:#333333;", style);
        Assert.Contains("--atom-text-lava-bg-base-light:#444444;", style);
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextLava>(p => p
            .Add(x => x.Word, "hi")
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "color:red;"));

        var root = cut.Find(".atom-text-lava");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("color:red;", root.GetAttribute("style"));
    }
}
