using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace BlazorAtoms.Clocks.Tests;

public class AtomTimeZoneMapTests : BunitContext
{
    // Live=false so no PeriodicTimer spins up; Loose JS so HighlightViewerZone detection is a no-op.
    private IRenderedComponent<AtomTimeZoneMap> Render(Action<ComponentParameterCollectionBuilder<AtomTimeZoneMap>>? extra = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<AtomTimeZoneMap>(p =>
        {
            p.Add(c => c.Live, false);
            extra?.Invoke(p);
        });
    }

    [Fact]
    public void Renders_svg_with_ocean_and_continents()
    {
        var cut = Render();
        Assert.Single(cut.FindAll("svg.tz-svg"));
        Assert.Single(cut.FindAll("rect.tz-ocean"));
        Assert.Single(cut.FindAll("path.tz-land"));
        Assert.Equal("img", cut.Find(".atom-tz-map").GetAttribute("role"));
    }

    [Fact]
    public void Continents_toggle_off()
    {
        var cut = Render(p => p.Add(c => c.ShowContinents, false));
        Assert.Empty(cut.FindAll("path.tz-land"));
    }

    [Fact]
    public void Renders_24_bands_with_data_offset()
    {
        var cut = Render();
        var bands = cut.FindAll("g.tz-band");
        Assert.Equal(24, bands.Count);
        var offsets = bands.Select(b => int.Parse(b.GetAttribute("data-offset")!)).OrderBy(x => x).ToArray();
        Assert.Equal(Enumerable.Range(-12, 24).ToArray(), offsets);
        Assert.Equal(24, cut.FindAll("g.tz-band rect").Count);
    }

    [Fact]
    public void Bands_toggle_off()
    {
        var cut = Render(p => p.Add(c => c.ShowBands, false));
        Assert.Empty(cut.FindAll("g.tz-band"));
    }

    [Fact]
    public void Default_cities_render_as_pins_with_titles()
    {
        var cut = Render();
        var pins = cut.FindAll("g.tz-pin");
        Assert.Equal(13, pins.Count);
        Assert.Equal(13, cut.FindAll("g.tz-pin circle").Count);
        Assert.All(cut.FindAll("g.tz-pin title"), t => Assert.False(string.IsNullOrWhiteSpace(t.TextContent)));
    }

    [Fact]
    public void Pins_toggle_off()
    {
        var cut = Render(p => p.Add(c => c.ShowPins, false));
        Assert.Empty(cut.FindAll("g.tz-pin"));
    }

    [Fact]
    public void Custom_cities_replace_defaults()
    {
        var cities = new[] { new MapCity("Reykjavik", -21.9, 64.1, "Atlantic/Reykjavik") };
        var cut = Render(p => p.Add(c => c.Cities, cities));
        Assert.Single(cut.FindAll("g.tz-pin"));
    }

    [Fact]
    public void Band_labels_toggle()
    {
        var on = Render();
        Assert.NotEmpty(on.FindAll("svg text"));

        var off = Render(p => p.Add(c => c.ShowBandLabels, false).Add(c => c.ShowPinLabels, false));
        Assert.Empty(off.FindAll("svg text"));
    }

    [Fact]
    public void Terminator_and_sun_toggle()
    {
        var on = Render();
        Assert.Single(on.FindAll("polygon.tz-night"));
        Assert.Single(on.FindAll("g.tz-sun"));

        var off = Render(p => p.Add(c => c.ShowTerminator, false).Add(c => c.ShowSunMarker, false));
        Assert.Empty(off.FindAll("polygon.tz-night"));
        Assert.Empty(off.FindAll("g.tz-sun"));
    }

    [Fact]
    public void Graticule_off_by_default_on_when_requested()
    {
        Assert.Empty(Render().FindAll("g.tz-graticule"));
        Assert.Single(Render(p => p.Add(c => c.ShowGraticule, true)).FindAll("g.tz-graticule"));
    }

    [Fact]
    public void Projection_places_null_island_at_center()
    {
        // A city at (lon 0, lat 0) must land at viewBox center: X=180, Y=90.
        var cut = Render(p => p.Add(c => c.Cities, new[] { new MapCity("Null", 0, 0, "UTC") }));
        var circle = cut.Find("g.tz-pin circle");
        Assert.Equal(180.0, double.Parse(circle.GetAttribute("cx")!), 3);
        Assert.Equal(90.0, double.Parse(circle.GetAttribute("cy")!), 3);
    }

    [Fact]
    public void Terminator_polygon_has_many_points()
    {
        var cut = Render();
        var pts = cut.Find("polygon.tz-night").GetAttribute("points") ?? "";
        // ~121 samples + 2 closing corners; sanity-check it built a real curve, not empty.
        Assert.True(pts.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 100);
        Assert.Matches(@"^[\d\.,\-\s]+$", pts);
    }

    [Fact]
    public void Selectable_band_click_raises_offset()
    {
        int? got = null;
        var cut = Render(p => p
            .Add(c => c.Selectable, true)
            .Add(c => c.OnBandSelect, EventCallback.Factory.Create<int>(this, o => got = o)));

        var band = cut.FindAll("g.tz-band").First(b => b.GetAttribute("data-offset") == "5");
        band.Click();
        Assert.Equal(5, got);
        Assert.Contains("is-selected", cut.FindAll("g.tz-band").First(b => b.GetAttribute("data-offset") == "5").GetAttribute("class"));
    }

    [Fact]
    public void Not_selectable_click_does_nothing()
    {
        int? got = null;
        var cut = Render(p => p
            .Add(c => c.OnBandSelect, EventCallback.Factory.Create<int>(this, o => got = o)));
        cut.FindAll("g.tz-band").First().Click();
        Assert.Null(got);
    }

    [Fact]
    public void City_pin_title_shows_name_and_time()
    {
        var cut = Render(p => p.Add(c => c.Cities, new[] { new MapCity("Tokyo", 139.7, 35.7, "Asia/Tokyo") }));
        var title = cut.Find("g.tz-pin title").TextContent;
        Assert.StartsWith("Tokyo", title);
        Assert.Matches(@"\d", title); // contains a date/time digit
    }

    [Fact]
    public void Width_and_color_tokens_emitted()
    {
        var cut = Render(p => p
            .Add(c => c.Width, 900)
            .Add(c => c.Ocean, "#001")
            .Add(c => c.Land, "#0f0")
            .Add(c => c.PinColor, "#f00"));
        var style = cut.Find(".atom-tz-map").GetAttribute("style") ?? "";
        Assert.Contains("--tzm-width:900px", style);
        Assert.Contains("--tzm-ocean:#001", style);
        Assert.Contains("--tzm-land:#0f0", style);
        Assert.Contains("--tzm-pin:#f00", style);
    }

    [Fact]
    public void Viewer_highlight_renders_under_loose_js()
    {
        // Loose JS → timezone() yields default → UTC band highlighted; must not throw.
        var cut = Render(p => p.Add(c => c.HighlightViewerZone, true));
        Assert.Single(cut.FindAll("svg.tz-svg"));
    }
}
