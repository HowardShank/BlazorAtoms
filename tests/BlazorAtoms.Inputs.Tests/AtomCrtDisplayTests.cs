namespace BlazorAtoms.Inputs.Tests;

public class AtomCrtDisplayTests : TestContext
{
    // Same enum-name -> data-attribute-string mapping used by AtomCrtInputTests. Data-driven so
    // new CrtFont values (SpecialElite, CutiveMono, ...) are exercised the moment they're added.
    public static IEnumerable<object[]> AllFontsData =>
        Enum.GetValues<CrtFont>().Select(f => new object[] { f, AtomCrtInputTests.FontDataAttr[f] });

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

    // ---- gap-fill tests --------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(AllFontsData))]
    public void Font_maps_to_data_attribute(CrtFont font, string expected)
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Font, font));

        Assert.Equal(expected, cut.Find(".atom-crt-display").GetAttribute("data-font"));
    }

    [Fact]
    public void Effect_flags_omit_data_attributes_when_false()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Glow, false)
            .Add(c => c.Scanlines, false)
            .Add(c => c.Bezel, false)
            .Add(c => c.CursorBlink, false));

        var root = cut.Find(".atom-crt-display");
        Assert.Null(root.GetAttribute("data-glow"));
        Assert.Null(root.GetAttribute("data-scanlines"));
        Assert.Null(root.GetAttribute("data-bezel"));
        Assert.Null(root.GetAttribute("data-cursor"));
    }

    [Fact]
    public void Label_renders_when_set()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Label, "Screen"));

        Assert.Contains("Screen", cut.Find("label").TextContent);
    }

    [Fact]
    public void HelpText_renders_in_subtext()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.HelpText, "system log"));

        Assert.Contains("system log", cut.Find(".atom-crt-display-subtext").TextContent);
    }

    [Fact]
    public void Multiline_true_sets_data_multiline_attribute()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Multiline, true));

        Assert.Equal("true", cut.Find(".atom-crt-display-field").GetAttribute("data-multiline"));
    }

    [Fact]
    public void Multiline_false_omits_data_multiline_attribute()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Multiline, false));

        Assert.Null(cut.Find(".atom-crt-display-field").GetAttribute("data-multiline"));
    }

    [Fact]
    public void Rows_drives_default_height_when_height_unset_and_multiline()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Rows, 6));
        // Multiline defaults to true; Height defaults to null; RootStyle emits Rows * 1.35 em.
        // 6 * 1.35 = 8.1; the "0.###" format trims trailing zeros.
        Assert.Contains("--crt-height:8.1em", cut.Find(".atom-crt-display").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Explicit_height_wins_over_rows_default()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Rows, 6)
            .Add(c => c.Height, 300d));

        var style = cut.Find(".atom-crt-display").GetAttribute("style") ?? "";
        Assert.Contains("--crt-height:300px", style);
        Assert.DoesNotContain("em", style); // no fallback em value alongside the explicit px
    }

    [Fact]
    public async Task Loop_true_restarts_after_finish()
    {
        // Value "A" completes in ~2ms at 500 CPS; 20ms loop delay; run for ~120ms so the loop
        // completes at least twice. The stable state after each loop iteration still shows "A" —
        // this is a regression guard that Loop doesn't leave the field blanked out permanently.
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "A")
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 500)
            .Add(c => c.Loop, true)
            .Add(c => c.LoopDelayMs, 20));

        await Task.Delay(120);
        cut.Render();
        Assert.Contains("A", cut.Find(".atom-crt-display-field").TextContent);
    }

    [Fact]
    public void Root_carries_status_role_and_polite_live_region()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p.Add(c => c.Value, "X"));

        var root = cut.Find(".atom-crt-display");
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("polite", root.GetAttribute("aria-live"));
    }

    [Fact]
    public async Task DisposeAsync_cancels_pending_animation_without_throwing()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "LONG STRING HERE")
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 5)); // slow enough that the loop is definitely mid-run

        // Component implements IAsyncDisposable; disposing while the internal Task.Delay loop is
        // pending must cancel cleanly (no OperationCanceledException surfacing, no orphan task).
        await ((IAsyncDisposable)cut.Instance).DisposeAsync();

        // Give any straggler continuation a moment to run; still no throw.
        await Task.Delay(30);
    }

    [Fact]
    public void Color_and_BackgroundColor_defaults_absent_when_unset()
    {
        var cut = RenderComponent<AtomCrtDisplay>(p => p.Add(c => c.Value, "X"));

        var style = cut.Find(".atom-crt-display").GetAttribute("style") ?? "";
        // Defaults come from the phosphor CSS rules, NOT inline vars — nothing should be emitted
        // inline for --crt-color / --crt-bg unless the params were set.
        Assert.DoesNotContain("--crt-color", style);
        Assert.DoesNotContain("--crt-bg", style);
    }
}
