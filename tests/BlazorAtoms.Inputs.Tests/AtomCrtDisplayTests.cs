namespace BlazorAtoms.Inputs.Tests;

public class AtomCrtDisplayTests : TestContext
{
    [Fact]
    public void Animate_false_shows_full_value_immediately()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "READY.")
            .Add(c => c.Animate, false));

        Assert.Contains("READY.", cut.Find(".atom-crt-display-field").TextContent);
    }

    [Fact]
    public void Animate_true_starts_with_zero_visible_characters()
    {
        // The typewriter loop is async — the *initial* synchronous render has _displayedLength=0
        // and the field shows only the caret glyph.
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "HELLO WORLD")
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 5));

        var text = cut.Find(".atom-crt-display-field").TextContent;
        Assert.DoesNotContain("HELLO", text);
    }

    [Fact]
    public void Placeholder_shown_when_value_empty()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "")
            .Add(c => c.Placeholder, "> AWAITING INPUT")
            .Add(c => c.Animate, false));

        var field = cut.Find(".atom-crt-display-field");
        Assert.Equal("true", field.GetAttribute("data-empty"));
        Assert.Contains("AWAITING INPUT", field.TextContent);
    }

    [Theory]
    [InlineData(CrtPhosphor.Green, "green")]
    [InlineData(CrtPhosphor.Amber, "amber")]
    [InlineData(CrtPhosphor.Red, "red")]
    public void Phosphor_maps_to_data_attribute(CrtPhosphor phosphor, string expected)
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Phosphor, phosphor));

        Assert.Equal(expected, cut.Find(".atom-crt-display").GetAttribute("data-phosphor"));
    }

    [Fact]
    public void Effect_flags_emit_data_attributes_when_true()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p.Add(c => c.Value, "X"));

        var root = cut.Find(".atom-crt-display");
        Assert.Equal("true", root.GetAttribute("data-glow"));
        Assert.Equal("true", root.GetAttribute("data-scanlines"));
        Assert.Equal("true", root.GetAttribute("data-bezel"));
        Assert.Equal("true", root.GetAttribute("data-cursor"));
    }

    [Fact]
    public void Color_and_BackgroundColor_emit_custom_properties()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Color, "#00ff41")
            .Add(c => c.BackgroundColor, "#000000"));

        var style = cut.Find(".atom-crt-display").GetAttribute("style")!;
        Assert.Contains("--crt-color:#00ff41", style);
        Assert.Contains("--crt-bg:#000000", style);
    }

    [Fact]
    public void FontSize_and_Width_emit_custom_properties()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Width, 400d)
            .Add(c => c.FontSize, 22d));

        var style = cut.Find(".atom-crt-display").GetAttribute("style")!;
        Assert.Contains("--crt-width:400px", style);
        Assert.Contains("--crt-font-size:22px", style);
    }

    [Fact]
    public void Visible_false_hides_via_display_none()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-crt-display").GetAttribute("style") ?? "");
    }

    [Fact]
    public async Task Typing_reveals_value_over_time()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "AB")
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 200)); // 5ms/char

        // Wait a beat longer than 2 chars worth to give the async loop time to run + rerender.
        await Task.Delay(120);
        cut.Render();
        var text = cut.Find(".atom-crt-display-field").TextContent;
        Assert.Contains("AB", text);
    }

    [Fact]
    public async Task Value_change_restarts_animation_from_start()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "HELLO")
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 200));

        await Task.Delay(120);
        cut.SetParametersAndRender(p => p.Add(c => c.Value, "WORLD"));

        // Immediately after Value change, the new animation starts fresh — displayed prefix
        // isn't still 'HELLO' and hasn't yet reached 'WORLD' fully.
        var text = cut.Find(".atom-crt-display-field").TextContent;
        Assert.DoesNotContain("HELLO", text);
    }
}
