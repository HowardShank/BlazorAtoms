using Bunit;
using BlazorAtoms.Typography;
using Xunit;

namespace BlazorAtoms.Typography.Tests;

public class AtomTextCycleTests
{
    [Fact]
    public void Renders_each_word_plus_a_duplicate_of_the_first()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p.Add(x => x.Words, new[] { "wOrK", "lifeStyle", "Everything" }));

        var items = cut.FindAll(".atom-text-cycle-item");
        Assert.Equal(4, items.Count); // 3 words + 1 duplicate wrap-around row
        Assert.Equal("wOrK", items[0].TextContent);
        Assert.Equal("lifeStyle", items[1].TextContent);
        Assert.Equal("Everything", items[2].TextContent);
        Assert.Equal("wOrK", items[3].TextContent);
        Assert.Equal("true", items[3].GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Emits_keyframes_sized_to_word_count()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p.Add(x => x.Words, new[] { "a", "b", "c", "d" }));

        var style = cut.Find("style").TextContent;
        Assert.Contains("@keyframes atom-text-cycle-v-n4", style);
        Assert.Contains("100%{transform:translateY(calc(var(--atom-text-cycle-item-height) * -4));}", style);
    }

    [Fact]
    public void Single_word_renders_statically_without_animation_or_style_block()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p.Add(x => x.Words, new[] { "solo" }));

        Assert.Empty(cut.FindAll("style"));
        var items = cut.FindAll(".atom-text-cycle-item");
        Assert.Single(items);
        Assert.Equal("solo", items[0].TextContent);
        var track = cut.Find(".atom-text-cycle-track");
        Assert.DoesNotContain("animation:", track.GetAttribute("style"));
    }

    [Fact]
    public void Empty_words_renders_nothing()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p.Add(x => x.Words, Array.Empty<string>()));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Duration_and_easing_flow_into_track_animation_shorthand()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p
            .Add(x => x.Words, new[] { "a", "b" })
            .Add(x => x.Duration, 3000)
            .Add(x => x.Easing, "linear"));

        var track = cut.Find(".atom-text-cycle-track");
        Assert.Contains("animation:atom-text-cycle-v-n2 3000ms linear infinite;", track.GetAttribute("style"));
        Assert.Contains("animation-direction:normal;", track.GetAttribute("style"));
    }

    [Theory]
    [InlineData(TextCycleEffect.SlideBottomToTop, "atom-text-cycle-axis-v", "normal", "translateY")]
    [InlineData(TextCycleEffect.SlideTopToBottom, "atom-text-cycle-axis-v", "reverse", "translateY")]
    [InlineData(TextCycleEffect.SlideRightToLeft, "atom-text-cycle-axis-h", "normal", "translateX")]
    [InlineData(TextCycleEffect.SlideLeftToRight, "atom-text-cycle-axis-h", "reverse", "translateX")]
    [InlineData(TextCycleEffect.SpinClockwise, "atom-text-cycle-axis-v", "normal", "translateY")]
    [InlineData(TextCycleEffect.SpinCounterClockwise, "atom-text-cycle-axis-v", "reverse", "translateY")]
    public void Effect_selects_axis_and_playback_direction(
        TextCycleEffect effect, string axisClass, string expectedPlayback, string expectedTransformFn)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p
            .Add(x => x.Words, new[] { "a", "b", "c" })
            .Add(x => x.Effect, effect));

        var root = cut.Find(".atom-text-cycle");
        Assert.Contains(axisClass, root.ClassList);

        var track = cut.Find(".atom-text-cycle-track");
        Assert.Contains($"animation-direction:{expectedPlayback};", track.GetAttribute("style"));

        var style = cut.Find("style").TextContent;
        Assert.Contains($"transform:{expectedTransformFn}(0", style);
    }

    [Fact]
    public void Horizontal_slide_uses_ItemWidth_not_ItemHeight()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p
            .Add(x => x.Words, new[] { "a", "b" })
            .Add(x => x.Effect, TextCycleEffect.SlideRightToLeft)
            .Add(x => x.ItemWidth, "10rem"));

        var viewport = cut.Find(".atom-text-cycle-viewport");
        Assert.Equal("width:10rem;", viewport.GetAttribute("style"));

        var style = cut.Find("style").TextContent;
        Assert.Contains("@keyframes atom-text-cycle-h-n2", style);
        Assert.Contains("var(--atom-text-cycle-item-width)", style);
        Assert.DoesNotContain("--atom-text-cycle-item-height", style);
    }

    [Fact]
    public void Spin_reuses_vertical_slide_layout_including_duplicate_row()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p
            .Add(x => x.Words, new[] { "a", "b", "c" })
            .Add(x => x.Effect, TextCycleEffect.SpinClockwise));

        // Spin rides translateY on the same transform as vertical slide, so it needs the exact
        // same seamless-wrap duplicate row (unlike the abandoned pure-rotation design, where a
        // full 360deg turn alone was seamless).
        var items = cut.FindAll(".atom-text-cycle-item");
        Assert.Equal(4, items.Count);

        var viewport = cut.Find(".atom-text-cycle-viewport");
        Assert.Equal("height:3.5rem;", viewport.GetAttribute("style")); // ItemHeight, not ItemWidth
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void Spin_rotate_is_a_multiple_of_360_times_SpinTurns_per_step(int spinTurns, int step)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p
            .Add(x => x.Words, new[] { "a", "b", "c" })
            .Add(x => x.Effect, TextCycleEffect.SpinClockwise)
            .Add(x => x.SpinTurns, spinTurns));

        var style = cut.Find("style").TextContent;
        var expectedAngle = -step * 360 * spinTurns;
        Assert.Contains($"translateY(calc(var(--atom-text-cycle-item-height) * -{step})) rotate({expectedAngle}deg)", style);
    }

    [Fact]
    public void CssClass_and_Style_append_to_root()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AtomTextCycle>(p => p
            .Add(x => x.Words, new[] { "a", "b" })
            .Add(x => x.CssClass, "extra")
            .Add(x => x.Style, "color:red;"));

        var root = cut.Find(".atom-text-cycle");
        Assert.Contains("extra", root.ClassList);
        Assert.Equal("color:red;", root.GetAttribute("style"));
    }
}
