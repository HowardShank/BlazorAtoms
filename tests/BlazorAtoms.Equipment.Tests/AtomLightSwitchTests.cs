using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Equipment.Tests;

public class AtomLightSwitchTests : BunitContext
{
    [Fact]
    public void Off_by_default_and_lever_points_down()
    {
        var cut = Render<AtomLightSwitch>();
        var root = cut.Find(".atom-lightswitch");
        Assert.Null(root.GetAttribute("data-on"));
        Assert.Equal("false", root.GetAttribute("aria-checked"));
        Assert.Contains("rotate(18 40 82)", cut.Find("g").GetAttribute("transform"));
    }

    [Fact]
    public void Click_toggles_and_flips_the_lever()
    {
        var changed = new List<bool>();
        var cut = Render<AtomLightSwitch>(p => p
            .Add(c => c.IsOn, false)
            .Add(c => c.IsOnChanged, v => changed.Add(v)));

        cut.Find(".atom-lightswitch").Click();

        Assert.Equal([true], changed);
    }

    [Fact]
    public void On_state_rotates_lever_the_other_way()
    {
        var cut = Render<AtomLightSwitch>(p => p.Add(c => c.IsOn, true));
        Assert.Contains("rotate(-18 40 82)", cut.Find("g").GetAttribute("transform"));
        Assert.Equal("true", cut.Find(".atom-lightswitch").GetAttribute("data-on"));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Enter")]
    public void Space_or_Enter_toggles(string key)
    {
        var changed = new List<bool>();
        var cut = Render<AtomLightSwitch>(p => p.Add(c => c.IsOnChanged, v => changed.Add(v)));
        cut.Find(".atom-lightswitch").KeyDown(new KeyboardEventArgs { Key = key });
        Assert.Equal([true], changed);
    }

    [Fact]
    public void Disabled_blocks_click_and_keyboard()
    {
        var changed = new List<bool>();
        var cut = Render<AtomLightSwitch>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.IsOnChanged, v => changed.Add(v)));

        var root = cut.Find(".atom-lightswitch");
        root.Click();
        root.KeyDown(new KeyboardEventArgs { Key = " " });

        Assert.Empty(changed);
        Assert.Equal("true", root.GetAttribute("aria-disabled"));
        Assert.Null(root.GetAttribute("tabindex"));
    }

    [Fact]
    public void AriaLabel_defaults_from_IsOn_and_is_overridable()
    {
        var off = Render<AtomLightSwitch>();
        Assert.Equal("Light switch, off", off.Find(".atom-lightswitch").GetAttribute("aria-label"));

        var overridden = Render<AtomLightSwitch>(p => p.Add(c => c.AriaLabel, "Hallway switch"));
        Assert.Equal("Hallway switch", overridden.Find(".atom-lightswitch").GetAttribute("aria-label"));
    }

    [Fact]
    public void CssClass_and_Style_land_on_the_root_svg()
    {
        var cut = Render<AtomLightSwitch>(p => p
            .Add(c => c.CssClass, "hallway")
            .Add(c => c.Style, "opacity:.5"));
        var root = cut.Find(".atom-lightswitch");
        Assert.Contains("hallway", root.GetAttribute("class"));
        Assert.Contains("opacity:.5", root.GetAttribute("style"));
    }

    [Fact]
    public void Width_maps_to_the_lightswitch_width_custom_property()
    {
        var cut = Render<AtomLightSwitch>(p => p.Add(c => c.Width, 96d));
        Assert.Contains("--lightswitch-width:96px", cut.Find(".atom-lightswitch").GetAttribute("style"));
    }
}
