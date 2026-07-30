namespace BlazorAtoms.Skeletons.Tests;

public class AtomSkeletonCardTests : BunitContext
{
    // ---- composition ----------------------------------------------------------------------------

    [Fact]
    public void Renders_media_avatar_and_lines_by_default()
    {
        var cut = Render<AtomSkeletonCard>();

        Assert.NotNull(cut.Find(".atom-skeleton-card"));
        Assert.NotNull(cut.Find(".atom-skeleton-card-body"));
        Assert.NotNull(cut.Find(".atom-skeleton-card-lines"));
        Assert.Single(cut.FindAll(".atom-skeleton-avatar"));
        Assert.Single(cut.FindAll(".atom-skeleton-text"));
        // media band + avatar + 3 text lines
        Assert.Equal(5, cut.FindAll(".atom-skeleton-block").Count);
    }

    [Fact]
    public void The_media_band_is_square_cornered_so_it_can_run_edge_to_edge()
    {
        var cut = Render<AtomSkeletonCard>(p => p
            .Add(c => c.ShowAvatar, false)
            .Add(c => c.Lines, 0)
            .Add(c => c.MediaHeight, "200px"));

        var media = cut.Find(".atom-skeleton-block");
        var style = media.GetAttribute("style");
        Assert.Contains("--skeleton-height:200px", style);
        Assert.Contains("--skeleton-radius:0", style);
    }

    [Fact]
    public void ShowMedia_false_drops_the_band()
    {
        var cut = Render<AtomSkeletonCard>(p => p
            .Add(c => c.ShowMedia, false)
            .Add(c => c.Lines, 0)
            .Add(c => c.ShowAvatar, false));

        Assert.Empty(cut.FindAll(".atom-skeleton-block"));
    }

    [Fact]
    public void ShowAvatar_false_drops_the_avatar_but_keeps_the_lines()
    {
        var cut = Render<AtomSkeletonCard>(p => p.Add(c => c.ShowAvatar, false));

        Assert.Empty(cut.FindAll(".atom-skeleton-avatar"));
        Assert.NotNull(cut.Find(".atom-skeleton-text"));
    }

    [Fact]
    public void Lines_reaches_the_text_block()
    {
        var cut = Render<AtomSkeletonCard>(p => p
            .Add(c => c.ShowMedia, false)
            .Add(c => c.ShowAvatar, false)
            .Add(c => c.Lines, 7));

        Assert.Equal(7, cut.FindAll(".atom-skeleton-block").Count);
    }

    [Fact]
    public void AvatarSize_reaches_the_avatar()
    {
        var cut = Render<AtomSkeletonCard>(p => p.Add(c => c.AvatarSize, "64px"));

        Assert.Contains("--skeleton-width:64px", cut.Find(".atom-skeleton-avatar").GetAttribute("style"));
    }

    [Fact]
    public void LineGap_reaches_the_text_container_not_the_card()
    {
        var cut = Render<AtomSkeletonCard>(p => p
            .Add(c => c.Gap, "2rem")
            .Add(c => c.LineGap, "4px"));

        Assert.Contains("--skeleton-gap:2rem", cut.Find(".atom-skeleton-card").GetAttribute("style"));
        Assert.Contains("--skeleton-gap:4px", cut.Find(".atom-skeleton-text").GetAttribute("style"));
    }

    // ---- theming --------------------------------------------------------------------------------

    [Fact]
    public void Card_level_tokens_reach_the_root()
    {
        var cut = Render<AtomSkeletonCard>(p => p
            .Add(c => c.Width, "24rem")
            .Add(c => c.Padding, "1rem")
            .Add(c => c.Gap, "0.5rem"));

        var style = cut.Find(".atom-skeleton-card").GetAttribute("style");
        Assert.Contains("--skeleton-width:24rem", style);
        Assert.Contains("--skeleton-padding:1rem", style);
        Assert.Contains("--skeleton-gap:0.5rem", style);
    }

    [Fact]
    public void The_inherited_axes_reach_every_composed_shape()
    {
        // The card paints nothing itself, so if forwarding breaks the card silently renders default
        // grey shimmer while claiming to be themed.
        var cut = Render<AtomSkeletonCard>(p => p
            .Add(c => c.Animation, SkeletonAnimation.Pulse)
            .Add(c => c.BaseColor, "#0a0a0a")
            .Add(c => c.HighlightColor, "#1b1b1b")
            .Add(c => c.Duration, "2.5s"));

        var shapes = cut.FindAll(".atom-skeleton-block");
        Assert.Equal(5, shapes.Count);
        foreach (var shape in shapes)
        {
            Assert.Equal("pulse", shape.GetAttribute("data-animation"));
            var style = shape.GetAttribute("style") ?? "";
            Assert.Contains("--skeleton-base-color:#0a0a0a", style);
            Assert.Contains("--skeleton-highlight-color:#1b1b1b", style);
            Assert.Contains("--skeleton-duration:2.5s", style);
        }
    }

    // ---- accessibility --------------------------------------------------------------------------

    [Fact]
    public void Only_the_card_is_named_when_an_AriaLabel_is_given()
    {
        var cut = Render<AtomSkeletonCard>(p => p.Add(c => c.AriaLabel, "Loading card"));

        Assert.Equal("status", cut.Find(".atom-skeleton-card").GetAttribute("role"));
        Assert.Equal("polite", cut.Find(".atom-skeleton-card").GetAttribute("aria-live"));
        // Exactly one live region: nested ones would announce the same load several times.
        Assert.Single(cut.FindAll("[role=status]"));
    }

    [Fact]
    public void Everything_is_hidden_from_assistive_tech_by_default()
    {
        var cut = Render<AtomSkeletonCard>();

        Assert.Empty(cut.FindAll("[role=status]"));
        Assert.Equal("true", cut.Find(".atom-skeleton-card").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Invisible_hides_the_whole_card()
    {
        var cut = Render<AtomSkeletonCard>(p => p.Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-skeleton-card").GetAttribute("style"));
    }
}
