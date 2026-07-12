using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Ratings.Tests;

public class AtomRatingTests : TestContext
{
    // ---- structure ---------------------------------------------------------------------------

    [Fact]
    public void Renders_max_icons_default_five()
    {
        var cut = RenderComponent<AtomRating>();
        Assert.Equal(5, cut.FindAll(".atom-rating-item").Count);
    }

    [Fact]
    public void Max_controls_icon_count()
    {
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.Max, 10));
        Assert.Equal(10, cut.FindAll(".atom-rating-item").Count);
    }

    // ---- fractional fill ---------------------------------------------------------------------

    [Fact]
    public void Fractional_value_fills_partial_icon()
    {
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Value, 4.3)
            .Add(c => c.ReadOnly, true));
        var fills = cut.FindAll(".atom-rating-fill");
        Assert.Contains("width:100%", fills[0].GetAttribute("style"));
        Assert.Contains("width:100%", fills[3].GetAttribute("style"));
        // 4.3 → the 5th icon is 30% filled.
        Assert.Contains("width:30%", fills[4].GetAttribute("style"));
    }

    [Fact]
    public void Null_value_leaves_every_icon_empty()
    {
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.ReadOnly, true));
        foreach (var fill in cut.FindAll(".atom-rating-fill"))
            Assert.Contains("width:0%", fill.GetAttribute("style"));
    }

    // ---- role / a11y -------------------------------------------------------------------------

    [Fact]
    public void Interactive_by_default_is_a_slider()
    {
        var cut = RenderComponent<AtomRating>();
        var root = cut.Find(".atom-rating");
        Assert.Equal("slider", root.GetAttribute("role"));
        Assert.Equal("0", root.GetAttribute("tabindex"));
        Assert.Equal("5", root.GetAttribute("aria-valuemax"));
    }

    [Fact]
    public void ReadOnly_is_an_image_with_no_tabindex()
    {
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.ReadOnly, true));
        var root = cut.Find(".atom-rating");
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Null(root.GetAttribute("tabindex"));
    }

    [Fact]
    public void Disabled_sets_aria_disabled_and_blocks_clicks()
    {
        double? captured = 7;
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.ValueChanged, (double? v) => captured = v));
        Assert.Equal("true", cut.Find(".atom-rating").GetAttribute("aria-disabled"));
        cut.FindAll(".atom-rating-item")[0].Click(new MouseEventArgs { OffsetX = 20 });
        Assert.Equal(7, captured); // unchanged — disabled swallows the click
    }

    // ---- icons -------------------------------------------------------------------------------

    [Fact]
    public void Icon_enum_selects_glyph_path()
    {
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.Icon, RatingIcon.Heart));
        Assert.Equal(RatingGlyphs.Path(RatingIcon.Heart),
            cut.Find(".atom-rating-full path").GetAttribute("d"));
    }

    [Fact]
    public void Every_icon_has_a_unique_non_empty_glyph()
    {
        var icons = (RatingIcon[])Enum.GetValues(typeof(RatingIcon));
        var paths = icons.Select(RatingGlyphs.Path).ToArray();
        Assert.All(paths, d => Assert.False(string.IsNullOrWhiteSpace(d)));
        // Distinct count == enum count proves none silently fell back to the Star default.
        Assert.Equal(icons.Length, paths.Distinct().Count());
    }

    [Fact]
    public void Gem_glyph_has_dedicated_path()
    {
        Assert.NotEqual(RatingGlyphs.Path(RatingIcon.Diamond), RatingGlyphs.Path(RatingIcon.Gem));
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.Icon, RatingIcon.Gem));
        Assert.Equal(RatingGlyphs.Path(RatingIcon.Gem),
            cut.Find(".atom-rating-full path").GetAttribute("d"));
    }

    [Fact]
    public void IconPath_overrides_enum()
    {
        const string custom = "M0 0h24v24H0z";
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.IconPath, custom));
        Assert.Equal(custom, cut.Find(".atom-rating-full path").GetAttribute("d"));
    }

    [Fact]
    public void EmptyIcon_can_differ_from_filled()
    {
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Icon, RatingIcon.Star)
            .Add(c => c.EmptyIcon, RatingIcon.Circle));
        Assert.Equal(RatingGlyphs.Path(RatingIcon.Circle),
            cut.Find(".atom-rating-empty path").GetAttribute("d"));
        Assert.Equal(RatingGlyphs.Path(RatingIcon.Star),
            cut.Find(".atom-rating-full path").GetAttribute("d"));
    }

    // ---- labels ------------------------------------------------------------------------------

    [Fact]
    public void ShowValue_renders_numeric_label()
    {
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Value, 4.3)
            .Add(c => c.ShowValue, true));
        Assert.Equal("4.3", cut.Find(".atom-rating-value").TextContent);
    }

    [Fact]
    public void ShowValue_shows_unrated_text_when_null()
    {
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.ShowValue, true)
            .Add(c => c.UnratedText, "No rating"));
        Assert.Equal("No rating", cut.Find(".atom-rating-value").TextContent);
    }

    [Fact]
    public void Count_renders_formatted_in_parens()
    {
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.Count, 1204));
        Assert.Equal("(1,204)", cut.Find(".atom-rating-count").TextContent);
    }

    // ---- styling tokens ----------------------------------------------------------------------

    [Fact]
    public void Color_size_gap_emit_css_variables()
    {
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Color, "#e0245e")
            .Add(c => c.Size, 32)
            .Add(c => c.Gap, 10));
        var style = cut.Find(".atom-rating").GetAttribute("style");
        Assert.Contains("--rating-color:#e0245e;", style);
        Assert.Contains("--rating-size:32px;", style);
        Assert.Contains("--rating-gap:10px;", style);
    }

    [Fact]
    public void Rotation_emits_rotate_variable_in_degrees()
    {
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.Rotation, 45));
        Assert.Contains("--rating-rotate:45deg;", cut.Find(".atom-rating").GetAttribute("style"));
    }

    [Fact]
    public void No_rotate_variable_when_rotation_unset()
    {
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.Color, "#111"));
        Assert.DoesNotContain("--rating-rotate", cut.Find(".atom-rating").GetAttribute("style"));
    }

    [Fact]
    public void No_style_attribute_when_nothing_set()
    {
        var cut = RenderComponent<AtomRating>();
        Assert.False(cut.Find(".atom-rating").HasAttribute("style"));
    }

    // ---- interaction -------------------------------------------------------------------------

    [Fact]
    public void Click_commits_snapped_value()
    {
        double? captured = null;
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Size, 20)
            .Add(c => c.ValueChanged, (double? v) => captured = v));
        // Click the 4th icon (index 3) at its right edge → value 4.
        cut.FindAll(".atom-rating-item")[3].Click(new MouseEventArgs { OffsetX = 20 });
        Assert.Equal(4, captured);
    }

    [Fact]
    public void Half_step_click_commits_half()
    {
        double? captured = null;
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Size, 20)
            .Add(c => c.Step, 0.5)
            .Add(c => c.ValueChanged, (double? v) => captured = v));
        // Left half of the 3rd icon (index 2) → 2.5.
        cut.FindAll(".atom-rating-item")[2].Click(new MouseEventArgs { OffsetX = 5 });
        Assert.Equal(2.5, captured);
    }

    [Fact]
    public void Clearable_click_on_current_value_resets_to_null()
    {
        double? captured = 3;
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Value, 3.0)
            .Add(c => c.Size, 20)
            .Add(c => c.Step, 1.0)
            .Add(c => c.Clearable, true)
            .Add(c => c.ValueChanged, (double? v) => captured = v));
        // Click the 3rd icon (index 2) right edge → 3, equals current → clears.
        cut.FindAll(".atom-rating-item")[2].Click(new MouseEventArgs { OffsetX = 20 });
        Assert.Null(captured);
    }

    [Fact]
    public void ArrowRight_increases_by_step()
    {
        double? captured = null;
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.Step, 1.0)
            .Add(c => c.ValueChanged, (double? v) => captured = v));
        cut.Find(".atom-rating").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal(1, captured);
    }

    [Fact]
    public void Keyboard_does_nothing_when_readonly()
    {
        double? captured = 9;
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.ReadOnly, true)
            .Add(c => c.ValueChanged, (double? v) => captured = v));
        cut.Find(".atom-rating").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal(9, captured);
    }

    // ---- shared escape hatch -----------------------------------------------------------------

    [Fact]
    public void CssClass_appends_to_root()
    {
        var cut = RenderComponent<AtomRating>(p => p.Add(c => c.CssClass, "mine"));
        var cls = cut.Find(".atom-rating").GetAttribute("class");
        Assert.Contains("atom-rating", cls);
        Assert.Contains("mine", cls);
    }

    [Fact]
    public void Additional_attributes_splat_onto_root()
    {
        var cut = RenderComponent<AtomRating>(p => p
            .Add(c => c.ReadOnly, true)
            .AddUnmatched("title", "four stars"));
        Assert.Equal("four stars", cut.Find(".atom-rating").GetAttribute("title"));
    }
}
