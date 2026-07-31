using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Equipment.Tests;

public class AtomFanTests : BunitContext
{
    [Fact]
    public void Off_by_default_with_no_spin_attribute()
    {
        var cut = Render<AtomFan>();
        var root = cut.Find(".atom-fan");
        Assert.Equal("off", root.GetAttribute("data-speed"));
        Assert.Null(root.GetAttribute("data-spinning"));
        Assert.Equal("OFF", cut.Find(".atom-fan-label").TextContent);
    }

    [Fact]
    public void Click_cycles_Off_Low_Medium_High_then_wraps_to_Off()
    {
        var current = FanSpeed.Off;
        var cut = Render<AtomFan>(p => p
            .Add(c => c.Speed, current)
            .Add(c => c.SpeedChanged, v => current = v));

        var expected = new[] { FanSpeed.Low, FanSpeed.Medium, FanSpeed.High, FanSpeed.Off };
        foreach (var next in expected)
        {
            cut.Find(".atom-fan").Click();
            Assert.Equal(next, current);
        }
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Enter")]
    public void Space_or_Enter_cycles_speed(string key)
    {
        var changed = new List<FanSpeed>();
        var cut = Render<AtomFan>(p => p.Add(c => c.SpeedChanged, v => changed.Add(v)));
        cut.Find(".atom-fan").KeyDown(new KeyboardEventArgs { Key = key });
        Assert.Equal([FanSpeed.Low], changed);
    }

    [Fact]
    public void Disabled_blocks_click_and_keyboard()
    {
        var changed = new List<FanSpeed>();
        var cut = Render<AtomFan>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.SpeedChanged, v => changed.Add(v)));

        var root = cut.Find(".atom-fan");
        root.Click();
        root.KeyDown(new KeyboardEventArgs { Key = " " });

        Assert.Empty(changed);
        Assert.Equal("true", root.GetAttribute("aria-disabled"));
        Assert.Null(root.GetAttribute("tabindex"));
    }

    [Theory]
    [InlineData(FanSpeed.Low, "LOW")]
    [InlineData(FanSpeed.Medium, "MED")]
    [InlineData(FanSpeed.High, "HIGH")]
    public void Nonzero_speed_sets_spinning_attribute_and_label(FanSpeed speed, string label)
    {
        var cut = Render<AtomFan>(p => p.Add(c => c.Speed, speed));
        var root = cut.Find(".atom-fan");
        Assert.Equal("true", root.GetAttribute("data-spinning"));
        Assert.Equal(speed.ToString().ToLowerInvariant(), root.GetAttribute("data-speed"));
        Assert.Equal(label, cut.Find(".atom-fan-label").TextContent);
    }

    [Fact]
    public void Direction_reflected_as_data_attribute()
    {
        var forward = Render<AtomFan>();
        Assert.Equal("forward", forward.Find(".atom-fan").GetAttribute("data-direction"));

        var reverse = Render<AtomFan>(p => p.Add(c => c.Direction, FanDirection.Reverse));
        Assert.Equal("reverse", reverse.Find(".atom-fan").GetAttribute("data-direction"));
    }

    [Fact]
    public void Kind_selects_desk_or_ceiling_housing()
    {
        var desk = Render<AtomFan>();
        Assert.Equal("desk", desk.Find(".atom-fan").GetAttribute("data-style"));
        Assert.NotEmpty(desk.FindAll(".atom-fan-grille"));
        Assert.Empty(desk.FindAll(".atom-fan-mount"));

        var ceiling = Render<AtomFan>(p => p.Add(c => c.Kind, FanStyle.Ceiling));
        Assert.Equal("ceiling", ceiling.Find(".atom-fan").GetAttribute("data-style"));
        Assert.Empty(ceiling.FindAll(".atom-fan-grille"));
        Assert.NotEmpty(ceiling.FindAll(".atom-fan-mount"));
    }

    [Fact]
    public void ShowDirectionIndicator_and_ShowSpeedLabel_are_opt_out()
    {
        var cut = Render<AtomFan>(p => p
            .Add(c => c.ShowDirectionIndicator, false)
            .Add(c => c.ShowSpeedLabel, false));

        Assert.Empty(cut.FindAll(".atom-fan-direction"));
        Assert.Empty(cut.FindAll(".atom-fan-label"));
    }

    [Fact]
    public void Every_blade_group_has_exactly_three_blades()
    {
        foreach (var kind in new[] { FanStyle.Desk, FanStyle.Ceiling })
        {
            var cut = Render<AtomFan>(p => p.Add(c => c.Kind, kind));
            Assert.Equal(3, cut.FindAll(".atom-fan-blade").Count);
        }
    }

    [Fact]
    public void AriaLabel_defaults_from_Speed_and_is_overridable()
    {
        var cut = Render<AtomFan>(p => p.Add(c => c.Speed, FanSpeed.High));
        Assert.Equal("Fan, speed High", cut.Find(".atom-fan").GetAttribute("aria-label"));

        var overridden = Render<AtomFan>(p => p.Add(c => c.AriaLabel, "Bedroom fan"));
        Assert.Equal("Bedroom fan", overridden.Find(".atom-fan").GetAttribute("aria-label"));
    }

    [Fact]
    public void CssClass_and_Style_land_on_the_root()
    {
        var cut = Render<AtomFan>(p => p
            .Add(c => c.CssClass, "bedroom")
            .Add(c => c.Style, "opacity:.5"));
        var root = cut.Find(".atom-fan");
        Assert.Contains("bedroom", root.GetAttribute("class"));
        Assert.Contains("opacity:.5", root.GetAttribute("style"));
    }

    [Fact]
    public void Width_maps_to_the_fan_width_custom_property()
    {
        var cut = Render<AtomFan>(p => p.Add(c => c.Width, 150d));
        Assert.Contains("--fan-width:150px", cut.Find(".atom-fan").GetAttribute("style"));
    }
}
