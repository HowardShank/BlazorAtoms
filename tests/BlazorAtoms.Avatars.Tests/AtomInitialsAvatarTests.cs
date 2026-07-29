namespace BlazorAtoms.Avatars.Tests;

public class AtomInitialsAvatarTests : BunitContext
{
    [Fact]
    public void Derives_initials_from_two_word_name()
    {
        var cut = Render<AtomInitialsAvatar>(p => p.Add(c => c.Name, "Ada Lovelace"));
        Assert.Equal("AL", cut.Find(".atom-avatar-initials").TextContent);
    }

    [Fact]
    public void Single_word_name_truncates_to_max()
    {
        var cut = Render<AtomInitialsAvatar>(p => p
            .Add(c => c.Name, "Madonna")
            .Add(c => c.MaxInitials, 2));
        Assert.Equal("MA", cut.Find(".atom-avatar-initials").TextContent);
    }

    [Fact]
    public void Explicit_initials_win_and_are_not_truncated()
    {
        var cut = Render<AtomInitialsAvatar>(p => p
            .Add(c => c.Name, "Ada Lovelace")
            .Add(c => c.Initials, "+12"));
        Assert.Equal("+12", cut.Find(".atom-avatar-initials").TextContent);
    }

    [Fact]
    public void Auto_background_is_deterministic_for_same_name()
    {
        string Bg(string name) => Render<AtomInitialsAvatar>(p => p.Add(c => c.Name, name))
            .Find(".atom-avatar").GetAttribute("style") ?? "";

        Assert.Equal(Bg("Ada Lovelace"), Bg("Ada Lovelace"));
        Assert.Contains("background:#", Bg("Ada Lovelace"));
    }

    [Fact]
    public void Explicit_background_overrides_auto()
    {
        var cut = Render<AtomInitialsAvatar>(p => p
            .Add(c => c.Name, "Ada Lovelace")
            .Add(c => c.Background, "#123456"));
        Assert.Contains("background:#123456", cut.Find(".atom-avatar").GetAttribute("style"));
    }

    [Fact]
    public void Passes_shape_through()
    {
        var cut = Render<AtomInitialsAvatar>(p => p
            .Add(c => c.Name, "Ada Lovelace")
            .Add(c => c.Shape, AvatarShape.Hexagon));
        Assert.Equal("hexagon", cut.Find(".atom-avatar").GetAttribute("data-shape"));
    }
}
