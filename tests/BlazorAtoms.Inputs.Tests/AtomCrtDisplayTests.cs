namespace BlazorAtoms.Inputs.Tests;

public class AtomCrtDisplayTests : BunitContext
{
    // Same enum-name -> data-attribute-string mapping used by AtomCrtInputTests. Data-driven so
    // new CrtFont values (SpecialElite, CutiveMono, ...) are exercised the moment they're added.
    public static IEnumerable<object[]> AllFontsData =>
        Enum.GetValues<CrtFont>().Select(f => new object[] { f, AtomCrtInputTests.FontDataAttr[f] });

    [Fact]
    public void Animate_false_shows_full_value_immediately()
    {
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "READY.")
            .Add(c => c.Animate, false));

        Assert.Contains("READY.", cut.Find(".atom-crt-display-field").TextContent);
    }

    [Fact]
    public void Animate_true_starts_with_zero_visible_characters()
    {
        // The typewriter loop is async — the *initial* synchronous render has _displayedLength=0
        // and the field shows only the caret glyph.
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "HELLO WORLD")
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 5));

        var text = cut.Find(".atom-crt-display-field").TextContent;
        Assert.DoesNotContain("HELLO", text);
    }

    [Fact]
    public void Placeholder_shown_when_value_empty()
    {
        var cut = Render<AtomCrtDisplay>(p => p
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
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Phosphor, phosphor));

        Assert.Equal(expected, cut.Find(".atom-crt-display").GetAttribute("data-phosphor"));
    }

    [Fact]
    public void Effect_flags_emit_data_attributes_when_true()
    {
        var cut = Render<AtomCrtDisplay>(p => p.Add(c => c.Value, "X"));

        var root = cut.Find(".atom-crt-display");
        Assert.Equal("true", root.GetAttribute("data-glow"));
        Assert.Equal("true", root.GetAttribute("data-scanlines"));
        Assert.Equal("true", root.GetAttribute("data-bezel"));
        Assert.Equal("true", root.GetAttribute("data-cursor"));
    }

    [Fact]
    public void Color_and_BackgroundColor_emit_custom_properties()
    {
        var cut = Render<AtomCrtDisplay>(p => p
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
        var cut = Render<AtomCrtDisplay>(p => p
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
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Visible, false));

        Assert.Contains("display:none", cut.Find(".atom-crt-display").GetAttribute("style") ?? "");
    }

    [Fact]
    public async Task Typing_reveals_value_over_time()
    {
        var cut = Render<AtomCrtDisplay>(p => p
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
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "HELLO")
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 200));

        await Task.Delay(120);
        cut.Render(p => p.Add(c => c.Value, "WORLD"));

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
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Font, font));

        Assert.Equal(expected, cut.Find(".atom-crt-display").GetAttribute("data-font"));
    }

    [Fact]
    public void Effect_flags_omit_data_attributes_when_false()
    {
        var cut = Render<AtomCrtDisplay>(p => p
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
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Label, "Screen"));

        Assert.Contains("Screen", cut.Find("label").TextContent);
    }

    [Fact]
    public void HelpText_renders_in_subtext()
    {
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.HelpText, "system log"));

        Assert.Contains("system log", cut.Find(".atom-crt-display-subtext").TextContent);
    }

    [Fact]
    public void Multiline_true_sets_data_multiline_attribute()
    {
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Multiline, true));

        Assert.Equal("true", cut.Find(".atom-crt-display-field").GetAttribute("data-multiline"));
    }

    [Fact]
    public void Multiline_false_omits_data_multiline_attribute()
    {
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Multiline, false));

        Assert.Null(cut.Find(".atom-crt-display-field").GetAttribute("data-multiline"));
    }

    [Fact]
    public void Rows_drives_default_height_when_height_unset_and_multiline()
    {
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Rows, 6));
        // Multiline defaults to true; Height defaults to null; RootStyle emits Rows * 1.35 em.
        // 6 * 1.35 = 8.1; the "0.###" format trims trailing zeros.
        Assert.Contains("--crt-height:8.1em", cut.Find(".atom-crt-display").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Explicit_height_wins_over_rows_default()
    {
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "X")
            .Add(c => c.Rows, 6)
            .Add(c => c.Height, 300d));

        var style = cut.Find(".atom-crt-display").GetAttribute("style") ?? "";
        Assert.Contains("--crt-height:300px", style);
        Assert.DoesNotContain("em", style); // no fallback em value alongside the explicit px
    }

    [Fact]
    public void Loop_true_restarts_after_finish()
    {
        // Regression guard that Loop doesn't leave the field blanked out permanently.
        //
        // This must POLL, not sample one instant. Each loop iteration deliberately resets
        // _displayedLength to 0 and renders that blank frame (AtomCrtDisplay.razor.cs) — that's what
        // a retype animation looks like — so at any given moment the field may legitimately hold
        // only the cursor. A single delay-then-assert therefore races the animation: it failed 5 runs
        // in 6 locally. The blank window is also far wider than the configured 2ms/char suggests,
        // because Task.Delay can't resolve below the ~15ms Windows timer tick.
        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, "A")
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 500)
            .Add(c => c.Loop, true)
            .Add(c => c.LoopDelayMs, 20));

        // Passes as soon as a typed frame appears; only fails if the field NEVER shows the value,
        // which is the actual regression being guarded against.
        cut.WaitForAssertion(
            () => Assert.Contains("A", cut.Find(".atom-crt-display-field").TextContent),
            TimeSpan.FromSeconds(5));
    }

    // ---- typing-speed ceiling --------------------------------------------------------------
    // Task.Delay can't resolve below the ~15.6ms Windows timer tick, so one-character-per-delay
    // capped typing at ~64 cps and silently ignored anything higher. AtomCrtDisplay now pins the
    // delay at the tick past that point and advances a stride of characters per step instead.
    // These assert the schedule directly rather than timing the animation — a wall-clock assertion
    // tight enough to prove the rate would be flaky on CI.

    [Theory]
    [InlineData(0.5, 2000, 1)]   // clamped-slow end
    [InlineData(20, 50, 1)]      // the default
    [InlineData(62.5, 16, 1)]    // exactly at the tick — still one char per delay
    public void Speeds_the_timer_can_resolve_keep_a_stride_of_one(double cps, int expectedDelay, int expectedStride)
    {
        var (delayMs, stride) = AtomCrtDisplay.ComputeTypingSchedule(cps);

        Assert.Equal(expectedDelay, delayMs);
        Assert.Equal(expectedStride, stride);
    }

    [Theory]
    [InlineData(125, 2)]
    [InlineData(500, 8)]
    [InlineData(1000, 16)]
    public void Speeds_past_the_timer_tick_hold_the_delay_and_raise_the_stride(double cps, int expectedStride)
    {
        var (delayMs, stride) = AtomCrtDisplay.ComputeTypingSchedule(cps);

        // Delay must not drop below the tick — shortening it further buys nothing and only makes
        // the configured rate a lie.
        Assert.Equal(16, delayMs);
        Assert.Equal(expectedStride, stride);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(125)]
    [InlineData(500)]
    [InlineData(2000)]
    public void Effective_rate_tracks_the_requested_rate(double cps)
    {
        var (delayMs, stride) = AtomCrtDisplay.ComputeTypingSchedule(cps);

        var effective = stride * 1000.0 / delayMs;
        // Within 15%: stride and delay are both integers, so exact rates aren't representable.
        // The point is that raising CharactersPerSecond now actually raises the speed.
        Assert.InRange(effective, cps * 0.85, cps * 1.15);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(60)]
    public void Already_achievable_speeds_keep_their_original_delay(double cps)
    {
        // Regression guard: the stride change must not alter timing for any speed that already
        // worked. This is the delay the pre-stride implementation computed.
        var originalDelayMs = Math.Max(1, (int)(1000.0 / Math.Max(0.1, cps)));

        var (delayMs, stride) = AtomCrtDisplay.ComputeTypingSchedule(cps);

        Assert.Equal(originalDelayMs, delayMs);
        Assert.Equal(1, stride);
    }

    [Fact]
    public void High_CharactersPerSecond_finishes_far_sooner_than_one_char_per_tick_allows()
    {
        // 205 chars at 2000 cps => stride 32, so ~7 steps of 16ms (~112ms). One character per tick
        // would need 205 * 16ms = ~3.3s, so this fails outright against the old implementation
        // while leaving a wide enough margin not to be flaky.
        //
        // 205 is deliberately not a multiple of the stride — it also covers the final clamped step.
        var value = new string('X', 205);

        var cut = Render<AtomCrtDisplay>(p => p
            .Add(c => c.Value, value)
            .Add(c => c.Animate, true)
            .Add(c => c.CharactersPerSecond, 2000));

        cut.WaitForAssertion(
            () => Assert.Equal(value, cut.Find(".atom-crt-display-field").TextContent.TrimEnd('█')),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Root_carries_status_role_and_polite_live_region()
    {
        var cut = Render<AtomCrtDisplay>(p => p.Add(c => c.Value, "X"));

        var root = cut.Find(".atom-crt-display");
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("polite", root.GetAttribute("aria-live"));
    }

    [Fact]
    public async Task DisposeAsync_cancels_pending_animation_without_throwing()
    {
        var cut = Render<AtomCrtDisplay>(p => p
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
        var cut = Render<AtomCrtDisplay>(p => p.Add(c => c.Value, "X"));

        var style = cut.Find(".atom-crt-display").GetAttribute("style") ?? "";
        // Defaults come from the phosphor CSS rules, NOT inline vars — nothing should be emitted
        // inline for --crt-color / --crt-bg unless the params were set.
        Assert.DoesNotContain("--crt-color", style);
        Assert.DoesNotContain("--crt-bg", style);
    }
}
