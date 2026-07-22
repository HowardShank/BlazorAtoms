namespace BlazorAtoms.Avatars.Tests;

public class AtomAvatarTests : BunitContext
{
    [Fact]
    public void No_src_renders_silhouette_svg()
    {
        var cut = Render<AtomAvatar>();
        Assert.NotNull(cut.Find(".atom-avatar-figure"));
        Assert.Empty(cut.FindAll(".atom-avatar-img"));
    }

    [Fact]
    public void Src_renders_image_not_silhouette()
    {
        var cut = Render<AtomAvatar>(p => p.Add(c => c.Src, "/u/ada.jpg"));
        var img = cut.Find(".atom-avatar-img");
        Assert.Equal("/u/ada.jpg", img.GetAttribute("src"));
        Assert.Empty(cut.FindAll(".atom-avatar-figure"));
    }

    [Fact]
    public void Shape_maps_to_data_attribute()
    {
        var cut = Render<AtomAvatar>(p => p.Add(c => c.Shape, Shape.Hexagon));
        Assert.Equal("hexagon", cut.Find(".atom-avatar").GetAttribute("data-shape"));
    }

    [Fact]
    public void Default_shape_is_circle()
    {
        var cut = Render<AtomAvatar>();
        Assert.Equal("circle", cut.Find(".atom-avatar").GetAttribute("data-shape"));
    }

    [Fact]
    public void Role_img_and_alt_label()
    {
        var cut = Render<AtomAvatar>(p => p
            .Add(c => c.Src, "/u/ada.jpg")
            .Add(c => c.Alt, "Ada Lovelace"));
        var root = cut.Find(".atom-avatar");
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Equal("Ada Lovelace", root.GetAttribute("aria-label"));
        Assert.Equal("Ada Lovelace", cut.Find(".atom-avatar-img").GetAttribute("alt"));
    }

    [Fact]
    public void Label_falls_back_when_no_alt()
    {
        var cut = Render<AtomAvatar>();
        Assert.Equal("avatar", cut.Find(".atom-avatar").GetAttribute("aria-label"));
    }

    [Fact]
    public void Size_and_radius_tokens_emitted()
    {
        var cut = Render<AtomAvatar>(p => p
            .Add(c => c.Shape, Shape.Rounded)
            .Add(c => c.Size, 64)
            .Add(c => c.Radius, 16));
        var style = cut.Find(".atom-avatar").GetAttribute("style") ?? "";
        Assert.Contains("--av-size:64px", style);
        Assert.Contains("--av-radius:16px", style);
    }

    [Fact]
    public void Solid_background_emitted()
    {
        var cut = Render<AtomAvatar>(p => p.Add(c => c.Background, "#123456"));
        Assert.Contains("background:#123456", cut.Find(".atom-avatar").GetAttribute("style"));
    }

    [Fact]
    public void Background_gradient_emitted_when_both_stops_set()
    {
        var cut = Render<AtomAvatar>(p => p
            .Add(c => c.BackgroundGradientFrom, "#0ea5e9")
            .Add(c => c.BackgroundGradientTo, "#7c3aed")
            .Add(c => c.BackgroundGradientAngle, 90));
        var style = cut.Find(".atom-avatar").GetAttribute("style") ?? "";
        Assert.Contains("linear-gradient(90deg,#0ea5e9,#7c3aed)", style);
    }

    [Fact]
    public void Figure_gradient_defines_linear_gradient_and_uses_it()
    {
        var cut = Render<AtomAvatar>(p => p
            .Add(c => c.FigureGradientFrom, "#ffffff")
            .Add(c => c.FigureGradientTo, "#e0e7ff"));

        Assert.NotNull(cut.Find("linearGradient"));
        var fill = cut.Find(".atom-avatar-figure g").GetAttribute("fill") ?? "";
        Assert.StartsWith("url(#fig-", fill);
    }

    [Fact]
    public void Solid_figure_color_used_without_gradient()
    {
        var cut = Render<AtomAvatar>(p => p.Add(c => c.FigureColor, "#ff8800"));
        Assert.Equal("#ff8800", cut.Find(".atom-avatar-figure g").GetAttribute("fill"));
        Assert.Empty(cut.FindAll("linearGradient"));
    }

    [Fact]
    public void Border_emitted_when_color_set()
    {
        var cut = Render<AtomAvatar>(p => p
            .Add(c => c.BorderColor, "#22d3ee")
            .Add(c => c.BorderWidth, 3));
        Assert.Contains("border:3px solid #22d3ee", cut.Find(".atom-avatar").GetAttribute("style"));
    }
}
