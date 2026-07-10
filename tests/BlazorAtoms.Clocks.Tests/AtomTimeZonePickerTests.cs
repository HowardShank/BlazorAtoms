using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorAtoms.Clocks.Tests;

public class AtomTimeZonePickerTests : TestContext
{
    private IRenderedComponent<AtomTimeZonePicker> Render(Action<ComponentParameterCollectionBuilder<AtomTimeZonePicker>>? extra = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return RenderComponent<AtomTimeZonePicker>(p => extra?.Invoke(p));
    }

    private static void Open(IRenderedComponent<AtomTimeZonePicker> cut) => cut.Find(".tzp-trigger").Click();

    [Fact]
    public void Shows_placeholder_when_unset()
    {
        var cut = Render(p => p.Add(c => c.Placeholder, "Pick one"));
        Assert.Contains("Pick one", cut.Find(".tzp-value").TextContent);
        Assert.Contains("is-placeholder", cut.Find(".tzp-value").GetAttribute("class"));
        Assert.Empty(cut.FindAll(".tzp-panel"));   // closed by default
    }

    [Fact]
    public void Selected_value_shows_id_and_offset()
    {
        var cut = Render(p => p.Add(c => c.Value, "Asia/Tokyo"));
        var text = cut.Find(".tzp-value").TextContent;
        Assert.Contains("Asia/Tokyo", text);
        Assert.Contains("UTC+09:00", text);         // Tokyo is UTC+9 year-round
        Assert.DoesNotContain("is-placeholder", cut.Find(".tzp-value").GetAttribute("class"));
    }

    [Fact]
    public void Opening_lists_all_system_zones()
    {
        var cut = Render();
        Open(cut);
        Assert.NotEmpty(cut.FindAll(".tzp-panel"));
        // Every runtime knows a large spread of zones — far more than a handful.
        Assert.True(cut.FindAll(".tzp-option").Count > 50);
    }

    [Fact]
    public void Custom_zones_limit_the_list()
    {
        var zones = new[]
        {
            TimeZoneInfo.FindSystemTimeZoneById("UTC"),
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"),
        };
        var cut = Render(p => p.Add(c => c.Zones, zones));
        Open(cut);
        Assert.Equal(2, cut.FindAll(".tzp-option").Count);
    }

    [Fact]
    public void Filter_narrows_options()
    {
        var cut = Render();
        Open(cut);
        var all = cut.FindAll(".tzp-option").Count;
        cut.Find(".tzp-search").Input("tokyo");
        var filtered = cut.FindAll(".tzp-option").Count;
        Assert.True(filtered < all);
        Assert.All(cut.FindAll(".tzp-option"), o => Assert.Contains("Tokyo", o.TextContent));
    }

    [Fact]
    public void Region_groups_render_headers()
    {
        var cut = Render(p => p.Add(c => c.ShowRegionGroups, true));
        Open(cut);
        Assert.NotEmpty(cut.FindAll(".tzp-group"));
    }

    [Fact]
    public void No_region_groups_when_disabled()
    {
        var cut = Render(p => p.Add(c => c.ShowRegionGroups, false));
        Open(cut);
        Assert.Empty(cut.FindAll(".tzp-group"));
    }

    [Fact]
    public void Offset_spans_toggle_with_show_offset()
    {
        var cut = Render(p => p.Add(c => c.ShowOffset, false));
        Open(cut);
        Assert.Empty(cut.FindAll(".tzp-offset"));

        var withOffset = Render(p => p.Add(c => c.ShowOffset, true));
        Open(withOffset);
        Assert.NotEmpty(withOffset.FindAll(".tzp-offset"));
    }

    [Fact]
    public void Clicking_option_raises_value_changed_and_closes()
    {
        string? got = null;
        var zones = new[]
        {
            TimeZoneInfo.FindSystemTimeZoneById("UTC"),
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"),
        };
        var cut = Render(p => p
            .Add(c => c.Zones, zones)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => got = v)));

        Open(cut);
        cut.FindAll(".tzp-option").First(o => o.TextContent.Contains("Tokyo")).Click();

        Assert.Equal("Asia/Tokyo", got);
        Assert.Empty(cut.FindAll(".tzp-panel"));   // closes on select
    }

    [Fact]
    public void Detect_button_present_by_default_absent_when_disabled()
    {
        var cut = Render();
        Open(cut);
        Assert.NotEmpty(cut.FindAll(".tzp-detect"));

        var no = Render(p => p.Add(c => c.AllowDetect, false));
        Open(no);
        Assert.Empty(no.FindAll(".tzp-detect"));
    }

    [Fact]
    public void Detect_selects_browser_zone_from_js()
    {
        string? got = null;
        var module = JSInterop.SetupModule("./_content/BlazorAtoms.Clocks/atom-clocks.js");
        module.Setup<string?>("timezoneId").SetResult("Asia/Tokyo");

        var cut = RenderComponent<AtomTimeZonePicker>(p => p
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => got = v)));
        Open(cut);
        cut.Find(".tzp-detect").Click();

        Assert.Equal("Asia/Tokyo", got);
    }

    [Fact]
    public void Width_token_emitted()
    {
        var cut = Render(p => p.Add(c => c.Width, 320));
        Assert.Contains("--tzp-width:320px", cut.Find(".atom-tz-picker").GetAttribute("style"));
    }

    [Fact]
    public void Keyboard_arrow_then_enter_selects()
    {
        string? got = null;
        var cut = Render(p => p
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => got = v)));
        Open(cut);
        var search = cut.Find(".tzp-search");
        search.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        search.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.NotNull(got);   // some zone got picked
    }
}
