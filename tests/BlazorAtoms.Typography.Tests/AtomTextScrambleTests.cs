using Bunit;
using BlazorAtoms.Typography;
using Xunit;

namespace BlazorAtoms.Typography.Tests;

public class AtomTextScrambleTests
{
    [Fact]
    public void Renders_one_span_per_character()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextScramble>(p => p.Add(x => x.Word, "cat"));

        var chars = cut.FindAll(".atom-text-scramble-char");
        Assert.Equal(3, chars.Count);
        Assert.Equal("c", chars[0].TextContent);
        Assert.Equal("a", chars[1].TextContent);
        Assert.Equal("t", chars[2].TextContent);
    }

    [Fact]
    public void Empty_word_renders_nothing()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextScramble>(p => p.Add(x => x.Word, ""));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Stagger_delay_scales_with_character_index()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextScramble>(p => p.Add(x => x.Word, "abc"));

        var chars = cut.FindAll(".atom-text-scramble-char");
        Assert.Contains("animation-delay:calc(var(--atom-text-scramble-stagger) * 0);", chars[0].GetAttribute("style"));
        Assert.Contains("animation-delay:calc(var(--atom-text-scramble-stagger) * 1);", chars[1].GetAttribute("style"));
        Assert.Contains("animation-delay:calc(var(--atom-text-scramble-stagger) * 2);", chars[2].GetAttribute("style"));
    }

    [Theory]
    [InlineData(TextScrambleEffect.RevolveScale, "atom-text-scramble-revolve-scale")]
    [InlineData(TextScrambleEffect.BallDrop, "atom-text-scramble-ball-drop")]
    [InlineData(TextScrambleEffect.SideSlide, "atom-text-scramble-side-slide")]
    [InlineData(TextScrambleEffect.RevolveDrop, "atom-text-scramble-revolve-drop")]
    [InlineData(TextScrambleEffect.DropVanish, "atom-text-scramble-drop-vanish")]
    [InlineData(TextScrambleEffect.Twister, "atom-text-scramble-twister")]
    [InlineData(TextScrambleEffect.LeftRight, "atom-text-scramble-left-right")]
    public void Effect_selects_root_class(TextScrambleEffect effect, string expectedClass)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextScramble>(p => p
            .Add(x => x.Word, "hi")
            .Add(x => x.Effect, effect));

        var root = cut.Find(".atom-text-scramble");
        Assert.Contains(expectedClass, root.ClassList);
    }

    [Fact]
    public void StaggerDelay_and_AnimationDuration_flow_into_root_style()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextScramble>(p => p
            .Add(x => x.Word, "hi")
            .Add(x => x.StaggerDelay, "80ms")
            .Add(x => x.AnimationDuration, "1s"));

        var root = cut.Find(".atom-text-scramble");
        Assert.Contains("--atom-text-scramble-stagger:80ms;", root.GetAttribute("style"));
        Assert.Contains("--atom-text-scramble-duration:1s;", root.GetAttribute("style"));
    }

    [Fact]
    public void Changing_Word_forces_the_root_to_remount()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextScramble>(p => p.Add(x => x.Word, "cat"));
        var firstRoot = cut.Find(".atom-text-scramble");

        cut.Render(p => p.Add(x => x.Word, "dog"));
        var secondRoot = cut.Find(".atom-text-scramble");

        // @key forces a full teardown/rebuild of the subtree (not a diff/patch), which is what
        // restarts the CSS entrance animation on word change without any JS.
        Assert.NotSame(firstRoot, secondRoot);
        var chars = cut.FindAll(".atom-text-scramble-char");
        Assert.Equal("d", chars[0].TextContent);
    }

    [Fact]
    public void Replay_forces_a_remount_even_when_Word_is_unchanged()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextScramble>(p => p.Add(x => x.Word, "cat"));

        var firstRoot = cut.Find(".atom-text-scramble");

        cut.InvokeAsync(cut.Instance.Replay);
        var secondRoot = cut.Find(".atom-text-scramble");

        Assert.NotSame(firstRoot, secondRoot);
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextScramble>(p => p
            .Add(x => x.Word, "hi")
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "color:red;"));

        var root = cut.Find(".atom-text-scramble");
        Assert.Contains("extra", root.ClassList);
        Assert.EndsWith("color:red;", root.GetAttribute("style"));
    }
}
