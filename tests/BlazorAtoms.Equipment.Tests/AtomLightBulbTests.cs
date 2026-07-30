using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Equipment.Tests;

public class AtomLightBulbTests : BunitContext
{
    [Fact]
    public void Off_by_default()
    {
        var cut = Render<AtomLightBulb>();
        var root = cut.Find(".atom-lightbulb");
        Assert.Null(root.GetAttribute("data-on"));
        Assert.Equal("false", root.GetAttribute("aria-checked"));
    }

    [Fact]
    public void Click_toggles_and_raises_IsOnChanged()
    {
        var changed = new List<bool>();
        var cut = Render<AtomLightBulb>(p => p
            .Add(c => c.IsOn, false)
            .Add(c => c.IsOnChanged, v => changed.Add(v)));

        cut.Find(".atom-lightbulb").Click();

        Assert.Equal([true], changed);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Enter")]
    public void Space_or_Enter_toggles(string key)
    {
        var changed = new List<bool>();
        var cut = Render<AtomLightBulb>(p => p
            .Add(c => c.IsOn, false)
            .Add(c => c.IsOnChanged, v => changed.Add(v)));

        cut.Find(".atom-lightbulb").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal([true], changed);
    }

    [Fact]
    public void Other_keys_do_nothing()
    {
        var changed = new List<bool>();
        var cut = Render<AtomLightBulb>(p => p.Add(c => c.IsOnChanged, v => changed.Add(v)));
        cut.Find(".atom-lightbulb").KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Assert.Empty(changed);
    }

    [Fact]
    public void Disabled_blocks_click_and_keyboard()
    {
        var changed = new List<bool>();
        var cut = Render<AtomLightBulb>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.IsOnChanged, v => changed.Add(v)));

        var root = cut.Find(".atom-lightbulb");
        root.Click();
        root.KeyDown(new KeyboardEventArgs { Key = " " });

        Assert.Empty(changed);
        Assert.Equal("true", root.GetAttribute("aria-disabled"));
        Assert.Null(root.GetAttribute("tabindex"));
    }

    [Fact]
    public void IsOn_true_sets_data_on_and_aria_checked()
    {
        var cut = Render<AtomLightBulb>(p => p.Add(c => c.IsOn, true));
        var root = cut.Find(".atom-lightbulb");
        Assert.Equal("true", root.GetAttribute("data-on"));
        Assert.Equal("true", root.GetAttribute("aria-checked"));
    }

    [Fact]
    public void AriaLabel_defaults_from_IsOn_and_is_overridable()
    {
        var off = Render<AtomLightBulb>();
        Assert.Equal("Light bulb, off", off.Find(".atom-lightbulb").GetAttribute("aria-label"));

        var on = Render<AtomLightBulb>(p => p.Add(c => c.IsOn, true));
        Assert.Equal("Light bulb, on", on.Find(".atom-lightbulb").GetAttribute("aria-label"));

        var overridden = Render<AtomLightBulb>(p => p.Add(c => c.AriaLabel, "Porch light"));
        Assert.Equal("Porch light", overridden.Find(".atom-lightbulb").GetAttribute("aria-label"));
    }

    [Fact]
    public void CssClass_and_Style_land_on_the_root_svg()
    {
        var cut = Render<AtomLightBulb>(p => p
            .Add(c => c.CssClass, "porch")
            .Add(c => c.Style, "opacity:.5"));
        var root = cut.Find(".atom-lightbulb");
        Assert.Contains("porch", root.GetAttribute("class"));
        Assert.Contains("opacity:.5", root.GetAttribute("style"));
    }

    [Fact]
    public void Width_maps_to_the_lightbulb_width_custom_property()
    {
        var cut = Render<AtomLightBulb>(p => p.Add(c => c.Width, 128d));
        Assert.Contains("--lightbulb-width:128px", cut.Find(".atom-lightbulb").GetAttribute("style"));
    }
}
