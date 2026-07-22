namespace BlazorAtoms.ActivityIndicators.Tests;

// Smoke tests — prove the wrapper's discovery, name resolution, parameter
// filtering, and unknown-name callback are wired correctly.
public class AtomActivityIndicatorTests : BunitContext
{
    [Fact]
    public void No_name_renders_a_random_indicator()
    {
        var cut = Render<AtomActivityIndicator>();

        // A round AtomActivity* indicator was discovered and rendered.
        Assert.NotNull(cut.Find("svg"));
    }

    [Theory]
    [InlineData("AtomActivityGears")]
    [InlineData("ActivityGears")]        // "Atom" prefix is optional
    [InlineData("Gears")]            // "AtomActivity" prefix is optional
    public void Named_resolves_the_specific_indicator(string name)
    {
        var cut = Render<AtomActivityIndicator>(p => p.Add(c => c.Name, name));

        Assert.Contains("activity-gears", cut.Markup);
    }

    [Fact]
    public void Unsupported_parameter_is_filtered_not_thrown()
    {
        // AtomActivitySwarm declares no Fill; forwarding it must be silently dropped, never throw.
        var cut = Render<AtomActivityIndicator>(p => p
            .Add(c => c.Name, "Swarm")
            .Add(c => c.Fill, "red"));

        Assert.Contains("activity-swarm", cut.Markup);
    }

    [Fact]
    public void Unknown_name_invokes_callback_then_falls_back_to_random()
    {
        var called = 0;
        string? requested = null;

        var cut = Render<AtomActivityIndicator>(p => p
            .Add(c => c.Name, "DoesNotExist")
            .Add(c => c.OnUnknownName, EventCallback.Factory.Create<string>(this, s => { called++; requested = s; })));

        Assert.Equal(1, called);
        Assert.Equal("DoesNotExist", requested);
        Assert.NotNull(cut.Find("svg"));   // still rendered something
    }

    // --- Escape hatch on an SVG root + forwarding through the wrapper ---

    [Fact]
    public void Svg_root_class_appends_and_splat_passes_through()
    {
        var cut = Render<Indicators.AtomActivityDna>(p => p
            .Add(c => c.CssClass, "brand")
            .AddUnmatched("data-test", "z"));
        var svg = cut.Find("svg");
        Assert.Equal("activity-dna brand", svg.GetAttribute("class"));
        Assert.Equal("z", svg.GetAttribute("data-test"));
    }

    [Fact]
    public void Wrapper_forwards_class_and_splat_to_chosen_indicator()
    {
        var cut = Render<AtomActivityIndicator>(p => p
            .Add(c => c.Name, "Gears")
            .Add(c => c.CssClass, "brand")
            .AddUnmatched("data-test", "z"));
        var svg = cut.Find("svg");
        Assert.Contains("brand", svg.GetAttribute("class"));
        Assert.Equal("z", svg.GetAttribute("data-test"));
    }
}
