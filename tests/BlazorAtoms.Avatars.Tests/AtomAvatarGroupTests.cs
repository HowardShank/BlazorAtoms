using System.Collections.Generic;
using System.Linq;

namespace BlazorAtoms.Avatars.Tests;

public class AtomAvatarGroupTests : TestContext
{
    private static readonly IReadOnlyList<string> Four =
        new[] { "Ada Lovelace", "Grace Hopper", "Alan Turing", "Katherine Johnson" };

    [Fact]
    public void Names_render_one_avatar_each_when_no_max()
    {
        var cut = RenderComponent<AtomAvatarGroup>(p => p.Add(c => c.Names, Four));
        Assert.Equal(4, cut.FindAll(".atom-avatar").Count);
    }

    [Fact]
    public void Max_caps_and_adds_overflow_chip()
    {
        var cut = RenderComponent<AtomAvatarGroup>(p => p
            .Add(c => c.Names, Four)
            .Add(c => c.Max, 2));

        // 2 visible + 1 overflow chip.
        Assert.Equal(3, cut.FindAll(".atom-avatar").Count);
        var texts = cut.FindAll(".atom-avatar-initials").Select(e => e.TextContent).ToList();
        Assert.Contains("+2", texts);
    }

    [Fact]
    public void No_overflow_chip_when_within_max()
    {
        var cut = RenderComponent<AtomAvatarGroup>(p => p
            .Add(c => c.Names, Four)
            .Add(c => c.Max, 4));
        Assert.DoesNotContain(cut.FindAll(".atom-avatar-initials").Select(e => e.TextContent), t => t.StartsWith("+"));
    }

    [Fact]
    public void ChildContent_used_when_no_names()
    {
        var cut = RenderComponent<AtomAvatarGroup>(p => p
            .AddChildContent<AtomAvatar>(a => a.Add(c => c.Src, "/a.jpg"))
            .AddChildContent<AtomAvatar>(a => a.Add(c => c.Src, "/b.jpg")));
        Assert.Equal(2, cut.FindAll(".atom-avatar").Count);
    }

    [Fact]
    public void Root_emits_overlap_and_ring_tokens()
    {
        var cut = RenderComponent<AtomAvatarGroup>(p => p
            .Add(c => c.Names, Four)
            .Add(c => c.Overlap, 16)
            .Add(c => c.RingWidth, 3)
            .Add(c => c.RingColor, "#000000"));
        var style = cut.Find(".atom-avatar-group").GetAttribute("style") ?? "";
        Assert.Contains("--avg-overlap:16px", style);
        Assert.Contains("--avg-ring:3px", style);
        Assert.Contains("--avg-ring-color:#000000", style);
    }
}
