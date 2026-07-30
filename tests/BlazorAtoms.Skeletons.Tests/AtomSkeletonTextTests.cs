namespace BlazorAtoms.Skeletons.Tests;

public class AtomSkeletonTextTests : BunitContext
{
    private static string StyleOf(IReadOnlyList<AngleSharp.Dom.IElement> els, int i) =>
        els[i].GetAttribute("style") ?? "";

    // ---- line count -----------------------------------------------------------------------------

    [Fact]
    public void Draws_three_lines_by_default()
    {
        var cut = Render<AtomSkeletonText>();

        Assert.Equal(3, cut.FindAll(".atom-skeleton-block").Count);
    }

    [Fact]
    public void Lines_is_honoured()
    {
        var cut = Render<AtomSkeletonText>(p => p.Add(c => c.Lines, 6));

        Assert.Equal(6, cut.FindAll(".atom-skeleton-block").Count);
    }

    [Fact]
    public void Zero_lines_renders_the_container_and_no_lines()
    {
        var cut = Render<AtomSkeletonText>(p => p.Add(c => c.Lines, 0));

        Assert.NotNull(cut.Find(".atom-skeleton-text"));
        Assert.Empty(cut.FindAll(".atom-skeleton-block"));
    }

    [Fact]
    public void A_negative_count_clamps_instead_of_throwing()
    {
        // A caller binding Lines to a computed value should not have to guard it.
        var cut = Render<AtomSkeletonText>(p => p.Add(c => c.Lines, -4));

        Assert.Empty(cut.FindAll(".atom-skeleton-block"));
    }

    // ---- the ragged last line -------------------------------------------------------------------

    [Fact]
    public void Only_the_last_line_is_short()
    {
        var cut = Render<AtomSkeletonText>(p => p.Add(c => c.Lines, 3));

        var lines = cut.FindAll(".atom-skeleton-block");
        Assert.DoesNotContain("--skeleton-width", StyleOf(lines, 0));
        Assert.DoesNotContain("--skeleton-width", StyleOf(lines, 1));
        Assert.Contains("--skeleton-width:60%", StyleOf(lines, 2));
    }

    [Fact]
    public void LastLineWidth_is_configurable()
    {
        var cut = Render<AtomSkeletonText>(p => p
            .Add(c => c.Lines, 2)
            .Add(c => c.LastLineWidth, "35%"));

        var lines = cut.FindAll(".atom-skeleton-block");
        Assert.Contains("--skeleton-width:35%", StyleOf(lines, 1));
    }

    [Fact]
    public void A_single_line_is_full_width()
    {
        // One short bar on its own reads as a mistake, not as the end of a paragraph.
        var cut = Render<AtomSkeletonText>(p => p.Add(c => c.Lines, 1));

        Assert.DoesNotContain("--skeleton-width", StyleOf(cut.FindAll(".atom-skeleton-block"), 0));
    }

    [Fact]
    public void Line_widths_are_identical_across_two_renders_of_the_same_input()
    {
        // Determinism matters: randomised widths would differ between the prerender and interactive
        // passes and visibly jump on hydration.
        var first = Render<AtomSkeletonText>(p => p.Add(c => c.Lines, 5));
        var second = Render<AtomSkeletonText>(p => p.Add(c => c.Lines, 5));

        Assert.Equal(
            first.FindAll(".atom-skeleton-block").Select(e => e.GetAttribute("style")),
            second.FindAll(".atom-skeleton-block").Select(e => e.GetAttribute("style")));
    }

    // ---- geometry -------------------------------------------------------------------------------

    [Fact]
    public void Lines_are_shorter_than_a_bare_block_by_default()
    {
        var cut = Render<AtomSkeletonText>();

        Assert.Contains("--skeleton-height:0.8rem", StyleOf(cut.FindAll(".atom-skeleton-block"), 0));
    }

    [Fact]
    public void Gap_and_Width_reach_the_container()
    {
        var cut = Render<AtomSkeletonText>(p => p
            .Add(c => c.Gap, "1rem")
            .Add(c => c.Width, "70%"));

        var style = cut.Find(".atom-skeleton-text").GetAttribute("style");
        Assert.Contains("--skeleton-gap:1rem", style);
        Assert.Contains("--skeleton-width:70%", style);
    }

    [Fact]
    public void LineHeight_and_LineRadius_reach_every_line()
    {
        var cut = Render<AtomSkeletonText>(p => p
            .Add(c => c.Lines, 2)
            .Add(c => c.LineHeight, "1.5rem")
            .Add(c => c.LineRadius, "9px"));

        foreach (var line in cut.FindAll(".atom-skeleton-block"))
        {
            var style = line.GetAttribute("style") ?? "";
            Assert.Contains("--skeleton-height:1.5rem", style);
            Assert.Contains("--skeleton-radius:9px", style);
        }
    }

    // ---- axis forwarding ------------------------------------------------------------------------

    [Fact]
    public void The_inherited_axes_are_forwarded_to_every_line()
    {
        // Text contributes layout only; the lines paint themselves, so the axes have to arrive there.
        var cut = Render<AtomSkeletonText>(p => p
            .Add(c => c.Lines, 3)
            .Add(c => c.Animation, SkeletonAnimation.Pulse)
            .Add(c => c.BaseColor, "#abc")
            .Add(c => c.HighlightColor, "#def")
            .Add(c => c.Duration, "3s"));

        foreach (var line in cut.FindAll(".atom-skeleton-block"))
        {
            Assert.Equal("pulse", line.GetAttribute("data-animation"));
            var style = line.GetAttribute("style") ?? "";
            Assert.Contains("--skeleton-base-color:#abc", style);
            Assert.Contains("--skeleton-highlight-color:#def", style);
            Assert.Contains("--skeleton-duration:3s", style);
        }
    }

    // ---- accessibility --------------------------------------------------------------------------

    [Fact]
    public void The_container_is_hidden_by_default_and_so_are_its_lines()
    {
        var cut = Render<AtomSkeletonText>();

        Assert.Equal("true", cut.Find(".atom-skeleton-text").GetAttribute("aria-hidden"));
        Assert.All(cut.FindAll(".atom-skeleton-block"),
            l => Assert.Equal("true", l.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Naming_the_container_does_not_name_the_lines()
    {
        // AriaLabel is not forwarded: one live region per skeleton, not one per line.
        var cut = Render<AtomSkeletonText>(p => p.Add(c => c.AriaLabel, "Loading article"));

        Assert.Equal("status", cut.Find(".atom-skeleton-text").GetAttribute("role"));
        Assert.All(cut.FindAll(".atom-skeleton-block"), l => Assert.False(l.HasAttribute("role")));
    }
}
